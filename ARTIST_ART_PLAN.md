# Beat Factory — Detailed Artist Art Plan (one artist + DRIP STUDIO)

The reference sheet is at `Assets/_BeatFactory/Art/Reference/character_sheet_dark_rage_artist.png` (1536×1024).
`Assets/Art/References/BeatFactory_Character_Reference.png` does not exist in the project. If you add the image there,
tell me and I'll use that path.

## 1. What the project has today

- **Artists:** 32 artists, all drawn by code (`Scripts/Editor/Art/ArtGenerator.*`). It is a flat vector style, and nothing is painted by hand.
- **One look per artist:** `ArtistVisualIdentity` (on every `ArtistData`) is the single source of the look: portrait, card,
  posters, the LED-screen image and the stage performer.
- **Stage performer:** `PerformerRig` is a 2D cut-out rig with 5 head sprites (idle, excited, smile, proud, blink), 1 torso,
  1 arm sprite (used for both arms), 1 leg sprite (both legs) and a microphone. Poses are animated in code: walk, idle groove,
  rap, hype (arms up), crowd interaction, big-drop jump, point, end pose. The outfit is painted into the torso, arm and leg
  sprites, so artist clothes cannot be swapped.
- **A wardrobe already exists, but only for the producer (the player), not for artists:** `PlayerAppearance` with 15 slots
  (hair, top, pants, sneakers, headwear, glasses, chain …), `PlayerAvatar` built from layers, `AvatarLibrary` sprites and the
  STYLE screen. Its ideas are reused below (slots, layers, IDs in the save, thumbnails).

## 2. The reference sheet: what can be used directly, what needs new sprites

**Can't be used as game sprites.** Every element is painted onto a dark background. Nothing is transparent and the elements touch each other.
They are also small:

| Element on the sheet | Approx. size on the sheet | Needed in the game |
|---|---|---|
| Full-body turnaround | ~130×370 px | ~1000 px tall (the stage close-up zooms ×3.3 on a ~190 px performer) |
| Close-up face | ~120×150 px | ~1024 px (the stadium LED wall shows the face very large) |
| Jackets, pants, shoes | ~90–110 px each | separate parts per body part, cut to the rig pivots |
| Animation frames | ~60×150 px | not used as frames: the rig animates parts instead |

Cutting pieces out would give blurry, low-resolution images with dark edges and no hidden parts (for example, the shoulder under a sleeve).
They would also not match the rig's joints.

**Can be used directly as direction (no new art needed):**
- The colour palette: black and charcoal, violet/purple neon, silver jewellery, a few red and white accents. It already matches the game's UI.
- The proportions: big head, slim torso, very baggy pants, chunky shoes, slight 3/4 view.
- The DRIP STUDIO screen layout (slots on the left, a large preview, an item grid with locked slots, SAVE OUTFIT) and the item list per slot.
- The list of expressions and poses. They map onto the existing rig states: IDLE → Idle, SING → Rap, POINT → Point,
  JUMP → BigDrop, CROWD → CrowdInteraction, END POSE → End, WALK → Walk. BEND and TURN are new.
- The concert look (LED wall close-up, purple beams, phones in the air) and the effects (smoke, lasers, sparks, fireworks).
  The game already has most of these.

**Not in this task:** the detailed fans (separate work for the crowd atlas) and the side/back turnaround views. The performer always faces the crowd.

**Code cannot reach this quality.** The current procedural style can't reproduce painted shading, fabric folds, locs or
jewellery at this level. That needs hand-made sprites from an artist (or reworked AI-assisted art). The code only assembles,
animates and swaps them.

## 3. Which artist

I suggest **NØX** (`nox`): Rage, locs, *"Wears all black. Every show turns into a mosh pit."*, signable in the Bedroom Studio.
That is exactly the "Dark Rage Artist" on the sheet. The artist's id and save data stay the same; only the art changes.

## 4. Required assets (transparent PNG, separate files)

**General rules:**
- **View and light:** front 3/4 view facing slightly left, the same light direction everywhere, no baked background, no drop shadow.
- **Rig size:** the whole character is drawn at **1000 px height** (feet to top of hair) on one template, and every part is
  exported from that template at full resolution.
