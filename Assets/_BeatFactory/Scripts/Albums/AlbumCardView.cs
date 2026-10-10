using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Album card: cover, title, genre, requirements checklist, progress, reward and RELEASE button.</summary>
    public class AlbumCardView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Image cover;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text kindText;
        [SerializeField] TMP_Text requirementsText;
        [SerializeField] RectTransform progressFill;
        [SerializeField] Image progressImage;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] Button releaseButton;
        [SerializeField] TMP_Text releaseLabel;
        [SerializeField] TMP_Text releaseValue;
        [SerializeField] GameObject releasedBadge;
        [SerializeField] CardFlash flash;
        [SerializeField] Color doneColor = new Color(0.3f, 1f, 0.66f);
        [SerializeField] Color todoColor = new Color(0.54f, 0.52f, 0.69f);

        AlbumData album;
        AlbumManager albums;
        Action<AlbumData> onRelease;
        bool wasReady;

        void Awake() => releaseButton.onClick.AddListener(() => onRelease?.Invoke(album));

        public void Bind(AlbumData a, AlbumManager manager, Action<AlbumData> release)
        {
            album = a;
            albums = manager;
            onRelease = release;
            cover.sprite = a.cover;
            titleText.text = a.title;
            Refresh();
        }

        public void Refresh()
        {
            bool released = albums.IsReleased(album);
            bool isNext = albums.Next == album;
            string genre = "<color=#" + ColorUtility.ToHtmlStringRGB(album.accent) + ">" + GenreLabel(album.genre) + "</color>";
            kindText.text = album.Kind + "   ·   " + genre;

            releasedBadge.SetActive(released);
            group.alpha = released || isNext ? 1f : 0.45f;

            var sb = new StringBuilder();
            if (released)
            {
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(doneColor)).Append(">").Append(Loc.T("album.released_note")).Append("</color>");
                SetProgress(1f, doneColor);
                rewardText.text = Loc.F("album.reputation_income", "mult", NumberFormatter.FormatMultiplier(album.reputationMultiplier));
                releaseButton.gameObject.SetActive(false);
            }
            else
            {
                var reqs = albums.GetRequirements(album);
                for (int i = 0; i < reqs.Length; i++)
                {
                    var r = reqs[i];
                    string col = ColorUtility.ToHtmlStringRGB(r.Met ? doneColor : todoColor);
                    sb.Append("<color=#").Append(col).Append(">").Append(r.Met ? "● " : "○ ").Append(r.label).Append("  <b>")
                      .Append(NumberFormatter.Format(Math.Min(r.current, r.target))).Append("/").Append(NumberFormatter.Format(r.target)).Append("</b></color>");
                    sb.Append(i % 2 == 0 && i < reqs.Length - 1 ? "      " : "\n");
                }
                SetProgress(albums.Progress(album), album.accent);
                rewardText.text = Loc.F("album.reward_line", "money", NumberFormatter.FormatMoney(albums.RewardFor(album)),
                                  "mult", NumberFormatter.FormatMultiplier(album.reputationMultiplier));
                bool ready = albums.CanRelease(album);
                releaseButton.gameObject.SetActive(isNext);
                releaseButton.interactable = ready;
                releaseLabel.text = Loc.T(ready ? "album.release" : "album.in_progress");
                releaseValue.text = NumberFormatter.Percent(Mathf.RoundToInt(albums.Progress(album) * 100));
                if (ready && !wasReady && flash != null) flash.Flash();
                wasReady = ready;
                if (!isNext) sb.Append(Loc.T("album.previous_first"));
            }
            requirementsText.text = sb.ToString().TrimEnd('\n');
        }

        void SetProgress(float p, Color c)
        {
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(p), 1);
            progressImage.color = c;
        }

        public static string GenreLabel(AlbumGenre g) => Loc.T("album_genre." + g.ToString().ToLowerInvariant());
    }
}
