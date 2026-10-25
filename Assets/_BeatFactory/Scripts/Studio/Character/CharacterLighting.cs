using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// How the room lights the character on one anchor. Filled automatically from the painting (colour/brightness around the
    /// spot) and the room's StudioLightGlow lights (direction and colour of the rim) — right-click StudioCharacterPlacement →
    /// "Auto-light anchors from the painting" — and editable by hand afterwards.
    /// </summary>
    [Serializable]
    public class CharacterLighting
    {
        [Tooltip("Multiplies the sprite: the room's colour and brightness at this spot.")]
        public Color ambient = new Color(0.78f, 0.72f, 0.8f);
        [Tooltip("Soft light across the body from the light direction.")]
        public Color keyColor = new Color(1f, 0.7f, 0.45f);
        [Range(0f, 1.5f)] public float keyStrength = 0.35f;
        [Tooltip("Thin coloured edge on the side facing the light (neon / lamp).")]
        public Color rimColor = new Color(0.85f, 0.45f, 1f);
        [Range(0f, 3f)] public float rimStrength = 1f;
        [Range(0.5f, 12f)] public float rimWidth = 2.5f;
        [Tooltip("Where the light comes from on screen (x right, y up).")]
        public Vector2 lightDirection = new Vector2(0.6f, 0.8f);
        [Tooltip("Darker towards the floor.")]
        [Range(0f, 1f)] public float floorShade = 0.3f;
        [Tooltip("Takes some colour out of the clean sprite so it matches the painting.")]
        [Range(0f, 1f)] public float desaturate = 0.12f;
        [Tooltip("Soft contact shadow under the character.")]
        [Range(0f, 1f)] public float shadowAlpha = 0.55f;
    }
}
