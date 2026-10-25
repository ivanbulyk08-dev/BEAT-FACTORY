using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Where things are in a painted venue (design space 800×1400, bottom-left origin): the stage floor the artist stands
    /// on, the stage front edge, the artist's height, the light truss and the LED side panels. Painted venues have the fans
    /// drawn in, so the generated crowd is not shown there.
    /// </summary>
    /// <summary>One painted camera angle of a venue (design space 800×1400): where the LED screen and the artist are in it.</summary>
    [Serializable]
    public struct VenueAngle
    {
        public Sprite sprite;
        [Tooltip("LED screen (x, y-bottom, w, h); zero = the screen is not visible from here.")]
        public Vector4 screen;
        [Tooltip("Artist feet (design) and height; height 0 = no artist in this view (e.g. looking from the stage at the crowd).")]
        public Vector2 artist;
        public float artistHeight;
        [Tooltip("When in the show: 0 = after the drop, 1 = build-up, 2 = chorus, 3 = big moment.")]
        public int slot;
        [Tooltip("Painted light fixtures in this view (design positions) that pulse and throw beams.")]
        public Vector2[] lights;
    }

    [Serializable]
    public struct VenueLayout
    {
        [Tooltip("Fans are part of the painting: the generated crowd and the stadium stand lights are hidden.")]
        public bool paintedCrowd;
        public float stageY, edgeY, artistHeight, trussY;
        [Tooltip("Extra LED panels drawn over the painting (x centre, y centre, width, height). Empty = none.")]
        public Vector4[] panels;
        [Tooltip("Painted phone screens in the crowd (design positions) that glint during the show.")]
        public Vector2[] phones;
        [Tooltip("Painted light fixtures of the main view (design positions) that pulse and throw beams.")]
        public Vector2[] lights;

        [Tooltip("Extra camera angles of this venue the show cuts to (side of the stage, from the back, low at the barrier...).")]
        public VenueAngle[] angles;

        public bool IsSet => stageY > 0f;
    }
}
