using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeatFactory
{
    public enum ChemistryLevel { Low = 0, Normal = 1, Good = 2, Great = 3, Perfect = 4 }

    /// <summary>Bonuses an artist gets from their chemistry with their studio partner.</summary>
    public struct ChemistryModifier
    {
        public float performance;   // concert show quality (+0.03 = +3%)
        public float fanGrowth;     // fans from concerts / buzz
        public float streams;       // stream generation (contributes to the global bonus)
        public float viralChance;   // HIT/VIRAL chance (contributes to the global bonus)

        public static ChemistryModifier For(ChemistryLevel l)
        {
            switch (l)
            {
                case ChemistryLevel.Normal: return new ChemistryModifier { performance = 0.01f, fanGrowth = 0.02f, streams = 0.01f };
                case ChemistryLevel.Good: return new ChemistryModifier { performance = 0.04f, fanGrowth = 0.04f, streams = 0.02f, viralChance = 0.03f };
                case ChemistryLevel.Great: return new ChemistryModifier { performance = 0.06f, fanGrowth = 0.07f, streams = 0.04f, viralChance = 0.05f };
                case ChemistryLevel.Perfect: return new ChemistryModifier { performance = 0.08f, fanGrowth = 0.12f, streams = 0.06f, viralChance = 0.08f };
                default: return default;
            }
        }
    }

    [Serializable]
    public class ChemistryPair
    {
        public string artistId;
        public string producerId;
        public float chemistry;   // 0..100 %
    }

    [Serializable]
    public class ChemistryPartner
    {
        public string artistId;
        public string producerId;
    }

    [Serializable]
    public class ChemistrySave
    {
        public List<ChemistryPair> pairs = new List<ChemistryPair>();
        public List<ChemistryPartner> partners = new List<ChemistryPartner>();
    }

    /// <summary>
    /// Artist Chemistry: every signed artist works with one studio partner (one of your hired producers; picked
    /// automatically, changeable any time — no restrictions). Chemistry (0–100%) grows while they work together:
    /// the partner's beats, viral hits, albums, concerts and tours. Styles matter: the artist's genre vs. the
    /// producer's sound sets the starting chemistry and how fast it grows (rage + drums/sound design = strong,
    /// R&amp;B + melody = strong, incompatible styles = slow). Higher chemistry gives small bonuses
    /// (show quality, fan growth, streams, viral chance) — an optimization, never a requirement.
    /// Progress is kept per artist–producer pair, so switching partners never erases a relationship.
    /// </summary>
    public class ArtistChemistryManager
    {
        readonly GameManager game;
        readonly Dictionary<string, ChemistryPair> pairs = new Dictionary<string, ChemistryPair>();
        readonly Dictionary<string, string> partners = new Dictionary<string, string>();

        /// <summary>Artist, partner producer, new level.</summary>
        public event Action<ArtistData, ProducerData, ChemistryLevel> LevelUp;
        /// <summary>Artist, gain in percent points (small feedback).</summary>
        public event Action<ArtistData, float> Gained;
        public event Action Changed;

        public ArtistChemistryManager(GameManager game)
        {
            this.game = game;
        }

        // ------------------------------------------------------------------ style compatibility

        /// <summary>How well a producer's sound fits a genre: 0.6 (clash) … 1.7 (perfect fit).</summary>
        public static float Affinity(string producerId, ArtistGenre g)
        {
            switch (producerId)
            {
                case "beginner": return g == ArtistGenre.HipHop || g == ArtistGenre.Underground ? 1.2f : 0.9f;
                case "melody": return g == ArtistGenre.Melodic || g == ArtistGenre.RnB ? 1.6f : g == ArtistGenre.Trap ? 1.1f : g == ArtistGenre.Rage ? 0.6f : g == ArtistGenre.Drill ? 0.7f : 1f;
                case "drum": return g == ArtistGenre.Trap || g == ArtistGenre.Drill ? 1.5f : g == ArtistGenre.Rage ? 1.4f : g == ArtistGenre.RnB ? 0.7f : g == ArtistGenre.Melodic ? 0.8f : 1f;
                case "sample": return g == ArtistGenre.Underground ? 1.6f : g == ArtistGenre.HipHop ? 1.5f : g == ArtistGenre.RnB ? 1.1f : g == ArtistGenre.Rage ? 0.7f : g == ArtistGenre.Experimental ? 0.9f : 1f;
                case "sound": return g == ArtistGenre.Experimental ? 1.7f : g == ArtistGenre.Rage ? 1.4f : g == ArtistGenre.RnB ? 0.7f : 1f;
                case "hitmaker": return g == ArtistGenre.Trap || g == ArtistGenre.Melodic || g == ArtistGenre.HipHop ? 1.3f : g == ArtistGenre.RnB ? 1.2f : 1f;
                case "superstar": return 1.3f;
                default: return 1f;
            }
        }

        public static string AffinityLabel(float a) => Loc.T(a >= 1.4f ? "chemistry.match.strong" : a >= 1.0f ? "chemistry.match.good" : a >= 0.8f ? "chemistry.match.average" : "chemistry.match.clash");

        public static ChemistryLevel LevelOf(float pct) =>
            pct >= 100f ? ChemistryLevel.Perfect : pct >= 70f ? ChemistryLevel.Great : pct >= 40f ? ChemistryLevel.Good : pct >= 20f ? ChemistryLevel.Normal : ChemistryLevel.Low;

        public static string LevelName(ChemistryLevel l) => Loc.T("chemistry.level." + l.ToString().ToLowerInvariant());

        /// <summary>Producers ranked by fit for this artist (shown as "preferred styles").</summary>
        public List<ProducerData> Preferred(ArtistData a, int count = 2)
        {
            var list = new List<ProducerData>();
            foreach (var p in game.Producers.Producers) list.Add(p.Data);
            list.Sort((x, y) => Affinity(y.id, a.genre).CompareTo(Affinity(x.id, a.genre)));
            return list.GetRange(0, Math.Min(count, list.Count));
        }

        // ------------------------------------------------------------------ pairs & partners

        static string Key(string artist, string producer) => artist + "|" + producer;

        public ChemistryPair Pair(ArtistData a, ProducerData p)
        {
            string k = Key(a.id, p.id);
            if (!pairs.TryGetValue(k, out var pair))
            {
                // first session together: compatible styles start warmer
                pair = new ChemistryPair { artistId = a.id, producerId = p.id, chemistry = Mathf.Clamp(10f + 15f * (Affinity(p.id, a.genre) - 0.6f), 5f, 30f) };
                pairs[k] = pair;
            }
            return pair;
        }

        /// <summary>The artist's studio partner (auto-picks the best-fitting hired producer the first time).</summary>
        public ProducerData Partner(ArtistData a)
        {
            if (partners.TryGetValue(a.id, out var id))
                foreach (var p in game.Producers.Producers) if (p.Data.id == id && p.IsHired) return p.Data;
            ProducerData best = null;
            float bestAff = -1;
            foreach (var p in game.Producers.Producers)
            {
                if (!p.IsHired) continue;
                float aff = Affinity(p.Data.id, a.genre);
                if (aff > bestAff) { bestAff = aff; best = p.Data; }
            }
            if (best != null) partners[a.id] = best.id;
            return best;
        }

        public void SetPartner(ArtistData a, ProducerData p)
        {
            if (a == null || p == null) return;
            partners[a.id] = p.id;
            Pair(a, p);
            Changed?.Invoke();
        }

        /// <summary>Next hired producer (for the CHANGE PARTNER button).</summary>
        public ProducerData NextPartner(ArtistData a)
        {
            var hired = new List<ProducerData>();
            foreach (var p in game.Producers.Producers) if (p.IsHired) hired.Add(p.Data);
            if (hired.Count == 0) return null;
            var cur = Partner(a);
            int i = cur == null ? -1 : hired.IndexOf(cur);
            return hired[(i + 1) % hired.Count];
        }

        public float Chemistry(ArtistData a)
        {
            var p = Partner(a);
            return p == null ? 0f : Pair(a, p).chemistry;
        }

        public ChemistryLevel Level(ArtistData a) => LevelOf(Chemistry(a));
        public ChemistryModifier Modifier(ArtistData a) => game.Artists.IsSigned(a) ? ChemistryModifier.For(Level(a)) : default;

        // ------------------------------------------------------------------ growth

        void Add(ArtistData a, float points)
        {
            var p = Partner(a);
            if (p == null || points <= 0) return;
            var pair = Pair(a, p);
            if (pair.chemistry >= 100f) return;
            var before = LevelOf(pair.chemistry);
            points *= Affinity(p.id, a.genre); // good style fit → faster
            pair.chemistry = Mathf.Min(100f, pair.chemistry + points);
            var after = LevelOf(pair.chemistry);
            Gained?.Invoke(a, points);
            if (after > before) LevelUp?.Invoke(a, p, after);
            Changed?.Invoke();
        }

        /// <summary>The partner made beats: ~0.4% per minute of their production at a normal fit.</summary>
        public void OnProduced(Producer producer, BeatResult r)
        {
            if (r.Source == BeatSource.Offline) return;
            foreach (var a in game.Artists.Artists)
            {
                if (!game.Artists.IsSigned(a)) continue;
                var p = Partner(a);
                if (p == null || p.id != producer.Data.id) continue;
                float pts = 0.4f * producer.Data.cycleSeconds / 60f * Mathf.Max(1, r.Count);
                if (r.Quality == BeatQuality.Viral) pts += 2f;
                else if (r.Quality == BeatQuality.Hit) pts += 0.5f;
                Add(a, pts);
            }
        }

        /// <summary>An album is a big shared project: everyone on the roster bonds with their partner.</summary>
        public void OnAlbum()
        {
            foreach (var a in game.Concerts.SignedArtists()) Add(a, 5f);
        }

        public void OnConcert(ConcertResult r)
        {
            if (r?.artist == null) return;
            Add(r.artist, 1f + 3f * r.fill + (r.soldOut ? 2f : 0f));
        }

        public void OnTour(ActiveTour t)
        {
            var a = game.Tours.ArtistOf(t);
            if (a != null) Add(a, t.shows);
        }

        // ------------------------------------------------------------------ global effects

        /// <summary>Stream generation bonus from the whole roster (×, capped at +15%).</summary>
        public double StreamMultiplier
        {
            get
            {
                double sum = 0;
                foreach (var a in game.Concerts.SignedArtists()) sum += Modifier(a).streams;
                return 1.0 + Math.Min(0.15, sum * 0.5);
            }
        }

        /// <summary>HIT/VIRAL chance bonus from the whole roster (×, capped at +12%).</summary>
        public float ViralChanceMultiplier
        {
            get
            {
                float sum = 0;
                foreach (var a in game.Concerts.SignedArtists()) sum += Modifier(a).viralChance;
                return 1f + Mathf.Min(0.12f, sum * 0.5f);
            }
        }

        // ------------------------------------------------------------------ save

        public ChemistrySave GetSaveData()
        {
            var s = new ChemistrySave();
            s.pairs.AddRange(pairs.Values);
            foreach (var kv in partners) s.partners.Add(new ChemistryPartner { artistId = kv.Key, producerId = kv.Value });
            return s;
        }

        public void LoadState(ChemistrySave s)
        {
            pairs.Clear();
            partners.Clear();
            if (s?.pairs != null)
                foreach (var p in s.pairs)
                    if (p != null && !string.IsNullOrEmpty(p.artistId)) pairs[Key(p.artistId, p.producerId)] = p;
            if (s?.partners != null)
                foreach (var p in s.partners)
                    if (p != null && !string.IsNullOrEmpty(p.artistId)) partners[p.artistId] = p.producerId;
            Changed?.Invoke();
        }
    }
}
