using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// GOALS: MISSIONS (career goals by category + career history) · DAILY · WEEKLY challenges.
    /// Progress bars update live; rewards are claimed manually with a short reveal animation. Built from code (UIKit).
    /// </summary>
    public class GoalsScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;

        public static GoalsScreen Instance { get; private set; }
        public bool IsOpen => root != null && root.gameObject.activeSelf;

        enum Tab { Missions, Daily, Weekly }

        RectTransform root, list, tabsRow;
        CanvasGroup group;
        readonly List<(Button button, Image bg, TextMeshProUGUI label)> tabs = new List<(Button, Image, TextMeshProUGUI)>();
        readonly List<Action> refreshers = new List<Action>();
        Tab tab = Tab.Missions;
        GameManager gm;
        float refreshTimer, openTime;
        bool dirty;

        static readonly Dictionary<MissionCategory, Color> CategoryColors = new Dictionary<MissionCategory, Color>
        {
            { MissionCategory.Production, new Color(0.24f, 0.88f, 1f) },
            { MissionCategory.Artists, new Color(1f, 0.24f, 0.78f) },
            { MissionCategory.Releases, new Color(0.71f, 0.36f, 1f) },
            { MissionCategory.Concerts, new Color(1f, 0.54f, 0.24f) },
            { MissionCategory.Studio, new Color(0.3f, 1f, 0.66f) },
            { MissionCategory.Career, new Color(0.95f, 0.76f, 0.31f) },
        };

        void Awake()
        {
            Instance = this;
            UIKit.Skin = skin;
            Build();
            root.gameObject.SetActive(false);
        }

        void Start()
        {
            gm = GameManager.Instance;
            gm.Missions.Changed += MarkDirty;
            gm.Challenges.Changed += MarkDirty;
            Loc.Changed += MarkDirty;
        }

        void MarkDirty() => dirty = true;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Loc.Changed -= MarkDirty; // static event: must be removed or the destroyed screen stays referenced after a reset
            if (gm != null) { gm.Missions.Changed -= MarkDirty; gm.Challenges.Changed -= MarkDirty; }
        }

        void Build()
        {
            var s = skin;
            root = UIKit.Rect("Goals", transform);
            UIKit.Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            var bg = UIKit.Image("Background", root, s.background);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var glow = UIKit.Image("Glow", root, new Color(0.71f, 0.36f, 1f, 0.25f), s.radial);
            UIKit.Place(glow.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(1500, 900));

            var title = UIKit.Text("Title", root, "", 96, s.text, s.display, TextAlignmentOptions.BottomLeft, 4);
            UIKit.TopRow(title.rectTransform, 70, 120, 50, 200);
            title.gameObject.AddComponent<LocalizedText>().Setup("goals.title");
            var sub = UIKit.Text("Sub", root, "", 28, s.textDim, s.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(sub.rectTransform, 194, 40, 52, 40);
            sub.gameObject.AddComponent<LocalizedText>().Setup("goals.subtitle");
            var close = UIKit.Button("Close", root, s.cardLight, "X", 46, out var x);
            x.color = s.text;
            ((Image)close.targetGraphic).sprite = s.circle;
            ((Image)close.targetGraphic).type = Image.Type.Simple;
            UIKit.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -70), new Vector2(104, 104));
            close.onClick.AddListener(Hide);

            tabsRow = UIKit.Rect("Tabs", root);
            UIKit.TopRow(tabsRow, 260, 100, 40, 40);
            string[] names = { "MISSIONS", "DAILY", "WEEKLY" };
            for (int i = 0; i < names.Length; i++)
            {
                var b = UIKit.Button("Tab" + names[i], tabsRow, s.cardLight, names[i], 34, out var label);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = new Vector2(i / 3f, 0);
                rt.anchorMax = new Vector2((i + 1) / 3f, 1);
                rt.offsetMin = new Vector2(i == 0 ? 0 : 8, 0);
                rt.offsetMax = new Vector2(i == 2 ? 0 : -8, 0);
                int idx = i;
                b.onClick.AddListener(() => { tab = (Tab)idx; Rebuild(); list.anchoredPosition = Vector2.zero; });
                tabs.Add((b, (Image)b.targetGraphic, label));
            }

            var scrollArea = UIKit.Rect("ScrollArea", root);
            UIKit.Stretch(scrollArea, 0, 0, 380, 0);
            list = UIKit.ScrollList("List", scrollArea, 20, new RectOffset(40, 40, 10, 140));
        }

        public void Show()
        {
            gm = GameManager.Instance;
            UIKit.Skin = skin;
            gm.Challenges.Refresh();
            root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            openTime = Time.unscaledTime;
            // open where the action is: a claimable tab first
            if (gm.Missions.ClaimableCount == 0 && gm.Challenges.ClaimableCount > 0)
            {
                bool dailyClaim = false;
                foreach (var c in gm.Challenges.Daily) if (c.completed && !c.claimed) dailyClaim = true;
                tab = dailyClaim ? Tab.Daily : Tab.Weekly;
            }
            Rebuild();
            list.anchoredPosition = Vector2.zero;
            AudioManager.Play(SoundId.Click);
        }

        public void Hide() => root.gameObject.SetActive(false);

        void Update()
        {
            if (!IsOpen) return;
            group.alpha = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.25f);
            if (dirty) { dirty = false; Rebuild(); return; }
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0) return;
            refreshTimer = 0.5f;
            foreach (var r in refreshers) r();
        }

        // ------------------------------------------------------------------ content

        void Rebuild()
        {
            UIKit.Skin = skin;
            refreshers.Clear();
            UIKit.Clear(list);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = i == (int)tab;
                tabs[i].bg.color = on ? skin.pink : skin.cardLight;
                tabs[i].label.color = on ? skin.background : skin.text;
                int claim = i == 0 ? gm.Missions.ClaimableCount : CountClaim(i == 1 ? gm.Challenges.Daily : gm.Challenges.Weekly);
                string baseName = Loc.T(i == 0 ? "goals.tab_missions" : i == 1 ? "goals.tab_daily" : "goals.tab_weekly");
                tabs[i].label.text = claim > 0 ? baseName + "  <color=#FFD23D>●" + claim + "</color>" : baseName;
            }
            if (tab == Tab.Missions) BuildMissions();
            else BuildChallenges(tab == Tab.Weekly);
            foreach (var r in refreshers) r();
        }

        static int CountClaim(IReadOnlyList<ChallengeState> set)
        {
            int n = 0;
            foreach (var c in set) if (c.completed && !c.claimed) n++;
            return n;
        }

        void Header(string text, Color color, string sub = null)
        {
            var b = UIKit.Block("Header_" + text, list, sub != null ? 100 : 76);
            var dot = UIKit.Image("Dot", b, color, skin.circle);
            UIKit.Place(dot.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(4, -44), new Vector2(18, 18));
            var t = UIKit.Text("T", b, text, 38, skin.text, skin.heading, TextAlignmentOptions.BottomLeft, 10);
            UIKit.TopRow(t.rectTransform, 10, 52, 34, 0);
            if (sub != null)
            {
                var st = UIKit.Text("S", b, sub, 24, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
                UIKit.TopRow(st.rectTransform, 64, 34, 34, 0);
            }
        }

        // ---- missions

        void BuildMissions()
        {
            int done = 0, total = MissionCatalog.All.Length;
            foreach (var x in MissionCatalog.All) if (gm.Missions.IsCompleted(x)) done++;
            var summary = UIKit.Block("Summary", list, 170);
            var sbg = UIKit.Card("Bg", summary, skin.card);
            UIKit.Stretch(sbg.rectTransform);
            var st = UIKit.Text("Title", summary, Loc.T("goals.career_progress"), 28, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 8);
            UIKit.TopRow(st.rectTransform, 24, 40, 36, 36);
            var sv = UIKit.Text("Value", summary, Loc.F("goals.completed_count", "done", done, "total", total), 52, skin.gold, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(sv.rectTransform, 62, 64, 36, 36);
            var bar = UIKit.Bar("Bar", summary, skin.gold, out var track);
            UIKit.TopRow(track.rectTransform, 136, 16, 36, 36);
            bar.fillAmount = done / (float)total;

            foreach (MissionCategory c in Enum.GetValues(typeof(MissionCategory)))
            {
                var visible = gm.Missions.Visible(c);
                if (visible.Count == 0) continue;
                Header(MissionCatalog.CategoryName(c), CategoryColors[c]);
                foreach (var m in visible) MissionCard(m, CategoryColors[c]);
            }

            var history = gm.Missions.History();
            if (history.Count > 0)
            {
                Header(Loc.T("goals.history"), skin.textDim, Loc.F("goals.history_count", "count", history.Count));
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    var m = history[i];
                    var b = UIKit.Block("Done_" + m.id, list, 84);
                    var rb = UIKit.Rounded("Bg", b, new Color(1, 1, 1, 0.04f));
                    UIKit.Stretch(rb.rectTransform);
                    var check = UIKit.Text("Check", b, "✓", 36, CategoryColors[m.category], skin.heading, TextAlignmentOptions.Center);
                    UIKit.Place(check.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(50, 60));
                    var t = UIKit.Text("Title", b, m.Title + "   <color=#8A84B0><size=75%>" + m.Description + "</size></color>", 28, skin.text, skin.heading, TextAlignmentOptions.Left, 2);
                    UIKit.Stretch(t.rectTransform, 80, 24, 0, 0);
                }
            }
        }

        void MissionCard(MissionDefinition m, Color color)
        {
            var b = UIKit.Block("Mission_" + m.id, list, 210);
            var bg = UIKit.Card("Bg", b, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var stripe = UIKit.Image("Stripe", b, color);
            stripe.rectTransform.anchorMin = new Vector2(0, 0);
            stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.offsetMin = new Vector2(16, 22);
            stripe.rectTransform.offsetMax = new Vector2(24, -22);
            var title = UIKit.Text("Title", b, m.Title, 40, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(title.rectTransform, 22, 52, 50, 330);
            var desc = UIKit.Text("Desc", b, m.Description, 26, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(desc.rectTransform, 76, 36, 50, 330);
            var fill = UIKit.Bar("Progress", b, color, out var track);
            UIKit.TopRow(track.rectTransform, 128, 22, 50, 330);
            var progress = UIKit.Text("ProgressText", b, "", 24, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(progress.rectTransform, 158, 34, 50, 330);
            var reward = UIKit.Text("Reward", b, m.reward.Describe(gm), 30, m.reward.Color(skin), skin.heading, TextAlignmentOptions.TopRight, 2);
            UIKit.Place(reward.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -26), new Vector2(290, 44));
            var claim = UIKit.Button("Claim", b, skin.gold, Loc.T("ui.claim"), 44, out var claimLabel);
            UIKit.Place((RectTransform)claim.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-26, 22), new Vector2(270, 110));
            claim.onClick.AddListener(() =>
            {
                if (!gm.Missions.Claim(m)) return;
                StartCoroutine(Reveal(b, m.reward.Describe(gm), color));
            });

            refreshers.Add(() =>
            {
                bool complete = gm.Missions.IsCompleted(m);
                fill.fillAmount = complete ? 1f : gm.Missions.Progress01(m);
                progress.text = complete ? "<color=#4DFFA8>" + Loc.T("ui.complete") + "</color>" : m.format(gm.Missions.Progress(m)) + " / " + m.format(m.target);
                claim.gameObject.SetActive(complete && !gm.Missions.IsClaimed(m));
                bg.color = complete ? Color.Lerp(skin.card, color, 0.12f) : skin.card;
            });
        }

        // ---- challenges

        void BuildChallenges(bool weekly)
        {
            var set = weekly ? gm.Challenges.Weekly : gm.Challenges.Daily;
            var head = UIKit.Block("ResetCard", list, 170);
            var hbg = UIKit.Card("Bg", head, skin.card);
            UIKit.Stretch(hbg.rectTransform);
            var ht = UIKit.Text("Title", head, Loc.T(weekly ? "goals.weekly_title" : "goals.daily_title"), 30, weekly ? skin.pink : skin.cyan, skin.heading, TextAlignmentOptions.TopLeft, 8);
            UIKit.TopRow(ht.rectTransform, 24, 44, 36, 36);
            var timer = UIKit.Text("Timer", head, "", 48, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(timer.rectTransform, 66, 60, 36, 36);
            var note = UIKit.Text("Note", head, Loc.T("goals.note"), 22, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(note.rectTransform, 128, 32, 36, 36);
            refreshers.Add(() =>
            {
                var span = weekly ? gm.Challenges.TimeToWeeklyReset : gm.Challenges.TimeToDailyReset;
                timer.text = Loc.F("goals.new_in", "time", span.TotalHours >= 24 ? NumberFormatter.FormatDuration(span) : NumberFormatter.FormatClock(span.TotalSeconds));
            });

            foreach (var c in set) ChallengeCard(c, weekly);
            if (set.Count == 0)
            {
                var e = UIKit.Block("Empty", list, 90);
                UIKit.Text("T", e, Loc.T("goals.empty"), 28, skin.textDim, skin.body);
            }
        }

        void ChallengeCard(ChallengeState c, bool weekly)
        {
            var t = ChallengeManager.Find(c.templateId);
            if (t == null) return;
            var color = weekly ? skin.pink : skin.cyan;
            var b = UIKit.Block("Challenge_" + c.templateId, list, 200);
            var bg = UIKit.Card("Bg", b, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var stripe = UIKit.Image("Stripe", b, color);
            stripe.rectTransform.anchorMin = new Vector2(0, 0);
            stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.offsetMin = new Vector2(16, 22);
            stripe.rectTransform.offsetMax = new Vector2(24, -22);
            var title = UIKit.Text("Title", b, Loc.Upper(ChallengeManager.Title(c)), 40, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(title.rectTransform, 24, 52, 50, 330);
            var fill = UIKit.Bar("Progress", b, color, out var track);
            UIKit.TopRow(track.rectTransform, 96, 22, 50, 330);
            var progress = UIKit.Text("ProgressText", b, "", 26, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(progress.rectTransform, 128, 36, 50, 330);
            var reward = UIKit.Text("Reward", b, t.reward.Describe(gm), 30, t.reward.Color(skin), skin.heading, TextAlignmentOptions.TopRight, 2);
            UIKit.Place(reward.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -26), new Vector2(290, 44));
            var claim = UIKit.Button("Claim", b, skin.gold, Loc.T("ui.claim"), 44, out _);
            UIKit.Place((RectTransform)claim.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-26, 22), new Vector2(270, 110));
            claim.onClick.AddListener(() =>
            {
                string text = gm.Challenges.Claim(c);
                if (text != null) StartCoroutine(Reveal(b, text, color));
            });
            refreshers.Add(() =>
            {
                fill.fillAmount = c.completed ? 1f : gm.Challenges.Progress01(c);
                progress.text = c.claimed ? "<color=#8A84B0>" + Loc.T("ui.claimed") + " ✓</color>" : c.completed ? "<color=#4DFFA8>" + Loc.T("ui.complete") + "</color>"
                    : (t.format != null ? t.format(gm.Challenges.Progress(c)) : NumberFormatter.Format(gm.Challenges.Progress(c))) + " / " + (t.format != null ? t.format(c.target) : NumberFormatter.Format(c.target));
                claim.gameObject.SetActive(c.completed && !c.claimed);
                bg.color = c.claimed ? new Color(skin.card.r, skin.card.g, skin.card.b, 0.6f) : c.completed ? Color.Lerp(skin.card, color, 0.12f) : skin.card;
            });
        }

        // ---- reward reveal: confetti + the reward rising from the card

        IEnumerator Reveal(RectTransform card, string text, Color color)
        {
            AudioManager.Play(SoundId.Collect);
            UIBurst.Spawn(root, root.InverseTransformPoint(card.position), 40, skin.confetti != null ? skin.confetti : skin.rounded,
                new[] { color, skin.gold, Color.white, skin.pink });
            var label = UIKit.Text("Reveal", root, text, 64, skin.gold, skin.display, TextAlignmentOptions.Center, 2);
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(900, 100);
            Vector2 start = root.InverseTransformPoint(card.position);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.1f)
            {
                rt.anchoredPosition = start + new Vector2(0, 160 * t);
                float s = t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.3f);
                rt.localScale = Vector3.one * s;
                label.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                yield return null;
            }
            Destroy(label.gameObject);
            dirty = true;
        }
    }
}
