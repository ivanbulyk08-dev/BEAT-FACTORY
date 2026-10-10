using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Released albums hang as posters in the studio — a visual history of the career.
    /// Basement: taped posters · Bedroom: black frames · Professional / Modern: gold frames.
    /// One slot group per studio tier; newest album first.
    /// </summary>
    public class AlbumWall : MonoBehaviour, IWallDecor
    {
        public void HideAll()
        {
            foreach (var t in tiers) foreach (var c in t.covers) c.transform.parent.localScale = Vector3.zero;
        }

        public void PopAll()
        {
            int n = 0;
            foreach (var t in tiers)
                foreach (var c in t.covers)
                    if (c.transform.parent.gameObject.activeInHierarchy) StartCoroutine(PopOne(c.transform.parent, n++ * 0.08f));
                    else c.transform.parent.localScale = Vector3.one;
        }

        System.Collections.IEnumerator PopOne(Transform t, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            for (float a = 0; a < 1f; a += Time.unscaledDeltaTime / 0.35f)
            {
                float s = a < 0.6f ? Mathf.Lerp(0f, 1.2f, a / 0.6f) : Mathf.Lerp(1.2f, 1f, (a - 0.6f) / 0.4f);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        [System.Serializable]
        public class TierSlots
        {
            public GameObject root;
            public Image[] covers;
        }

        [SerializeField] TierSlots[] tiers;
        [SerializeField] float popDuration = 0.45f;

        AlbumManager albums;
        StudioManager studios;
        Transform popping;
        float popTime;

        void Start()
        {
            var gm = GameManager.Instance;
            albums = gm.Albums;
            studios = gm.Studios;
            albums.Changed += Refresh;
            albums.Released += OnReleased;
            studios.CurrentChanged += _ => Refresh();
            Refresh();
        }

        void OnDestroy()
        {
            if (albums == null) return;
            albums.Changed -= Refresh;
            albums.Released -= OnReleased;
        }

        void Refresh()
        {
            int tier = StudioSceneView.TierFor(studios.Level);
            List<AlbumData> list = albums.ReleasedNewestFirst();
            for (int t = 0; t < tiers.Length; t++)
            {
                tiers[t].root.SetActive(t == tier);
                for (int i = 0; i < tiers[t].covers.Length; i++)
                {
                    var holder = tiers[t].covers[i].transform.parent.gameObject; // frame holder
                    bool has = i < list.Count;
                    holder.SetActive(has);
                    if (has) tiers[t].covers[i].sprite = list[i].cover;
                }
            }
        }

        void OnReleased(AlbumData album, double _)
        {
            int tier = StudioSceneView.TierFor(studios.Level);
            if (tier >= tiers.Length || tiers[tier].covers.Length == 0) return;
            popping = tiers[tier].covers[0].transform.parent;
            popTime = 0f;
        }

        void Update()
        {
            if (popping == null) return;
            popTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(popTime / popDuration);
            float s = p < 0.6f ? Mathf.Lerp(0f, 1.25f, p / 0.6f) : Mathf.Lerp(1.25f, 1f, (p - 0.6f) / 0.4f);
            popping.localScale = new Vector3(s, s, 1f);
            if (p >= 1f) { popping.localScale = Vector3.one; popping = null; }
        }
    }
}
