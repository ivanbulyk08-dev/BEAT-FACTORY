using System;
using UnityEngine;

namespace BeatFactory
{
    public enum Wave { Sine, Triangle, Saw, Square, SuperSaw, Bell, EPiano }

    /// <summary>
    /// An artist's sound: tempo, key, scale, drum feel, 808, chords, lead and pad timbres, distortion, brightness and swing.
    /// Built deterministically from the artist (genre + their id + their look), so every artist has their OWN original sound —
    /// two rage artists differ (darker/distorted vs brighter/melodic vs minimal/bass-heavy). No real songs are copied.
    /// </summary>
    [Serializable]
    public class ConcertMusicProfile
    {
        public string flavor;          // "dark · distorted", "bright · melodic" … (English, logs)
        public string flavorKey;       // music.flavor.{key}

        /// <summary>"dark · aggressive · distorted  ·  150 BPM" in the player's language.</summary>
        public string FlavorText => Loc.Or("music.flavor." + flavorKey, flavor) + "  ·  " + Mathf.RoundToInt(bpm) + " BPM";
        public float bpm;
        public int root;               // MIDI note of the key (bass octave)
        public int[] scale;            // semitone offsets
        public int[][] progression;    // chord roots as scale degrees, one per bar
        public float drive;            // 0..1 distortion on 808/lead
        public float brightness;       // 0..1 filter cutoff
        public float swing;            // 0..0.3 delay of off-beat 16ths
        public float reverb;           // 0..1
        public int drums;              // 0 rage, 1 trap, 2 drill, 3 melodic, 4 rnb, 5 experimental, 6 boom-bap, 7 dusty
        public bool halfTime;          // snare on beat 3
        public Wave lead, pad;
        public float leadLevel, padLevel, bassLevel;
        public bool glide808;
        public int melodySeed;
        public bool crackle;           // vinyl dust (underground)
        public bool glitch;            // experimental stutters

        public float BeatSeconds => 60f / bpm;
    }

    public static class ArtistConcertMusic
    {
        static readonly int[] Minor = { 0, 2, 3, 5, 7, 8, 10 };
        static readonly int[] Phrygian = { 0, 1, 3, 5, 7, 8, 10 };
        static readonly int[] Dorian = { 0, 2, 3, 5, 7, 9, 10 };
        static readonly int[] HarmonicMinor = { 0, 2, 3, 5, 7, 8, 11 };
        static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };

