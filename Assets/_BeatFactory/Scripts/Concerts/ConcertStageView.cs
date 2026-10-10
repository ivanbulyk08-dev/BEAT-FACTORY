using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The live concert: venue + LED screen + light show (lasers, moving heads, follow spots, haze, pyro, sparks) + the artist
    /// performing on stage + a living crowd, inside a "camera" rect the show director moves between shots.
    /// The director follows the artist's soundtrack: INTRO → VERSE → BUILD-UP → DROP → CHORUS → BIG MOMENT → OUTRO.
    /// Layering keeps the artist readable: back lights/lasers behind the performer, flames at the stage wings, flashes capped.
    /// </summary>
    public class ConcertStageView : MonoBehaviour
    {
        public const float W = 800, H = 1400, CrowdH = 560;

        Image venue, crowdMass, portrait, screenTint, strobe;
        RectTransform cam, screenRt, fxLayer, portraitRt;
        ConcertCrowd crowd;
        PerformerRig performer;
        StageLightShow lights;
        ConcertAudio audio;

        Sprite[] venueSprites, crowdSprites;
        Vector4[] screens;
        Sprite confettiSprite, micSprite;
        Texture crowdAtlas;
        UISkin skin;
        GenreStage look;
        int tier, quality = 2;
        float popularity, time, energy = 1f, strobeTimer;

        // camera
        Vector2 camFocus = new Vector2(W / 2, H / 2), camFocusTarget = new Vector2(W / 2, H / 2);
        float camZoom = 1f, camZoomTarget = 1f, camSpeed = 3f, shake;
        bool camClamp = true;
        Coroutine showRoutine;

        public RectTransform FxLayer => fxLayer;
        public float Energy { get => energy; set => energy = value; }
        public ConcertCrowd Crowd => crowd;
        public PerformerRig Performer => performer;
        public StageLightShow Lights => lights;
        public ShowPart CurrentSection { get; private set; }
        public event Action<ShowPart> SectionChanged;

        public static ConcertStageView Create(RectTransform parent, Sprite[] venues, Sprite[] crowds, Vector4[] screenRects,
            Sprite beam, Sprite glow, Sprite confetti, Texture atlas, Sprite mic, UISkin skin,
            Sprite laserCore, Sprite laserGlow, Sprite flame, Sprite spark)
        {
            var holder = UIKit.Rect("Stage", parent);
            UIKit.Stretch(holder);
            var fitter = holder.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = W / H;
            var view = holder.gameObject.AddComponent<ConcertStageView>();
            view.venueSprites = venues;
            view.crowdSprites = crowds;
            view.screens = screenRects;
            view.confettiSprite = confetti;
            view.crowdAtlas = atlas;
            view.micSprite = mic;
            view.skin = skin;
            view.Build(beam, glow, laserCore, laserGlow, flame, spark);
            return view;
        }

        static RectTransform Norm(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(x / W, y / H);
            rt.anchorMax = new Vector2((x + w) / W, (y + h) / H);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        void Build(Sprite beam, Sprite glow, Sprite laserCore, Sprite laserGlow, Sprite flame, Sprite spark)
        {
            var root = (RectTransform)transform;
            cam = UIKit.Stretch(UIKit.Rect("Camera", root));
            venue = UIKit.Image("Venue", cam, Color.white);
            UIKit.Stretch(venue.rectTransform);

            screenRt = UIKit.Rect("LedScreen", cam);
            screenRt.gameObject.AddComponent<RectMask2D>();
            var screenBg = UIKit.Image("Bg", screenRt, new Color(0.05f, 0.03f, 0.1f));
            UIKit.Stretch(screenBg.rectTransform);
            portrait = UIKit.Image("Portrait", screenRt, Color.white);
            portraitRt = portrait.rectTransform;
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
            screenTint = UIKit.Image("Tint", screenRt, new Color(1, 0, 1, 0.2f));
            UIKit.Stretch(screenTint.rectTransform);
            for (int i = 0; i < 18; i++)
            {
                var line = UIKit.Image("Scan", screenRt, new Color(0, 0, 0, 0.18f));
                line.rectTransform.anchorMin = new Vector2(0, i / 18f);
                line.rectTransform.anchorMax = new Vector2(1, i / 18f);
                line.rectTransform.sizeDelta = new Vector2(0, 2);
            }

            lights = StageLightShow.Create(cam, beam, glow, laserCore, laserGlow, flame, spark, skin != null ? skin.circle : null);
            lights.AttachScreen(screenRt);
            performer = PerformerRig.Create(cam, micSprite);
            crowdMass = UIKit.Image("CrowdMass", cam, Color.white);
            Norm(crowdMass.rectTransform, 0, 0, W, CrowdH);
            crowd = ConcertCrowd.Create(cam, crowdAtlas, skin);

            // draw order (back → front): venue · LED panels · LED screen · back lights & lasers · ARTIST · pyro/sparks/front
            // lasers · crowd · low haze · controlled flash
            lights.PanelsLayer.SetSiblingIndex(venue.transform.GetSiblingIndex() + 1);
            screenRt.SetSiblingIndex(lights.PanelsLayer.GetSiblingIndex() + 1);
            lights.BackLayer.SetSiblingIndex(screenRt.GetSiblingIndex() + 1);
            performer.transform.SetSiblingIndex(lights.BackLayer.GetSiblingIndex() + 1);
            lights.FrontLayer.SetSiblingIndex(performer.transform.GetSiblingIndex() + 1);
            crowdMass.transform.SetSiblingIndex(lights.FrontLayer.GetSiblingIndex() + 1);
            crowd.transform.SetSiblingIndex(crowdMass.transform.GetSiblingIndex() + 1);
            lights.HazeFrontLayer.SetAsLastSibling();
            lights.FlashLayer.SetAsLastSibling();
            lights.transform.SetAsLastSibling();

            strobe = UIKit.Image("Strobe", root, new Color(1, 1, 1, 0));
            UIKit.Stretch(strobe.rectTransform);
            fxLayer = UIKit.Rect("FX", root);
            fxLayer.anchorMin = fxLayer.anchorMax = new Vector2(0.5f, 0.5f);
        }

        /// <summary>Sets the venue, the artist (on stage and on the big screen), the genre look, crowd size and the light rig.</summary>
        public void Setup(VenueTier venueTier, ArtistData artist, float artistPopularity, float fill, ConcertAudio concertAudio = null)
        {
            tier = Mathf.Clamp((int)venueTier, 0, venueSprites.Length - 1);
            popularity = artistPopularity;
            audio = concertAudio;
            quality = GameManager.Instance != null ? GameManager.Instance.Settings.ConcertQualityLevel : 2;
            look = GenreStage.For(artist != null ? artist.genre : ArtistGenre.HipHop);
            venue.sprite = venueSprites[tier];
            crowdMass.sprite = crowdSprites[tier];
            crowdMass.color = new Color(0.75f, 0.75f, 0.85f, Mathf.Clamp01((popularity - 55f) / 30f) * 0.85f * fill);
            var s = screens[tier];
            Norm(screenRt, s.x, s.y, s.z, s.w);
            portrait.sprite = artist != null ? artist.avatar : null;
            portrait.enabled = portrait.sprite != null;
            screenTint.color = new Color(look.palette[0].r, look.palette[0].g, look.palette[0].b, 0.18f);
            time = 0f;
            energy = 1f;
            camFocus = camFocusTarget = new Vector2(W / 2, H / 2);
            camZoom = camZoomTarget = 1f;
            UIKit.Clear(fxLayer);

            float[] stageY = { 545, 548, 550, 552, 548 };
            float[] edgeY = { 505, 500, 490, 488, 470 };
            float[] height = { 175, 180, 185, 190, 190 };
            float bpm = audio != null && audio.Profile != null ? audio.Profile.bpm : look.bpm;
            performer.Setup(artist != null ? artist.visual : null, new Vector2(400, stageY[tier]), height[tier], edgeY[tier], bpm);
            crowd.Setup(venueTier, popularity, fill, artist, artist != null ? artist.id.GetHashCode() : 1, quality);
            crowd.Intensity = 0.3f;
            int seed = Environment.TickCount;
            lights.Setup(venueTier, popularity, look, quality, ShowTime, bpm, null, () => performer.DesignPosition, seed);
            CurrentSection = ShowPart.Intro;
        }

        float ShowTime() => audio != null && audio.Track != null ? audio.Time : time;

        // ------------------------------------------------------------------ camera

        public void Shot(Vector2 focus, float zoom, float speed = 3f, bool clamp = true)
        {
            camClamp = clamp;
            camFocusTarget = focus;
            camZoomTarget = zoom;
            camSpeed = speed;
        }

        public void Wide() => Shot(new Vector2(W / 2, H / 2), 1f, 2.5f);
        public void StageShot() => Shot(performer.DesignPosition + new Vector2(0, 150), 1.75f, 2.5f);
        public void CloseUp() => Shot(performer.HeadDesign, 3.3f, 3f);
        public void CrowdShot() => Shot(new Vector2(400, 330), 1.6f, 2.5f);
        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        void UpdateCamera(float dt)
        {
            float k = 1f - Mathf.Exp(-dt * camSpeed);
            camFocus = Vector2.Lerp(camFocus, camFocusTarget, k);
            camZoom = Mathf.Lerp(camZoom, camZoomTarget, k);
            var rt = (RectTransform)transform;
            float u = rt.rect.width / W;
            var f = new Vector2(W / 2, H / 2);
            if (!camClamp) f = camFocus;
            else if (camZoom > 1.001f)
            {
                float halfW = W / (2f * camZoom), halfH = H / (2f * camZoom);
                f = new Vector2(Mathf.Clamp(camFocus.x, halfW, W - halfW), Mathf.Clamp(camFocus.y, halfH, H - halfH));
            }
            shake = Mathf.MoveTowards(shake, 0f, dt * 30f);
            var jitter = shake > 0 ? new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * shake : Vector2.zero;
            cam.localScale = new Vector3(camZoom, camZoom, 1f);
            cam.anchoredPosition = -(f - new Vector2(W / 2, H / 2)) * u * camZoom + jitter;
        }

        // ------------------------------------------------------------------ show director

        /// <summary>Length of the performance part (before the results), from the soundtrack.</summary>
        public float ShowLength => audio != null && audio.Track != null ? audio.Track.Start(ShowPart.Outro) + 1.4f : 13.5f;

        float SectionStart(ShowPart p)
        {
            if (audio != null && audio.Track != null) return audio.Track.Start(p);
            float[] plan = { 0f, 1.8f, 4.3f, 5.8f, 7.8f, 10.6f, 12.6f };
            return plan[(int)p];
        }

        public void RunShow()
        {
            if (showRoutine != null) StopCoroutine(showRoutine);
            if (audio != null && audio.Track != null) lights.Setup((VenueTier)tier, popularity, look, quality, ShowTime, audio.Profile.bpm, audio.Track.kicks, () => performer.DesignPosition, Environment.TickCount);
            showRoutine = StartCoroutine(Director());
        }

        void Enter(ShowPart s, float intensity)
        {
            CurrentSection = s;
            crowd.Intensity = intensity;
            lights.SetPart(s);
            SectionChanged?.Invoke(s);
        }

        IEnumerator Until(ShowPart p, float offset = 0f)
        {
            float at = SectionStart(p) + offset;
            while (ShowTime() < at) yield return null;
        }

        IEnumerator Director()
        {
            bool big = popularity >= 50 || tier >= 3;
            time = 0;
            Enter(ShowPart.Intro, 0.3f);
            Wide();
            yield return Until(ShowPart.Verse);
            Enter(ShowPart.Verse, 0.4f);
            performer.SetState(PerformState.Rap);
            StageShot();
            yield return Until(ShowPart.Verse, 1.2f);
            CloseUp(); // "that's the artist I signed" — same face as the profile

            yield return Until(ShowPart.BuildUp);
            Enter(ShowPart.BuildUp, 0.55f);
            Wide();
            performer.SetState(PerformState.Hype); // artist raises the hands → the wave spreads from the front
            crowd.Wave(performer.DesignPosition, FanState.HandsUp, 380f + popularity * 3f);
            yield return Until(ShowPart.BuildUp, 0.9f);
            crowd.Intensity = 0.72f;

            yield return Until(ShowPart.Drop);
            Enter(ShowPart.Drop, 1f);
            performer.SetState(PerformState.BigDrop);
            crowd.Surge(FanState.Jump);
            lights.DropHit(audio);
            Flash(0.12f);
            Shake(14f);
            Shot(new Vector2(400, 640), 1.08f, 4f);

            yield return Until(ShowPart.Chorus);
            Enter(ShowPart.Chorus, 0.75f);
            performer.SetState(PerformState.CrowdInteraction);
            CrowdShot();
            if (popularity >= 25) crowd.PhoneLights(3f);
            float chorusEnd = SectionStart(ShowPart.BigMoment);
            while (ShowTime() < chorusEnd - 1.0f)
            {
                crowd.ArtistAtEdge = performer.AtEdge;
                crowd.ArtistX = performer.DesignPosition.x;
                yield return null;
            }
            crowd.ArtistAtEdge = false;
            performer.SetState(PerformState.Point);
            crowd.PointAt(UnityEngine.Random.value < 0.5f ? 200 : 600);
            Wide();

            yield return Until(ShowPart.BigMoment);
            Enter(ShowPart.BigMoment, 1f);
            performer.SetState(PerformState.BigDrop);
            crowd.Wave(new Vector2(400, 470), FanState.Hype, 500f);
            if (big) crowd.PhoneLights(2.5f);
            lights.BigMomentHit(audio);
            Flash(0.1f);
            Shake(10f);
            Shot(new Vector2(400, 700), 0.96f + (big ? 0 : 0.1f), 3f);
            if (tier >= (int)VenueTier.Arena || popularity >= 70) Celebrate(tier == (int)VenueTier.Stadium);

            yield return Until(ShowPart.Outro);
            Enter(ShowPart.Outro, 0.6f);
            lights.FinaleHit(audio);
            EndPose();
            showRoutine = null;
        }

        /// <summary>End of the show: the artist celebrates and thanks the crowd, hands go up, lights find the artist.</summary>
        public void EndPose()
        {
            performer.SetState(PerformState.End);
            crowd.Intensity = 0.6f;
            lights.SetPart(ShowPart.Outro);
            crowd.Wave(performer.DesignPosition, FanState.HandsUp, 450f, 3f);
            Shot(new Vector2(400, 560 - 330f / 1.15f), 1.15f, 2f, clamp: false);
        }

        public void SkipToEnd()
        {
            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = null;
            CurrentSection = ShowPart.Outro;
            EndPose();
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            time += dt;
            UpdateCamera(dt);
            float bpm = audio != null && audio.Profile != null ? audio.Profile.bpm : look.bpm;
            float beat = ShowTime() * bpm / 60f;
            float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f);
            float intensity = crowd != null ? crowd.Intensity : 1f;

            // big screen: zoom on the beat, glitch for rage/experimental
            var srect = screenRt.rect;
            float size = Mathf.Max(srect.width, srect.height) * (1.02f + 0.03f * pulse);
            portraitRt.sizeDelta = new Vector2(size, size);
            portraitRt.anchoredPosition = look.glitch && UnityEngine.Random.value < 0.06f ? new Vector2(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-4f, 4f)) : Vector2.zero;
            var tint = screenTint.color;
            tint.a = 0.1f + 0.15f * pulse;
            screenTint.color = tint;

            // genre strobes: short and capped so the artist's silhouette always survives
            if (look.strobe && intensity > 0.8f && quality > 0)
            {
                strobeTimer -= dt;
                if (strobeTimer <= 0f && pulse > 0.95f && UnityEngine.Random.value < 0.3f)
                {
                    strobeTimer = 0.45f;
                    strobe.color = new Color(1, 1, 1, 0.08f);
                }
            }
            var sc = strobe.color;
            sc.a = Mathf.MoveTowards(sc.a, 0f, dt * 1.2f);
            strobe.color = sc;
        }

        public void Flash(float alpha) => strobe.color = new Color(1, 1, 1, Mathf.Min(alpha, 0.15f));

        /// <summary>Confetti from the top (arena) or fireworks bursting over the stage (stadium).</summary>
        public void Celebrate(bool fireworks)
        {
            var rt = (RectTransform)transform;
            var colors = new[] { look.palette[0], look.palette[1], Color.white, new Color(1f, 0.82f, 0.24f) };
            int pieces = quality == 0 ? 25 : quality == 1 ? 40 : 60;
            if (fireworks)
            {
                for (int i = 0; i < (quality == 0 ? 2 : 5); i++)
                {
                    var pos = new Vector2((UnityEngine.Random.value - 0.5f) * rt.rect.width * 0.8f, rt.rect.height * (0.25f + UnityEngine.Random.value * 0.18f));
                    UIBurst.Spawn(fxLayer, pos, pieces / 2, confettiSprite, colors);
                }
            }
            else UIBurst.Spawn(fxLayer, new Vector2(0, rt.rect.height * 0.3f), pieces, confettiSprite, colors);
        }
    }
}
