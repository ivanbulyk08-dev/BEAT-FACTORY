# Beat Factory — Art Style Guide

**Master reference:** `Assets/_BeatFactory/Art/Studios/studio_01/Basement_Main.png` (864×1536). Every new image must look like it belongs in that room.

**Character reference:** `Art/Reference/nox_parts_sheet.png` and `character_sheet_dark_rage_artist.png`.

## 1. In one sentence

Painterly-clean 2D illustration of a lived-in underground music world at night: deep black and violet, warm tungsten pools of light, purple neon accents, detailed gear and clothing, readable at phone size.

## 2. Colour palette

| Role | Colour | Hex | Use |
|---|---|---|---|
| Shadow base | Near-black violet | `#0D0B1A` | darkest areas, UI background |
| Wall / cloth dark | Deep plum | `#1E1630` | large surfaces |
| Mid violet | Night violet | `#3A2A5C` | walls in neon light, fabric |
| Neon | Electric purple | `#8A3DFF` / `#B65CFF` | LED strips, rim light, UI accent |
| Neon hot | Magenta | `#FF3DC8` | screens, highlights, MAKE BEAT |
| Warm light | Tungsten orange | `#FF9A3D` → `#FFC97A` | lamps, wood, skin highlights |
| Warm shadow | Burnt umber | `#5A2E1A` | floor, wood, skin shade |
| Cool accent | Ice cyan | `#3DE1FF` | small UI/screen details only |
| Money | Mint | `#4DFFA8` | money in UI only |
| Gold | Soft gold | `#F2C14E` | fame, premium, jewellery |
| Text | Off-white | `#F2F0FF` | text, chrome highlights |

**Rules:**
- **Warm vs. cool.** Every scene has one warm key light (lamp, sunset, stage spot) and violet neon fill. No scene is only one colour temperature.
- **Saturation.** Saturated colour goes in lights and small accents; big surfaces stay dark and desaturated.
- **No pure white** except in glints. No pure black outline fills.

## 3. Linework and shading

- **Lines.** Dark (not black) outlines, thicker on silhouettes (≈3–4 px at 1080 px width), thinner inside (1–2 px). The line colour is tinted by the local light (violet in neon, brown in warm light).
- **Shading.** Cel-plus-soft: 2–3 hard value steps plus soft gradients for light falloff. Rim light in the neon colour on characters and furniture edges facing the neon.
- **Textures.** Painted, not photographic: fabric folds, wood grain, worn concrete, cable clutter. No noise filters, no pixel art, no photo textures.
- **Glow.** Light sources get a soft bloom painted in; the game adds animated glow on top (see §8). Do not paint very strong bloom that hides detail.

## 4. Characters

- **Proportions.** Stylised realistic, about 6.5–7 heads tall (not chibi): slim torso, long legs, oversized clothing (baggy pants, chunky shoes, puffers). Faces are recognisable at 120 px head height.
- **Faces.** Anime-influenced but grounded: defined jaw, detailed eyes, nose bridge, small details (face tattoos, piercings).
- **Diversity.** Skin tones from very dark to very light, varied hair (locs, braids, twists, fades, dyed tips), body types.
- **Pose language.** Relaxed, confident, hip-hop body language. The front/3-4 view faces slightly left, the same as the NØX sheet.
- **Same person everywhere.** One artist = the same face, hair, tattoos and signature items on the portrait, poster, LED wall and stage.

## 5. Lighting rules

- **Key light.** One warm practical light per room (hanging bulb, desk lamp, sunset window).
- **Fill.** Purple LED strips (ceiling, stairs, behind monitors). Magenta and cyan come from screens.
- **Light belongs to sources.** Every bright area has a visible source in the image. The game animates those sources only.
- **Stage.** Concerts may break the warm rule: there the key light is coloured spots, and the warm light is pyro.

## 6. Perspective and composition (portrait 9:16)

- **Camera.** Eye level slightly above a seated person; one- or two-point perspective; mild wide angle.
- **Focal point.** The workstation (laptop/desk/monitors) is in the middle third (y ≈ 40–55 % from the top), never covered by UI.
- **Reserved zones.**
  - Top 12 % is for the HUD (ceiling, pipes, lights — nothing important).
  - Bottom 22 % is for MAKE BEAT and navigation (floor, rug, table clutter — nothing important).
- **Foreground.** Close objects (table, cans, notebook) frame the bottom; they may be cut off.
- **Safe width.** Important content stays within the middle 80 % horizontally (tall phones crop the sides).

## 7. UI design

- **Panels.** Dark glass: `#0D0B1A` at 85–92 % opacity, 28–40 px corner radius, a 1 px violet top edge, soft shadow.
- **Primary buttons.** Magenta → violet gradient pill with a soft neon glow (MAKE BEAT style). Secondary: dark glass with a violet outline.
- **Typography.**
  - Anton for big numbers.
  - Oswald for headings and buttons (spaced capitals).
  - Roboto for body text.
  - Unicode fallbacks for CJK/Hangul/Cyrillic (already in the project).
- **Icons.** Painted line icons in the same lighting (violet rim, slight warm highlight), 128 px source; no flat clip-art.
- **Hierarchy.** Money and income are the brightest UI elements; everything else stays darker so the studio art breathes.

## 8. Animation standards

- **Lights.**
  - Lamps: brightness ±8–12 %, a 0.3 s flicker every 18–45 s.
  - Neon: breathing ±15–20 % over 4–7 s, each strip with its own phase.
  - Never a strobe in the studio.
- **Screens.** DAW/visualiser loops synced to the BPM, subtle; MAKE BEAT gives a 1–1.5 s activity burst.
- **Characters.**
  - 2D cut-out rig, 30 fps updates; idle breathing on every character.
  - Performance poses blend over 0.1–0.2 s.
- **UI.** 150–250 ms ease-out moves, a 1.08 press punch, no bouncy overshoot over 10 %.
- **Concerts.** May use strobes (capped at 15 % alpha, max 2 per second) and camera shake (synced to the kick).

## 9. Texture resolution and export

| Asset | Size | Format |
|---|---|---|
| Studio background | **1080×1920** minimum (1440×2560 preferred), 9:16 | PNG, no transparency, sRGB |
| Studio foreground layer | same canvas as the background, cropped to the content | PNG with transparency |
| Character part | drawn on a 1000 px tall template | PNG with transparency, 20–40 px overlap |
| Portrait / poster | 1024×1024 | PNG |
| Venue background | 1080×1920 | PNG |
| Icons | 128×128 (256 for shop/hero items) | PNG with transparency |
| UI panels / 9-slice | 256×256 with a defined border | PNG with transparency |

- **Never upscale** art in the engine. If an image is smaller than the target, it must be redrawn or upscaled with a dedicated tool before import.
- **The current basement** is 864×1536 (below target): a 1080×1920 or 1440×2560 version is needed for sharp results on large phones.

## 10. Mobile optimisation rules

- **Texture settings.**
  - Backgrounds: no mipmaps, max size 2048, ASTC 6×6 (iOS/Android) or uncompressed only where banding shows.
  - Characters: one sprite atlas per artist.
- **Overdraw.** At most 2 full-screen layers (background + optional foreground); glows are small sprites, not full-screen.
- **Effects.** No post-processing; light animation = alpha/scale of glow sprites; screens = procedural meshes (see LaptopDawOverlay).
- **Memory budget.** About 120 MB of textures in memory at once (one studio + UI + one artist; concerts unload the studio art).
