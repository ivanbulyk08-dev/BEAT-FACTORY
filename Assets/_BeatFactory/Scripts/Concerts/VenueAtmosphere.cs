using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// Living atmosphere over a painted concert view (the main view and every camera angle), design space 800×1400:
    /// the painted light fixtures pulse on the beat, beams sweep out of some of them, smoke drifts over the stage and
    /// flame jets fire at the sides of the stage on the big hits. Everything is a handful of pooled images — no per-frame
    /// allocations. Back layer (fixtures, beams) sits behind the artist, front layer (smoke, flames) in front of him.
    /// </summary>
    public class VenueAtmosphere : MonoBehaviour
    {
        const float W = ConcertStageView.W, H = ConcertStageView.H;
        const int MaxLights = 28, MaxBeams = 8, Puffs = 10, MaxFlames = 4;

        /// <summary>What one view looks like: painted fixtures, the height of the stage floor (smoke) and pyro spots.</summary>
        public struct View
        {
            public Vector2[] lights;
            public float smokeY;
            public Vector2[] pyro;
        }

        class Light { public Image glow; public Vector2 pos; public float phase; public bool offBeat; }
        class Beam { public Image img; public float phase, speed, swing, baseAngle; }
        class Puff { public Image img; public float x, y, speed, size, phase; }
        class Flame { public Image img, glow; public Vector2 pos; public float t = -1f, height; }

        RectTransform back, front, cam;
        readonly List<Light> lights = new List<Light>();
        readonly List<Beam> beams = new List<Beam>();
        readonly List<Puff> puffs = new List<Puff>();
        readonly List<Flame> flames = new List<Flame>();
        Color tint = new Color(0.72f, 0.4f, 1f);
        float beat, energy = 0.3f, clock, lastW;
        bool hot;

        public RectTransform Back => back;
        public RectTransform Front => front;

        public static VenueAtmosphere Create(RectTransform cam, Sprite glow, Sprite beam, Sprite flame)
        {
            var go = new GameObject("Atmosphere", typeof(RectTransform));
            go.transform.SetParent(cam, false);
            var a = go.AddComponent<VenueAtmosphere>();
            a.cam = cam;
            a.back = Space("AtmosphereBack", cam);
            a.front = Space("AtmosphereFront", cam);
            for (int i = 0; i < MaxBeams; i++)
            {
                var img = Img("Beam", a.back, beam, new Vector2(0.5f, 1f));
                a.beams.Add(new Beam { img = img });
            }
            for (int i = 0; i < MaxLights; i++)
            {
                var img = Img("Fixture", a.back, glow, new Vector2(0.5f, 0.5f));
                a.lights.Add(new Light { glow = img });
            }
            for (int i = 0; i < Puffs; i++)
            {
                var img = Img("Smoke", a.front, glow, new Vector2(0.5f, 0.5f));
                a.puffs.Add(new Puff { img = img });
            }
            for (int i = 0; i < MaxFlames; i++)
            {
                var g = Img("FlameGlow", a.front, glow, new Vector2(0.5f, 0.5f));
                var f = Img("Flame", a.front, flame, new Vector2(0.5f, 0f));
                a.flames.Add(new Flame { img = f, glow = g });
            }
            return a;
        }

        static RectTransform Space(string name, RectTransform parent)
        {
            var rt = UIKit.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(W, H);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static Image Img(string name, RectTransform parent, Sprite s, Vector2 pivot)
        {
            var img = UIKit.Image(name, parent, new Color(1, 1, 1, 0));
            img.sprite = s;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            img.gameObject.SetActive(false);
            return img;
        }

        /// <summary>Shows the effects for a view (null lights / no pyro are fine). Called on every camera cut.</summary>
        public void Show(View v, Color colour, int seed)
        {
            tint = colour;
            var rnd = new System.Random(seed);
            var pts = v.lights ?? new Vector2[0];
            for (int i = 0; i < lights.Count; i++)
            {
                var l = lights[i];
                bool on = i < pts.Length;
                l.glow.gameObject.SetActive(on);
                if (!on) continue;
                l.pos = pts[i];
                l.phase = (float)rnd.NextDouble();
                l.offBeat = rnd.Next(2) == 0;
                l.glow.rectTransform.anchoredPosition = l.pos;
            }
            // beams from the highest fixtures, pointing down towards the stage / crowd
            var order = new List<Vector2>(pts);
            order.Sort((a, b) => b.y.CompareTo(a.y));
            for (int i = 0; i < beams.Count; i++)
            {
                var b = beams[i];
                bool on = i < order.Count && i < 6;
                b.img.gameObject.SetActive(on);
                if (!on) continue;
                var p = order[i];
                var rt = b.img.rectTransform;
                rt.anchoredPosition = p;
                float h = Mathf.Clamp(p.y - v.smokeY + 120f, 300f, 1000f);
                rt.sizeDelta = new Vector2(110f, h);
                b.baseAngle = Mathf.Clamp((p.x - W / 2f) * 0.06f, -25f, 25f); // outer fixtures lean outwards
                b.phase = (float)rnd.NextDouble() * 6.28f;
                b.speed = 0.5f + (float)rnd.NextDouble() * 0.5f;
                b.swing = 14f + (float)rnd.NextDouble() * 14f;
            }
            // smoke along the stage floor (and a little above)
            for (int i = 0; i < puffs.Count; i++)
            {
                var p = puffs[i];
                p.img.gameObject.SetActive(true);
                p.x = (float)rnd.NextDouble() * (W + 400f) - 200f;
                p.y = v.smokeY + 30f + (float)rnd.NextDouble() * 170f;
                p.size = 320f + (float)rnd.NextDouble() * 280f;
                p.speed = (rnd.Next(2) == 0 ? -1f : 1f) * (10f + (float)rnd.NextDouble() * 18f);
                p.phase = (float)rnd.NextDouble() * 6.28f;
            }
            var py = v.pyro ?? new Vector2[0];
            for (int i = 0; i < flames.Count; i++)
            {
                var f = flames[i];
                f.t = -1f;
                f.img.gameObject.SetActive(false);
                f.glow.gameObject.SetActive(false);
                f.pos = i < py.Length ? py[i] : new Vector2(-9999, -9999);
            }
            lastW = -1f;
        }

        /// <summary>Show part: flames on the big hits, more movement while the show is hot.</summary>
        public void SetPart(ShowPart p)
        {
            hot = p == ShowPart.Drop || p == ShowPart.BigMoment || p == ShowPart.Chorus;
            if (p == ShowPart.Drop || p == ShowPart.BigMoment) Pyro();
        }

        public void Pyro()
        {
            for (int i = 0; i < flames.Count; i++)
            {
                var f = flames[i];
                if (f.pos.x < -1000f) continue;
                f.t = -0.12f * (i % 2); // left and right a beat apart
                f.height = 300f + Random.value * 120f;
            }
        }

        public void Tick(float beats, float excitement, float time, float dt)
        {
            beat = beats;
            energy = Mathf.MoveTowards(energy, Mathf.Clamp01(excitement), dt * 1.5f);
            clock = time;
            float w = cam.rect.width;
            if (!Mathf.Approximately(w, lastW))
            {
                lastW = w;
                var s = new Vector3(w / W, w / W, 1f);
                back.localScale = front.localScale = s;
            }
            float pulse = Mathf.Pow(1f - Mathf.Repeat(beat, 1f), 3f);
            float pulseOff = Mathf.Pow(1f - Mathf.Repeat(beat + 0.5f, 1f), 3f);

            foreach (var l in lights)
            {
                if (!l.glow.gameObject.activeSelf) continue;
                float p = l.offBeat ? pulseOff : pulse;
                float flick = 0.85f + 0.15f * Mathf.Sin(clock * 9f + l.phase * 20f);
                float a = (0.18f + 0.5f * p * energy) * flick;
                l.glow.color = new Color(Mathf.Lerp(tint.r, 1f, 0.6f), Mathf.Lerp(tint.g, 1f, 0.6f), 1f, a);
                float size = 46f + 40f * p * energy;
                l.glow.rectTransform.sizeDelta = new Vector2(size, size);
            }
            foreach (var b in beams)
            {
                if (!b.img.gameObject.activeSelf) continue;
                float speed = b.speed * (hot ? 1.8f : 0.8f);
                float ang = b.baseAngle + Mathf.Sin(clock * speed + b.phase) * b.swing;
                b.img.rectTransform.localEulerAngles = new Vector3(0, 0, ang);
                b.img.color = new Color(tint.r, tint.g, tint.b, (0.1f + 0.22f * energy) * (0.75f + 0.25f * pulse));
            }
            foreach (var p in puffs)
            {
                p.x += p.speed * dt * (hot ? 1.6f : 1f);
                if (p.x > W + 300f) p.x = -300f;
                if (p.x < -300f) p.x = W + 300f;
                float bob = Mathf.Sin(clock * 0.4f + p.phase) * 18f;
                var rt = p.img.rectTransform;
                rt.anchoredPosition = new Vector2(p.x, p.y + bob);
                float sz = p.size * (1f + 0.08f * Mathf.Sin(clock * 0.3f + p.phase));
                rt.sizeDelta = new Vector2(sz * 1.5f, sz * 0.6f);
                float a = (0.1f + 0.1f * energy) * (0.7f + 0.3f * Mathf.Sin(clock * 0.5f + p.phase));
                p.img.color = new Color(Mathf.Lerp(tint.r, 1f, 0.35f), Mathf.Lerp(tint.g, 1f, 0.3f), Mathf.Lerp(tint.b, 1f, 0.3f), a);
            }
            foreach (var f in flames)
            {
                if (f.t < -0.5f) continue;
                f.t += dt;
                if (f.t < 0f) continue;
                const float life = 0.9f;
                bool on = f.t < life;
                f.img.gameObject.SetActive(on);
                f.glow.gameObject.SetActive(on);
                if (!on) { f.t = -1f; continue; }
                float k = f.t / life;
                float grow = Mathf.Clamp01(k / 0.15f);
                float fade = 1f - Mathf.Clamp01((k - 0.55f) / 0.45f);
                var rt = f.img.rectTransform;
                rt.anchoredPosition = f.pos;
                float h = f.height * grow * (0.92f + 0.08f * Mathf.Sin(clock * 40f));
                rt.sizeDelta = new Vector2(h * 0.38f, h);
                f.img.color = new Color(1f, 1f, 1f, fade);
                var g = f.glow.rectTransform;
                g.anchoredPosition = f.pos + new Vector2(0, h * 0.4f);
                g.sizeDelta = new Vector2(h * 1.4f, h * 1.2f);
                f.glow.color = new Color(1f, 0.55f, 0.2f, 0.35f * fade);
            }
        }
    }
}
