using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// "WELCOME BACK — VEXXO FINISHED THE TOUR": appears when a tour ends (also for tours that ended while the game was
    /// closed). Shows the stadium/arena backdrop, the shows played and counts up revenue, fans, popularity and XP.
    /// </summary>
    public class TourCompletePopup : MonoBehaviour
    {
        [SerializeField] UISkin skin;
        [SerializeField] ConcertShowUI show;

        readonly Queue<ActiveTour> queue = new Queue<ActiveTour>();
        RectTransform root;
        CanvasGroup group;
        ConcertStageView stage;
        RewardPanel panel;
        ModalWindow[] modals;
        float startTime;
        bool showing;
        bool firstAfterLoad = true;

        public static TourCompletePopup Instance { get; private set; }
        public bool IsOpen => root != null && root.gameObject.activeSelf;

        void Awake()
        {
            Instance = this;
            UIKit.Skin = skin;
        }

        void Start()
        {
            var gm = GameManager.Instance;
            Build();
            root.gameObject.SetActive(false);
            modals = FindObjectsByType<ModalWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            gm.Tours.TourFinished += t => { if (!queue.Contains(t)) queue.Enqueue(t); };
            startTime = Time.unscaledTime;
        }

        void Build()
        {
            var s = skin;
            root = UIKit.Rect("TourComplete", transform);
            UIKit.Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            var bg = UIKit.Image("Black", root, Color.black);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var holder = UIKit.Rect("StageHolder", root);
            UIKit.Stretch(holder);
            stage = show.CreateStage(holder);
            var dim = UIKit.Image("Dim", root, new Color(0, 0, 0, 0.35f));
            UIKit.Stretch(dim.rectTransform);
            panel = RewardPanel.Build(root, Loc.T("ui.collect"));
            panel.collect.onClick.AddListener(Close);
        }

        bool Busy()
        {
            if (show != null && show.IsPlaying) return true;
            if (modals != null) foreach (var m in modals) if (m != null && m.IsOpen) return true;
            return false;
        }

        void Update()
        {
            if (showing || queue.Count == 0 || Time.unscaledTime - startTime < 2f || Busy()) return;
            var t = queue.Dequeue();
            if (GameManager.Instance.Tours.ActiveFor(GameManager.Instance.Tours.ArtistOf(t)) != t) return; // already collected
            StartCoroutine(Show(t));
        }

        IEnumerator Show(ActiveTour t)
        {
            showing = true;
            UIKit.Skin = skin;
            var gm = GameManager.Instance;
            var artist = gm.Tours.ArtistOf(t);
            var data = Tours.Get(t.Kind);
            var career = artist != null ? gm.Concerts.Career(artist) : null;
            float popBefore = career != null ? career.popularity : 0;
            int levelBefore = career != null ? career.ConcertLevel : 1;
            gm.Tours.Collect(t); // rewards are paid right away (safe if the app closes)

            var s = skin;
            stage.Setup(data.venue, artist, career != null ? career.popularity : 50f, t.capacity > 0 ? t.attendance / (float)t.capacity : 1f);
            stage.Crowd.Intensity = 0.9f;
            stage.EndPose();
            root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            panel.Clear();
            bool welcome = firstAfterLoad && gm.SecondsAway > 60;
            firstAfterLoad = false;
            panel.header.text = Loc.T(welcome ? "tour.welcome_back" : "tour.complete");
            panel.title.text = artist != null ? artist.displayName : Loc.T("tour.tour");
            panel.subtitle.text = Loc.F("tour.finished", "title", t.Title);
            panel.attendance.text = Loc.F("tour.shows_completed", "done", t.shows, "total", t.shows);
            panel.attendanceLabel.text = Loc.F("tour.fans_in_cities", "fans", Loc.P("count.fans", t.attendance), "cities", Loc.P("count.cities", t.cities.Count)) + (t.albumBonus ? "  ·  " + Loc.T("tour.album_bonus") : "");
            panel.attendanceLabel.color = t.albumBonus ? s.gold : s.cyan;
            panel.glow.color = new Color(Venues.Get(data.venue).color.r, Venues.Get(data.venue).color.g, Venues.Get(data.venue).color.b, 0.25f);
            panel.AddRow(Loc.T("concert.row_revenue"), s.money, t.revenue, RewardPanel.Money, string.Join(" · ", t.cities.GetRange(0, Mathf.Min(4, t.cities.Count))) + (t.cities.Count > 4 ? " …" : ""));
            panel.AddRow(Loc.T("concert.row_fans"), s.pink, t.fans, RewardPanel.Plain, career != null ? Loc.F("concert.total", "value", Loc.P("count.fans", career.fans)) : "");
            panel.AddRow(Loc.T("concert.row_popularity"), s.fame, t.popularity, RewardPanel.Percent, career != null ? UIKit.Pct(popBefore) + "  →  " + UIKit.Pct(career.popularity) : "");
            panel.AddRow(Loc.T("concert.row_xp"), s.cyan, t.xp, RewardPanel.Int, career != null ? Loc.F("concert.level_name", "level", career.ConcertLevel) : "");
            string chips = "";
            if (career != null && career.ConcertLevel > levelBefore) chips += "<color=#4DFFA8>" + Loc.F("concert.level_up", "level", career.ConcertLevel) + "</color>   ";
            if (t.Kind >= TourKind.World) chips += "<color=#FFD23D>" + (career != null ? career.Stage : Loc.T("career.stage6")) + "</color>";
            panel.chips.text = chips;

            AudioManager.Play(SoundId.StudioTransition);
            for (float x = 0; x < 1f; x += Time.unscaledDeltaTime / 0.35f)
            {
                group.alpha = x;
                yield return null;
            }
            group.alpha = 1f;
            stage.Celebrate(data.venue == VenueTier.Stadium);
            yield return RewardPanel.SlideIn(panel.root, panel.group);
            yield return panel.CountUp(0.6f);
        }

        void Close()
        {
            StopAllCoroutines();
            panel.ShowFinal();
            AudioManager.Play(SoundId.Collect);
            UIBurst.Spawn(panel.root, new Vector2(0, 700), 50, skin.confetti != null ? skin.confetti : skin.rounded);
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            root.gameObject.SetActive(false);
            showing = false;
            ConcertsUIEvents.RaiseShowClosed();
        }

        /// <summary>Opens a finished tour right away (COLLECT on the career screen).</summary>
        public void ShowNow(ActiveTour t)
        {
            if (showing) return;
            StartCoroutine(Show(t));
        }
    }
}
