using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Applies a <see cref="PlayerAppearance"/> to the layered player rig (main character and the style-menu preview).
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        [Serializable]
        public class ArmLayers
        {
            public Image sleeve, hand, tattooArm, tattooHand, watch, rings;
        }

        [SerializeField] AvatarLibrary library;
        [Header("Head")]
        [SerializeField] Image hairBack;
        [SerializeField] Image hairFront;
        [SerializeField] Image headwear;
        [SerializeField] Image headphones;
        [SerializeField] Image glasses;
        [SerializeField] Image earrings;
        [SerializeField] Image tattooFace;
        [Header("Body")]
        [SerializeField] Image pantsFar;
        [SerializeField] Image shoeFar;
        [SerializeField] Image pantsNear;
        [SerializeField] Image shoeNear;
        [SerializeField] Image pantsHem;
        [SerializeField] Image top;
        [SerializeField] Image chain;
        [SerializeField] Image tattooNeck;
        [Header("Arms")]
        [SerializeField] ArmLayers armFar;
        [SerializeField] ArmLayers armNear;
        [Tooltip("Follow the saved appearance automatically (off for previews driven by code).")]
        [SerializeField] bool followGame = true;

        AppearanceManager manager;

        void Start()
        {
            if (!followGame || GameManager.Instance == null) return;
            manager = GameManager.Instance.Appearance;
            manager.Changed += OnChanged;
            Apply(manager.Current);
        }

        void OnDestroy()
        {
            if (manager != null) manager.Changed -= OnChanged;
        }

        void OnChanged() => Apply(manager.Current);

        static void Set(Image img, Sprite sprite)
        {
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
        }

        public void Apply(PlayerAppearance a)
        {
            if (library == null || a == null) return;
            var L = library;
            bool hat = a.headwear > 0;
            Set(hairBack, AvatarLibrary.At(L.hairBack, a.hair));
            Set(hairFront, hat ? null : AvatarLibrary.At(L.hairFront, a.hair));
            Set(headwear, a.headwear == 1 ? L.cap : a.headwear == 2 ? L.beanie : null);
            Set(headphones, a.headphones ? L.headphones : null);
            Set(glasses, a.glasses == 1 ? L.glassesRound : a.glasses == 2 ? L.sunglasses : null);
            Set(earrings, a.earrings ? L.earrings : null);
            Set(tattooFace, a.tattooFace ? L.tattooFace : null);

            Set(pantsFar, AvatarLibrary.At(L.pantsFar, a.pants));
            Set(pantsNear, AvatarLibrary.At(L.pantsNear, a.pants));
            Set(pantsHem, AvatarLibrary.At(L.pantsHem, a.pants));
            Set(shoeFar, AvatarLibrary.At(L.shoesFar, a.sneakers));
            Set(shoeNear, AvatarLibrary.At(L.shoesNear, a.sneakers));
            Set(top, AvatarLibrary.At(L.tops, a.top));
            Set(chain, a.chain == 1 ? L.chainThin : a.chain == 2 ? L.chainCuban : null);
            Set(tattooNeck, a.tattooNeck ? L.tattooNeck : null);

            foreach (var arm in new[] { armFar, armNear })
            {
                if (arm == null) continue;
                Set(arm.sleeve, AvatarLibrary.At(L.sleeves, a.top));
                Set(arm.hand, L.hand);
                // the forearm sleeve tattoo only shows with the short-sleeve tee
                Set(arm.tattooArm, a.tattooArm && a.top == 2 ? L.tattooArm : null);
                Set(arm.tattooHand, a.tattooHand ? L.tattooHand : null);
                Set(arm.watch, a.watch == 1 ? L.watchSmart : a.watch == 2 ? L.watchGold : null);
                Set(arm.rings, a.rings ? L.rings : null);
            }
        }
    }
}

