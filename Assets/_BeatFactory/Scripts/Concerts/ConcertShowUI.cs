using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The concert itself: "VEXXO IS PERFORMING..." on a live stage (venue art, crowd, lights, lasers, smoke, phones),
    /// attendance counting up, random events popping in, a finale (confetti / fireworks), then the results card with
    /// animated money, fans, popularity and XP. Tap during the show to skip to the results.
    /// </summary>
    public class ConcertShowUI : MonoBehaviour
    {
        [SerializeField] UISkin skin;
        [SerializeField] Sprite[] venueSprites;
        [SerializeField] Sprite[] crowdSprites;
        [Tooltip("LED screen per venue: x, y, width, height in venue art pixels (800×1400).")]
        [SerializeField] Vector4[] screenRects;
        [SerializeField] VenueLayout[] venueLayouts;
        [SerializeField] Sprite[] fanSprites;
        [SerializeField] Sprite beamSprite;
        [SerializeField] Sprite glowSprite;
        [Tooltip("Crowd atlas (tinted fan parts).")]
        [SerializeField] Texture crowdAtlas;
        [SerializeField] Sprite micSprite;
        [Header("Light show")]
        [SerializeField] Sprite laserCoreSprite;
        [SerializeField] Sprite laserGlowSprite;
        [SerializeField] Sprite flameSprite;
        [SerializeField] Sprite sparkSprite;
        [Tooltip("Pre-rendered stadium seen from a drone (stadium intro).")]
        [SerializeField] Sprite stadiumAerialSprite;

        ConcertAudio concertAudio;

        public static ConcertShowUI Instance { get; private set; }
        public bool IsPlaying => root != null && root.gameObject.activeSelf;

        RectTransform root;
        CanvasGroup group;
        ConcertStageView stage;
        StadiumFlyover flyover;
        TextMeshProUGUI liveText, title, subtitle, counter, counterLabel, calloutTitle, calloutText, vibe;
        Image counterBar, callout;
        RectTransform calloutRt;
        RewardPanel results;
        bool skip;
        ConcertResult current;
        GameObject[] hud;

        public Sprite[] VenueSprites => venueSprites;
        public Sprite[] CrowdSprites => crowdSprites;
        public Vector4[] ScreenRects => screenRects;
        public Sprite BeamSprite => beamSprite;
        public Sprite GlowSprite => glowSprite;
        public Texture CrowdAtlasTexture => crowdAtlas;
        public Sprite MicSprite => micSprite;
        public UISkin Skin => skin;

        /// <summary>Creates a live stage (venue, performer, crowd) with this show's art.</summary>
        public ConcertStageView CreateStage(RectTransform parent) =>
            ConcertStageView.Create(parent, venueSprites, crowdSprites, screenRects, beamSprite, glowSprite,
                skin.confetti != null ? skin.confetti : skin.rounded, crowdAtlas, micSprite, skin,
                laserCoreSprite, laserGlowSprite, flameSprite, sparkSprite, venueLayouts, fanSprites);

        void Awake()
        {
            Instance = this;
            concertAudio = gameObject.AddComponent<ConcertAudio>();
            UIKit.Skin = skin;
            Build();
            root.gameObject.SetActive(false);
        }

        void Build()
        {
            var s = skin;
            root = UIKit.Rect("ConcertShow", transform);
            UIKit.Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            var bg = UIKit.Image("Black", root, Color.black);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var tap = bg.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => skip = true);

            var stageHolder = UIKit.Rect("StageHolder", root);
            UIKit.Stretch(stageHolder);
            stage = CreateStage(stageHolder);
            if (stadiumAerialSprite != null) flyover = StadiumFlyover.Create(root, skin, stadiumAerialSprite, glowSprite, beamSprite);

            // top: LIVE chip + title
            var shade = UIKit.Image("TopShade", root, new Color(0, 0, 0, 0.55f), s.barGradient);
            UIKit.TopRow(shade.rectTransform, 0, 420);
            var live = UIKit.Rounded("Live", root, new Color(1f, 0.2f, 0.3f, 0.95f));
            UIKit.Place(live.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(170, 56));
            liveText = UIKit.Text("LiveText", live.rectTransform, Loc.T("concert.live"), 30, Color.white, s.heading, TextAlignmentOptions.Center, 4);
            title = UIKit.Text("Title", root, "", 74, s.text, s.display, TextAlignmentOptions.Center, 2);
            UIKit.TopRow(title.rectTransform, 190, 100, 40, 40);
            subtitle = UIKit.Text("Subtitle", root, "", 34, s.textDim, s.heading, TextAlignmentOptions.Center, 8);
            UIKit.TopRow(subtitle.rectTransform, 290, 50, 40, 40);
            vibe = UIKit.Text("Vibe", root, "", 28, s.pink, s.heading, TextAlignmentOptions.Center, 10);
            UIKit.TopRow(vibe.rectTransform, 340, 44, 40, 40);

            // bottom: attendance counter
            var box = UIKit.Card("AttendanceCard", root, new Color(0.05f, 0.04f, 0.1f, 0.88f));
            box.rectTransform.anchorMin = new Vector2(0, 0);
            box.rectTransform.anchorMax = new Vector2(1, 0);
            box.rectTransform.pivot = new Vector2(0.5f, 0);
            box.rectTransform.offsetMin = new Vector2(30, 90);
            box.rectTransform.offsetMax = new Vector2(-30, 320);
            counterLabel = UIKit.Text("Label", box.rectTransform, Loc.T("concert.attendance"), 30, s.textDim, s.heading, TextAlignmentOptions.Center, 10);
            UIKit.TopRow(counterLabel.rectTransform, 22, 40);
            counter = UIKit.Text("Counter", box.rectTransform, "0 / 0", 80, s.text, s.display, TextAlignmentOptions.Center, 2);
            UIKit.TopRow(counter.rectTransform, 64, 96);
            counterBar = UIKit.Bar("Bar", box.rectTransform, s.cyan, out var track);
            UIKit.TopRow(track.rectTransform, 176, 24, 60, 60);

            // event callout
            callout = UIKit.Card("Callout", root, new Color(0.05f, 0.04f, 0.1f, 0.92f));
            calloutRt = callout.rectTransform;
            calloutRt.anchorMin = new Vector2(0, 0.27f);
            calloutRt.anchorMax = new Vector2(1, 0.27f);
            calloutRt.pivot = new Vector2(0.5f, 0.5f);
            calloutRt.offsetMin = new Vector2(40, 120 - 130);
            calloutRt.offsetMax = new Vector2(-40, 120 + 130);
            calloutTitle = UIKit.Text("Title", calloutRt, "", 76, s.fame, s.display, TextAlignmentOptions.Center, 3);
            UIKit.TopRow(calloutTitle.rectTransform, 24, 100, 30, 30);
            calloutText = UIKit.Text("Text", calloutRt, "", 30, s.text, s.body, TextAlignmentOptions.Center);
            UIKit.TopRow(calloutText.rectTransform, 130, 110, 40, 40);
            calloutText.textWrappingMode = TextWrappingModes.Normal;
            callout.gameObject.SetActive(false);
            hud = new[] { shade.gameObject, box.gameObject, vibe.gameObject };

            results = RewardPanel.Build(root, Loc.T("ui.collect"));
            results.collect.onClick.AddListener(Close);
            results.root.gameObject.SetActive(false);
        }

        public void Play(ConcertResult r)
        {
            if (r == null) return;
            current = r;
            UIKit.Skin = skin;
            StopAllCoroutines();
            if (flyover != null) flyover.gameObject.SetActive(false);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            transform.SetAsLastSibling();
            StartCoroutine(Show(r));
        }

        IEnumerator Show(ConcertResult r)
        {
            skip = false;
            var s = skin;
            var look = GenreStage.For(r.artist.genre);
            // the artist's own original soundtrack renders in the background while we fade in
            concertAudio.Prepare(r.artist, r.popularityBefore);
            stage.Setup(r.venue.tier, r.artist, r.popularityBefore, r.fill, concertAudio);
            foreach (var h in hud) h.SetActive(true);
            results.root.gameObject.SetActive(false);
            callout.gameObject.SetActive(false);
            liveText.text = Loc.T("concert.live");
            title.text = Loc.F("concert.performing", "artist", r.artist.displayName.ToUpperInvariant());
            subtitle.text = r.venue.Name + "  ·  " + r.city.ToUpperInvariant();
            string vibeText = Loc.Or("concert.vibe." + r.artist.genre.ToString().ToLowerInvariant(), look.vibe);
            vibe.text = vibeText;
            vibe.color = look.palette[0];
            counter.text = "0 / " + N(r.capacity);
            counterBar.fillAmount = 0f;
            counterBar.color = r.venue.color;
            counterLabel.text = Loc.T("concert.attendance");

            // stadium: the drone intro flies in while the soundtrack renders; it lands on the stage and the music starts
            bool intro = r.venue.tier == VenueTier.Stadium && flyover != null;
            if (intro)
            {
                float bpm = r.artist != null ? ArtistConcertMusic.For(r.artist).bpm : look.bpm;
                StartCoroutine(flyover.Play(r.artist, r.venue.Name + "\n" + r.city.ToUpperInvariant(), look.palette, bpm, stage.RevealFromIntro));
            }
            // fade in → concert intro → music starts
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 1f;
            if (intro) while (!flyover.Revealed) yield return null;
            yield return concertAudio.Begin();
            if (concertAudio.Profile != null) vibe.text = vibeText + "  ·  " + Loc.Upper(concertAudio.Profile.FlavorText);

            // the show follows the soundtrack (sections, camera, lights, crowd); attendance fills up, events pop in
            stage.RunShow();
            float length = stage.ShowLength;
            float arrive = Mathf.Min(4.5f, length * 0.35f);
            var events = r.events.FindAll(e => e != ConcertEvent.SoldOut);
            float[] eventTimes =
            {
                (concertAudio.Track != null ? concertAudio.Track.Start(ShowPart.BuildUp) : 4.3f) - 0.9f,
                (concertAudio.Track != null ? concertAudio.Track.Start(ShowPart.Chorus) : 7.8f) + 0.4f,
                (concertAudio.Track != null ? concertAudio.Track.Start(ShowPart.BigMoment) : 10.6f) - 0.6f,
            };
            int nextEvent = 0;
            float elapsed = 0;
            while (!skip)
            {
                float t = concertAudio.Track != null ? concertAudio.Time : elapsed;
                elapsed += Time.unscaledDeltaTime;
                if (t >= length || elapsed > length + 3f) break;
                float k = Mathf.Clamp01(t / arrive);
                int shown = Mathf.RoundToInt(r.attendance * (1f - Mathf.Pow(1f - k, 2f)));
                counter.text = N(shown) + " / " + N(r.capacity);
                counterBar.fillAmount = shown / (float)r.capacity;
                if (t >= arrive) counterLabel.text = Loc.F("concert.attendance_pct", "value", NumberFormatter.Percent(r.fill * 100f)) + "  ·  " + r.AttendanceLabel;
                liveText.alpha = 0.6f + 0.4f * Mathf.Sin(t * 6f);
                if (nextEvent < events.Count && nextEvent < eventTimes.Length && t >= eventTimes[nextEvent])
                    StartCoroutine(Callout(events[nextEvent++]));
                yield return null;
            }
            if (skip)
            {
                // jump to the end: the outro plays, the music fades instead of being cut
                if (concertAudio.Track != null) concertAudio.Seek(concertAudio.Track.Start(ShowPart.Outro));
                stage.SkipToEnd();
            }
            counter.text = N(r.attendance) + " / " + N(r.capacity);
            counterBar.fillAmount = r.fill;
            counterLabel.text = Loc.F("concert.attendance_pct", "value", NumberFormatter.Percent(r.fill * 100f)) + "  ·  " + r.AttendanceLabel;

            // music fades out → the crowd keeps cheering → results
            concertAudio.FadeOut(2.2f);
            if (!skip && r.soldOut) yield return Callout(ConcertEvent.SoldOut);
            if (r.soldOut && r.venue.tier < VenueTier.Arena) stage.Celebrate(false);
            callout.gameObject.SetActive(false);
            foreach (var h in hud) h.SetActive(false);

            yield return ShowResults(r);
        }

        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;
        static string N(int v) => NumberFormatter.Group(v);

        IEnumerator Callout(ConcertEvent ev)
        {
            var c = ConcertRewardCalculator.EventColor(ev);
            calloutTitle.text = ConcertRewardCalculator.EventTitle(ev);
            calloutTitle.color = c;
            calloutText.text = ConcertRewardCalculator.EventText(ev);
            callout.gameObject.SetActive(true);
            AudioManager.Play(ev == ConcertEvent.ViralMoment ? SoundId.Viral : ev == ConcertEvent.LowEnergy ? SoundId.Click : SoundId.Hit);
            if (ev == ConcertEvent.ViralMoment || ev == ConcertEvent.CrowdGoesCrazy) stage.Flash(0.35f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                float s = Mathf.LerpUnclamped(0.5f, 1f, 1f + 2.7f * Mathf.Pow(t - 1f, 3) + 1.7f * Mathf.Pow(t - 1f, 2));
                calloutRt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            calloutRt.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(skip ? 0.1f : 1f);
            callout.gameObject.SetActive(false);
        }

        IEnumerator ShowResults(ConcertResult r)
        {
            var s = skin;
            var c = GameManager.Instance.Concerts.Career(r.artist);
            results.Clear();
            results.header.text = r.soldOut ? Loc.F("concert.venue_sold_out", "venue", r.venue.Name) : Loc.T("concert.complete");
            results.title.text = r.artist.displayName;
            results.subtitle.text = r.venue.Name + "  ·  " + r.city.ToUpperInvariant();
            results.attendance.text = N(r.attendance) + " / " + N(r.capacity);
            results.attendanceLabel.text = Loc.F("concert.sold_energy", "sold", NumberFormatter.Percent(r.fill * 100f), "energy", NumberFormatter.Percent(r.crowdEnergy * 100f));
            results.attendanceLabel.color = r.soldOut ? s.fame : r.fill >= 0.6f ? s.cyan : s.textDim;
            results.glow.color = new Color(r.venue.color.r, r.venue.color.g, r.venue.color.b, 0.25f);
            results.AddRow(Loc.T("concert.row_revenue"), s.money, r.Revenue, RewardPanel.Money,
                Loc.F("concert.revenue_detail", "tickets", NumberFormatter.FormatMoney(r.ticketRevenue), "count", N(r.attendance),
                    "price", NumberFormatter.Money(NumberFormatter.Group(r.ticketPrice)), "extra", NumberFormatter.FormatMoney(r.extraRevenue)));
            results.AddRow(Loc.T("concert.row_fans"), s.pink, r.fans, RewardPanel.Plain, Loc.F("concert.total", "value", Loc.P("count.fans", c.fans)));
            results.AddRow(Loc.T("concert.row_popularity"), s.fame, r.popularity, RewardPanel.Percent,
                r.popularity <= 0.001f ? Loc.T("concert.studio_limit_reached") : Loc.F("concert.popularity_now", "value", UIKit.Pct(c.popularity), "cap", UIKit.Pct(GameManager.Instance.Concerts.PopularityCap)));
            results.AddRow(Loc.T("concert.row_xp"), s.cyan, r.xp, RewardPanel.Int,
                c.MaxConcertLevel ? Loc.F("concert.level_max", "level", c.ConcertLevel)
                    : Loc.F("concert.level_progress", "level", c.ConcertLevel, "xp", NumberFormatter.Group(c.LevelXp), "need", NumberFormatter.Group(c.LevelXpNeeded)));
            string chips = "";
            if (r.levelAfter > r.levelBefore) chips += "<color=#4DFFA8>" + Loc.F("concert.level_up", "level", r.levelAfter) + "</color>   ";
            if (r.stageAfter > r.stageBefore) chips += "<color=#FFD23D>" + Loc.F("concert.new_status", "status", Loc.Or("career.stage" + r.stageAfter, ArtistCareer.Stages[r.stageAfter])) + "</color>";
            results.chips.text = chips;
            results.collectLabel.text = Loc.T("ui.collect");
            results.root.gameObject.SetActive(true);
            results.root.SetAsLastSibling();
            yield return RewardPanel.SlideIn(results.root, results.group);
            yield return results.CountUp();
            if (r.levelAfter > r.levelBefore || r.stageAfter > r.stageBefore)
            {
                AudioManager.Play(SoundId.Unlock);
                UIBurst.Spawn(results.root, new Vector2(0, 300), 40, skin.confetti != null ? skin.confetti : skin.rounded);
            }
        }

        void Close()
        {
            AudioManager.Play(SoundId.Collect);
            results.ShowFinal();
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            root.gameObject.SetActive(false);
            group.alpha = 1f;
            concertAudio.Stop();
            ConcertsUIEvents.RaiseShowClosed();
        }
    }

    /// <summary>Tiny UI event hub so screens can refresh after the concert overlay closes.</summary>
    public static class ConcertsUIEvents
    {
        public static event System.Action ShowClosed;
        public static void RaiseShowClosed() => ShowClosed?.Invoke();
    }
}
