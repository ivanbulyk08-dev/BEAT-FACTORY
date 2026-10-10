using System;
using System.Collections.Generic;

namespace BeatFactory
{
    public enum ShowPart { Intro = 0, Verse = 1, BuildUp = 2, Drop = 3, Chorus = 4, BigMoment = 5, Outro = 6 }

    /// <summary>A rendered concert soundtrack (+ crowd bed) with its timing markers (sections, beats, kicks).</summary>
    public class ConcertTrack
    {
        public ConcertMusicProfile profile;
        public int sampleRate;
        public float[] music, crowd;
        /// <summary>Start time (s) of each ShowPart, plus the end of the track as the last entry.</summary>
        public float[] sections = new float[8];
        public readonly List<float> kicks = new List<float>();
        public float Length => sections[7];
        public float BeatSeconds => profile.BeatSeconds;

        public ShowPart PartAt(float t)
        {
            for (int i = 6; i >= 0; i--) if (t >= sections[i]) return (ShowPart)i;
            return ShowPart.Intro;
        }

        public float Start(ShowPart p) => sections[(int)p];
    }

    /// <summary>
    /// Tiny offline synthesizer for original concert music: kick, 808 (with glides), snare/clap, hats (rolls / drill triplets),
    /// crash, riser, impact, pads, leads (supersaw, bell, e-piano, square), reverb, sidechain and soft clipping.
    /// Arrangement: INTRO → VERSE → BUILD-UP → DROP → CHORUS → BIG MOMENT → OUTRO (fade). Rendered on a worker thread.
    /// </summary>
    public static class ConcertTrackSynth
    {
        public const int Rate = 22050;
        /// <summary>Show plan in seconds (snapped to the beat): intro, verse, build-up, drop, chorus, big moment, outro, end.</summary>
        static readonly float[] Plan = { 0f, 1.8f, 4.3f, 5.8f, 7.8f, 10.6f, 12.6f, 15.2f };

        // ------------------------------------------------------------------ helpers

        static float Freq(float midi) => 440f * (float)Math.Pow(2, (midi - 69f) / 12f);
        static float Saw(double ph) => (float)(2.0 * (ph - Math.Floor(ph + 0.5)));
        static float Sq(double ph) => ph - Math.Floor(ph) < 0.5 ? 1f : -1f;
        static float Tri(double ph) => 1f - 4f * Math.Abs((float)(ph - Math.Floor(ph + 0.25)) - 0.25f) * 2f;
        static float Sin(double ph) => (float)Math.Sin(ph * 2 * Math.PI);

        static float Osc(Wave w, double ph, double ph2)
        {
            switch (w)
            {
                case Wave.Sine: return Sin(ph);
                case Wave.Triangle: return Tri(ph);
                case Wave.Saw: return Saw(ph);
                case Wave.Square: return Sq(ph) * 0.7f;
                case Wave.SuperSaw: return (Saw(ph) + Saw(ph * 1.006) + Saw(ph * 0.994)) * 0.45f;
                case Wave.Bell: return Sin(ph) * 0.7f + Sin(ph * 2.76) * 0.25f * (float)Math.Exp(-ph2 * 6) + Sin(ph * 5.4) * 0.12f * (float)Math.Exp(-ph2 * 12);
                default: return Sin(ph) * 0.8f + Sin(ph * 2) * 0.25f * (float)Math.Exp(-ph2 * 4) + Tri(ph * 3) * 0.08f; // e-piano
            }
        }

        static int S(float t) => (int)(t * Rate);

        class Bus
        {
            public float[] dry, send;
            public Bus(int n) { dry = new float[n]; send = new float[n]; }
        }

        // ------------------------------------------------------------------ instruments

        static void Kick(float[] b, float t0, float gain, List<float> kicks)
        {
            kicks?.Add(t0);
            int s0 = S(t0), n = S(0.42f);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float f = 46f + 120f * (float)Math.Exp(-t * 32f);
                ph += f / Rate;
                float env = (float)Math.Exp(-t * 7.5f);
                float click = i < 60 ? (1f - i / 60f) * 0.4f : 0f;
                b[s0 + i] += (Sin(ph) * env + click) * gain;
            }
        }

