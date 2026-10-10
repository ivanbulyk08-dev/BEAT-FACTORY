using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Album release ceremony: screen darkens, the cover spins/scales into view, the title appears,
    /// a small visualizer plays, then the money / reputation / poster rewards appear one by one.
    /// </summary>
    public class AlbumReleasePresentation : MonoBehaviour
    {
        [SerializeField] GameObject window;
        [SerializeField] CanvasGroup group;
        [SerializeField] Image dim;
        [SerializeField] Image spotlight;
        [SerializeField] RectTransform coverHolder;
        [SerializeField] Image cover;
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] RectTransform[] bars;
        [SerializeField] TMP_Text[] rewardLines;
        [SerializeField] Button continueButton;
        [SerializeField] RectTransform burstLayer;
        [SerializeField] Sprite confettiSprite;

        AlbumManager albums;
        bool playing;
        float vizTime;

        void Start()
        {
            albums = GameManager.Instance.Albums;
            albums.Released += OnReleased;
            continueButton.onClick.AddListener(() => StartCoroutine(Close()));
            window.SetActive(false);
        }

        void OnDestroy()
        {
            if (albums != null) albums.Released -= OnReleased;
        }

        void OnReleased(AlbumData album, double reward)
        {
            StopAllCoroutines();
            StartCoroutine(Play(album, reward));
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.6f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }

        IEnumerator Play(AlbumData album, double reward)
        {
            playing = true;
            window.SetActive(true);
            window.transform.SetAsLastSibling();
            group.alpha = 1f;
            group.blocksRaycasts = true;
            cover.sprite = album.cover;
            spotlight.color = new Color(album.accent.r, album.accent.g, album.accent.b, 0f);
            headerText.text = Loc.F("album.out_now", "kind", album.Kind);
            titleText.text = album.title;
            // feature the biggest artist on your roster
            ArtistData feat = null;
            var roster = GameManager.Instance.Artists;
            foreach (var a in roster.Artists) if (roster.IsSigned(a)) feat = a;
            subtitleText.text = (feat != null ? Loc.F("album.feat", "artist", "<b>" + feat.displayName + "</b>") + "   ·   " : "") +
                                AlbumCardView.GenreLabel(album.genre) + "   ·   " + album.Tagline;
            foreach (var t in new[] { headerText, titleText, subtitleText }) t.alpha = 0f;
            rewardLines[0].text = "+" + NumberFormatter.FormatMoney(reward);
            rewardLines[1].text = Loc.F("album.reveal_reputation", "mult", NumberFormatter.FormatMultiplier(album.reputationMultiplier));
            rewardLines[2].text = Loc.T("album.reveal_poster");
            foreach (var r in rewardLines) r.alpha = 0f;
            continueButton.gameObject.SetActive(false);
            coverHolder.localScale = Vector3.zero;

            // 1. darken
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                dim.color = new Color(0.02f, 0.015f, 0.04f, 0.97f * t); // near-opaque: Linear color space makes alpha look weaker
                yield return null;
            }
            // 2-3. cover spins and scales into view
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                float e = EaseOutBack(Mathf.Clamp01(t));
                coverHolder.localScale = Vector3.one * e;
                coverHolder.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-25f, 0f, Mathf.Clamp01(t * 1.3f)));
                spotlight.color = new Color(album.accent.r, album.accent.g, album.accent.b, 0.4f * Mathf.Clamp01(t));
                yield return null;
            }
            coverHolder.localScale = Vector3.one;
            coverHolder.localEulerAngles = Vector3.zero;
            if (burstLayer != null) UIBurst.Spawn(burstLayer, Vector2.zero, 60, confettiSprite);

            // 4. title
            yield return Fade(headerText, 0.25f);
            yield return Fade(titleText, 0.35f);
            yield return Fade(subtitleText, 0.25f);
            // 6. rewards one by one
            foreach (var r in rewardLines)
            {
                yield return new WaitForSecondsRealtime(0.18f);
                yield return Fade(r, 0.25f);
            }
            continueButton.gameObject.SetActive(true);
        }

        static IEnumerator Fade(TMP_Text t, float duration)
        {
            for (float a = 0; a < 1f; a += Time.unscaledDeltaTime / duration)
            {
                t.alpha = a;
                t.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, a);
                yield return null;
            }
            t.alpha = 1f;
            t.transform.localScale = Vector3.one;
        }

        IEnumerator Close()
        {
            continueButton.interactable = false;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
            {
                group.alpha = 1f - t;
                yield return null;
            }
            group.blocksRaycasts = false;
            window.SetActive(false);
            continueButton.interactable = true;
            playing = false;
        }

        void Update()
        {
            if (!playing || bars == null) return;
            // 5. small music visualizer under the cover
            vizTime += Time.unscaledDeltaTime;
            for (int i = 0; i < bars.Length; i++)
            {
                float beat = Mathf.Pow(1f - Mathf.Repeat(vizTime * 85f / 60f + i * 0.03f, 1f), 2f);
                float n = Mathf.PerlinNoise(i * 0.4f, vizTime * 3f);
                float h = Mathf.Lerp(0.12f, 1f, n * 0.6f + beat * 0.4f);
                bars[i].localScale = new Vector3(1f, h, 1f);
            }
        }
    }
}
