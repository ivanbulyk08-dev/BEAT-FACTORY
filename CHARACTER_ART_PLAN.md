# Beat Factory — Character Art Plan

Builds on `ARTIST_ART_PLAN.md` (NØX: rig, wardrobe, import pipeline — already implemented). Style: `ART_STYLE_GUIDE.md` §4.

## Characters in the game today

| Group | Count | Where they appear | Current art |
|---|---|---|---|
| Artists (signable) | 32 | contract card, artist cards, posters, LED wall, stage performer | procedural flat style (`ArtGenerator.Artists.cs`, `Art/Characters/Artists`, `Art/Characters/Artists/Stage`: 256 parts) |
| NØX (detailed) | 1 | stage performer (layered rig + wardrobe) | cut from the user's sheet, low resolution (`Art/Artists/nox`) |
| Producers (hireable) | 7 types, 3 drawn | studio card (at desks), producer rows (portraits) | procedural (`Art/Characters/Producers/beginner|melody|drum`) |
| Player avatar (producer) | 1 + STYLE wardrobe | studio card, STYLE screen | procedural layers (`Art/Characters/Player/Layers`, 65 parts, 36 thumbnails) |
| Fans | thousands | concerts | procedural crowd atlas (`Art/Venues/crowd_atlas.png`) — see CONCERT_ART_PLAN.md |

## One system for all characters

The rig made for NØX (`ArtistRig` + `WardrobeItem` + `LayeredPerformer`, import via `rig.txt`) becomes the standard for every
character. That includes the producers and the player: same template, same joints, same file naming
(`Art/Artists/README.md`).

| Layer set | Files per character |
|---|---|
| Body | 6 heads (idle, rap, smile, proud, blink, hype), neck, torso, upper arms ×2, forearms ×2, hands (mic, open, point, fist, far), legs ×2 |
| Default outfit | hair back/front, top, jacket (optional), pants (waist + legs), shoes ×2, accessories |
| Portrait | 1024×1024 bust in the same outfit (card, contract, poster, LED wall) |
| Producer extra | `<id>_seated.png` set: torso + legs bent at a desk, hands on keys/mouse (2 frames each), headphones |

## Priorities (realistic scope)

Drawing 32 detailed artists = about 32 × 70 = 2,200 PNGs. That is not realistic in one go, so:

1. **Hero tier (fully layered):** NØX first, then the 4–6 artists who perform most (one per genre).
2. **Standard tier:**
   - all other artists get a high-quality **portrait** plus **one full-body flattened stage pose** (mic up);
   - on stage they use a light 3-part rig (body, head with expressions, mic arm);
   - this replaces the flat style and is about 10 files each.
3. **Producers:**
   - 7 producer types, seated rig (≈20 files each) + portrait;
   - they appear in the studio at the desks.
4. **Player avatar:**
   - redraw the STYLE wardrobe items in the new style on the same template;
   - the slots stay the same (hair, top, pants, sneakers, headwear, glasses, chain, watch, headphones, rings, earrings, tattoos).

## DRIP STUDIO

- **Today.** The data, save and live dressing exist for NØX (`ArtistWardrobe`, `SaveData.artistOutfits`); the **screen does not exist yet**.
  The player's STYLE screen (`StyleScreenUI`) is a separate, older system for the producer.
- **Plan.** DRIP STUDIO screen for detailed artists (opened from the artist career page), layout as in the user's reference.
  Later the player's STYLE can move onto the same system.

## Missing assets (characters)

| Asset | Status |
|---|---|
| NØX full-resolution layered parts (≈70 PNG) | **missing**; the current cut-outs are a stand-in |
| NØX portrait 1024² | missing (the old procedural portrait is used) |
| Hero artists 2–6: layered parts + portraits | missing |
| 26 standard artists: portrait + stage pose + 6 heads | missing |
| 7 producers: seated rig + portrait | missing |
| Player avatar: wardrobe items redrawn (≈60 PNG) | missing |

## Code (after approval)

- **Light rig for standard artists.** A `LayeredPerformer` variant with 3 parts, or `ArtistRig` with an optional "flat body" mode.
  Files: `Scripts/Concerts/LayeredPerformer.cs`, `Scripts/Artists/Wardrobe/ArtistRig.cs`.
- **Portrait override.** `ArtistData.avatar` is set from `Art/Artists/<id>/<id>_portrait.png` when present.
  Files: `ArtistRigImporter.cs`, `BeatFactorySceneBuilder.Studio.cs`.
- **Seated producer rig in studios:** new `ProducerRigView` using the same bones and the per-studio desk positions.
- **DRIP STUDIO screen:** new `DripStudioScreen.cs` plus builder code and localisation keys (7 languages).

**Complexity:**
- Code: medium-high (4–6 days, mostly DRIP STUDIO and the producer rigs).
- Art: very high.

**Tests:**
- rig completeness per character;
- a wardrobe round-trip;
- every artist has a portrait;
- close-up screenshots at 1440p.