        static void Bass(float[] b, float t0, float len, float midi, float fromMidi, float gain, float drive, bool is808)
        {
            int s0 = S(t0), n = S(len + (is808 ? 0.15f : 0.05f));
            double ph = 0;
            float k = 1f + drive * 6f, norm = (float)Math.Tanh(k);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float m = fromMidi > 0 && t < 0.08f ? fromMidi + (midi - fromMidi) * (t / 0.08f) : midi;
                ph += Freq(m) / Rate;
                float env = Math.Min(1f, t / 0.004f) * (is808 ? (float)Math.Exp(-t * 1.1f) : (float)Math.Exp(-t * 3f));
                if (t > len) env *= Math.Max(0f, 1f - (t - len) / (is808 ? 0.15f : 0.05f));
                float x = Sin(ph) + (is808 ? 0.15f * Sin(ph * 2) : 0.1f * Tri(ph));
                b[s0 + i] += (float)Math.Tanh(x * k) / norm * env * gain;
            }
        }

        static void Snare(float[] b, float t0, float gain, Random r, bool clap)
        {
            int s0 = S(t0), n = S(0.3f);
            float prev = 0;
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float nz = (float)(r.NextDouble() * 2 - 1);
                float hp = nz - prev; prev = nz;
                float env;
                if (clap) env = (t < 0.03f ? (float)Math.Exp(-(t % 0.011f) * 120f) : (float)Math.Exp(-(t - 0.03f) * 18f));
                else env = (float)Math.Exp(-t * 20f);
                ph += 185.0 / Rate;
                float tone = clap ? 0 : Sin(ph) * (float)Math.Exp(-t * 35f) * 0.6f;
                b[s0 + i] += (hp * 0.55f * env + tone) * gain;
            }
        }

        static void Hat(float[] b, float t0, float gain, bool open, Random r)
        {
            int s0 = S(t0), n = S(open ? 0.32f : 0.06f);
            float prev = 0, prev2 = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float nz = (float)(r.NextDouble() * 2 - 1);
                float hp = nz - 2 * prev + prev2; prev2 = prev; prev = nz;
                b[s0 + i] += hp * 0.25f * (float)Math.Exp(-t * (open ? 9f : 70f)) * gain;
            }
        }

        static void Crash(float[] b, float t0, float gain, Random r)
        {
            int s0 = S(t0), n = S(2.2f);
            float prev = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float nz = (float)(r.NextDouble() * 2 - 1);
                float hp = nz - prev; prev = nz;
                b[s0 + i] += hp * 0.35f * (float)Math.Exp(-t * 2.2f) * gain;
            }
        }

        static void Riser(float[] b, float t0, float len, float gain, Random r)
        {
            int s0 = S(t0), n = S(len);
            float lp = 0;
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float u = i / (float)n;
                float nz = (float)(r.NextDouble() * 2 - 1);
                float a = 0.02f + 0.5f * u * u;
                lp += (nz - lp) * a;
                ph += (220f + 1400f * u * u) / Rate;
                b[s0 + i] += (lp * 0.6f + Saw(ph) * 0.12f) * u * u * gain;
            }
        }

        static void Impact(float[] b, float t0, float gain, Random r)
        {
            int s0 = S(t0), n = S(1.6f);
            double ph = 0;
            float lp = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)Rate;
                ph += (38f + 30f * (float)Math.Exp(-t * 6)) / Rate;
                float nz = (float)(r.NextDouble() * 2 - 1);
                lp += (nz - lp) * 0.08f;
                b[s0 + i] += (Sin(ph) * (float)Math.Exp(-t * 2.2f) + lp * 0.8f * (float)Math.Exp(-t * 5f)) * gain;
            }
        }

        static void Note(Bus bus, float t0, float len, float midi, Wave w, float gain, float bright, float attack, float release, float sendAmt, bool pluck)
        {
            int s0 = S(t0), n = S(len + release);
            double ph = 0;
            float lp = 0;
            float cutoff = 0.04f + 0.5f * bright * bright;
            float f = Freq(midi);
            for (int i = 0; i < n && s0 + i < bus.dry.Length; i++)
            {
                float t = i / (float)Rate;
                ph += f * (1 + 0.003 * Math.Sin(t * 30)) / Rate;
                float env = attack > 0 ? Math.Min(1f, t / attack) : 1f;
                if (pluck) env *= (float)Math.Exp(-t * 5f) * 0.8f + 0.2f;
                if (t > len) env *= Math.Max(0f, 1f - (t - len) / release);
                float x = Osc(w, ph, t);
                lp += (x - lp) * cutoff;
                float v = lp * env * gain;
                bus.dry[s0 + i] += v;
                bus.send[s0 + i] += v * sendAmt;
            }
        }

        static void Reverb(float[] input, float[] output, float mix)
        {
            int[] combs = { 557, 593, 641, 677 };
            int[] alls = { 113, 277 };
            var cb = new float[combs.Length][];
            var ci = new int[combs.Length];
            var cl = new float[combs.Length];
            for (int i = 0; i < combs.Length; i++) cb[i] = new float[combs[i] * 2];
            var ab = new float[alls.Length][];
            var ai = new int[alls.Length];
            for (int i = 0; i < alls.Length; i++) ab[i] = new float[alls[i]];
            for (int s = 0; s < input.Length; s++)
            {
                float x = input[s], acc = 0;
                for (int c = 0; c < combs.Length; c++)
                {
                    var buf = cb[c];
                    float y = buf[ci[c]];
                    cl[c] = y * 0.7f + cl[c] * 0.3f;
                    buf[ci[c]] = x + cl[c] * 0.8f;
                    ci[c] = (ci[c] + 1) % buf.Length;
                    acc += y;
                }
                acc *= 0.25f;
                for (int a = 0; a < alls.Length; a++)
                {
                    var buf = ab[a];
                    float y = buf[ai[a]];
                    float v = acc + y * 0.5f;
                    buf[ai[a]] = v;
                    acc = y - v * 0.5f;
                    ai[a] = (ai[a] + 1) % buf.Length;
                }
                output[s] += acc * mix;
            }
        }

        // ------------------------------------------------------------------ arrangement

        static bool In(int step, params int[] steps) { foreach (var s in steps) if (s == step) return true; return false; }

        public static ConcertTrack Render(ConcertMusicProfile p, float crowdSize, int seed)
        {
            var r = new Random(seed);
            var track = new ConcertTrack { profile = p, sampleRate = Rate };
            float beat = p.BeatSeconds;
            for (int i = 0; i < Plan.Length; i++) track.sections[i] = (float)Math.Round(Plan[i] / beat) * beat;
            track.sections[7] = Math.Max(track.sections[7], track.sections[6] + 4 * beat);
            int n = S(track.sections[7] + 1.5f);

            var drums = new float[n];
            var bass = new float[n];
            var mel = new Bus(n);
            var fx = new float[n];
            float step = beat / 4f;
            int totalSteps = (int)(track.sections[7] / step);

            // melody motif (the artist's hook): 16 steps of scale degrees (-1 = rest)
            var mr = new Random(p.melodySeed);
            var motif = new int[16];
            float density = p.drums == 2 || p.drums == 1 ? 0.35f : p.drums == 4 ? 0.3f : 0.45f;
            int deg = mr.Next(5);
            for (int i = 0; i < 16; i++)
            {
                motif[i] = mr.NextDouble() < density || i == 0 ? deg : -1;
                if (motif[i] >= 0) deg = Math.Max(0, Math.Min(9, deg + mr.Next(-2, 3)));
            }

            int Scale(int degree)
            {
                int o = degree / p.scale.Length, d = degree % p.scale.Length;
                if (d < 0) { d += p.scale.Length; o--; }
                return p.scale[d] + 12 * o;
            }

            float lastBass = -1;
            for (int s = 0; s < totalSteps; s++)
            {
                float t = s * step;
                var part = track.PartAt(t + 0.001f);
                if (part == ShowPart.Outro && t > track.sections[6] + 2 * beat) break;
                int st = s % 16, bar = s / 16;
                if (p.swing > 0 && st % 2 == 1) t += step * p.swing;
                var chordDeg = p.progression[bar % p.progression.Length][0];
                int rootMidi = p.root + Scale(chordDeg);
                bool full = part == ShowPart.Verse || part == ShowPart.Drop || part == ShowPart.Chorus || part == ShowPart.BigMoment;
                bool big = part == ShowPart.Drop || part == ShowPart.BigMoment;
                float partBeat = (t - track.Start(part)) / beat;
                float buildLen = (track.Start(ShowPart.Drop) - track.Start(ShowPart.BuildUp)) / beat;

                // ---- drums
                bool kick = false, snare = false, clap = false; float hat = 0; bool openHat = false;
                switch (p.drums)
                {
                    case 0: kick = In(st, 0, 6, 8, 14) || (big && st == 11); snare = In(st, 4, 12); hat = 1f; openHat = big && st == 14; break;
                    case 1: kick = In(st, 0, 7, 10); clap = st == 8; hat = st % 2 == 0 ? 1f : 0.6f; if (bar % 2 == 1 && st >= 12) hat = 1f; break;
                    case 2: kick = In(st, 0, 10) || (big && st == 3); snare = st == 8; hat = In(st, 0, 3, 6, 8, 11, 14) ? 1f : 0; snare |= st == 15 && bar % 2 == 1; break;
                    case 3: kick = In(st, 0, 8, 10); clap = In(st, 4, 12); hat = st % 2 == 0 ? 0.8f : 0; openHat = st == 14; break;
                    case 4: kick = In(st, 0, 7, 10); snare = st == 8; hat = st % 2 == 0 ? 0.6f : 0.2f; break;
                    case 5: kick = In(st, 0, 3, 9, 13); snare = In(st, 8, 15); hat = r.NextDouble() < 0.55 ? 0.8f : 0; break;
                    case 6: kick = In(st, 0, 7, 10); snare = In(st, 4, 12); hat = st % 2 == 0 ? 0.8f : 0.25f; break;
                    default: kick = In(st, 0, 9); snare = In(st, 4, 12); hat = st % 2 == 0 ? 0.6f : 0; break;
                }
                if (part == ShowPart.Intro) { kick = false; snare = false; clap = false; hat *= 0.5f; }
                if (part == ShowPart.BuildUp)
                {
                    // snare roll that speeds up + quarter kicks fading out
                    float u = partBeat / Math.Max(1f, buildLen);
                    int every = u < 0.5f ? 4 : u < 0.8f ? 2 : 1;
                    snare = s % every == 0; clap = false;
                    kick = st % 4 == 0 && u < 0.6f;
                    hat = 0.4f;
                    if (u > 0.85f && st % 2 == 1) Snare(drums, t + step * 0.5f, 0.35f + 0.4f * u, r, false); // 32nds
                    if (snare) { Snare(drums, t, 0.25f + 0.55f * u, r, false); snare = false; }
                }
                if (part == ShowPart.Outro) { kick = st == 0 && partBeat < 1; snare = false; hat = 0; clap = false; }

                if (kick) Kick(drums, t, big ? 1f : 0.85f, track.kicks);
                if (snare) Snare(drums, t, big ? 0.9f : 0.75f, r, false);
                if (clap) Snare(drums, t, 0.8f, r, true);
                if (hat > 0) Hat(drums, t, hat * (big ? 0.9f : 0.7f), openHat, r);
                if (p.drums == 1 && full && bar % 2 == 1 && st >= 12) Hat(drums, t + step * 0.5f, 0.6f, false, r); // trap roll
                if (p.glitch && big && r.NextDouble() < 0.06) for (int st2 = 1; st2 < 4; st2++) Hat(drums, t + step * st2 / 4f, 0.8f, false, r);
                if (p.crackle && r.NextDouble() < 0.5) fx[Math.Min(n - 1, S(t + (float)r.NextDouble() * step))] += (float)(r.NextDouble() * 0.4 - 0.2);

                // ---- bass / 808 on the kicks
                bool is808 = p.drums <= 2 || p.drums == 5;
                if ((kick || (part == ShowPart.Outro && st == 0 && partBeat < 1)) && part != ShowPart.Intro)
                {
                    int note = rootMidi + (st == 10 && is808 ? 7 : 0) - (is808 ? 12 : 0);
                    float len = is808 ? step * (st == 0 ? 6 : 3) : step * 2.5f;
                    float from = p.glide808 && lastBass > 0 && Math.Abs(lastBass - note) > 0 && r.NextDouble() < 0.4 ? lastBass : -1;
                    Bass(bass, t, len, note, from, p.bassLevel * (big ? 1f : 0.85f), p.drive, is808);
                    lastBass = note;
                }

                // ---- chords (pad / e-piano) once per bar, stabs for r&b / boom-bap
                int third = Scale(chordDeg + 2) - Scale(chordDeg), fifth = Scale(chordDeg + 4) - Scale(chordDeg), seventh = Scale(chordDeg + 6) - Scale(chordDeg);
                int[] chord = p.pad == Wave.EPiano ? new[] { 0, third, fifth, seventh } : new[] { 0, third, fifth };
                if (st == 0 && part != ShowPart.BuildUp)
                {
                    float level = p.padLevel * (part == ShowPart.Intro || part == ShowPart.Outro ? 1.1f : full ? 0.7f : 0.9f);
                    float len = part == ShowPart.Outro ? beat * 6 : beat * 4;
                    if (p.pad != Wave.EPiano || part == ShowPart.Intro || part == ShowPart.Outro)
                        foreach (var c in chord) Note(mel, t, len, rootMidi + 24 + c, p.pad, level * 0.22f, p.brightness * (part == ShowPart.Intro ? 0.5f : 1f), 0.25f, 0.6f, 0.6f, false);
                }
                if (p.pad == Wave.EPiano && full && In(st, 0, 6, 10))
                    foreach (var c in chord) Note(mel, t, step * 3, rootMidi + 24 + c, Wave.EPiano, p.padLevel * 0.2f, p.brightness, 0.005f, 0.3f, 0.5f, true);
                if (part == ShowPart.BuildUp && st % 4 == 0)
                    foreach (var c in chord) Note(mel, t, beat, rootMidi + 24 + c, p.pad, p.padLevel * 0.15f * (0.5f + partBeat / Math.Max(1, buildLen)), p.brightness * (0.3f + 0.7f * partBeat / Math.Max(1, buildLen)), 0.05f, 0.2f, 0.4f, false);

                // ---- lead hook
                int m = motif[st];
                if (m >= 0 && (part == ShowPart.Verse || full))
                {
                    float level = p.leadLevel * (part == ShowPart.Verse ? 0.55f : 1f) * 0.32f;
                    int midi = p.root + 36 + Scale(m);
                    bool pluck = p.lead == Wave.Bell || p.lead == Wave.EPiano || p.lead == Wave.Triangle;
                    Note(mel, t, step * 1.6f, midi, p.lead, level, p.brightness, 0.004f, 0.18f, 0.5f, pluck);
                    if (part == ShowPart.BigMoment || (part == ShowPart.Chorus && p.lead == Wave.SuperSaw))
                        Note(mel, t, step * 1.6f, midi + 12, p.lead, level * 0.5f, p.brightness, 0.004f, 0.18f, 0.6f, pluck);
                }
            }

            // ---- transitions: riser, impacts, crashes
            float b0 = track.Start(ShowPart.BuildUp), d0 = track.Start(ShowPart.Drop);
            Riser(fx, b0, d0 - b0, 0.55f, r);
            Impact(fx, d0, 0.9f, r);
            Crash(drums, d0, 1f, r);
            Crash(drums, track.Start(ShowPart.BigMoment), 0.9f, r);
            Impact(fx, track.Start(ShowPart.BigMoment), 0.6f, r);
            Crash(drums, track.Start(ShowPart.Outro), 0.8f, r);
            Riser(fx, track.Start(ShowPart.Intro) + 0.2f, track.Start(ShowPart.Verse) - 0.2f, 0.25f, r);

            // ---- mix: sidechain the melodic bus to the kick, reverb, glue, fade out
            var mix = new float[n];
            var wet = new float[n];
            Reverb(mel.send, wet, 0.35f + 0.5f * p.reverb);
            var duck = new float[n];
            for (int i = 0; i < n; i++) duck[i] = 1f;
            foreach (var k in track.kicks)
            {
                int s0 = S(k), len = S(0.22f);
                for (int i = 0; i < len && s0 + i < n; i++) duck[s0 + i] = Math.Min(duck[s0 + i], 0.45f + 0.55f * i / len);
            }
            float fadeStart = track.sections[6] + 2 * beat, fadeEnd = track.sections[7] + 1.2f;
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float x = drums[i] * 0.8f + bass[i] * 0.75f * duck[i] + (mel.dry[i] + wet[i]) * 0.9f * duck[i] + fx[i] * 0.6f;
                x = (float)Math.Tanh(x * 1.2f);
                if (t > fadeStart) x *= Math.Max(0f, 1f - (t - fadeStart) / (fadeEnd - fadeStart));
                if (t < 0.4f) x *= t / 0.4f;
                mix[i] = x;
                peak = Math.Max(peak, Math.Abs(x));
            }
            float g = 0.9f / peak;
            for (int i = 0; i < n; i++) mix[i] *= g;
            track.music = mix;
            track.crowd = RenderCrowd(track, n, crowdSize, r);
            return track;
        }

        /// <summary>Crowd bed: murmur → noise swells on the drop and big moment, claps on the beat in the chorus, a final cheer.</summary>
        static float[] RenderCrowd(ConcertTrack track, int n, float size, Random r)
        {
            var c = new float[n];
            float lp = 0, lp2 = 0, beat = track.BeatSeconds;
            float cut = 0.06f + 0.1f * size;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                var part = track.PartAt(t);
                float level;
                switch (part)
                {
                    case ShowPart.Intro: level = 0.35f; break;
                    case ShowPart.Verse: level = 0.25f; break;
                    case ShowPart.BuildUp: level = 0.3f + 0.4f * (t - track.Start(ShowPart.BuildUp)) / Math.Max(0.1f, track.Start(ShowPart.Drop) - track.Start(ShowPart.BuildUp)); break;
                    case ShowPart.Drop: level = 0.95f * (float)Math.Exp(-(t - track.Start(ShowPart.Drop)) * 0.8f) + 0.45f; break;
                    case ShowPart.Chorus: level = 0.5f; break;
                    case ShowPart.BigMoment: level = 0.9f; break;
                    default: level = 1f * (float)Math.Exp(-(t - track.Start(ShowPart.Outro)) * 0.35f); break;
                }
                float nz = (float)(r.NextDouble() * 2 - 1);
                lp += (nz - lp) * cut;
                lp2 += (lp - lp2) * 0.3f;
                // "woo" formant wobble
                float wob = 1f + 0.25f * (float)Math.Sin(t * 7.3f + Math.Sin(t * 2.1f) * 3);
                c[i] = lp2 * level * wob * (0.6f + 0.6f * size);
            }
            // claps on the beat in the chorus / big moment (many hands, slightly out of sync)
            for (float t = track.Start(ShowPart.Chorus); t < track.Start(ShowPart.Outro); t += beat)
                for (int k = 0; k < 6; k++)
                {
                    int s0 = S(t + (float)r.NextDouble() * 0.03f);
                    for (int i = 0; i < S(0.05f) && s0 + i < n; i++) c[s0 + i] += (float)(r.NextDouble() * 2 - 1) * 0.08f * (float)Math.Exp(-i / (float)Rate * 60f);
                }
            float peak = 0.0001f;
            foreach (var v in c) peak = Math.Max(peak, Math.Abs(v));
            for (int i = 0; i < n; i++) c[i] *= 0.8f / peak;
            return c;
        }

        /// <summary>Pyro whoosh (flame cannons).</summary>
        public static float[] Whoosh(int seed)
        {
            var r = new Random(seed);
            int n = S(0.9f);
            var b = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float nz = (float)(r.NextDouble() * 2 - 1);
                lp += (nz - lp) * (0.05f + 0.25f * (float)Math.Exp(-t * 4));
                b[i] = lp * Math.Min(1f, t / 0.03f) * (float)Math.Exp(-t * 3.5f) * 1.6f;
            }
            return b;
        }

        /// <summary>Spark crackle.</summary>
        public static float[] Crackle(int seed)
        {
            var r = new Random(seed);
            int n = S(0.8f);
            var b = new float[n];
            for (int k = 0; k < 90; k++)
            {
                int s0 = r.Next(n - 200);
                float a = (float)r.NextDouble() * (1f - s0 / (float)n);
                for (int i = 0; i < 40; i++) b[s0 + i] += (float)(r.NextDouble() * 2 - 1) * a * (1f - i / 40f);
            }
            return b;
        }
    }
}
