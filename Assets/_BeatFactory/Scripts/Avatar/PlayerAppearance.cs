using System;

namespace BeatFactory
{
    public enum AvatarSlot { Hair, Top, Pants, Sneakers, Headwear, Glasses, Chain, Watch, Headphones, Rings, Earrings, TattooArm, TattooNeck, TattooHand, TattooFace }

    /// <summary>The player's look (saved). Indexes point into <see cref="AvatarCatalog"/>; 0 = none for optional slots.</summary>
    [Serializable]
    public class PlayerAppearance
    {
        public int hair;
        public int top;
        public int pants;
        public int sneakers;
        public int headwear;
        public int glasses;
        public int chain;
        public int watch;
        public bool headphones = true;
        public bool rings;
        public bool earrings;
        public bool tattooArm;
        public bool tattooNeck;
        public bool tattooHand = true;
        public bool tattooFace;
        [UnityEngine.Tooltip("While true, the producer's outfit upgrades automatically with every new room (off after manual styling).")]
        public bool autoStyle = true;

        public PlayerAppearance Clone() => (PlayerAppearance)MemberwiseClone();

        /// <summary>
        /// The producer's "glow-up" per room: worn hoodie in the basement → purple hoodie → tee and first chain →
        /// satin bomber → designer pieces → luxury puffer, gold watch, shades and chunky designer sneakers.
        /// Hair is kept (that's the player's choice).
        /// </summary>
        public static PlayerAppearance Preset(int room, int hair)
        {
            var a = new PlayerAppearance { hair = hair, headphones = true, tattooHand = true };
            switch (Math.Min(room, 9))
            {
                case 0: a.top = 0; a.pants = 0; a.sneakers = 0; break;
                case 1: a.top = 1; a.pants = 1; a.sneakers = 1; a.earrings = true; break;
                case 2: a.top = 2; a.pants = 2; a.sneakers = 1; a.chain = 1; a.watch = 1; a.earrings = true; a.tattooArm = true; break;
                case 3: a.top = 3; a.pants = 2; a.sneakers = 1; a.chain = 1; a.watch = 1; a.rings = true; a.earrings = true; a.tattooArm = true; break;
                case 4: a.top = 3; a.pants = 3; a.sneakers = 2; a.chain = 2; a.watch = 1; a.rings = true; a.earrings = true; a.tattooNeck = true; break;
                case 5: a.top = 5; a.pants = 3; a.sneakers = 2; a.chain = 2; a.watch = 2; a.rings = true; a.earrings = true; a.glasses = 2; a.tattooNeck = true; break;
                case 6: a.top = 4; a.pants = 1; a.sneakers = 3; a.chain = 2; a.watch = 2; a.rings = true; a.earrings = true; a.tattooNeck = true; break;
                case 7: a.top = 5; a.pants = 3; a.sneakers = 3; a.chain = 2; a.watch = 2; a.rings = true; a.earrings = true; a.glasses = 2; a.headwear = 1; a.tattooNeck = true; break;
                default: a.top = 5; a.pants = 3; a.sneakers = 3; a.chain = 2; a.watch = 2; a.rings = true; a.earrings = true; a.glasses = 2; a.tattooNeck = true; a.tattooFace = true; break;
            }
            return a;
        }

        public int Get(AvatarSlot slot)
        {
            switch (slot)
            {
                case AvatarSlot.Hair: return hair;
                case AvatarSlot.Top: return top;
                case AvatarSlot.Pants: return pants;
                case AvatarSlot.Sneakers: return sneakers;
                case AvatarSlot.Headwear: return headwear;
                case AvatarSlot.Glasses: return glasses;
                case AvatarSlot.Chain: return chain;
                case AvatarSlot.Watch: return watch;
                case AvatarSlot.Headphones: return headphones ? 1 : 0;
                case AvatarSlot.Rings: return rings ? 1 : 0;
                case AvatarSlot.Earrings: return earrings ? 1 : 0;
                case AvatarSlot.TattooArm: return tattooArm ? 1 : 0;
                case AvatarSlot.TattooNeck: return tattooNeck ? 1 : 0;
                case AvatarSlot.TattooHand: return tattooHand ? 1 : 0;
                default: return tattooFace ? 1 : 0;
            }
        }

