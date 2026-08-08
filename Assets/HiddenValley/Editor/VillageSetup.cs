using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HiddenValley.Unity;

namespace HiddenValley.Editor
{
    /// <summary>
    /// Generates the playable slice scene — Heartwood, the climb, the Ash Shelf and the
    /// lower shelf — from <c>Assets/HiddenValley/Layout/heartwood.json</c>, runnable
    /// headless:
    ///
    ///   Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.VillageSetup.GenerateHeartwood
    ///
    /// This is the project's data-driven property applied to space. Content JSON already
    /// decides WHAT exists; the layout JSON decides WHERE. Moving the trade post, adding a
    /// tree, or re-cutting the climb for the interest-density walk is a JSON edit and a
    /// re-run — the scene file itself is never authored by hand and can always be deleted.
    ///
    /// Everything the binders need is wired here: each world object id gets geometry, an
    /// interaction zone and a WorldObjectBinder; each NPC gets schedule waypoints and an
    /// NpcBinder (direct walk, no NavMesh — see phase log 2026-08-07). Player, Pip, camera
    /// and controls ride along; GameHud spawns at runtime from TouchControls.
    /// </summary>
    public static class VillageSetup
    {
        // Single source of truth: the same file LayoutSpawner reads at runtime. The old
        // Assets/HiddenValley/Layout copy drifted from this one and is deleted.
        private const string LayoutPath = "Assets/StreamingAssets/Layout/heartwood.json";
        private const string ScenePath = "Assets/HiddenValley/Scenes/Heartwood.unity";
        private const string GreyboxScenePath = "Assets/HiddenValley/Scenes/Greybox.unity";
        private const string SettingsDir = "Assets/HiddenValley/Settings";

        /// <summary>Actors live on this layer so the NavMesh bake and the camera's
        /// occlusion cast see only the world, never the people standing in it.</summary>
        private const int ActorLayer = 2; // Ignore Raycast

        // The palette lives in RuntimeArt (single source for editor + runtime).

