using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Generates all placeholder art as PNG files (original, procedural, vector style).
    /// Every file is a "slot": replace it with final art of the same size and name.
    /// Characters/environments live in partial files ArtGenerator.Characters.cs / ArtGenerator.Environments.cs.
    /// Menu: Tools > Beat Factory > Art > Regenerate Art.
    /// </summary>
    public static partial class ArtGenerator
    {
        public const string ArtRoot = BeatFactorySceneBuilder.Root + "/Art";
        public const string Env = ArtRoot + "/Environments";
        public const string Props = ArtRoot + "/Props";
        public const string Chars = ArtRoot + "/Characters";
        public const string Icons = ArtRoot + "/UI/Icons";
        public const string Fx = ArtRoot + "/Effects";
        public const string Daw = ArtRoot + "/DAW";

        /// <summary>Room canvas. Rooms are drawn on a 1000×560 layout, then lifted by <see cref="Lift"/>
        /// so a deeper floor gives the producer stations a full-width front row.</summary>
        /// <remarks>CardH is the studio card height in UI units; the extra ceiling above it (SceneH − CardH)
        /// lets the room fill narrower phones, where the room is scaled to the card WIDTH and anchored at the bottom.</remarks>
        public const int SceneW = 1000, DrawH = 560, Lift = 120, CardH = DrawH + Lift, SceneH = CardH + 180;

        /// <summary>Visual studio tiers (studio level 0, 1, 2, 3+).</summary>
        public static readonly string[] EnvPaths =
        {
            Env + "/Basement/env_basement.png",
            Env + "/BedroomStudio/env_bedroom.png",
            Env + "/SmallProStudio/env_smallpro.png",
            Env + "/ProfessionalStudio/env_professional.png",
            Env + "/ModernStudio/env_modern.png",
            Env + "/LuxuryStudio/env_luxury.png",
            Env + "/MajorStudio/env_major.png",
            Env + "/EliteStudio/env_elite.png",
            Env + "/RecordLabelStudio/env_label.png",
            Env + "/MegaComplex/env_mega.png",
        };

        public static string StationPath(string id) => $"{Props}/Furniture/station_{id}.png";
        public const string SpeakerConePath = Props + "/Speakers/speaker_cone.png";
        public const string DawBackgroundPath = Daw + "/daw_background.png";

        public static string CharFolder(string id) => id == "player" ? Chars + "/Player" : $"{Chars}/Producers/{id}";
        public static string CharPart(string id, string part) => $"{CharFolder(id)}/{id}_{part}.png";
        public static string PortraitPath(string id) => $"{Chars}/Portraits/portrait_{id}.png";

        public static readonly string[] Expressions = { "idle", "focus", "smile", "excited", "blink" };

        [MenuItem("Tools/Beat Factory/Art/Regenerate Art")]
        static void Menu()
        {
            GenerateAll(true);
            Debug.Log("[Beat Factory] Art regenerated in " + ArtRoot);
        }

        public static void GenerateAll(bool overwrite)
        {
            for (int i = 0; i < EnvPaths.Length; i++)
            {
                int tier = i;
                Make(EnvPaths[i], overwrite, () => Environment(tier));
            }

            Make(StationPath("beginner"), overwrite, StationBeginner);
            Make(StationPath("melody"), overwrite, StationMelody);
            Make(StationPath("drum"), overwrite, StationDrum);
            Make(SpeakerConePath, overwrite, SpeakerCone);
            Make(DawBackgroundPath, overwrite, DawBackground);

            foreach (var c in Cast)
            {
                var ch = c;
                Make(CharPart(ch.id, "body"), overwrite, () => Body(ch));
                Make(CharPart(ch.id, "arm"), overwrite, () => Arm(ch));
                foreach (var e in Expressions)
                {
                    var expr = e;
                    Make(CharPart(ch.id, "head_" + expr), overwrite, () => Head(ch, expr));
                }
                Make(PortraitPath(ch.id), overwrite, () => Portrait(ch));
            }
            // The player's outfit improves with the studio tier.
            for (int t = 0; t < 4; t++)
            {
                var look = PlayerTier(t);
                Make(CharPart("player", "body_t" + t), overwrite, () => Body(look));
                Make(CharPart("player", "arm_t" + t), overwrite, () => Arm(look));
            }
            foreach (var c in ExtraProducers)
            {
                var ch = c;
                Make(PortraitPath(ch.id), overwrite, () => Portrait(ch));
            }
            foreach (var a in Roster)
            {
                var spec = a;
                Make(ArtistPortraitPath(spec.id), overwrite, () => ArtistPortrait(spec));
            }

            Make(Icons + "/icon_money.png", overwrite, IconMoney);
            Make(Icons + "/icon_beat.png", overwrite, IconBeat);
            Make(Icons + "/icon_producer.png", overwrite, IconProducer);
            Make(Icons + "/icon_upgrade.png", overwrite, IconUpgrade);
            Make(Icons + "/icon_studio.png", overwrite, IconStudio);
            Make(Icons + "/icon_settings.png", overwrite, IconSettings);
            Make(Icons + "/icon_music.png", overwrite, IconMusic);
            Make(Icons + "/icon_viral.png", overwrite, IconViral);
            Make(Icons + "/icon_artist.png", overwrite, IconMic);
            Make(Icons + "/icon_prestige.png", overwrite, IconCrown);
            Make(Icons + "/icon_streams.png", overwrite, IconPlay);
            Make(Icons + "/icon_boost.png", overwrite, IconBolt);
            Make(Icons + "/icon_fame.png", overwrite, IconStar);

            Make(Fx + "/fx_note.png", overwrite, FxNote);
            Make(Fx + "/fx_sparkle.png", overwrite, FxSparkle);
            Make(Fx + "/fx_confetti.png", overwrite, FxConfetti);
            Make(Fx + "/fx_glow.png", overwrite, FxGlow);
            GenerateVenues(overwrite);
            GeneratePerformers(overwrite);

            DeleteOldArt();
        }

        static void Make(string path, bool overwrite, Func<SdfPainter> draw)
        {
            if (!overwrite && File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            draw().Save(path);
        }

        /// <summary>Removes files from the previous art layout (bedroom-first, back-view characters).</summary>
        static void DeleteOldArt()
        {
            string[] old =
            {
                Env + "/studio_bedroom.png", Env + "/studio_pro.png", Env + "/studio_modern.png",
                Props + "/station_beginner.png", Props + "/station_melody.png", Props + "/station_drum.png", Props + "/speaker_cone.png",
            };
            foreach (var f in old) if (File.Exists(f)) AssetDatabase.DeleteAsset(f);
            foreach (var id in new[] { "main", "beginner", "melody", "drum" })
            {
                foreach (var part in new[] { "body", "head", "arm" })
                {
                    string f = $"{Chars}/{id}_{part}.png";
                    if (File.Exists(f)) AssetDatabase.DeleteAsset(f);
                }
                string portrait = $"{Chars}/Portraits/portrait_{id}.png";
                if (id == "main" && File.Exists(portrait)) AssetDatabase.DeleteAsset(portrait);
            }
        }

        // ================================================================== DAW (original "BEATLAB" interface)

        /// <summary>Static layout of the fictional BEATLAB DAW (400 × 250). Animated parts are added at runtime.</summary>
        static SdfPainter DawBackground()
        {
            var p = new SdfPainter(400, 250);
            p.Fill(RRect(0, 0, 400, 250), Hex("0D0B18"));
            // transport bar
            p.Fill(RRect(0, 220, 400, 30), Hex("1C1532"));
            p.Fill(Polygon(new Vector2(12, 228), new Vector2(12, 242), new Vector2(24, 235)), Hex("4DFFA8"));
            p.Fill(RRect(30, 229, 12, 12, 2), Hex("8A84B0"));
            p.Fill(Circle(new Vector2(54, 235), 6), Hex("FF3D5A"));
            p.Fill(RRect(300, 226, 92, 18, 4), Hex("0D0B18"));
            // channel rack (left)
            string[] colors = { "FF3DC8", "3DE1FF", "4DFFA8", "FFD23D", "B65CFF", "FF8A3D" };
            for (int i = 0; i < 6; i++)
            {
                float y = 194 - i * 22;
                p.Fill(RRect(4, y, 76, 18, 3), Hex("17132B"));
                p.Fill(RRect(7, y + 3, 12, 12, 2), Hex(colors[i]));
            }
            // timeline lanes + grid
            for (int i = 0; i < 6; i++)
                p.Fill(RRect(84, 194 - i * 22, 312, 18), Hex(i % 2 == 0 ? "141127" : "17132D"));
            for (int b = 0; b <= 8; b++)
                p.Fill(RRect(84 + b * 39, 62, b % 4 == 0 ? 2 : 1, 152), Hex("2A2448", b % 4 == 0 ? 1f : 0.7f));
            p.Fill(RRect(84, 214, 312, 4), Hex("1C1532"));
            // bottom: mixer | piano roll | synth + waveform
            p.Fill(RRect(0, 0, 400, 58), Hex("130F24"));
            for (int i = 0; i < 6; i++)
            {
                float x = 6 + i * 22;
                p.Fill(RRect(x, 4, 18, 50, 3), Hex("0B0918"));
                p.Fill(RRect(x + 7, 8, 4, 40, 2), Hex("2A2448"));
                p.Fill(Circle(new Vector2(x + 9, 51), 2), Hex(colors[i]));
            }
            p.Fill(RRect(144, 4, 120, 50, 3), Hex("0B0918"));
            for (int r = 0; r < 8; r++)
            {
                bool black = r == 1 || r == 3 || r == 6;
                p.Fill(RRect(146, 6 + r * 6, 8, 5, 1), black ? Hex("1A1626") : Hex("D9D6E4"));
                p.Fill(RRect(156, 6 + r * 6 + 5.4f, 106, 0.6f), Hex("2A2448", 0.7f));
            }
            for (int s = 0; s <= 16; s++) p.Fill(RRect(156 + s * 6.6f, 6, s % 4 == 0 ? 1 : 0.5f, 48), Hex("2A2448", s % 4 == 0 ? 1f : 0.6f));
            p.Fill(RRect(268, 30, 128, 24, 3), Hex("1C1532"));
            for (int k = 0; k < 4; k++)
            {
                p.Fill(Circle(new Vector2(284 + k * 30, 42), 8), Hex("0B0918"));
                p.Fill(Ring(new Vector2(284 + k * 30, 42), 9.5f, 1.2f), Hex(colors[k], 0.8f));
            }
            p.Fill(RRect(268, 4, 128, 24, 3), Hex("0B0918"));
            p.Fill(RRect(270, 16, 124, 0.8f), Hex("2A2448"));
            return p;
        }

        // ================================================================== ICONS (128 × 128, white — tinted in UI)

        static readonly Color W = Color.white;
        static SdfPainter Icon() => new SdfPainter(128, 128);

        static SdfPainter IconMoney()
        {
            var p = Icon();
            p.Fill(Ring(new Vector2(64, 64), 50, 11), W);
            p.Fill(Capsule(new Vector2(64, 30), new Vector2(64, 98), 4.5f), W);
            p.Fill(Arc(new Vector2(64, 74), 14, 9, 15, 270), W);
            p.Fill(Arc(new Vector2(64, 50), 14, 9, 195, 90), W);
            return p;
        }

        static SdfPainter IconBeat()
        {
            var p = Icon();
            p.Fill(Box(new Vector2(50, 34), new Vector2(22, 16), 16, 20), W);
            p.Fill(RRect(64, 34, 11, 76, 3), W);
            p.Fill(Capsule(new Vector2(72, 106), new Vector2(100, 80), 8), W);
            return p;
        }

        static SdfPainter IconProducer()
        {
            var p = Icon();
            p.Fill(Circle(new Vector2(64, 76), 24), W);
            p.Fill(Arc(new Vector2(64, 76), 33, 9, 10, 170), W);
            p.Fill(Box(new Vector2(31, 72), new Vector2(9, 15), 8), W);
            p.Fill(Box(new Vector2(97, 72), new Vector2(9, 15), 8), W);
            p.Fill(Intersect(Ellipse(new Vector2(64, 8), new Vector2(46, 42)), RRect(0, 0, 128, 44)), W);
            return p;
        }

        static SdfPainter IconUpgrade()
        {
            var p = Icon();
            p.Fill(Polygon(new Vector2(64, 116), new Vector2(110, 66), new Vector2(80, 66), new Vector2(80, 14),
                new Vector2(48, 14), new Vector2(48, 66), new Vector2(18, 66)), W);
            return p;
        }

        static SdfPainter IconStudio()
        {
            var p = Icon();
            p.Fill(Subtract(Union(Polygon(new Vector2(8, 62), new Vector2(64, 114), new Vector2(120, 62)), RRect(24, 12, 80, 56, 4)),
                RRect(52, 12, 24, 32, 4)), W);
            return p;
        }

        static SdfPainter IconSettings()
        {
            var p = Icon();
            Func<Vector2, float> gear = Circle(new Vector2(64, 64), 36);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                gear = Union(gear, Box(new Vector2(64, 64) + dir * 44, new Vector2(10, 12), 3, a + 90));
            }
            p.Fill(Subtract(gear, Circle(new Vector2(64, 64), 15)), W);
            return p;
        }

        static SdfPainter IconMusic()
        {
            var p = Icon();
            p.Fill(Box(new Vector2(34, 30), new Vector2(18, 13), 13, 20), W);
            p.Fill(Box(new Vector2(88, 42), new Vector2(18, 13), 13, 20), W);
            p.Fill(RRect(44, 30, 9, 74, 3), W);
            p.Fill(RRect(98, 42, 9, 66, 3), W);
            p.Fill(Polygon(new Vector2(44, 104), new Vector2(107, 116), new Vector2(107, 98), new Vector2(44, 86)), W);
            return p;
        }

        static SdfPainter IconViral()
        {
            var p = Icon();
            var outer = SmoothUnion(Circle(new Vector2(64, 46), 34), Polygon(new Vector2(36, 58), new Vector2(66, 124), new Vector2(94, 58)), 16);
            var inner = SmoothUnion(Circle(new Vector2(64, 38), 15), Polygon(new Vector2(52, 44), new Vector2(66, 82), new Vector2(78, 44)), 8);
            p.Fill(Subtract(outer, inner), W);
            return p;
        }

        static SdfPainter IconMic()
        {
            var p = Icon();
            p.Fill(Capsule(new Vector2(64, 72), new Vector2(64, 96), 22), W);
            p.Fill(Arc(new Vector2(64, 72), 34, 8, 200, 340), W);
            p.Fill(RRect(60, 20, 8, 22), W);
            p.Fill(RRect(38, 12, 52, 10, 5), W);
            return p;
        }

        static SdfPainter IconCrown()
        {
            var p = Icon();
            p.Fill(Polygon(new Vector2(14, 34), new Vector2(114, 34), new Vector2(114, 96), new Vector2(88, 64),
                new Vector2(64, 108), new Vector2(40, 64), new Vector2(14, 96)), W);
            p.Fill(RRect(14, 14, 100, 14, 5), W);
            return p;
        }

        static SdfPainter IconPlay()
        {
            var p = Icon();
            p.Fill(Subtract(Circle(new Vector2(64, 64), 54), Polygon(new Vector2(50, 38), new Vector2(50, 90), new Vector2(94, 64))), W);
            return p;
        }

        static SdfPainter IconBolt()
        {
            var p = Icon();
            p.Fill(Polygon(new Vector2(76, 124), new Vector2(30, 60), new Vector2(60, 60), new Vector2(48, 4),
                new Vector2(100, 74), new Vector2(68, 74)), W);
            return p;
        }

        static SdfPainter IconStar()
        {
            var p = Icon();
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = (90 + i * 36) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? 58 : 24;
                pts[i] = new Vector2(64 + Mathf.Cos(a) * r, 60 + Mathf.Sin(a) * r);
            }
            p.Fill(Polygon(pts), W);
            return p;
        }

        // ================================================================== EFFECTS

        static SdfPainter FxNote()
        {
            var p = new SdfPainter(64, 64);
            p.Fill(Box(new Vector2(24, 16), new Vector2(11, 8), 8, 20), W);
            p.Fill(RRect(31, 16, 6, 40, 2), W);
            p.Fill(Capsule(new Vector2(35, 54), new Vector2(50, 42), 4), W);
            return p;
        }

        static SdfPainter FxSparkle()
        {
            var p = new SdfPainter(64, 64);
            p.Fill(Polygon(new Vector2(32, 63), new Vector2(37, 37), new Vector2(63, 32), new Vector2(37, 27),
                new Vector2(32, 1), new Vector2(27, 27), new Vector2(1, 32), new Vector2(27, 37)), W);
            return p;
        }

        static SdfPainter FxConfetti()
        {
            var p = new SdfPainter(24, 12);
            p.Fill(RRect(1, 1, 22, 10, 3), W);
            return p;
        }

        static SdfPainter FxGlow()
        {
            var p = new SdfPainter(128, 128);
            p.Glow(Circle(new Vector2(64, 64), 2), W, 62);
            return p;
        }

        static SdfPainter SpeakerCone()
        {
            var p = new SdfPainter(64, 64);
            var c = new Vector2(32, 32);
            p.Fill(Circle(c, 30), Hex("0E0C14"));
            p.Fill(Ring(c, 26, 3), Hex("4A4458"));
            p.Fill(Circle(c, 18), Hex("1C1A26"));
            p.Fill(Circle(c, 9), Hex("3A3548"));
            p.Fill(Circle(c + new Vector2(-3, 3), 3), Hex("FFFFFF", 0.25f));
            return p;
        }
    }
}
