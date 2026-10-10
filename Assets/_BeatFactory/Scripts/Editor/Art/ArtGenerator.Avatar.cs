using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BeatFactory.EditorTools.Art.SdfPainter;

namespace BeatFactory.EditorTools.Art
{
    /// <summary>
    /// Layered player avatar for the customization system: every hairstyle, top, pants, sneaker, accessory and tattoo
    /// is its own transparent layer on a shared canvas (head 160², body 200×190, arm 56×140), plus menu thumbnails.
    /// Output: Art/Characters/Player/Layers, Art/Characters/Player/Thumbs and Data/AvatarLibrary.asset.
    /// </summary>
    public static partial class ArtGenerator
    {
        public const string LibraryPath = BeatFactorySceneBuilder.Root + "/Data/AvatarLibrary.asset";
        static string LayerPath(string name) => $"{Chars}/Player/Layers/{name}.png";
        static string ThumbPath(string key) => $"{Chars}/Player/Thumbs/{key}.png";

        /// <summary>Neutral player: same face and skin, no clothes-specific extras.</summary>
        static Look AvatarBase()
        {
            var l = Cast[0].Clone();
            l.chainThin = l.chainCuban = l.watchSmart = l.watchGold = l.rings = l.handTattoo = l.armTattoo = l.neckTattoo = false;
            l.phonesOnHead = l.phonesOnNeck = l.glasses = l.sunglasses = l.earStud = l.earHoop = l.faceDots = l.faceStar = false;
            l.cap = l.beanie = Color.clear;
            l.portraitExpression = "idle";
            return l;
        }

        static Look HairLook(int i)
        {
            var l = AvatarBase();
            switch (i)
            {
                case 0: l.hairStyle = HairStyle.DreadsHalfUp; l.hair = Hex("17110F"); l.hairTip = Hex("9B5CFF"); l.metal = Hex("D9DEE8"); break;
                case 1: l.hairStyle = HairStyle.DreadsTied; l.hair = Hex("17110F"); l.hairTip = Hex("F2C14E"); l.metal = Hex("F2C14E"); break;
                case 2: l.hairStyle = HairStyle.Braids; l.hair = Hex("120D0C"); l.hairTip = Hex("3A2A24"); l.metal = Hex("F2C14E"); break;
                case 3: l.hairStyle = HairStyle.CurlyFade; l.hair = Hex("17110F"); l.hairTip = Hex("3A2C27"); break;
                case 4: l.hairStyle = HairStyle.LongStraight; l.hair = Hex("1A1426"); l.hairTip = Hex("FF3DC8"); break;
                case 5: l.hairStyle = HairStyle.Messy; l.hair = Hex("231813"); l.hairTip = Hex("4A3428"); break;
                default: l.hairStyle = HairStyle.Bob; l.hair = Hex("B48CFF"); l.hairTip = Hex("FF7BC8"); break;
            }
            return l;
        }

        static Look TopLook(int i)
        {
            var l = AvatarBase();
            switch (i)
            {
                case 0: l.top = TopStyle.Hoodie; l.cloth = Hex("4A4752"); l.clothShade = Hex("34313B"); l.accent = Hex("8A84B0"); break;
                case 1: l.top = TopStyle.Hoodie; l.cloth = Hex("6C3BD9"); l.clothShade = Hex("4A2799"); l.accent = Hex("FF3DC8"); break;
                case 2: l.top = TopStyle.Tee; l.cloth = Hex("1B1B20"); l.clothShade = Hex("101014"); l.accent = Hex("3DE1FF"); break;
                case 3: l.top = TopStyle.Bomber; l.cloth = Hex("1E1B2A"); l.clothShade = Hex("13111C"); l.accent = Hex("B65CFF"); break;
                case 4: l.top = TopStyle.Tracksuit; l.cloth = Hex("C2183A"); l.clothShade = Hex("8E1029"); l.accent = Hex("F4F4F6"); break;
                default: l.top = TopStyle.Puffer; l.cloth = Hex("17161D"); l.clothShade = Hex("0D0C12"); l.accent = Hex("F2C14E"); break;
            }
            return l;
        }