        [MenuItem("Hidden Valley/Setup/Heartwood Scene")]
        public static void GenerateHeartwood()
        {
            if (!File.Exists(LayoutPath))
                throw new FileNotFoundException($"No layout at {LayoutPath}.");

            var layout = JObject.Parse(File.ReadAllText(LayoutPath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Static geometry only. World objects, NPCs and Pip are spawned at runtime by
            // LayoutSpawner from StreamingAssets/Layout/heartwood.json — packing them into
            // the scene produces a corrupted level0 (full-combination bisect, 2026-08-07).
            // Kit entries expand into multi-primitive buildings; "runtime": true blocks
            // (water) are skipped here and spawned live with their flow shader.
            foreach (var block in layout["blocks"] ?? new JArray())
            {
                var spec = (JObject)block;
                if (spec["runtime"]?.Value<bool>() == true) continue;
                foreach (var piece in LayoutKits.Expand(spec))
                    Primitive(piece, piece["name"]?.Value<string>() ?? "Block");
            }

            int pathIndex = 0;
            foreach (var path in layout["paths"] ?? new JArray())
                foreach (var piece in LayoutKits.ExpandPath((JObject)path, pathIndex++))
                    Primitive(piece, piece["name"]?.Value<string>() ?? "Path");

            // Light — same single-sun rig as the grey-box room.
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0);

            var player = Player(Position(layout["player"]?["pos"]));
            var cameraGo = CameraRig(player);
            Systems(player, cameraGo);

            // Deliberately NO NavMesh anywhere: a scene containing a NavMeshSurface —
            // baked or unbaked — packs into a level0 the iOS player rejects as
            // corrupted (bisection, 2026-08-07). NPCs walk their waypoints directly;
            // see NpcBinder.Update.

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            if (File.Exists(GreyboxScenePath))
                scenes.Add(new EditorBuildSettingsScene(GreyboxScenePath, false));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log($"[HiddenValley] Heartwood generated at {ScenePath} and set as the build scene.");
        }

        // ---- pieces -----------------------------------------------------------

        private static GameObject Primitive(JObject spec, string name)
        {
            var shape = spec["shape"]?.Value<string>() switch
            {
                "cylinder" => PrimitiveType.Cylinder,
                "sphere" => PrimitiveType.Sphere,
                _ => PrimitiveType.Cube
            };

            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.transform.position = Position(spec["pos"]);
            go.transform.localScale = Size(spec["size"], Vector3.one);

            var rot = spec["rot"];
            if (rot != null)
                go.transform.rotation = Quaternion.Euler(Position(rot));

            string mat = spec["mat"]?.Value<string>() ?? "grey";
            go.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetup.Material(
                $"{SettingsDir}/Mat_{mat}.mat", RuntimeArt.PaletteColor(mat));

            return go;
        }

        private static void WorldObject(JObject spec)
        {
            string id = spec["id"].Value<string>();

            // Root is unscaled so the interaction zone keeps its true radius; the visual
            // primitive carries the scale and the physical collider.
            var root = new GameObject(id);
            root.transform.position = Position(spec["pos"]);

            var visual = Primitive(spec, "Visual");
            visual.transform.SetParent(root.transform, true);

            bool solid = spec["solid"]?.Value<bool>() ?? true;
            if (!solid) Object.DestroyImmediate(visual.GetComponent<Collider>());

            var binder = root.AddComponent<WorldObjectBinder>();
            var serialized = new SerializedObject(binder);
            serialized.FindProperty("worldObjectId").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (spec["blocking"]?.Value<bool>() == true && solid)
                ProjectSetup.Bind(binder, "blockingCollider", visual.GetComponent<Collider>());

            var zoneGo = new GameObject("Zone") { layer = ActorLayer };
            zoneGo.transform.SetParent(root.transform, false);
            zoneGo.transform.localPosition = Vector3.up * 0.5f;
            var zone = zoneGo.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.radius = 2.4f;
            var interactionZone = zoneGo.AddComponent<InteractionZone>();
            ProjectSetup.Bind(interactionZone, "binder", binder);
        }

        private static void Npc(JObject spec)
        {
            string id = spec["id"].Value<string>();

            var root = new GameObject(id) { layer = ActorLayer };
            root.transform.position = Position(spec["pos"]);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetup.Material(
                $"{SettingsDir}/Mat_npc.mat", new Color(0.62f, 0.55f, 0.72f));

            var binder = root.AddComponent<NpcBinder>();
            var serialized = new SerializedObject(binder);
            serialized.FindProperty("npcId").stringValue = id;

            var waypoints = (JObject)spec["waypoints"] ?? new JObject();
            var waypointRoot = GameObject.Find("Waypoints") ?? new GameObject("Waypoints");
            var array = serialized.FindProperty("waypoints");
            array.arraySize = waypoints.Count;

            int i = 0;
            foreach (var pair in waypoints)
            {
                var point = new GameObject(pair.Key);
                point.transform.SetParent(waypointRoot.transform, false);
                point.transform.position = Position(pair.Value);

                var element = array.GetArrayElementAtIndex(i++);
                element.FindPropertyRelative("id").stringValue = pair.Key;
                element.FindPropertyRelative("point").objectReferenceValue = point.transform;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Pip(JObject layout, GameObject player)
        {
            var root = new GameObject("Pip") { layer = ActorLayer };
            root.transform.position = Position(layout["pip"]?["pos"]);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.45f;
            visual.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetup.Material(
                $"{SettingsDir}/Mat_pip.mat", new Color(0.95f, 0.9f, 0.7f));

            var lampGo = new GameObject("Lamp") { layer = ActorLayer };
            lampGo.transform.SetParent(root.transform, false);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 9f;
            lamp.color = new Color(1f, 0.92f, 0.75f);
            lamp.intensity = 1.4f;

            var pip = root.AddComponent<PipCompanion>();
            ProjectSetup.Bind(pip, "follow", player.transform);
            ProjectSetup.Bind(pip, "lamp", lamp);

            // Pip talks through the same NPC path as everyone else. No agent — Pip flies.
            var binder = root.AddComponent<NpcBinder>();
            var serialized = new SerializedObject(binder);
            serialized.FindProperty("npcId").stringValue = "npc.pip";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Player(Vector3 spawn)
        {
            var player = new GameObject("Player") { tag = "Player", layer = ActorLayer };
            player.transform.position = spawn;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.layer = ActorLayer;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetup.Material(
                $"{SettingsDir}/PlayerGrey.mat", new Color(0.85f, 0.8f, 0.7f));

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            nose.layer = ActorLayer;
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0, 1.5f, 0.35f);
            nose.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = ProjectSetup.Material(
                $"{SettingsDir}/GreyboxDark.mat", new Color(0.35f, 0.35f, 0.38f));

            // Portrait is attached at runtime (Resources textures are not serialised here).
            player.AddComponent<PlayerPortrait>();

            return player;
        }

        private static GameObject CameraRig(GameObject player)
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.farClipPlane = 300f;
            cameraGo.AddComponent<AudioListener>();

            var follow = cameraGo.AddComponent<FollowCamera>();
            ProjectSetup.Bind(follow, "target", player.transform);
            ProjectSetup.BindInt(follow, "collisionMask", ~(1 << ActorLayer));

            return cameraGo;
        }

        private static TouchControls Systems(GameObject player, GameObject cameraGo)
        {
            var controlsGo = new GameObject("Controls");
            var controls = controlsGo.AddComponent<TouchControls>();
            controlsGo.AddComponent<FrameTimeHud>();

            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();

            var spawner = bootstrap.AddComponent<LayoutSpawner>();
            ProjectSetup.Bind(spawner, "player", player.transform);

            var pc = player.AddComponent<PlayerController>();
            ProjectSetup.Bind(pc, "controls", controls);
            ProjectSetup.Bind(pc, "cameraTransform", cameraGo.transform);

            var interaction = player.AddComponent<InteractionController>();
            ProjectSetup.Bind(interaction, "player", player.transform);

            // GameHud attaches at runtime from TouchControls.Start so regenerating the
            // scene never depends on serializing HUD state into the scene file.

            return controls;
        }

        // ---- json helpers -----------------------------------------------------

        private static Vector3 Position(JToken t)
        {
            if (t is not JArray a || a.Count < 3) return Vector3.zero;
            return new Vector3(a[0].Value<float>(), a[1].Value<float>(), a[2].Value<float>());
        }

        private static Vector3 Size(JToken t, Vector3 fallback)
            => t is JArray a && a.Count >= 3 ? Position(t) : fallback;
    }
}
