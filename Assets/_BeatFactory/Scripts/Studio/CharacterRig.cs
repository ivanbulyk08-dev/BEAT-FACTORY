using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Procedural 2D animation for a seated character built from parts (body, head, far arm, near arm).
    /// States: Idle (nods, blinks), Producing (calm typing, focused), Making Beat (fast typing, excited),
    /// Success (jump, arms up, big smile), Upgrade (proud pulse), Bounce (a producer finished a beat).
    /// The face swaps between expression sprites. No Animator needed.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public enum Face { Idle, Focus, Excited, Smile, Proud, Blink }

        [SerializeField] RectTransform body;
        [SerializeField] RectTransform head;
        [Tooltip("Far arm (behind the body).")]
        [SerializeField] RectTransform armL;
        [Tooltip("Near arm (in front of the body).")]
        [SerializeField] RectTransform armR;

        [Header("Face")]
        [SerializeField] Image faceImage;
        [Tooltip("Sprites in Face order: idle, focus, excited, smile, proud, blink.")]
        [SerializeField] Sprite[] faces;
        [Tooltip("Faces used while working (alternates every few seconds).")]
        [SerializeField] Face workFaceA = Face.Focus;
        [SerializeField] Face workFaceB = Face.Idle;

        [Header("Style")]
        [Tooltip("Head nods to this tempo (the music loop is 85 BPM).")]
        [SerializeField] float bpm = 85f;
        [SerializeField] float nodAmount = 3f;
        [SerializeField] float typingSpeed = 16f;
        [SerializeField] float typingAngle = 9f;
        [Tooltip("Producers keep working all the time; the player character works when you tap MAKE BEAT.")]
        [SerializeField] bool alwaysWorking;
        [SerializeField] float phaseOffset;
        [Tooltip("After MAKE BEAT the player keeps producing (calm, focused) for this long.")]
        [SerializeField] float producingSeconds = 3f;
        [Tooltip("Arm angles for the cheer pose (Success).")]
        [SerializeField] float cheerAngleL = 155f;
        [SerializeField] float cheerAngleR = -150f;

        RectTransform root;
        Vector2 rootPos, bodyPos, headPos, armLPos, armRPos;
        float armLRot, armRRot;
        Vector3 rootScale;
        float time, work, producing, success, upgrade, bounce, blinkTimer, blink;

        const float SuccessTime = 0.9f, UpgradeTime = 1.1f, BounceTime = 0.35f;

        void Awake()
        {
            root = (RectTransform)transform;
            rootPos = root.anchoredPosition;
            rootScale = root.localScale;
            bodyPos = body.anchoredPosition;
            headPos = head.anchoredPosition;
            armLPos = armL.anchoredPosition;
            armRPos = armR.anchoredPosition;
            armLRot = armL.localEulerAngles.z;
            armRRot = armR.localEulerAngles.z;
            time = phaseOffset * 10f;
            blinkTimer = 1.5f + phaseOffset * 3f;
        }

        public void Work(float seconds = 0.6f)
        {
            work = Mathf.Max(work, seconds);
            producing = producingSeconds;
        }

        /// <summary>Swaps clothing sprites (the player's outfit improves with the studio).</summary>
        public void SetOutfit(Sprite bodySprite, Sprite armSprite)
        {
            if (bodySprite != null && body.TryGetComponent<Image>(out var b)) b.sprite = bodySprite;
            if (armSprite == null) return;
            if (armL.TryGetComponent<Image>(out var l)) l.sprite = armSprite;
            if (armR.TryGetComponent<Image>(out var r)) r.sprite = armSprite;
        }

        /// <summary>Moves the character's resting spot (each room places the desk differently).</summary>
        public void SetHome(Vector2 position, bool facingRight = false)
        {
            if (root == null) root = (RectTransform)transform;
            rootPos = position;
            root.anchoredPosition = position;
            if (rootScale == Vector3.zero) rootScale = root.localScale;
            // mirrored rooms put the desk on the right: the producer faces right
            rootScale.x = Mathf.Abs(rootScale.x) * (facingRight ? -1f : 1f);
            root.localScale = rootScale;
        }

        public void Success() => success = SuccessTime;
        public void Upgrade() => upgrade = UpgradeTime;
        public void Bounce() => bounce = BounceTime;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            if (work <= 0f) producing = Mathf.Max(0, producing - dt);
            work = Mathf.Max(0, work - dt);
            success = Mathf.Max(0, success - dt);
            upgrade = Mathf.Max(0, upgrade - dt);
            bounce = Mathf.Max(0, bounce - dt);

            float beat = time * bpm / 60f * Mathf.PI * 2f;
            bool energetic = work > 0f;
            bool working = alwaysWorking || energetic || producing > 0f;
            float speed = energetic ? typingSpeed * 1.4f : typingSpeed * 0.75f;

            // Head nods with the music (stronger while making a beat).
            float nod = Mathf.Sin(beat) * nodAmount * (energetic ? 2.2f : working ? 1.3f : 1f);
            float headTilt = Mathf.Sin(beat * 0.5f) * (energetic ? 4f : 1.5f);

            // Arms: alternate typing while working, rest otherwise.
            float typing = working ? Mathf.Sin(time * speed) * typingAngle * (energetic ? 1.3f : 0.7f) : 0f;
            float lRot = armLRot + typing, rRot = armRRot - typing;
            float lY = working ? Mathf.Max(0, Mathf.Sin(time * speed)) * 4f : 0f;
            float rY = working ? Mathf.Max(0, -Mathf.Sin(time * speed)) * 4f : 0f;

            float breathe = 1f + Mathf.Sin(time * 2.2f) * 0.012f;

            Vector2 jump = Vector2.zero;
            float scale = 1f;
            if (success > 0f)
            {
                float p = 1f - success / SuccessTime;
                jump.y = Mathf.Sin(p * Mathf.PI) * 26f;
                float up = Mathf.Sin(Mathf.Min(1f, p * 2.2f) * Mathf.PI * 0.5f) * Mathf.Clamp01((1f - p) * 4f);
                lRot = Mathf.LerpAngle(lRot, cheerAngleL, up);
                rRot = Mathf.LerpAngle(rRot, cheerAngleR, up);
                headTilt += Mathf.Sin(p * Mathf.PI * 4f) * 6f;
            }
            if (upgrade > 0f)
            {
                float p = 1f - upgrade / UpgradeTime;
                scale *= 1f + Mathf.Sin(Mathf.Min(1f, p * 1.6f) * Mathf.PI) * 0.1f;
                headTilt += Mathf.Sin(Mathf.Min(1f, p * 1.6f) * Mathf.PI) * 6f; // chin up
                float pump = Mathf.Sin(p * Mathf.PI * 6f) * 10f * (1f - p);
                lRot += pump;
                rRot -= pump;
            }
            if (bounce > 0f)
            {
                float p = 1f - bounce / BounceTime;
                breathe *= 1f - Mathf.Sin(p * Mathf.PI) * 0.05f;
            }

            root.anchoredPosition = rootPos + jump;
            root.localScale = rootScale * scale;
            body.localScale = new Vector3(1f, breathe, 1f);
            head.anchoredPosition = headPos + new Vector2(0f, nod + (breathe - 1f) * 120f);
            head.localEulerAngles = new Vector3(0, 0, headTilt);
            armL.anchoredPosition = armLPos + new Vector2(0f, lY);
            armR.anchoredPosition = armRPos + new Vector2(0f, rY);
            armL.localEulerAngles = new Vector3(0, 0, lRot);
            armR.localEulerAngles = new Vector3(0, 0, rRot);

            UpdateFace(dt, energetic, working);
        }

        void UpdateFace(float dt, bool energetic, bool working)
        {
            if (faceImage == null || faces == null || faces.Length < 6) return;

            Face face;
            if (success > 0f) face = success > SuccessTime * 0.55f ? Face.Excited : Face.Smile;
            else if (upgrade > 0f) face = Face.Proud;
            else if (bounce > 0f) face = Face.Smile;
            else if (energetic) face = Face.Excited;
            else if (working) face = Mathf.Repeat(time, 7f) < 4.5f ? workFaceA : workFaceB;
            else face = Face.Idle;

            // Natural blinking on calm faces.
            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blink = 0.12f;
                blinkTimer = 2.2f + Random.value * 3f;
            }
            blink = Mathf.Max(0f, blink - dt);
            if (blink > 0f && (face == Face.Idle || face == Face.Focus || face == Face.Proud)) face = Face.Blink;

            var sprite = faces[(int)face];
            if (sprite != null && faceImage.sprite != sprite) faceImage.sprite = sprite;
        }
    }
}
