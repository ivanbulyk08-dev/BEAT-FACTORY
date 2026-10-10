using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The animated studio on the HOME screen: room background per studio tier (bedroom / pro / modern),
    /// the player character, producers that appear at their stations when hired, and reactions to
    /// beats, upgrades, HIT and VIRAL events.
    /// </summary>
    public class StudioSceneView : MonoBehaviour
    {
        [Header("Room")]
        [SerializeField] Image backgroundFront;
        [SerializeField] Image backgroundBack;
        [Tooltip("Room art per visual tier: 0 Bedroom, 1 Pro studio, 2 Modern studio.")]
        [SerializeField] Sprite[] tierBackgrounds;
        [Tooltip("Animated extras per tier (monitor screens, speaker cones).")]
        [SerializeField] GameObject[] tierOverlays;
        [SerializeField] Image lampGlow;
        [SerializeField] Color[] tierGlowColors;
        [SerializeField] float crossfadeTime = 0.6f;

        [Header("Characters")]
        [SerializeField] CharacterRig mainCharacter;
        [Tooltip("Player outfit per tier (body + arm sprites): the clothes get better with the studio.")]
        [SerializeField] Sprite[] playerBodies;
        [SerializeField] Sprite[] playerArms;
        [Tooltip("Producer stations in producer order (Beginner, Melody, Drum).")]
        [SerializeField] GameObject[] producerStations;

        [Header("Feedback")]
        [SerializeField] SceneFX fx;
        [SerializeField] RectTransform beatOrigin;
        [SerializeField] TMP_Text studioNameText;
        [SerializeField] Color noteColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] Color sparkleColor = new Color(1f, 0.82f, 0.24f);
        [SerializeField] Color hitColor = new Color(1f, 0.54f, 0.24f);
        [SerializeField] Color viralColor = new Color(1f, 0.24f, 0.78f);

        [Header("Upgrade show")]
        [Tooltip("Black overlay over the room: lights go out, then flicker back on.")]
        [SerializeField] Image blackout;
        [Tooltip("The room (zoomed like a camera during the move-in).")]
        [SerializeField] RectTransform room;
        [SerializeField] CanvasGroup banner;
        [SerializeField] TMP_Text bannerTitle;
        [SerializeField] TMP_Text bannerName;
        [SerializeField] TMP_Text bannerSub;

        [Header("Room layouts")]
        [Tooltip("Where the main character sits in each room (room coordinates). Every room places the desk differently.")]
        [SerializeField] Vector2[] tierCharacterPositions;
        [Tooltip("Rooms with the desk on the right: the producer faces right.")]
        [SerializeField] bool[] tierFacingRight;
        [Tooltip("Where beat notes come from in each room (the main monitor).")]
        [SerializeField] Vector2[] tierBeatOrigins;

        GameManager gm;
        CharacterRig[] stationRigs;
        int tier = -1;
        float fade = 1f;
        Coroutine show;

        /// <summary>Every studio level has its own room (10 rooms: Basement → Mega Music Complex).</summary>
        public static int TierFor(int studioLevel) => Mathf.Clamp(studioLevel, 0, 9);

        void Start()
        {
            gm = GameManager.Instance;
            stationRigs = new CharacterRig[producerStations.Length];
            for (int i = 0; i < producerStations.Length; i++)
                stationRigs[i] = producerStations[i].GetComponentInChildren<CharacterRig>(true);

            gm.Studios.CurrentChanged += OnStudioChanged;
            gm.Producers.ProducerChanged += OnProducerChanged;
            gm.Producers.ProducerProduced += OnProducerProduced;
            gm.Beats.BeatsCreated += OnBeats;
            gm.Genres.GenreUnlocked += OnPurchase;
            gm.Studios.StudioUpgraded += OnPurchase;
            gm.Artists.ArtistSigned += OnPurchase;
            gm.Streams.PlatformReleased += OnPurchase;
            gm.Prestige.Prestiged += OnPrestige;

            SetTier(TierFor(gm.Studios.Level), instant: true);
            RefreshStations();
            RefreshName();
            Loc.Changed += RefreshName;
        }

        void OnDestroy()
        {
            Loc.Changed -= RefreshName;
            if (gm == null) return;
            gm.Studios.CurrentChanged -= OnStudioChanged;
            gm.Producers.ProducerChanged -= OnProducerChanged;
            gm.Producers.ProducerProduced -= OnProducerProduced;
            gm.Beats.BeatsCreated -= OnBeats;
            gm.Genres.GenreUnlocked -= OnPurchase;
            gm.Studios.StudioUpgraded -= OnPurchase;
            gm.Artists.ArtistSigned -= OnPurchase;
            gm.Streams.PlatformReleased -= OnPurchase;
            gm.Prestige.Prestiged -= OnPrestige;
        }

        // ------------------------------------------------------------------ room

        void OnStudioChanged(StudioData _)
        {
            SetTier(TierFor(gm.Studios.Level), instant: false);
            RefreshName();
        }

        void RefreshName()
        {
            if (studioNameText != null && gm.Studios.Current != null)
                studioNameText.text = Loc.Upper(gm.Studios.Current.DisplayName) +
                                      "  <size=70%><color=#FFD23D>" + Loc.F("common.level_short", "level", gm.Studios.Level + 1) + "</color></size>";
        }

        void SetTier(int newTier, bool instant)
        {
            newTier = Mathf.Clamp(newTier, 0, tierBackgrounds.Length - 1);
            if (newTier == tier) return;

            if (!instant && tier >= 0 && gameObject.activeInHierarchy)
            {
                if (show != null) StopCoroutine(show);
                show = StartCoroutine(UpgradeShow(newTier));
                return;
            }
            ApplyTier(newTier);
        }

        void ApplyTier(int newTier)
        {
            backgroundBack.sprite = backgroundFront.sprite;
            backgroundFront.sprite = tierBackgrounds[newTier];
            fade = 1f;
            backgroundBack.gameObject.SetActive(false);
            tier = newTier;

            for (int i = 0; i < tierOverlays.Length; i++)
                if (tierOverlays[i] != null) tierOverlays[i].SetActive(i == tier);
            if (lampGlow != null && tier < tierGlowColors.Length) lampGlow.color = tierGlowColors[tier];
            if (blackout != null) blackout.gameObject.SetActive(false);
            if (playerBodies != null && tier < playerBodies.Length)
                mainCharacter.SetOutfit(playerBodies[tier], playerArms != null && tier < playerArms.Length ? playerArms[tier] : null);
            if (tierCharacterPositions != null && tier < tierCharacterPositions.Length)
                mainCharacter.SetHome(tierCharacterPositions[tier], tierFacingRight != null && tier < tierFacingRight.Length && tierFacingRight[tier]);
            if (tierBeatOrigins != null && tier < tierBeatOrigins.Length)
                beatOrigin.anchoredPosition = tierBeatOrigins[tier];
        }

        System.Collections.IEnumerator Banner(string title, string name, string sub)
        {
            if (banner == null) yield break;
            bannerTitle.text = title;
            bannerName.text = name;
            bannerSub.text = sub;
            banner.gameObject.SetActive(true);
            banner.transform.SetAsLastSibling();
            var rt = (RectTransform)banner.transform;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                banner.alpha = t;
                float s = Mathf.LerpUnclamped(0.6f, 1f, 1f + 2.7f * Mathf.Pow(t - 1f, 3) + 1.7f * Mathf.Pow(t - 1f, 2));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            banner.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.9f);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
            {
                banner.alpha = 1f - t;
                yield return null;
            }
            banner.gameObject.SetActive(false);
        }

        /// <summary>
        /// Studio upgrade: lights go out, the new room appears, lights flicker on, equipment pops in,
        /// screens boot, the character is proud and celebrates, particles fly.
        /// </summary>
        /// <summary>
        /// Move-in show: the camera pulls back as the lights die and the price is paid, the old room disappears,
        /// the new room is revealed with a push-in, lights flicker on, screens boot, neon and equipment pop in,
        /// posters/records appear, the producer reacts and "NEW STUDIO UNLOCKED" lands.
        /// </summary>
        System.Collections.IEnumerator UpgradeShow(int newTier)
        {
            var studio = gm.Studios.Get(newTier);
            Vector3 baseScale = room != null ? room.localScale : Vector3.one;
            if (blackout != null)
            {
                blackout.gameObject.SetActive(true);
                blackout.transform.SetAsLastSibling();
                fx.transform.SetAsLastSibling();
            }
            // 1-3. darken, money paid, camera pulls back
            if (banner != null && studio != null)
            {
                bannerTitle.text = "";
                bannerName.text = "-" + NumberFormatter.FormatMoney(studio.upgradeCost);
                bannerSub.text = "";
                banner.gameObject.SetActive(true);
                banner.alpha = 1f;
                banner.transform.localScale = Vector3.one;
            }
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.55f)
            {
                if (blackout != null) blackout.color = new Color(0, 0, 0, t * 0.97f);
                if (room != null) room.localScale = baseScale * Mathf.Lerp(1f, 0.9f, t * t);
                if (banner != null) banner.transform.localScale = Vector3.one * (1f + t * 0.15f);
                yield return null;
            }
            if (banner != null) banner.gameObject.SetActive(false);

            // 4-6. old room gone, new room revealed with a push-in
            ApplyTier(newTier);
            var overlay = newTier < tierOverlays.Length ? tierOverlays[newTier] : null;
            var items = overlay != null ? overlay.transform : null;
            if (items != null) foreach (Transform child in items) child.localScale = Vector3.zero;
            var walls = GetComponentsInChildren<IWallDecor>(true);
            foreach (var w in walls) w.HideAll();
            if (blackout != null) { blackout.gameObject.SetActive(true); blackout.color = new Color(0, 0, 0, 0.97f); }
            yield return new WaitForSecondsRealtime(0.15f);

            // 7. lights flicker on while the camera pushes in
            float[] flicker = { 0.9f, 0.4f, 0.85f, 0.25f, 0.6f, 0.1f, 0f };
            for (int i = 0; i < flicker.Length; i++)
            {
                if (blackout != null) blackout.color = new Color(0, 0, 0, flicker[i]);
                if (room != null) room.localScale = baseScale * Mathf.Lerp(1.14f, 1f, (i + 1f) / flicker.Length);
                if (i == 2) fx.Sparkles(beatOrigin.position, 20, sparkleColor, 380f);
                yield return new WaitForSecondsRealtime(0.09f);
            }
            if (room != null) room.localScale = baseScale;
            if (blackout != null) blackout.gameObject.SetActive(false);

            // 8-10. screens boot, neon/equipment pop in, posters and records appear
            if (items != null)
            {
                foreach (var daw in items.GetComponentsInChildren<DawScreen>(true)) daw.Boot();
                int n = 0;
                foreach (Transform child in items) StartCoroutine(Pop(child, n++ * 0.07f));
            }
            yield return new WaitForSecondsRealtime(0.3f);
            foreach (var w in walls) w.PopAll();

            // 11. the producer reacts
            mainCharacter.Upgrade();
            fx.Shake(10f, 0.35f);
            yield return new WaitForSecondsRealtime(0.5f);
            mainCharacter.Success();

            // 12-13. NEW STUDIO UNLOCKED + reward
            fx.Confetti(70);
            fx.Sparkles(mainCharacter.transform.position + Vector3.up * 0.2f, 28, sparkleColor, 380f);
            if (studio != null)
                yield return Banner(Loc.T("studio.new_unlocked"), Loc.Upper(studio.DisplayName),
                    Loc.F("common.income", "mult", NumberFormatter.FormatMultiplier(studio.incomeMultiplier)) + "   ·   " + Loc.F("common.level", "level", newTier + 1));
            show = null;
        }

        static System.Collections.IEnumerator Pop(Transform t, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            for (float a = 0; a < 1f; a += Time.unscaledDeltaTime / 0.35f)
            {
                float s = a < 0.7f ? Mathf.Lerp(0f, 1.15f, a / 0.7f) : Mathf.Lerp(1.15f, 1f, (a - 0.7f) / 0.3f);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        void Update()
        {
            if (fade >= 1f) { enabled = false; return; }
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / crossfadeTime);
            backgroundBack.color = new Color(1, 1, 1, 1f - fade);
            if (fade >= 1f) backgroundBack.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ producers

        void RefreshStations()
        {
            var list = gm.Producers.Producers;
            for (int i = 0; i < producerStations.Length; i++)
                producerStations[i].SetActive(i < list.Count && list[i].IsHired);
        }

        void OnProducerChanged(Producer p)
        {
            int i = p.Index;
            bool wasActive = i < producerStations.Length && producerStations[i].activeSelf;
            RefreshStations();
            if (i >= producerStations.Length || !p.IsHired) return;

            var pos = stationRigs[i] != null ? stationRigs[i].transform.position : beatOrigin.position;
            fx.Sparkles(pos + Vector3.up * 0.1f, wasActive ? 10 : 22, sparkleColor);
            if (stationRigs[i] != null) stationRigs[i].Upgrade();
            mainCharacter.Upgrade();
        }

        void OnProducerProduced(Producer p, BeatResult r)
        {
            if (p.Index >= stationRigs.Length || stationRigs[p.Index] == null) return;
            stationRigs[p.Index].Bounce();
            if (isActiveAndEnabled || gameObject.activeInHierarchy)
                fx.Notes(stationRigs[p.Index].transform.position, 1, noteColor);
        }

        // ------------------------------------------------------------------ beats & events

        void OnBeats(BeatResult r)
        {
            if (r.Source == BeatSource.Offline || !gameObject.activeInHierarchy) return;

            if (r.Source == BeatSource.Manual)
            {
                mainCharacter.Work(0.6f);
                fx.Notes(beatOrigin.position, 2, noteColor);
            }

            switch (r.Quality)
            {
                case BeatQuality.Great:
                    fx.Sparkles(beatOrigin.position, 8, sparkleColor);
                    break;
                case BeatQuality.Hit:
                    mainCharacter.Success();
                    fx.Sparkles(beatOrigin.position, 26, hitColor, 320f);
                    fx.Shake(8f, 0.3f);
                    break;
                case BeatQuality.Viral:
                    mainCharacter.Success();
                    foreach (var rig in stationRigs) if (rig != null && rig.gameObject.activeInHierarchy) rig.Success();
                    fx.Confetti(60);
                    fx.Sparkles(beatOrigin.position, 30, viralColor, 420f);
                    fx.Shake(18f, 0.6f);
                    break;
            }
        }

        void OnPurchase<T>(T _)
        {
            if (!gameObject.activeInHierarchy) return;
            mainCharacter.Upgrade();
            fx.Sparkles(mainCharacter.transform.position + Vector3.up * 0.2f, 14, sparkleColor);
        }

        void OnPrestige(double _)
        {
            RefreshStations();
            SetTier(TierFor(gm.Studios.Level), instant: true);
            RefreshName();
        }
    }
}