        public static ConcertMusicProfile For(ArtistData a)
        {
            int h = Math.Abs((a != null ? a.id : "x").GetHashCode());
            int variant = h % 3;            // 0 darker/aggressive · 1 brighter/melodic · 2 minimal/bass-heavy/experimental
            var r = new System.Random(h);
            var p = new ConcertMusicProfile { melodySeed = h, root = 33 + h / 3 % 12 };
            var g = a != null ? a.genre : ArtistGenre.HipHop;
            float accentBright = 0.5f;
            if (a != null && a.visual != null) accentBright = Mathf.Clamp01(a.visual.accent.maxColorComponent);

            switch (g)
            {
                case ArtistGenre.Rage:
                    p.bpm = 148 + r.Next(10); p.scale = Phrygian; p.drums = 0; p.lead = Wave.SuperSaw; p.pad = Wave.Saw;
                    p.drive = 0.75f; p.brightness = 0.75f; p.glide808 = true; p.leadLevel = 0.5f; p.padLevel = 0.25f; p.bassLevel = 1f;
                    p.progression = Prog(r, new[] { 0, 0, 5, 6 }, new[] { 0, 1, 0, 6 }, new[] { 0, 3, 5, 1 });
                    break;
                case ArtistGenre.Trap:
                    p.bpm = 136 + r.Next(8); p.scale = Minor; p.drums = 1; p.halfTime = true; p.lead = Wave.Bell; p.pad = Wave.Saw;
                    p.drive = 0.4f; p.brightness = 0.45f; p.glide808 = true; p.leadLevel = 0.45f; p.padLevel = 0.3f; p.bassLevel = 1f;
                    p.progression = Prog(r, new[] { 0, 5, 3, 4 }, new[] { 0, 5, 6, 4 }, new[] { 0, 3, 5, 4 });
                    break;
                case ArtistGenre.Drill:
                    p.bpm = 140 + r.Next(5); p.scale = HarmonicMinor; p.drums = 2; p.halfTime = true; p.lead = Wave.Bell; p.pad = Wave.Triangle;
                    p.drive = 0.35f; p.brightness = 0.4f; p.glide808 = true; p.leadLevel = 0.55f; p.padLevel = 0.2f; p.bassLevel = 1f;
                    p.progression = Prog(r, new[] { 0, 5, 4, 0 }, new[] { 0, 6, 5, 4 }, new[] { 0, 0, 5, 4 });
                    break;
                case ArtistGenre.Melodic:
                    p.bpm = 120 + r.Next(10); p.scale = Dorian; p.drums = 3; p.lead = Wave.Triangle; p.pad = Wave.SuperSaw;
                    p.drive = 0.1f; p.brightness = 0.65f; p.reverb = 0.7f; p.leadLevel = 0.5f; p.padLevel = 0.5f; p.bassLevel = 0.8f;
                    p.progression = Prog(r, new[] { 0, 5, 2, 6 }, new[] { 0, 3, 5, 4 }, new[] { 5, 3, 0, 4 });
                    break;
                case ArtistGenre.RnB:
                    p.bpm = 76 + r.Next(14); p.scale = Dorian; p.drums = 4; p.halfTime = true; p.lead = Wave.EPiano; p.pad = Wave.EPiano;
                    p.drive = 0.05f; p.brightness = 0.55f; p.swing = 0.18f; p.reverb = 0.6f; p.leadLevel = 0.35f; p.padLevel = 0.6f; p.bassLevel = 0.85f;
                    p.progression = Prog(r, new[] { 1, 4, 0, 5 }, new[] { 0, 3, 1, 4 }, new[] { 5, 1, 4, 0 });
                    break;
                case ArtistGenre.Experimental:
                    p.bpm = 122 + r.Next(14); p.scale = Phrygian; p.drums = 5; p.lead = Wave.Square; p.pad = Wave.Saw; p.glitch = true;
                    p.drive = 0.55f; p.brightness = 0.85f; p.reverb = 0.4f; p.leadLevel = 0.45f; p.padLevel = 0.35f; p.bassLevel = 0.9f;
                    p.progression = Prog(r, new[] { 0, 1, 0, 6 }, new[] { 0, 4, 1, 5 }, new[] { 0, 0, 1, 1 });
                    break;
                case ArtistGenre.Underground:
                    p.bpm = 86 + r.Next(10); p.scale = Minor; p.drums = 7; p.lead = Wave.Square; p.pad = Wave.EPiano; p.crackle = true;
                    p.drive = 0.2f; p.brightness = 0.3f; p.swing = 0.22f; p.reverb = 0.3f; p.leadLevel = 0.35f; p.padLevel = 0.45f; p.bassLevel = 0.9f;
                    p.progression = Prog(r, new[] { 0, 3, 0, 4 }, new[] { 0, 5, 3, 4 }, new[] { 0, 6, 5, 4 });
                    break;
                default: // hip-hop: punchy drums, bass, chopped sample stabs, modern low end
                    p.bpm = 88 + r.Next(9); p.scale = Minor; p.drums = 6; p.lead = Wave.Square; p.pad = Wave.EPiano;
                    p.drive = 0.25f; p.brightness = 0.5f; p.swing = 0.15f; p.reverb = 0.35f; p.leadLevel = 0.4f; p.padLevel = 0.5f; p.bassLevel = 0.9f;
                    p.progression = Prog(r, new[] { 0, 5, 3, 4 }, new[] { 0, 3, 5, 4 }, new[] { 0, 2, 5, 4 });
                    break;
            }

            // the artist's own flavor
            switch (variant)
            {
                case 0:
                    p.drive = Mathf.Clamp01(p.drive + 0.2f); p.brightness *= 0.85f; p.padLevel *= 0.8f;
                    p.flavorKey = g == ArtistGenre.RnB || g == ArtistGenre.Melodic ? "dark_moody" : "dark_aggressive";
                    p.flavor = g == ArtistGenre.RnB || g == ArtistGenre.Melodic ? "dark · moody · deep" : "dark · aggressive · distorted";
                    break;
                case 1: p.brightness = Mathf.Clamp01(p.brightness + 0.2f); p.leadLevel *= 1.2f; p.padLevel *= 1.25f; p.reverb = Mathf.Clamp01(p.reverb + 0.25f); p.flavor = "bright · melodic · atmospheric"; p.flavorKey = "bright"; p.root += 2; break;
                default: p.bassLevel *= 1.15f; p.leadLevel *= 0.75f; p.padLevel *= 0.7f; p.glitch |= g != ArtistGenre.RnB; p.flavor = "minimal · bass-heavy · experimental"; p.flavorKey = "minimal"; break;
            }
            p.brightness = Mathf.Clamp01(p.brightness * Mathf.Lerp(0.85f, 1.15f, accentBright));
            return p;
        }

        static int[][] Prog(System.Random r, params int[][] options)
        {
            var pick = options[r.Next(options.Length)];
            var bars = new int[pick.Length][];
            for (int i = 0; i < pick.Length; i++) bars[i] = new[] { pick[i] };
            return bars;
        }
    }
}
