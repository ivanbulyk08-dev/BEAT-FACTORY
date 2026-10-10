using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// An artist's career page: portrait, status, stats (popularity, fans, concert level, performance, ticket price),
    /// BOOK A CONCERT venue cards, TOURS, CONCERT HISTORY and ACHIEVEMENTS. Built from code (UIKit).
    /// </summary>
    public class ArtistCareerScreen : MonoBehaviour
    {
        [SerializeField] UISkin skin;
        [SerializeField] ConcertShowUI show;

        public static ArtistCareerScreen Instance { get; private set; }
        public bool IsOpen => root != null && root.gameObject.activeSelf;

        RectTransform root, list, portraitHolder;
        CanvasGroup group;
        Image portrait, ring, glow, headerGlow;
        TextMeshProUGUI nameText, stageText, genreText, statusText;
        Image statusPill;
        readonly List<Action> refreshers = new List<Action>();
        ArtistData artist;
        float refreshTimer, openTime;
        GameManager gm;

        void Awake()
        {
            Instance = this;
            UIKit.Skin = skin;
            Build();
            root.gameObject.SetActive(false);
            ConcertsUIEvents.ShowClosed += RebuildIfOpen;
        }

        void Start()
        {
            gm = GameManager.Instance;
            gm.Tours.TourCollected += OnTourCollected;
            Loc.Changed += RebuildIfOpen;
        }

        void RebuildIfOpen() { if (IsOpen) Rebuild(); }
        void OnTourCollected(ActiveTour _) => RebuildIfOpen();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // static events: remove handlers so a destroyed screen is not kept alive after a progress reset
            ConcertsUIEvents.ShowClosed -= RebuildIfOpen;
            Loc.Changed -= RebuildIfOpen;
            if (gm != null) gm.Tours.TourCollected -= OnTourCollected;
        }

        // ------------------------------------------------------------------ static frame

        void Build()
        {
            var s = skin;
            root = UIKit.Rect("ArtistCareer", transform);
            UIKit.Stretch(root);
            group = root.gameObject.AddComponent<CanvasGroup>();
            var bg = UIKit.Image("Background", root, s.background);
            UIKit.Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            headerGlow = UIKit.Image("GenreGlow", root, new Color(1, 0.3f, 0.8f, 0.35f), s.radial);
            UIKit.Place(headerGlow.rectTransform, new Vector2(0.3f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(1400, 1100));

            const float headerH = 640;
            var header = UIKit.Rect("Header", root);
            UIKit.TopRow(header, 0, headerH);

            portraitHolder = UIKit.Rect("PortraitHolder", header);
            UIKit.Place(portraitHolder, new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -130), new Vector2(300, 300));
            glow = UIKit.Glow("Glow", portraitHolder, new Color(1, 0.3f, 0.8f, 0.4f), 70);
            portrait = UIKit.Image("Portrait", portraitHolder, Color.white);
            portrait.preserveAspect = true;
            UIKit.Stretch(portrait.rectTransform);
            ring = UIKit.Image("Ring", portraitHolder, Color.white, s.ring);
            UIKit.Stretch(ring.rectTransform, -12, -12, -12, -12);

            nameText = UIKit.Text("Name", header, "ARTIST", 110, s.text, s.display, TextAlignmentOptions.BottomLeft, 2);
            UIKit.TopRow(nameText.rectTransform, 150, 140, 400, 40);
            stageText = UIKit.Text("Stage", header, "", 34, s.gold, s.heading, TextAlignmentOptions.TopLeft, 8);
            UIKit.TopRow(stageText.rectTransform, 300, 50, 400, 40);
            genreText = UIKit.Text("Genre", header, "", 28, s.textDim, s.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.TopRow(genreText.rectTransform, 356, 44, 400, 40);

            statusPill = UIKit.Rounded("Status", header, new Color(1, 1, 1, 0.08f));
            UIKit.TopRow(statusPill.rectTransform, 480, 104, 50, 50);
            statusText = UIKit.Text("StatusText", statusPill.rectTransform, "", 38, s.text, s.heading, TextAlignmentOptions.Center, 6);
            UIKit.Stretch(statusText.rectTransform, 20, 20, 8, 8);

            var close = UIKit.Button("Close", header, s.cardLight, "X", 46, out var x);
            x.color = s.text;
            ((Image)close.targetGraphic).sprite = s.circle;
            ((Image)close.targetGraphic).type = Image.Type.Simple;
            UIKit.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -40), new Vector2(104, 104));
            close.onClick.AddListener(Hide);

            var scrollArea = UIKit.Rect("ScrollArea", root);
            UIKit.Stretch(scrollArea, 0, 0, headerH, 0);
            list = UIKit.ScrollList("List", scrollArea, 22, new RectOffset(40, 40, 10, 120));
        }

        // ------------------------------------------------------------------ open / close

        public void Show(ArtistData a)
        {
            if (a == null) return;
            gm = GameManager.Instance;
            UIKit.Skin = skin;
            artist = a;
            root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            openTime = Time.unscaledTime;
            var gc = ArtistGenres.Color(a.genre);
            portrait.sprite = a.avatar;
            portrait.enabled = a.avatar != null;
            ring.color = gc;
            glow.color = new Color(gc.r, gc.g, gc.b, 0.4f);
            headerGlow.color = new Color(gc.r, gc.g, gc.b, 0.3f);
            nameText.text = a.displayName;
            Rebuild();
            list.anchoredPosition = Vector2.zero;
            AudioManager.Play(SoundId.Click);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            // open animation: fade + header slides in
            float k = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.3f);
            group.alpha = k;
            portraitHolder.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - Mathf.Pow(1f - k, 3f));
            portraitHolder.anchoredPosition = new Vector2(60, -130 + Mathf.Sin(Time.unscaledTime * 1.5f) * 5f);
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0) return;
            refreshTimer = 0.5f;
            foreach (var r in refreshers) r();
        }

        // ------------------------------------------------------------------ content

        void Rebuild()
        {
            if (artist == null) return;
            UIKit.Skin = skin;
            refreshers.Clear();
            UIKit.Clear(list);
            refreshers.Add(RefreshHeader);
            BuildStats();
            Section(Loc.T("career.section_chemistry"), Loc.T("career.section_chemistry_sub"));
            BuildChemistry();
            Section(Loc.T("career.section_book"), Loc.T("career.section_book_sub"));
            foreach (var v in Venues.All) VenueCard(v.tier);
            Section(Loc.T("career.section_tours"), Loc.T("career.section_tours_sub"));
            BuildTours();
            Section(Loc.T("career.section_history"), null);
            BuildHistory();
            Section(Loc.T("career.section_achievements"), null);
            BuildAchievements();
            foreach (var r in refreshers) r();
        }

        void RefreshHeader()
        {
            var c = gm.Concerts.Career(artist);
            stageText.text = c.Stage + "  ·  " + Loc.F("career.career_lv", "level", c.CareerLevel);
            genreText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(ArtistGenres.Color(artist.genre)) + ">" + ArtistGenres.Label(artist.genre) + "</color>   ·   " + Loc.F("career.concert_lv", "level", c.ConcertLevel) +
                             "   ·   " + Loc.Upper(Loc.P("count.fans", c.fans));
            var now = DateTime.UtcNow;
            var tour = gm.Tours.ActiveFor(artist);
            if (!gm.Artists.IsSigned(artist)) { statusText.text = Loc.T("career.status_not_signed"); statusPill.color = new Color(1, 0.33f, 0.4f, 0.18f); }
            else if (tour != null && tour.IsFinished(now)) { statusText.text = Loc.T("career.status_tour_finished"); statusPill.color = new Color(skin.gold.r, skin.gold.g, skin.gold.b, 0.25f); }
            else if (tour != null) { statusText.text = Loc.F("career.status_on_tour", "done", tour.ShowsDone(now), "total", tour.shows, "time", NumberFormatter.FormatClock(tour.SecondsLeft(now))); statusPill.color = new Color(skin.pink.r, skin.pink.g, skin.pink.b, 0.2f); }
            else if (c.IsResting(now)) { statusText.text = Loc.F("career.status_resting", "time", NumberFormatter.FormatClock(c.RestSeconds(now))); statusPill.color = new Color(1, 1, 1, 0.08f); }
            else { statusText.text = Loc.T("career.status_ready"); statusPill.color = new Color(skin.money.r, skin.money.g, skin.money.b, 0.18f); }
        }

        void Section(string title, string sub)
        {
            var b = UIKit.Block("Section_" + title, list, sub != null ? 110 : 80);
            var t = UIKit.Text("Title", b, title, 42, skin.text, skin.heading, TextAlignmentOptions.BottomLeft, 10);
            UIKit.TopRow(t.rectTransform, 10, 60);
            if (sub != null)
            {
                var st = UIKit.Text("Sub", b, sub, 26, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
                UIKit.TopRow(st.rectTransform, 72, 36);
            }
        }

        // ---- stats

        void BuildStats()
        {
            var card = UIKit.Block("Stats", list, 520);
            var bg = UIKit.Card("Bg", card, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var c = gm.Concerts.Career(artist);

            Tile(card, 0, 0, "career.tile_popularity", skin.fame, out var popValue, out var popSub, out var popBar, out var popCap);
            Tile(card, 1, 0, "career.tile_fans", skin.pink, out var fansValue, out var fansSub, out _, out _);
            Tile(card, 0, 1, "career.tile_level", skin.cyan, out var lvValue, out var lvSub, out var lvBar, out _);
            Tile(card, 1, 1, "career.tile_performance", skin.money, out var perfValue, out var perfSub, out var perfBar, out _);
            Tile(card, 0, 2, "career.tile_ticket", skin.gold, out var ticketValue, out var ticketSub, out _, out _);
            Tile(card, 1, 2, "career.tile_concerts", skin.text, out var showsValue, out var showsSub, out _, out _);

            refreshers.Add(() =>
            {
                float cap = gm.Concerts.PopularityCap;
                popValue.text = UIKit.Pct(c.popularity);
                popBar.fillAmount = c.popularity / 100f;
                popCap.rectTransform.anchorMin = popCap.rectTransform.anchorMax = new Vector2(cap / 100f, 0.5f);
                popSub.text = c.popularity >= cap - 0.01f ? "<color=#FF8A3D>" + Loc.F("career.studio_limit_upgrade", "value", NumberFormatter.Percent(cap)) + "</color>" : Loc.F("career.studio_limit", "value", NumberFormatter.Percent(cap));
                fansValue.text = NumberFormatter.Format(c.fans);
                fansSub.text = c.lastFansGain > 0 ? Loc.F("career.fans_last_show", "value", "+" + NumberFormatter.Format(c.lastFansGain)) : Loc.T("career.fans_grow");
                lvValue.text = Loc.F("common.level_short", "level", c.ConcertLevel);
                lvBar.fillAmount = c.MaxConcertLevel ? 1f : c.LevelXp / (float)c.LevelXpNeeded;
                lvSub.text = c.MaxConcertLevel ? Loc.T("career.max_level") : Loc.F("career.xp", "xp", NumberFormatter.Group(c.LevelXp), "need", NumberFormatter.Group(c.LevelXpNeeded));
                float perf = c.PerformanceQuality(artist, gm.Studios.Level);
                perfValue.text = NumberFormatter.Percent(perf * 100f);
                perfBar.fillAmount = perf;
                perfSub.text = Loc.T("career.performance_sub");
                var best = BestVenue();
                var q = gm.Concerts.Quote(artist, best);
                ticketValue.text = NumberFormatter.Money(NumberFormatter.Group(q.ticketPrice));
                ticketSub.text = Loc.F("career.ticket_at", "venue", Venues.Get(best).Name);
                showsValue.text = NumberFormatter.Group(c.concerts);
                showsSub.text = Loc.F("career.sold_out_count", "count", c.soldOuts) + "  ·  " + Loc.P("count.tours", c.toursCompleted);
            });
        }

        // ---- chemistry with the studio partner

        void BuildChemistry()
        {
            var chem = gm.Chemistry;
            var card = UIKit.Block("Chemistry", list, 400);
            var bg = UIKit.Card("Bg", card, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var partnerLabel = UIKit.Text("PartnerLabel", card, Loc.T("chemistry.partner_label"), 24, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.TopRow(partnerLabel.rectTransform, 24, 32, 36, 300);
            var partner = UIKit.Text("Partner", card, "", 48, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(partner.rectTransform, 58, 60, 36, 300);
            var match = UIKit.Text("Match", card, "", 26, skin.cyan, skin.heading, TextAlignmentOptions.TopLeft, 4);
            UIKit.TopRow(match.rectTransform, 122, 36, 36, 300);
            var change = UIKit.Button("Change", card, skin.cardLight, Loc.T("chemistry.change"), 36, out var changeLabel);
            changeLabel.color = skin.text;
            UIKit.Place((RectTransform)change.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -40), new Vector2(240, 96));
            change.onClick.AddListener(() =>
            {
                var next = chem.NextPartner(artist);
                if (next == null) return;
                chem.SetPartner(artist, next);
                AudioManager.Play(SoundId.Click);
                foreach (var r in refreshers) r();
            });

            var level = UIKit.Text("Level", card, "", 40, skin.pink, skin.display, TextAlignmentOptions.TopLeft, 3);
            UIKit.TopRow(level.rectTransform, 176, 54, 36, 36);
            var bar = UIKit.Bar("Bar", card, skin.pink, out var track);
            UIKit.TopRow(track.rectTransform, 238, 22, 36, 36);
            var bonus = UIKit.Text("Bonus", card, "", 26, skin.money, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(bonus.rectTransform, 276, 38, 36, 36);
            var pref = UIKit.Text("Preferred", card, "", 24, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(pref.rectTransform, 326, 36, 36, 36);

            refreshers.Add(() =>
            {
                var p = chem.Partner(artist);
                change.gameObject.SetActive(p != null && chem.NextPartner(artist) != p);
                var prefs = chem.Preferred(artist, 2);
                var names = new List<string>();
                foreach (var x in prefs) names.Add(x.DisplayName);
                pref.text = names.Count > 0 ? Loc.F("chemistry.prefers", "names", string.Join(", ", names)) : "";
                if (p == null)
                {
                    partner.text = "—";
                    match.text = Loc.T("chemistry.hire_first");
                    level.text = "";
                    bar.fillAmount = 0;
                    bonus.text = "";
                    return;
                }
                float aff = ArtistChemistryManager.Affinity(p.id, artist.genre);
                partner.text = p.DisplayName;
                match.text = ArtistChemistryManager.AffinityLabel(aff) + "  ·  " + Loc.F("chemistry.growth", "mult", NumberFormatter.FormatMultiplier(aff));
                match.color = aff >= 1.0f ? skin.money : aff >= 0.8f ? skin.cyan : skin.fame;
                float pct = chem.Pair(artist, p).chemistry;
                var lv = ArtistChemistryManager.LevelOf(pct);
                level.text = Loc.F("chemistry.level_line", "level", ArtistChemistryManager.LevelName(lv), "value", UIKit.Pct(pct));
                level.color = lv == ChemistryLevel.Perfect ? skin.gold : skin.pink;
                bar.fillAmount = pct / 100f;
                bar.color = level.color;
                var m = ChemistryModifier.For(lv);
                bonus.text = m.performance <= 0 && m.fanGrowth <= 0 ? Loc.T("chemistry.no_bonus")
                    : (gm.Artists.IsSigned(artist) ? "" : "<color=#8A84B0>" + Loc.T("chemistry.when_signed") + "</color>  ") +
                      Loc.F("chemistry.bonus_line", "show", Pc(m.performance), "fans", Pc(m.fanGrowth), "streams", Pc(m.streams)) +
                      (m.viralChance > 0 ? "  ·  " + Loc.F("chemistry.bonus_viral", "viral", Pc(m.viralChance)) : "");
            });
        }

        static string Pc(float v) => "+" + NumberFormatter.Percent(Mathf.RoundToInt(v * 100));

        VenueTier BestVenue()
        {
            var best = VenueTier.Club;
            foreach (var v in Venues.All) if (gm.Concerts.Quote(artist, v.tier).unlocked) best = v.tier;
            return best;
        }

        void Tile(RectTransform card, int col, int row, string label, Color color, out TextMeshProUGUI value, out TextMeshProUGUI sub, out Image bar, out Image cap)
        {
            var t = UIKit.Rect("Tile_" + label, card); // label = localization key
            t.anchorMin = new Vector2(col * 0.5f, 1f - (row + 1) / 3f);
            t.anchorMax = new Vector2(col * 0.5f + 0.5f, 1f - row / 3f);
            t.offsetMin = new Vector2(col == 0 ? 36 : 18, 10);
            t.offsetMax = new Vector2(col == 0 ? -18 : -36, -10);
            var l = UIKit.Text("Label", t, Loc.T(label), 24, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.TopRow(l.rectTransform, 14, 32);
            value = UIKit.Text("Value", t, "-", 60, color, skin.display, TextAlignmentOptions.TopLeft, 1);
            UIKit.TopRow(value.rectTransform, 46, 70);
            sub = UIKit.Text("Sub", t, "", 22, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(sub.rectTransform, 122, 30);
            bar = null;
            cap = null;
            if (label == "career.tile_popularity" || label == "career.tile_level" || label == "career.tile_performance")
            {
                bar = UIKit.Bar("Bar", t, color, out var track);
                track.rectTransform.anchorMin = new Vector2(0.48f, 1);
                track.rectTransform.anchorMax = new Vector2(1, 1);
                track.rectTransform.pivot = new Vector2(0.5f, 1);
                track.rectTransform.offsetMin = new Vector2(0, -90);
                track.rectTransform.offsetMax = new Vector2(0, -72);
                cap = UIKit.Image("Cap", track.rectTransform, Color.white);
                cap.rectTransform.sizeDelta = new Vector2(4, 30);
                cap.gameObject.SetActive(label == "career.tile_popularity");
            }
        }

        // ---- venues

        void VenueCard(VenueTier tier)
        {
            var v = Venues.Get(tier);
            var block = UIKit.Block("Venue_" + v.name, list, 340);
            var bg = UIKit.Card("Bg", block, skin.card);
            UIKit.Stretch(bg.rectTransform);

            // cinematic venue thumbnail
            var thumb = UIKit.Rect("Thumb", block);
            thumb.anchorMin = new Vector2(0, 0);
            thumb.anchorMax = new Vector2(0, 1);
            thumb.pivot = new Vector2(0, 0.5f);
            thumb.offsetMin = new Vector2(16, 16);
            thumb.offsetMax = new Vector2(316, -16);
            thumb.gameObject.AddComponent<RectMask2D>();
            if (show != null && show.VenueSprites != null && (int)tier < show.VenueSprites.Length)
            {
                var art = UIKit.Image("Art", thumb, Color.white, show.VenueSprites[(int)tier]);
                UIKit.Stretch(art.rectTransform);
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = 800f / 1400f;
            }
            var shade = UIKit.Image("Shade", thumb, new Color(0, 0, 0, 0.45f));
            UIKit.Stretch(shade.rectTransform, 0, 0, 200, 0);
            var vname = UIKit.Text("VenueName", thumb, v.Name, 34, Color.white, skin.heading, TextAlignmentOptions.Bottom, 4);
            UIKit.Stretch(vname.rectTransform, 8, 8, 0, 14);
            var band = UIKit.Image("Band", block, v.color);
            band.rectTransform.anchorMin = new Vector2(0, 0);
            band.rectTransform.anchorMax = new Vector2(0, 1);
            band.rectTransform.offsetMin = new Vector2(316, 16);
            band.rectTransform.offsetMax = new Vector2(322, -16);

            var info = UIKit.Rect("Info", block);
            UIKit.Stretch(info, 350, 24, 20, 20);
            var line1 = UIKit.Text("Capacity", info, "", 28, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 3);
            UIKit.TopRow(line1.rectTransform, 0, 40);
            var line2 = UIKit.Text("Expected", info, "", 28, skin.text, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(line2.rectTransform, 46, 40);
            var fillBar = UIKit.Bar("ExpectedFill", info, v.color, out var fillTrack);
            UIKit.TopRow(fillTrack.rectTransform, 92, 14, 0, 0);
            var revenue = UIKit.Text("Revenue", info, "", 58, skin.money, skin.display, TextAlignmentOptions.TopLeft, 1);
            UIKit.TopRow(revenue.rectTransform, 118, 70, 0, 320);
            var revLabel = UIKit.Text("RevLabel", info, Loc.T("career.expected_revenue"), 20, skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 4);
            UIKit.TopRow(revLabel.rectTransform, 188, 28, 0, 320);
            var lockText = UIKit.Text("Lock", info, "", 30, skin.fame, skin.heading, TextAlignmentOptions.Left, 2);
            UIKit.Stretch(lockText.rectTransform, 0, 0, 120, 0);
            lockText.textWrappingMode = TextWrappingModes.Normal;

            var book = UIKit.Button("Book", info, skin.gold, Loc.T("career.book"), 48, out var bookLabel, "$0", out var bookCost);
            UIKit.Place((RectTransform)book.transform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(300, 150));
            book.onClick.AddListener(() => OnBook(tier, block));

            var lockOverlay = UIKit.Image("LockOverlay", thumb, new Color(0.02f, 0.02f, 0.05f, 0.6f));
            UIKit.Stretch(lockOverlay.rectTransform);
            var lockIcon = UIKit.Text("LockIcon", lockOverlay.rectTransform, Loc.T("ui.locked"), 34, skin.textDim, skin.heading, TextAlignmentOptions.Center, 8);

            refreshers.Add(() =>
            {
                var q = gm.Concerts.Quote(artist, tier);
                line1.text = Loc.F("career.venue_line", "capacity", NumberFormatter.Group(q.capacity), "ticket", NumberFormatter.Money(NumberFormatter.Group(q.ticketPrice)));
                lockOverlay.gameObject.SetActive(!q.unlocked);
                revenue.gameObject.SetActive(q.unlocked);
                revLabel.gameObject.SetActive(q.unlocked);
                fillTrack.gameObject.SetActive(q.unlocked);
                book.gameObject.SetActive(q.unlocked);
                lockText.gameObject.SetActive(!q.unlocked);
                if (!q.unlocked)
                {
                    line2.text = v.Description;
                    lockText.text = q.lockReason;
                    return;
                }
                line2.text = Loc.F("career.expected_fans", "fans", "<b>" + NumberFormatter.Group(q.expectedAttendance) + "</b>") + "  ·  " + NumberFormatter.Percent(q.expectedFill * 100f);
                fillBar.fillAmount = q.expectedFill;
                revenue.text = NumberFormatter.FormatMoney(q.TotalRevenue);
                bookCost.text = NumberFormatter.FormatMoney(q.bookingCost);
                var block = gm.Concerts.Block(artist);
                bool afford = gm.Economy.CanAfford(q.bookingCost);
                bookLabel.text = block == ConcertManager.BlockKind.None ? Loc.T("career.book")
                    : block == ConcertManager.BlockKind.Resting ? Loc.T("career.resting") : Loc.Upper(gm.Concerts.BlockReason(artist));
                if (block == ConcertManager.BlockKind.Resting) bookCost.text = NumberFormatter.FormatClock(gm.Concerts.Career(artist).RestSeconds(DateTime.UtcNow));
                ((Image)book.targetGraphic).color = block == ConcertManager.BlockKind.None && afford ? skin.gold : skin.locked;
            });
        }

        void OnBook(VenueTier tier, RectTransform card)
        {
            var r = gm.Concerts.Book(artist, tier);
            if (r == null)
            {
                AudioManager.Play(SoundId.Click, 0.7f);
                StartCoroutine(Shake(card));
                return;
            }
            if (show != null) show.Play(r);
        }

        System.Collections.IEnumerator Shake(RectTransform rt)
        {
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                rt.localPosition += new Vector3(Mathf.Sin(t * 60f) * 4f, 0, 0);
                yield return null;
            }
        }

        // ---- tours

        void BuildTours()
        {
            var c = gm.Concerts.Career(artist);
            if (!string.IsNullOrEmpty(c.newAlbumId))
            {
                var b = UIKit.Block("AlbumBonus", list, 150);
                var bg = UIKit.Card("Bg", b, new Color(skin.gold.r * 0.3f, skin.gold.g * 0.25f, skin.gold.b * 0.1f, 1f));
                UIKit.Stretch(bg.rectTransform);
                var t1 = UIKit.Text("Title", b, Loc.F("career.new_album", "title", "\"" + c.newAlbumTitle.ToUpperInvariant() + "\""), 38, skin.gold, skin.heading, TextAlignmentOptions.TopLeft, 4);
                UIKit.TopRow(t1.rectTransform, 22, 50, 36, 36);
                var t2 = UIKit.Text("Bonus", b, Loc.T("career.album_tour_bonus"), 28, skin.text, skin.body, TextAlignmentOptions.TopLeft);
                UIKit.TopRow(t2.rectTransform, 80, 40, 36, 36);
            }

            var active = gm.Tours.ActiveFor(artist);
            if (active != null) ActiveTourCard(active);
            foreach (var d in Tours.All) TourCard(d.kind, active != null);
        }

        void ActiveTourCard(ActiveTour t)
        {
            var b = UIKit.Block("ActiveTour", list, 330);
            var bg = UIKit.Card("Bg", b, new Color(0.2f, 0.06f, 0.18f, 1f));
            UIKit.Stretch(bg.rectTransform);
            var head = UIKit.Text("Head", b, Loc.T("artist.on_tour"), 28, skin.pink, skin.heading, TextAlignmentOptions.TopLeft, 10);
            UIKit.TopRow(head.rectTransform, 24, 40, 36, 36);
            var title = UIKit.Text("Title", b, t.Title, 54, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(title.rectTransform, 64, 70, 36, 36);
            var bar = UIKit.Bar("Progress", b, skin.pink, out var track);
            UIKit.TopRow(track.rectTransform, 150, 26, 36, 36);
            var info = UIKit.Text("Info", b, "", 28, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(info.rectTransform, 190, 40, 36, 36);
            var collect = UIKit.Button("Collect", b, skin.money, Loc.T("ui.collect"), 48, out _);
            UIKit.Place((RectTransform)collect.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 24), new Vector2(330, 100));
            collect.onClick.AddListener(() => { if (TourCompletePopup.Instance != null) TourCompletePopup.Instance.ShowNow(t); });
            var expect = UIKit.Text("Expect", b, "", 30, skin.money, skin.heading, TextAlignmentOptions.BottomLeft, 2);
            UIKit.Stretch(expect.rectTransform, 36, 380, 230, 30);
            refreshers.Add(() =>
            {
                var now = DateTime.UtcNow;
                bar.fillAmount = (float)t.Progress(now);
                int done = t.ShowsDone(now);
                string city = done > 0 && done <= t.cities.Count ? t.cities[done - 1] : t.cities.Count > 0 ? t.cities[0] : "";
                info.text = Loc.F("career.tour_progress", "done", done, "total", t.shows) + "  ·  " +
                            (t.IsFinished(now) ? Loc.T("career.tour_finished") : Loc.F("career.time_left", "time", NumberFormatter.FormatClock(t.SecondsLeft(now)))) +
                            (city.Length > 0 ? "  ·  " + Loc.F(t.IsFinished(now) ? "career.last_show" : "career.now_city", "city", city) : "");
                expect.text = NumberFormatter.FormatMoney(t.revenue) + "  ·  +" + Loc.P("count.fans", t.fans);
                collect.gameObject.SetActive(t.IsFinished(now));
            });
        }

        void TourCard(TourKind kind, bool busy)
        {
            var d = Tours.Get(kind);
            var b = UIKit.Block("Tour_" + d.name, list, 300);
            var bg = UIKit.Card("Bg", b, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var vc = Venues.Get(d.venue).color;
            var stripe = UIKit.Image("Stripe", b, vc);
            stripe.rectTransform.anchorMin = new Vector2(0, 0);
            stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.offsetMin = new Vector2(16, 20);
            stripe.rectTransform.offsetMax = new Vector2(24, -20);
            var kindText = UIKit.Text("Kind", b, d.Name + "   <color=#8A84B0><size=75%>" + Loc.Upper(d.Audience) + "</size></color>", 30, vc, skin.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.TopRow(kindText.rectTransform, 22, 40, 50, 36);
            var title = UIKit.Text("Title", b, "", 46, skin.text, skin.display, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(title.rectTransform, 64, 60, 50, 36);
            var details = UIKit.Text("Details", b, "", 26, skin.textDim, skin.body, TextAlignmentOptions.TopLeft);
            UIKit.TopRow(details.rectTransform, 130, 36, 50, 36);
            var rewards = UIKit.Text("Rewards", b, "", 40, skin.money, skin.heading, TextAlignmentOptions.TopLeft, 2);
            UIKit.TopRow(rewards.rectTransform, 176, 54, 50, 380);
            var lockText = UIKit.Text("Lock", b, "", 30, skin.fame, skin.heading, TextAlignmentOptions.BottomLeft, 2);
            UIKit.Stretch(lockText.rectTransform, 50, 36, 180, 28);
            var start = UIKit.Button("Start", b, skin.pink, Loc.T("career.start_tour"), 46, out var startLabel, "$0", out var startCost);
            UIKit.Place((RectTransform)start.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 20), new Vector2(330, 108));
            start.onClick.AddListener(() => OnStartTour(kind, b));

            refreshers.Add(() =>
            {
                var q = gm.Tours.Quote(artist, kind);
                title.text = q.title;
                details.text = Loc.P("count.shows", q.shows) + "  ·  " + NumberFormatter.FormatDuration(q.hours * 3600.0) + "  ·  " + Venues.Get(d.venue).Name + (d.capacityScale > 1 ? " XL" : "") +
                               "  ·  " + Loc.P("count.seats", q.capacity) + (q.albumBonus ? "  ·  <color=#F2C14E>" + Loc.T("career.album_bonus") + "</color>" : "");
                rewards.gameObject.SetActive(q.unlocked);
                rewards.text = NumberFormatter.FormatMoney(q.revenue) + "   <color=#FF3DC8>+" + Loc.P("count.fans", q.fans) + "</color>";
                lockText.gameObject.SetActive(!q.unlocked);
                lockText.text = q.lockReason;
                start.gameObject.SetActive(q.unlocked && !busy);
                startCost.text = NumberFormatter.FormatMoney(q.cost);
                var block = gm.Concerts.Block(artist);
                bool ok = block == ConcertManager.BlockKind.None && gm.Economy.CanAfford(q.cost);
                startLabel.text = Loc.T(block == ConcertManager.BlockKind.Resting ? "career.resting" : "career.start_tour");
                ((Image)start.targetGraphic).color = ok ? skin.pink : skin.locked;
                bg.color = q.unlocked ? skin.card : new Color(skin.card.r, skin.card.g, skin.card.b, 0.6f);
            });
        }


        void OnStartTour(TourKind kind, RectTransform card)
        {
            var t = gm.Tours.Start(artist, kind);
            if (t == null)
            {
                AudioManager.Play(SoundId.Click, 0.7f);
                StartCoroutine(Shake(card));
                return;
            }
            AudioManager.Play(SoundId.Unlock);
            UIBurst.Spawn(root, new Vector2(0, 0), 50, skin.confetti != null ? skin.confetti : skin.rounded);
            Rebuild();
            list.anchoredPosition = Vector2.zero;
        }

        // ---- history

        void BuildHistory()
        {
            var c = gm.Concerts.Career(artist);
            var card = UIKit.Block("HistorySummary", list, 400);
            var bg = UIKit.Card("Bg", card, skin.card);
            UIKit.Stretch(bg.rectTransform);
            var title = UIKit.Text("Title", card, Loc.F("career.history_title", "artist", artist.displayName.ToUpperInvariant()), 32, skin.gold, skin.heading, TextAlignmentOptions.TopLeft, 6);
            UIKit.TopRow(title.rectTransform, 24, 44, 36, 36);
            string[] labels = { Loc.T("career.h_concerts"), Loc.T("career.h_fans"), Loc.T("career.h_revenue"), Loc.T("career.h_venue"), Loc.T("career.h_attendance"), Loc.T("career.h_best") };
            string[] values =
            {
                NumberFormatter.Group(c.concerts),
                NumberFormatter.Format(c.fans),
                NumberFormatter.FormatMoney(c.totalRevenue),
                c.bestVenue >= 0 ? Venues.Get((VenueTier)c.bestVenue).Name : "—",
                c.bestAttendance > 0 ? NumberFormatter.Percent(c.bestAttendance * 100f) : "—",
                string.IsNullOrEmpty(c.BestConcertDisplay) ? "—" : "\"" + c.BestConcertDisplay + "\"",
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var row = UIKit.Rect("Row" + i, card);
                UIKit.TopRow(row, 84 + i * 50, 48, 36, 36);
                var l = UIKit.Text("L", row, labels[i], 24, skin.textDim, skin.heading, TextAlignmentOptions.Left, 6);
                l.rectTransform.anchorMax = new Vector2(0.45f, 1);
                var v = UIKit.Text("V", row, values[i], 30, skin.text, skin.heading, TextAlignmentOptions.Right, 1);
                v.rectTransform.anchorMin = new Vector2(0.4f, 0);
            }

            if (c.history.Count == 0)
            {
                var empty = UIKit.Block("Empty", list, 90);
                var t = UIKit.Text("T", empty, Loc.T("career.no_concerts"), 28, skin.textDim, skin.body);
                return;
            }
            foreach (var r in c.history)
            {
                var b = UIKit.Block("Record", list, 112);
                var rb = UIKit.Rounded("Bg", b, new Color(1, 1, 1, 0.04f));
                UIKit.Stretch(rb.rectTransform);
                var vc = Venues.Get((VenueTier)Mathf.Clamp(r.venue, 0, 4)).color;
                var dot = UIKit.Image("Dot", b, vc, skin.circle);
                UIKit.Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(22, 22));
                var name = UIKit.Text("Name", b, (r.tour ? "<color=#FF3DC8>" + Loc.T("tour.tour") + "</color>  " : "") + r.DisplayName, 30, skin.text, skin.heading, TextAlignmentOptions.TopLeft, 2);
                UIKit.Stretch(name.rectTransform, 70, 300, 16, 50);
                var sub = UIKit.Text("Sub", b, NumberFormatter.Group(r.attendance) + " / " + NumberFormatter.Group(r.capacity) + "  ·  " + NumberFormatter.Percent(r.Fill * 100f) +
                    (r.soldOut ? "  ·  <color=#FF8A3D>" + Loc.T("concert.sold_out") + "</color>" : "") + "  ·  +" + Loc.P("count.fans", r.fans), 24, skin.textDim, skin.body, TextAlignmentOptions.BottomLeft);
                UIKit.Stretch(sub.rectTransform, 70, 300, 56, 14);
                var rev = UIKit.Text("Revenue", b, NumberFormatter.FormatMoney(r.revenue), 40, skin.money, skin.display, TextAlignmentOptions.Right, 1);
                UIKit.Stretch(rev.rectTransform, 600, 30, 10, 10);
            }
        }

        // ---- achievements

        void BuildAchievements()
        {
            var all = AchievementManager.All;
            for (int i = 0; i < all.Length; i += 2)
            {
                var row = UIKit.Block("Ach" + i, list, 140);
                for (int k = 0; k < 2 && i + k < all.Length; k++)
                {
                    var a = all[i + k];
                    bool got = gm.Achievements.IsUnlocked(a.id);
                    var chip = UIKit.Card("Chip", row, got ? new Color(0.22f, 0.17f, 0.06f) : skin.card);
                    chip.rectTransform.anchorMin = new Vector2(k * 0.5f, 0);
                    chip.rectTransform.anchorMax = new Vector2(k * 0.5f + 0.5f, 1);
                    chip.rectTransform.offsetMin = new Vector2(k == 0 ? 0 : 10, 0);
                    chip.rectTransform.offsetMax = new Vector2(k == 0 ? -10 : 0, 0);
                    var t = UIKit.Text("Title", chip.rectTransform, a.Title, 32, got ? skin.gold : skin.textDim, skin.heading, TextAlignmentOptions.TopLeft, 4);
                    UIKit.TopRow(t.rectTransform, 18, 46, 24, 20);
                    var d = UIKit.Text("Desc", chip.rectTransform, a.Description, 22, got ? skin.text : new Color(skin.textDim.r, skin.textDim.g, skin.textDim.b, 0.7f), skin.body, TextAlignmentOptions.TopLeft);
                    UIKit.TopRow(d.rectTransform, 70, 56, 24, 20);
                    d.textWrappingMode = TextWrappingModes.Normal;
                }
            }
        }
    }
}
