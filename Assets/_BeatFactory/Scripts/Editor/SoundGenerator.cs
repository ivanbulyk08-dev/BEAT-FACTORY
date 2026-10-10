using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Original lo-fi music loop synthesizer + WAV writing/import helpers.
    /// Sound effects are designed in AudioGen.SfxLibrary (Tools > Beat Factory > Audio > Regenerate Sounds).
    /// </summary>
    public static class SoundGenerator
    {
        const int Rate = 44100;

        enum Wave { Sine, Soft, Triangle, Saw }

        /// <summary>Creates the music loop (only if missing unless overwrite).</summary>
        public static void GenerateMusic(string path, bool overwrite)
        {
            if (!overwrite && File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            WriteWav(path, Music());
            ImportAudio(path, music: true);
        }

        public static void WriteWavFile(string path, float[] samples) => WriteWav(path, samples);

        /// <summary>Mobile-friendly import settings: SFX decompressed (instant), music streamed (low memory).</summary>
        public static void ImportAudio(string path, bool music)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var s = importer.defaultSampleSettings;
            if (music)
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.5f;
            }
            else
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.ADPCM;
            }
            importer.defaultSampleSettings = s;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ legacy sound designs (kept for reference, unused)
        // ------------------------------------------------------------------ sound designs

        static float[] Click()
        {
            var b = Buf(0.06f);
            Tone(b, 0, 0.05f, 1900, 1300, 0.6f, 0.001f, 70, Wave.Sine);
            Noise(b, 0, 0.012f, 0.15f, 0.0005f, 300, 0.9f, highpass: true);
            return Normalize(b, 0.6f);
        }

        static void Kick(float[] b, float t, float vol, bool wrap = false)
        {
            Tone(b, t, 0.38f, 150, 42, vol, 0.002f, 8f, Wave.Sine, wrap);
            Noise(b, t, 0.012f, vol * 0.25f, 0.0005f, 250, 0.5f, false, wrap);
        }

        static float[] Beat()
        {
            var b = Buf(0.4f);
            Kick(b, 0, 1f);
            Noise(b, 0.0f, 0.05f, 0.1f, 0.001f, 70, 0.95f, highpass: true); // hat tick on top
            return Normalize(b, 0.85f);
        }

        static float[] BeatGood()
        {
            var b = Buf(0.6f);
            Kick(b, 0, 0.8f);
            Tone(b, 0.01f, 0.55f, Note(84), Note(84), 0.35f, 0.003f, 6, Wave.Sine);
            Tone(b, 0.01f, 0.55f, Note(91), Note(91), 0.2f, 0.003f, 7, Wave.Sine);
            return Normalize(b, 0.85f);
        }

        static float[] Upgrade()
        {
            var b = Buf(0.3f);
            Tone(b, 0, 0.1f, Note(79), Note(79), 0.4f, 0.002f, 18, Wave.Soft);
            Tone(b, 0.08f, 0.2f, Note(86), Note(86), 0.45f, 0.002f, 14, Wave.Soft);
            return Normalize(b, 0.7f);
        }

        static float[] Hire()
        {
            var b = Buf(0.6f);
            int[] notes = { 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++)
                Tone(b, i * 0.07f, 0.3f, Note(notes[i]), Note(notes[i]), 0.4f, 0.003f, 10, Wave.Triangle);
            Echo(b, 0.11f, 0.25f);
            return Normalize(b, 0.75f);
        }

        static float[] Unlock()
        {
            var b = Buf(1.1f);
            int[] notes = { 72, 76, 79, 84, 88, 91 };
            for (int i = 0; i < notes.Length; i++)
            {
                Tone(b, i * 0.06f, 0.45f, Note(notes[i]), Note(notes[i]), 0.35f, 0.003f, 7, Wave.Triangle);
                Tone(b, i * 0.06f, 0.45f, Note(notes[i] + 12), Note(notes[i] + 12), 0.1f, 0.003f, 9, Wave.Sine);
            }
            Echo(b, 0.13f, 0.3f);
            return Normalize(b, 0.8f);
        }

        static void Chord(float[] b, float t, float dur, int[] notes, float vol, float decay, Wave wave)
        {
            foreach (var n in notes)
            {
                Tone(b, t, dur, Note(n), Note(n), vol, 0.005f, decay, wave);
                Tone(b, t, dur, Note(n) * 1.004f, Note(n) * 1.004f, vol * 0.5f, 0.005f, decay, wave); // detune = width
            }
        }

        static float[] Hit()
        {
            var b = Buf(1.2f);
            Kick(b, 0, 1f);
            Chord(b, 0, 1.0f, new[] { 60, 64, 67, 72 }, 0.12f, 3.5f, Wave.Saw);
            Noise(b, 0, 0.6f, 0.15f, 0.002f, 6, 0.3f);
            Echo(b, 0.15f, 0.3f);
            return Normalize(b, 0.9f);
        }

        static float[] Viral()
        {
            var b = Buf(2.2f);
            Tone(b, 0, 0.55f, 200, 1600, 0.25f, 0.4f, 0, Wave.Soft);         // riser
            Noise(b, 0, 0.55f, 0.25f, 0.5f, 0, 0.8f, highpass: true);        // whoosh
            Kick(b, 0.55f, 1f);
            Chord(b, 0.55f, 1.5f, new[] { 48, 60, 64, 67, 72, 76 }, 0.1f, 2.2f, Wave.Saw);
            int[] sparkle = { 84, 88, 91, 96, 100 };
            for (int i = 0; i < sparkle.Length; i++)
                Tone(b, 0.6f + i * 0.06f, 0.5f, Note(sparkle[i]), Note(sparkle[i]), 0.12f, 0.002f, 6, Wave.Sine);
            Echo(b, 0.17f, 0.35f);
            return Normalize(b, 0.95f);
        }

        static float[] Prestige()
        {
            var b = Buf(3.5f);
            Noise(b, 0, 1.2f, 0.35f, 1.15f, 0, 0.6f, highpass: true);
            Tone(b, 0, 1.2f, 100, 900, 0.2f, 1.0f, 0, Wave.Soft);
            Tone(b, 1.2f, 2.2f, Note(36), Note(36), 0.8f, 0.005f, 1.6f, Wave.Sine);
            Chord(b, 1.2f, 2.2f, new[] { 48, 55, 62, 64, 67, 71 }, 0.08f, 1.4f, Wave.Triangle);
            Echo(b, 0.2f, 0.4f);
            return Normalize(b, 0.95f);
        }

        static float[] CashOut()
        {
            var b = Buf(0.5f);
            Tone(b, 0, 0.08f, Note(83), Note(83), 0.45f, 0.001f, 10, Wave.Soft);
            Tone(b, 0.07f, 0.4f, Note(88), Note(88), 0.5f, 0.001f, 7, Wave.Soft);
            return Normalize(b, 0.7f);
        }

        static float[] Boost()
        {
            var b = Buf(0.6f);
            Tone(b, 0, 0.4f, 300, 1500, 0.35f, 0.01f, 3, Wave.Soft);
            Noise(b, 0, 0.4f, 0.2f, 0.25f, 6, 0.85f, highpass: true);
            Tone(b, 0.38f, 0.2f, Note(91), Note(91), 0.2f, 0.002f, 12, Wave.Sine);
            return Normalize(b, 0.75f);
        }

        static float[] Collect()
        {
            var b = Buf(0.9f);
            for (int k = 0; k < 2; k++)
            {
                float t = k * 0.12f;
                Tone(b, t, 0.08f, Note(83 + k * 2), Note(83 + k * 2), 0.4f, 0.001f, 10, Wave.Soft);
                Tone(b, t + 0.07f, 0.35f, Note(88 + k * 2), Note(88 + k * 2), 0.45f, 0.001f, 7, Wave.Soft);
            }
            Echo(b, 0.12f, 0.25f);
            return Normalize(b, 0.75f);
        }

        /// <summary>8-bar lo-fi loop (85 BPM): Fmaj7 - Em7 - Dm7 - Cmaj7, bass, swung drums, vinyl crackle.</summary>
        static float[] Music()
        {
            const float bpm = 85f;
            float beat = 60f / bpm;
            const int bars = 8;
            var b = Buf(bars * 4 * beat);
            var rng = new System.Random(808);

            int[][] chords =
            {
                new[] { 53, 57, 60, 64 }, // Fmaj7
                new[] { 52, 55, 59, 62 }, // Em7
                new[] { 50, 53, 57, 60 }, // Dm7
                new[] { 48, 52, 55, 59 }, // Cmaj7
            };
            int[] bass = { 41, 40, 38, 36 };
            int[] scale = { 69, 72, 74, 76, 79, 81 }; // A minor pentatonic for the melody

            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                var chord = chords[bar % 4];

                // Electric-piano-like chords (soft sine + octave), two hits per bar
                foreach (var hitBeat in new[] { 0f, 2.5f })
                {
                    float dur = hitBeat == 0 ? 2.3f * beat : 1.6f * beat;
                    foreach (var n in chord)
                    {
                        Tone(b, t0 + hitBeat * beat + 0.01f, dur, Note(n), Note(n), 0.09f, 0.02f, 1.1f, Wave.Sine, true);
                        Tone(b, t0 + hitBeat * beat + 0.01f, dur, Note(n + 12) * 1.003f, Note(n + 12) * 1.003f, 0.025f, 0.02f, 1.6f, Wave.Triangle, true);
                    }
                }

                // Bass
                Tone(b, t0, 1.6f * beat, Note(bass[bar % 4]), Note(bass[bar % 4]), 0.32f, 0.01f, 1.2f, Wave.Sine, true);
                Tone(b, t0 + 2.5f * beat, 1.2f * beat, Note(bass[bar % 4]), Note(bass[bar % 4]), 0.26f, 0.01f, 1.5f, Wave.Sine, true);

                // Drums: kick on 1 and the "and" of 3, snare on 2 and 4, swung hats
                Kick(b, t0, 0.6f, true);
                Kick(b, t0 + 2.5f * beat, 0.45f, true);
                foreach (var s in new[] { 1f, 3f })
                {
                    Noise(b, t0 + s * beat, 0.25f, 0.2f, 0.002f, 16, 0.25f, false, true);
                    Tone(b, t0 + s * beat, 0.12f, 190, 160, 0.12f, 0.002f, 25, Wave.Sine, true);
                }
                for (int h = 0; h < 8; h++)
                {
                    float swing = h % 2 == 1 ? 0.08f * beat : 0f;
                    Noise(b, t0 + h * 0.5f * beat + swing, 0.05f, h % 2 == 0 ? 0.05f : 0.035f, 0.001f, 60, 0.95f, true, true);
                }

                // Lazy melody in the second half
                if (bar >= 4)
                {
                    for (int s = 0; s < 8; s++)
                    {
                        if (rng.NextDouble() > 0.38) continue;
                        int n = scale[rng.Next(scale.Length)];
                        Tone(b, t0 + s * 0.5f * beat, 0.9f * beat, Note(n), Note(n), 0.07f, 0.01f, 3f, Wave.Triangle, true);
                    }
                }
            }

            // Vinyl crackle + hiss
            for (int i = 0; i < b.Length; i++)
            {
                b[i] += (float)(rng.NextDouble() * 2 - 1) * 0.004f;
                if (rng.NextDouble() < 0.0004) b[i] += (float)(rng.NextDouble() * 2 - 1) * 0.08f;
            }

            // Warm low-pass (lo-fi). Run twice so the start of the loop matches the end.
            float y = 0f;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < b.Length; i++)
                {
                    y += 0.45f * (b[i] - y);
                    if (pass == 1) b[i] = y;
                }
            return Normalize(b, 0.7f);
        }

        // ------------------------------------------------------------------ synthesis helpers

        static float[] Buf(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];
        static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static float Osc(Wave wave, double phase)
        {
            double x = phase % (2 * Math.PI);
            switch (wave)
            {
                case Wave.Soft: return (float)Math.Tanh(2.5 * Math.Sin(x)) * 0.8f;          // rounded square
                case Wave.Triangle: return (float)(2 / Math.PI * Math.Asin(Math.Sin(x)));
                case Wave.Saw:
                    // A few harmonics = soft saw without harsh aliasing
                    return (float)(Math.Sin(x) + Math.Sin(2 * x) / 2 + Math.Sin(3 * x) / 3 + Math.Sin(4 * x) / 4) * 0.55f;
                default: return (float)Math.Sin(x);
            }
        }

        /// <summary>Adds a tone: frequency glides from f0 to f1 (exponential), envelope = attack then exponential decay.</summary>
        static void Tone(float[] buf, float start, float dur, float f0, float f1, float vol, float attack, float decay, Wave wave, bool wrap = false)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float k = n > 1 ? (float)i / (n - 1) : 0f;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                phase += 2 * Math.PI * f / Rate;
                float env = (attack > 0 ? Mathf.Min(1f, t / attack) : 1f) * Mathf.Exp(-decay * t);
                env *= Mathf.Min(1f, (n - i) / (0.005f * Rate)); // tiny fade-out: no clicks
                int idx = s0 + i;
                if (wrap) idx %= buf.Length;
                else if (idx >= buf.Length) break;
                buf[idx] += Osc(wave, phase) * env * vol;
            }
        }

        static readonly System.Random noiseRng = new System.Random(1234);

        /// <summary>Filtered noise. tone = filter amount (0..1); highpass keeps the bright part.</summary>
        static void Noise(float[] buf, float start, float dur, float vol, float attack, float decay, float tone, bool highpass = false, bool wrap = false)
        {
            int s0 = (int)(start * Rate), n = (int)(dur * Rate);
            float lp = 0f;
            float a = highpass ? 0.5f : Mathf.Clamp(1f - tone, 0.02f, 1f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float x = (float)(noiseRng.NextDouble() * 2 - 1);
                lp += a * (x - lp);
                float v = highpass ? x - lp : lp;
                float env = (attack > 0 ? Mathf.Min(1f, t / attack) : 1f) * Mathf.Exp(-decay * t);
                env *= Mathf.Min(1f, (n - i) / (0.005f * Rate));
                int idx = s0 + i;
                if (wrap) idx %= buf.Length;
                else if (idx >= buf.Length) break;
                buf[idx] += v * env * vol;
            }
        }

        static void Echo(float[] buf, float delay, float feedback)
        {
            int d = (int)(delay * Rate);
            for (int i = d; i < buf.Length; i++) buf[i] += buf[i - d] * feedback;
        }

        static void FadeOutEnd(float[] buf, float seconds)
        {
            int n = Mathf.Min(buf.Length, (int)(seconds * Rate));
            for (int i = 0; i < n; i++) buf[buf.Length - 1 - i] *= (float)i / n;
        }

        static float[] Normalize(float[] buf, float peak)
        {
            float max = 0f;
            foreach (var v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            if (max < 1e-6f) return buf;
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
            return buf;
        }

        static void WriteWav(string path, float[] samples)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int dataBytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataBytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);      // PCM
                w.Write((short)1);      // mono
                w.Write(Rate);
                w.Write(Rate * 2);      // byte rate
                w.Write((short)2);      // block align
                w.Write((short)16);     // bits
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataBytes);
                foreach (var s in samples)
                    w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
