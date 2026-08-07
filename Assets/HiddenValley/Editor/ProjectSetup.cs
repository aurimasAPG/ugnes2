using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using HiddenValley.Unity;

namespace HiddenValley.Editor
{
    /// <summary>
    /// One-shot, idempotent project configuration, runnable headless:
    ///
    ///   Unity -quit -batchmode -projectPath . -executeMethod HiddenValley.Editor.ProjectSetup.All
    ///
    /// This exists because the repo was authored before the Unity project ever opened
    /// (phase log, blocker B1): there are no ProjectSettings, no URP assets and no scene in
    /// version control history to lean on. Everything the Phase 0 done-state needs — URP
    /// configured for mobile, iOS player settings, the grey-box room with joystick, follow
    /// camera and frame-time HUD — is constructed here by code, so the setup is reviewable,
    /// diffable and repeatable instead of a pile of clicks nobody can audit.
    /// </summary>
    public static class ProjectSetup
    {
        private const string SettingsDir = "Assets/HiddenValley/Settings";
        private const string ScenesDir = "Assets/HiddenValley/Scenes";
        private const string ScenePath = ScenesDir + "/Greybox.unity";

        // Provisional — both halves are open [CONFIRM] items (#8 the title, and the
        // shipping identifier belongs to whoever owns the developer account).
        private const string BundleId = "lt.apgmedia.hiddenvalley";

        [MenuItem("Hidden Valley/Setup/All")]
        public static void All()
        {
            ConfigureRenderPipeline();
            ConfigurePlayerSettings();
            GenerateGreyboxScene();
            VillageSetup.GenerateHeartwood(); // runs last — it owns the build-scene list
            AssetDatabase.SaveAssets();
            Debug.Log("[HiddenValley] Project setup complete.");
        }

        // ---- render pipeline --------------------------------------------------

        [MenuItem("Hidden Valley/Setup/Render Pipeline")]
        public static void ConfigureRenderPipeline()
        {
            Directory.CreateDirectory(SettingsDir);

            var rendererPath = SettingsDir + "/UrpRenderer.asset";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var pipelinePath = SettingsDir + "/UrpPipeline.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // The frame budget is 16.7 ms on an iPhone 13 ([CONFIRM] #3, answered). These
            // are the knobs that spend it: no HDR target, MSAA 4x (near-free on TBDR GPUs,
            // and grey-box geometry aliases badly without it), short shadow range.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 45f;
            pipeline.renderScale = 1f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // The manifest ships the Input System package, but the runtime layer reads
            // legacy Input (FrameTimeHud, TouchControls) — "Both" keeps either path alive.
            var projectSettings = AssetDatabase
                .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").First();
            var serialized = new SerializedObject(projectSettings);
            serialized.FindProperty("activeInputHandler").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[HiddenValley] URP configured.");
        }

        // ---- player settings --------------------------------------------------