        public void Set(AvatarSlot slot, int v)
        {
            switch (slot)
            {
                case AvatarSlot.Hair: hair = v; break;
                case AvatarSlot.Top: top = v; break;
                case AvatarSlot.Pants: pants = v; break;
                case AvatarSlot.Sneakers: sneakers = v; break;
                case AvatarSlot.Headwear: headwear = v; break;
                case AvatarSlot.Glasses: glasses = v; break;
                case AvatarSlot.Chain: chain = v; break;
                case AvatarSlot.Watch: watch = v; break;
                case AvatarSlot.Headphones: headphones = v != 0; break;
                case AvatarSlot.Rings: rings = v != 0; break;
                case AvatarSlot.Earrings: earrings = v != 0; break;
                case AvatarSlot.TattooArm: tattooArm = v != 0; break;
                case AvatarSlot.TattooNeck: tattooNeck = v != 0; break;
                case AvatarSlot.TattooHand: tattooHand = v != 0; break;
                default: tattooFace = v != 0; break;
            }
        }
    }

    /// <summary>One selectable item in the customization menu.</summary>
    public readonly struct AvatarItem
    {
        public readonly AvatarSlot Slot;
        public readonly int Value;
        public readonly string Name;
        /// <summary>Studio level needed to unlock (0 = Basement).</summary>
        public readonly int UnlockTier;

        public AvatarItem(AvatarSlot slot, int value, string name, int unlockTier = 0)
        {
            Slot = slot;
            Value = value;
            Name = name;
            UnlockTier = unlockTier;
        }

        /// <summary>Slots with an "off" state (tapping the equipped item takes it off).</summary>
        public bool Optional => Slot >= AvatarSlot.Headwear;

        /// <summary>Localized item name (style.item.Hair_3 ...).</summary>
        public string DisplayName => Loc.Or("style.item." + AvatarCatalog.Key(Slot, Value), Name);
    }

    /// <summary>
    /// Every customization item, grouped by menu category. Premium items unlock with better studios
    /// (the visual story: basement → bedroom → professional → modern).
    /// </summary>
    public static class AvatarCatalog
    {
        public static readonly string[] Categories = { "HAIR", "CLOTHES", "ACCESSORIES", "TATTOOS", "SNEAKERS" };

        public static readonly string[] HairNames = { "Half-Up Dreads", "Tied Dreads", "Box Braids", "Curly Fade", "Long & Dyed", "Messy Short", "Lilac Bob" };
        public static readonly int[] HairTiers = { 0, 0, 0, 0, 1, 0, 1 };
        public static readonly string[] TopNames = { "Worn Hoodie", "Purple Hoodie", "Graphic Tee", "Satin Bomber", "Tracksuit", "Luxury Puffer" };
        public static readonly int[] TopTiers = { 0, 1, 0, 2, 2, 3 };
        public static readonly string[] PantsNames = { "Rinse Denim", "Joggers", "Cargos", "Wide Trousers" };
        public static readonly int[] PantsTiers = { 0, 0, 1, 3 };
        public static readonly string[] SneakerNames = { "Retro Low", "Court High", "Minimal Lux", "Chunky Future" };
        public static readonly int[] SneakerTiers = { 0, 1, 2, 3 };

