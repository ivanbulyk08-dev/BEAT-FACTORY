using System;
using UnityEngine;

namespace BeatFactory.EditorTools.AudioGen
{
    /// <summary>
    /// Small offline synthesizer used to create ORIGINAL production-style sounds (kick, 808, snare, clap, hats,
    /// plucks, keys, stabs, bells, risers, impacts, reverb). Everything is generated from math — no samples —
    /// so all output can be freely distributed with the game. 44.1 kHz mono float buffers.
    /// </summary>
    public static class Dsp
    {
        public const int Rate = 44100;

        public static float[] Buf(float seconds) => new float[Mathf.Max(1, Mathf.CeilToInt(seconds * Rate))];

        public static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>Mixes src into dst at a time offset (seconds) with gain.</summary>
        public static void Add(float[] dst, float[] src, float at, float gain)
        {
            int o = (int)(at * Rate);
            for (int i = 0; i < src.Length && o + i < dst.Length; i++)
                if (o + i >= 0) dst[o + i] += src[i] * gain;
        }

        static float Coef(float hz) => 1f - Mathf.Exp(-2f * Mathf.PI * hz / Rate);

        // ------------------------------------------------------------------ drums

        /// <summary>Punchy kick: fast pitch drop, short click, gentle saturation.</summary>
        public static float[] Kick(float dur = 0.35f, float f0 = 150f, float f1 = 46f, float clickAmount = 0.35f, int seed = 1)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            double ph = 0;
            float decay = 1f / (dur * 0.32f);
            float lp = 0f, cl = Coef(4000);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float f = f1 + (f0 - f1) * Mathf.Exp(-t * 32f);
                ph += 2 * Math.PI * f / Rate;
                float body = (float)Math.Sin(ph) * Mathf.Exp(-t * decay);
                lp += cl * ((float)(rng.NextDouble() * 2 - 1) - lp);
                float click = lp * Mathf.Exp(-t * 420f) * clickAmount;
                b[i] = (float)Math.Tanh((body + click) * 1.4f) * 0.9f;
            }
            return b;
        }

        /// <summary>808 bass: sine with a short pitch glide into the note, saturated so phone speakers can hear it.</summary>
        public static float[] Bass808(int note, float dur = 0.45f, float glideSemis = 3f, float drive = 2.2f)
        {
            var b = Buf(dur);
            float f = Midi(note);
            double ph = 0;
            float decay = 3.2f / dur;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float fr = f * Mathf.Pow(2f, glideSemis * Mathf.Exp(-t * 28f) / 12f);
                ph += 2 * Math.PI * fr / Rate;
                float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * decay);
                b[i] = (float)Math.Tanh(Math.Sin(ph) * drive) / (float)Math.Tanh(drive) * env * 0.8f;
            }
            return b;
        }

        public static float[] Snare(float dur = 0.2f, int seed = 2)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            float lp = 0, hp = 0, cl = Coef(6500), ch = Coef(1200);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                ph += 2 * Math.PI * (200f - 30f * Mathf.Min(1f, t * 30f)) / Rate;
                float tone = (float)Math.Sin(ph) * Mathf.Exp(-t * 32f) * 0.5f;
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += cl * (n - lp);
                hp += ch * (lp - hp);
                float noise = (lp - hp) * Mathf.Exp(-t * 20f) * 0.9f;
                b[i] = tone + noise;
            }
            return b;
        }

        /// <summary>Clap: three quick noise bursts + short tail, band-passed (no harsh highs).</summary>
        public static float[] Clap(float dur = 0.25f, int seed = 3)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            float lp = 0, hp = 0, cl = Coef(3200), ch = Coef(800);
            float[] starts = { 0f, 0.011f, 0.022f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float env = 0f;
                foreach (var s in starts) if (t >= s) env = Mathf.Max(env, Mathf.Exp(-(t - s) * 170f));
                if (t >= 0.022f) env = Mathf.Max(env, 0.45f * Mathf.Exp(-(t - 0.022f) * 18f));
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += cl * (n - lp);
                hp += ch * (lp - hp);
                b[i] = (lp - hp) * env * 1.6f;
            }
            return b;
        }

        /// <summary>Hi-hat: high noise, short (closed) or longer (open). Kept soft.</summary>
        public static float[] Hat(float dur = 0.06f, bool open = false, int seed = 4)
        {
            var b = Buf(open ? Mathf.Max(dur, 0.25f) : dur);
            var rng = new System.Random(seed);
            float lp1 = 0, lp2 = 0, c1 = Coef(7000), c2 = Coef(11000);
            float decay = open ? 14f : 75f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp1 += c1 * (n - lp1);
                lp2 += c2 * (n - lp2);
                float hp = lp2 - lp1; // band 7–11 kHz: airy but not harsh
                b[i] = hp * Mathf.Exp(-t * decay) * 1.4f;
            }
            return b;
        }

        /// <summary>Rimshot / wooden perc: two short tones + a tiny click.</summary>
        public static float[] Rim(float dur = 0.08f, float pitch = 1f, int seed = 5)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float a = Mathf.Sin(2 * Mathf.PI * 1650f * pitch * t) * Mathf.Exp(-t * 90f) * 0.35f;
                float w = Mathf.Sin(2 * Mathf.PI * 520f * pitch * t) * Mathf.Exp(-t * 55f) * 0.6f;
                float c = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 900f) * 0.25f;
                b[i] = a + w + c;
            }
            return b;
        }

        // ------------------------------------------------------------------ melodic

        /// <summary>Karplus-Strong pluck — natural, soft "plucked synth/guitar" tone.</summary>
        public static float[] Pluck(int note, float dur = 0.4f, float brightness = 0.5f, int seed = 6)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed + note);
            float f = Midi(note);
            int n = Mathf.Max(2, Mathf.RoundToInt(Rate / f));
            var line = new float[n];
            float lp = 0f, c = Mathf.Lerp(0.15f, 0.9f, brightness);
            for (int i = 0; i < n; i++)
            {
                lp += c * ((float)(rng.NextDouble() * 2 - 1) - lp);
                line[i] = lp;
            }
            float damping = Mathf.Lerp(0.990f, 0.997f, Mathf.Clamp01(dur));
            int idx = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                int nxt = (idx + 1) % n;
                float v = (line[idx] + line[nxt]) * 0.5f * damping;
                float outv = line[idx];
                line[idx] = v;
                idx = nxt;
                // add a little sine body so it reads as a note on small speakers
                float body = Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 6f) * 0.25f;
                b[i] = (outv + body) * Mathf.Min(1f, t / 0.002f);
            }
            return b;
        }

        /// <summary>Electric-piano-like keys (Rhodes flavour).</summary>
        public static float[] Keys(int[] notes, float dur = 0.6f)
        {
            var b = Buf(dur);
            foreach (var note in notes)
            {
                float f = Midi(note);
                for (int i = 0; i < b.Length; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 2.6f);
                    float trem = 1f + 0.06f * Mathf.Sin(2 * Mathf.PI * 5f * t);
                    float s = Mathf.Sin(2 * Mathf.PI * f * t) * 0.7f
                              + Mathf.Sin(2 * Mathf.PI * f * 2 * t) * 0.22f * Mathf.Exp(-t * 4f)
                              + Mathf.Sin(2 * Mathf.PI * f * 3 * t) * 0.06f * Mathf.Exp(-t * 7f)
                              + Mathf.Sin(2 * Mathf.PI * f * 14 * t) * 0.04f * Mathf.Exp(-t * 35f); // tine
                    b[i] += s * env * trem / notes.Length;
                }
            }
            return b;
        }

        /// <summary>Synth stab/chord: detuned saws through a closing low-pass filter.</summary>
        public static float[] Stab(int[] notes, float dur = 0.35f, float cutoffStart = 3500f, float cutoffEnd = 500f)
        {
            var b = Buf(dur);
            var phases = new double[notes.Length * 2];
            float lp1 = 0, lp2 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                double s = 0;
                for (int k = 0; k < notes.Length; k++)
                    for (int v = 0; v < 2; v++)
                    {
                        float f = Midi(notes[k]) * (v == 0 ? 0.996f : 1.004f);
                        int pi = k * 2 + v;
                        phases[pi] += f / Rate;
                        double x = phases[pi] - Math.Floor(phases[pi]);
                        s += (2 * x - 1); // saw
                    }
                s /= notes.Length * 2;
                float cutoff = cutoffEnd + (cutoffStart - cutoffEnd) * Mathf.Exp(-t * 14f);
                float c = Coef(cutoff);
                lp1 += c * ((float)s - lp1);
                lp2 += c * (lp1 - lp2); // 2-pole: smoother, no fizz
                float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * (3.5f / dur));
                b[i] = lp2 * env * 1.4f;
            }
            return b;
        }

        /// <summary>Soft FM bell / digital ding (for money & sparkles — not a cartoon coin).</summary>
        public static float[] Bell(int note, float dur = 0.5f, float index = 2.5f)
        {
            var b = Buf(dur);
            float f = Midi(note);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float idx = index * Mathf.Exp(-t * 10f);
                float mod = Mathf.Sin(2 * Mathf.PI * f * 2.0f * t) * idx;
                float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * (4.5f / dur));
                b[i] = Mathf.Sin(2 * Mathf.PI * f * t + mod) * env * 0.6f;
            }
            return b;
        }

        /// <summary>Simple tone sweep (rising "level up" tone).</summary>
        public static float[] Sweep(float f0, float f1, float dur, float harmonics = 0.3f)
        {
            var b = Buf(dur);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float k = t / dur;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                ph += 2 * Math.PI * f / Rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k)) ;
                b[i] = ((float)Math.Sin(ph) + harmonics * (float)Math.Sin(ph * 2)) * env * 0.5f;
            }
            return b;
        }

        // ------------------------------------------------------------------ FX

        /// <summary>Noise riser with an opening filter + rising tone. Ends at full level.</summary>
        public static float[] Riser(float dur = 0.6f, float f0 = 250f, float f1 = 2400f, int seed = 7)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            float lp = 0, hp = 0, ch = Coef(300);
            double ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float k = t / dur;
                float cutoff = Mathf.Lerp(600f, 7000f, k * k);
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += Coef(cutoff) * (n - lp);
                hp += ch * (lp - hp);
                float f = f0 * Mathf.Pow(f1 / f0, k);
                ph += 2 * Math.PI * f / Rate;
                float amp = k * k;
                b[i] = ((lp - hp) * 0.8f + (float)Math.Sin(ph) * 0.25f) * amp;
            }
            return b;
        }

        /// <summary>Reverse-cymbal style swell (soft air rising into the hit).</summary>
        public static float[] Swell(float dur = 0.4f, int seed = 8)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            float lp1 = 0, lp2 = 0, c1 = Coef(3000), c2 = Coef(9000);
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp1 += c1 * (n - lp1);
                lp2 += c2 * (n - lp2);
                b[i] = (lp2 - lp1) * k * k * k * 1.2f;
            }
            return b;
        }

        /// <summary>Cinematic low impact (boom + filtered noise).</summary>
        public static float[] Impact(float dur = 1.0f, int seed = 9)
        {
            var b = Buf(dur);
            var rng = new System.Random(seed);
            double ph = 0;
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 38f + 50f * Mathf.Exp(-t * 9f);
                ph += 2 * Math.PI * f / Rate;
                float boom = (float)Math.Sin(ph) * Mathf.Exp(-t * 3.8f);
                lp += Coef(1800f * Mathf.Exp(-t * 3f) + 200f) * ((float)(rng.NextDouble() * 2 - 1) - lp);
                float noise = lp * Mathf.Exp(-t * 6f) * 0.7f;
                b[i] = (float)Math.Tanh((boom + noise) * 1.5f);
            }
            return b;
        }

        // ------------------------------------------------------------------ processing

        /// <summary>Small Schroeder reverb (4 combs + 2 all-pass), adds a sense of space. wet 0..1.</summary>
        public static void Reverb(float[] buf, float wet, float size = 1f)
        {
            if (wet <= 0f) return;
            float[] combMs = { 29.7f, 37.1f, 41.1f, 43.7f };
            var outBuf = new float[buf.Length];
            foreach (var ms in combMs)
            {
                int d = Mathf.Max(1, (int)(ms * size * Rate / 1000f));
                var line = new float[d];
                int p = 0;
                float damp = 0f;
                for (int i = 0; i < buf.Length; i++)
                {
                    float y = line[p];
                    damp += 0.35f * (y - damp);
                    line[p] = buf[i] + damp * 0.78f;
                    p = (p + 1) % d;
                    outBuf[i] += y * 0.25f;
                }
            }
            foreach (var ms in new[] { 5.0f, 1.7f })
            {
                int d = Mathf.Max(1, (int)(ms * Rate / 1000f));
                var line = new float[d];
                int p = 0;
                for (int i = 0; i < outBuf.Length; i++)
                {
                    float x = outBuf[i];
                    float y = line[p];
                    float v = x + y * 0.7f;
                    line[p] = v;
                    p = (p + 1) % d;
                    outBuf[i] = y - v * 0.7f;
                }
            }
            for (int i = 0; i < buf.Length; i++) buf[i] += outBuf[i] * wet;
        }

        public static void Lowpass(float[] buf, float hz)
        {
            float c = Coef(hz), y = 0;
            for (int i = 0; i < buf.Length; i++) { y += c * (buf[i] - y); buf[i] = y; }
        }

        /// <summary>
        /// Final mastering: remove DC, gentle soft-clip, normalize to the target peak, tiny fades (no clicks).
        /// Peaks stay well below 0 dBFS so nothing ever clips.
        /// </summary>
        public static float[] Master(float[] buf, float peak = 0.7f, float fadeOutSeconds = 0.02f, bool fadeIn = true)
        {
            // DC blocker (~20 Hz high-pass)
            float prevX = 0, prevY = 0, r = 0.997f;
            for (int i = 0; i < buf.Length; i++)
            {
                float y = buf[i] - prevX + r * prevY;
                prevX = buf[i];
                prevY = y;
                buf[i] = y;
            }
            float max = 0f;
            foreach (var v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            if (max > 1e-6f)
            {
                // soft-clip relative to the peak, then normalize
                for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] / max * 1.3f);
                max = 0f;
                foreach (var v in buf) max = Mathf.Max(max, Mathf.Abs(v));
                for (int i = 0; i < buf.Length; i++) buf[i] *= peak / max;
            }
            if (fadeIn)
            {
                int fi = Mathf.Min(buf.Length, 44); // 1 ms
                for (int i = 0; i < fi; i++) buf[i] *= (float)i / fi;
            }
            int fo = Mathf.Min(buf.Length, (int)(fadeOutSeconds * Rate));
            for (int i = 0; i < fo; i++) buf[buf.Length - 1 - i] *= (float)i / fo;
            return buf;
        }

        /// <summary>Removes trailing silence below the threshold (keeps sounds short).</summary>
        public static float[] Trim(float[] buf, float threshold = 0.0015f)
        {
            int end = buf.Length - 1;
            while (end > 0 && Mathf.Abs(buf[end]) < threshold) end--;
            int len = Mathf.Min(buf.Length, end + (int)(0.02f * Rate));
            var o = new float[len];
            Array.Copy(buf, o, len);
            return o;
        }
    }
}
