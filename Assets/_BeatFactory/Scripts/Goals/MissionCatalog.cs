using System.Collections.Generic;

namespace BeatFactory
{
    public enum MissionCategory { Production, Artists, Releases, Concerts, Studio, Career }

    /// <summary>One career goal: a gameplay stat that must reach a target, and its reward.</summary>
    public class MissionDefinition
    {
        public string id;
        public MissionCategory category;
        public string title;
        public string description;
        public StatId stat;

        public string Title => Loc.Or("mission." + id + ".title", title);
        public string Description => Loc.Or("mission." + id + ".desc", description);
        public double target;
        public GoalReward reward;
        /// <summary>Formats the progress numbers ("742K / 1M", "$10K", studio names …).</summary>
        public System.Func<double, string> format;
    }

    /// <summary>
    /// The mission list (data only — no gameplay script knows about missions). Goals follow normal play:
    /// each category goes from a first step to a long-term milestone; rewards grow with difficulty. Early rewards are small (30–180 s of income) so the first minutes are not skipped.
    /// Chart missions (Top 100 / Top 10 / #1) are not included: the game has no chart system yet.
    /// </summary>
    public static class MissionCatalog
    {
        static string N(double v) => NumberFormatter.Format(v);
        static string Money(double v) => NumberFormatter.FormatMoney(v);
        static readonly string[] StudioNames = { "Basement", "Bedroom Studio", "Small Pro Studio", "Professional Studio", "Modern Studio", "Luxury Studio", "Major Studio", "Elite Studio", "Record Label Studio", "Mega Music Complex" };

        static MissionDefinition M(string id, MissionCategory c, string title, string desc, StatId stat, double target, GoalReward reward, System.Func<double, string> fmt = null) =>
            new MissionDefinition { id = id, category = c, title = title, description = desc, stat = stat, target = target, reward = reward, format = fmt ?? N };

        public static readonly MissionDefinition[] All = Build();

