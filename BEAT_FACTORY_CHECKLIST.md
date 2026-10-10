# Beat Factory — Development Checklist

Unity 6000.6.3f1 · URP 2D · uGUI + TextMeshPro · New Input System · Portrait 1080×1920

## Architecture
- Everything game-specific lives in `Assets/_BeatFactory/`
  - `Scripts/Core` — GameManager (creates systems, `GameManager.Instance.X`)
  - `Scripts/Config` — ScriptableObject configs (all balance values)
  - `Scripts/Economy`, `Producers`, `Genres`, `Studio`, `Artists`, `Streams`, `Prestige`, `Save`, `Audio`, `Services` (ads, notifications)
  - `Scripts/UI` — MonoBehaviours that only display state and forward clicks
  - `Scripts/Utils` — NumberFormatter etc.
  - `Scripts/Editor` — scene builder (Tools > Beat Factory > Build Game Scene)
  - `Data/` — ScriptableObject assets (configs, producer/genre/studio/artist definitions)
- Gameplay systems are plain C# classes owned by GameManager; UI listens to their C# events.
- Money is `double` (enough for 1e308; formatted by NumberFormatter).

## Art pass (done)
- [x] Procedural original art (Tools > Beat Factory > Art > Regenerate Art), see ART_PIPELINE.md
- [x] HOME: animated studio scene (3 room tiers with crossfade), main character + 3 producers at stations, monitor equalizers, pulsing speakers
- [x] Character animations: idle nod, typing, success, upgrade, bounce
- [x] FX: notes on beats, sparkles on upgrades/HIT, confetti + shake on VIRAL; "BEAT CREATED +$5"
- [x] Icons set (nav, top bar, MAKE BEAT), producer portraits on cards, income/sec on cards

## Phases
- [x] 1. Project architecture (folders, GameManager, EconomyConfig, NumberFormatter, SafeArea, scene builder)
- [x] 2. Currency system (EconomyManager: Money / Fame, events, top bar HUD)
- [x] 3. Manual beat creation (BeatManager, MAKE BEAT button, beat counter, pooled +$ floating text, button bounce)
- [x] 4. Producer system (ProducerData assets, ProducerManager with single tick, scrollable list, progress bars, unlock chain, $/s in HUD)
- [x] 5. Producer upgrades (cost = upgradeBaseCost * growth^(level-1), x2 income milestone every 10 levels (EconomyConfig.producerMilestoneEvery), disabled button when unaffordable)
- [x] 6. Beat quality system (BeatQualityConfig asset, weighted roll, avg multiplier for big batches & $/s, colored floating text, HIT/VIRAL popup + screen flash, SpecialChanceMultiplier hook for Viral Boost)
- [x] 7. Genres (8 GenreData assets, unlock in order, best genre = income multiplier, MultiplierStack for all income bonuses, color theme transition, unlock button in studio card, "NEW GENRE!" celebration)
- [x] 8. Studio progression (10 StudioData assets (Basement → Mega Music Complex, $0 … $20T), income + speed multipliers via MultiplierStack, gates genres, upgrade button, level pips, card color transition + bounce, animated equalizer that grows with the studio, "STUDIO UPGRADE!" celebration)
- [x] 9. Save system (JSON in persistentDataPath, temp-file write + .bak backup, version/migration hook, autosave every 10s + after purchases + on pause/focus loss/quit, GameSettings saved, ResetProgress, Tools > Beat Factory > Save menu)
  - Note: save path depends on Company/Product name — set the final Company Name before release (Phase 22).
- [x] 10. Offline earnings (OfflineEarningsManager: avg income/s incl. quality × time, 8h cap, 60s minimum, efficiency setting; on game start AND on return from background; auto-collect on pause/quit so nothing is lost; WELCOME BACK modal; reusable ModalWindow; hidden "x2 WITH AD" hook for Phase 17)
- [x] 11. UI polish (bottom nav with 6 tabs + glyph icons + fade, PRODUCERS screen with x1/x10/MAX bulk buy (closed-form geometric cost), MUSIC screen with genre & studio lists, "can buy" badges, throttled button refresh, ShopRow prefab, placeholder screens for Artists/Prestige/Settings)
- [x] 12. Artists (32 ArtistData assets, stacking multipliers ×1.03–×1.06 each, studio requirements, ARTISTS screen + total bonus, avatars in the studio card with pop-in, "ARTIST SIGNED!" celebration, badge, saved)
- [x] 13. Streams (StreamConfig: 4 platforms, additive platform multipliers, 1 stream = $1; GOOD+ beats earn streams = base reward × streamShare (0.5/1/2/5) × platforms; CASH OUT; streams pill in top bar → MUSIC; "NOW STREAMING!" celebration; saved)
  - Balance: +10% income with SoundCloud only, ~+55% with all platforms
  - [ ] Later: offline beats don't earn streams yet
