using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>ALBUMS list: one card per album, refreshed as beats/producers/studio/artists change.</summary>
    public class AlbumsPanelUI : MonoBehaviour
    {
        [SerializeField] AlbumCardView cardPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] float refreshInterval = 0.5f;

        readonly List<AlbumCardView> cards = new List<AlbumCardView>();
        AlbumManager albums;
        float timer;

        void Start()
        {
            var gm = GameManager.Instance;
            albums = gm.Albums;
            foreach (var a in albums.Albums)
            {
                var card = Instantiate(cardPrefab, container);
                card.name = "Album_" + a.id;
                card.Bind(a, albums, Release);
                cards.Add(card);
            }
            albums.Changed += RefreshAll;
            RefreshAll();
        }

        void OnDestroy()
        {
            if (albums != null) albums.Changed -= RefreshAll;
        }

        void OnEnable()
        {
            if (albums != null) RefreshAll();
        }

        void Release(AlbumData a) => albums.TryRelease(a);

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < refreshInterval) return;
            timer = 0f;
            RefreshAll();
        }

        void RefreshAll()
        {
            foreach (var c in cards) c.Refresh();
            if (summaryText != null)
                summaryText.text = Loc.F("album.summary", "released", albums.ReleasedCount, "total", albums.Albums.Count,
                                   "mult", "<b>" + NumberFormatter.FormatMultiplier(albums.Multiplier) + "</b>");
        }
    }
}
