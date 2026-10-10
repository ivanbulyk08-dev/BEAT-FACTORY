using System;
using UnityEngine;

namespace BeatFactory
{
    [Serializable]
    public class StreamPlatform
    {
        [Tooltip("Unique id used by the save system. Do not change it after release.")]
        public string id;
        public string displayName;
        [Tooltip("1-2 letters on the placeholder icon.")]
        public string shortName;
        [Tooltip("Streams multiplier. Multipliers of all released platforms are ADDED together.")]
        public double streamMultiplier = 1;
        [Tooltip("Price to release your music on this platform. 0 = available from the start.")]
        public double releaseCost;
        public Color color = Color.white;
    }

    /// <summary>
    /// Streaming settings. Good beats earn streams on every platform you released on;
    /// streams can be cashed out for money.
    /// </summary>
    [CreateAssetMenu(fileName = "StreamConfig", menuName = "Beat Factory/Stream Config")]
    public class StreamConfig : ScriptableObject
    {
        [Tooltip("Money paid for 1,000 streams when cashing out.")]
        [Min(0.0001f)] public double moneyPerThousandStreams = 1000; // 1 stream = $1, so the stream count equals the cash-out money

        public StreamPlatform[] platforms =
        {
            new StreamPlatform { id = "soundcloud", displayName = "SoundCloud", shortName = "SC", streamMultiplier = 0.8, releaseCost = 0,           color = new Color(1f, 0.53f, 0.2f) },
            new StreamPlatform { id = "spotify",    displayName = "Spotify",    shortName = "SP", streamMultiplier = 1.0, releaseCost = 250_000,       color = new Color(0.12f, 0.84f, 0.38f) },
            new StreamPlatform { id = "youtube",    displayName = "YouTube",    shortName = "YT", streamMultiplier = 1.2, releaseCost = 20_000_000,    color = new Color(1f, 0.2f, 0.2f) },
            new StreamPlatform { id = "tiktok",     displayName = "TikTok",     shortName = "TT", streamMultiplier = 2.0, releaseCost = 2_000_000_000, color = new Color(0.2f, 0.95f, 0.95f) },
        };

        public double MoneyPerStream => moneyPerThousandStreams / 1000.0;
    }
}