- [x] 14. Prestige (RESTART LABEL: unlock at $10B run earnings, Fame = √(run / $100M), +5%/Fame; resets run systems, keeps Fame/stats/settings; PRESTIGE screen with progress bar + preview; reusable ConfirmDialog; "LABEL RESTARTED!" celebration; badge when Fame would at least double; saved immediately)
- [x] 15. Boosts (BoostConfig: 2X MONEY 5m, 2X SPEED 5m, PRODUCER BOOST ×3 10m, VIRAL BOOST ×5 3m; real-time timers, stacking up to 60m; free boost every 20m; VIRAL beat → 2X MONEY 2m; boost strip with countdown chips + BOOSTS popup; offline earnings exclude all boost effects; saved)
- [x] 16. Audio (AudioManager: 8-voice SFX pool + looping music with fade, per-sound volume/pitch variation/anti-spam interval, respects Sound/Music settings; AudioEvents maps gameplay events → sounds; click sound in ButtonPunch; AudioConfig asset; 12 SFX + 22.6s lo-fi loop synthesized by Tools > Beat Factory > Audio > Regenerate Sounds; AudioListener on camera)
  - Replace placeholder clips by dragging your own AudioClips into Data/Config/AudioConfig
- [x] 17. Ads abstraction (IAdsProvider + FakeAdsProvider/FakeAdOverlay test ads that pause game & audio; AdsManager with interstitial caps: not in first 5 min, ≥4 min apart, ≤5/session, never with Remove Ads; rewarded: x2 offline earnings, WATCH AD per boost, INSTANT VIRAL BEAT; interstitial only 3s after prestige; IsShowingAd guards OnApplicationPause; adsRemoved saved)
  - To go live: install an ad SDK, implement IAdsProvider, return it from AdsManager.CreateProvider()
- [x] 18. Tutorial (6 steps: welcome → make beats ($15) → hire → upgrade ($50) → unlock Hip-Hop → finish; non-blocking banner above MAKE BEAT + pulsing outline around the target; progress "$X / $Y"; auto-advance by polling 5×/s; already-done steps skipped (old saves); SKIP; saved)
- [x] 19. Settings (SETTINGS screen: Music / Sound effects / Notifications switches (saved, applied live), stats card, REPLAY TUTORIAL, RESET SAVE with confirmation, version; NotificationManager abstraction: schedules "Your producers created N new beats!" at 2h and "$X is waiting" at 8h when the app goes to background, cancels on return, respects the setting; MobileNotificationProvider on phones (com.unity.mobile.notifications 2.5.0), permission asked after the tutorial; LogNotificationProvider in the Editor)
- [x] 20. Optimization (physics simulation off, money text formatted max 20×/s, equalizer 30 Hz, no enumerator allocs in badges; late game ~2.8 ms/frame in editor incl. editor overhead; all heavy UI updates throttled, hidden tabs inactive, pooled floating text)
  - [ ] Later if needed on low-end phones: nested Canvases for top bar / lists (needs draw-order care)
- [x] 21. Final bug fixing (money/earnings clamped below Infinity so saves never break; x2 offline ad pays correctly even if money was auto-collected during the ad; smoke test: tutorial, all tabs + all buttons, boosts, offline, HIT/VIRAL, prestige, 1e308 money, MAX buy to lvl 5035, save/reload → 0 exceptions)
- [x] 22. Mobile build preparation (Tools > Beat Factory > Build: Apply Mobile Settings / Build Android APK / Build AAB; company "Beat Factory Studio", id com.beatfactorystudio.beatfactory, v1.0.0 build 1; IL2CPP, ARM64+ARMv7, min Android 8 (API 26), iOS 13; portrait; generated neon equalizer icon; only Game scene in build)
  - [ ] Install Android Build Support / iOS Build Support in Unity Hub, then build
  - [ ] Replace placeholder company/bundle id with your own before publishing
  - [ ] Optional: real ad SDK (IAdsProvider), com.unity.mobile.notifications (INotificationProvider)

## Added after the first checklist (state on 09.10.2026)
- [x] Concerts, tours, living crowd, per-artist concert music (Scripts/Concerts)
- [x] Missions (41), daily/weekly challenges, artist chemistry (Scripts/Goals)
- [x] Localization: 7 languages, CLDR plurals, locale number formats, static CJK/Cyrillic font atlases (Scripts/Localization, Resources/Localization, Tools > Beat Factory > Localization)
- [x] AdMob (SDK 11.5.0, UMP consent, test mode, privacy-settings button, retry without internet, INSTANT VIRAL 15-min cooldown)
- [x] Android Back button (BackButtonRouter), adaptive icon, release-build check (Tools > Beat Factory > Release)
- [x] Device-clock guard for offline earnings (GameManager.TrustedSecondsSince), unreadable saves are kept as .corrupt copies
- [x] EditMode tests (Scripts/Editor/Tests, Window > General > Test Runner)
- [ ] Before release: real rewarded/iOS ad unit IDs, privacy policy URL (AppLinks.cs), release keystore + AAB, device test pass
- [ ] Not built yet: music charts, single tracks, random industry events, in-app purchases, cloud saves
