using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    public enum FanState : byte { Idle, HandsUp, Jump, Dance, Sing, Phone, Clap, Hype, Point, Reach }

    /// <summary>
    /// A living concert crowd. Every fan is a different person: skin tone, hair style and color, clothes, height, cap or
    /// beanie, phone. Fans pick their own moves (idle, hands up, jumping, dancing, singing, phone, clapping, hype,
    /// pointing, reaching) based on the music's intensity and their own excitement, and react to waves started by the
    /// artist. Size scales with popularity: ~60 fans for an unknown artist up to ~3,500 for a superstar.
    /// Rendering: three CrowdLayer meshes (front row · far LOD blobs · detailed fans), no per-fan GameObjects.
    /// </summary>
    public class ConcertCrowd : MonoBehaviour
    {
        struct Fan
        {
            public Vector2 pos;
            public float s;           // depth scale
            public bool detailed, front, staff;
            public byte body, hair;   // hair 0..9, 10 cap, 11 beanie, 12 side head
            public bool flip, longSleeve, phone;
            public Color32 skin, cloth, hairC, sleeve;
            public float phase, speed, excite, height;
            public FanState state;
            public float stateUntil;
            public float waveAt;
            public FanState waveState;
            public int sign;
        }

        static readonly Color[] Skins =
        {
            Hex("3E2618"), Hex("4A2E1F"), Hex("6B4430"), Hex("8D5A3B"), Hex("B57A52"), Hex("C68E64"), Hex("E0B48C"), Hex("F1C9A3"), Hex("F6DCCB"),
        };
        static readonly Color[] Clothes =
        {
            Hex("16151C"), Hex("2A2834"), Hex("F2F0F5"), Hex("6C3BD9"), Hex("FF3DC8"), Hex("2EA56F"), Hex("2E3A52"), Hex("C2183A"), Hex("FF8A3D"),
            Hex("3DE1FF"), Hex("F2C14E"), Hex("4A4F2E"), Hex("8A84B0"), Hex("B48CFF"), Hex("E8E2D6"), Hex("1E8C83"), Hex("5A2E1A"), Hex("9B9BA6"),
        };
        static readonly Color[] Hairs =
        {
            Hex("120D0B"), Hex("120D0B"), Hex("231813"), Hex("3A2416"), Hex("6B4A2A"), Hex("D8B060"), Hex("B33A2A"), Hex("FF6FB5"), Hex("6C8CFF"),
            Hex("B48CFF"), Hex("E8E8EE"), Hex("2EC4B6"),
        };

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        CrowdLayer frontLayer, farLayer, nearLayer;
        Fan[] fans = new Fan[0];
        int frontEnd, farEnd;      // fans[0..frontEnd) front row, [frontEnd..farEnd) far blobs, [farEnd..] detailed (all sorted far → near)
        readonly List<(RectTransform rt, TextMeshProUGUI text, Image card)> signs = new List<(RectTransform, TextMeshProUGUI, Image)>();
        System.Random rnd = new System.Random();
        GenreStage look;
        Color ambient = new Color(0.3f, 0.2f, 0.5f);
        float yMax = 460, time, bpm = 120, frameToggle;
        int camFlashCount;
        Vector2[] camFlashes = new Vector2[0];
        float[] camFlashPhase = new float[0];
        bool barrier;

        /// <summary>Music intensity 0..1 set by the director (normal → build-up → drop → chorus → big moment).</summary>
        public float Intensity { get; set; } = 0.3f;
        /// <summary>Overall energy multiplier (popularity, attendance).</summary>
        public float EnergyScale { get; private set; } = 1f;
        public int Count => fans.Length;
        /// <summary>Where the artist stands (design x) — front row reaches toward it.</summary>
        public float ArtistX { get; set; } = 400;
        public bool ArtistAtEdge { get; set; }

        public static ConcertCrowd Create(RectTransform parent, Texture atlas, UISkin skin)
        {
            var root = UIKit.Rect("Crowd", parent);
            UIKit.Stretch(root);
            var crowd = root.gameObject.AddComponent<ConcertCrowd>();
            crowd.frontLayer = crowd.Layer("FrontRow", root, atlas);
            for (int i = 0; i < 6; i++)
            {
                var card = UIKit.Image("Sign" + i, root, Color.white, skin.rounded, true);
                var text = UIKit.Text("Text", card.rectTransform, "", 40, Color.black, skin.display, TextAlignmentOptions.Center, 1);
                UIKit.Stretch(text.rectTransform, 6, 6, 4, 4);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = Vector2.zero;
                card.gameObject.SetActive(false);
                crowd.signs.Add((card.rectTransform, text, card));
            }
            crowd.farLayer = crowd.Layer("FarCrowd", root, atlas);
            crowd.nearLayer = crowd.Layer("NearCrowd", root, atlas);
            return crowd;
        }

        CrowdLayer Layer(string name, RectTransform parent, Texture atlas)
        {
            var rt = UIKit.Rect(name, parent);
            UIKit.Stretch(rt);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var l = rt.gameObject.AddComponent<CrowdLayer>();
            l.texture = atlas;
            l.raycastTarget = false;
            return l;
        }

        // ------------------------------------------------------------------ setup

        /// <summary>How many fans to show: grows steeply with popularity, shrinks with an empty house, capped per venue.</summary>
        public static int VisibleFans(VenueTier tier, float popularity, float fill)
        {
            float p = Mathf.Clamp01(popularity / 100f);
            float n = 50 + 3500 * Mathf.Pow(p, 2.6f);
            n *= 0.45f + 0.55f * Mathf.Clamp01(fill);
            int[] cap = { 260, 600, 1500, 3000, 3600 };
            int[] min = { 30, 120, 300, 900, 1800 };
            return Mathf.Clamp(Mathf.RoundToInt(n), min[(int)tier], cap[(int)tier]);
        }

        public void Setup(VenueTier tier, float popularity, float fill, ArtistData artist, int seed, int quality = 2)
        {
            rnd = new System.Random(seed);
            look = GenreStage.For(artist != null ? artist.genre : ArtistGenre.HipHop);
            bpm = look.bpm;
            ambient = Color.Lerp(look.palette[0], new Color(0.2f, 0.18f, 0.35f), 0.6f);
            EnergyScale = Mathf.Lerp(0.55f, 1.05f, Mathf.Clamp01(popularity / 90f)) * Mathf.Lerp(0.7f, 1f, fill);
            float[] baseScale = { 1.25f, 1.12f, 1.0f, 0.88f, 0.78f };
            float[] top = { 470, 470, 462, 455, 445 };
            yMax = top[(int)tier];
            float bs = baseScale[(int)tier];
            int total = Mathf.RoundToInt(VisibleFans(tier, popularity, fill) * (quality == 0 ? 0.55f : quality == 1 ? 0.8f : 1f));
            int detailCap = quality == 0 ? 200 : quality == 1 ? 320 : 420;
            barrier = popularity >= 40;

            var list = new List<Fan>(total + 16);
            // front row at the barrier: packed along the stage, the most excited fans, signs
            int frontCount = Mathf.Clamp(total / 18, 6, 34);
            if (total < 80) frontCount = Mathf.Min(frontCount, total / 3);
            for (int i = 0; i < frontCount; i++)
            {
                float x = Mathf.Lerp(150, 650, (i + 0.5f) / frontCount) + R(-10, 10);
                var f = NewFan(new Vector2(x, yMax - R(0, 14)), bs * 0.5f, true);
                f.front = true;
                f.excite = R(0.7f, 1f);
                f.sign = -1;
                list.Add(f);
            }
            // signs held by front-row fans (artist-specific texts)
            int signCount = Mathf.Min(signs.Count, Mathf.Clamp(frontCount / 4, popularity >= 20 ? 2 : 1, signs.Count));
            // fan signs: stage name + localized chants (the tour phrase from the artist look stays stylized)
            string stage = artist != null ? artist.displayName.ToUpperInvariant() : "♥";
            var src = artist != null && artist.visual != null ? artist.visual.signs : null;
            var texts = new[] { stage, Loc.T("crowd.sign_love"), Loc.F("crowd.sign_4life", "name", stage),
                src != null && src.Length > 3 ? src[3] : stage, Loc.T("crowd.sign_fan") };
            for (int i = 0; i < signs.Count; i++)
            {
                bool on = i < signCount && frontCount > 0;
                signs[i].rt.gameObject.SetActive(on);
                if (!on) continue;
                int fi = Mathf.Clamp((int)((i + 0.5f) / signCount * frontCount), 0, frontCount - 1);
                var f = list[fi]; f.sign = i; list[fi] = f;
                signs[i].text.text = texts[i % texts.Length];
                var c = i % 3 == 0 ? Color.white : i % 3 == 1 ? look.palette[0] : new Color(1f, 0.85f, 0.3f);
                signs[i].card.color = c;
                signs[i].text.color = c.grayscale > 0.6f ? new Color(0.08f, 0.06f, 0.12f) : Color.white;
            }
            // security staff in front of the barrier for bigger names
            if (popularity >= 50)
            {
                int staff = popularity >= 80 ? 4 : 2;
                for (int i = 0; i < staff; i++)
                {
                    var f = NewFan(new Vector2(Mathf.Lerp(190, 610, staff == 1 ? 0.5f : i / (staff - 1f)), yMax + 18), bs * 0.5f, true);
                    f.staff = true; f.cloth = new Color32(14, 13, 18, 255); f.sleeve = f.cloth; f.longSleeve = false; f.hair = 12; f.phone = false; f.sign = -1;
                    list.Add(f);
                }
            }
            frontEnd = list.Count;

            // everyone else: groups of friends, denser toward the stage and the center, gaps when the house isn't full
            int rest = total - frontCount;
            var others = new List<Fan>(rest);
            while (others.Count < rest)
            {
                float u = (float)rnd.NextDouble();
                float y = (yMax - 26) * (1f - Mathf.Pow(u, 1.25f));
                float x = Mathf.Clamp(400 + Gauss() * 230f, 10, 790);
                int group = 1 + rnd.Next(4);
                for (int g = 0; g < group && others.Count < rest; g++)
                {
                    var p = new Vector2(x + R(-18, 18) * (1 + g * 0.4f), Mathf.Clamp(y + R(-10, 10), 0, yMax - 26));
                    float s = bs * Mathf.Lerp(1.2f, 0.45f, p.y / yMax) * R(0.9f, 1.08f);
                    var f = NewFan(p, s, s > 0.52f);
                    f.phase = g == 0 ? f.phase : others[others.Count - 1].phase + R(-0.1f, 0.1f); // friends move together
                    others.Add(f);
                }
            }
            // detailed fans are capped (performance); the smallest become LOD blobs
            others.Sort((a, b) => b.s.CompareTo(a.s));
            for (int i = 0; i < others.Count; i++)
            {
                var f = others[i];
                f.detailed = f.detailed && i < detailCap;
                others[i] = f;
            }
            var far = others.FindAll(f => !f.detailed);
            var near = others.FindAll(f => f.detailed);
            far.Sort((a, b) => b.pos.y.CompareTo(a.pos.y));
            near.Sort((a, b) => b.pos.y.CompareTo(a.pos.y));
            list.Sort(0, frontEnd, Comparer<Fan>.Create((a, b) => b.pos.y.CompareTo(a.pos.y)));
            list.AddRange(far);
            farEnd = list.Count;
            list.AddRange(near);
            fans = list.ToArray();

            // photographers' camera flashes in the pit
            camFlashCount = popularity >= 50 ? (popularity >= 80 ? 10 : 5) : 0;
            camFlashes = new Vector2[camFlashCount];
            camFlashPhase = new float[camFlashCount];
            for (int i = 0; i < camFlashCount; i++)
            {
                camFlashes[i] = new Vector2(R(80, 720), R(yMax - 60, yMax + 10));
                camFlashPhase[i] = R(0, 10);
            }
            time = 0;
        }

        Fan NewFan(Vector2 pos, float s, bool detailed)
        {
            var f = new Fan
            {
                pos = pos, s = s, detailed = detailed,
                body = (byte)rnd.Next(CrowdAtlas.BodyCount),
                flip = rnd.NextDouble() < 0.5,
                skin = Skins[rnd.Next(Skins.Length)],
                cloth = Clothes[rnd.Next(Clothes.Length)],
                hairC = Hairs[rnd.Next(Hairs.Length)],
                phase = R(0, 1), speed = R(0.7f, 1.4f), excite = R(0.15f, 1f), height = R(0.92f, 1.1f),
                phone = rnd.NextDouble() < 0.18, sign = -1, waveAt = -1,
            };
            double h = rnd.NextDouble();
            f.hair = (byte)(h < 0.1 ? 10 : h < 0.16 ? 11 : h < 0.28 ? 12 : rnd.Next(CrowdAtlas.HairCount));
            f.longSleeve = f.body == 0 || f.body == 2 || f.body == 4 || rnd.NextDouble() < 0.3;
            f.sleeve = f.longSleeve ? f.cloth : f.skin;
            if (f.hair == 10 || f.hair == 11) f.hairC = Clothes[rnd.Next(Clothes.Length)];
            f.state = FanState.Idle;
            f.stateUntil = R(0, 2);
            return f;
        }

        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        float Gauss() => (float)((rnd.NextDouble() + rnd.NextDouble() + rnd.NextDouble()) / 1.5 - 1.0);

        // ------------------------------------------------------------------ reactions

        /// <summary>A reaction spreading from a point (the artist raises a hand → front reacts → spreads through the crowd).</summary>
        public void Wave(Vector2 origin, FanState state, float speed = 420f, float duration = 1.8f)
        {
            for (int i = 0; i < fans.Length; i++)
            {
                if (fans[i].staff) continue;
                float d = Vector2.Distance(fans[i].pos, origin);
                fans[i].waveAt = time + d / speed + R(0f, 0.25f);
                fans[i].waveState = state;
            }
            waveDuration = duration;
        }

        float waveDuration = 1.8f;

        /// <summary>The artist points at a side: fans there go wild.</summary>
        public void PointAt(float x)
        {
            for (int i = 0; i < fans.Length; i++)
                if (!fans[i].staff && Mathf.Abs(fans[i].pos.x - x) < 200)
                {
                    fans[i].state = FanState.Hype;
                    fans[i].stateUntil = time + R(1.2f, 2.2f);
                }
        }

        /// <summary>Big moment: everyone reacts at once (with tiny offsets).</summary>
        public void Surge(FanState state = FanState.Jump)
        {
            for (int i = 0; i < fans.Length; i++)
            {
                if (fans[i].staff) continue;
                fans[i].state = state;
                fans[i].stateUntil = time + R(1.5f, 2.6f);
                fans[i].phase = R(0f, 0.15f);
            }
        }

        FanState Choose(ref Fan f)
        {
            float e = Mathf.Clamp01(Intensity * EnergyScale * (0.55f + 0.9f * f.excite));
            if (f.front && ArtistAtEdge && Mathf.Abs(f.pos.x - ArtistX) < 260) return FanState.Reach;
            double r = rnd.NextDouble();
            if (e < 0.35f)
            {
                if (r < 0.45) return FanState.Idle;
                if (r < 0.6) return FanState.Sing;
                if (r < 0.7) return FanState.Clap;
                if (r < 0.82) return f.phone ? FanState.Phone : FanState.Dance;
                return FanState.Dance;
            }
            if (e < 0.68f)
            {
                if (r < 0.25) return FanState.HandsUp;
                if (r < 0.45) return FanState.Dance;
                if (r < 0.58) return FanState.Clap;
                if (r < 0.7) return FanState.Sing;
                if (r < 0.8) return f.phone ? FanState.Phone : FanState.HandsUp;
                if (r < 0.92) return FanState.Jump;
                return FanState.Point;
            }
            if (r < 0.36) return FanState.Jump;
            if (r < 0.6) return FanState.Hype;
            if (r < 0.84) return FanState.HandsUp;
            if (r < 0.94) return f.phone ? FanState.Phone : FanState.Jump;
            return FanState.Dance;
        }

        // ------------------------------------------------------------------ simulation + rendering

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            time += dt;
            float beat = time * bpm / 60f;
            float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 2f);
            float light = 0.82f + 0.18f * pulse * Intensity;

            for (int i = 0; i < fans.Length; i++)
            {
                ref var f = ref fans[i];
                if (f.staff) continue;
                if (f.waveAt >= 0 && time >= f.waveAt)
                {
                    f.state = f.waveState;
                    f.stateUntil = time + waveDuration + R(0, 0.4f);
                    f.waveAt = -1;
                }
                else if (time >= f.stateUntil)
                {
                    f.state = Choose(ref f);
                    f.stateUntil = time + R(1.4f, 3.6f) / f.speed;
                }
            }

            // far blobs are cheap but many: refresh them every other frame
            frameToggle = 1 - frameToggle;
            if (frameToggle > 0.5f || fans.Length - farEnd < 50) DrawRange(farLayer, frontEnd, farEnd, beat, light, false);
            DrawRange(frontLayer, 0, frontEnd, beat, light, true);
            DrawRange(nearLayer, farEnd, fans.Length, beat, light, false);
        }

        void DrawRange(CrowdLayer layer, int from, int to, float beat, float light, bool front)
        {
            layer.Begin();
            if (front && barrier)
            {
                // barrier + camera flashes in the photo pit
                var bc = Tint(new Color(0.55f, 0.55f, 0.62f), 0.8f * light, 0.5f);
                for (float x = 110; x < 690; x += 40) layer.Quad(new Vector2(x, yMax + 2), new Vector2(42, 42), CrowdAtlas.UV(CrowdAtlas.BarrierCol, CrowdAtlas.PropRow), bc);
            }
            for (int i = from; i < to; i++) DrawFan(layer, ref fans[i], beat, light);
            if (front)
                for (int i = 0; i < camFlashCount; i++)
                {
                    float k = Mathf.Repeat(time * 0.7f + camFlashPhase[i], 3.2f);
                    if (k > 0.12f) continue;
                    float a = 1f - k / 0.12f;
                    layer.Quad(camFlashes[i] - new Vector2(22, 22), new Vector2(44, 44), CrowdAtlas.UV(CrowdAtlas.FlashCol, CrowdAtlas.PropRow), new Color(1, 1, 1, a));
                }
            layer.Commit();
            if (front) PlaceSigns(beat);
        }

        Color32 Tint(Color c, float bright, float fog)
        {
            var col = Color.Lerp(c * bright, ambient * 0.6f, fog);
            col.a = 1f;
            return col;
        }

        static Vector2 ArmDir(float deg, bool left)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(left ? -Mathf.Sin(r) : Mathf.Sin(r), -Mathf.Cos(r));
        }

        void Pose(ref Fan f, float beat, out float jump, out float sway, out float aL, out float aR)
        {
            float b = beat * f.speed * 0.5f + f.phase;
            float hop = Mathf.Abs(Mathf.Sin(Mathf.PI * (beat + f.phase)));
            float t = time * f.speed + f.phase * 6.28f;
            jump = 0; sway = Mathf.Sin(t * 0.8f) * 1.2f;
            aL = 8; aR = 8;
            switch (f.state)
            {
                case FanState.Idle: jump = 0.8f * Mathf.Abs(Mathf.Sin(t)); aL = 6 + 3 * Mathf.Sin(t); aR = 6; break;
                case FanState.HandsUp: jump = 2f * hop; aL = 162 + 12 * Mathf.Sin(t * 2); aR = 162 + 12 * Mathf.Sin(t * 2 + 1.3f); break;
                case FanState.Jump: jump = 11f * hop; aL = 140 + 25 * hop; aR = 135 + 25 * hop; break;
                case FanState.Dance: sway = 5f * Mathf.Sin(Mathf.PI * b); jump = 2f * hop; aL = 90 + 55 * Mathf.Sin(Mathf.PI * b); aR = 90 - 55 * Mathf.Sin(Mathf.PI * b); break;
                case FanState.Sing: jump = 1.5f * hop; aL = 12; aR = 115 + 25 * Mathf.Sin(t * 1.5f); sway = 2.5f * Mathf.Sin(t); break;
                case FanState.Phone: jump = 0.5f * hop; aL = 10; aR = 172; break;
                case FanState.Clap: { float c = Mathf.Abs(Mathf.Sin(Mathf.PI * beat * 2f)); aL = 158 + 14 * c; aR = 158 + 14 * c; jump = 1.5f * hop; break; }
                case FanState.Hype: jump = 16f * hop; aL = 150 + 28 * Mathf.Sin(Mathf.PI * beat * 2f); aR = 150 + 28 * Mathf.Sin(Mathf.PI * beat * 2f + 1f); break;
                case FanState.Point: jump = 2f * hop; aL = 10; aR = f.pos.x < ArtistX ? 125 : 145; break;
                case FanState.Reach: jump = 4f * hop; aL = 150 + 8 * Mathf.Sin(t * 3); aR = 150 + 8 * Mathf.Sin(t * 3 + 2); break;
            }
            jump *= 0.55f + 0.6f * Intensity;
        }

        void DrawFan(CrowdLayer layer, ref Fan f, float beat, float light)
        {
            float depth = f.pos.y / yMax;
            float fog = Mathf.Lerp(0.05f, 0.55f, depth);
            float bright = light * Mathf.Lerp(1f, 0.75f, depth);
            Pose(ref f, beat, out float jump, out float sway, out float aL, out float aR);
            float s = f.s;

            if (!f.detailed)
            {
                // LOD: one quad (head + shoulders), hands-up variant when energetic
                bool up = f.state == FanState.HandsUp || f.state == FanState.Hype || f.state == FanState.Jump || f.state == FanState.Reach;
                int col = up ? CrowdAtlas.FarHandsCol : (f.body % 3 == 0 ? CrowdAtlas.FarWideCol : CrowdAtlas.FarCol);
                var c = Tint(Color.Lerp(f.cloth, f.hairC, 0.3f), bright * 0.85f, fog);
                float size = 64 * s * f.height;
                layer.Quad(new Vector2(f.pos.x - size * 0.5f + sway * s, f.pos.y + jump * s), new Vector2(size, size), CrowdAtlas.UV(col, CrowdAtlas.PropRow), c, f.flip);
                if (f.phone && (f.state == FanState.Phone || phoneFlash > 0f))
                {
                    float a = (f.state == FanState.Phone ? 0.5f : phoneFlash * 0.6f);
                    layer.Quad(new Vector2(f.pos.x - 7 * s, f.pos.y + (54 + jump) * s), new Vector2(14 * s, 14 * s), CrowdAtlas.UV(CrowdAtlas.GlowCol, CrowdAtlas.PropRow), new Color(0.9f, 0.95f, 1f, a));
                }
                return;
            }

            float h = f.height;
            var basePos = new Vector2(f.pos.x + sway * s, f.pos.y + jump * s);
            var bodyC = Tint(f.cloth, bright, fog);
            var skinC = Tint(f.skin, bright, fog);
            var hairC = Tint(f.hairC, bright, fog);
            var sleeveC = Tint(f.sleeve, bright, fog);
            float cell = 64 * s;
            // arms first (behind the torso when down; raised arms read fine either way)
            var shoulderL = basePos + new Vector2(-(32 - CrowdAtlas.ShoulderX) * s, CrowdAtlas.ShoulderY * s * h);
            var shoulderR = basePos + new Vector2((32 - CrowdAtlas.ShoulderX) * s, CrowdAtlas.ShoulderY * s * h);
            var armUV = CrowdAtlas.UV(CrowdAtlas.ArmCol, CrowdAtlas.PropRow);
            var sleeveUV = CrowdAtlas.UV(CrowdAtlas.SleeveCol, CrowdAtlas.PropRow);
            float armLen = cell * 0.82f, armW = cell * 0.9f;
            var dL = ArmDir(aL, true);
            var dR = ArmDir(aR, false);
            if (f.staff) { dL = ArmDir(25, true); dR = ArmDir(25, false); }
            bool lUp = aL > 60, rUp = aR > 60;
            if (!lUp) { layer.Limb(shoulderL, dL, armW, armLen, armUV, skinC); if (f.longSleeve) layer.Limb(shoulderL, dL, armW, armLen * 1.1f, sleeveUV, sleeveC); }
            if (!rUp) { layer.Limb(shoulderR, dR, armW, armLen, armUV, skinC); if (f.longSleeve) layer.Limb(shoulderR, dR, armW, armLen * 1.1f, sleeveUV, sleeveC); }

            layer.Quad(new Vector2(basePos.x - cell * 0.5f, basePos.y), new Vector2(cell, cell * h), CrowdAtlas.UV(f.body, CrowdAtlas.BodyRow), bodyC, f.flip);
            var headBL = new Vector2(basePos.x - cell * 0.5f, basePos.y + 42 * s * h);
            if (f.hair == 12)
                layer.Quad(headBL, new Vector2(cell, cell), CrowdAtlas.UV(CrowdAtlas.HeadSideCol, CrowdAtlas.HairRow), skinC, f.flip);
            else
            {
                layer.Quad(headBL, new Vector2(cell, cell), CrowdAtlas.UV(CrowdAtlas.HeadBackCol, CrowdAtlas.HairRow), skinC, f.flip);
                int hairCol = f.hair == 10 ? CrowdAtlas.CapCol : f.hair == 11 ? CrowdAtlas.BeanieCol : f.hair;
                layer.Quad(headBL, new Vector2(cell, cell), CrowdAtlas.UV(hairCol, CrowdAtlas.HairRow), hairC, f.flip);
            }

            if (lUp) { layer.Limb(shoulderL, dL, armW, armLen, armUV, skinC); if (f.longSleeve) layer.Limb(shoulderL, dL, armW, armLen * 1.1f, sleeveUV, sleeveC); }
            if (rUp) { layer.Limb(shoulderR, dR, armW, armLen, armUV, skinC); if (f.longSleeve) layer.Limb(shoulderR, dR, armW, armLen * 1.1f, sleeveUV, sleeveC); }

            // phone held up (recording the show): the screen glows, flashes during big moments
            bool holding = f.phone && (f.state == FanState.Phone || (phoneFlash > 0f && rUp));
            if (holding)
            {
                var hand = shoulderR + dR * armLen * 0.95f;
                float ps = cell * 0.55f;
                layer.Quad(hand - new Vector2(ps * 0.5f, ps * 0.35f), new Vector2(ps, ps), CrowdAtlas.UV(CrowdAtlas.PhoneCol, CrowdAtlas.PropRow), new Color(0.85f, 0.92f, 1f, 1f));
                float glow = Mathf.Max(0.25f, phoneFlash);
                layer.Quad(hand - new Vector2(ps, ps * 0.8f), new Vector2(ps * 2f, ps * 2f), CrowdAtlas.UV(CrowdAtlas.GlowCol, CrowdAtlas.PropRow), new Color(0.85f, 0.92f, 1f, 0.35f * glow));
            }
        }

        // ---- phone flash wave
        float phoneFlash;

        /// <summary>A wave of phone lights across the crowd (big moments).</summary>
        public void PhoneLights(float seconds)
        {
            phoneFlashUntil = time + seconds;
            for (int i = 0; i < fans.Length; i++) if (rnd.NextDouble() < 0.12) fans[i].phone = true;
        }

        float phoneFlashUntil;

        void LateUpdate()
        {
            float target = time < phoneFlashUntil ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(time * 3f)) : 0f;
            phoneFlash = Mathf.MoveTowards(phoneFlash, target, Time.unscaledDeltaTime * 2f);
        }

        void PlaceSigns(float beat)
        {
            var layerRt = frontLayer.rectTransform.rect;
            float kx = layerRt.width / 800f, ky = layerRt.height / 1400f;
            for (int i = 0; i < frontEnd; i++)
            {
                ref var f = ref fans[i];
                if (f.sign < 0 || f.sign >= signs.Count) continue;
                Pose(ref f, beat, out float jump, out float sway, out _, out _);
                var sg = signs[f.sign];
                bool raised = f.state != FanState.Idle && f.state != FanState.Sing;
                float lift = raised ? 112 : 78;
                var p = new Vector2(f.pos.x + sway * f.s, f.pos.y + (jump + lift) * f.s);
                sg.rt.sizeDelta = new Vector2(150 * f.s * kx, 82 * f.s * ky);
                sg.rt.anchoredPosition = new Vector2(p.x * kx, p.y * ky);
                sg.rt.localEulerAngles = new Vector3(0, 0, Mathf.Sin(time * 2f + i) * 6f);
            }
        }
    }
}
