# Beat Factory — Economy Balance v3 (10 rooms)

## Rooms (studio levels)
| # | Room | Price | Income | Speed | Offline | HIT/VIRAL | Unlocks |
|---|---|---|---|---|---|---|---|
| 1 | Basement | free | ×1 | ×1 | 2h | ×1 | Beginner, Melody, Drum producers · 5 artists · Hip-Hop · Basement Tapes |
| 2 | Bedroom Studio | $5M | ×2 | ×1.1 | 3h | ×1.1 | Sample Producer · 5 artists · Trap · Night Shift |
| 3 | Small Pro Studio | $25M | ×2.5 | ×1.2 | 4h | ×1.2 | Sound Designer · 3 artists · Rage · Neon Riot |
| 4 | Professional Studio | $150M | ×3 | ×1.3 | 5h | ×1.35 | Hitmaker · 4 artists |
| 5 | Modern Studio | $1B | ×4 | ×1.4 | 6h | ×1.5 | Superstar Producer · 3 artists · Hyperpop · Cold Block |
| 6 | Luxury Studio | $7B | ×5 | ×1.5 | 8h | ×1.75 | 2 artists |
| 7 | Major Studio | $50B | ×6 | ×1.6 | 9h | ×2 | 2 artists · Electronic · Velvet Hours |
| 8 | Elite Studio | $350B | ×7.5 | ×1.75 | 10h | ×2.3 | 3 artists · Experimental |
| 9 | Record Label Studio | $2.5T | ×9 | ×1.9 | 12h | ×2.6 | 3 artists · Signal // Noise |
| 10 | Mega Music Complex | $20T | ×12 | ×2 | 14h | ×3 | 2 legend artists · Future |

Why not ×100 for the last room? The income bonuses multiply with each other: genres, 32 artists, albums, fame and the producer tiers. With the suggested ×2…×100 rooms, the simulator finished all 10 rooms in about 1 hour. With these values each room still gives a big jump, and prices still grow faster than income (rooms cost ×5–8 more each step).

## Money scale
Money values are ×10 from v2.
- Manual beat: $10 + 4% of producer income/s.
- Producers: hire costs $1K / $15K / $200K / $4M / $80M / $2B / $50B. Each one earns about ×4–5 more than the previous one. Upgrades cost ×1.5 per level, and every 10 levels the producer earns ×2.
- Genres: ×1.3 → ×3.
- Artists: ×1.03 → ×1.06 each, about ×4 in total. Their prices are a share of their room's price.
- Albums: reputation up to ×2.8 in total.
- Streams platforms: $250K / $20M / $2B.
- Prestige: available after $10B earned in a run; fame = √(earnings / $100M).

## Simulated players (Tools > Beat Factory > Simulate Economy)
Times are hours:minutes until the room is bought.
- **Active** (plays 3 h, then 20 min every hour; some ads): L2 0:17 · L3 0:27 · L4 0:37 · L5 0:58 · L6 1:40 · L7 2:29 · L8 5:09 · L9 10:13 · L10 18:00
- **Optimized** (long sessions, boosts almost always on): L2 0:09 · L5 0:33 · L7 1:25 · L9 4:10 · L10 8:08
- **Casual** (10 minutes every 3 hours): L2 3:00 · L5 6:00 · L7 12:00 · L10 ~36:00
- **Offline** (5 minutes every 8 hours): L2 16:00 · L5 32:00 · L10 ~80:00

## Concerts & tours (code: `Scripts/Concerts`)
Career loop: sign → music (viral beats, albums, studio upgrades, social buzz) → popularity + fans → concerts → bigger venues → tours.

- **Popularity limit from the studio:** 30% + 7.5% per room (L1 30% … L4 52.5% … L6 67.5% … L9 90%). Halls need studio L3, arenas L6 (7B), stadiums L9 (2.5T), which ties artists to the room progression.
- **Venues:**
  - Club: 300 seats, $15–25 tickets, no requirements, 2-minute rest after a show.
  - Small Venue: 1,000 seats, $25–35, needs 20% popularity and 25K fans, 5 min.
  - Concert Hall: 3,000 seats, $35–50, needs 40% and 150K fans, 10 min.
  - Arena: 15,000 seats, $50–80, needs 65% and 1M fans, 20 min.
  - Stadium: 60,000 seats, $70–120, needs 90% and 6M fans, 40 min.
- **Attendance:** fans × reach(popularity: 2.5% → 6%) ÷ capacity, then 1 − e^(−0.9·demand). Bonuses: +2% per concert level, +6% for a new album, +5% after a viral beat in the last 10 minutes, plus luck (−10%…+8%) and show quality. Fills at the venue's minimum requirements: club ~35%, small ~50%, hall ~80%, arena ~95%, stadium usually sells out.
- **Revenue:** ticket sales (real numbers) plus merch & streams. Merch & streams = producer income/s without ad boosts × 10 / 25 / 60 / 150 / 360 s × fill × show quality, and it scales the reward into the late game.
- **Booking cost:** 35% of the expected revenue, so a show nets about 65%. One artist playing back to back adds roughly 5–12% income. That is powerful with a full roster but never replaces producers.
- **Events:**
  - SOLD OUT: +20% revenue.
  - VIRAL MOMENT: 5% chance + 1% per concert level. +30% revenue, +15% fans, +3% popularity.
  - CROWD GOES CRAZY: +5% fans, +1% popularity.
  - ENCORE: +10% revenue, +20% XP.
  - LOW ENERGY (below 60% full): −10% revenue.
- **Concert XP levels:** 0 / 300 / 800 / 1.6K / 3K / 5K / 8K / 12K / 18K / 26K. Each level adds +2% attendance, +4% ticket price, +5% revenue and +1% viral chance.
- **Tours** (rewards are fixed at the start, paid on COLLECT; 90% of a show's revenue per show; cost 30% of the revenue):
  - Mini: 3 small-venue shows over 20 min, needs concert level 2.
  - Regional: 5 halls over 1 h, needs level 3.
  - National: 8 arenas over 3 h, needs level 5.
  - World: 12 stadiums over 8 h, needs level 7.
  - Mega: 20 stadiums ×1.5 over 12 h, needs level 9, 95% popularity and the Mega Complex.
  - A NEW ALBUM (the featured artist of the latest album) gives the next tour +25% revenue and +40% fans.
- **Offline:** tours run on real time. Catalogue streams keep growing fans and popularity at 50% speed for the offline-hours window.

## Tools
- `Tools > Beat Factory > Apply Default Economy Balance`: resets every price and multiplier to these values.
- Room data lives in `Data/Studios`, including the tagline, the offline hours, the HIT/VIRAL bonus and the extra "unlocks" text. The upgrade screen builds its unlock list automatically from the producers, artists, genres and albums that require the room.
