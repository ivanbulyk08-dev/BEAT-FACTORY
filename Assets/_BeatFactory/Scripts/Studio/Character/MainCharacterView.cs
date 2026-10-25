using UnityEngine;
using UnityEngine.UI;

namespace BeatFactory
{
    /// <summary>
    /// The character's visuals: a contact shadow under him and the body sprite drawn with the CharacterLit shader (room
    /// ambient, key light, coloured rim). The root is placed/mirrored by StudioCharacterPlacement; this only sets the
    /// sprite, shadow and the light values — once per pose change, nothing per frame.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainCharacterView : MonoBehaviour
    {
        [SerializeField] Image body;
        [SerializeField] Image shadow;
        [Tooltip("STORE clothes drawn over the body (same rect): pants, then top.")]
        [SerializeField] Image pantsLayer;
        [SerializeField] Image topLayer;
        [SerializeField] StoreCatalog store;

        static readonly int AmbientId = Shader.PropertyToID("_Ambient");
        static readonly int KeyColorId = Shader.PropertyToID("_KeyColor");
        static readonly int KeyStrengthId = Shader.PropertyToID("_KeyStrength");
        static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        static readonly int RimWidthId = Shader.PropertyToID("_RimWidth");
        static readonly int LightDirId = Shader.PropertyToID("_LightDir");
        static readonly int FloorShadeId = Shader.PropertyToID("_FloorShade");
        static readonly int DesaturateId = Shader.PropertyToID("_Desaturate");

        Material material; // own copy: the shared asset stays untouched
        Material clothesMaterial; // same light, no rim (the clothes' own edges are inside the figure)
        CharacterPoseLibrary.Entry shownPose;
        GameManager listening;

        public Image Body => body;

        public void Init(Image bodyImage, Image shadowImage)
        {
            body = bodyImage;
            shadow = shadowImage;
        }

        void OnEnable()
        {
            if (listening == null && GameManager.Instance != null)
            {
                listening = GameManager.Instance;
                listening.Wardrobe.Changed += OnWardrobe;
            }
            ApplyClothes();
        }

        void OnDestroy()
        {
            if (listening != null) listening.Wardrobe.Changed -= OnWardrobe;
            if (material != null) Destroy(material);
            if (clothesMaterial != null) Destroy(clothesMaterial);
        }

        void OnWardrobe(string key)
        {
            if (string.IsNullOrEmpty(key) || key == ArtistWardrobe.PlayerKey) ApplyClothes();
        }

        /// <summary>The bought clothes he wears, for the pose shown now (nothing worn / no art for this pose = hidden).</summary>
        void ApplyClothes()
        {
            if (body == null || shownPose == null) return;
            // an item drawn as the whole character replaces his body (and the other clothes) where it has art
            Sprite full = null;
            var gm = GameManager.Instance;
            if (gm != null && store != null)
            {
                // the top wins when both the top and the pants are drawn as whole figures
                foreach (var slot in new[] { WardrobeSlot.Top, WardrobeSlot.Pants })
                {
                    var item = store.Find(gm.Wardrobe.PlayerWorn(slot));
                    if (item != null && item.fullFigure) full = item.For(shownPose.pose);
                    if (full != null) break;
                }
            }
            var r = body.rectTransform;
            if (full != null)
            {
                body.sprite = full;
                // same height as the pose; wider/narrower figure keeps its proportions around the ground point
                var orig = shownPose.sprite.rect;
                float k = (full.rect.width / full.rect.height) / (orig.width / orig.height);
                float gx = shownPose.groundPivot.x;
                r.anchorMin = new Vector2(gx - gx * k, 0f);
                r.anchorMax = new Vector2(gx + (1f - gx) * k, 1f);
                r.offsetMin = r.offsetMax = Vector2.zero;
                if (pantsLayer != null) pantsLayer.enabled = false;
                if (topLayer != null) topLayer.enabled = false;
                return;
            }
            body.sprite = shownPose.sprite;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            Layer(pantsLayer, WardrobeSlot.Pants);
            Layer(topLayer, WardrobeSlot.Top);
        }

        void Layer(Image img, WardrobeSlot slot)
        {
            if (img == null) return;
            Sprite s = null;
            var gm = GameManager.Instance;
            if (gm != null && store != null && shownPose != null)
            {
                var item = store.Find(gm.Wardrobe.PlayerWorn(slot));
                if (item != null) s = item.For(shownPose.pose);
            }
            img.sprite = s;
            img.enabled = s != null;
        }

        public void Show(CharacterPoseLibrary.Entry pose, CharacterLighting light, bool mirror)
        {
            body.sprite = pose.sprite;
            shownPose = pose;
            ApplyClothes();
            if (light == null) light = new CharacterLighting();
            if (body.material != null && body.material.shader.name == "BeatFactory/UI/CharacterLit")
            {
                if (material == null || body.material != material)
                {
                    material = new Material(body.material) { name = "CharacterLit (instance)" };
                    body.material = material;
                }
                // the light direction is given on screen; the shader works in sprite space (mirrored with the figure)
                var dir = light.lightDirection.sqrMagnitude > 0.0001f ? light.lightDirection.normalized : Vector2.up;
                if (mirror) dir.x = -dir.x;
                material.SetColor(AmbientId, light.ambient);
                material.SetColor(KeyColorId, light.keyColor);
                material.SetFloat(KeyStrengthId, light.keyStrength);
                material.SetColor(RimColorId, light.rimColor);
                material.SetFloat(RimStrengthId, light.rimStrength);
                material.SetFloat(RimWidthId, light.rimWidth);
                material.SetVector(LightDirId, new Vector4(dir.x, dir.y, 0, 0));
                material.SetFloat(FloorShadeId, light.floorShade);
                material.SetFloat(DesaturateId, light.desaturate);
                body.SetMaterialDirty();
                if (clothesMaterial == null) clothesMaterial = new Material(material) { name = "CharacterLit clothes (instance)" };
                clothesMaterial.CopyPropertiesFromMaterial(material);
                clothesMaterial.SetFloat(RimStrengthId, 0f);
                foreach (var l in new[] { pantsLayer, topLayer })
                    if (l != null) { l.material = clothesMaterial; l.SetMaterialDirty(); }
            }
            PlaceShadow(pose, light.shadowAlpha);
        }

        /// <summary>Soft ellipse on the floor: narrow under the feet when standing, long under the body when sitting.</summary>
        void PlaceShadow(CharacterPoseLibrary.Entry pose, float alpha)
        {
            if (shadow == null) return;
            shadow.enabled = alpha > 0.01f;
            var c = shadow.color;
            c.a = alpha;
            shadow.color = c;
            var r = shadow.rectTransform;
            bool standing = pose.groundPivot.y < 0.08f;
            if (standing)
            {
                r.anchorMin = new Vector2(pose.groundPivot.x - 0.42f, -0.025f);
                r.anchorMax = new Vector2(pose.groundPivot.x + 0.42f, 0.055f);
            }
            else
            {
                r.anchorMin = new Vector2(-0.02f, -0.03f);
                r.anchorMax = new Vector2(1.02f, Mathf.Max(0.12f, pose.groundPivot.y + 0.06f));
            }
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
