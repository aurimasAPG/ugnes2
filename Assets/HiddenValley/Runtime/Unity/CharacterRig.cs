using System.Collections.Generic;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Procedural character bodies — the art tier for a fully generated pipeline. Each
    /// character is a handful of primitives (body, head, hood, arms, one prop) with
    /// proportions from a per-key table, so the three NPCs differ in silhouette at the
    /// camera's 6 m: Vesk broad with an apron, Orrel tall and narrow with a ledger,
    /// Coll stooped with a gear charm. Built onto the existing "Visual" anchor, so
    /// CapsuleAnimator's lean/gait/squash drives the whole body unchanged.
    ///
    /// Replaces the portrait billboards in the world (portraits remain in the dialogue
    /// panel). IP note: proportions are stocky-but-not-chibi, hoods not hats, and no
    /// color-signature pairs lifted from any known property.
    /// </summary>
    public static class CharacterRig
    {
        private struct Proportions
        {
            public float width, height, headSize, stoop;
            public string bodyMat, accentMat;
            public string prop; // apron | ledger | gear | none
        }

        private static readonly Dictionary<string, Proportions> Table = new Dictionary<string, Proportions>
        {
            ["player"] = new Proportions { width = 1.00f, height = 1.05f, headSize = 1.0f, stoop = 0, bodyMat = "player", accentMat = "moss", prop = "none" },
            ["vesk"] = new Proportions { width = 1.35f, height = 0.95f, headSize = 1.05f, stoop = 2f, bodyMat = "npc", accentMat = "bark", prop = "apron" },
            ["orrel"] = new Proportions { width = 0.80f, height = 1.25f, headSize = 0.9f, stoop = 0, bodyMat = "npc", accentMat = "slate", prop = "ledger" },
            ["coll"] = new Proportions { width = 1.05f, height = 0.88f, headSize = 1.1f, stoop = 14f, bodyMat = "npc", accentMat = "dark", prop = "gear" },
        };

        /// <summary>Builds the body as children of <paramref name="anchor"/>, stripping
        /// any existing placeholder mesh/billboard from it first. When a painted sprite
        /// exists under Resources/Art/Sprites/char_{key}, it wins: a lit alpha-cutout
        /// billboard (the Don't Starve stance — 2D characters in a 3D world read as art
        /// direction when every character commits to it). The procedural rig remains the
        /// fallback for keys with no painting yet.</summary>
        public static void Build(Transform anchor, string key)
        {
            if (anchor == null) return;

            var sprite = Resources.Load<Texture2D>($"Art/Sprites/char_{key}");
            if (sprite != null)
            {
                BuildSprite(anchor, key, sprite);
                return;
            }

            // Strip the placeholder capsule and any world billboard.
            var meshRenderer = anchor.GetComponent<MeshRenderer>();
            if (meshRenderer != null) Object.Destroy(meshRenderer);
            var meshFilter = anchor.GetComponent<MeshFilter>();
            if (meshFilter != null) Object.Destroy(meshFilter);
            var portrait = anchor.parent != null ? anchor.parent.Find("Portrait") : null;
            if (portrait != null) Object.Destroy(portrait.gameObject);

            if (!Table.TryGetValue(key, out var p))
                p = Table["player"];

            int layer = anchor.gameObject.layer;
            // The anchor arrives scaled (the old capsule's 0.7/0.9/0.7); build in a
            // neutral child so proportions are authored in meters.
            anchor.localScale = Vector3.one;
            anchor.localPosition = Vector3.zero;

            var body = Part(anchor, layer, PrimitiveType.Capsule, $"{key}.body", p.bodyMat,
                new Vector3(0, 0.78f * p.height, 0),
                new Vector3(0.62f * p.width, 0.62f * p.height, 0.5f * p.width));

            // Stoop pitches the whole body forward slightly — Coll listens to machinery.
            body.transform.localRotation = Quaternion.Euler(p.stoop, 0, 0);

            Part(anchor, layer, PrimitiveType.Sphere, $"{key}.head", p.bodyMat,
                new Vector3(0, 1.52f * p.height, 0.04f * p.stoop / 14f),
                Vector3.one * (0.46f * p.headSize));

            // Hood: a flattened sphere sitting back on the head — silhouette, not a hat.
            Part(anchor, layer, PrimitiveType.Sphere, $"{key}.hood", p.accentMat,
                new Vector3(0, 1.58f * p.height, -0.07f),
                new Vector3(0.52f * p.headSize, 0.42f * p.headSize, 0.52f * p.headSize));

            Part(anchor, layer, PrimitiveType.Capsule, $"{key}.arm.l", p.bodyMat,
                new Vector3(-0.38f * p.width, 0.85f * p.height, 0),
                new Vector3(0.16f, 0.42f * p.height, 0.16f));
            Part(anchor, layer, PrimitiveType.Capsule, $"{key}.arm.r", p.bodyMat,
                new Vector3(0.38f * p.width, 0.85f * p.height, 0),
                new Vector3(0.16f, 0.42f * p.height, 0.16f));

            switch (p.prop)
            {
                case "apron":
                    Part(anchor, layer, PrimitiveType.Cube, $"{key}.apron", p.accentMat,
                        new Vector3(0, 0.62f * p.height, 0.26f * p.width),
                        new Vector3(0.5f * p.width, 0.62f * p.height, 0.06f));
                    break;
                case "ledger":
                    Part(anchor, layer, PrimitiveType.Cube, $"{key}.ledger", "sand",
                        new Vector3(-0.42f * p.width, 0.72f * p.height, 0.12f),
                        new Vector3(0.08f, 0.3f, 0.22f));
                    break;
                case "gear":
                    var charm = Part(anchor, layer, PrimitiveType.Cylinder, $"{key}.charm", "lamp",
                        new Vector3(0, 1.05f * p.height, 0.3f * p.width),
                        new Vector3(0.14f, 0.02f, 0.14f));
                    charm.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
            }
        }

        /// <summary>Painted-sprite heights: silhouettes differ by stature too.</summary>
        private static readonly Dictionary<string, float> SpriteHeight = new Dictionary<string, float>
        {
            ["player"] = 1.80f, ["vesk"] = 1.78f, ["orrel"] = 1.96f, ["coll"] = 1.58f, ["pip"] = 0.55f,
        };

        public static void BuildSprite(Transform anchor, string key, Texture2D sprite)
        {
            // Strip any placeholder mesh/billboard.
            var meshRenderer = anchor.GetComponent<MeshRenderer>();
            if (meshRenderer != null) Object.Destroy(meshRenderer);
            var meshFilter = anchor.GetComponent<MeshFilter>();
            if (meshFilter != null) Object.Destroy(meshFilter);
            var oldPortrait = anchor.parent != null ? anchor.parent.Find("Portrait") : null;
            if (oldPortrait != null) Object.Destroy(oldPortrait.gameObject);

            anchor.localScale = Vector3.one;
            anchor.localPosition = Vector3.zero;

            float height = SpriteHeight.TryGetValue(key, out var h) ? h : 1.8f;
            float width = height * ((float)sprite.width / sprite.height);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"{key}.sprite";
            quad.layer = anchor.gameObject.layer;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(anchor, false);
            quad.transform.localPosition = new Vector3(0, height / 2f, 0);
            quad.transform.localScale = new Vector3(width, height, 1f);

            // Lit + alpha clip via the SpriteLit material ASSET — instantiating the
            // asset keeps the alpha-test variant that build stripping would otherwise
            // remove (runtime-built materials render as opaque black slabs in players).
            var template = Resources.Load<Material>("Art/SpriteLit");
            var material = template != null
                ? new Material(template)
                : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.mainTexture = sprite;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", sprite);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            quad.AddComponent<Billboard>();
        }

        private static GameObject Part(Transform parent, int layer, PrimitiveType type,
            string name, string mat, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = layer;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor(mat);
            return go;
        }
    }
}