        static Look PantsLook(int i)
        {
            var l = AvatarBase();
            switch (i)
            {
                case 0: l.pantsStyle = PantsStyle.Denim; l.pants = Hex("2E3A52"); l.pantsShade = Hex("222B3D"); break;
                case 1: l.pantsStyle = PantsStyle.Jogger; l.pants = Hex("22202C"); l.pantsShade = Hex("17151F"); l.accent = Hex("FF3DC8"); break;
                case 2: l.pantsStyle = PantsStyle.Cargo; l.pants = Hex("4A4F2E"); l.pantsShade = Hex("363A21"); l.accent = Hex("FF8A3D"); break;
                default: l.pantsStyle = PantsStyle.Wide; l.pants = Hex("3A3646"); l.pantsShade = Hex("2A2734"); break;
            }
            l.sneaker = SneakerStyle.LowRetro;
            return l;
        }

        static Look SneakerLook(int i)
        {
            var l = AvatarBase();
            switch (i)
            {
                case 0: l.sneaker = SneakerStyle.LowRetro; l.shoe = Hex("C9C6CF"); l.shoeAccent = Hex("5A5766"); l.sole = Hex("D9D2C2"); break;
                case 1: l.sneaker = SneakerStyle.HighTop; l.shoe = Hex("F2F2F5"); l.shoeAccent = Hex("3DE1FF"); l.sole = Hex("F4F4F6"); break;
                case 2: l.sneaker = SneakerStyle.Minimal; l.shoe = Hex("F7F5F0"); l.shoeAccent = Hex("F2C14E"); l.sole = Hex("EDE6D8"); break;
                default: l.sneaker = SneakerStyle.Chunky; l.shoe = Hex("121117"); l.shoeAccent = Hex("F4F4F6"); l.sole = Hex("F4F4F6"); break;
            }
            return l;
        }

        static Look With(Action<Look> setup)
        {
            var l = AvatarBase();
            setup(l);
            return l;
        }

        static SdfPainter HeadLayer(Look l, string expr, HeadPart part)
        {
            var p = new SdfPainter(160, 160);
            DrawHead(p, l, expr, new Tf(Vector2.zero, 1f), true, part);
            return p;
        }

        static SdfPainter ShoeThumb(Look l)
        {
            var p = new SdfPainter(130, 90);
            Sneaker(p, l, 22, 12, 82, far: false);
            return p;
        }

