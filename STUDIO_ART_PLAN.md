# Beat Factory — Studio Art Plan (10 portrait studios)

Style rules: `ART_STYLE_GUIDE.md`. Reference quality: `Art/Studios/studio_01/Basement_Main.png`.
Studio data (names, prices, bonuses): `Data/Studios/*.asset`. Prices and the economy are **not** changed by this plan.

## Current state

- **Code art.** Studios 2–10 are drawn by code (`Scripts/Editor/Art/ArtGenerator.Rooms.cs` / `Environments.cs`), flat style, 1000×860 landscape cards
  (`Art/Environments/<Studio>/env_*.png`), shown inside the small STUDIO card (`StudioSceneView`), which is hidden on HOME now.
- **Basement.** Only the basement has the new full-screen painted art (`BeatFactorySceneBuilder.Basement.cs`, built into the scene).
  It shows the basement **whatever studio level the player has**. It still needs to switch per studio level (see "Code" below).

## Per-studio art package (same for all 10)

All images are 9:16 portrait, **1440×2560** (min 1080×1920). Composition reserves the top 12 % for the HUD and the bottom 22 % for MAKE BEAT and navigation.

| File | Content |
|---|---|
| `studio_NN/<Name>_Main.png` | full background, flattened, including painted light sources |
| `studio_NN/<Name>_Foreground.png` (optional) | front objects on transparency (table, couch arm), for slight parallax |
| `studio_NN/<Name>_Screens.json` (I write it) | four corners of every animatable screen (laptop, monitors, TV, LED wall) |
| `studio_NN/<Name>_Lights.json` (I write it) | position, size, colour and type (lamp / neon / screen spill) of every light to animate |
| `studio_NN/<Name>_Clean_Screens.png` (optional) | the same background with the screens painted dark/empty, so the live DAW can replace them fully instead of covering them |

The JSON files are made by me from the delivered image (as for the basement); no extra work for the artist.

## The 10 studios

| # | Studio | Architecture | Key gear | Light (warm key / neon) | Animated |
|---|---|---|---|---|---|
| 1 | Basement ✅ art delivered | concrete, pipes, stairs, window to the city | laptop + MIDI keys, 2 monitors, mic on a stand, crates | hanging bulb / purple LED strips | laptop DAW, lamp, neon ×3, fridge |
| 2 | Bedroom Studio | small apartment bedroom, slanted ceiling, window with city at dusk | desktop PC + 1 monitor, MIDI keys, budget monitors, foam panels | desk lamp / LED strip behind the monitor | monitor DAW, lamp, LED strip, string lights |
| 3 | Small Pro Studio | rented room, acoustic panels, glass to a small vocal booth | 2 monitors, controller, audio interface, booth mic | ceiling spots / violet wall wash | 2 screens, booth light, VU meters |
| 4 | Professional Studio | control room with a mixing console and booth window | analogue-style console, 3 monitors, studio monitors on stands | console lamps / purple soffit | console meters, 3 screens, booth glow |
| 5 | Modern Studio | loft with brick walls, big windows, plants | 49" ultrawide, Push-style pad controller, synth wall | warm practicals / neon sign | ultrawide DAW, synth LEDs, neon sign |
| 6 | Luxury Studio | penthouse, glass walls over the night skyline, marble | large console, gold records, lounge sofa | warm cove light / violet LED ceiling | console, skyline lights, fireplace |
| 7 | Major Studio | big label room, wood diffusers, live room behind glass | SSL-style console, outboard racks, grand piano in the live room | wood warm light / purple accent | console + racks LEDs, live room lights |
| 8 | Elite Studio | dark sci-fi room, curved walls, holographic panels | curved screen wall, modular synth wall | amber floor light / violet holograms | screen wall, modular cable LEDs, hologram |
| 9 | Record Label Studio | top-floor HQ, label logo wall, award shelf, view over the city | double console, wall of screens with charts | gold accent light / purple logo backlight | screens wall, logo glow, city |
| 10 | Mega Music Complex | huge control room over a live stage / arena | giant console, LED wall, view over a rehearsal stage | stage-style spots / violet LED wall | LED wall, stage lights below, console |

**Visual progression:**
- Each studio is a clear step up in cleanliness, space and gear. The darkness, the violet/orange palette and the line style stay the same.
- The first three keep "lived-in clutter" (cans, posters, crates); the later ones become sleek but never sterile.

## Rules for the artist (studios)

1. **Draw each studio as its own image.** Don't crop studios from collages, and don't reuse one room with different props.
2. **Light sources.**
   - Paint the light sources on; the game only adds subtle animated glow on top.
   - Avoid huge bloom so the animation stays believable.
3. **Screens.**
   - Screens may contain a painted UI, but a version with dark screens is better (`_Clean_Screens.png`).
   - Screens must face the camera enough to read a waveform (no extreme angles).
4. **No people** in the background images. Producers and the player are added by the game later; a painted chair is fine.
5. **Composition.** Keep the workstation in the middle third, nothing important in the top 12 % / bottom 22 %.

## Code (after approval; not done now)

1. **Studio art data.** `StudioData` gets an optional `mainArt` sprite + screens/lights data. `StudioSceneView`'s HOME backdrop switches per
   studio level, falling back to the basement until an image exists. Files: `Scripts/Studio/StudioData.cs`,
   `Scripts/Editor/BeatFactorySceneBuilder.Basement.cs` (generalised to any studio).
2. **Reusable layers.** `LaptopDawOverlay` (screens) and `StudioLightGlow` (lamps/neon) are already generic; they are spawned from the per-studio data.
3. **Studio upgrade transition.** The old room fades out and the new room fades in, with a light sweep; reuses `StudioUpgradePopup`.
4. **Producers in the room (Phase 4 dependency).** The animated producers stand at positions defined per studio.

**Complexity:**
- Code: medium (2–3 days).
- Art: 9 new paintings plus a high-res basement — the main cost.

**Definition of done (per studio):**
- the art is imported;
- it covers 9:16 to 9:21 screens without stretching;
- the screens are animated and aligned;
- lights animate;
- MAKE BEAT and the HUD don't cover the workstation;
- tested on a 1080p and a 1440p phone at 60 fps.
