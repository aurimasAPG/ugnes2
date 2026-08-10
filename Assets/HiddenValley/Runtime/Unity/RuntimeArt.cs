using System.Collections.Generic;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Loads generated art from <c>Resources/Art</c>: tileable mat textures, character
    /// portraits, and inventory icons. Flat-color fallbacks keep grey-box playable if a
    /// texture is missing.
    /// </summary>
    public static class RuntimeArt
    {
        private static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> IconCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> CharCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// THE palette — single source for both the editor generators and the runtime
        /// spawner (they used to carry diverging copies). Tuned to the world bible's
        /// sentence: "beautiful in the specific way that wet stone and low cloud are
        /// beautiful… never twee" — cool desaturated minerals, warmth reserved for
        /// man-made light (lamp, kiln, Pip).
        /// </summary>
        public static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            ["grey"] = new Color(0.52f, 0.54f, 0.56f),   // wet field stone
            ["dark"] = new Color(0.30f, 0.32f, 0.36f),
            ["stone"] = new Color(0.46f, 0.49f, 0.52f),  // channel masonry
            ["slate"] = new Color(0.28f, 0.31f, 0.36f),  // roofs
            ["sand"] = new Color(0.68f, 0.63f, 0.52f),   // cold buff
            ["earth"] = new Color(0.42f, 0.38f, 0.32f),  // paths
            ["moss"] = new Color(0.42f, 0.52f, 0.38f),   // grey-green
            ["bark"] = new Color(0.38f, 0.32f, 0.27f),
            ["water"] = new Color(0.28f, 0.42f, 0.52f),
            ["ash"] = new Color(0.55f, 0.53f, 0.50f),    // lower shelf / Quietday
            ["lamp"] = new Color(1.00f, 0.85f, 0.60f),   // the warm accent
            ["npc"] = new Color(0.58f, 0.54f, 0.62f),
            ["pip"] = new Color(0.95f, 0.90f, 0.70f),
            ["player"] = new Color(0.80f, 0.76f, 0.68f),
        };

        /// <summary>Wet materials get specular life from the sun; everything else stays matte.</summary>
        public static readonly Dictionary<string, float> Smoothness = new Dictionary<string, float>
        {
            ["grey"] = 0.45f, ["stone"] = 0.45f, ["slate"] = 0.40f, ["dark"] = 0.35f,
            ["water"] = 0.75f,
        };

        public static Color PaletteColor(string key)
            => Palette.TryGetValue(key ?? "grey", out var c) ? c : Palette["grey"];

        public static float SmoothnessFor(string key)
            => Smoothness.TryGetValue(key ?? "", out var s) ? s : 0.08f;

        public static Material MaterialFor(string key)
        {
            key = string.IsNullOrEmpty(key) ? "grey" : key;
            if (MatCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var material = new Material(shader);
            var tex = Resources.Load<Texture2D>($"Art/Textures/mat_{key}");
            if (tex != null)
            {
                material.mainTexture = tex;
                // Textures are near-white detail maps; the palette still owns the hue.
                material.color = PaletteColor(key);
            }
            else
            {
                material.color = PaletteColor(key);
                Debug.LogWarning($"[HiddenValley] Missing texture Art/Textures/mat_{key}");
            }

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", SmoothnessFor(key));

            // Lamp surfaces emit: windows must glow at night or the village dies at
            // dusk (night screenshot, pass 5 — dark facades under a working sky).
            if (key == "lamp" && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.45f) * 1.35f);
            }

            MatCache[key] = material;
            return material;
        }

        private static Material _waterMaterial;

        /// <summary>
        /// Transparent flowing water. Translucency is functionally load-bearing: the
        /// drowned lens — the slice's one undocumented solution — must be visible
        /// through the pool surface to be findable "by anyone who looks in the water".
        /// </summary>
        public static Material WaterMaterial()
        {
            if (_waterMaterial != null) return _waterMaterial;

            var material = new Material(MaterialFor("water")); // copy: base stays opaque
            material.SetFloat("_Surface", 1f); // URP Lit: transparent
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);

            var color = material.color;
            color.a = 0.72f;
            material.color = color;

            _waterMaterial = material;
            return _waterMaterial;
        }

        public static Texture2D Icon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (IconCache.TryGetValue(itemId, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>($"Art/Icons/{itemId}");
            IconCache[itemId] = tex;
            return tex;
        }

        public static Texture2D Character(string characterKey)
        {
            if (string.IsNullOrEmpty(characterKey)) return null;
            if (CharCache.TryGetValue(characterKey, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>($"Art/Characters/char_{characterKey}");
            CharCache[characterKey] = tex;
            return tex;
        }

        /// <summary>
        /// Adds a camera-facing portrait quad so grey-box capsules read as characters.
        /// </summary>
        public static void AttachBillboard(Transform host, string characterKey, float height = 1.9f, float width = 1.1f)
        {
            var tex = Character(characterKey);
            if (tex == null || host == null) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Portrait";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(0f, height * 0.55f, 0.05f);
            go.transform.localScale = new Vector3(width, height, 1f);

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.mainTexture = tex;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<Billboard>();
        }
    }

    /// <summary>Keeps a portrait quad facing the main camera on Y only.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 to = transform.position - cam.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }
    }

    /// <summary>Attaches the player portrait once Resources are available.</summary>
    public sealed class PlayerPortrait : MonoBehaviour
    {
        private void Start() => RuntimeArt.AttachBillboard(transform, "player");
    }
}
