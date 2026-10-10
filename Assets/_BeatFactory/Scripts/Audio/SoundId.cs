namespace BeatFactory
{
    /// <summary>Every sound event in the game. Clips (with variations) are assigned in the AudioConfig asset.</summary>
    public enum SoundId
    {
        Click = 0,            // normal UI button
        Beat = 1,             // MAKE BEAT (kick + 808/pluck/stab/keys variations)
        BeatGood = 2,         // GOOD/GREAT beat
        Upgrade = 3,          // producer level up
        Hire = 4,             // generic hire (fallback when a producer has no identity sound)
        Unlock = 5,           // genre / artist / platform unlocked
        Hit = 6,
        Viral = 7,
        Prestige = 8,
        CashOut = 9,          // streams → money
        Boost = 10,
        Collect = 11,         // offline earnings collected
        ClickStrong = 12,     // important buttons (confirm, x2 ad ...)
        Tab = 13,             // bottom navigation
        ProducerBeginner = 14,
        ProducerMelody = 15,
        ProducerDrums = 16,
        Money = 17,           // generic money reward
        StudioTransition = 18,// studio upgrade (riser → impact)
    }

    /// <summary>Mix groups with their own volume (AudioConfig) — the player controls Master/Music/SFX.</summary>
    public enum SoundCategory
    {
        SFX = 0,        // events: unlocks, viral, prestige, money
        UI = 1,         // clicks, tabs
        Production = 2, // beats, producers, upgrades
    }
}
