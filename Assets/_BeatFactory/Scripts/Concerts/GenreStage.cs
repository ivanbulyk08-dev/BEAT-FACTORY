using UnityEngine;

namespace BeatFactory
{
    /// <summary>Stage show look per genre: rage = aggressive red/purple + glitch, R&amp;B = soft and warm, drill = dark high-contrast strobes …</summary>
    public struct GenreStage
    {
        public Color[] palette;
        public float bpm;
        public bool strobe;
        public bool glitch;
        public bool soft;
        public bool extraLasers;
        public string vibe;

        public static GenreStage For(ArtistGenre g)
        {
            switch (g)
            {
                case ArtistGenre.Rage:
                    return new GenreStage { palette = new[] { C("FF2B3D"), C("B65CFF"), C("FF3DC8") }, bpm = 150, strobe = true, glitch = true, extraLasers = true, vibe = "MOSH PIT ENERGY" };
                case ArtistGenre.Trap:
                    return new GenreStage { palette = new[] { C("B65CFF"), C("FF3DC8"), C("6C3BD9") }, bpm = 140, strobe = true, vibe = "808s SHAKING THE FLOOR" };
                case ArtistGenre.Underground:
                    return new GenreStage { palette = new[] { C("A8A3C8"), C("FFB347"), C("6A6290") }, bpm = 90, vibe = "RAW AND REAL" };
                case ArtistGenre.Drill:
                    return new GenreStage { palette = new[] { C("E8F0FF"), C("5A86C8"), C("2A3A5A") }, bpm = 142, strobe = true, vibe = "COLD AND HEAVY" };
                case ArtistGenre.Melodic:
                    return new GenreStage { palette = new[] { C("FF8AD8"), C("8AE8FF"), C("C8A8FF") }, bpm = 120, soft = true, vibe = "EVERYONE IS SINGING" };
                case ArtistGenre.RnB:
                    return new GenreStage { palette = new[] { C("FFB38A"), C("FF7A9A"), C("FFD8A0") }, bpm = 80, soft = true, vibe = "SMOOTH ATMOSPHERE" };
                case ArtistGenre.Experimental:
                    return new GenreStage { palette = new[] { C("3DE1FF"), C("4DFFA8"), C("FF3DC8") }, bpm = 128, extraLasers = true, glitch = true, vibe = "A SOUND FROM THE FUTURE" };
                default:
                    return new GenreStage { palette = new[] { C("FFD23D"), C("FF8A3D"), C("F2F0FF") }, bpm = 95, vibe = "HANDS IN THE AIR" };
            }
        }

        static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