- **Overlap:** each part overlaps its neighbour by 20–40 px, so joints never open when they rotate.
- **Pivots:** marked in the template file (one small dot layer per joint) or listed in a short text file.
- **Format:** PNG-32 with straight alpha, sRGB, no white or dark fringe on the edges.
- **File names:** `nox_<part>.png` for the body, `nox_<slot>_<item>_<part>.png` for clothes. Folder: `Assets/_BeatFactory/Art/Artists/nox/`.

### 4.1 Base body (under the clothes)

| File | Size (px) | Pivot | Notes |
|---|---|---|---|
| `nox_head_idle.png` | 420×460 | bottom of the neck | face only: no hair, no earrings |
| `nox_head_rap.png` | 420×460 | same | mouth open (singing / rapping) |
| `nox_head_hype.png` | 420×460 | same | shouting, eyebrows up |
| `nox_head_smile.png` | 420×460 | same | |
| `nox_head_proud.png` | 420×460 | same | chin up, end pose |
| `nox_head_blink.png` | 420×460 | same | eyes closed |
| `nox_neck.png` | 140×120 | top centre | |
| `nox_torso_base.png` | 360×420 | hips centre | skin and undershirt shape (hidden by tops) |
| `nox_upperarm_near.png`, `nox_upperarm_far.png` | 120×230 | shoulder | skin |
| `nox_forearm_near.png`, `nox_forearm_far.png` | 110×210 | elbow | skin |
| `nox_hand_mic.png` | 120×140 | wrist | hand holding the mic (the mic sprite stays separate) |
| `nox_hand_open.png`, `nox_hand_point.png`, `nox_hand_fist.png` | 120×140 | wrist | for hype, point and end pose |
| `nox_leg_near.png`, `nox_leg_far.png` | 160×420 | hip | only needed if the pants are cropped |

### 4.2 DRIP STUDIO items: the first set (2 per slot, 10 items)

Each clothing item is drawn on the same template and exported **per body part**, with the same sizes and pivots as the body part it covers.

| Slot | Items | Files per item |
|---|---|---|
| Hair | `locs_black` (default), `locs_red` | `_back.png` (behind the head), `_front.png` (over the face) |
| Top | `tee_cross_black`, `tee_skull_white` | `_torso.png` |
| Jacket (optional) | `leather_black` (default), `puffer_purple` | `_torso.png`, `_upperarm_near.png`, `_upperarm_far.png`, `_forearm_near.png`, `_forearm_far.png`, `_collar.png` (in front of the neck) |
| Pants | `baggy_black_cargo` (default), `baggy_denim_ripped` | `_waist.png`, `_leg_near.png`, `_leg_far.png` |
| Shoes | `chunky_black` (default), `chunky_white` | `_near.png`, `_far.png` |
| Accessories | `chains_silver_cross`, `shades_black`, `rings_silver` | chain: `_torso.png`; shades: `_head.png`; rings: `_hand.png` |

Plus, for each item:
- **Thumbnail:** `nox_<slot>_<item>_thumb.png`, 256×256 on a transparent background, for the DRIP STUDIO grid.

### 4.3 Portrait and close-up

| File | Size | Use |
|---|---|---|
| `nox_portrait.png` | 1024×1024 | contract card, posters, LED wall (default outfit) |
| `nox_portrait_bg.png` (optional) | 1024×1024 | coloured backdrop for the card |

If the jacket changes, the portrait can either stay in the default outfit or be assembled from the same layers
(head + hair + jacket collar + chain). The second option is in the plan as an extra step.

**Total for one artist with the first DRIP set:** about 70 PNG files. The full list on the sheet (6 jackets, 4 tees, 7 pants,
4 shoes, 5 hairstyles, accessories) is about 200.

## 5. Integration architecture (what I will build once the plan is approved)

