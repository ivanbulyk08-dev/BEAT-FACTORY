using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Stadium "drone" intro (2D): the pre-rendered aerial stadium (Art/Venues/stadium_aerial.png, ~45,000 fans drawn in)
    /// over a night sky and city lights, with live layers on top — phone lights twinkling in the stands, light beams
    /// sweeping from the stage on the beat, the LED wall pulsing with the artist, floodlights shimmering.
    /// Camera path: 0–3 s wide aerial view · 3–6 s approach over the stands · 6–9 s descend toward the stage
    /// (the view tilts lower) · 9 s hand-off to the live stage, where the artist raises the mic and the music starts.
    /// Tap anywhere (or SKIP) to skip. ~70 UI objects, one 2048×1120 sprite and two 128×128 textures (made once).
    /// </summary>
    public class StadiumFlyover : MonoBehaviour
    {
        public const float RevealAt = 9f;

        // aerial art layout (pixels, bottom-left origin) — keep in sync with ArtGenerator.StadiumAerial
        static readonly Vector2 ArtSize = new Vector2(2048, 1120), ArtCenter = new Vector2(1024, 560);
        static readonly Vector2 ArtStage = new Vector2(1024, 752);
        static readonly Rect ArtScreen = new Rect(1024 - 196, 752 - 4, 392, 134);
        static readonly Vector2[] ArtFloodlights = { new Vector2(288, 975), new Vector2(1760, 975), new Vector2(238, 469), new Vector2(1810, 469) };
        const float DesignWidth = 1400f; // aerial art width in design units (800×1400 frame)

        RectTransform frame, world;
        CanvasGroup group;
        RawImage twinkle, city;
        Image flash, screenGlow, portrait;
        Image[] beams = new Image[0], floods = new Image[0];
        TextMeshProUGUI caption, skipLabel;
        bool skipped;
        Color[] palette = { Color.white };
        float bpm = 120;

        static Texture2D twinkleTex;

        public bool Skipped => skipped;

        /// <summary>True from the moment the live stage takes over (music can start).</summary>
        public bool Revealed { get; private set; }

        static float Unit => DesignWidth / 2048f;
        static Vector2 Local(Vector2 art) => (art - ArtCenter) * Unit;

        public static StadiumFlyover Create(RectTransform parent, UISkin skin, Sprite aerial, Sprite glow, Sprite beam)
        {
            var root = UIKit.Stretch(UIKit.Rect("StadiumIntro", parent));
            var fly = root.gameObject.AddComponent<StadiumFlyover>();
            fly.Build(skin, aerial, glow, beam);
            root.gameObject.SetActive(false);
            return fly;
        }

        void Build(UISkin skin, Sprite aerial, Sprite glow, Sprite beam)
        {
            var root = (RectTransform)transform;
            group = root.gameObject.AddComponent<CanvasGroup>();
            var sky = UIKit.Image("Sky", root, new Color(0.012f, 0.016f, 0.05f));
            UIKit.Stretch(sky.rectTransform);
            sky.raycastTarget = true; // tap anywhere = skip the intro (not the concert)
            var tap = sky.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => skipped = true);

            // design space 800×1400 scaled to cover the screen (like the stage view)
            frame = UIKit.Rect("Frame", root);
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(800, 1400);

            var horizon = UIKit.Image("HorizonGlow", frame, new Color(0.42f, 0.24f, 0.85f, 0.25f), glow);
            UIKit.Place(horizon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(2000, 760));
            for (int i = 0; i < 50; i++)
            {
                var star = UIKit.Image("Star", frame, new Color(1, 1, 1, 0.2f + 0.5f * Hash(i, 3)), skin.circle);
                float sz = 2f + 3f * Hash(i, 7);
                UIKit.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420 + 840 * Hash(i, 1), 330 + 380 * Hash(i, 2)), new Vector2(sz, sz));
            }

            world = UIKit.Rect("World", frame);
            world.anchorMin = world.anchorMax = new Vector2(0.5f, 0.5f);
            world.sizeDelta = Vector2.zero;

            // city lights around the stadium
            city = new GameObject("City", typeof(RectTransform)).AddComponent<RawImage>();
            city.transform.SetParent(world, false);
            city.texture = TwinkleTexture();
            city.color = new Color(1f, 0.62f, 0.3f, 0.3f);
            city.uvRect = new Rect(0, 0, 18, 3.7f);
            city.raycastTarget = false;
            UIKit.Place(city.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(4400, 900)); // below the horizon only

            var spill = UIKit.Image("LightSpill", world, new Color(1f, 0.9f, 0.75f, 0.18f), glow);
            UIKit.Place(spill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(DesignWidth * 1.6f, DesignWidth * 1.0f));

            // the stadium (pre-rendered), phone lights clipped to it
            var art = UIKit.Image("Aerial", world, Color.white, aerial);
            UIKit.Place(art.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ArtSize * Unit);
            art.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            twinkle = new GameObject("PhoneLights", typeof(RectTransform)).AddComponent<RawImage>();
            twinkle.transform.SetParent(art.transform, false);
            twinkle.texture = TwinkleTexture();
            twinkle.uvRect = new Rect(0, 0, 14, 7.6f);
            twinkle.raycastTarget = false;
            UIKit.Stretch(twinkle.rectTransform);

            // the LED wall comes alive with the artist
            var screenRt = UIKit.Rect("LedWall", world);
            UIKit.Place(screenRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Local(ArtScreen.center), ArtScreen.size * Unit);
            screenRt.gameObject.AddComponent<RectMask2D>();
            portrait = UIKit.Image("Artist", screenRt, Color.white);
            UIKit.Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ArtScreen.height * Unit * 1.4f, ArtScreen.height * Unit * 1.4f));
            screenGlow = UIKit.Image("Pulse", screenRt, new Color(1, 1, 1, 0));
            UIKit.Stretch(screenGlow.rectTransform);

            beams = new Image[8];
            var top = Local(new Vector2(ArtStage.x, ArtScreen.yMax + 22));
            for (int i = 0; i < beams.Length; i++)
            {
                var b = UIKit.Image("Beam", world, Color.white, beam);
                var rt = b.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = top + new Vector2((-1f + 2f * i / (beams.Length - 1)) * ArtScreen.width * 0.7f * Unit, 0);
                rt.sizeDelta = new Vector2(46, 760);
                beams[i] = b;
            }

            floods = new Image[ArtFloodlights.Length];
            for (int i = 0; i < floods.Length; i++)
            {
                floods[i] = UIKit.Image("Floodlight", world, new Color(1f, 0.95f, 0.82f, 0.75f), glow);
                UIKit.Place(floods[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Local(ArtFloodlights[i]), new Vector2(170, 130));
            }

            flash = UIKit.Image("Flash", root, new Color(1, 1, 1, 0));
            UIKit.Stretch(flash.rectTransform);

            caption = UIKit.Text("Caption", root, "", 64, skin.text, skin.display, TextAlignmentOptions.Center, 3);
            UIKit.Place(caption.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 240), new Vector2(1000, 200));
            caption.textWrappingMode = TextWrappingModes.Normal;

            var skipBtn = UIKit.Rounded("Skip", root, new Color(0, 0, 0, 0.45f));
            UIKit.Place(skipBtn.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -150), new Vector2(300, 80));
            skipBtn.raycastTarget = true;
            var sb = skipBtn.gameObject.AddComponent<Button>();
            sb.onClick.AddListener(() => skipped = true);
            skipLabel = UIKit.Text("Label", skipBtn.rectTransform, "", 32, skin.text, skin.heading, TextAlignmentOptions.Center, 3);
        }

        static float Hash(int i, int salt) => Mathf.Repeat(Mathf.Sin(i * 12.9898f + salt * 78.233f) * 43758.5453f, 1f);

        /// <summary>
        /// Plays the flight. <paramref name="onReveal"/> runs at <see cref="RevealAt"/> (or right away when skipped):
        /// that's where the live stage takes over. The coroutine ends when the intro has faded out.
        /// </summary>
        public IEnumerator Play(ArtistData artist, string captionText, Color[] colors, float beatsPerMinute, Action onReveal)
        {
            skipped = false;
            Revealed = false;
            palette = colors != null && colors.Length > 0 ? colors : palette;
            bpm = beatsPerMinute > 0 ? beatsPerMinute : 120;
            portrait.sprite = artist != null ? artist.avatar : null;
            portrait.enabled = portrait.sprite != null;
            caption.text = captionText;
            skipLabel.text = Loc.T("concert.skip_intro");
            group.alpha = 1f;
            flash.color = new Color(1, 1, 1, 0);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            float t = 0;
            Pose(0);
            while (t < RevealAt && !skipped)
            {
                yield return null;
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                Pose(t);
            }
            onReveal?.Invoke();
            Revealed = true;

            // hand-off: a soft flash, then the live stage shows through
            float fade = skipped ? 0.25f : 0.8f;
            for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / fade)
            {
                flash.color = new Color(1, 1, 1, 0.2f * (1f - k));
                group.alpha = 1f - k;
                if (!skipped) Pose(t += Time.unscaledDeltaTime);
                yield return null;
            }
            group.alpha = 0f;
            gameObject.SetActive(false);
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        /// <summary>The camera path at time t.</summary>
        void Pose(float t)
        {
            var parent = (RectTransform)transform;
            float cover = Mathf.Max(parent.rect.width / 800f, parent.rect.height / 1400f);
            frame.localScale = new Vector3(cover, cover, 1f);

            float a = Ease(t / 3f);           // 0–3 wide aerial
            float b = Ease((t - 3f) / 3f);    // 3–6 over the stands
            float c = Ease((t - 6f) / 3f);    // 6–9 down to the stage
            float scale = Mathf.Lerp(0.56f, 0.66f, a) * Mathf.Lerp(1f, 1.75f, b) * Mathf.Lerp(1f, 1.55f, c);
            float tilt = 1f + 0.05f * a + 0.08f * b + 0.13f * c;   // the drone comes down: things stand up taller
            float roll = Mathf.Lerp(-5f, -3f, a) + 2f * b + 1f * c;
            var stage = Local(ArtScreen.center);
            var focus = Vector2.Lerp(new Vector2(0, -40), new Vector2(0, -10), a);
            focus = Vector2.Lerp(focus, new Vector2(0, 90), b);
            focus = Vector2.Lerp(focus, stage + new Vector2(0, 8), c);
            float drift = Mathf.Sin(t * 0.55f) * 8f * (1f - c); // a drone is never perfectly still

            world.localScale = new Vector3(scale, scale * tilt, 1f);
            world.localEulerAngles = new Vector3(0, 0, roll);
            world.anchoredPosition = -new Vector2(focus.x * scale, focus.y * scale * tilt) + new Vector2(drift, 0);

            // the show is about to start: beams sweep on the beat, phone lights come up, the LED wall pulses
            float beat = t * bpm / 60f;
            float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 2f);
            for (int i = 0; i < beams.Length; i++)
            {
                var col = palette[i % palette.Length];
                col.a = (0.1f + 0.2f * pulse) * Mathf.Lerp(0.5f, 1f, b);
                beams[i].color = col;
                beams[i].rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 0.9f + i * 0.8f) * 30f);
            }
            for (int i = 0; i < floods.Length; i++)
                floods[i].transform.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Sin(t * 1.3f + i));
            var tw = Color.Lerp(Color.white, palette[Mathf.FloorToInt(beat) % palette.Length], 0.5f * c);
            tw.a = Mathf.Lerp(0.3f, 0.9f, Mathf.Max(b * 0.6f, c)) * (0.75f + 0.25f * pulse);
            twinkle.color = tw;
            if (Mathf.Repeat(t, 0.3f) < Time.unscaledDeltaTime)
                twinkle.uvRect = new Rect(Hash((int)(t * 10f), 5), Hash((int)(t * 10f), 9), twinkle.uvRect.width, twinkle.uvRect.height);
            var sg = palette[0];
            sg.a = 0.15f + 0.3f * pulse;
            screenGlow.color = sg;
            caption.alpha = Ease(t / 1.2f) * (1f - Ease((t - 5.5f) / 1.2f));
        }

        /// <summary>128×128 tile of a few soft white lights (phones in the stands, city lights).</summary>
        static Texture2D TwinkleTexture()
        {
            if (twinkleTex != null) return twinkleTex;
            const int n = 128;
            var px = new Color32[n * n];
            var rnd = new System.Random(91);
            for (int k = 0; k < 40; k++)
            {
                // soft round lights (no square pixels when the camera gets close)
                int cx = rnd.Next(n), cy = rnd.Next(n);
                for (int dy = -3; dy <= 3; dy++)
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / 3.2f;
                        if (d >= 1f) continue;
                        byte al = (byte)(255 * Mathf.Pow(1f - d, 2.2f));
                        int i = ((cy + dy + n) % n) * n + (cx + dx + n) % n;
                        if (px[i].a < al) px[i] = new Color32(255, 255, 255, al);
                    }
            }
            twinkleTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "StadiumLights" };
            twinkleTex.SetPixels32(px);
            twinkleTex.Apply(false, true);
            return twinkleTex;
        }
    }
}
