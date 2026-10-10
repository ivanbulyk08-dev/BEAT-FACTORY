using UnityEngine;

namespace BeatFactory
{
    /// <summary>Shared helpers for showing beat results (text and color depend on quality).</summary>
    public static class BeatFeedback
    {
        /// <summary>"+$5" for normal beats, "+$10 / GOOD" (two lines) for better ones.</summary>
        public static string RewardText(in BeatResult result, BeatQualityConfig config)
        {
            string money = "+" + NumberFormatter.FormatMoney(result.Reward);
            if (result.Quality == BeatQuality.Normal)
                return result.Source == BeatSource.Manual ? "<size=45%>" + Loc.T("home.beat_created") + "</size>\n" + money : money;
            return money + "\n<size=55%>" + config.GetTier(result.Quality).Label + "</size>";
        }

        public static Color RewardColor(in BeatResult result, BeatQualityConfig config, Color normalColor) =>
            result.Quality == BeatQuality.Normal ? normalColor : config.GetTier(result.Quality).color;

        /// <summary>Better beats get bigger floating text.</summary>
        public static float SizeMultiplier(in BeatResult result) => result.Quality switch
        {
            BeatQuality.Good => 1.1f,
            BeatQuality.Great => 1.25f,
            BeatQuality.Hit => 1.4f,
            BeatQuality.Viral => 1.6f,
            _ => 1f,
        };
    }
}
