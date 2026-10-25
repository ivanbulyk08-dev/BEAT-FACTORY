using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// The single source of truth for how an artist looks. Every image of the artist — contract portrait, card, studio
    /// poster, LED screen and the full-body performer on stage — is generated from ONE character definition
    /// (ArtGenerator.Roster → Look: face shape, skin, eyes, hair, facial hair, accessories, tattoos, clothes), so the artist is
    /// always recognizably the same person. The scene builder fills these references from the generated assets.
    /// </summary>
    [Serializable]
    public class ArtistVisualIdentity
    {
        [Header("Description (from the character definition)")]
        public Color skin = new Color(0.55f, 0.35f, 0.23f);
        public Color hair = Color.black;
        public Color hairTip = Color.black;
        public string hairStyle;
        public string facialHair;
        public string accessories;
        public string tattoos;
        public string outfit;
        public Color clothing = Color.gray;
        public Color accent = Color.magenta;
        [Tooltip("Stage colors (genre + outfit).")]
        public Color[] palette = new Color[0];

        [Header("Stage performer (same face as the portrait)")]
        [Tooltip("Head per expression: idle, excited (rapping / hype), smile, proud, blink.")]
        public Sprite[] stageHeads = new Sprite[0];
        public Sprite stageTorso;
        public Sprite stageArm;
        public Sprite stageLeg;
        [Header("Detailed artist (optional)")]
        [Tooltip("Hand-made layered art + DRIP STUDIO wardrobe. When set, the stage performer uses it instead of the parts above.")]
        public ArtistRig rig;

        [Header("Fans")]
        [Tooltip("What fans write on their signs.")]
        public string[] signs = new string[0];

        public bool HasDetailedRig => rig != null && rig.heads != null && rig.heads.Length > 0 && rig.Body(RigPart.Torso) != null;
        public bool HasPerformer => HasDetailedRig || stageTorso != null && stageArm != null && stageLeg != null && stageHeads != null && stageHeads.Length > 0;

        public Sprite Head(int expression) =>
            stageHeads == null || stageHeads.Length == 0 ? null : stageHeads[Mathf.Clamp(expression, 0, stageHeads.Length - 1)];

        public const int Idle = 0, Excited = 1, Smile = 2, Proud = 3, Blink = 4;
    }
}
