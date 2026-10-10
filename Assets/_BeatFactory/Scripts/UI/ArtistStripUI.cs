using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Row of artist avatars inside the studio card: signed artists "hang out" in your studio.
    /// Empty slots show a dim "?". A newly signed artist pops in.
    /// </summary>
    public class ArtistStripUI : MonoBehaviour
    {
        [SerializeField] Image[] slots;
        [SerializeField] TMP_Text[] slotLetters;
        [SerializeField] Color emptyColor = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] Color emptyTextColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] Color letterColor = new Color(0.05f, 0.04f, 0.1f);
        [SerializeField] float popDuration = 0.35f;

        ArtistManager artists;
        int poppingSlot = -1;
        float popTime;

        Sprite defaultSprite;

        void Start()
        {
            defaultSprite = slots.Length > 0 ? slots[0].sprite : null;
            artists = GameManager.Instance.Artists;
            artists.Changed += Refresh;
            artists.ArtistSigned += OnSigned;
            Refresh();
            enabled = false;
        }

        void OnDestroy()
        {
            if (artists == null) return;
            artists.Changed -= Refresh;
            artists.ArtistSigned -= OnSigned;
        }

        /// <summary>The artists shown in the studio: the most valuable signed artists (roster order), newest tier first.</summary>
        System.Collections.Generic.List<ArtistData> Shown()
        {
            var signedList = new System.Collections.Generic.List<ArtistData>();
            var list = artists.Artists;
            for (int i = list.Count - 1; i >= 0 && signedList.Count < slots.Length; i--)
                if (artists.IsSigned(list[i])) signedList.Add(list[i]);
            return signedList;
        }

        void Refresh()
        {
            var shown = Shown();
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(true);
                bool signed = i < shown.Count;
                var a = signed ? shown[i] : null;
                if (signed && a.avatar != null)
                {
                    slots[i].sprite = a.avatar;
                    slots[i].color = Color.white;
                    slotLetters[i].text = "";
                }
                else
                {
                    slots[i].sprite = defaultSprite;
                    slots[i].color = signed ? a.color : emptyColor;
                    slotLetters[i].text = signed ? a.shortName : "?";
                    slotLetters[i].color = signed ? letterColor : emptyTextColor;
                }
            }
        }

        void OnSigned(ArtistData artist)
        {
            poppingSlot = Shown().IndexOf(artist);
            if (poppingSlot < 0 || poppingSlot >= slots.Length) return;
            popTime = 0f;
            enabled = true;
        }

        void Update()
        {
            popTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(popTime / popDuration);
            // 0 -> 1.35 -> 1
            float s = p < 0.5f ? Mathf.Lerp(0.2f, 1.35f, p / 0.5f) : Mathf.Lerp(1.35f, 1f, (p - 0.5f) / 0.5f);
            slots[poppingSlot].transform.localScale = new Vector3(s, s, 1f);
            if (p >= 1f)
            {
                slots[poppingSlot].transform.localScale = Vector3.one;
                enabled = false;
            }
        }
    }
}
