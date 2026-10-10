using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Generic list row used by shop-like screens (genres, studios, artists):
    /// colored icon with letters, title, subtitle and either a buy button or a status text.
    /// Holds no game logic — the screen script fills it in.
    /// </summary>
    public class ShopRowView : MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image iconBackground;
        [SerializeField] TMP_Text iconLetters;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] Button button;
        [SerializeField] Image buttonImage;
        [SerializeField] TMP_Text buttonLabel;
        [SerializeField] TMP_Text buttonCost;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Image iconImage;
        [SerializeField] CardFlash flash;
        [SerializeField] Button rowButton;

        ButtonPunch punch;
        Action onClick;
        Action rowClick;

        void Awake()
        {
            punch = button.GetComponent<ButtonPunch>();
            button.onClick.AddListener(() => onClick?.Invoke());
            if (rowButton != null)
            {
                rowButton.onClick.AddListener(() => rowClick?.Invoke());
                rowButton.enabled = false;
            }
        }

        public void SetIcon(Color color, string letters)
        {
            iconBackground.color = color;
            iconLetters.text = letters;
            if (iconImage != null) iconImage.enabled = false;
        }

        public void SetTexts(string title, string subtitle)
        {
            titleText.text = title;
            subtitleText.text = subtitle;
        }

        public void SetLocked(bool locked, float lockedAlpha = 0.45f) => canvasGroup.alpha = locked ? lockedAlpha : 1f;

        /// <summary>Shows a buy button instead of the status text.</summary>
        public void ShowButton(string label, string cost, Color color, bool interactable, Action click)
        {
            onClick = click;
            button.gameObject.SetActive(true);
            statusText.gameObject.SetActive(false);
            buttonLabel.text = label;
            buttonCost.text = cost;
            buttonImage.color = color;
            button.interactable = interactable;
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        /// <summary>Shows a status text (e.g. "ACTIVE", "LOCKED") instead of the button.</summary>
        public void ShowStatus(string status, Color color)
        {
            onClick = null;
            button.gameObject.SetActive(false);
            statusText.gameObject.SetActive(true);
            statusText.text = status;
            statusText.color = color;
        }

        public void Punch()
        {
            if (punch != null) punch.Punch();
            if (flash != null) flash.Flash();
        }

        /// <summary>Optional portrait/artwork instead of the letter icon.</summary>
        public void SetIconSprite(Sprite sprite, Color background)
        {
            iconBackground.color = background;
            if (iconImage == null) return;
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
            iconLetters.text = sprite != null ? "" : iconLetters.text;
        }

        /// <summary>Whole row is clickable (e.g. "view artist").</summary>
        public void SetRowClick(Action click)
        {
            rowClick = click;
            if (rowButton != null) rowButton.enabled = click != null;
        }
    }
}
