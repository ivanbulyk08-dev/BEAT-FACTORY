# Beat Factory — Economy Rebalance v4 (report, 11.10.2026)

Prompt 56284. Every number below comes from the economy simulation (`Tools > Beat Factory > Simulate Economy`,
code `Scripts/Editor/EconomySimulator.cs`), which reads the real data assets and uses the game's formulas.
"Play" = time spent in the game; calendar time is in brackets.

## 1. Why money came so fast

1. **Producers paid back in ~30 seconds.** The Beginner Producer cost $1,000 and earned $15/s base (≈$31/s with beat quality).
   Upgrades cost ×1.5 per level, but income doubles every 10 levels, so long upgrade chains stayed cheap.
2. **Rewards measured in "seconds of income" snowballed.** The first album (25 beats, 1 producer) and four early missions
   (50 beats, 5 upgrades, $10K from beats, 1,000 beats) all paid 2–10 minutes of income, unlocked within the same minute,
   and each payout bought upgrades that raised the next payout. In the simulation this gave $100K after 15 seconds.
3. **Each tap was worth $20.** $10 base × the average beat quality (×2.06): $60/s at 3 taps/s, so the first $1K came after 13 s.
4. **Rewarded-ad boosts stacked without a limit.** 2X MONEY × 2X SPEED × 3X PRODUCER = ×12 on producer income, each ad
   adds 5–10 more minutes (up to 60), and boost ads have no cooldown. That makes $1M in about 9 seconds possible.
5. **VIRAL beats chained 2X MONEY boosts.** Every VIRAL beat (manual taps included) added 2 minutes of 2X MONEY, stacking.
   In busy studios this kept 2X MONEY on almost permanently.
6. **Late producers were too cheap for their income** (hire cost only ×13–25 per tier), so the 10 studios were done in about
   2.5 hours of play.

No double payments, overflow or duplicate-claim bugs were found. Rewarded callbacks are already guarded, the offline
period can be claimed only once, and the device-clock guard pays a moved clock only once.

## 2. Changed formulas

- Manual beat: `(0.8 + 0.03 × producer base income/s) × multipliers × beat quality` (was 10 + 4%).
- Producer upgrade cost: `upgradeBaseCost × costGrowth^(level − 1)`, with `costGrowth` now set **per producer** (1.6 → 2.0).
  Before, every producer used the global 1.5. Producer income per beat stays `baseIncome × level × 2^(level/10)`.
- Boosts on producer income: `min(money × speed × producer, 4)`. New `BoostConfig.maxCombinedProducerBoost = 4`
  (a "BoostCap" multiplier in the producer-income stack). Offline earnings and mission rewards divide out the capped
  factor, so they stay consistent.
- VIRAL-beat 2X MONEY: at most once per `BoostConfig.viralBeatBoostCooldownMinutes = 10` minutes (2 minutes long, as before).
- Money below $100 is shown with one decimal, rounded down ("$0.8", "$12.5"). Before, it was cut to whole dollars, so a $0.8 beat would have shown as "$0".

## 3. Starting values, old → new

| Value | Old | New |
|---|---|---|
| Starting money | $0 | $0 |
| MAKE BEAT base | $10 | $0.80 |
| Tap share of producer income | 4% | 3% |
| Hip-Hop genre (first upgrade) | $50,000 | $250 |
| Beginner Producer hire | $1,000 | $1,500 |
| Mission "Make 10 / 50 / 100 beats" | 60 / 120 / 180 s of income | 30 / 60 / 90 s |
| Missions "Earn $10K from beats" / "5 upgrades" | 120 / 120 s | 60 / 60 s |
| Mission "Make 1,000 beats" | 600 s | 180 s |
| Combined boost cap | none (×12) | ×4 |
| VIRAL 2X MONEY cooldown | none | 10 min |

Unchanged: studio prices and bonuses, artists, albums, stream platforms, concerts and tours, offline rules (50%, 2–14 h by studio),
prestige, daily challenges, the instant VIRAL cooldown (15 min) and the free boost (every 20 min).

## 4. Producers