1. **`ArtistRig` data (ScriptableObject):** body part sprites + pivots + expression heads for one artist, and a reference to `WardrobeItem`s.
2. **`WardrobeItem` (ScriptableObject):**
   - id, slot, sprites per body part, thumbnail;
   - unlock rule (studio level / price / concert level);
   - which slots it hides (a hood hides the hair back, a jacket hides the top's sleeves).
3. **`ArtistOutfit` (saved per artist, IDs only):** e.g. `{ hair: "locs_red", jacket: "leather_black", … }`. Old saves have no
   outfit, so the default one is used. No save version bump is needed (new optional field).
4. **`LayeredPerformerRig`:** the current `PerformerRig` gets more bones (upper arm, forearm, hand, waist), and each bone carries
   ordered clothing layers. The existing poses and states are kept and adapted to the extra joints (elbow bend for SING / POINT).
   Artists without an `ArtistRig` keep using today's rig and art, so nothing changes for the other 31 artists.
5. **DRIP STUDIO screen:**
   - opens from the artist's career page;
   - slot tabs, a live preview (the same rig, idle pose), an item grid with locked items, SAVE OUTFIT;
   - all texts are localization keys in 7 languages.
6. **Import pipeline:** an editor tool (Tools > Beat Factory > Art > Import Artist Rig) does the following:
   - reads `Art/Artists/<id>/`;
   - sets the import settings (sprite, no mipmaps, max size 1024, compression) and pivots;
   - fills the `ArtistRig` and `WardrobeItem` assets;
   - reports missing or wrongly sized files.
7. **Tests:**
   - wardrobe save round-trip and old-save default;
   - every item has all parts for its slot;
   - the rig builds with any combination of items.
8. **Memory/performance:**
   - only the selected artist's sprites are loaded (one sprite atlas per artist, ~2048² ASTC);
   - DRIP previews reuse the same sprites.

## 6. Order of work

1. You approve the plan and the artist (NØX), and get the art from section 4 (base body + default outfit first).
2. I build steps 1–4 and 6 with **placeholder parts** cut from the current procedural NØX, so the system can be tested before the art arrives.
   I will label them clearly as placeholders.
3. When the real PNGs arrive, the import tool plugs them in. Then I tune pivots and poses and test the concert close-ups.
4. DRIP STUDIO screen and the second item of every slot.
5. The portrait made from the same layers (optional).

## 7. Status (12.10.2026)

**Built and tested (21/21 EditMode tests, Play Mode concert with NØX):**
- **Rig and wardrobe data.** `ArtistRig` and `WardrobeItem` assets (`Scripts/Artists/Wardrobe/`), plus `ArtistWardrobe`.
  The outfit is saved as item ids in `SaveData.artistOutfits`; old saves fall back to the defaults.
- **Detailed performer.** `LayeredPerformer` is used by `PerformerRig` whenever an artist has a rig: elbows bend, shoes stay flat,
  the mic sits in the palm, and the outfit changes live. The other 31 artists are unchanged.
- **Import tool.** Tools > Beat Factory > Art > Import Artist Rig (NØX): sets pivots from `rig.txt` and lists missing files.
- **Placeholder parts for NØX.** Tools > Beat Factory > Art > Generate NØX Placeholder Parts. These are **flat placeholders,
  not production art**.

**Still to do:**
- the real art (section 4);
- DRIP STUDIO screen (step 4 of section 6);
- the portrait built from layers.

**Update (12.10.2026, later):**
- NØX's parts are now cut from the user's sheet (`Art/Reference/nox_parts_sheet.png`) by Tools > Beat Factory > Art >
  Build NØX From Character Sheet, then imported. The placeholders are gone.
- What comes from where:
  - the body / default look from the front figure;
  - 5 expressions (+ hype) from HEADS;
  - arms and hands from BODY PARTS;
  - DRIP extras: purple fur jacket, ripped denim, black and purple shades.
- **Limits:**
  - **Resolution.** The sheet is low resolution (figure ~400 px), so parts are upscaled 1.7–3.6× and look soft in close-ups.
  - **Hidden areas.** The jacket and pants behind the hanging arms/hands are filled in by the tool, so the edges are visible on close inspection.
  - **Baked-in outfit.** The default hair, top, pants and shoes are painted into the body (empty items in those slots), so they
    can't be swapped yet.
  - **Real fix.** Separate full-resolution PNGs (section 4).