        public static AvatarItem[] Items(int category)
        {
            var list = new System.Collections.Generic.List<AvatarItem>();
            switch (category)
            {
                case 0:
                    for (int i = 0; i < HairNames.Length; i++) list.Add(new AvatarItem(AvatarSlot.Hair, i, HairNames[i], HairTiers[i]));
                    break;
                case 1:
                    for (int i = 0; i < TopNames.Length; i++) list.Add(new AvatarItem(AvatarSlot.Top, i, TopNames[i], TopTiers[i]));
                    for (int i = 0; i < PantsNames.Length; i++) list.Add(new AvatarItem(AvatarSlot.Pants, i, PantsNames[i], PantsTiers[i]));
                    break;
                case 2:
                    list.Add(new AvatarItem(AvatarSlot.Headphones, 1, "Studio Headphones"));
                    list.Add(new AvatarItem(AvatarSlot.Headwear, 1, "Backwards Cap"));
                    list.Add(new AvatarItem(AvatarSlot.Headwear, 2, "Knit Beanie"));
                    list.Add(new AvatarItem(AvatarSlot.Glasses, 1, "Round Frames"));
                    list.Add(new AvatarItem(AvatarSlot.Glasses, 2, "Shades", 1));
                    list.Add(new AvatarItem(AvatarSlot.Chain, 1, "Thin Chain", 1));
                    list.Add(new AvatarItem(AvatarSlot.Chain, 2, "Cuban Link", 2));
                    list.Add(new AvatarItem(AvatarSlot.Watch, 1, "Smartwatch", 1));
                    list.Add(new AvatarItem(AvatarSlot.Watch, 2, "Gold Watch", 3));
                    list.Add(new AvatarItem(AvatarSlot.Rings, 1, "Rings", 1));
                    list.Add(new AvatarItem(AvatarSlot.Earrings, 1, "Studs"));
                    break;
                case 3:
                    list.Add(new AvatarItem(AvatarSlot.TattooHand, 1, "Hand Note"));
                    list.Add(new AvatarItem(AvatarSlot.TattooArm, 1, "Arm Sleeve"));
                    list.Add(new AvatarItem(AvatarSlot.TattooNeck, 1, "Neck Rose"));
                    list.Add(new AvatarItem(AvatarSlot.TattooFace, 1, "Face Dots"));
                    break;
                default:
                    for (int i = 0; i < SneakerNames.Length; i++) list.Add(new AvatarItem(AvatarSlot.Sneakers, i, SneakerNames[i], SneakerTiers[i]));
                    break;
            }
            return list.ToArray();
        }

        /// <summary>Stable key used for thumbnails ("Hair_3", "Glasses_2" ...).</summary>
        public static string Key(AvatarSlot slot, int value) => slot + "_" + value;
    }

    /// <summary>Player appearance + unlocks (best studio reached, kept after prestige).</summary>
    public class AppearanceManager
    {
        public PlayerAppearance Current { get; private set; } = new PlayerAppearance();
        public int BestTier { get; private set; }

        public event Action Changed;

        public bool IsUnlocked(AvatarItem item) => BestTier >= item.UnlockTier;
        public bool IsEquipped(AvatarItem item) => Current.Get(item.Slot) == item.Value;

        /// <summary>Equips the item (or takes an optional item off when it is already equipped).</summary>
        public bool Toggle(AvatarItem item)
        {
            if (!IsUnlocked(item)) return false;
            if (IsEquipped(item))
            {
                if (!item.Optional) return false;
                Current.Set(item.Slot, 0);
            }
            else Current.Set(item.Slot, item.Value);
            if (item.Slot != AvatarSlot.Hair) Current.autoStyle = false; // the player styles manually from now on
            Changed?.Invoke();
            return true;
        }

        /// <summary>Turns the automatic room-based glow-up back on and applies it.</summary>
        public void ResetToAutoStyle(int room)
        {
            Current = PlayerAppearance.Preset(room, Current.hair);
            Changed?.Invoke();
        }

        public void ReachTier(int tier)
        {
            bool upgrade = tier > BestTier;
            if (upgrade) BestTier = tier;
            // the producer dresses better in every new room (unless the player styles manually)
            if (Current.autoStyle && tier >= 0)
            {
                var preset = PlayerAppearance.Preset(tier, Current.hair);
                if (preset.top != Current.top || preset.sneakers != Current.sneakers || preset.chain != Current.chain || upgrade)
                {
                    Current = preset;
                    upgrade = true;
                }
            }
            if (upgrade) Changed?.Invoke();
        }

        public void LoadState(PlayerAppearance appearance, int bestTier)
        {
            Current = appearance != null ? appearance.Clone() : new PlayerAppearance();
            BestTier = Math.Max(0, bestTier);
            Changed?.Invoke();
        }
    }
}
