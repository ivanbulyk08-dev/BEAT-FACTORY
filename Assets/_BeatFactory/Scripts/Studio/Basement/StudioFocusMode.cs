using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// "Studio only" HOME screen: the basement artwork and the MAKE BEAT button (the top bar, boosts and bottom navigation
    /// stay). The producer list, studio card and tutorial are hidden with CanvasGroups — not deleted — so every system,
    /// the save, economy, ads and localization keep running. Untick <see cref="focusOnStudio"/> to get the full UI back.
    /// </summary>
    public class StudioFocusMode : MonoBehaviour
    {
        public bool focusOnStudio = true;
        [Tooltip("UI hidden while focusing on the studio.")]
        [SerializeField] RectTransform[] hidden = new RectTransform[0];
        [Tooltip("Scripts paused while focusing (e.g. the tutorial, which points at hidden buttons).")]
        [SerializeField] Behaviour[] paused = new Behaviour[0];
        [Tooltip("Studio card: its panel is hidden while focusing (the painted room shows through), its buttons stay.")]
        [SerializeField] UnityEngine.UI.Mask cardMask;
        [Tooltip("Studio card overlays: the next-goal/upgrade button and STYLE share one compact row at the top so the room stays visible.")]
        [SerializeField] RectTransform nextGoal;
        [SerializeField] RectTransform styleButton;
        [SerializeField] float rowTop = 8f, rowHeight = 84f;
        [SerializeField] RectTransform makeBeatButton;
        [SerializeField] RectTransform makeBeatGlow;
        [Tooltip("MAKE BEAT distance from the bottom of its screen and height (reference pixels).")]
        [SerializeField] float buttonBottom = 14f, buttonHeight = 200f, sideMargin = 44f;

        void Start() => Apply();

        void OnValidate()
        {
            if (Application.isPlaying) Apply();
        }

        public void Apply()
        {
            foreach (var rt in hidden)
            {
                if (rt == null) continue;
                var cg = rt.GetComponent<CanvasGroup>();
                if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>(); // (no ?? : Unity's fake-null)
                cg.alpha = focusOnStudio ? 0f : 1f;
                cg.blocksRaycasts = cg.interactable = !focusOnStudio;
            }
            foreach (var b in paused) if (b != null) b.enabled = !focusOnStudio;
            if (cardMask != null) cardMask.showMaskGraphic = !focusOnStudio;
            if (!focusOnStudio) return;
            if (nextGoal != null)
            {
                nextGoal.anchorMin = nextGoal.anchorMax = new Vector2(0, 1);
                nextGoal.pivot = new Vector2(0, 1);
                nextGoal.anchoredPosition = new Vector2(0, -rowTop);
                nextGoal.sizeDelta = new Vector2(560, rowHeight);
            }
            if (styleButton != null)
            {
                styleButton.anchorMin = styleButton.anchorMax = new Vector2(1, 1);
                styleButton.pivot = new Vector2(1, 1);
                // same row as the next goal (the studio card clips anything above it)
                styleButton.anchoredPosition = new Vector2(0, -rowTop);
                styleButton.sizeDelta = new Vector2(210, rowHeight);
            }
            Place(makeBeatButton, 0f);
            Place(makeBeatGlow, 40f);
        }

        void Place(RectTransform rt, float pad)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(sideMargin - pad, buttonBottom - pad);
            rt.offsetMax = new Vector2(-sideMargin + pad, buttonBottom + buttonHeight + pad);
        }
    }
}
