using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>Large visual card in the STYLE menu: thumbnail, name, equipped highlight or lock.</summary>
    public class StyleItemCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image thumbnail;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text tagText;
        [SerializeField] Image selectedFrame;
        [SerializeField] GameObject equippedBadge;
        [SerializeField] GameObject lockOverlay;
        [SerializeField] TMP_Text lockText;

        public AvatarItem Item { get; private set; }
        Action<StyleItemCard> onClick;
        float pop = -1f, shake = -1f;

        void Awake() => button.onClick.AddListener(() => onClick?.Invoke(this));

        public void Bind(AvatarItem item, Sprite thumb, string tag, Action<StyleItemCard> click)
        {
            Item = item;
            onClick = click;
            thumbnail.sprite = thumb;
            thumbnail.enabled = thumb != null;
            nameText.text = item.DisplayName;
            tagText.text = tag;
        }

        public void SetState(bool equipped, bool unlocked, string lockLabel)
        {
            selectedFrame.enabled = equipped;
            equippedBadge.SetActive(equipped);
            lockOverlay.SetActive(!unlocked);
            lockText.text = lockLabel;
        }

        /// <summary>Selection confirmation: quick pop.</summary>
        public void Pop() { pop = 0f; enabled = true; }

        /// <summary>Locked item tapped: small shake.</summary>
        public void Shake() { shake = 0f; enabled = true; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float s = 1f, x = 0f;
            if (pop >= 0f)
            {
                pop += dt / 0.3f;
                s = 1f + Mathf.Sin(Mathf.Clamp01(pop) * Mathf.PI) * 0.1f;
                if (pop >= 1f) pop = -1f;
            }
            if (shake >= 0f)
            {
                shake += dt / 0.35f;
                x = Mathf.Sin(shake * Mathf.PI * 6f) * 10f * (1f - Mathf.Clamp01(shake));
                if (shake >= 1f) shake = -1f;
            }
            thumbnail.transform.localScale = new Vector3(s, s, 1f);
            thumbnail.rectTransform.anchoredPosition = new Vector2(x, thumbnail.rectTransform.anchoredPosition.y);
            if (pop < 0f && shake < 0f) enabled = false;
        }
    }
}
