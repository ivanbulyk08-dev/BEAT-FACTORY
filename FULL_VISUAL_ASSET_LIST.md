# Beat Factory — Full Visual Asset List

Status: ✅ delivered (production quality) · 🟡 delivered but not production-ready · ❌ missing.
All sizes and rules: `ART_STYLE_GUIDE.md` §9. Folder for new art: `Assets/_BeatFactory/Art/...` (paths below).

## 1. Studios (`Art/Studios/studio_NN/`)

| # | Asset | Status |
|---|---|---|
| 1 | Basement_Main.png | 🟡 864×1536 (style ✅, resolution below the 1080×1920 target) |
| 1 | Basement_Main_CleanScreens.png (optional) | ❌ |
| 2–10 | <Studio>_Main.png (Bedroom, Small Pro, Professional, Modern, Luxury, Major, Elite, Record Label, Mega Complex) | ❌ ×9 |
| 1–10 | <Studio>_Foreground.png (optional) | ❌ ×10 |

## 2. Characters (`Art/Artists/<id>/`, `Art/Producers/<id>/`, `Art/Player/`)

| Asset | Status |
|---|---|
| NØX layered parts (≈70 PNG, 1000 px template) | 🟡 cut from the concept sheet (low res, outfit baked in) |
| NØX portrait 1024² | ❌ |
| Hero artists ×5: layered parts + portrait | ❌ |
| Standard artists ×26: portrait + stage pose + 6 heads | ❌ |
| Producers ×7: seated rig (≈20 PNG) + portrait | ❌ |
| Player avatar wardrobe in the new style (≈60 PNG + 36 thumbs) | ❌ |
| DRIP STUDIO items for NØX (jackets, tees, pants, shoes, hair, accessories as separate parts) | ❌ (only on the concept sheet) |

## 3. Concerts (`Art/Venues/`)

| Asset | Status |
|---|---|
| venue_0..4 backgrounds 1440×2560 | ❌ ×5 (procedural now) |
| venue_0..4 foregrounds | ❌ ×5 |
| Crowd atlas 2048×1024 (painted fan parts) | ❌ |
| Far crowd strips ×3 | ❌ |
| stadium_aerial.png 2048×1120 painted | ❌ (procedural now) |
| Effects: beam, laser core/glow, flame, spark, smoke, confetti, glow, note, mic | ❌ ×10 (procedural now) |

## 4. UI (`Art/UI/`)

| Asset | Status |
|---|---|
| Nav icons ×12, HUD icons ×9 | ❌ |
| 9-slices: panel, primary/secondary button, card, tab | ❌ ×5 |
| MAKE BEAT skin ×2 | ❌ |
| Album covers ×6 (`Art/Albums/`) | ❌ (procedural now) |
| Venue thumbnails ×5, genre badges ×8 | ❌ |
| App icon (adaptive foreground + background) | 🟡 generated from the old icon |
| Store screenshots / feature graphic | ❌ (after the redesign) |

## 5. References (not used in the game)

| Asset | Use |
|---|---|
| `Art/Reference/character_sheet_dark_rage_artist.png` | style / DRIP STUDIO reference |
| `Art/Reference/nox_parts_sheet.png` | NØX design reference (source of the current stand-in parts) |

## Totals (to reach the full target)

About **9 studio paintings** plus a high-res basement, **5 venues + aerial**, **≈2,100 character PNGs** (32 artists + 7 producers + player wardrobe;
much less with the "standard tier" from CHARACTER_ART_PLAN.md: ≈700), **≈60 UI images**, **≈15 effect/crowd images**.
