# Beat Factory — UI Redesign Plan

Style: `ART_STYLE_GUIDE.md` §7. All UI is built by code (`Scripts/Editor/BeatFactorySceneBuilder*.cs`, `UIPrefabBuilder.cs`,
runtime `Scripts/UI/UIKit.cs` + `UISkin`). Most of the redesign is therefore **new sprites + style values**, not rebuilding screens.

## Features that exist (redesign these)

| Screen / element | Files | Notes |
|---|---|---|
| Top HUD (money, fame, beats, streams) | `CurrencyHUD.cs`, `BeatCounterUI.cs`, `StreamCounterHUD.cs`, builder `CreateTopBar` | dark glass bar over the studio art |
| Boost bar + BOOSTS popup + featured boost card | `BoostBarUI.cs`, `BoostsPopupUI.cs`, `BoostOfferCard.cs`, builder `.Boosts.cs` | already in the new violet style |
| Goals button + Missions / Challenges / Chemistry | `GoalsButton.cs`, `GoalsScreen.cs`, `MissionCatalog.cs` | |
| HOME: studio + MAKE BEAT | `MakeBeatButton.cs`, `StudioFocusMode.cs`, builder `.Basement.cs` | keep uncluttered |
| Bottom navigation (6 tabs) | `ScreenNavigator.cs`, `NavBadge.cs`, builder `.Screens.cs` | new painted icons |
| Producers (list, hire/upgrade, buy ×1/×10/max) | `ProducerListUI.cs`, `ProducerRow.cs`, `BuyModeSelector.cs`, prefab `ProducerRow` | portraits from the new character art |
| Music: genres, streams/platforms, albums | `MusicScreenUI.cs`, `GenreUnlockButton.cs`, `StreamsPanelUI.cs`, `AlbumsPanelUI.cs`, `AlbumCardView.cs` | album covers need new art |
| Album release ceremony | `AlbumReleasePresentation.cs` | |
| Career: artists, contract popup, artist career page (concert booking, tours) | `ArtistsScreenUI.cs`, `ArtistContractPopup.cs`, `ArtistCareerScreen.cs`, `TourCompletePopup.cs` | **concert booking lives here** |
| Concert show + results | `ConcertShowUI.cs`, `RewardPanel.cs` | see CONCERT_ART_PLAN.md |
| Studio card, next-goal HUD, studio upgrade popup | `StudioCardUI.cs`, `NextGoalHUD.cs`, `StudioUpgradePopup.cs` | the card is hidden on HOME now; the upgrade popup is still used |
| STYLE (player wardrobe) | `StyleScreenUI.cs`, `StyleItemCard.cs` | |
| Prestige | `PrestigeScreenUI.cs` | |
| Settings + language picker + privacy | `SettingsScreenUI.cs`, `LanguagePicker.cs` | |
| Popups: welcome back, celebration, confirm, tutorial | `WelcomeBackPopup.cs`, `CelebrationPopup.cs`, `ConfirmDialog.cs`, `TutorialUI.cs` | |
| Test-ad overlay | `FakeAdOverlay.cs` | editor/dev only |

## Not implemented (separate; would be new features)

| Feature | Status |
|---|---|
| **DRIP STUDIO** screen | data + save + live dressing exist for NØX; **no screen** |
| **Charts** (Top 100 / Top 10 / #1) | not in the game (MissionCatalog notes it) |
| **Beat Spin** | not in the game |
| **Shop** (in-app purchases, Remove Ads) | only the `AdsRemoved` flag exists; no store, no IAP |

These need game design and approval before any UI work. The redesign only reserves visual slots for them.

## Redesign approach

1. **Visual tokens.**
   - Update `UISkin` (colours, fonts) and the generated surface sprites (`UIStyle`: panel, button, glow) to the guide.
   - This restyles almost everything at once.
2. **Painted assets replace generated ones.**
   - Nav icons (6) and HUD icons (money, fame, beats, streams, boost, viral, upgrade).
   - Panel/button 9-slices, the MAKE BEAT button skin, the album covers (6).
3. **Screen layouts.** Keep the current information and order; for the HOME and Career pages, check that studio/artist art is visible.
   - **Producers:** larger portrait rows.
   - **Career:** artist cards with the new portraits, booking cards with venue thumbnails.
4. **Transitions.** Shared 200 ms fade + 12 px slide for tab changes (`ScreenNavigator`) and popups (`ModalWindow` already animates).

## Missing assets (UI)

| Asset | Count | Size |
|---|---|---|
| Nav icons (Studio, Producers, Music, Career, Prestige, Settings), normal + selected | 12 | 128² |
| HUD icons (money, fame, beats, streams, boost bolt, viral, upgrade, goals, lock) | 9 | 128² |
| Panel 9-slice, button 9-slice (primary, secondary), card, tab | 5 | 256² |
| MAKE BEAT button skin (normal/pressed) | 2 | 1000×220 |
| Album covers | 6 | 512² |
| Venue thumbnails for booking cards | 5 | 512×288 |
| Genre badges | 8 | 256² |

**Rule:** localisation is untouched. All text stays in `Resources/Localization`; the art must not contain words.

## Code (after approval)

- **Skin swap:** `UISkin` asset values, `UIStyle.Load` loads painted sprites when present (generated ones stay as a fallback).
- **Icons:** the builder loads from `Art/UI/Icons` (same file names), so there are no code changes beyond the import settings.
- **Transitions:** `ScreenNavigator` gets a short fade/slide.

**Complexity:**
- Code: low-medium (2 days).
- Art: medium (≈50 small assets).

**Tests:**
- screenshots of every screen in EN + RU + ZH;
- tap targets ≥ 96 px;
- 60 fps while scrolling the producer list.
