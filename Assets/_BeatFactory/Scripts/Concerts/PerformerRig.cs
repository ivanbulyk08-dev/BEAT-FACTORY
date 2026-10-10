using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    public enum PerformState { Idle, Rap, Hype, Walk, CrowdInteraction, BigDrop, Point, End }

    /// <summary>
    /// The signed artist performing on stage, assembled from their <see cref="ArtistVisualIdentity"/> — the same face, hair,
    /// clothes and tattoos as their portrait. Layered rig (legs, torso, head with expressions, arms, microphone) animated in
    /// code: idle groove, rapping into the mic, hype (arms up), walking the stage, crowd interaction at the edge, the big
    /// drop jump, pointing at the crowd and the end-of-show celebration. States blend smoothly.
    /// </summary>
    public class PerformerRig : MonoBehaviour
    {
        const float NativeHeight = 345f;
        /// <summary>Grip point on the mic sprite (fraction of its height, handle middle).</summary>
        const float MicGrip = 0.34f;
        /// <summary>Hand center relative to the shoulder pivot in arm space (arm sprite: pivot y 126, hand y 22).</summary>
        const float HandOffset = -104f;

        RectTransform root, body, head, armNear, armFar, legNear, legFar, torso, mic;
        Image headImg;
        ArtistVisualIdentity id;
        Vector2 home, target, pos;
        float scale = 1f, time, bpm = 120, stateTime, edgeY;
        PerformState state = PerformState.Idle;
        bool facingRight;
        // smoothed pose values
        float aNear, aFar, lNear, lFar, bob, lean, nearLen = 1f, jumpY, micTilt = 12f;

        public PerformState State => state;
        public Vector2 DesignPosition => pos;
        /// <summary>Head position in design units (for the close-up camera).</summary>
        public Vector2 HeadDesign => pos + new Vector2(0, 300 * scale);
        public float Scale => scale;
        public bool AtEdge => state == PerformState.CrowdInteraction && Mathf.Abs(pos.y - edgeY) < 4f;

        float kx = 1, ky = 1;

        public static PerformerRig Create(RectTransform parent, Sprite micSprite)
        {
            var r = UIKit.Rect("Performer", parent);
            UIKit.Stretch(r);
            var rig = r.gameObject.AddComponent<PerformerRig>();
            rig.Build(r, micSprite);
            return rig;
        }

        RectTransform Part(string name, Transform parent, Vector2 size, Vector2 pivot, Vector2 pos, out Image img)
        {
            img = UIKit.Image(name, parent, Color.white);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        void Build(RectTransform holder, Sprite micSprite)
        {
            root = UIKit.Rect("Rig", holder);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(200, NativeHeight);
            // rig space: origin = between the feet; hips at y 140
            legFar = Part("LegFar", root, new Vector2(60, 150), new Vector2(0.5f, 146f / 150f), new Vector2(9, 146), out var lf);
            lf.color = new Color(0.72f, 0.72f, 0.78f);
            armFar = Part("ArmFar", root, new Vector2(56, 140), new Vector2(0.5f, 126f / 140f), new Vector2(3, 220), out var af);
            af.color = new Color(0.72f, 0.72f, 0.78f);
            body = UIKit.Rect("Body", root);
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f);
            body.pivot = new Vector2(0.5f, 0f);
            body.sizeDelta = new Vector2(200, NativeHeight);
            torso = Part("Torso", body, new Vector2(200, 190), new Vector2(115f / 200f, 64f / 190f), new Vector2(0, 140), out _);
            head = Part("Head", body, new Vector2(160, 160), new Vector2(88f / 160f, 24f / 160f), new Vector2(-16, 242), out headImg);
            legNear = Part("LegNear", root, new Vector2(60, 150), new Vector2(0.5f, 146f / 150f), new Vector2(-8, 146), out _);
            legNear.SetSiblingIndex(1);
            // The mic is NOT a child of the arm (it would turn upside down when the arm is raised): it follows the hand
            // position but keeps its own upright orientation (grille up, handle down). Drawn under the near arm so the
            // fingers wrap around the handle instead of the mic passing through the hand.
            mic = Part("Mic", root, new Vector2(18, 48), new Vector2(0.5f, MicGrip), new Vector2(-23, 112), out var micImg);
            micImg.sprite = micSprite;
            armNear = Part("ArmNear", root, new Vector2(56, 140), new Vector2(0.5f, 126f / 140f), new Vector2(-23, 216), out _);
            micImg.enabled = micSprite != null;
        }

        public void Setup(ArtistVisualIdentity identity, Vector2 stagePos, float heightDesign, float edge, float tempo)
        {
            id = identity;
            bpm = tempo;
            home = pos = target = stagePos;
            edgeY = edge;
            scale = heightDesign / NativeHeight;
            bool ok = identity != null && identity.HasPerformer;
            root.gameObject.SetActive(ok);
            if (!ok) return;
            torso.GetComponent<Image>().sprite = identity.stageTorso;
            armNear.GetComponent<Image>().sprite = identity.stageArm;
            armFar.GetComponent<Image>().sprite = identity.stageArm;
            legNear.GetComponent<Image>().sprite = identity.stageLeg;
            legFar.GetComponent<Image>().sprite = identity.stageLeg;
            headImg.sprite = identity.Head(ArtistVisualIdentity.Idle);
            SetState(PerformState.Walk);
            pos = home + new Vector2(-260, 0); // walks in from the side
            target = home;
            time = 0;
        }

        public void SetState(PerformState s)
        {
            state = s;
            stateTime = 0;
            switch (s)
            {
                case PerformState.Walk: target = new Vector2(Random.Range(250f, 550f), home.y); break;
                case PerformState.CrowdInteraction: target = new Vector2(Mathf.Clamp(pos.x + Random.Range(-80f, 80f), 260, 540), edgeY); break;
                case PerformState.End: target = new Vector2(400, edgeY + 6); break;
                default: if (pos.y < home.y - 1) target = new Vector2(pos.x, home.y); else target = pos; break;
            }
        }

        public void WalkTo(float x) { state = PerformState.Walk; stateTime = 0; target = new Vector2(x, home.y); }

        void Update()
        {
            if (id == null || !root.gameObject.activeSelf) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            time += dt;
            stateTime += dt;
            var rect = ((RectTransform)transform).rect;
            kx = rect.width / 800f;
            ky = rect.height / 1400f;
            float beat = time * bpm / 60f;
            float onBeat = Mathf.Abs(Mathf.Sin(Mathf.PI * beat));

            // movement
            var to = target - pos;
            float speed = state == PerformState.Walk || state == PerformState.CrowdInteraction || state == PerformState.End ? 120f : 60f;
            bool walking = to.magnitude > 3f;
            if (walking)
            {
                var step = to.normalized * speed * dt;
                if (step.magnitude > to.magnitude) step = to;
                pos += step;
                if (Mathf.Abs(to.x) > 2f) facingRight = to.x > 0;
            }
            else if (state == PerformState.Walk && stateTime > 1.2f) state = PerformState.Rap;

            // target pose per state (angles: 0 = arm hanging; negative = forward/up in the facing direction)
            float tNear = -8, tFar = 6, tBob = 3f * onBeat, tLean = 0, tLen = 1f, tJump = 0;
            int expr = ArtistVisualIdentity.Idle;
            bool syllable = Mathf.Repeat(time * 7.5f, 1f) < 0.5f;
            switch (state)
            {
                case PerformState.Idle:
                    tNear = -20 + 10 * Mathf.Sin(time * 2f); tFar = 12; tLean = 3 * Mathf.Sin(time);
                    break;
                case PerformState.Rap:
                    tNear = -150; tLen = 0.42f; tFar = -40 + 35 * Mathf.Sin(Mathf.PI * beat); tLean = 4 * Mathf.Sin(Mathf.PI * beat * 0.5f);
                    expr = syllable ? ArtistVisualIdentity.Excited : ArtistVisualIdentity.Idle;
                    break;
                case PerformState.Hype:
                    tNear = -196 + 10 * Mathf.Sin(time * 6f); tFar = 160 - 10 * Mathf.Sin(time * 6f); tBob = 6f * onBeat; expr = ArtistVisualIdentity.Excited;
                    break;
                case PerformState.Walk:
                    tNear = -150; tLen = 0.42f; tFar = -25 * Mathf.Sin(time * 8f);
                    expr = syllable ? ArtistVisualIdentity.Excited : ArtistVisualIdentity.Idle;
                    break;
                case PerformState.CrowdInteraction:
                    tNear = -120 + 15 * Mathf.Sin(time * 3f); tFar = 20; tLean = -8; expr = ArtistVisualIdentity.Smile;
                    break;
                case PerformState.Point:
                    tNear = -105; tFar = 15; tLean = -6; expr = ArtistVisualIdentity.Excited;
                    break;
                case PerformState.BigDrop:
                    tNear = -198; tFar = 158; tJump = 34f * onBeat; tBob = 0; expr = ArtistVisualIdentity.Excited;
                    break;
                case PerformState.End:
                    tNear = -192 + 14 * Mathf.Sin(time * 5f); tFar = 25; expr = stateTime > 1.6f ? ArtistVisualIdentity.Proud : ArtistVisualIdentity.Smile;
                    tLean = stateTime > 2.4f && stateTime < 3.4f ? -18 : 0; // a bow: thank you!
                    break;
            }
            if (state != PerformState.Rap && state != PerformState.Walk && Mathf.Repeat(time, 3.7f) < 0.12f) expr = ArtistVisualIdentity.Blink;

            float k = 1f - Mathf.Exp(-dt * 10f);
            aNear = Mathf.Lerp(aNear, tNear, k);
            aFar = Mathf.Lerp(aFar, tFar, k);
            bob = Mathf.Lerp(bob, tBob, k);
            lean = Mathf.Lerp(lean, tLean, k);
            nearLen = Mathf.Lerp(nearLen, tLen, k);
            jumpY = Mathf.Lerp(jumpY, tJump, 1f - Mathf.Exp(-dt * 16f));
            float stride = walking ? Mathf.Sin(time * 9f) * 24f : 0f;
            lNear = Mathf.Lerp(lNear, stride, k);
            lFar = Mathf.Lerp(lFar, -stride, k);

            // apply
            root.anchoredPosition = new Vector2(pos.x * kx, (pos.y + jumpY) * ky);
            float s = scale * kx;
            float edgeScale = Mathf.Lerp(1f, 1.06f, Mathf.InverseLerp(home.y, edgeY, pos.y));
            root.localScale = new Vector3((facingRight ? -s : s) * edgeScale, s * edgeScale, 1f);
            body.anchoredPosition = new Vector2(0, bob);
            body.localEulerAngles = new Vector3(0, 0, lean);
            armNear.anchoredPosition = new Vector2(-23, 216 + bob);
            armFar.anchoredPosition = new Vector2(3, 220 + bob);
            armNear.localEulerAngles = new Vector3(0, 0, aNear);
            armFar.localEulerAngles = new Vector3(0, 0, aFar);
            armNear.localScale = new Vector3(1f, nearLen, 1f);
            // mic in the hand: follow the hand, stay upright with a natural tilt toward the face/crowd
            float tMic = state == PerformState.Rap || state == PerformState.Walk ? 10f : state == PerformState.End ? 14f
                : state == PerformState.Hype || state == PerformState.BigDrop ? 6f : 12f;
            micTilt = Mathf.Lerp(micTilt, tMic, k);
            float ar = aNear * Mathf.Deg2Rad;
            // the hand = (0, HandOffset·len) rotated by the arm angle around the shoulder
            var handLocal = new Vector2(-(HandOffset * nearLen) * Mathf.Sin(ar), (HandOffset * nearLen) * Mathf.Cos(ar));
            mic.anchoredPosition = armNear.anchoredPosition + handLocal;
            mic.localEulerAngles = new Vector3(0, 0, micTilt);
            legNear.localEulerAngles = new Vector3(0, 0, lNear);
            legFar.localEulerAngles = new Vector3(0, 0, lFar);
            head.localEulerAngles = new Vector3(0, 0, Mathf.Sin(Mathf.PI * beat) * (state == PerformState.Rap ? 5f : 2.5f));
            var spr = id.Head(expr);
            if (spr != null && headImg.sprite != spr) headImg.sprite = spr;
        }
    }
}
