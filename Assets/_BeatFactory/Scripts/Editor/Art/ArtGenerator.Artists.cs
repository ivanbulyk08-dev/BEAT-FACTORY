using System;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// The fictional artist universe: 32 original underground artists across 8 genres.
    /// Names were created independently in the naming culture of modern trap/rage/SoundCloud rap
    /// (short names, X/V/Z/K, Ø, numbers, "Lil"/"Young"/"Baby", lowercase) — none is a variant of a real artist.
    /// Each entry defines the gameplay data AND the look (portrait), so name and character always match.
    /// </summary>
    public static partial class ArtGenerator
    {
        public class ArtistSpec
        {
            public string id, name, bio, beat;
            public ArtistGenre genre;
            public int studio, popularity;
            public double cost, multiplier, listeners;
            public Look look;
        }

        static readonly (string skin, string shade, string lip, string eye)[] SkinTones =
        {
            ("3E2618", "2C1A10", "2A1610", "140C08"), // 0 deep
            ("4A2E1F", "34200F", "2E1912", "160C08"), // 1 dark
            ("6B4430", "4E2F20", "4A2620", "1E120C"), // 2 brown
            ("8D5A3B", "6B4129", "5C3022", "24140C"), // 3 medium brown
            ("B57A52", "94603E", "7A4232", "2A180E"), // 4 tan
            ("C68E64", "A46E48", "9A5048", "3A2A18"), // 5 olive
            ("E0B48C", "C2926A", "C0626A", "4A2C18"), // 6 light tan
            ("F1C9A3", "D6A57E", "C77B6F", "2E1C12"), // 7 light
            ("F6DCCB", "E2B9A2", "D08A86", "3A4A5A"), // 8 pale
        };

        /// <summary>Compact look builder. Extras: chain, cuban, stud, hoop, glasses, shades, beanie=HEX, cap=HEX, dots, star,
        /// neck, septum, nose, beard, stubble, lashes, lips, square, slit, phones, silver.</summary>
        static Look Mk(int skin, HairStyle hair, string hairHex, string tipHex, TopStyle top, string cloth, string accent, string expr, string extras = "")
        {
            var s = SkinTones[skin];
            var l = new Look
            {
                skin = Hex(s.skin), skinShade = Hex(s.shade), lip = Hex(s.lip), eye = Hex(s.eye), brow = Hex("0C0806"),
                hairStyle = hair, hair = Hex(hairHex), hairTip = Hex(tipHex),
                top = top, cloth = Hex(cloth), clothShade = Color.Lerp(Hex(cloth), Color.black, 0.35f), accent = Hex(accent),
                portraitExpression = expr, metal = Hex("F2C14E"),
                phones = Hex("16161A"), phonesAccent = Hex(accent),
                noseW = skin <= 3 ? 1.25f : 1f, browThick = 3f,
            };
            foreach (var raw in extras.Split(','))
            {
                var e = raw.Trim();
                if (e.Length == 0) continue;
                var kv = e.Split('=');
                switch (kv[0])
                {
                    case "chain": l.chainThin = true; break;
                    case "cuban": l.chainCuban = true; break;
                    case "stud": l.earStud = true; break;
                    case "hoop": l.earHoop = true; break;
                    case "glasses": l.glasses = true; break;
                    case "shades": l.sunglasses = true; break;
                    case "beanie": l.beanie = Hex(kv[1]); break;
                    case "cap": l.cap = Hex(kv[1]); break;
                    case "dots": l.faceDots = true; break;
                    case "star": l.faceStar = true; break;
                    case "neck": l.neckTattoo = true; break;
                    case "septum": l.septum = true; break;
                    case "nose": l.noseRing = true; break;
                    case "beard": l.beard = true; l.squareJaw = true; break;
                    case "stubble": l.stubble = true; break;
                    case "lashes": l.lashes = true; l.jaw = 0.92f; l.browThick = 2.3f; break;
                    case "lips": l.lipFull = true; break;
                    case "square": l.squareJaw = true; break;
                    case "slit": l.browSlit = true; break;
                    case "phones": l.phonesOnNeck = true; break;
                    case "silver": l.metal = Hex("D9DEE8"); break;
                }
            }
            return l;
        }

        static ArtistSpec Art(string id, string name, ArtistGenre g, int studio, double cost, double mult, double listeners, int pop,
            string bio, string beat, Look look)
        {
            look.id = id;
            look.portraitBg = Color.Lerp(ArtistGenres.Color(g), Color.black, 0.75f);
            return new ArtistSpec { id = id, name = name, genre = g, studio = studio, cost = cost, multiplier = mult, listeners = listeners,
                popularity = pop, bio = bio, beat = beat, look = look };
        }

        /// <summary>Portrait with a genre-specific backdrop (rage gets a distorted glitch pass over the whole image).</summary>
        static SdfPainter ArtistPortrait(ArtistSpec a)
        {
            var g = a.genre;
            Color gc = ArtistGenres.Color(g);
            var circle = Circle(new Vector2(128, 128), 122);
            var rnd = new System.Random(a.id.GetHashCode());
            float R() => (float)rnd.NextDouble();

            void Background(SdfPainter p)
            {
                Func<Vector2, float> In(Func<Vector2, float> s) => Intersect(s, circle);
                switch (g)
                {
                    case ArtistGenre.Rage:
                        for (int i = 0; i < 14; i++)
                        {
                            float y = R() * 256, h = 2 + R() * 8;
                            p.Fill(In(RRect(0, y, 256, h)), new Color(gc.r, gc.g, gc.b, 0.25f + R() * 0.3f));
                        }
                        break;
                    case ArtistGenre.Melodic:
                    case ArtistGenre.RnB:
                        for (int i = 0; i < 9; i++)
                        {
                            var c = new Vector2(R() * 256, 60 + R() * 196);
                            float r = 14 + R() * 30;
                            p.Fill(In(Circle(c, r)), q => new Color(1, 1, 1, 0.12f * (1f - Mathf.Clamp01((q - c).magnitude / r))));
                        }
                        break;
                    case ArtistGenre.Drill:
                        for (float y = 0; y < 256; y += 6) p.Fill(In(RRect(0, y, 256, 1.2f)), new Color(1, 1, 1, 0.05f));
                        p.Fill(In(RRect(0, 0, 256, 90)), new Color(0, 0, 0, 0.3f));
                        break;
                    case ArtistGenre.Experimental:
                        for (int i = 0; i < 6; i++)
                            p.Fill(In(Ring(new Vector2(128, 150), 30 + i * 22, 2)), new Color(i % 2 == 0 ? 0.24f : 1f, i % 2 == 0 ? 0.88f : 0.24f, i % 2 == 0 ? 1f : 0.78f, 0.35f));
                        break;
                    case ArtistGenre.Trap:
                        for (int i = 0; i < 5; i++)
                        {
                            var c = new Vector2(R() * 256, 140 + R() * 100);
                            p.Fill(In(Ellipse(c, new Vector2(70, 22))), q => new Color(gc.r, gc.g, gc.b, 0.18f * (1f - Mathf.Clamp01((q - c).magnitude / 70f))));
                        }
                        break;
                    case ArtistGenre.Underground:
                        for (int i = 0; i < 120; i++) p.Fill(In(Circle(new Vector2(R() * 256, R() * 256), 0.8f + R() * 1.6f)), new Color(1, 1, 1, 0.06f));
                        break;
                    default: // hip-hop: halftone gold dots
                        for (float x = 8; x < 256; x += 14)
                        for (float y = 120; y < 256; y += 14)
                            p.Fill(In(Circle(new Vector2(x, y), 1.2f + (y - 120) / 60f)), new Color(gc.r, gc.g, gc.b, 0.22f));
                        break;
                }
            }

            void Overlay(SdfPainter p)
            {
                if (g != ArtistGenre.Rage) return;
                // distorted look: chromatic glitch slices across the portrait
                for (int i = 0; i < 6; i++)
                {
                    float y = 40 + R() * 180, h = 1.5f + R() * 4f, off = (R() - 0.5f) * 30f;
                    p.Fill(Intersect(RRect(off, y, 256, h), circle), new Color(1f, 0.15f, 0.25f, 0.35f));
                    p.Fill(Intersect(RRect(-off, y + h + 1, 256, h * 0.6f), circle), new Color(0.2f, 0.9f, 1f, 0.25f));
                }
            }

            return Portrait(a.look, Background, Overlay, gc);
        }

        const ArtistGenre Rage = ArtistGenre.Rage, Trap = ArtistGenre.Trap, Under = ArtistGenre.Underground, Drill = ArtistGenre.Drill,
            Melodic = ArtistGenre.Melodic, RnB = ArtistGenre.RnB, Exp = ArtistGenre.Experimental, HipHop = ArtistGenre.HipHop;
        const HairStyle Messy = HairStyle.Messy, Bob = HairStyle.Bob, Locs = HairStyle.LocsCap, Half = HairStyle.DreadsHalfUp,
            Tied = HairStyle.DreadsTied, Braids = HairStyle.Braids, Long = HairStyle.LongStraight, Curly = HairStyle.CurlyFade, Twists = HairStyle.Twists;
        const TopStyle Hoodie = TopStyle.Hoodie, Jacket = TopStyle.Jacket, Tee = TopStyle.Tee, Bomber = TopStyle.Bomber, Puffer = TopStyle.Puffer, Track = TopStyle.Tracksuit;

        /// <summary>Sorted by price (= unlock order). Existing save ids are kept for the first five original artists.</summary>
        public static readonly ArtistSpec[] Roster =
        {
            // ---- Basement: nobodies with something to prove
            Art("korv", "KORV", Under, 0, 15_000, 1.06, 2_100, 1, "Raps in the stairwell of his building. Doesn't do interviews.", "Dusty loops, hard kicks, no hi-hats",
                Mk(2, Messy, "120D0B", "2E2420", Hoodie, "2A2A33", "8A84B0", "focus", "beanie=3A3A44,stubble,stud,silver")),
            Art("lil_vexi", "lil vexi", Rage, 0, 22_000, 1.06, 4_800, 1, "Sixteen, chaotic, already has forty unreleased songs.", "Distorted leads, fast 808 rolls",
                Mk(7, Messy, "16121A", "FF2A3D", Tee, "141418", "FF2A3D", "excited", "dots,chain,silver")),
            Art("zyven", "ZYVEN", Trap, 0, 30_000, 1.07, 7_500, 1, "Quiet in person, menacing on the mic.", "Dark bells, sliding 808s",
                Mk(1, Tied, "100B09", "B65CFF", Hoodie, "2A1A44", "B65CFF", "mean", "chain,stud,silver,stubble")),
            Art("baby_ruin", "Baby Ruin", Melodic, 0, 42_000, 1.07, 11_000, 2, "Sings about heartbreak over every beat you send him.", "Guitar loops, soft pads, bouncy 808",
                Mk(4, Curly, "1A1210", "3A2A22", Bomber, "E8B3D6", "FFFFFF", "smile", "hoop,chain")),
            Art("k4iro", "K4IRO", Exp, 0, 60_000, 1.08, 15_000, 2, "Builds songs from field recordings and broken keyboards.", "Glitchy drums, pitched vocal chops",
                Mk(5, Bob, "3DE1FF", "B65CFF", Bomber, "1E1B2A", "3DE1FF", "proud", "glasses,septum,silver")),

            // ---- Bedroom: the internet starts talking
            Art("nox", "NØX", Rage, 1, 150_000, 1.08, 38_000, 2, "Wears all black. Every show turns into a mosh pit.", "Blown-out 808s, siren synths",
                Mk(0, Locs, "0E0A09", "2A201C", Tee, "141418", "FF2A3D", "mean", "cap=16161C,cuban,silver,dots,neck,slit")),
            Art("young_vonn", "Young Vonn", Trap, 1, 240_000, 1.08, 72_000, 2, "Grew up on his uncle's mixtapes. Flow like a metronome.", "Rolling hats, big 808, triplet bounce",
                Mk(3, Curly, "17110F", "3A2C27", Track, "1E2A55", "F4F4F6", "proud", "cuban,stud,stubble")),
            Art("vexxo", "VEXXO", Rage, 1, 400_000, 1.09, 140_000, 3, "Screams half his ad-libs. Fans copy his haircut.", "Hyper synths at 160 BPM",
                Mk(6, Long, "141418", "FF2A3D", Puffer, "141418", "FF2A3D", "mean", "shades,hoop,silver,dots")),
            Art("solei", "SOLEI", RnB, 1, 650_000, 1.09, 210_000, 3, "Late-night voice notes turned into a career.", "Warm Rhodes, slow swing, live bass",
                Mk(2, Braids, "B07840", "F2C14E", Jacket, "F1E4D3", "FFB38A", "smile", "lashes,lips,hoop,nose")),
            Art("rising", "GRIMLINE", Drill, 1, 1_000_000, 1.10, 320_000, 3, "Cold delivery, sliding 808s, zero small talk. Records in one take.", "Sliding 808 basslines, sparse percussion",
                Mk(3, Messy, "120D0B", "2E2420", Puffer, "16161C", "F4F4F6", "mean", "beanie=16161C,cuban,silver,dots,slit,stubble,square")),

            // ---- Professional: real fans, real money
            Art("unknown_rapper", "KID OBSIDIAN", Under, 2, 8_000_000, 1.10, 610_000, 3, "Writes at 4 AM in a parked car. Few words, every one of them sharp.", "Raw samples, heavy kicks",
                Mk(1, Braids, "0F0B0A", "2A201C", Hoodie, "1B1A22", "8A84B0", "focus", "phones,chain,silver,dots,neck,stud,stubble,lips")),
            Art("rovan", "RØVAN", Melodic, 2, 15_000_000, 1.10, 900_000, 3, "Auto-tune is his instrument. Every hook sounds like a hit.", "Ambient guitars, bouncy 808s",
                Mk(5, Half, "17110F", "FF8AD8", Puffer, "8FB8FF", "FFFFFF", "idle", "chain,stud")),
            Art("kash_v", "KASH V", Trap, 2, 28_000_000, 1.11, 1_400_000, 4, "Talks money, saves money, buys studio gear.", "Hard trap with orchestral hits",
                Mk(1, Braids, "0F0B0A", "2A201C", Bomber, "1F5E3B", "F2C14E", "proud", "cuban,beard,shades")),
            Art("lil_raze", "Lil Raze", Rage, 2, 50_000_000, 1.11, 2_200_000, 4, "Jumps off every speaker stack he can find.", "Distorted bass, video-game synths",
                Mk(7, Tied, "120E10", "FF2A3D", Tee, "141418", "FF2A3D", "excited", "dots,slit,cuban,silver")),
            Art("drayo_k", "DRAYO K", Drill, 2, 90_000_000, 1.12, 3_100_000, 4, "Short verses, cold stare, sold-out shows in four cities.", "Gritty drill slides, off-beat snares",
                Mk(1, Messy, "0F0B0A", "2A201C", Puffer, "0E0E12", "7FA7C9", "mean", "beanie=0E0E12,chain,silver,beard")),

            // ---- Recording complex: radio-ready
            Art("underground", "VELVET JUNO", Melodic, 3, 1_000_000_000, 1.12, 4_800_000, 4, "Turns heartbreak into hooks. Hums melodies into voice memos all day.", "Airy pads, melodic 808s",
                Mk(6, Long, "F08BB8", "FFD3A8", Puffer, "C9A8FF", "FF6FB5", "smile", "lashes,lips,hoop,chain,star")),
            Art("xavo", "XAVØ", Rage, 3, 2_500_000_000, 1.12, 6_200_000, 4, "Masked stage outfits, unmasked lyrics.", "Industrial rage, metallic hits",
                Mk(3, Twists, "141010", "D9DEE8", Puffer, "C9CCD6", "16161C", "mean", "shades,cuban,silver,neck,dots")),
            Art("famous", "PRISM ODDITY", Exp, 3, 5_000_000_000, 1.13, 7_900_000, 4, "Builds songs out of glitches and broken synths. Nobody sounds like this.", "Hyperpop glitches, detuned synths",
                Mk(5, Bob, "3DE1FF", "4DFFA8", Bomber, "2A2A33", "4DFFA8", "excited", "lashes,glasses,septum,star,hoop,silver")),
            Art("kayvon_blu", "Kayvon Blu", Melodic, 3, 10_000_000_000, 1.13, 9_500_000, 4, "Sings like he's on the phone at 3 AM.", "Plucked synths, airy pads",
                Mk(2, Long, "141A2E", "3D7BFF", Hoodie, "7FB2FF", "FFFFFF", "smile", "chain,hoop,silver")),
            Art("zayro", "ZAYRO", HipHop, 3, 20_000_000_000, 1.14, 12_000_000, 5, "Lyricist first. Shouts out the producer in every song.", "Soul samples, punchy drums",
                Mk(4, Curly, "1A1210", "3A2A22", Bomber, "5A3A22", "F2C14E", "proud", "glasses,chain,beard")),

            // ---- Music company: household names
            Art("vanta", "VANTA", Rage, 4, 150_000_000_000, 1.15, 18_000_000, 5, "Strobes on, lights off. Nobody has seen his face clearly.", "Wall-of-noise synths, double-time hats",
                Mk(0, Half, "0E0A09", "FF2A3D", Puffer, "0E0E12", "FF2A3D", "mean", "shades,cuban,silver,dots,neck")),
            Art("riven", "riven", Melodic, 4, 400_000_000_000, 1.15, 22_000_000, 5, "Soft voice, enormous fanbase.", "Lo-fi guitars, gentle 808s",
                Mk(8, Messy, "D9DEE8", "B48CFF", Hoodie, "B48CFF", "FFFFFF", "idle", "stud,star,silver")),
            Art("okko", "OKKO", Drill, 4, 1_000_000_000_000, 1.15, 28_000_000, 5, "Runs his own label at twenty-two.", "Dark piano drill, heavy slides",
                Mk(2, Braids, "0F0B0A", "2A201C", Track, "16161C", "7FA7C9", "mean", "cuban,silver,beard")),
            Art("hexxa", "HEXXA", Exp, 4, 2_500_000_000_000, 1.15, 31_000_000, 5, "Half her songs are ninety seconds long. All of them chart.", "Hyperpop bleeps, pitched-up vocals",
                Mk(6, Bob, "FF3DC8", "3DE1FF", Puffer, "4DFFA8", "FF3DC8", "excited", "lashes,septum,hoop,star,silver")),

            // ---- Record label: superstars
            Art("superstar", "AURELIA NOVA", HipHop, 5, 20_000_000_000_000, 1.18, 48_000_000, 5, "Charts, campaigns, sold-out shows. Expensive — and worth it.", "Big pop-rap drums, gospel chords",
                Mk(3, Braids, "C98E4A", "F2C14E", Puffer, "F4F1FA", "F2C14E", "smile", "lashes,lips,hoop,chain,nose")),
            Art("vante", "VANTÉ", RnB, 5, 60_000_000_000_000, 1.18, 55_000_000, 5, "A falsetto that made three producers cry.", "Silky chords, slow trap drums",
                Mk(3, Tied, "17110F", "F2C14E", Jacket, "F4F1FA", "F2C14E", "smile", "chain,hoop,stubble")),
            Art("xeno", "XENØ", Rage, 5, 200_000_000_000_000, 1.18, 63_000_000, 5, "Arena rage. Pyro on every drop.", "Stadium rage, detuned leads",
                Mk(5, Locs, "120D0B", "2E2420", Tee, "141418", "FF2A3D", "mean", "cap=FF2A3D,cuban,dots,slit,neck")),
            Art("lil_kavo", "Lil Kavo", Trap, 5, 600_000_000_000_000, 1.18, 70_000_000, 5, "Started on a free upload page, now on billboards.", "Bouncy trap, playful flutes",
                Mk(1, Twists, "100B09", "B65CFF", Bomber, "6C3BD9", "FF3DC8", "excited", "cuban,stud")),

            // ---- Music empire: legends
            Art("ravenxo", "RAVENXO", Rage, 6, 5_000_000_000_000_000, 1.20, 88_000_000, 5, "Gothic rage icon: black nails, silver rings, world tour.", "Dark rage with church organ samples",
                Mk(8, Long, "0E0A10", "2A1A30", Puffer, "0E0E12", "D9DEE8", "mean", "lashes,cuban,silver,dots,hoop")),
            Art("kaivn", "KAIVN", Melodic, 6, 15_000_000_000_000_000, 1.20, 95_000_000, 5, "Every feature is an event.", "Ethereal pads, slow-motion 808s",
                Mk(2, Half, "17110F", "4DFFA8", Puffer, "F4F1FA", "4DFFA8", "proud", "cuban,stud")),
            Art("zora", "ZØRA", RnB, 6, 40_000_000_000_000_000, 1.20, 110_000_000, 5, "Voice of the decade, according to everyone.", "Neo-soul keys, live bass",
                Mk(0, Braids, "0F0B0A", "F2C14E", Bomber, "16161C", "F2C14E", "smile", "lashes,lips,hoop,nose,chain")),
            Art("oblvn", "OBLVN", Exp, 6, 100_000_000_000_000_000, 1.20, 130_000_000, 5, "Nobody knows what comes next. That's the point.", "Granular textures, broken rhythms",
                Mk(5, Messy, "D9DEE8", "3DE1FF", Bomber, "1E1B2A", "3DE1FF", "idle", "shades,septum,star,silver")),
        };
    }
}
