using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Reusable "Are you sure?" popup. Usage:
    /// confirmDialog.Show("RESTART LABEL?", "You will lose ...", "RESTART", () => DoIt());
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] ModalWindow window;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] Button confirmButton;
        [SerializeField] TMP_Text confirmLabel;
        [SerializeField] Image confirmImage;
        [SerializeField] Button cancelButton;

        Action onConfirm;

        void Awake()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(() => window.Hide());
        }

        public void Show(string title, string message, string confirmText, Color confirmColor, Action confirm)
        {
            titleText.text = title;
            messageText.text = message;
            confirmLabel.text = confirmText;
            confirmImage.color = confirmColor;
            onConfirm = confirm;
            window.Show();
        }

        void OnConfirm()
        {
            var action = onConfirm;
            onConfirm = null;
            window.Hide();
            action?.Invoke();
        }
    }
}
