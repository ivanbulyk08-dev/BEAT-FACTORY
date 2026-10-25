using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Marks a piece of the room cut from the painting that is closer to the camera than the character (bed, sofa, legs of a
    /// painted person). The character is drawn below the first of these, and above everything else in the room (light
    /// overlays included, so LED strips never cross his body).
    /// </summary>
    public class StudioForeground : MonoBehaviour { }
}
