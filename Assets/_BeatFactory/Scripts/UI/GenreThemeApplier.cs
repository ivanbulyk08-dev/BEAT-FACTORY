using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Shows the current genre and smoothly recolors the screen with the genre's theme.
    /// </summary>
    public class GenreThemeApplier : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Graphic[] accentGraphics;
        [SerializeField] TMP_Text genreText;
        [SerializeField] TMP_Text multiplierText;
        [SerializeField] float transitionDuration = 0.8f;

        GenreManager genres;
        Color fromBackground, toBackground, fromAccent, toAccent;
        float time;

        void Start()
        {
            genres = GameManager.Instance.Genres;
            genres.CurrentChanged += OnGenreChanged;

            var current = genres.Current;
            if (current != null)
            {
                SetTexts(current);
                toBackground = current.backgroundColor;
                toAccent = current.accentColor;
                ApplyColors(1f);
            }
            Loc.Changed += OnLanguageChanged;
            enabled = false;
        }

        void OnLanguageChanged()
        {
            if (genres.Current != null) SetTexts(genres.Current);
        }

        void OnDestroy()
        {
            if (genres != null) genres.CurrentChanged -= OnGenreChanged;
            Loc.Changed -= OnLanguageChanged;
        }

        void OnGenreChanged(GenreData genre)
        {
            if (genre == null) return;
            SetTexts(genre);

            fromBackground = background != null ? background.color : genre.backgroundColor;
            fromAccent = genreText.color;
            toBackground = genre.backgroundColor;
            toAccent = genre.accentColor;
            time = 0f;
            enabled = true;
        }

        void SetTexts(GenreData genre)
        {
            string name = Loc.Upper(genre.DisplayName);
            string mult = genre.incomeMultiplier > 1 ? NumberFormatter.FormatMultiplier(genre.incomeMultiplier) : "";
            if (multiplierText != null)
            {
                genreText.text = name;
                multiplierText.text = mult;
            }
            else
            {
                // Single line: "HIP-HOP  ×3"
                genreText.text = mult.Length > 0 ? name + "  <b>" + mult + "</b>" : name;
            }
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / transitionDuration);
            ApplyColors(t * t * (3f - 2f * t)); // smoothstep
            if (t >= 1f) enabled = false;
        }

        void ApplyColors(float t)
        {
            if (background != null) background.color = Color.Lerp(fromBackground, toBackground, t);
            var accent = Color.Lerp(fromAccent, toAccent, t);
            genreText.color = accent;
            if (multiplierText != null) multiplierText.color = accent;
            foreach (var g in accentGraphics)
                if (g != null) g.color = accent;
        }
    }
}
