using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The concert light show, synced to the music clock: moving heads with volumetric beams, laser patterns (fan, sweeps,
    /// X-cross, framing the artist, over the crowd), follow spots + backlight on the artist, floor uplights, animated LED
    /// panels, haze and smoke, pyro flame bursts, spark gerbs/waterfalls and controlled flashes.
    /// Scale grows with the venue and the artist's popularity; LOW / MEDIUM / HIGH quality limits the counts.
    /// Everything is pooled UI images in "design space" (800×1400 venue pixels): one shared atlas-free material, no allocations
    /// per frame. Lasers and flames never cross the artist: back effects are drawn behind the performer, front ones avoid them.
    /// </summary>
    public class StageLightShow : MonoBehaviour
    {
        public enum LaserPattern { Fan, SweepLR, SweepRL, Cross, FrameArtist, OverCrowd }

        class Laser { public RectTransform rt, glowRt; public Image core, glow; public float len, width; public bool front; public Vector2 origin; public float angle; }
        class Head { public RectTransform beam, fixture; public Image beamImg, fixtureImg, halo; public Vector2 pos; public float phase, angle; }
        class Particle { public RectTransform rt; public Image img; public Vector2 pos, vel; public float life, max, size; public Color color; }
        class FlameUnit { public RectTransform rt, glowRt; public Image img, glow; public Vector2 pos; public float t = -1, height; }
        class Puff { public RectTransform rt; public Image img; public Vector2 pos, vel; public float t = -1, max; }

        RectTransform back, front, panels, hazeFront, flashLayer;
        Sprite beamSprite, glowSprite, laserCore, laserGlow, flameSprite, sparkSprite, circleSprite;
        readonly List<Laser> lasers = new List<Laser>();
        readonly List<Head> heads = new List<Head>();
        readonly List<Image> uplights = new List<Image>();
        readonly List<Particle> sparks = new List<Particle>();
        readonly List<FlameUnit> flames = new List<FlameUnit>();
        readonly List<Puff> smoke = new List<Puff>();
        readonly List<Image> haze = new List<Image>();
        readonly List<(RectTransform rt, Image[] bars, Image bg)> ledPanels = new List<(RectTransform, Image[], Image)>();
        Image spotL, spotR, pool, backlight, blinder, screenFlash;
        Image[] screenBars = new Image[0];
        RectTransform screenRect;

        Func<float> clock;
        Func<Vector2> artistPos;
        IList<float> kicks;
        int kickIndex;
        Color[] palette = { Color.magenta, Color.cyan, Color.white };
        float bpm = 120, intensity = 0.3f, pulse, kickPulse, flash, beatTimer;
        int lastBeat = -1, tier, quality = 2, activeLasers, activeHeads, maxLasers, maxSparks;
        float laserTarget, popularity;
        LaserPattern pattern = LaserPattern.Fan;
        LaserPattern[] patternPool = { LaserPattern.Fan };
        System.Random rnd = new System.Random();
        bool flamesAllowed, sparksAllowed;
        ShowPart part = ShowPart.Intro;
        float stageY, trussY;

        public ShowPart Part => part;

        // ------------------------------------------------------------------ construction

        /// <summary>Creates the light-show layers inside the stage camera (the caller orders them around the performer/crowd).</summary>
        public static StageLightShow Create(RectTransform cam, Sprite beam, Sprite glow, Sprite laserCore, Sprite laserGlow, Sprite flame, Sprite spark, Sprite circle)
        {
            var go = new GameObject("LightShow", typeof(RectTransform));
            go.transform.SetParent(cam, false);
            UIKit.Stretch((RectTransform)go.transform);
            var ls = go.AddComponent<StageLightShow>();
            ls.beamSprite = beam; ls.glowSprite = glow; ls.laserCore = laserCore; ls.laserGlow = laserGlow;
            ls.flameSprite = flame; ls.sparkSprite = spark; ls.circleSprite = circle;
            ls.panels = ls.Space("LedPanels", cam);
            ls.back = ls.Space("BackLights", cam);
            ls.front = ls.Space("FrontFX", cam);
            ls.hazeFront = ls.Space("HazeFront", cam);
            ls.flashLayer = ls.Space("Flash", cam);
            ls.BuildPools();
            return ls;
        }

        public RectTransform PanelsLayer => panels;
        /// <summary>Painted venue positions (stage, truss, side panels); default = the generated venues.</summary>
        public VenueLayout Layout { get; set; }
        public RectTransform BackLayer => back;
        public RectTransform FrontLayer => front;
        public RectTransform HazeFrontLayer => hazeFront;
        public RectTransform FlashLayer => flashLayer;

        /// <summary>A rect in venue design units (800×1400), scaled to the camera every frame.</summary>
        RectTransform Space(string name, RectTransform cam)
        {
            var rt = UIKit.Rect(name, cam);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ConcertStageView.W, ConcertStageView.H);
            return rt;
        }

        Image Img(string name, RectTransform parent, Sprite s, Vector2 pivot)
        {
            var img = UIKit.Image(name, parent, Color.clear, s);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            img.gameObject.SetActive(false);
            return img;
        }

        void BuildPools()
        {
            for (int i = 0; i < 6; i++) haze.Add(Img("Haze" + i, back, glowSprite, new Vector2(0.5f, 0.5f)));
            backlight = Img("Backlight", back, glowSprite, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 12; i++) uplights.Add(Img("Uplight" + i, back, beamSprite, new Vector2(0.5f, 1f)));
            for (int i = 0; i < 16; i++)
            {
                var beam = Img("MoverBeam" + i, back, beamSprite, new Vector2(0.5f, 1f));
                var halo = Img("MoverHalo" + i, back, glowSprite, new Vector2(0.5f, 0.5f));
                var fix = Img("Mover" + i, back, circleSprite, new Vector2(0.5f, 0.5f));
                heads.Add(new Head { beam = beam.rectTransform, beamImg = beam, fixture = fix.rectTransform, fixtureImg = fix, halo = halo });
            }
            for (int i = 0; i < 20; i++)
            {
                bool f = i >= 14; // the last ones shoot over the crowd (front layer)
                var layer = f ? front : back;
                var g = Img("LaserGlow" + i, layer, laserGlow, new Vector2(0.5f, 0f));
                var c = Img("Laser" + i, layer, laserCore, new Vector2(0.5f, 0f));
                lasers.Add(new Laser { rt = c.rectTransform, glowRt = g.rectTransform, core = c, glow = g, front = f });
            }
            spotL = Img("FollowSpotL", back, beamSprite, new Vector2(0.5f, 1f));
            spotR = Img("FollowSpotR", back, beamSprite, new Vector2(0.5f, 1f));
            pool = Img("SpotPool", back, glowSprite, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 6; i++)
            {
                var img = Img("Flame" + i, front, flameSprite, new Vector2(0.5f, 0f));
                var gl = Img("FlameGlow" + i, front, glowSprite, new Vector2(0.5f, 0.5f));
                gl.rectTransform.SetSiblingIndex(img.rectTransform.GetSiblingIndex());
                flames.Add(new FlameUnit { rt = img.rectTransform, img = img, glowRt = gl.rectTransform, glow = gl });
            }
            for (int i = 0; i < 10; i++)
            {
                var img = Img("Smoke" + i, front, glowSprite, new Vector2(0.5f, 0.5f));
                smoke.Add(new Puff { rt = img.rectTransform, img = img });
            }
            for (int i = 0; i < 140; i++)
            {
                var img = Img("Spark" + i, front, sparkSprite, new Vector2(0.5f, 0.5f));
                sparks.Add(new Particle { rt = img.rectTransform, img = img });
            }
            for (int i = 0; i < 4; i++)
            {
                var rt = UIKit.Rect("LedPanel" + i, panels);
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0f);
                var bg = UIKit.Image("Bg", rt, new Color(0.03f, 0.02f, 0.06f, 0.95f));
                UIKit.Stretch(bg.rectTransform);
                var bars = new Image[7];
                for (int b = 0; b < bars.Length; b++)
                {
                    bars[b] = UIKit.Image("Bar" + b, rt, Color.white);
                    var br = bars[b].rectTransform;
                    br.anchorMin = new Vector2((b + 0.15f) / bars.Length, 0.04f);
                    br.anchorMax = new Vector2((b + 0.85f) / bars.Length, 0.5f);
                    br.offsetMin = br.offsetMax = Vector2.zero;
                }
                rt.gameObject.SetActive(false);
                ledPanels.Add((rt, bars, bg));
            }
            hazeLow = Img("HazeLow", hazeFront, glowSprite, new Vector2(0.5f, 0.5f));
            blinder = Img("Blinder", flashLayer, glowSprite, new Vector2(0.5f, 0.5f));
            screenFlash = Img("ScreenFlash", flashLayer, null, new Vector2(0.5f, 0.5f));
        }

        Image hazeLow;

        /// <summary>Animated bars on the main LED screen (under the artist's face only during drops).</summary>
        public void AttachScreen(RectTransform screen)
        {
            screenRect = screen;
            screenBars = new Image[12];
            for (int i = 0; i < screenBars.Length; i++)
            {
                var img = UIKit.Image("EqBar" + i, screen, new Color(1, 1, 1, 0));
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2((i + 0.15f) / screenBars.Length, 0f);
                rt.anchorMax = new Vector2((i + 0.85f) / screenBars.Length, 0.35f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                screenBars[i] = img;
            }
        }

        // ------------------------------------------------------------------ setup

        public void Setup(VenueTier venue, float artistPopularity, GenreStage look, int qualityLevel, Func<float> time, float tempo,
            IList<float> kickTimes, Func<Vector2> artist, int seed)
        {
            rnd = new System.Random(seed);
            tier = (int)venue;
            popularity = artistPopularity;
            quality = Mathf.Clamp(qualityLevel, 0, 2);
            palette = look.palette;
            clock = time;
            bpm = tempo;
            kicks = kickTimes;
            kickIndex = 0;
            artistPos = artist;
            lastBeat = -1;
            part = ShowPart.Intro;
            intensity = 0.25f;
            float q = quality == 0 ? 0.4f : quality == 1 ? 0.7f : 1f;
            float fame = Mathf.Clamp01(popularity / 100f);
            float[] trussTop = { 1150, 1180, 1130, 1210, 1150 };
            float[] stageTop = { 560, 560, 560, 560, 560 };
            trussY = Layout.IsSet ? Layout.trussY : trussTop[tier];
            stageY = Layout.IsSet ? Layout.edgeY + 20f : stageTop[tier];

            // venue scale: club 2–4 movers, a couple of lasers; stadium: everything
            int[] moverBase = { 3, 5, 7, 10, 14 };
            int[] laserBase = { 2, 4, 6, 12, 18 };
            int[] upBase = { 0, 4, 6, 8, 12 };
            int[] hazeBase = { 2, 3, 4, 5, 6 };
            int[] sparkBase = { 0, 30, 50, 100, 140 };
            activeHeads = Mathf.Clamp(Mathf.RoundToInt((moverBase[tier] + fame * 2) * Mathf.Lerp(0.6f, 1f, q)), 2, heads.Count);
            maxLasers = Mathf.Clamp(Mathf.RoundToInt((laserBase[tier] * (0.75f + 0.5f * fame)) * q), tier == 0 ? 1 : 2, lasers.Count);
            int ups = Mathf.Clamp(Mathf.RoundToInt(upBase[tier] * Mathf.Lerp(0.5f, 1f, q)), 0, uplights.Count);
            if (Layout.IsSet)
            {
                // painted venues already show their own rigs and beams: fewer live lights, no extra fixtures
                activeHeads = Mathf.Max(2, Mathf.RoundToInt(activeHeads * 0.45f));
                maxLasers = Mathf.Max(1, Mathf.RoundToInt(maxLasers * 0.55f));
                ups = Mathf.RoundToInt(ups * 0.4f);
            }
            int hz = Mathf.Clamp(Mathf.RoundToInt(hazeBase[tier] * Mathf.Lerp(0.5f, 1f, q)), 1, haze.Count);
            maxSparks = Mathf.RoundToInt(sparkBase[tier] * q);
            flamesAllowed = tier >= (int)VenueTier.Arena;
            sparksAllowed = tier >= (int)VenueTier.SmallVenue && maxSparks > 0;

            // moving heads along the truss
            for (int i = 0; i < heads.Count; i++)
            {
                var h = heads[i];
                bool on = i < activeHeads;
                h.beamImg.gameObject.SetActive(on); h.fixtureImg.gameObject.SetActive(on && !Layout.IsSet); h.halo.gameObject.SetActive(on && !Layout.IsSet);
                if (!on) continue;
                h.pos = new Vector2(Mathf.Lerp(80, 720, activeHeads == 1 ? 0.5f : i / (activeHeads - 1f)), trussY - 8);
                h.phase = (float)rnd.NextDouble() * 6.28f;
                h.beam.anchoredPosition = h.pos;
                h.beam.sizeDelta = new Vector2(Mathf.Lerp(150, 230, tier / 4f), Mathf.Lerp(760, 1180, tier / 4f));
                h.fixture.anchoredPosition = h.pos;
                h.fixture.sizeDelta = new Vector2(16, 16);
                h.halo.rectTransform.anchoredPosition = h.pos;
                h.halo.rectTransform.sizeDelta = new Vector2(70, 70);
            }
            // floor uplights behind the artist
            for (int i = 0; i < uplights.Count; i++)
            {
                var u = uplights[i];
                bool on = i < ups;
                u.gameObject.SetActive(on);
                if (!on) continue;
                var rt = u.rectTransform;
                float x = Mathf.Lerp(120, 680, ups == 1 ? 0.5f : i / (ups - 1f));
                rt.anchoredPosition = new Vector2(x, stageY + 30);
                rt.localEulerAngles = new Vector3(0, 0, 180 + (x - 400) * 0.05f);
                rt.sizeDelta = new Vector2(90, Mathf.Lerp(380, 700, tier / 4f));
            }
            for (int i = 0; i < haze.Count; i++)
            {
                bool on = i < hz;
                haze[i].gameObject.SetActive(on);
                if (!on) continue;
                var rt = haze[i].rectTransform;
                rt.sizeDelta = Vector2.one * (650 + (float)rnd.NextDouble() * 500);
                rt.anchoredPosition = new Vector2((float)rnd.NextDouble() * 800, 650 + (float)rnd.NextDouble() * 450);
            }
            hazeLow.gameObject.SetActive(true);
            hazeLow.rectTransform.sizeDelta = new Vector2(1400, 360);
            hazeLow.rectTransform.anchoredPosition = new Vector2(400, 470);
            foreach (var l in lasers) { l.core.gameObject.SetActive(false); l.glow.gameObject.SetActive(false); }
            foreach (var s in sparks) { s.life = 0; s.img.gameObject.SetActive(false); }
            foreach (var f in flames) { f.t = -1; f.img.gameObject.SetActive(false); f.glow.gameObject.SetActive(false); }
            foreach (var p in smoke) { p.t = -1; p.img.gameObject.SetActive(false); }

            // follow spots + backlight on the artist
            foreach (var s in new[] { spotL, spotR })
            {
                s.gameObject.SetActive(true);
                s.rectTransform.sizeDelta = new Vector2(tier >= 3 ? 210 : 170, 900);
            }
            spotL.rectTransform.anchoredPosition = new Vector2(60, trussY + 20);
            spotR.rectTransform.anchoredPosition = new Vector2(740, trussY + 20);
            spotL.gameObject.SetActive(tier >= 1);
            spotR.gameObject.SetActive(true);
            pool.gameObject.SetActive(true);
            pool.rectTransform.sizeDelta = new Vector2(260, 70);
            backlight.gameObject.SetActive(true);
            backlight.rectTransform.sizeDelta = new Vector2(420, 520);

            // LED side panels (hall and bigger)
            Vector4[] panelSlots = Layout.IsSet ? (Layout.panels ?? new Vector4[0]) : tier switch
            {
                0 => new Vector4[0],
                1 => new[] { new Vector4(70, 640, 90, 250), new Vector4(730, 640, 90, 250) },
                2 => new[] { new Vector4(110, 700, 80, 300), new Vector4(690, 700, 80, 300) },
                3 => new[] { new Vector4(75, 650, 90, 140), new Vector4(725, 650, 90, 140) },
                _ => new[] { new Vector4(50, 600, 80, 170), new Vector4(750, 600, 80, 170), new Vector4(240, 1110, 120, 60), new Vector4(560, 1110, 120, 60) },
            };
            for (int i = 0; i < ledPanels.Count; i++)
            {
                bool on = i < panelSlots.Length;
                ledPanels[i].rt.gameObject.SetActive(on);
                if (!on) continue;
                ledPanels[i].rt.anchoredPosition = new Vector2(panelSlots[i].x, panelSlots[i].y);
                ledPanels[i].rt.sizeDelta = new Vector2(panelSlots[i].z, panelSlots[i].w);
            }
            blinder.gameObject.SetActive(true);
            blinder.rectTransform.sizeDelta = new Vector2(1600, 600);
            blinder.rectTransform.anchoredPosition = new Vector2(400, trussY);
            screenFlash.gameObject.SetActive(false);

            SetPatternPool(LaserPattern.Fan);
            laserTarget = 0;
        }

        // ------------------------------------------------------------------ show control (director)

        public void SetPart(ShowPart p)
        {
            part = p;
            switch (p)
            {
                case ShowPart.Intro: intensity = 0.25f; laserTarget = 0; SetPatternPool(LaserPattern.Fan); break;
                case ShowPart.Verse: intensity = 0.4f; laserTarget = tier >= 3 ? 0.35f : 0; SetPatternPool(LaserPattern.Fan, LaserPattern.SweepLR, LaserPattern.SweepRL); break;
                case ShowPart.BuildUp: intensity = 0.55f; laserTarget = 0.5f; SetPatternPool(LaserPattern.SweepLR, LaserPattern.SweepRL, LaserPattern.Fan); break;
                case ShowPart.Drop: intensity = 1f; laserTarget = 1f; SetPatternPool(LaserPattern.Fan, LaserPattern.Cross, LaserPattern.OverCrowd); break;
                case ShowPart.Chorus: intensity = 0.85f; laserTarget = 0.85f; SetPatternPool(LaserPattern.Cross, LaserPattern.SweepLR, LaserPattern.FrameArtist, LaserPattern.OverCrowd); break;
                case ShowPart.BigMoment: intensity = 1f; laserTarget = 1f; SetPatternPool(LaserPattern.FrameArtist, LaserPattern.Fan, LaserPattern.Cross, LaserPattern.OverCrowd); break;
                default: intensity = 0.5f; laserTarget = 0.4f; SetPatternPool(LaserPattern.Fan); break;
            }
        }

        void SetPatternPool(params LaserPattern[] pool)
        {
            patternPool = pool;
            pattern = pool[rnd.Next(pool.Length)];
        }

        /// <summary>DROP: blinder flash, LED screen flash, flames left → right, sparks, all lasers.</summary>
        public void DropHit(ConcertAudio audio)
        {
            flash = 0.18f;
            screenFlashT = 0.35f;
            kickPulse = 1f;
            if (flamesAllowed) FlameSequence(audio, rnd.NextDouble() < 0.5);
            if (sparksAllowed) Gerbs(audio, 0.6f);
            if (tier >= 2) Smoke(4);
        }

        /// <summary>BIG MOMENT: everything at maximum, sparks waterfall on the biggest stages.</summary>
        public void BigMomentHit(ConcertAudio audio)
        {
            flash = 0.2f;
            screenFlashT = 0.4f;
            if (flamesAllowed) FlameSequence(audio, rnd.NextDouble() < 0.5, all: tier >= 4);
            if (sparksAllowed) { Gerbs(audio, 1f); if (tier >= 3) Waterfall(); }
            if (tier >= 2) Smoke(6);
        }

        /// <summary>End of the show: one last burst on big stages.</summary>
        public void FinaleHit(ConcertAudio audio)
        {
            flash = 0.12f;
            if (flamesAllowed && popularity >= 60) FlameSequence(audio, true, all: true);
            if (sparksAllowed) Gerbs(audio, 0.5f);
        }

        float screenFlashT;

        // ---- pyro
        readonly List<(float at, int index)> flameQueue = new List<(float, int)>();
        ConcertAudio pyroAudio;

        void FlameSequence(ConcertAudio audio, bool leftFirst, bool all = false)
        {
            pyroAudio = audio;
            float[] xs = tier >= 4 ? new[] { 40f, 150f, 650f, 760f } : new[] { 70f, 730f };
            float now = Time.unscaledTime;
            float gap = 0.2f + (float)rnd.NextDouble() * 0.2f;
            int half = xs.Length / 2;
            for (int i = 0; i < xs.Length; i++)
            {
                bool left = i < half;
                if (!all && xs.Length > 2 && (i == 1 || i == 2)) continue; // outer cannons only, unless it's the climax
                float at = now + ((left == leftFirst) ? 0f : gap) + (i % half) * 0.08f;
                flameQueue.Add((at, i));
                flames[i].pos = new Vector2(xs[i], stageY - 10);
            }
        }

        void FireFlame(int i)
        {
            var f = flames[i];
            f.t = 0;
            f.height = Mathf.Lerp(260, 380, tier / 4f) * (0.9f + 0.2f * (float)rnd.NextDouble());
            f.img.gameObject.SetActive(true);
            f.glow.gameObject.SetActive(true);
            f.rt.anchoredPosition = f.pos;
            f.glowRt.anchoredPosition = f.pos + new Vector2(0, f.height * 0.35f);
            pyroAudio?.Pyro(0.9f + (float)rnd.NextDouble() * 0.2f);
        }

        // ---- sparks
        void Gerbs(ConcertAudio audio, float amount)
        {
            int n = Mathf.RoundToInt(maxSparks * 0.5f * amount);
            float[] xs = { 140, 660 };
            foreach (var x in xs)
                for (int i = 0; i < n / 2; i++)
                    Emit(new Vector2(x, stageY - 20), new Vector2((float)(rnd.NextDouble() - 0.5) * 160, 520 + (float)rnd.NextDouble() * 380), 0.6f + (float)rnd.NextDouble() * 0.5f,
                        new Color(1f, 0.85f + 0.15f * (float)rnd.NextDouble(), 0.55f));
            audio?.Sparks();
        }

        void Waterfall()
        {
            int n = maxSparks / 2;
            for (int i = 0; i < n; i++)
                Emit(new Vector2(60 + (float)rnd.NextDouble() * 680, trussY - 20), new Vector2((float)(rnd.NextDouble() - 0.5) * 30, -(float)rnd.NextDouble() * 60), 1.2f + (float)rnd.NextDouble() * 0.6f,
                    new Color(1f, 0.92f, 0.7f));
        }

        void Emit(Vector2 pos, Vector2 vel, float life, Color c)
        {
            foreach (var s in sparks)
            {
                if (s.life > 0) continue;
                s.pos = pos; s.vel = vel; s.life = s.max = life; s.color = c; s.size = 6 + (float)rnd.NextDouble() * 6;
                s.img.gameObject.SetActive(true);
                return;
            }
        }

        // ---- smoke (CO2 jets at the stage sides, never over the artist)
        void Smoke(int n)
        {
            if (quality == 0) n /= 2;
            for (int k = 0; k < n; k++)
                foreach (var p in smoke)
                {
                    if (p.t >= 0) continue;
                    bool left = k % 2 == 0;
                    p.t = 0; p.max = 1.6f + (float)rnd.NextDouble();
                    p.pos = new Vector2(left ? 90 + (float)rnd.NextDouble() * 80 : 630 + (float)rnd.NextDouble() * 80, stageY - 20);
                    p.vel = new Vector2(left ? -20 : 20, 160 + (float)rnd.NextDouble() * 120);
                    p.img.gameObject.SetActive(true);
                    break;
                }
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var cam = (RectTransform)transform.parent;
            float unit = cam.rect.width / ConcertStageView.W;
            foreach (var l in new[] { panels, back, front, hazeFront, flashLayer }) l.localScale = new Vector3(unit, unit, 1f);

            float t = clock != null ? clock() : Time.unscaledTime;
            float beatLen = 60f / bpm;
            int beat = Mathf.FloorToInt(t / beatLen);
            if (beat != lastBeat)
            {
                lastBeat = beat;
                pulse = 1f; // BEAT → light pulse
                if (beat % 4 == 0)
                {
                    // STRONG BEAT → laser movement / pattern change (every 2 bars) within the section's patterns
                    if (beat % 8 == 0 && patternPool.Length > 1) pattern = patternPool[rnd.Next(patternPool.Length)];
                }
            }
            if (kicks != null)
                while (kickIndex < kicks.Count && kicks[kickIndex] <= t)
                {
                    kickIndex++;
                    kickPulse = 1f; // KICK → backlight pulse
                }
            if (kicks != null && kickIndex > 0 && kickIndex <= kicks.Count && t + 1f < kicks[kickIndex - 1]) kickIndex = 0; // after a seek
            pulse = Mathf.MoveTowards(pulse, 0f, dt * 4f);
            kickPulse = Mathf.MoveTowards(kickPulse, 0f, dt * 5f);
            float beatPhase = Mathf.Repeat(t / beatLen, 1f);
            var art = artistPos != null ? artistPos() : new Vector2(400, 560);

            UpdateHeads(t, art);
            UpdateLasers(t, beatPhase, art, dt);
            UpdateArtistLight(t, art);
            UpdateUplights(t);
            UpdatePanels(t);
            UpdateHaze(t);
            UpdatePyro(dt);
            UpdateSparks(dt);
            UpdateSmoke(dt);

            // controlled flashes: a blinder glow under the truss + LED screen flash (never a full white-out over the artist)
            flash = Mathf.MoveTowards(flash, 0f, dt * 0.5f);
            blinder.color = new Color(1f, 0.97f, 0.9f, flash + 0.04f * pulse * intensity);
            screenFlashT = Mathf.MoveTowards(screenFlashT, 0f, dt);
            if (screenRect != null)
            {
                bool eq = part == ShowPart.Drop || part == ShowPart.BigMoment || part == ShowPart.Chorus;
                for (int i = 0; i < screenBars.Length; i++)
                {
                    float h = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(t * 3f + i * 0.7f)) * (0.5f + 0.5f * pulse);
                    screenBars[i].rectTransform.anchorMax = new Vector2(screenBars[i].rectTransform.anchorMax.x, 0.05f + 0.3f * h);
                    var c = palette[i % palette.Length];
                    screenBars[i].color = new Color(c.r, c.g, c.b, eq ? 0.55f : 0f);
                }
            }
        }

        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, Mathf.Clamp01(a));

        void UpdateHeads(float t, Vector2 art)
        {
            float speed = Mathf.Lerp(0.35f, 1.6f, intensity);
            float light = 0.045f + 0.13f * intensity + 0.08f * pulse * intensity;
            for (int i = 0; i < activeHeads; i++)
            {
                var h = heads[i];
                float target;
                switch (part)
                {
                    case ShowPart.Drop:
                    case ShowPart.BigMoment:
                        target = (i % 2 == 0 ? 1 : -1) * (25 + 20 * Mathf.Sin(t * speed * 2 + h.phase)); break; // fast crossing
                    case ShowPart.Chorus:
                        target = Mathf.Sin(t * speed + i * 0.6f) * 35; break;                                 // wave
                    case ShowPart.Outro:
                        target = Angle(h.pos, art + new Vector2(0, 120)) + Mathf.Sin(t + i) * 4; break;       // all on the artist
                    default:
                        target = Mathf.Sin(t * speed * 0.6f + h.phase) * 28; break;                            // slow sweep
                }
                h.angle = Mathf.LerpAngle(h.angle, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f));
                h.beam.localEulerAngles = new Vector3(0, 0, h.angle);
                var c = palette[i % palette.Length];
                if (part == ShowPart.Drop && flash > 0.08f) c = Color.white;
                h.beamImg.color = A(c, light);
                h.fixtureImg.color = A(Color.Lerp(c, Color.white, 0.6f), 0.6f + 0.4f * pulse);
                h.halo.color = A(c, 0.25f + 0.35f * pulse * intensity);
            }
        }

        /// <summary>Rotation (degrees) for an image pointing DOWN from <paramref name="from"/> toward <paramref name="to"/>.</summary>
        static float Angle(Vector2 from, Vector2 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
        }

        void UpdateLasers(float t, float beatPhase, Vector2 art, float dt)
        {
            float target = laserTarget * maxLasers;
            activeLasers = Mathf.RoundToInt(Mathf.MoveTowards(activeLasers, target, dt * 12f));
            if (activeLasers < target && part == ShowPart.BuildUp) activeLasers = Mathf.Min(activeLasers + 1, (int)target);
            int backCount = 0, frontCount = 0;
            for (int i = 0; i < lasers.Count; i++)
            {
                var l = lasers[i];
                bool overCrowd = pattern == LaserPattern.OverCrowd;
                bool want = l.front ? overCrowd && frontCount < Mathf.Max(2, activeLasers / 3) && tier >= 2
                                    : backCount < activeLasers;
                if (l.front && want) frontCount++;
                if (!l.front && want) backCount++;
                l.core.gameObject.SetActive(want);
                l.glow.gameObject.SetActive(want);
                if (!want) continue;
                int k = l.front ? frontCount - 1 : backCount - 1;
                int n = l.front ? Mathf.Max(1, frontCount) : Mathf.Max(1, activeLasers);
                float u = n == 1 ? 0.5f : k / (n - 1f);
                Vector2 origin; float angle; float len = 1250;
                float breathe = Mathf.Sin(t * 1.3f) * 8f;
                switch (l.front ? LaserPattern.OverCrowd : pattern)
                {
                    case LaserPattern.SweepLR:
                    case LaserPattern.SweepRL:
                    {
                        origin = new Vector2(Mathf.Lerp(160, 640, u), 640);
                        float s = Mathf.Sin(t * 1.4f) * 50f;
                        angle = pattern == LaserPattern.SweepLR ? -s : s;
                        break;
                    }
                    case LaserPattern.Cross:
                    {
                        bool left = k % 2 == 0;
                        origin = new Vector2(left ? 170 : 630, 620);
                        angle = (left ? -1 : 1) * (25 + 20 * u + 10 * Mathf.Sin(t * 1.8f + u * 3));
                        break;
                    }
                    case LaserPattern.FrameArtist:
                    {
                        // beams frame the artist: they end beside the head, never on the face
                        bool left = k % 2 == 0;
                        origin = new Vector2(left ? 60 + 40 * u : 740 - 40 * u, 900 + 120 * u);
                        var aim = art + new Vector2((left ? -1 : 1) * (90 + 30 * Mathf.Sin(t * 2 + u)), 150 + 60 * u);
                        var d = aim - origin;
                        angle = Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
                        len = d.magnitude;
                        break;
                    }
                    case LaserPattern.OverCrowd:
                    {
                        bool left = k % 2 == 0;
                        origin = new Vector2(left ? 110 : 690, 560);
                        angle = (left ? -1 : 1) * (70 + 20 * Mathf.Sin(t * 0.9f + u * 2));
                        len = 1100;
                        break;
                    }
                    default: // fan from behind the artist
                        origin = new Vector2(400, 610);
                        angle = Mathf.Lerp(-68, 68, u) + breathe;
                        break;
                }
                // strong beats nudge the pattern; drops make beams flick
                angle += (part == ShowPart.Drop ? 6f : 2f) * pulse * ((k % 2 == 0) ? 1 : -1);
                l.angle = Mathf.LerpAngle(l.angle, angle, 1f - Mathf.Exp(-dt * 8f));
                var c = palette[(k + (lastBeat / 4)) % palette.Length];
                float bright = 0.75f + 0.25f * pulse;
                float thick = 2.5f + (k % 3) * 0.9f;
                l.rt.anchoredPosition = origin;
                l.glowRt.anchoredPosition = origin;
                l.rt.localEulerAngles = l.glowRt.localEulerAngles = new Vector3(0, 0, l.angle);
                l.rt.sizeDelta = new Vector2(thick, len);
                l.glowRt.sizeDelta = new Vector2(thick * 7f, len);
                l.core.color = A(Color.Lerp(c, Color.white, 0.45f), bright);
                l.glow.color = A(c, 0.55f * bright);
            }
        }

        void UpdateArtistLight(float t, Vector2 art)
        {
            // follow spots: warm white cones landing on the artist (cinematic key light), stronger in big moments
            float k = part == ShowPart.Drop || part == ShowPart.BigMoment || part == ShowPart.Outro ? 1f : 0.7f;
            var target = art + new Vector2(0, 40);
            foreach (var s in new[] { spotL, spotR })
            {
                if (!s.gameObject.activeSelf) continue;
                var rt = s.rectTransform;
                rt.localEulerAngles = new Vector3(0, 0, Angle(rt.anchoredPosition, target));
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, Vector2.Distance(rt.anchoredPosition, target) + 60);
                s.color = new Color(1f, 0.95f, 0.85f, 0.1f * k);
            }
            pool.rectTransform.anchoredPosition = art + new Vector2(0, 6);
            pool.color = new Color(1f, 0.95f, 0.85f, 0.35f * k);
            // backlight: colored glow behind the artist, pumps with the kick — keeps the silhouette readable
            backlight.rectTransform.anchoredPosition = art + new Vector2(0, 170);
            var c = palette[0];
            backlight.color = A(c, 0.22f + 0.25f * kickPulse * intensity);
        }

        void UpdateUplights(float t)
        {
            for (int i = 0; i < uplights.Count; i++)
            {
                var u = uplights[i];
                if (!u.gameObject.activeSelf) continue;
                // chase: one light after another on the beat
                bool chase = part == ShowPart.BuildUp || part == ShowPart.Chorus;
                float on = chase ? (((lastBeat + i) % 3 == 0) ? 1f : 0.3f) : 0.6f + 0.4f * pulse;
                var c = palette[(i + 1) % palette.Length];
                u.color = A(c, (0.05f + 0.1f * intensity) * on);
            }
        }

        void UpdatePanels(float t)
        {
            for (int p = 0; p < ledPanels.Count; p++)
            {
                var panel = ledPanels[p];
                if (!panel.rt.gameObject.activeSelf) continue;
                bool mirror = p % 2 == 1;
                for (int i = 0; i < panel.bars.Length; i++)
                {
                    int idx = mirror ? panel.bars.Length - 1 - i : i;
                    float h = 0.12f + 0.86f * Mathf.Abs(Mathf.Sin(t * (1.5f + idx * 0.37f) + idx)) * (0.4f + 0.6f * Mathf.Max(pulse, intensity * 0.5f));
                    var br = panel.bars[i].rectTransform;
                    br.anchorMax = new Vector2(br.anchorMax.x, 0.04f + 0.92f * h);
                    var c = Color.Lerp(palette[i % palette.Length], Color.white, screenFlashT);
                    panel.bars[i].color = A(c, 0.85f);
                }
                panel.bg.color = Color.Lerp(new Color(0.03f, 0.02f, 0.06f, 0.95f), new Color(1, 1, 1, 0.9f), screenFlashT * 0.8f);
            }
        }

        void UpdateHaze(float t)
        {
            for (int i = 0; i < haze.Count; i++)
            {
                var h = haze[i];
                if (!h.gameObject.activeSelf) continue;
                var rt = h.rectTransform;
                var p = rt.anchoredPosition;
                p.x += Time.unscaledDeltaTime * (6 + i * 3);
                if (p.x > 1150) p.x = -350;
                rt.anchoredPosition = p;
                var c = Color.Lerp(palette[i % palette.Length], new Color(0.8f, 0.8f, 0.9f), 0.6f);
                h.color = A(c, 0.02f + 0.03f * intensity + 0.015f * pulse);
            }
            hazeLow.color = A(Color.Lerp(palette[0], new Color(0.8f, 0.8f, 0.9f), 0.7f), 0.05f + 0.04f * intensity);
        }

        void UpdatePyro(float dt)
        {
            float now = Time.unscaledTime;
            for (int i = flameQueue.Count - 1; i >= 0; i--)
                if (flameQueue[i].at <= now) { FireFlame(flameQueue[i].index); flameQueue.RemoveAt(i); }
            foreach (var f in flames)
            {
                if (f.t < 0) continue;
                f.t += dt;
                const float life = 0.75f;
                float k = f.t / life;
                if (k >= 1f) { f.t = -1; f.img.gameObject.SetActive(false); f.glow.gameObject.SetActive(false); continue; }
                float grow = Mathf.Clamp01(f.t / 0.1f);
                float fade = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                float flicker = 1f + 0.08f * Mathf.Sin(f.t * 60f);
                f.rt.sizeDelta = new Vector2(f.height * 0.36f * flicker, f.height * grow * (0.9f + 0.1f * flicker));
                f.img.color = new Color(1, 1, 1, fade);
                f.glowRt.sizeDelta = Vector2.one * f.height * 1.4f;
                f.glow.color = new Color(1f, 0.55f, 0.15f, 0.45f * fade);
            }
        }

        void UpdateSparks(float dt)
        {
            foreach (var s in sparks)
            {
                if (s.life <= 0) continue;
                s.life -= dt;
                if (s.life <= 0) { s.img.gameObject.SetActive(false); continue; }
                s.vel += Vector2.down * 900f * dt;
                s.vel *= 1f - 0.6f * dt;
                s.pos += s.vel * dt;
                s.rt.anchoredPosition = s.pos;
                float k = s.life / s.max;
                s.rt.sizeDelta = Vector2.one * s.size * (0.5f + 0.5f * k);
                s.img.color = A(s.color, Mathf.Min(1f, k * 1.5f));
            }
        }

        void UpdateSmoke(float dt)
        {
            foreach (var p in smoke)
            {
                if (p.t < 0) continue;
                p.t += dt;
                float k = p.t / p.max;
                if (k >= 1f) { p.t = -1; p.img.gameObject.SetActive(false); continue; }
                p.pos += p.vel * dt;
                p.vel *= 1f - 1.2f * dt;
                p.rt.anchoredPosition = p.pos;
                p.rt.sizeDelta = Vector2.one * Mathf.Lerp(120, 520, Mathf.Sqrt(k));
                p.img.color = new Color(0.92f, 0.92f, 1f, 0.32f * (1f - k));
            }
        }
    }
}
