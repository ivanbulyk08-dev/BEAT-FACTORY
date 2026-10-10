using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Promo posters of the artists you signed hang in the studio (portrait + stage name + genre color).
    /// Better rooms have more poster spots, so the walls tell the story of your career.
    /// Signing an artist pops their poster onto the wall.
    /// </summary>
    public class ArtistPosterWall : MonoBehaviour, IWallDecor
    {
        [System.Serializable]
        public class Poster
        {
            public GameObject root;
            public Image portrait;
            public Image strip;
            public TMP_Text nameText;
        }

        [System.Serializable]
        public class TierPosters
        {
            public GameObject root;
            public Poster[] posters;
        }

        [SerializeField] TierPosters[] tiers;

        ArtistManager artists;
        StudioManager studios;

        void Start()
        {
            var gm = GameManager.Instance;
            artists = gm.Artists;
            studios = gm.Studios;
            artists.Changed += Refresh;
            artists.ArtistSigned += OnSigned;
            studios.CurrentChanged += _ => Refresh();
            Refresh();
        }

        void OnDestroy()
        {
            if (artists == null) return;
            artists.Changed -= Refresh;
            artists.ArtistSigned -= OnSigned;
        }

        /// <summary>Signed artists, biggest names first.</summary>
        List<ArtistData> Signed()
        {
            var list = new List<ArtistData>();
            for (int i = artists.Artists.Count - 1; i >= 0; i--)
                if (artists.IsSigned(artists.Artists[i])) list.Add(artists.Artists[i]);
            return list;
        }

        void Refresh()
        {
            int tier = StudioSceneView.TierFor(studios.Level);
            var signed = Signed();
            for (int t = 0; t < tiers.Length; t++)
            {
                tiers[t].root.SetActive(t == tier);
                for (int i = 0; i < tiers[t].posters.Length; i++)
                {
                    var p = tiers[t].posters[i];
                    bool has = i < signed.Count;
                    p.root.SetActive(has);
                    if (!has) continue;
                    var a = signed[i];
                    p.portrait.sprite = a.avatar;
                    var gc = ArtistGenres.Color(a.genre);
                    p.strip.color = gc;
                    p.nameText.text = a.displayName;
                }
            }
        }

        void OnSigned(ArtistData a)
        {
            int tier = StudioSceneView.TierFor(studios.Level);
            if (tier >= tiers.Length) return;
            int index = Signed().IndexOf(a);
            if (index < 0 || index >= tiers[tier].posters.Length) return;
            StartCoroutine(PopOne(tiers[tier].posters[index].root.transform, 0.2f));
        }

        public void HideAll()
        {
            foreach (var t in tiers) foreach (var p in t.posters) p.root.transform.localScale = Vector3.zero;
        }

        public void PopAll()
        {
            int n = 0;
            foreach (var t in tiers)
                foreach (var p in t.posters)
                    if (p.root.activeInHierarchy) StartCoroutine(PopOne(p.root.transform, 0.1f + n++ * 0.09f));
                    else p.root.transform.localScale = Vector3.one;
        }

        static IEnumerator PopOne(Transform t, float delay)
        {
            t.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(delay);
            for (float a = 0; a < 1f; a += Time.unscaledDeltaTime / 0.35f)
            {
                float s = a < 0.6f ? Mathf.Lerp(0f, 1.2f, a / 0.6f) : Mathf.Lerp(1.2f, 1f, (a - 0.6f) / 0.4f);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
