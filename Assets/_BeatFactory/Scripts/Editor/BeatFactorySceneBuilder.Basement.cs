using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory.EditorTools
{
    /// <summary>
    /// Shared helpers for the painted studio rooms (full-screen aspect-fitted paintings with live layers aligned to them:
    /// DAW screens, glows, foreground cut-outs) and the studio focus mode. The rooms themselves: BeatFactorySceneBuilder.Rooms.cs.
    /// </summary>
    public static partial class BeatFactorySceneBuilder
    {
        /// <summary>Size of the room being built (the helpers below map that painting's pixels).</summary>
        static float ArtW = 1440f, ArtH = 2560f;
        /// <summary>Image pixel (top-left origin) → normalized anchor (bottom-left origin).</summary>
        static Vector2 ArtUV(float x, float y) => new Vector2(x / ArtW, 1f - y / ArtH);

        static Sprite ImportArt(string path, int maxSize)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return null;
            // sharp: no mipmaps (UI is never minified much), no compression artefacts, full resolution
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A painted room covering the screen (keeps its aspect ratio, never stretched).</summary>
        static RectTransform Room(RectTransform holder, string name, Sprite sprite)
        {
            var art = UIFactory.Image(name, holder, Color.white);
            art.sprite = sprite;
            art.raycastTarget = false;
            art.preserveAspect = false;
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = ArtW / ArtH;
            return art.rectTransform;
        }

        /// <summary>
        /// A piece of the painting (inside <paramref name="polygon"/>, art px, top-left origin) saved as a transparent PNG and drawn
        /// over the characters, so furniture in front of them (a bed, a table) stays in front.
        /// </summary>
        static void Occluder(RectTransform art, string name, string srcPath, string outPath, Vector2[] polygon)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(System.IO.File.ReadAllBytes(srcPath));
            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, h - y - 0.5f); // to top-left origin
                    float d = PolygonDistance(p, polygon);  // negative inside
                    var c = px[y * w + x];
                    c.a = (byte)(255 * Mathf.Clamp01(0.5f - d));
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(outPath);
            var sprite = ImportArt(outPath, 2048);
            var img = UIFactory.Image(name, art, Color.white);
            img.sprite = sprite;
            img.raycastTarget = false;
            UIFactory.Stretch(img.rectTransform);
            img.gameObject.AddComponent<StudioForeground>(); // the main character stays behind it
        }

        static float PolygonDistance(Vector2 p, Vector2[] v)
        {
            float d = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                var e = v[j] - v[i];
                var wv = p - v[i];
                var b = wv - e * Mathf.Clamp01(Vector2.Dot(wv, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, b.magnitude);
                if ((v[i].y > p.y) != (v[j].y > p.y) && p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x) inside = !inside;
            }
            return inside ? -d : d;
        }

        /// <summary>
        /// Painted smoke inside <paramref name="area"/> (art px, top-left origin), limited to <paramref name="outline"/>:
        /// the smoke is detected by its lavender colour against the warm room, painted out of the room (a "clean" patch filled
        /// from the surrounding pixels) and cut into its own transparent image that WavySmoke animates on top.
        /// </summary>
        static void AnimatePaintedSmoke(RectTransform art, string srcPath, string outDir, RectInt area, Vector2[] outline)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(System.IO.File.ReadAllBytes(srcPath));
            int W = tex.width, H = tex.height, w = area.width, h = area.height;
            var all = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            var src = new Color[w * h];
            var mask = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int ax = area.x + x, ay = area.y + y;                // top-left origin
                    var c = (Color)all[(H - 1 - ay) * W + ax];
                    src[y * w + x] = c;
                    float blue = c.b - 0.55f * c.r - 0.2f * c.g;        // lavender smoke vs. red/brown room
                    float m = Mathf.Clamp01((blue - 0.03f) / 0.16f);
                    float inside = Mathf.Clamp01(1.5f - PolygonDistance(new Vector2(ax + 0.5f, ay + 0.5f), outline) / 2f);
                    mask[y * w + x] = m * inside;
                }
            // clean patch: diffuse the surrounding colours into the smoke
            var clean = (Color[])src.Clone();
            for (int it = 0; it < 120; it++)
                for (int y = 1; y < h - 1; y++)
                    for (int x = 1; x < w - 1; x++)
                    {
                        int i = y * w + x;
                        if (mask[i] < 0.02f) continue;
                        clean[i] = (clean[i - 1] + clean[i + 1] + clean[i - w] + clean[i + w]) * 0.25f;
                    }
            var cleanPx = new Color32[w * h];
            var smokePx = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x, ti = (h - 1 - y) * w + x;        // texture rows are bottom-up
                    float m = mask[i];
                    // the patch covers the smoke plus a soft rim so the edge never shows
                    float cover = Mathf.Clamp01(m * 6f);
                    var cl = clean[i]; cl.a = cover;
                    cleanPx[ti] = cl;
                    // smoke colour: un-mix the painted pixel from the room behind it
                    var s = m > 0.01f ? (src[i] - clean[i] * (1f - m)) / m : src[i];
                    s.a = m;
                    smokePx[ti] = new Color(Mathf.Clamp01(s.r), Mathf.Clamp01(s.g), Mathf.Clamp01(s.b), m);
                }
            string cleanPath = outDir + "/Smoke_Clean.png", smokePath = outDir + "/Smoke_Wisp.png";
            void Save(Color32[] px, string path)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.SetPixels32(px);
                t.Apply();
                System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            Save(cleanPx, cleanPath);
            Save(smokePx, smokePath);

            RectTransform Place(Image img)
            {
                img.raycastTarget = false;
                var r = img.rectTransform;
                r.anchorMin = ArtUV(area.xMin, area.yMax);
                r.anchorMax = ArtUV(area.xMax, area.yMin);
                r.offsetMin = r.offsetMax = Vector2.zero;
                return r;
            }
            var patch = UIFactory.Image("SmokeRemoved", art, Color.white);
            patch.sprite = ImportArt(cleanPath, 256);
            Place(patch);
            var go = UIFactory.Rect("PaintedSmoke", art).gameObject;
            go.AddComponent<CanvasRenderer>();
            var smoke = go.AddComponent<WavySmoke>();
            smoke.sprite = ImportArt(smokePath, 256);
            Place(smoke);
        }

        /// <summary>The producer cut-out, its chair emblem placed at <paramref name="chairEmblem"/> (art px, top-left origin).</summary>
        static GameObject[] CreateSeatedProducer(RectTransform art, Vector2 chairEmblem, float scale, bool flip = false)
        {
            var PaintedChairEmblem = chairEmblem;
            var ProducerScale = scale;
            if (!System.IO.File.Exists(Art.SeatedProducerCutout.BodyPath)) Art.SeatedProducerCutout.Build();
            var bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art.SeatedProducerCutout.BodyPath);
            var headSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art.SeatedProducerCutout.HeadPath);
            if (bodySprite == null || headSprite == null) return null;
            var size = new Vector2(Art.SeatedProducerCutout.W, Art.SeatedProducerCutout.H) * ProducerScale;

            Image Layer(string name, Sprite sprite, Vector2 pivotSrc)
            {
                var img = UIFactory.Image(name, art, Color.white);
                img.sprite = sprite;
                img.raycastTarget = false;
                var r = img.rectTransform;
                var off = (pivotSrc - Art.SeatedProducerCutout.Emblem) * ProducerScale;
                if (flip) off.x = -off.x; // mirrored: faces a desk on the left
                var pivotArt = PaintedChairEmblem + off;
                r.anchorMin = r.anchorMax = ArtUV(pivotArt.x, pivotArt.y);
                r.pivot = new Vector2(pivotSrc.x / Art.SeatedProducerCutout.W, 1f - pivotSrc.y / Art.SeatedProducerCutout.H);
                r.anchoredPosition = Vector2.zero;
                if (flip) r.localScale = new Vector3(-1f, 1f, 1f);
                var keep = img.gameObject.AddComponent<ArtPixelSize>();
                keep.sizeInArtPixels = size;
                keep.artSize = new Vector2(ArtW, ArtH);
                return img;
            }
            var body = Layer("ProducerBody", bodySprite, Art.SeatedProducerCutout.Emblem);
            var head = Layer("ProducerHead", headSprite, Art.SeatedProducerCutout.Neck);
            var view = body.gameObject.AddComponent<SeatedProducerView>();
            UIFactory.SetRef(view, "body", body.rectTransform);
            UIFactory.SetRef(view, "head", head.rectTransform);
            return new[] { body.gameObject, head.gameObject };
        }

        static void Glow(RectTransform art, string name, Sprite sprite, Color color, Vector2 uv, Vector2 sizePx, float angle,
            StudioLightGlow.Kind kind, float alpha, float variation, float period, bool sliced = false)
        {
            var img = UIFactory.Image(name, art, color);
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            // anchored to the painting (normalized), sized in painting pixels relative to the art rect
            rt.anchorMin = rt.anchorMax = uv;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0, 0, angle);
            var keep = img.gameObject.AddComponent<ArtPixelSize>();
            keep.sizeInArtPixels = sizePx;
            keep.artSize = new Vector2(ArtW, ArtH);
            var g = img.gameObject.AddComponent<StudioLightGlow>();
            g.kind = kind;
            g.baseAlpha = alpha;
            g.variation = variation;
            g.period = period;
        }

        /// <summary>Hides everything but the studio and MAKE BEAT (reversible: StudioFocusMode.focusOnStudio).</summary>
        static void CreateStudioFocus(GameObject canvas, RectTransform safeArea, RectTransform home, Component tutorial)
        {
            var focus = canvas.AddComponent<StudioFocusMode>();
            var so = new SerializedObject(focus);
            var list = new System.Collections.Generic.List<Object>();
            // top bar, boosts/goals and the bottom navigation stay visible; only the HOME screen is reduced to the studio
            foreach (Transform child in home)
            {
                if (child.name == "MakeBeatButton" || child.name == "MakeBeatGlow") continue;
                if (child.name == "StudioScene")
                {
                    // keep the studio name, next-goal / upgrade button, artists and STYLE; hide only the old drawn room
                    var room = child.Find("Room");
                    if (room != null) list.Add(room);
                    // compact: only NEXT GOAL (upgrade) and STYLE stay, in one row
                    foreach (var n in new[] { "StudioLabel", "Artists" })
                        if (child.Find(n) != null) list.Add(child.Find(n));
                    so.FindProperty("nextGoal").objectReferenceValue = child.Find("NextGoal");
                    so.FindProperty("styleButton").objectReferenceValue = child.Find("StyleButton");
                    so.FindProperty("cardMask").objectReferenceValue = child.GetComponent<Mask>();
                    continue;
                }
                list.Add(child);
            }
            UIFactory.SetArray(so.FindProperty("hidden"), list.ToArray());
            UIFactory.SetArray(so.FindProperty("paused"), tutorial != null ? new Object[] { tutorial } : new Object[0]);
            so.FindProperty("makeBeatButton").objectReferenceValue = home.Find("MakeBeatButton");
            so.FindProperty("makeBeatGlow").objectReferenceValue = home.Find("MakeBeatGlow");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
