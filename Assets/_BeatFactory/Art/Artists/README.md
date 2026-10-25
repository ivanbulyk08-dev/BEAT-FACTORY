# Detailed artists: how to deliver the art

One folder per artist: `Assets/_BeatFactory/Art/Artists/<id>/` (NØX = `nox`). The full plan, with sizes and the item list, is in `ARTIST_ART_PLAN.md` in the project root.

## Template

1. **One character template for everything.** Draw the artist and every clothing item on the same canvas (default 800×1050 px, character ~1000 px tall, feet near the bottom).
2. **Pose and view.** Front 3/4 view facing slightly left, arms hanging down, legs straight.
3. **Separate transparent PNGs.** Each body part and each clothing part is its own transparent PNG (straight alpha, sRGB).
4. **No baked effects.** No background, no drop shadow.
5. **Overlap at joints.** Parts overlap their neighbour by 20–40 px at the joints (shoulder, elbow, wrist, hip, ankle, neck).
6. **Placement.** Either export each layer at the full canvas size, or trim it and add an `offset` line to `rig.txt`. Trimmed exports use far less memory.

## File names

Body:
- `<id>_head_idle|rap|smile|proud|blink.png` (face only, no hair); `<id>_head_hype.png` is optional
- `<id>_neck`, `<id>_torso_base`
- `<id>_upperarm_near|far`, `<id>_forearm_near|far`
- `<id>_hand_mic|open|point|fist` (near hand), `<id>_hand_far`
- `<id>_leg_near|far`

Clothes: `<id>_<slot>_<item>_<part>.png`

| Slot | Parts |
|---|---|
| hair | back, front |
| top | torso (+ optional upperarm_near / upperarm_far sleeves) |
| jacket | torso, upperarm_near, upperarm_far, forearm_near, forearm_far, collar |
| pants | waist, leg_near, leg_far |
| shoes | near, far |
| chain | torso |
| glasses | head |
| rings | hand |

Thumbnail per item: `<id>_<slot>_<item>_thumb.png`, 256×256.

## rig.txt

```
canvas 800 1050
joint Feet 400 20            # every joint: Feet Hips Neck ShoulderNear ElbowNear WristNear ShoulderFar ElbowFar WristFar HipNear AnkleNear HipFar AnkleFar
offset nox_neck.png 370 750  # bottom-left of a trimmed PNG on the canvas (omit for full-canvas exports)
default jacket leather_black # default outfit (hair, top, pants, shoes required)
item jacket_leather_black | Leather Jacket | 0 | 0   # English name | studio level | price
```

Then run **Tools > Beat Factory > Art > Import Artist Rig**. The tool:
- sets the import settings and pivots;
- builds the rig and the wardrobe items;
- links the rig to the artist;
- lists missing or unrecognised files.

`PLACEHOLDER.txt` in a folder means its PNGs are generated stand-ins. Delete it when the real art is in.
