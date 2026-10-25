# Beat Factory — Concert Art Plan

Style: `ART_STYLE_GUIDE.md`. Existing system: 2D UI stage (`Scripts/Concerts/ConcertStageView.cs`, `ConcertCrowd.cs`,
`StageLightShow.cs`, `PerformerRig.cs` / `LayeredPerformer.cs`, `StadiumFlyover.cs`, `StandsLights.cs`).

## What exists (keep)

- **Venues.** 5 tiers: Club, Small Venue, Concert Hall, Arena, Stadium (`VenueData.cs`); capacities and rewards are unchanged.
- **Show.** The director follows the music sections; camera styles per tier; lasers, moving heads, haze, pyro, sparks, confetti/fireworks.
- **Crowd.** About 60–3,600 mesh-drawn fans: varied bodies/hair/skin/clothes, moves (jump, dance, phone, headbang, mosh), signs, phone lights, moshpits.
- **Stadium.** Drone intro (skippable) + stadium wristband lights.
- **Detailed performer.** NØX (layered rig).
- **Festival.** **No Festival tier** exists; adding one would change progression and needs approval.

## What changes visually

| Element | Now | Target | Asset |
|---|---|---|---|
| Venue backgrounds | procedural flat (`Art/Venues/venue_0..4.png`, 800×1400) | painted, 1440×2560, same style as the basement; one per tier | 5 paintings |
| Venue foreground (stage edge, barrier, speakers) | part of the background | separate transparent layer so the crowd can sit between stage and camera | 5 PNG |
| LED screen area | painted rectangle | the same, with its 4 corners documented (`VenueScreens`) | — |
| Crowd near rows | procedural atlas (flat) | painted fan parts in the new style: 12 bodies, 16 hair, 6 heads, arms, phones, signs; same atlas layout (`CrowdAtlas.cs`, 1024×512 → 2048×1024) | 1 atlas |
| Crowd far rows | blobs | painted "crowd strip" textures (3 densities) tiled under the LOD system | 3 PNG |
| Stadium aerial | procedural (`stadium_aerial.png`) | painted night stadium, 2048×1120, same layout points | 1 painting |
| Effects | procedural sprites (`Art/Effects`) | painted beam, laser, flame, spark, smoke, confetti in the new style | ≈10 PNG |
| Performers | flat procedural + NØX | see CHARACTER_ART_PLAN.md | — |

## Per-tier art direction

| Tier | Room | Light | Crowd |
|---|---|---|---|
| Club | brick basement club, low ceiling, small DJ riser, graffiti | red / purple, one warm bulb | 30–260, close, detailed |
| Small Venue | black-box room, truss, small LED wall | violet wash + white spots | 120–600 |
| Concert Hall | theatre with balconies, curtains | warm house lights → coloured spots | 300–1,500, balconies |
| Arena | indoor arena, big LED wall, side screens | lasers, moving heads | 900–3,000 + painted stands |
| Stadium | open-air at night, huge stage, stands | everything + fireworks | 1,800–3,600 near + stands texture + wristbands |

## Performance

- **Crowd budget.** Keep the existing per-tier caps and the quality settings (`ConcertQualityLevel`).
- **Draw calls.** The painted crowd atlas keeps the 3-layer mesh approach (3 draw calls for thousands of fans).
- **Memory.** Unload the studio backdrop while the concert overlay is open (≈20 MB at 1440p RGBA). Venue backgrounds load per show.
- **Particles.** No particle systems; pooled UI images as now.

## Code (after approval)

1. **Assets per tier.** Swap the venue sprites (no code beyond import settings).
2. **Foreground layer.** Add a venue foreground layer above the crowd in `ConcertStageView.Build` (one image, sibling order).
3. **Crowd art.** `CrowdAtlas`: support a 2048 atlas with the same cell layout; the colour tint becomes optional (painted fans keep their colours, with a light tint only for stage light).
4. **Stadium aerial.** Load the painted aerial instead of generating it (the importer keeps the layout points).

**Complexity:**
- Code: low-medium (1–2 days).
- Art: high (5 venues + aerial + atlas + effects).

**Tests:** the 5 tiers, screenshots at intro / verse / drop / big moment, 60 fps on a mid-range Android with the HIGH concert quality, and a memory check.
