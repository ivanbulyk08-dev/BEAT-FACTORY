using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Artist contract: portrait, archetype, music style, bio, contract terms and a premium SIGN CONTRACT button.
    /// Signing plays a short ceremony: the "SIGNED" stamp slams onto the contract, flash + confetti,
    /// the artist joins the studio (artist strip) and the income boost starts immediately.
    /// </summary>
    public class ArtistContractPopup : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] RectTransform portraitHolder;
        [SerializeField] Image portrait;
        [SerializeField] Image portraitRing;
        [SerializeField] Image portraitGlow;
        [SerializeField] Image roleChip;
        [SerializeField] TMP_Text roleText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text styleText;
        [SerializeField] TMP_Text bioText;
        [SerializeField] TMP_Text costText;
        [SerializeField] TMP_Text genreText;
        [SerializeField] TMP_Text boostText;
        [SerializeField] TMP_Text requiresText;
        [SerializeField] TMP_Text beatText;
        [SerializeField] Image[] popularityPips;
        [SerializeField] Button signButton;
        [SerializeField] TMP_Text signLabel;
        [SerializeField] TMP_Text signCost;
        [SerializeField] Button closeButton;
        [SerializeField] CanvasGroup stamp;
        [SerializeField] Image flash;
        [SerializeField] RectTransform burstLayer;
        [SerializeField] Sprite confettiSprite;
        [SerializeField] Color okColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color badColor = new Color(1f, 0.33f, 0.4f);

        ArtistData artist;
        ArtistManager artists;
        StudioManager studios;
        EconomyManager economy;
        bool signing;
        float idle;

        public static ArtistContractPopup Instance { get; private set; }
        public bool IsOpen => window != null && window.IsOpen;

        void Awake()
        {
            Instance = this;
            basePos = portraitHolder.anchoredPosition;
            signButton.onClick.AddListener(OnSign);
            closeButton.onClick.AddListener(() => { if (!signing) window.Hide(); });
        }

        void Start()
        {
            var gm = GameManager.Instance;
            artists = gm.Artists;
            studios = gm.Studios;
            economy = gm.Economy;
            economy.MoneyChanged += _ => { if (window.IsOpen && !signing) RefreshButton(); };
        }

        public void Show(ArtistData a)
        {
            artist = a;
            signing = false;
            portrait.sprite = a.avatar;
            portrait.enabled = a.avatar != null;
            var gc = ArtistGenres.Color(a.genre);
            portraitRing.color = gc;
            portraitGlow.color = new Color(gc.r, gc.g, gc.b, 0.35f);
            roleChip.color = new Color(gc.r, gc.g, gc.b, 0.2f);
            roleText.color = gc;
            roleText.text = ArtistGenres.Label(a.genre);
            // stage names keep their own capitalization (e.g. "lil vexi", "Young Vonn")
            nameText.text = a.displayName;
            styleText.text = ArtistGenres.Listeners(a.monthlyListeners);
            bioText.text = string.IsNullOrEmpty(a.personality) ? "" : Loc.F("contract.quote", "text", a.Personality);
            costText.text = NumberFormatter.FormatMoney(a.cost);
            genreText.text = ArtistGenres.Label(a.genre);
            genreText.color = gc;
            if (beatText != null) beatText.text = a.BeatStyle;
            if (popularityPips != null)
                for (int i = 0; i < popularityPips.Length; i++)
                    popularityPips[i].color = i < a.popularity ? gc : new Color(1, 1, 1, 0.12f);
            boostText.text = Loc.F("contract.all_income", "mult", NumberFormatter.FormatMultiplier(a.incomeMultiplier));
            var studio = studios.Get(a.requiredStudioLevel);
            bool studioOk = artists.MeetsStudioRequirement(a);
            requiresText.text = studio != null ? studio.DisplayName : "—";
            requiresText.color = studioOk ? okColor : badColor;

            bool signed = artists.IsSigned(a);
            stamp.alpha = signed ? 1f : 0f;
            stamp.transform.localScale = Vector3.one;
            flash.color = new Color(1, 1, 1, 0);
            RefreshButton();
            window.Show();
            enabled = true;
        }

        void RefreshButton()
        {
            if (artist == null) return;
            bool signed = artists.IsSigned(artist);
            signButton.gameObject.SetActive(!signed);
            if (signed) return;
            if (!artists.MeetsStudioRequirement(artist))
            {
                signLabel.text = Loc.T("ui.locked");
                signCost.text = Loc.F("common.needs", "what", studios.Get(artist.requiredStudioLevel)?.DisplayName ?? Loc.T("common.better_studio"));
                signButton.interactable = false;
                return;
            }
            signLabel.text = Loc.T("contract.sign");
            signCost.text = NumberFormatter.FormatMoney(artist.cost);
            signButton.interactable = artists.CanSign(artist);
        }

        void OnSign()
        {
            if (signing || artist == null) return;
            if (!artists.TrySign(artist)) return;
            StartCoroutine(Ceremony());
        }

        IEnumerator Ceremony()
        {
            signing = true;
            signButton.gameObject.SetActive(false);

            // stamp slams onto the contract
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
            {
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3);
                stamp.alpha = e;
                float s = Mathf.Lerp(2.4f, 1f, e);
                stamp.transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            stamp.alpha = 1f;
            stamp.transform.localScale = Vector3.one;

            // impact: flash, confetti, portrait bounce
            if (burstLayer != null) UIBurst.Spawn(burstLayer, Vector2.zero, 46, confettiSprite);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                flash.color = new Color(1, 1, 1, 0.55f * (1f - t));
                float b = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.12f;
                portraitHolder.localScale = new Vector3(b, b, 1f);
                yield return null;
            }
            flash.color = new Color(1, 1, 1, 0);
            portraitHolder.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(0.9f);
            signing = false;
            window.Hide();
        }

        void Update()
        {
            if (!window.IsOpen) { enabled = false; return; }
            // subtle idle: the glow breathes, the portrait floats
            idle += Time.unscaledDeltaTime;
            if (!signing) portraitHolder.anchoredPosition = new Vector2(0, Mathf.Sin(idle * 1.6f) * 6f) + basePos;
            var g = portraitGlow.color;
            g.a = 0.25f + 0.15f * Mathf.Sin(idle * 2.2f);
            portraitGlow.color = g;
        }

        Vector2 basePos;
    }
}
