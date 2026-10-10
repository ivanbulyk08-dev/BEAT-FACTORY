using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BeatFactory.EditorTools.AudioGen.Dsp;

namespace BeatFactory.EditorTools.AudioGen
{
    /// <summary>
    /// Designs every game sound from the Dsp building blocks and writes WAV files into
    /// Assets/_BeatFactory/Audio/SFX/... All sounds are in A minor (A C D E G) so they fit the music and each other.
    /// Menu: Tools > Beat Factory > Audio > Regenerate Sounds.
    /// </summary>
    public static class SfxLibrary
    {
        public const string AudioRoot = BeatFactorySceneBuilder.Root + "/Audio";
        public const string Sfx = AudioRoot + "/SFX";
        public const string MusicFolder = AudioRoot + "/Music";
        public const string MusicPath = MusicFolder + "/MUS_LoFi_Loop_01.wav";

        // A minor pentatonic notes (MIDI)
        const int A1 = 33, C2 = 36, D2 = 38, E2 = 40, A2 = 45, A3 = 57, C4 = 60, D4 = 62, E4 = 64, G4 = 67,
                  A4 = 69, C5 = 72, D5 = 74, E5 = 76, G5 = 79, A5 = 81, C6 = 84, E6 = 88, A6 = 93;

        /// <summary>Every sound: id → list of (file path, generator).</summary>
        public static readonly Dictionary<SoundId, (string path, Func<float[]> make)[]> Library = new Dictionary<SoundId, (string, Func<float[]>)[]>
        {
            [SoundId.Beat] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Beat/SFX_Beat_Kick808_01.wav",   () => Beat808(A1, 1)),
                (Sfx + "/Beat/SFX_Beat_Kick808_02.wav",   () => Beat808(C2, 2)),
                (Sfx + "/Beat/SFX_Beat_KickPluck_01.wav", () => BeatPluck(E4, 3)),
                (Sfx + "/Beat/SFX_Beat_KickPluck_02.wav", () => BeatPluck(A4, 4)),
                (Sfx + "/Beat/SFX_Beat_KickClapStab_01.wav", () => BeatClapStab(5)),
                (Sfx + "/Beat/SFX_Beat_KickKeys_01.wav",  () => BeatKeys(6)),
            },
            [SoundId.BeatGood] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Beat/SFX_Beat_Great_01.wav", () => BeatGreat(new[] { A4, C5, E5 }, 7)),
                (Sfx + "/Beat/SFX_Beat_Great_02.wav", () => BeatGreat(new[] { C5, E5, G5 }, 8)),
            },
            [SoundId.Click] = new (string, Func<float[]>)[]
            {
                (Sfx + "/UI/SFX_UI_Click_01.wav", () => Master(Rim(0.07f, 1.0f, 11), 0.5f, 0.01f)),
                (Sfx + "/UI/SFX_UI_Click_02.wav", () => Master(Rim(0.07f, 1.12f, 12), 0.5f, 0.01f)),
                (Sfx + "/UI/SFX_UI_Click_03.wav", () => Master(Trim(Pluck(G5, 0.14f, 0.25f, 13)), 0.45f, 0.02f)),
            },
            [SoundId.ClickStrong] = new (string, Func<float[]>)[]
            {
                (Sfx + "/UI/SFX_UI_Strong_01.wav", UiStrong),
            },
            [SoundId.Tab] = new (string, Func<float[]>)[]
            {
                (Sfx + "/UI/SFX_UI_Tab_01.wav", () => UiTab(14)),
                (Sfx + "/UI/SFX_UI_Tab_02.wav", () => UiTab(15)),
            },
            [SoundId.ProducerBeginner] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Producers/SFX_Producer_Beginner_01.wav", () => ProducerBeginner(C4, 21)),
                (Sfx + "/Producers/SFX_Producer_Beginner_02.wav", () => ProducerBeginner(D4, 22)),
            },
            [SoundId.ProducerMelody] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Producers/SFX_Producer_Melody_01.wav", () => ProducerMelody(new[] { A3, C4, E4, G4 }, A5)),
                (Sfx + "/Producers/SFX_Producer_Melody_02.wav", () => ProducerMelody(new[] { C4, E4, G4, C5 }, E6)),
            },
            [SoundId.ProducerDrums] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Producers/SFX_Producer_Drums_01.wav", () => ProducerDrums(false)),
                (Sfx + "/Producers/SFX_Producer_Drums_02.wav", () => ProducerDrums(true)),
            },
            [SoundId.Money] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Money/SFX_Money_01.wav", () => Money(E5, A5, A4)),
                (Sfx + "/Money/SFX_Money_02.wav", () => Money(D5, G5, D4 + 12)),
            },
            [SoundId.Upgrade] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Upgrade/SFX_Upgrade_01.wav", () => Upgrade(C4, C5, G5, new[] { C4, E4, G4 }, 31)),
                (Sfx + "/Upgrade/SFX_Upgrade_02.wav", () => Upgrade(D4, D5, A5, new[] { D4, G4, A4 }, 32)),
                (Sfx + "/Upgrade/SFX_Upgrade_03.wav", () => Upgrade(E4, E5, C6 - 12 + 12, new[] { A3, C4, E4 }, 33)),
            },
            [SoundId.Unlock] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Upgrade/SFX_Unlock_01.wav", Unlock),
            },
            [SoundId.StudioTransition] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Transitions/SFX_Transition_Studio_01.wav", StudioTransition),
            },
            [SoundId.Hit] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Impacts/SFX_Hit_01.wav", Hit),
            },
            [SoundId.Viral] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Viral/SFX_Viral_01.wav", () => Viral(A1, new[] { A2, A3, C4, E4, G4 }, 41)),
                (Sfx + "/Viral/SFX_Viral_02.wav", () => Viral(C2, new[] { C4 - 12, C4, E4, G4, A4 }, 42)),
            },
            [SoundId.Prestige] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Transitions/SFX_Prestige_01.wav", Prestige),
            },
            [SoundId.Boost] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Upgrade/SFX_Boost_01.wav", Boost),
            },
            [SoundId.Collect] = new (string, Func<float[]>)[]
            {
                (Sfx + "/Money/SFX_Money_Collect_01.wav", Collect),
            },
        };

        [MenuItem("Tools/Beat Factory/Audio/Regenerate Sounds")]
        static void Menu()
        {
            GenerateAll(overwrite: true);
            Debug.Log("[Beat Factory] Sounds regenerated in " + Sfx);
        }

        public static void GenerateAll(bool overwrite)
        {
            foreach (var pair in Library)
                foreach (var (path, make) in pair.Value)
                    Write(path, overwrite, make, music: false);
            SoundGenerator.GenerateMusic(MusicPath, overwrite);
        }

        static void Write(string path, bool overwrite, Func<float[]> make, bool music)
        {
            if (!overwrite && File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SoundGenerator.WriteWavFile(path, make());
            SoundGenerator.ImportAudio(path, music);
        }

        // ================================================================== designs

        static float[] Mix(float seconds) => Buf(seconds + 0.4f); // room for reverb tails (trimmed later)

        // Trim quiet reverb tails (≈ -44 dB) so sounds stay short, then master with a 60 ms fade.
        static float[] Finish(float[] b, float peak = 0.75f) => Master(Trim(b, 0.006f), peak, 0.06f);

        // --- MAKE BEAT: always a kick + a short musical layer
        static float[] Beat808(int note, int seed)
        {
            var b = Mix(0.5f);
            Add(b, Kick(0.32f, 150f, 48f, 0.35f, seed), 0f, 0.9f);
            Add(b, Bass808(note, 0.42f), 0.005f, 0.55f);
            Add(b, Hat(0.05f, false, seed + 100), 0f, 0.12f);
            return Finish(b, 0.8f);
        }

        static float[] BeatPluck(int note, int seed)
        {
            var b = Mix(0.5f);
            Add(b, Kick(0.3f, 150f, 50f, 0.3f, seed), 0f, 0.9f);
            var p = Pluck(note, 0.42f, 0.45f, seed);
            Reverb(p, 0.18f);
            Add(b, p, 0.004f, 0.42f);
            return Finish(b, 0.78f);
        }

        static float[] BeatClapStab(int seed)
        {
            var b = Mix(0.45f);
            Add(b, Kick(0.3f, 150f, 48f, 0.3f, seed), 0f, 0.85f);
            Add(b, Clap(0.22f, seed), 0.0f, 0.28f);
            Add(b, Stab(new[] { A3, C4, E4 }, 0.28f, 2600f, 500f), 0.002f, 0.32f);
            return Finish(b, 0.78f);
        }

        static float[] BeatKeys(int seed)
        {
            var b = Mix(0.55f);
            Add(b, Kick(0.3f, 145f, 47f, 0.3f, seed), 0f, 0.9f);
            var k = Keys(new[] { C4, G4 }, 0.45f);
            Reverb(k, 0.2f);
            Add(b, k, 0.003f, 0.45f);
            Add(b, Hat(0.2f, true, seed), 0.0f, 0.06f);
            return Finish(b, 0.78f);
        }

        static float[] BeatGreat(int[] arp, int seed)
        {
            var b = Mix(0.7f);
            Add(b, Kick(0.32f, 150f, 48f, 0.3f, seed), 0f, 0.85f);
            for (int i = 0; i < arp.Length; i++) Add(b, Pluck(arp[i], 0.4f, 0.55f, seed + i), i * 0.055f, 0.32f);
            Add(b, Bell(arp[arp.Length - 1] + 12, 0.35f, 1.5f), 0.16f, 0.12f);
            Reverb(b, 0.18f);
            return Finish(b, 0.8f);
        }

        // --- UI
        static float[] UiStrong()
        {
            var b = Mix(0.35f);
            Add(b, Kick(0.18f, 120f, 60f, 0.2f, 51), 0f, 0.45f);
            var p = Pluck(C5, 0.28f, 0.4f, 52);
            Reverb(p, 0.2f);
            Add(b, p, 0f, 0.5f);
            return Finish(b, 0.55f);
        }

        static float[] UiTab(int seed)
        {
            var b = Mix(0.08f);
            Add(b, Hat(0.04f, false, seed), 0f, 0.4f);
            Add(b, Rim(0.05f, 1.25f, seed), 0f, 0.35f);
            return Master(Trim(b), 0.38f, 0.01f);
        }

        // --- producers (hire): each has its own instrument
        static float[] ProducerBeginner(int note, int seed)
        {
            var b = Mix(0.6f);
            Add(b, Kick(0.22f, 120f, 55f, 0.2f, seed), 0f, 0.55f);
            Add(b, Rim(0.07f, 1f, seed), 0.12f, 0.35f);
            var k = Keys(new[] { note, note + 7 }, 0.45f);
            Reverb(k, 0.2f);
            Add(b, k, 0.24f, 0.45f);
            return Finish(b, 0.65f);
        }

        static float[] ProducerMelody(int[] chord, int sparkle)
        {
            var b = Mix(0.9f);
            for (int i = 0; i < chord.Length; i++) Add(b, Pluck(chord[i], 0.5f, 0.5f, 60 + i), i * 0.07f, 0.34f);
            Add(b, Bell(sparkle, 0.4f, 1.8f), chord.Length * 0.07f, 0.16f);
            Reverb(b, 0.3f, 1.1f);
            return Finish(b, 0.65f);
        }

        static float[] ProducerDrums(bool alt)
        {
            var b = Mix(0.65f);
            Add(b, Kick(0.26f, 150f, 48f, 0.3f, 71), 0f, 0.8f);
            Add(b, Hat(0.05f, false, 72), 0.1f, 0.18f);
            if (alt) { Add(b, Hat(0.05f, false, 73), 0.17f, 0.12f); Add(b, Clap(0.22f, 74), 0.24f, 0.45f); }
            else Add(b, Snare(0.2f, 75), 0.2f, 0.5f);
            Add(b, Hat(0.05f, false, 76), 0.32f, 0.16f);
            Add(b, Kick(0.24f, 140f, 50f, 0.2f, 77), 0.4f, 0.55f);
            Reverb(b, 0.08f);
            return Finish(b, 0.68f);
        }

        // --- money: soft FM ding + pluck + tiny sub (music, not a cartoon coin)
        static float[] Money(int ding1, int ding2, int pluck)
        {
            var b = Mix(0.55f);
            Add(b, Bell(ding1, 0.35f, 2f), 0f, 0.45f);
            Add(b, Bell(ding2, 0.4f, 1.6f), 0.06f, 0.35f);
            Add(b, Pluck(pluck, 0.3f, 0.5f, 81), 0f, 0.25f);
            Add(b, Kick(0.12f, 110f, 70f, 0f, 82), 0f, 0.25f);
            Reverb(b, 0.18f);
            return Finish(b, 0.6f);
        }

        static float[] Collect()
        {
            var b = Mix(0.9f);
            Add(b, Money(E5, A5, A4), 0f, 0.8f);
            var k = Keys(new[] { A3, C4, E4 }, 0.6f);
            Add(b, k, 0.08f, 0.35f);
            Add(b, Bell(A5 + 12, 0.4f, 1.2f), 0.18f, 0.12f);
            Reverb(b, 0.22f);
            return Finish(b, 0.68f);
        }

        // --- upgrade: pluck → rising tone → small impact + chord (under 1 s)
        static float[] Upgrade(int start, int sweepFrom, int sweepTo, int[] chord, int seed)
        {
            var b = Mix(0.9f);
            Add(b, Pluck(start, 0.25f, 0.5f, seed), 0f, 0.4f);
            Add(b, Sweep(Midi(sweepFrom), Midi(sweepTo), 0.26f, 0.25f), 0.06f, 0.22f);
            Add(b, Riser(0.28f, 400f, 2000f, seed), 0.04f, 0.12f);
            Add(b, Kick(0.25f, 130f, 50f, 0.2f, seed), 0.32f, 0.55f);
            Add(b, Stab(chord, 0.35f, 3000f, 600f), 0.32f, 0.3f);
            Add(b, Bell(chord[chord.Length - 1] + 24, 0.3f, 1.4f), 0.34f, 0.12f);
            Reverb(b, 0.2f);
            return Finish(b, 0.68f);
        }

        static float[] Unlock()
        {
            var b = Mix(1.2f);
            Add(b, Riser(0.35f, 300f, 1800f, 91), 0f, 0.22f);
            Add(b, Keys(new[] { A3, C4, E4, G4 }, 0.8f), 0.35f, 0.4f);
            int[] arp = { A4, C5, E5, A5 };
            for (int i = 0; i < arp.Length; i++) Add(b, Pluck(arp[i], 0.35f, 0.55f, 92 + i), 0.35f + i * 0.06f, 0.28f);
            Add(b, Bell(E6, 0.5f, 1.4f), 0.6f, 0.14f);
            Reverb(b, 0.3f, 1.1f);
            return Finish(b, 0.72f);
        }

        // --- big moments
        static float[] StudioTransition()
        {
            var b = Mix(1.6f);
            Add(b, Riser(0.55f, 200f, 2400f, 101), 0f, 0.4f);
            Add(b, Swell(0.5f, 102), 0.05f, 0.2f);
            Add(b, Impact(1.0f, 103), 0.55f, 0.65f);
            Add(b, Bass808(A1, 0.7f), 0.55f, 0.45f);
            Add(b, Stab(new[] { A3, C4, E4, G4 }, 0.5f, 3200f, 500f), 0.55f, 0.28f);
            Reverb(b, 0.28f, 1.2f);
            return Finish(b, 0.8f);
        }

        static float[] Hit()
        {
            var b = Mix(1.0f);
            for (int i = 0; i < 4; i++) Add(b, Snare(0.12f, 110 + i), i * 0.06f, 0.12f + i * 0.06f);
            Add(b, Kick(0.35f, 155f, 46f, 0.35f, 115), 0.25f, 0.9f);
            Add(b, Clap(0.25f, 116), 0.25f, 0.4f);
            Add(b, Bass808(A1, 0.5f), 0.255f, 0.5f);
            Add(b, Stab(new[] { A3, C4, E4 }, 0.35f, 3000f, 500f), 0.25f, 0.3f);
            Reverb(b, 0.2f);
            return Finish(b, 0.82f);
        }

        static float[] Viral(int bass, int[] chord, int seed)
        {
            var b = Mix(2.0f);
            Add(b, Riser(0.6f, 220f, 3000f, seed), 0f, 0.45f);
            Add(b, Swell(0.55f, seed + 1), 0.05f, 0.25f);
            Add(b, Impact(1.1f, seed + 2), 0.6f, 0.6f);
            Add(b, Kick(0.4f, 160f, 45f, 0.4f, seed + 3), 0.6f, 0.8f);
            Add(b, Bass808(bass, 0.9f), 0.605f, 0.55f);
            Add(b, Stab(chord, 0.6f, 3600f, 600f), 0.6f, 0.3f);
            int[] sparkle = { A5, C6, E6, A6 };
            for (int i = 0; i < sparkle.Length; i++) Add(b, Pluck(sparkle[i], 0.35f, 0.6f, seed + 10 + i), 0.64f + i * 0.05f, 0.16f);
            Reverb(b, 0.32f, 1.2f);
            return Finish(b, 0.85f);
        }

        static float[] Prestige()
        {
            var b = Mix(3.0f);
            Add(b, Riser(1.2f, 120f, 2600f, 121), 0f, 0.45f);
            Add(b, Swell(1.1f, 122), 0.1f, 0.25f);
            Add(b, Impact(1.2f, 123), 1.2f, 0.7f);
            Add(b, Bass808(A1, 1.2f), 1.205f, 0.5f);
            Add(b, Keys(new[] { A3, C4, E4, G4, A4 }, 1.5f), 1.2f, 0.4f);
            int[] arp = { E5, G5, A5, C6, E6 };
            for (int i = 0; i < arp.Length; i++) Add(b, Bell(arp[i], 0.5f, 1.4f), 1.3f + i * 0.09f, 0.12f);
            Reverb(b, 0.4f, 1.3f);
            return Finish(b, 0.85f);
        }

        static float[] Boost()
        {
            var b = Mix(0.8f);
            Add(b, Swell(0.3f, 131), 0f, 0.35f);
            Add(b, Pluck(A4, 0.3f, 0.5f, 132), 0.3f, 0.35f);
            Add(b, Pluck(E5, 0.3f, 0.5f, 133), 0.36f, 0.3f);
            Add(b, Bell(A5, 0.35f, 1.6f), 0.42f, 0.16f);
            Add(b, Hat(0.05f, false, 134), 0.3f, 0.12f);
            Reverb(b, 0.22f);
            return Finish(b, 0.65f);
        }
    }
}
