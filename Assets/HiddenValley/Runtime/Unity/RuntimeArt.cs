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

        private static readonly Dictionary<string, Color> Fallback = new Dictionary<string, Color>
        {
            ["grey"] = new Color(0.55f, 0.55f, 0.55f),
            ["dark"] = new Color(0.35f, 0.35f, 0.38f),
            ["sand"] = new Color(0.76f, 0.68f, 0.50f),
            ["moss"] = new Color(0.45f, 0.62f, 0.35f),
            ["bark"] = new Color(0.45f, 0.36f, 0.28f),
            ["water"] = new Color(0.30f, 0.50f, 0.65f),
            ["npc"] = new Color(0.62f, 0.55f, 0.72f),
            ["pip"] = new Color(0.95f, 0.9f, 0.7f),
            ["player"] = new Color(0.85f, 0.8f, 0.7f),
        };

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
                material.color = Color.white;
            }
            else
            {
                material.color = Fallback.TryGetValue(key, out var c) ? c : Fallback["grey"];
                Debug.LogWarning($"[HiddenValley] Missing texture Art/Textures/mat_{key}");
            }

            MatCache[key] = material;
            return material;
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
