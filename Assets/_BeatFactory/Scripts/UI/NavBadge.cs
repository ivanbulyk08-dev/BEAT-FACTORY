using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Small dot on a navigation tab that tells the player "you can buy something here".
    /// Checked a few times per second, not every frame.
    /// </summary>
    public class NavBadge : MonoBehaviour
    {
        public enum Source
        {
            Music,      // next genre or studio is affordable
            Producers,  // any producer level is affordable
            Artists,    // an artist can be signed
            Prestige,   // a restart would at least double your Fame (or it's the first one)
        }

        [SerializeField] Source source;
        [SerializeField] GameObject dot;
        [SerializeField] float checkInterval = 0.5f;

        float timer;

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            var gm = GameManager.Instance;
            if (gm == null) return;

            bool show = false;
            switch (source)
            {
                case Source.Music:
                    show = gm.Genres.CanUnlockNext() || gm.Studios.CanUpgrade() || gm.Streams.CanReleaseAny();
                    break;
                case Source.Prestige:
                    show = gm.Prestige.CanPrestige && gm.Prestige.PendingFame >= System.Math.Max(1, gm.Economy.FamePoints);
                    break;
                case Source.Artists:
                    show = gm.Artists.CanSignAny() || gm.Albums.CanReleaseAny();
                    break;
                case Source.Producers:
                    var list = gm.Producers.Producers; // index loop: no enumerator allocation
                    for (int i = 0; i < list.Count; i++)
                        if (gm.Producers.CanLevelUp(list[i])) { show = true; break; }
                    break;
            }
            if (dot.activeSelf != show) dot.SetActive(show);
        }
    }
}
