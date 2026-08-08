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

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("npc");

            // npc.vesk → vesk billboard, etc.
            string portraitKey = id.StartsWith("npc.") ? id.Substring(4) : id;
            RuntimeArt.AttachBillboard(root.transform, portraitKey);

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

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.45f;
            visual.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.MaterialFor("pip");
            RuntimeArt.AttachBillboard(root.transform, "pip", height: 0.9f, width: 0.9f);

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