| Producer | Studio | Hire old → new | 1st upgrade old → new | Growth old → new | Income/s at Lv1 old → new |
|---|---|---|---|---|---|
| Beginner | Basement | $1K → $1.5K | $600 → $400 | 1.5 → 1.6 | $15 → $0.35 |
| Melody Maker | Basement | $15K → $12K | $9K → $3K | 1.5 → 1.6 | $60 → $2 |
| Drum Producer | Basement | $200K → $100K | $120K → $25K | 1.5 → 1.7 | $240 → $10 |
| Sample Producer | Bedroom | $4M → $8M | $2.4M → $2M | 1.5 → 1.9 | $640 → $40 |
| Sound Designer | Small Pro | $80M → $250M | $48M → $60M | 1.5 → 2.0 | $2.6K → $183 |
| Hitmaker | Professional | $2B → $5B | $1.2B → $1.25B | 1.5 → 2.0 | $10.3K → $850 |
| Superstar | Modern | $50B → $140B | $30B → $35B | 1.5 → 2.0 | $41K → $4K |

Each producer earns ~5× more than the previous one but costs 8–80× more. With the ×1.6–2.0 growth, every producer
stops paying back after roughly 10–15 levels, and saving for the next producer becomes the better choice.
Values live in `Data/Producers/*.asset`. Defaults are in `DefaultGameData.cs`; `Tools > Beat Factory > Apply Default Economy Balance` restores them.

## 5. Studios: prices kept, projected unlock times (play time [calendar])

| Studio | Price | Active, no ads | Optimized, no ads | Casual | Ads always on |
|---|---|---|---|---|---|
| Bedroom | $5M | 3:00 [6h] | 3:20 | 1:10 [1.1 d] | 1:30 |
| Small Pro | $25M | 4:00 [10h] | 4:39 | 1:40 [1.5 d] | 2:14 |
| Professional | $150M | 5:30 [1 d] | 6:27 | 2:10 [2.1 d] | 3:15 |
| Modern | $1B | 7:44 [1.3 d] | 9:21 | 3:06 [3 d] | 5:00 |
| Luxury | $7B | 11:00 [2.3 d] | 13:00 | 4:30 [4.4 d] | 7:22 |
| Major | $50B | 15:02 [3.3 d] | 18:00 | 6:10 [6.1 d] | 10:00 |
| Elite | $350B | 22:00 [5.1 d] | 26:00 | 9:05 [9 d] | 15:08 |
| Record Label | $2.5T | 33:00 [8 d] | 40:00 | 13:40 [13.5 d] | 22:33 |
| Mega Complex | $20T | 49:00 [12 d] | 59:00 [7 d] | 20:24 [20 d] | 34:00 [4 d] |

Before the rebalance (same model, old values), an active player bought the last studio after 2:34 of play.

## 6–7. Simulation results

Profiles: CASUAL (10 min every 3 h, 1 tap/s), ACTIVE (2 h, then 30 min every 2 h, taps 3 → 0.5/s, 3 artists on stage),
OPTIMIZED (4 h, then 30 min every hour, best-ROI buys, 6 artists on stage), AD_OCCASIONAL (OPTIMIZED + one PRODUCER BOOST
ad every 30 min), AD_FREQUENT (PRODUCER + 2X MONEY kept on by ads, INSTANT VIRAL on cooldown, doubled offline).
All profiles use the free boost and daily challenges, and nobody prestiges.

| Milestone (lifetime earnings) | Target | Active | Optimized | Casual | Ad occasional | Ad frequent | Active before |
|---|---|---|---|---|---|---|---|
| First upgrade (Hip-Hop) | 0:30–1:30 | 0:44 | 0:44 | 2:12 | 0:44 | 0:08 | — |
| $100 | 0:15–0:45 | 0:18 | 0:18 | 0:52 | 0:18 | 0:01 | 0:01 |
| $1K | 2–5 min | 2:47 | 2:32 | 7:34 | 2:32 | 1:01 | 0:13 |
| First producer | 3–7 min | 4:19 | 3:33 | 11:16 | 3:33 | 1:54 | 0:13 |
| $10K | 8–15 min | 8:59 | 7:17 | 17:17 [3h] | 7:17 | 5:09 | 0:14 |
| $100K | 20–40 min | 34:29 | 29:33 | 28:09 [6h] | 24:04 | 15:01 | 0:15 |
| $1M | 45–90 min | 1:20:00 | 1:14:00 | 40:01 [12h] | 59:45 | 33:01 | 0:53 |

Income per minute (ACTIVE; samples include whatever boost is running): 5 min $1.5K · 30 min $8.6K · 1 h $11K · 2 h $19.6K ·
5 h $3.2M · 10 h $61M · 20 h $1.5B · 40 h $18B.