        static MissionDefinition[] Build()
        {
            var P = MissionCategory.Production; var A = MissionCategory.Artists; var R = MissionCategory.Releases;
            var C = MissionCategory.Concerts; var S = MissionCategory.Studio; var K = MissionCategory.Career;
            System.Func<double, string> studio = v => v <= 0 ? "—" : StudioName((int)v);
            var list = new List<MissionDefinition>
            {
                // PRODUCTION
                M("prod_beats_10", P, "FIRST TRACKS", "Make 10 beats", StatId.BeatsMade, 10, GoalReward.Money(30)),
                M("prod_beats_50", P, "WARMING UP", "Make 50 beats", StatId.BeatsMade, 50, GoalReward.Money(60)),
                M("prod_beats_100", P, "BEAT MACHINE", "Make 100 beats", StatId.BeatsMade, 100, GoalReward.Money(90)),
                M("prod_beat_money_10k", P, "PAID IN BEATS", "Earn $10,000 from beats", StatId.BeatEarnings, 1e4, GoalReward.Money(60), Money),
                M("prod_upgrades_5", P, "LEVEL UP THE TEAM", "Upgrade producers 5 times", StatId.ProducerUpgrades, 5, GoalReward.Money(60)),
                M("prod_viral_1", P, "GONE VIRAL", "Produce a VIRAL beat", StatId.ViralBeats, 1, GoalReward.Boost(BoostType.Viral, 180)),
                M("prod_beats_1k", P, "THOUSAND HITS", "Make 1,000 beats", StatId.BeatsMade, 1e3, GoalReward.Money(180)),
                M("prod_beat_money_1m", P, "MILLION-DOLLAR LOOPS", "Earn $1M from beats", StatId.BeatEarnings, 1e6, GoalReward.Money(300), Money),
                M("prod_upgrades_100", P, "DREAM TEAM", "Upgrade producers 100 times", StatId.ProducerUpgrades, 100, GoalReward.Money(600)),
                M("prod_viral_25", P, "VIRAL MACHINE", "Produce 25 VIRAL beats", StatId.ViralBeats, 25, GoalReward.Money(900)),
                M("prod_beats_10k", P, "FACTORY MODE", "Make 10,000 beats", StatId.BeatsMade, 1e4, GoalReward.Boost(BoostType.Money, 600)),

                // ARTISTS
                M("art_sign_1", A, "THE FIRST SIGNING", "Sign your first artist", StatId.ArtistsSignedLifetime, 1, GoalReward.Fans(10)),
                M("art_sign_3", A, "BUILDING A ROSTER", "Sign 3 artists", StatId.ArtistsSignedLifetime, 3, GoalReward.Money(300)),
                M("art_genres_3", A, "GENRE HOPPER", "Have artists from 3 different genres", StatId.ArtistGenres, 3, GoalReward.Boost(BoostType.Producer, 600)),
                M("art_pop_100", A, "THE BUZZ", "Reach 100% total popularity across your artists", StatId.TotalPopularity, 100, GoalReward.Fans(15), v => NumberFormatter.Percent(v)),
                M("art_fans_100k", A, "100K CLUB", "Help an artist reach 100K fans", StatId.MaxArtistFans, 1e5, GoalReward.Money(600)),
                M("art_success_3", A, "HIT FACTORY", "Have 3 established artists (40%+ popularity)", StatId.SuccessfulArtists, 3, GoalReward.Money(900)),
                M("art_fans_1m", A, "MILLION FANS", "Help an artist reach 1M fans", StatId.MaxArtistFans, 1e6, GoalReward.Fame(1)),
                M("art_level_max", A, "STAGE LEGEND", "Max out an artist's concert level", StatId.MaxConcertLevel, 10, GoalReward.Fame(3), v => Loc.F("common.level_short", "level", v.ToString("0"))),

                // RELEASES
                M("rel_album_1", R, "DEBUT ALBUM", "Release your first album", StatId.AlbumsReleased, 1, GoalReward.Money(300)),
                M("rel_streams_1m", R, "1M STREAMS", "Reach 1M total streams", StatId.Streams, 1e6, GoalReward.Money(300)),
                M("rel_album_5", R, "DISCOGRAPHY", "Release 5 albums", StatId.AlbumsReleased, 5, GoalReward.Fame(1)),
                M("rel_streams_10m", R, "10M STREAMS", "Reach 10M total streams", StatId.Streams, 1e7, GoalReward.Money(900)),
                M("rel_streams_1b", R, "BILLION STREAMS", "Reach 1B total streams", StatId.Streams, 1e9, GoalReward.Fame(2)),

                // CONCERTS
                M("con_first", C, "FIRST SHOW", "Host your first concert", StatId.Concerts, 1, GoalReward.Fans(10)),
                M("con_soldout_club", C, "PACKED CLUB", "Sell out an Underground Club", StatId.SoldOutClubs, 1, GoalReward.Money(300)),
                M("con_attendees_10k", C, "10K CROWD", "Reach 10,000 total concert attendees", StatId.ConcertAttendees, 1e4, GoalReward.Money(600)),
                M("con_soldout_hall", C, "HALL SOLD OUT", "Sell out a Concert Hall", StatId.SoldOutHalls, 1, GoalReward.Money(900)),
                M("con_tour", C, "ON THE ROAD", "Complete a tour", StatId.ToursCompleted, 1, GoalReward.Money(900)),
                M("con_fans_100k", C, "WON ON STAGE", "Gain 100,000 fans from concerts", StatId.ConcertFans, 1e5, GoalReward.Fans(20)),
                M("con_arena", C, "ARENA NIGHT", "Host an Arena concert", StatId.ArenaShows, 1, GoalReward.Fame(1)),

                // STUDIO
                M("stu_bedroom", S, "MOVING UP", "Unlock the Bedroom Studio", StatId.BestStudio, 1, GoalReward.Money(120), studio),
                M("stu_pro", S, "GOING PRO", "Unlock the Professional Studio", StatId.BestStudio, 3, GoalReward.Boost(BoostType.Speed, 600), studio),
                M("stu_modern", S, "MODERN SOUND", "Unlock the Modern Studio", StatId.BestStudio, 4, GoalReward.Money(900), studio),
                M("stu_luxury", S, "LUXURY LIFE", "Unlock the Luxury Studio", StatId.BestStudio, 5, GoalReward.Fame(1), studio),
                M("stu_max", S, "THE EMPIRE", "Reach the Mega Music Complex", StatId.BestStudio, 9, GoalReward.Fame(5), studio),

                // CAREER
                M("car_1m", K, "FIRST MILLION", "Earn $1M", StatId.MoneyEarned, 1e6, GoalReward.Money(300), Money),
                M("car_1b", K, "BILLIONAIRE PRODUCER", "Earn $1B", StatId.MoneyEarned, 1e9, GoalReward.Money(900), Money),
                M("car_artists_5", K, "LABEL BOSS", "Have 5 established artists (40%+ popularity)", StatId.SuccessfulArtists, 5, GoalReward.Fame(2)),
                M("car_1t", K, "TRILLION-DOLLAR SOUND", "Earn $1T", StatId.MoneyEarned, 1e12, GoalReward.Fame(2), Money),
                M("car_stadium", K, "TOP PRODUCER", "Put one of your artists on a stadium stage", StatId.StadiumShows, 1, GoalReward.Fame(3)),
            };
            return list.ToArray();
        }

        public static string CategoryName(MissionCategory c) => Loc.T("mission.category." + c.ToString().ToLowerInvariant());

        static string StudioName(int level)
        {
            var s = GameManager.Instance != null ? GameManager.Instance.Studios.Get(level) : null;
            return s != null ? s.DisplayName : StudioNames[System.Math.Min(StudioNames.Length - 1, level)];
        }
    }
}
