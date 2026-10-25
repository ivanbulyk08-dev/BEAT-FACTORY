using System;
using UnityEditor;
using UnityEngine;
using static BeatFactory.CharacterPose;
using Dir = BeatFactory.StudioCharacterAnchor.Direction;
using Layer = BeatFactory.StudioCharacterAnchor.Layer;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// The 10 studio rooms (Dark Rage style set, 941×1672 paintings in Art/Studios/studio_NN), one per studio level: live DAW
    /// on the painted monitors, animated lamps / neon over the painted lights, and the main character's spots (art px,
    /// top-left origin, read off each painting). In these rooms the character himself works at the desk on MAKE BEAT
    /// (workstation anchor); the painted chair stays empty.
    /// </summary>
    public static partial class BeatFactorySceneBuilder
    {
        const string StudiosArt = Root + "/Art/Studios/";

        static readonly Color Purple = new Color(0.7f, 0.35f, 1f);
        static readonly Color Warm = new Color(1f, 0.62f, 0.25f);
        static readonly Color Red = new Color(1f, 0.16f, 0.22f);
        static readonly Color Blue = new Color(0.45f, 0.42f, 1f);
        static readonly Color Magenta = new Color(1f, 0.3f, 0.9f);

        struct RoomDef
        {
            public string name, art;
            public Action<RectTransform> live;
            public Spot[] spots;
            public RoomDef(string name, string art, Action<RectTransform> live, params Spot[] spots) { this.name = name; this.art = art; this.live = live; this.spots = spots; }
        }

        static RoomDef[] StudioRooms() => new[]
        {
            new RoomDef("Basement", "studio_01/v3/Basement.png", rt =>
            {
                Screen(rt, "MonitorDAW", 388, 607, 532, 690);
                Lamp(rt, "BulbCeiling", 645, 200, 620, 320, 0.16f, 6f);
                Lamp(rt, "Bulb", 645, 268, 150, 190, 0.5f, 4f);
                Lamp(rt, "DeskLamp", 640, 602, 130, 130, 0.45f, 4.5f);
                Lamp(rt, "Candle", 57, 600, 60, 70, 0.5f, 1.6f);
                Neon(rt, "WindowNeon", Purple, 460, 333, 370, 28, 0f, 5.5f);
                Neon(rt, "WallTube", Purple, 143, 540, 24, 290, 0f, 4.6f);
                Ambient(rt, "StairsLight", Blue, 870, 380, 220, 340, 6.5f);
            },
                new Spot("Desk", 615, 905, 0.52f, WorkPoses, Dir.Left, work: true),
                new Spot("Stairs", 770, 950, 0.54f, StandPoses, Dir.Left),
                new Spot("Rug", 540, 1135, 0.7f, StandPoses),
                new Spot("Bed", 165, 850, 0.52f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 430, 1170, 0.71f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Bedroom", "studio_02/v3/Bedroom.png", rt =>
            {
                Screen(rt, "MonitorDAW", 420, 528, 550, 612);
                Lamp(rt, "CeilingGlow", 340, 140, 500, 260, 0.15f, 7f);
                Lamp(rt, "CeilingLamp", 340, 95, 160, 90, 0.5f, 5f);
                Lamp(rt, "DeskLamp", 668, 540, 110, 110, 0.5f, 4.4f);
                Lamp(rt, "Candle1", 290, 605, 50, 60, 0.5f, 1.7f);
                Lamp(rt, "Candle2", 75, 225, 50, 60, 0.5f, 1.9f);
                Lamp(rt, "Candle3", 45, 570, 50, 60, 0.5f, 1.5f);
                Neon(rt, "WindowNeon", Purple, 322, 430, 22, 260, 0f, 5.2f);
                Neon(rt, "ShelfLed", Purple, 100, 190, 220, 24, -25f, 6.1f);
                Neon(rt, "MirrorNeon", Purple, 900, 640, 20, 420, -8f, 4.8f);
            },
                new Spot("Desk", 380, 885, 0.52f, WorkPoses, Dir.Right, work: true),
                new Spot("Mirror", 740, 960, 0.57f, StandPoses, Dir.Left),
                new Spot("Rug", 560, 1120, 0.7f, StandPoses),
                new Spot("Bed", 190, 830, 0.52f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 470, 1160, 0.71f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("SmallPro", "studio_03/v3/SmallPro.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 322, 527, 447, 606);
                Screen(rt, "MonitorRightDAW", 452, 527, 578, 606);
                Neon(rt, "CoveLed", Purple, 470, 205, 560, 24, 0f, 5.5f);
                Neon(rt, "WallTube", Purple, 203, 470, 18, 260, 0f, 4.7f);
                Neon(rt, "ShelfLed", Warm, 70, 250, 200, 22, 20f, 6.2f);
                Lamp(rt, "Spot1", 255, 65, 60, 50, 0.45f, 5f);
                Lamp(rt, "Spot2", 710, 65, 60, 50, 0.45f, 5.6f);
                Lamp(rt, "DeskLamp", 685, 585, 100, 100, 0.45f, 4.2f);
                Ambient(rt, "WindowCity", Purple, 900, 500, 150, 300, 6f);
            },
                new Spot("Desk", 680, 880, 0.52f, WorkPoses, Dir.Left, work: true),
                new Spot("Mic", 245, 905, 0.54f, StandPoses),
                new Spot("Rug", 470, 1140, 0.71f, StandPoses),
                new Spot("Sofa", 800, 875, 0.54f, SeatPoses, Dir.Left, Layer.Front),
                new Spot("RugFloor", 400, 1180, 0.74f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Professional", "studio_04/v3/Professional.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 437, 507, 552, 582);
                Screen(rt, "MonitorRightDAW", 560, 507, 675, 582);
                Neon(rt, "BoothLeft", Red, 78, 500, 20, 400, 0f, 4.4f);
                Neon(rt, "BoothRight", Red, 243, 480, 20, 440, 0f, 5.7f);
                Neon(rt, "CeilingLed", Red, 590, 172, 480, 22, -2f, 6.1f);
                Neon(rt, "CeilingDiag", Red, 170, 85, 380, 22, 28f, 5.2f);
                Ambient(rt, "PosterFrame", Red, 870, 400, 140, 340, 5f);
                Lamp(rt, "Candle1", 100, 900, 80, 90, 0.5f, 1.6f);
                Lamp(rt, "Candle2", 300, 585, 50, 60, 0.5f, 1.8f);
                Lamp(rt, "Spot1", 295, 95, 50, 40, 0.45f, 5f);
                Lamp(rt, "Spot2", 370, 155, 50, 40, 0.45f, 5.6f);
                Lamp(rt, "Spot3", 765, 70, 50, 40, 0.45f, 4.7f);
            },
                new Spot("Desk", 720, 860, 0.52f, WorkPoses, Dir.Left, work: true),
                new Spot("Rug", 560, 1130, 0.74f, StandPoses),
                new Spot("Sofa", 180, 820, 0.54f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 460, 1170, 0.77f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Modern", "studio_05/v3/Modern.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 268, 556, 362, 618);
                Screen(rt, "MonitorRightDAW", 366, 556, 460, 618);
                Lamp(rt, "DeskLamp", 85, 565, 100, 100, 0.45f, 4.4f);
                Lamp(rt, "Candle1", 125, 630, 50, 60, 0.5f, 1.6f);
                Lamp(rt, "Candle2", 200, 950, 80, 90, 0.5f, 1.8f);
                Lamp(rt, "Candle3", 770, 720, 50, 60, 0.5f, 1.5f);
                Neon(rt, "MezzLed", Warm, 250, 272, 500, 20, 4f, 5.8f);
                Neon(rt, "WallTube", Purple, 80, 450, 20, 220, 0f, 5.1f);
                Ambient(rt, "StairLights", Warm, 720, 500, 260, 300, 4.3f);
                Ambient(rt, "WindowCity", Purple, 850, 400, 200, 400, 6.4f);
            },
                new Spot("Desk", 520, 790, 0.48f, WorkPoses, Dir.Left, work: true),
                new Spot("Stairs", 650, 800, 0.49f, StandPoses, Dir.Left),
                new Spot("Rug", 600, 1080, 0.7f, StandPoses),
                new Spot("Sofa", 300, 850, 0.52f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 520, 1190, 0.74f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Luxury", "studio_06/Luxury.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 378, 588, 468, 636);
                Screen(rt, "MonitorMidDAW", 470, 588, 560, 636);
                Screen(rt, "MonitorRightDAW", 562, 588, 652, 636);
                Ambient(rt, "ChandelierHalo", Purple, 530, 200, 700, 400, 7f);
                Neon(rt, "Chandelier", Purple, 530, 100, 420, 200, 0f, 5.5f, sliced: false);
                Lamp(rt, "DeskLamp", 305, 630, 90, 90, 0.45f, 4.6f);
                Lamp(rt, "Candle1", 930, 830, 50, 60, 0.5f, 1.6f);
                Lamp(rt, "Candle2", 820, 880, 50, 60, 0.5f, 1.8f);
                Lamp(rt, "Candle3", 210, 1090, 70, 80, 0.5f, 1.5f);
                Neon(rt, "BarNeon", Purple, 880, 622, 140, 26, -10f, 5.1f);
                Ambient(rt, "Plaques", Warm, 110, 500, 260, 400, 6.6f);
            },
                new Spot("Desk", 600, 820, 0.52f, WorkPoses, Dir.Left, work: true),
                new Spot("Bar", 760, 900, 0.57f, StandPoses, Dir.Left),
                new Spot("Rug", 520, 1120, 0.74f, StandPoses),
                new Spot("Sofa", 260, 890, 0.54f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 600, 1230, 0.78f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Major", "studio_07/Major.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 252, 537, 355, 596);
                Screen(rt, "MonitorMidDAW", 359, 537, 482, 596);
                Screen(rt, "MonitorRightDAW", 486, 537, 566, 596);
                Ambient(rt, "Console", Warm, 430, 650, 760, 140, 4.4f);
                Ambient(rt, "LiveRoom", Purple, 370, 450, 600, 220, 6f);
                Neon(rt, "WallLed", Purple, 360, 330, 640, 20, 2f, 5.5f);
                Neon(rt, "CeilingLed", Blue, 400, 110, 420, 20, 7f, 6.2f);
                Lamp(rt, "Candle1", 35, 630, 50, 60, 0.5f, 1.6f);
                Lamp(rt, "Candle2", 140, 1410, 80, 90, 0.5f, 1.8f);
                Lamp(rt, "Candle3", 910, 690, 50, 60, 0.5f, 1.5f);
            },
                new Spot("Desk", 620, 820, 0.5f, WorkPoses, Dir.Left, work: true),
                new Spot("Side", 760, 860, 0.52f, StandPoses, Dir.Left),
                new Spot("Rug", 560, 1060, 0.7f, StandPoses),
                new Spot("Sofa", 210, 1000, 0.61f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 480, 1160, 0.74f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("Elite", "studio_08/Elite.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 378, 582, 470, 640);
                Screen(rt, "MonitorRightDAW", 475, 582, 575, 640);
                Neon(rt, "FloorRing", Purple, 450, 840, 560, 110, 0f, 5f, sliced: false);
                Neon(rt, "StarChandelier", Purple, 490, 100, 260, 200, 0f, 4.6f, sliced: false);
                Ambient(rt, "Holo1", Purple, 300, 560, 140, 90, 3.8f);
                Ambient(rt, "Holo2", Purple, 640, 530, 200, 110, 4.2f);
                Neon(rt, "TableRing", Purple, 500, 1180, 460, 110, 0f, 5.6f, sliced: false);
                Lamp(rt, "Candle", 420, 1080, 60, 70, 0.5f, 1.6f);
                Lamp(rt, "CabinetLed", 60, 460, 140, 40, 0.4f, 5f);
            },
                new Spot("Desk", 580, 850, 0.5f, WorkPoses, Dir.Left, work: true),
                new Spot("Ring", 300, 850, 0.5f, StandPoses),
                new Spot("Window", 760, 785, 0.48f, WindowPoses),
                new Spot("SofaLeft", 230, 1060, 0.63f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("SofaRight", 720, 1060, 0.63f, SeatPoses, Dir.Left, Layer.Front)),

            new RoomDef("RecordLabel", "studio_09/RecordLabel.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 302, 540, 398, 606);
                Screen(rt, "MonitorMidDAW", 402, 540, 486, 606);
                Screen(rt, "MonitorRightDAW", 489, 540, 574, 606);
                Neon(rt, "BigStar", Purple, 340, 330, 300, 340, 0f, 5.5f, sliced: false);
                Lamp(rt, "DeskLamp", 690, 600, 100, 100, 0.45f, 4.6f);
                Lamp(rt, "Candle", 840, 1150, 100, 110, 0.5f, 1.6f);
                Ambient(rt, "ShelfLeft", Warm, 30, 300, 60, 500, 6.2f);
                Neon(rt, "CeilingLed", Purple, 550, 90, 300, 20, -8f, 5.8f);
                Ambient(rt, "GlassRoom", Purple, 800, 450, 280, 300, 6.4f);
                Neon(rt, "UnderDesk", Warm, 450, 830, 600, 26, 0f, 5.1f);
            },
                new Spot("Desk", 600, 860, 0.52f, WorkPoses, Dir.Left, work: true),
                new Spot("DeskLeft", 240, 870, 0.54f, StandPoses),
                new Spot("Rug", 380, 1050, 0.68f, StandPoses),
                new Spot("Sofa", 760, 920, 0.57f, SeatPoses, Dir.Left, Layer.Front),
                new Spot("RugFloor", 300, 1150, 0.74f, FloorPoses, Dir.Right, Layer.Front)),

            new RoomDef("MegaComplex", "studio_10/MegaComplex.png", rt =>
            {
                Screen(rt, "MonitorLeftDAW", 312, 667, 405, 724);
                Screen(rt, "MonitorMidDAW", 418, 667, 535, 724);
                Screen(rt, "MonitorRightDAW", 540, 667, 658, 724);
                Neon(rt, "StageScreens", Magenta, 465, 490, 360, 200, 0f, 3.6f, sliced: false);
                Ambient(rt, "Arena", Purple, 470, 600, 700, 150, 5.4f);
                Neon(rt, "PlatformRing", Purple, 450, 905, 380, 80, 0f, 5f, sliced: false);
                Neon(rt, "CeilingStar", Purple, 480, 80, 200, 160, 0f, 4.4f, sliced: false);
                Lamp(rt, "Candle1", 340, 1050, 60, 70, 0.5f, 1.6f);
                Lamp(rt, "Candle2", 490, 1150, 60, 70, 0.5f, 1.8f);
                Ambient(rt, "Plaques", Warm, 40, 400, 120, 500, 6.6f);
            },
                new Spot("Desk", 560, 905, 0.44f, WorkPoses, Dir.Left, work: true),
                new Spot("Platform", 340, 915, 0.44f, StandPoses),
                new Spot("Walkway", 700, 1040, 0.52f, StandPoses, Dir.Left),
                new Spot("Sofa", 150, 1130, 0.61f, SeatPoses, Dir.Right, Layer.Front),
                new Spot("RugFloor", 620, 1240, 0.71f, FloorPoses, Dir.Right, Layer.Front)),
        };

        struct DeskSeat
        {
            public float x, y, scale; public bool left;
            public DeskSeat(float x, float y, float scale, bool left) { this.x = x; this.y = y; this.scale = scale; this.left = left; }
        }

        /// <summary>Rooms 3–10: where the desk producer sits on the painted chair (art px, top-left), his size and which way he works.</summary>
        static readonly System.Collections.Generic.Dictionary<string, DeskSeat> DeskSeats = new System.Collections.Generic.Dictionary<string, DeskSeat>
        {
            { "SmallPro", new DeskSeat(460, 785, 0.23f, true) },
            { "Professional", new DeskSeat(545, 745, 0.23f, false) },
            { "Modern", new DeskSeat(445, 750, 0.216f, true) },
            { "Luxury", new DeskSeat(480, 765, 0.23f, true) },
            { "Major", new DeskSeat(495, 750, 0.223f, false) },
            { "Elite", new DeskSeat(470, 740, 0.223f, true) },
            { "RecordLabel", new DeskSeat(490, 750, 0.23f, false) },
            { "MegaComplex", new DeskSeat(465, 840, 0.196f, true) },
        };

        /// <summary>The producer working at the desk (from the user's pictures), seat on the chair, breathing + bobbing to the beat.</summary>
        static void DeskProducer(RectTransform rt, DeskSeat seat)
        {
            var art = Art.DeskProducerArt.Get(seat.left);
            if (art.sprite == null) return;
            var img = UIFactory.Image("DeskProducer", rt, new Color(0.96f, 0.93f, 1f));
            img.sprite = art.sprite;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = ArtUV(seat.x, seat.y);
            r.pivot = art.pivot;
            r.anchoredPosition = Vector2.zero;
            var keep = img.gameObject.AddComponent<ArtPixelSize>();
            keep.sizeInArtPixels = new Vector2(art.sprite.rect.width, art.sprite.rect.height) * seat.scale;
            keep.artSize = new Vector2(ArtW, ArtH);
            var view = img.gameObject.AddComponent<SeatedProducerView>();
            UIFactory.SetRef(view, "body", r);
        }

        /// <summary>All studio rooms + the room switcher + the main character, behind the UI.</summary>
        static RectTransform CreateBasementBackdrop(Transform canvas)
        {
            var holder = UIFactory.Rect("StudioBackdrop", canvas);
            UIFactory.Stretch(holder);
            var rooms = new System.Collections.Generic.List<UnityEngine.Object>();
            var levels = new System.Collections.Generic.List<int>();
            var defs = StudioRooms();
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                var path = StudiosArt + d.art;
                var sprite = ImportArt(path, 2048);
                if (sprite == null) { Debug.LogWarning("Room art missing: " + path); continue; }
                ArtW = sprite.texture.width; ArtH = sprite.texture.height;
                var rt = Room(holder, d.name, sprite);
                d.live(rt);
                var spots = d.spots;
                if (DeskSeats.TryGetValue(d.name, out var seat))
                {
                    // a producer sits at the desk here all the time: the main character no longer goes to the desk
                    DeskProducer(rt, seat);
                    spots = System.Array.FindAll(spots, sp => !sp.work);
                }
                CharacterSpots(rt, null, spots);
                rt.gameObject.SetActive(i == 0);
                rooms.Add(rt.gameObject);
                levels.Add(i);
            }
            if (rooms.Count == 0) return holder;

            var switcher = holder.gameObject.AddComponent<StudioBackdropSwitcher>();
            var so = new SerializedObject(switcher);
            UIFactory.SetArray(so.FindProperty("rooms"), rooms.ToArray());
            var lv = so.FindProperty("levels");
            lv.arraySize = levels.Count;
            for (int i = 0; i < levels.Count; i++) lv.GetArrayElementAtIndex(i).intValue = levels[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            CreateMainCharacter(holder, switcher);
            return holder;
        }

        // ---- live layers (art px, top-left origin)

        /// <summary>Live DAW on a painted monitor (axis-aligned screen rectangle).</summary>
        static void Screen(RectTransform rt, string name, float x0, float y0, float x1, float y1)
        {
            var d = UIFactory.Rect(name, rt);
            UIFactory.Stretch(d);
            d.gameObject.AddComponent<CanvasRenderer>();
            var o = d.gameObject.AddComponent<LaptopDawOverlay>();
            o.topLeft = ArtUV(x0, y0); o.topRight = ArtUV(x1, y0);
            o.bottomRight = ArtUV(x1, y1); o.bottomLeft = ArtUV(x0, y1);
            o.opacity = 0.93f;
            o.raycastTarget = false;
        }

        /// <summary>Warm bulb / candle / lamp: drift + rare flicker.</summary>
        static void Lamp(RectTransform rt, string name, float x, float y, float w, float h, float alpha, float period) =>
            Glow(rt, name, UIStyle.RadialSprite, Warm, ArtUV(x, y), new Vector2(w, h), 0f, StudioLightGlow.Kind.Lamp, alpha, 0.12f, period);

        /// <summary>LED strip / neon tube breathing (sliced soft bar), or a neon shape (radial) with sliced: false.</summary>
        static void Neon(RectTransform rt, string name, Color c, float x, float y, float w, float h, float angle, float period, bool sliced = true) =>
            Glow(rt, name, sliced ? UIStyle.SoftGlowSprite : UIStyle.RadialSprite, c, ArtUV(x, y), new Vector2(w, h), angle,
                StudioLightGlow.Kind.Neon, sliced ? 0.5f : 0.3f, 0.18f, period, sliced: sliced);

        /// <summary>Large soft coloured light area (window city glow, console spill).</summary>
        static void Ambient(RectTransform rt, string name, Color c, float x, float y, float w, float h, float period) =>
            Glow(rt, name, UIStyle.RadialSprite, c, ArtUV(x, y), new Vector2(w, h), 0f, StudioLightGlow.Kind.Neon, 0.15f, 0.2f, period);
    }
}