        [MenuItem("Hidden Valley/Setup/Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Hidden Valley";
            PlayerSettings.companyName = "APG Media";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            PlayerSettings.iOS.targetOSVersionString = "16.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

            // Automatic signing, so the generated Xcode project provisions itself with
            // -allowProvisioningUpdates. The team id is the owner's personal team,
            // extracted from the development certificate on 2026-08-07.
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "32U8KR34UT";

            // Landscape-only. A follow-camera exploration game framed for portrait is a
            // different layout job; if that decision changes it changes here, once.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            Debug.Log("[HiddenValley] iOS player settings configured.");
        }

        // ---- the grey-box room ------------------------------------------------

        [MenuItem("Hidden Valley/Setup/Greybox Scene")]
        public static void GenerateGreyboxScene()
        {
            Directory.CreateDirectory(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var grey = Material(SettingsDir + "/Greybox.mat", new Color(0.55f, 0.55f, 0.55f));
            var dark = Material(SettingsDir + "/GreyboxDark.mat", new Color(0.35f, 0.35f, 0.38f));

            // Floor and perimeter. The room is 40x40 — big enough to sprint in.
            Block("Floor", grey, new Vector3(0, -0.5f, 0), new Vector3(40, 1, 40));
            Block("Wall.N", dark, new Vector3(0, 1.5f, 20.5f), new Vector3(42, 3, 1));
            Block("Wall.S", dark, new Vector3(0, 1.5f, -20.5f), new Vector3(42, 3, 1));
            Block("Wall.E", dark, new Vector3(20.5f, 1.5f, 0), new Vector3(1, 3, 40));
            Block("Wall.W", dark, new Vector3(-20.5f, 1.5f, 0), new Vector3(1, 3, 40));

            // The adversarial-camera furniture the Phase 1 gate is tested against:
            // a tight corner alcove, a narrow corridor, pillars, a ramp, and steps.
            Block("Alcove.A", dark, new Vector3(-14, 1.5f, 14), new Vector3(8, 3, 1));
            Block("Alcove.B", dark, new Vector3(-17.5f, 1.5f, 10), new Vector3(1, 3, 9));
            Block("Corridor.A", dark, new Vector3(8, 1.5f, -10), new Vector3(1, 3, 12));
            Block("Corridor.B", dark, new Vector3(11, 1.5f, -10), new Vector3(1, 3, 12));
            Block("Pillar.1", dark, new Vector3(4, 2, 6), new Vector3(1.2f, 4, 1.2f));
            Block("Pillar.2", dark, new Vector3(-2, 2, -4), new Vector3(1.2f, 4, 1.2f));
            Block("Pillar.3", dark, new Vector3(13, 2, 8), new Vector3(1.2f, 4, 1.2f));

            var ramp = Block("Ramp", grey, new Vector3(-8, 0.9f, -12), new Vector3(4, 0.4f, 10));
            ramp.transform.rotation = Quaternion.Euler(-20f, 0, 0);
            Block("RampTop", grey, new Vector3(-8, 1.55f, -18.2f), new Vector3(4, 3.1f, 3f));

            for (int i = 0; i < 6; i++)
            {
                Block($"Step.{i}", grey,
                    new Vector3(15, 0.125f + i * 0.25f, 14 + i * 0.9f),
                    new Vector3(3, 0.25f + i * 0.5f, 0.9f));
            }

            // Light. One directional, soft shadows — the whole grey-box lighting rig.
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0);

            // Player: CharacterController capsule with the visual as a child. Layer 2
            // (Ignore Raycast) keeps the follow camera's occlusion cast off the player.
            var player = new GameObject("Player") { tag = "Player", layer = 2 };
            player.transform.position = new Vector3(0, 1.0f, 0);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<MeshRenderer>().sharedMaterial =
                Material(SettingsDir + "/PlayerGrey.mat", new Color(0.85f, 0.8f, 0.7f));

            // A nose, so facing is readable on a grey capsule.
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0, 1.5f, 0.35f);
            nose.transform.localScale = new Vector3(0.15f, 0.15f, 0.3f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = dark;

            // Systems objects.
            var controlsGo = new GameObject("Controls");
            var controls = controlsGo.AddComponent<TouchControls>();
            controlsGo.AddComponent<FrameTimeHud>();

            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.farClipPlane = 300f;
            cameraGo.AddComponent<AudioListener>();
            var follow = cameraGo.AddComponent<FollowCamera>();
            Bind(follow, "target", player.transform);
            BindInt(follow, "collisionMask", ~(1 << 2));

            var pc = player.AddComponent<PlayerController>();
            Bind(pc, "controls", controls);
            Bind(pc, "cameraTransform", cameraGo.transform);

            var interaction = player.AddComponent<InteractionController>();
            Bind(interaction, "player", player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"[HiddenValley] Greybox scene written to {ScenePath} and set as the build scene.");
        }

        // ---- helpers ----------------------------------------------------------

        internal static GameObject Block(string name, UnityEngine.Material material,
                                         Vector3 position, Vector3 scale)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        internal static UnityEngine.Material Material(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (material == null)
            {
                material = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            return material;
        }

        /// <summary>Serialized fields on the runtime components are private by design;
        /// the editor wires them the same way the inspector would.</summary>
        internal static void Bind(Component component, string field, Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void BindInt(Component component, string field, int value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
