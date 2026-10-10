using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Artist card: portrait with a genre ring, big stage name, genre chip, popularity pips + monthly listeners,
    /// and either a CONTRACT price button, a SIGNED badge or the unlock requirement.
    /// </summary>
    public class ArtistCardView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Button cardButton;
        [SerializeField] Image portrait;
        [SerializeField] Image ring;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image genreChip;
        [SerializeField] TMP_Text genreText;
        [SerializeField] Image[] popularityPips;
        [SerializeField] TMP_Text listenersText;
        [SerializeField] Button contractButton;
        [SerializeField] TMP_Text contractLabel;
        [SerializeField] TMP_Text contractPrice;
        [SerializeField] GameObject signedBadge;
        [SerializeField] TMP_Text lockedText;
        [SerializeField] CardFlash flash;
        [SerializeField] Color pipOff = new Color(1f, 1f, 1f, 0.12f);

        ArtistData artist;
        Action<ArtistData> onOpen;

        void Awake()
        {
            cardButton.onClick.AddListener(() => onOpen?.Invoke(artist));
            contractButton.onClick.AddListener(() => onOpen?.Invoke(artist));
        }

        public ArtistData Artist => artist;

        public void Bind(ArtistData a, Action<ArtistData> open)
        {
            artist = a;
            onOpen = open;
            var gc = ArtistGenres.Color(a.genre);
            portrait.sprite = a.avatar;
            portrait.enabled = a.avatar != null;
            ring.color = gc;
            nameText.text = a.displayName;
            genreChip.color = new Color(gc.r, gc.g, gc.b, 0.16f);
            genreText.color = gc;
            for (int i = 0; i < popularityPips.Length; i++) popularityPips[i].color = i < a.popularity ? gc : pipOff;
            ApplyTexts();
        }

        /// <summary>Language-dependent texts (called again when the language changes).</summary>
        public void ApplyTexts()
        {
            genreText.text = ArtistGenres.Label(artist.genre) + "   <color=#8A84B0>" + Loc.F("artist.income_chip", "mult", NumberFormatter.FormatMultiplier(artist.incomeMultiplier)) + "</color>";
            listenersText.text = ArtistGenres.Listeners(artist.monthlyListeners);
        }

        /// <summary>signed: badge · unlocked: CONTRACT price · locked: requirement text.</summary>
        public void SetState(bool signed, bool unlocked, bool affordable, string requirement)
        {
            // signed artists have a career: the button opens concerts & tours instead of the contract
            signedBadge.SetActive(false);
            contractButton.gameObject.SetActive(signed || unlocked);
            lockedText.gameObject.SetActive(!signed && !unlocked);
            lockedText.text = requirement;
            group.alpha = unlocked || signed ? 1f : 0.5f;
            contractButton.interactable = true;
            var img = contractButton.targetGraphic as Image;
            var gm = GameManager.Instance;
            if (signed && gm != null && gm.Concerts != null)
            {
                var c = gm.Concerts.Career(artist);
                var tour = gm.Tours.ActiveFor(artist);
                contractLabel.text = Loc.T(tour != null ? "artist.on_tour" : "artist.concerts_button");
                contractPrice.text = Loc.Upper(Loc.P("count.fans", c.fans));
                listenersText.text = "<color=#F2C14E>" + c.Stage + "</color>  ·  " + Loc.F("artist.popularity_value", "value", UIKit.Pct(c.popularity)) +
                                     (tour != null ? "  ·  <color=#FF3DC8>" + Loc.F("artist.tour_shows", "done", tour.ShowsDone(System.DateTime.UtcNow), "total", tour.shows) + "</color>" : "");
                if (img != null) img.color = new Color(1f, 0.24f, 0.78f);
                return;
            }
            listenersText.text = ArtistGenres.Listeners(artist.monthlyListeners);
            contractLabel.text = Loc.T("artist.contract");
            contractPrice.text = NumberFormatter.FormatMoney(artist.cost);
            if (img != null) img.color = affordable ? new Color(0.95f, 0.76f, 0.31f) : new Color(0.38f, 0.36f, 0.45f);
        }

        public void Flash()
        {
            if (flash != null) flash.Flash();
        }
    }
}
