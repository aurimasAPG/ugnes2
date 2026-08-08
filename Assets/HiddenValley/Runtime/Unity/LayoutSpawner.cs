using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HiddenValley.Unity
{
    /// <summary>
    /// Spawns world objects, NPCs and Pip from the layout JSON at runtime.
    ///
    /// Why this exists: packing every Heartwood object into the scene file produces a
    /// level0 the player refuses to load ("corrupted", "Position out of bounds") on both
    /// macOS and iOS — reproduced 2026-08-07. Bisect showed every content group alone is
    /// fine, every pair is fine, and only the full combination corrupts. Spawning the
    /// dynamic content after load keeps the serialized scene at the known-good "shell"
    /// size (blocks + player + systems), and keeps the data-driven property: moving an NPC
    /// is still a JSON edit with zero C# changes.
    ///
    /// Static grey-box geometry (blocks) stays in the scene: the shell profile packs and
    /// runs cleanly, and static colliders do not need to be recreated every boot.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class LayoutSpawner : MonoBehaviour
    {
        [SerializeField] private string layoutRelativePath = "Layout/heartwood.json";
        [SerializeField] private Transform player;

        private const int ActorLayer = 2; // Ignore Raycast — matches VillageSetup

        public int SpawnedWorldObjects { get; private set; }
        public int SpawnedNpcs { get; private set; }
        public bool SpawnedPip { get; private set; }

        private void Awake()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            var path = Path.Combine(Application.streamingAssetsPath, layoutRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogError($"[HiddenValley] Layout missing at {path}.");
                WriteBootMarker($"FAIL layout missing: {path}");
                return;
            }

            var layout = JObject.Parse(File.ReadAllText(path));
            float start = Time.realtimeSinceStartup;

            foreach (var jt in layout["worldObjects"] ?? new JArray())
                SpawnWorldObject((JObject)jt);

            if (player != null)
                SpawnPip(layout, player);

            foreach (var jn in layout["npcs"] ?? new JArray())
                SpawnNpc((JObject)jn);

            SpawnRuntimeBlocks(layout);
            SpawnFx(layout);
            SpawnZones(layout);
            SpawnScatter(layout);

            float ms = (Time.realtimeSinceStartup - start) * 1000f;
            string summary =
                $"[HiddenValley] Runtime layout spawn: {ms:0} ms — " +
                $"wo={SpawnedWorldObjects} npc={SpawnedNpcs} pip={SpawnedPip} from {layoutRelativePath}.";
            Debug.Log(summary);
            WriteBootMarker($"OK {summary}");
        }

        // ---- world objects ----------------------------------------------------

        private void SpawnWorldObject(JObject spec)
        {
            string id = spec["id"]?.Value<string>();
            if (string.IsNullOrEmpty(id)) return;

            var root = new GameObject(id);
            root.transform.position = Pos(spec["pos"]);

            var visual = MakePrimitive(spec, "Visual");
            visual.transform.SetParent(root.transform, true);

            bool solid = spec["solid"]?.Value<bool>() ?? true;
            if (!solid)
            {
                var col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            var binder = root.AddComponent<WorldObjectBinder>();
            Collider blocking = null;
            if (spec["blocking"]?.Value<bool>() == true && solid)
                blocking = visual.GetComponent<Collider>();
            binder.Configure(id, blocking);

            var zoneGo = new GameObject("Zone") { layer = ActorLayer };
            zoneGo.transform.SetParent(root.transform, false);
            zoneGo.transform.localPosition = Vector3.up * 0.5f;
            var zone = zoneGo.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.radius = 2.4f;
            zoneGo.AddComponent<InteractionZone>().Configure(binder);

            SpawnedWorldObjects++;
        }

        // ---- NPCs -------------------------------------------------------------

        private void SpawnNpc(JObject spec)
        {
            string id = spec["id"]?.Value<string>();
            if (string.IsNullOrEmpty(id)) return;

            var root = new GameObject(id) { layer = ActorLayer };
            root.transform.position = Pos(spec["pos"]);

            var visual = new GameObject("Visual") { layer = ActorLayer };
            visual.transform.SetParent(root.transform, false);

            // npc.vesk → the vesk rig. Billboards retired; portraits live in dialogue.
            string rigKey = id.StartsWith("npc.") ? id.Substring(4) : id;
            CharacterRig.Build(visual.transform, rigKey);

            var waypoints = (JObject)spec["waypoints"] ?? new JObject();
            var waypointRoot = GameObject.Find("Waypoints") ?? new GameObject("Waypoints");
            var list = new List<NpcBinder.Waypoint>();

            foreach (var pair in waypoints)
            {
                var point = new GameObject(pair.Key);
                point.transform.SetParent(waypointRoot.transform, false);
                point.transform.position = Pos(pair.Value);
                list.Add(new NpcBinder.Waypoint { id = pair.Key, point = point.transform });
            }

            root.AddComponent<NpcBinder>().Configure(id, list);
            SpawnedNpcs++;
        }

        // ---- Pip --------------------------------------------------------------

        private void SpawnPip(JObject layout, Transform follow)
        {
            var root = new GameObject("Pip") { layer = ActorLayer };
            root.transform.position = Pos(layout["pip"]?["pos"]);

            // A moth, not an orb. Painted sprite when one exists; primitive moth
            // (body + flapped wing quads) as fallback.
            var visual = new GameObject("Visual") { layer = ActorLayer };
            visual.transform.SetParent(root.transform, false);

            var pipSprite = Resources.Load<Texture2D>("Art/Sprites/char_pip");
            if (pipSprite != null)
            {
                CharacterRig.BuildSprite(visual.transform, "pip", pipSprite);
            }
            else
            {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.layer = ActorLayer;
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(visual.transform, false);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
            body.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("pip");

            for (int side = -1; side <= 1; side += 2)
            {
                var wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wing.name = side < 0 ? "Wing.L" : "Wing.R";
                wing.layer = ActorLayer;
                Destroy(wing.GetComponent<Collider>());
                wing.transform.SetParent(visual.transform, false);
                wing.transform.localPosition = new Vector3(side * 0.10f, 0.03f, 0);
                wing.transform.localScale = new Vector3(0.22f, 0.3f, 1f);
                wing.transform.localRotation = Quaternion.Euler(90, 0, side * 20f);
                var wingRenderer = wing.GetComponent<MeshRenderer>();
                wingRenderer.sharedMaterial = RuntimeArt.MaterialFor("pip");
                wingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            }

            var lampGo = new GameObject("Lamp") { layer = ActorLayer };
            lampGo.transform.SetParent(root.transform, false);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 9f;
            lamp.color = new Color(1f, 0.92f, 0.75f);
            lamp.intensity = 1.4f;

            root.AddComponent<PipCompanion>().Configure(follow, lamp);
            root.AddComponent<NpcBinder>().Configure("npc.pip");
            SpawnedPip = true;
        }

        // ---- runtime water / fx / zones / scatter -----------------------------

        /// <summary>Blocks tagged "runtime": true (water) spawn here with the flow shader
        /// instead of serializing into the scene.</summary>
        private void SpawnRuntimeBlocks(JObject layout)
        {
            foreach (var jt in layout["blocks"] ?? new JArray())
            {
                var spec = (JObject)jt;
                if (spec["runtime"]?.Value<bool>() != true) continue;

                var go = MakePrimitive(spec, spec["name"]?.Value<string>() ?? "Water");
                var collider = go.GetComponent<Collider>();
                if (collider != null) Destroy(collider); // you can wade; barriers are world objects

                if ((spec["mat"]?.Value<string>() ?? "") == "water")
                {
                    go.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.WaterMaterial();
                    var flow = spec["flow"] is JArray f && f.Count >= 2
                        ? new Vector2(f[0].Value<float>(), f[1].Value<float>())
                        : Vector2.zero;
                    WaterFlow.Attach(go, flow, flow == Vector2.zero ? 0.008f : 0.03f);
                }
            }
        }

        /// <summary>"fx" fields on blocks and world objects: smoke columns, ember lights.</summary>
        private void SpawnFx(JObject layout)
        {
            void Handle(JObject spec)
            {
                string fx = spec["fx"]?.Value<string>();
                if (string.IsNullOrEmpty(fx)) return;

                Vector3 at = Pos(spec["pos"]);
                Vector3 size = Size(spec["size"], Vector3.one);

                switch (fx)
                {
                    case "smoke":
                        SpawnSmoke(at + Vector3.up * (size.y * 1.6f + 3.5f));
                        break;
                    case "emberlight":
                        var host = GameObject.Find(spec["id"]?.Value<string>() ?? spec["name"]?.Value<string>() ?? "");
                        FlickerLight.Attach(host != null ? host : gameObject,
                            new Color(1f, 0.62f, 0.32f), 1.6f, 9f);
                        break;
                }
            }

            foreach (var jt in layout["blocks"] ?? new JArray()) Handle((JObject)jt);
            foreach (var jt in layout["worldObjects"] ?? new JArray()) Handle((JObject)jt);
        }

        /// <summary>The kiln smoke column — a landmark readable from the village entrance,
        /// and narratively load-bearing (smoke presence is story signal).</summary>
        private void SpawnSmoke(Vector3 at)
        {
            var go = new GameObject("Smoke");
            go.transform.position = at;

            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.startLifetime = 9f;
            main.startSpeed = 0.9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startColor = new Color(0.62f, 0.62f, 0.64f, 0.35f);
            main.maxParticles = 40;

            var emission = system.emission;
            emission.rateOverTime = 3.5f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.25f;

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(0.25f); // drift with the prevailing wind

            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.Linear(0, 0.4f, 1, 1.6f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", new Color(1, 1, 1, 0.3f));
                renderer.material = material;
            }
        }

        private void SpawnZones(JObject layout)
        {
            foreach (var jt in layout["zones"] ?? new JArray())
            {
                var spec = (JObject)jt;
                if (spec["bounds"] is not JArray b || b.Count < 6) continue;

                ZoneAmbience.Spawn(
                    spec["id"]?.Value<string>() ?? "zone",
                    new Vector3(b[0].Value<float>(), b[1].Value<float>(), b[2].Value<float>()),
                    new Vector3(b[3].Value<float>(), b[4].Value<float>(), b[5].Value<float>()),
                    Tint(spec["fogTint"]),
                    spec["fogDensityMul"]?.Value<float>() ?? 1f,
                    Tint(spec["ambientTint"]));
            }
        }

        private static Color Tint(JToken t)
        {
            if (t is not JArray a || a.Count < 3) return Color.white;
            return new Color(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>());
        }

        /// <summary>
        /// Seeded ground cover — deterministic from the seed so every walk sees the same
        /// world. No colliders, no shadows; a capped count of tiny primitives.
        /// </summary>
        private void SpawnScatter(JObject layout)
        {
            var root = new GameObject("Scatter");
            int total = 0;

            foreach (var jt in layout["scatter"] ?? new JArray())
            {
                var spec = (JObject)jt;
                if (spec["region"] is not JArray r || r.Count < 4) continue;

                float cx = r[0].Value<float>(), cz = r[1].Value<float>();
                float w = r[2].Value<float>(), d = r[3].Value<float>();
                float y = spec["y"]?.Value<float>() ?? 0f;
                int count = Mathf.Min(spec["count"]?.Value<int>() ?? 100, 600);
                var random = new System.Random(spec["seed"]?.Value<int>() ?? 1);

                var items = new List<string>();
                foreach (var item in spec["items"] as JArray ?? new JArray("tuft"))
                    items.Add(item.Value<string>());

                for (int i = 0; i < count; i++)
                {
                    float x = cx + ((float)random.NextDouble() - 0.5f) * w;
                    float z = cz + ((float)random.NextDouble() - 0.5f) * d;

                    // Snap to the actual ground: terrain plates tilt and offset, and a
                    // tuft floating a hand's width above its shadow reads as a bug from
                    // ten meters away (pass 1 screenshot).
                    Vector3 at = new Vector3(x, y, z);
                    if (Physics.Raycast(new Vector3(x, y + 4f, z), Vector3.down,
                            out var hit, 12f, ~(1 << ActorLayer), QueryTriggerInteraction.Ignore))
                        at = hit.point;

                    string kind = items[random.Next(items.Count)];
                    SpawnScatterItem(root.transform, kind, at, random);
                    total++;
                }
            }

            if (total > 0)
            {
                root.AddComponent<WindSway>();
                Debug.Log($"[HiddenValley] Scatter: {total} instances.");
            }
        }

        private void SpawnScatterItem(Transform parent, string kind, Vector3 at, System.Random random)
        {
            GameObject go;
            float scale = 0.7f + (float)random.NextDouble() * 0.7f;

            if (kind == "stone")
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "stone";
                go.transform.localScale = new Vector3(0.35f, 0.18f, 0.3f) * scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("stone");
                at.y += 0.06f;
            }
            else // tuft: two crossed thin slabs
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "tuft";
                go.transform.localScale = new Vector3(0.3f, 0.22f, 0.035f) * scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("moss");
                at.y += 0.1f * scale;

                var cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cross.name = "tuft";
                cross.transform.localScale = go.transform.localScale;
                cross.transform.rotation = Quaternion.Euler(0, 90f, 0);
                cross.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("moss");
                Prep(cross, at, parent);
            }

            go.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
            Prep(go, at, parent);
        }

        private static void Prep(GameObject go, Vector3 at, Transform parent)
        {
            go.transform.position = at;
            go.transform.SetParent(parent, true);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // ---- primitives / materials -------------------------------------------

        private GameObject MakePrimitive(JObject spec, string name)
        {
            var shape = spec["shape"]?.Value<string>() switch
            {
                "cylinder" => PrimitiveType.Cylinder,
                "sphere" => PrimitiveType.Sphere,
                _ => PrimitiveType.Cube
            };

            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.position = Pos(spec["pos"]);
            go.transform.localScale = Size(spec["size"], Vector3.one);

            var rot = spec["rot"];
            if (rot != null)
                go.transform.rotation = Quaternion.Euler(Pos(rot));

            string mat = spec["mat"]?.Value<string>() ?? "grey";
            go.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor(mat);
            return go;
        }

        private static void WriteBootMarker(string line)
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, "boot-marker.txt");
                File.WriteAllText(path, $"{System.DateTime.UtcNow:o}\n{line}\n");
            }
            catch
            {
                // Marker is diagnostic only — never take the game down.
            }
        }

        private static Vector3 Pos(JToken t)
        {
            if (t is not JArray a || a.Count < 3) return Vector3.zero;
            return new Vector3(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>());
        }

        private static Vector3 Size(JToken t, Vector3 fallback)
            => t is JArray a && a.Count >= 3 ? Pos(t) : fallback;
    }
}