Where the money comes from (ACTIVE): producers 38%, offline 35%, streams 19%, concerts 4%, daily challenges 2.5%, taps 0.6%,
missions and albums < 0.1% each. Early on, tapping is the main income until the first producers are hired.
OPTIMIZED: producers 46%, streams 24%, offline 16%, concerts 10.5%. CASUAL: offline 68%.

Most profitable path (all profiles): Hip-Hop → Beginner → Melody Maker → Drum Producer → Bedroom → Trap → Small Pro →
Sample → Rage → Professional → Sound Designer → Modern → Hyperpop → Luxury → Hitmaker → Major → Superstar → Elite →
Electronic → Experimental → Record Label → Mega Complex → Future. Artists are signed in between.

Ads: occasional ads make the game ~1.2–1.35× faster; ads kept on all the time make it ~1.7× faster (2.2× to the first $1M).
The no-ad game is complete on its own.

## 8. Remaining risks

- **Prestige is not simulated.** The first restart is possible at $10B earned (≈10 h of active play) and gives about +50% income.
  Players who restart will have their own pace.
- **Tours and artist fan growth are approximated.** The best concert venue follows the studio level. Concerts are 4–10% of income.
- **The simulated player is a perfect buyer.** Real players buy less efficiently and will be somewhat slower.
- **Slow stretch before the Bedroom Studio.** Between $1M (1:20) and the Bedroom Studio (3:00) the player mostly upgrades
  Drum Producers and signs the first artists. Worth watching in a real playtest.
- **The boost cap is not shown in the UI.** Stacking all three boosts shows "2X / 2X / 3X" chips, but producers earn ×4.
  The income per second shown is correct. A note in the BOOSTS window would make this clear.
- **The VIRAL-boost cooldown is not saved.** It restarts when the game restarts, so at most one extra 2-minute boost per launch.
- **Old saves keep their levels and money but earn much less.** The test save had $129.8M and earned $540/s afterwards.
  Nothing breaks, but a player from the old version can buy a lot at once.
- **Offline earnings are a large share for casual players (68%).** This is by design (50%, capped by the studio's offline hours)
  but can be lowered with `EconomyConfig.offlineEfficiency`.
- **Not tested on a phone.**

## 9. Modified files

- `Assets/_BeatFactory/Scripts/Config/EconomyConfig.cs`: manual beat defaults.
- `Assets/_BeatFactory/Scripts/Config/BoostConfig.cs`: `maxCombinedProducerBoost`, `viralBeatBoostCooldownMinutes`.
- `Assets/_BeatFactory/Scripts/Boosts/BoostManager.cs`: capped `ProducerIncomeFactor`, `CapFactor`.
- `Assets/_BeatFactory/Scripts/Core/GameManager.cs`: BoostCap multiplier, VIRAL 2X MONEY cooldown.
- `Assets/_BeatFactory/Scripts/Goals/MissionCatalog.cs`: smaller early mission rewards.
- `Assets/_BeatFactory/Scripts/Utils/NumberFormatter.cs`: decimals for money under $100.
- `Assets/_BeatFactory/Scripts/Editor/DefaultGameData.cs`: new producer table (with growth), Hip-Hop price.
- `Assets/_BeatFactory/Scripts/Editor/EconomySimulator.cs`: rewritten simulation (5 profiles, all income sources, report).
- `Assets/_BeatFactory/Scripts/Editor/Tests/BeatFactoryEditModeTests.cs`: 3 new tests, first-producer test updated.
- Data: `Data/Config/EconomyConfig.asset`, `Data/Config/BoostConfig.asset`, `Data/Genres/02_hiphop.asset`, `Data/Producers/01–07`.
- Docs: `ECONOMY_BALANCE.md` (v4), this report.

## 10. Tests

- **Passed (18/18 EditMode):** including new `Economy_ActivePlayer_HitsEarlyPacingTargets` (all early targets, the Bedroom
  Studio after 1.5 h, the Professional Studio after 4 h, every studio reachable without ads), `Economy_RewardedAds_HelpButDoNotTrivialize`,
  `Boosts_StackedBoosts_AreCapped`, and the updated `FirstProducer_AffordableWithFewTaps` (more than 300 taps, fewer than 1,600).
- **Play Mode, old save:** it loads without errors, money and levels are kept, income is recalculated, and the Welcome Back offline popup works.
  The real save was backed up first and restored afterwards (byte-identical).
- **Play Mode, fresh start:** MAKE BEAT shows "+$0.8 per beat", the Beginner Producer costs $1,500, and 60 taps earn ≈ $100.
- **Not run:** phone test, a real human playthrough, Android build (the task said not to rebuild).