        static Sprite MakeSprite(string path, bool overwrite, Func<SdfPainter> draw)
        {
            if (overwrite || !File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                draw().Save(path);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Tools/Beat Factory/Art/Regenerate Avatar Layers")]
        static void AvatarMenu() => EnsureAvatarLibrary(true);

        public static AvatarLibrary EnsureAvatarLibrary(bool overwrite)
        {
            var lib = AssetDatabase.LoadAssetAtPath<AvatarLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<AvatarLibrary>();
                BeatFactorySceneBuilder.EnsureFolderPublic(Path.GetDirectoryName(LibraryPath).Replace('\\', '/'));
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            var b = AvatarBase();
            Sprite S(string name, Func<SdfPainter> draw) => MakeSprite(LayerPath(name), overwrite, draw);

            // head
            lib.faces = new Sprite[Expressions.Length];
            for (int i = 0; i < Expressions.Length; i++)
            {
                string e = Expressions[i];
                lib.faces[i] = S("face_" + e, () => HeadLayer(b, e, HeadPart.Face));
            }
            int hairCount = AvatarCatalog.HairNames.Length;
            lib.hairBack = new Sprite[hairCount];
            lib.hairFront = new Sprite[hairCount];
            for (int i = 0; i < hairCount; i++)
            {
                var hl = HairLook(i);
                lib.hairBack[i] = S("hair_back_" + i, () => HeadLayer(hl, "idle", HeadPart.HairBack));
                lib.hairFront[i] = S("hair_front_" + i, () => HeadLayer(hl, "idle", HeadPart.HairFront));
            }
            Look capL = With(l => l.cap = Hex("C2183A")), beanieL = With(l => l.beanie = Hex("6C3BD9")), phonesL = With(l => l.phonesOnHead = true);
            Look roundL = With(l => l.glasses = true), shadesL = With(l => { l.sunglasses = true; l.metal = Hex("F2C14E"); });
            Look earL = With(l => l.earStud = true), faceTatL = With(l => l.faceDots = true);
            lib.cap = S("acc_cap", () => HeadLayer(capL, "idle", HeadPart.Accessories));
            lib.beanie = S("acc_beanie", () => HeadLayer(beanieL, "idle", HeadPart.Accessories));
            lib.headphones = S("acc_headphones", () => HeadLayer(phonesL, "idle", HeadPart.Accessories));
            lib.glassesRound = S("acc_glasses", () => HeadLayer(roundL, "idle", HeadPart.Accessories));
            lib.sunglasses = S("acc_shades", () => HeadLayer(shadesL, "idle", HeadPart.Accessories));
            lib.earrings = S("acc_earrings", () => HeadLayer(earL, "idle", HeadPart.Accessories));
            lib.tattooFace = S("tat_face", () => HeadLayer(faceTatL, "idle", HeadPart.Accessories));

            // body
            lib.bodyBase = S("body_base", () => Body(b, BodyPart.Base));
            int topCount = AvatarCatalog.TopNames.Length;
            lib.tops = new Sprite[topCount];
            lib.sleeves = new Sprite[topCount];
            for (int i = 0; i < topCount; i++)
            {
                var tl = TopLook(i);
                lib.tops[i] = S("top_" + i, () => Body(tl, BodyPart.Top));
                lib.sleeves[i] = S("sleeve_" + i, () => Arm(tl, ArmPart.Sleeve));
            }
            int pantsCount = AvatarCatalog.PantsNames.Length;
            lib.pantsFar = new Sprite[pantsCount];
            lib.pantsNear = new Sprite[pantsCount];
            lib.pantsHem = new Sprite[pantsCount];
            for (int i = 0; i < pantsCount; i++)
            {
                var pl = PantsLook(i);
                lib.pantsFar[i] = S("pants_far_" + i, () => Body(pl, BodyPart.PantsFar));
                lib.pantsNear[i] = S("pants_near_" + i, () => Body(pl, BodyPart.PantsNear));
                lib.pantsHem[i] = pl.pantsStyle == PantsStyle.Wide ? S("pants_hem_" + i, () => Body(pl, BodyPart.Hem)) : null;
            }
            int shoeCount = AvatarCatalog.SneakerNames.Length;
            lib.shoesFar = new Sprite[shoeCount];
            lib.shoesNear = new Sprite[shoeCount];
            for (int i = 0; i < shoeCount; i++)
            {
                var sl = SneakerLook(i);
                lib.shoesFar[i] = S("shoe_far_" + i, () => Body(sl, BodyPart.ShoeFar));
                lib.shoesNear[i] = S("shoe_near_" + i, () => Body(sl, BodyPart.ShoeNear));
            }
            Look thinL = With(l => { l.chainThin = true; l.metal = Hex("D9DEE8"); }), cubanL = With(l => { l.chainCuban = true; l.metal = Hex("F2C14E"); });
            Look neckL = With(l => l.neckTattoo = true);
            lib.chainThin = S("chain_thin", () => Body(thinL, BodyPart.Chains));
            lib.chainCuban = S("chain_cuban", () => Body(cubanL, BodyPart.Chains));
            lib.tattooNeck = S("tat_neck", () => Body(neckL, BodyPart.NeckTattoo));

            // arm
            Look armTatL = With(l => { l.top = TopStyle.Tee; l.armTattoo = true; }), handTatL = With(l => l.handTattoo = true);
            Look smartL = With(l => l.watchSmart = true), goldL = With(l => { l.watchGold = true; l.metal = Hex("F2C14E"); });
            Look ringsL = With(l => { l.rings = true; l.metal = Hex("F2C14E"); });
            lib.hand = S("hand", () => Arm(b, ArmPart.Hand));
            lib.tattooArm = S("tat_arm", () => Arm(armTatL, ArmPart.TatArm));
            lib.tattooHand = S("tat_hand", () => Arm(handTatL, ArmPart.TatHand));
            lib.watchSmart = S("watch_smart", () => Arm(smartL, ArmPart.Watch));
            lib.watchGold = S("watch_gold", () => Arm(goldL, ArmPart.Watch));
            lib.rings = S("rings", () => Arm(ringsL, ArmPart.Rings));

            // thumbnails for the style menu
            lib.thumbKeys = new List<string>();
            lib.thumbSprites = new List<Sprite>();
            void Thumb(AvatarSlot slot, int value, Func<SdfPainter> draw)
            {
                string key = AvatarCatalog.Key(slot, value);
                lib.thumbKeys.Add(key);
                lib.thumbSprites.Add(MakeSprite(ThumbPath(key), overwrite, draw));
            }
            Look P(Look l) { l.top = TopStyle.Hoodie; l.cloth = Hex("6C3BD9"); l.clothShade = Hex("4A2799"); l.accent = Hex("FF3DC8"); return l; }
            for (int i = 0; i < hairCount; i++) { var hl = P(HairLook(i)); Thumb(AvatarSlot.Hair, i, () => Portrait(hl)); }
            for (int i = 0; i < topCount; i++) { var tl = TopLook(i); Thumb(AvatarSlot.Top, i, () => Body(tl, BodyPart.Top)); }
            for (int i = 0; i < pantsCount; i++)
            {
                var pl = PantsLook(i);
                Thumb(AvatarSlot.Pants, i, () => Body(pl, BodyPart.PantsFar | BodyPart.PantsNear | BodyPart.Hem | BodyPart.ShoeFar | BodyPart.ShoeNear));
            }
            for (int i = 0; i < shoeCount; i++) { var sl = SneakerLook(i); Thumb(AvatarSlot.Sneakers, i, () => ShoeThumb(sl)); }
            Thumb(AvatarSlot.Headphones, 1, () => Portrait(P(With(l => l.phonesOnHead = true))));
            Thumb(AvatarSlot.Headwear, 1, () => Portrait(P(With(l => l.cap = Hex("C2183A")))));
            Thumb(AvatarSlot.Headwear, 2, () => Portrait(P(With(l => l.beanie = Hex("6C3BD9")))));
            Thumb(AvatarSlot.Glasses, 1, () => Portrait(P(With(l => l.glasses = true))));
            Thumb(AvatarSlot.Glasses, 2, () => Portrait(P(With(l => { l.sunglasses = true; l.metal = Hex("F2C14E"); }))));
            Thumb(AvatarSlot.Chain, 1, () => Portrait(P(With(l => { l.chainThin = true; l.metal = Hex("D9DEE8"); }))));
            Thumb(AvatarSlot.Chain, 2, () => Portrait(P(With(l => { l.chainCuban = true; l.metal = Hex("F2C14E"); }))));
            Thumb(AvatarSlot.Earrings, 1, () => Portrait(P(With(l => l.earStud = true))));
            Thumb(AvatarSlot.Watch, 1, () => Arm(smartL));
            Thumb(AvatarSlot.Watch, 2, () => Arm(goldL));
            Thumb(AvatarSlot.Rings, 1, () => Arm(ringsL));
            Thumb(AvatarSlot.TattooHand, 1, () => Arm(handTatL));
            Thumb(AvatarSlot.TattooArm, 1, () => Arm(armTatL));
            Thumb(AvatarSlot.TattooNeck, 1, () => Body(With(l => { l.top = TopStyle.Tee; l.neckTattoo = true; }), BodyPart.Base | BodyPart.Top | BodyPart.NeckTattoo));
            Thumb(AvatarSlot.TattooFace, 1, () => Portrait(P(With(l => l.faceDots = true))));

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return lib;
        }
    }
}

namespace BeatFactory.EditorTools
{
    /// <summary>Entry point used by the scene builder.</summary>
    public static class AvatarBuilder
    {
        public static AvatarLibrary EnsureLibrary(bool overwrite) => Art.ArtGenerator.EnsureAvatarLibrary(overwrite);
    }
}
