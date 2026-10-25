using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if NOVA_URP
using UnityEngine.Rendering.Universal;
#endif

namespace NovaVanguard.Editor
{
    /// <summary>
    /// One-time project setup that runs automatically when the project is opened in the editor
    /// (and can be re-run from the menu). It creates everything that can't live in Git as plain
    /// source: the URP pipeline assets, base materials, the Main scene, player settings, build scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class NovaProjectSetup
    {
        const string MarkerPath = "ProjectSettings/NovaSetupDone.txt";
        const string ScenePath = "Assets/NovaVanguard/Scenes/Main.unity";
        const string SettingsDir = "Assets/NovaVanguard/Settings";
        const string MaterialsDir = "Assets/NovaVanguard/Resources/NovaMaterials";

        static NovaProjectSetup()
        {
            if (File.Exists(MarkerPath)) return;
            // Delay so the asset database is ready and the Input System / URP packages have compiled.
            EditorApplication.delayCall += () => { if (!File.Exists(MarkerPath) && !EditorApplication.isCompiling) RunSetup(); };
        }

        [MenuItem("Nova Vanguard/Run Project Setup")]
        public static void RunSetup()
        {
            Debug.Log("[Nova Vanguard] Running project setup...");
            Directory.CreateDirectory(SettingsDir); Directory.CreateDirectory(MaterialsDir); Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            SetupPlayerSettings();
            SetupRenderPipeline();
            SetupMaterials();
            SetupScene();
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(MarkerPath, "Setup completed. Delete this file (or use Nova Vanguard > Run Project Setup) to run it again.\n");
            Debug.Log("[Nova Vanguard] Project setup complete. Open Assets/NovaVanguard/Scenes/Main.unity and press Play.");
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
        }

        static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "JaKing Games";
            PlayerSettings.productName = "Nova Vanguard";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.jaking.novavanguard");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.jaking.novavanguard");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.jaking.novavanguard");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)25;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            // Both input backends: the game supports the new Input System and falls back to the legacy manager.
            var psAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (psAssets != null && psAssets.Length > 0)
            {
                var so = new SerializedObject(psAssets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue != 2) { prop.intValue = 2; so.ApplyModifiedPropertiesWithoutUndo(); Debug.Log("[Nova Vanguard] Active Input Handling set to Both (restart the editor if prompted)."); }
            }
        }

        static void SetupRenderPipeline()
        {
#if NOVA_URP
            string rendererPath = SettingsDir + "/NovaRenderer.asset";
            string pipelinePath = SettingsDir + "/NovaURP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                // Load the renderer's shader/post-process resources (postProcessData is null on a raw CreateInstance,
                // which silently disables the Volume post-processing that PostFx relies on).
                ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
                renderer.renderingMode = RenderingMode.Forward;
                renderer.depthPrimingMode = DepthPrimingMode.Disabled;
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 0f;
            // mainLightRenderingMode / additionalLightsRenderingMode have internal setters in URP, so go through serialization.
            var pso = new SerializedObject(pipeline);
            var mainMode = pso.FindProperty("m_MainLightRenderingMode");
            var addMode = pso.FindProperty("m_AdditionalLightsRenderingMode");
            if (mainMode != null) mainMode.intValue = (int)LightRenderingMode.PerPixel;
            if (addMode != null) addMode.intValue = (int)LightRenderingMode.PerVertex;
            pso.ApplyModifiedPropertiesWithoutUndo();
            pipeline.maxAdditionalLightsCount = 4;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Per-quality-level override: null means "use the default pipeline", so clear any stale overrides.
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(current, false);
            Debug.Log("[Nova Vanguard] URP configured.");
#else
            Debug.LogWarning("[Nova Vanguard] URP package not found; the game will run on the Built-in pipeline (no bloom). Install com.unity.render-pipelines.universal and re-run setup.");
#endif
        }

        /// <summary>Base materials in Resources guarantee the shaders ship in player builds.</summary>
        static void SetupMaterials()
        {
            void Make(string name, string shaderName)
            {
                string path = $"{MaterialsDir}/{name}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
                var sh = Shader.Find(shaderName);
                if (sh == null) { Debug.LogWarning("[Nova Vanguard] Shader not found: " + shaderName); return; }
                AssetDatabase.CreateAsset(new Material(sh), path);
            }
            Make("Lit_URP", "Universal Render Pipeline/Lit");
            Make("Unlit_URP", "Universal Render Pipeline/Unlit");
            Make("Particle_URP", "Universal Render Pipeline/Particles/Unlit");
            Make("Lit_BuiltIn", "Standard");
            Make("Unlit_BuiltIn", "Unlit/Color");
            Make("Particle_BuiltIn", "Legacy Shaders/Particles/Additive");
            // Make sure the Particles/Unlit URP material is set up as additive-transparent in the asset itself.
            var p = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsDir}/Particle_URP.mat");
            if (p != null)
            {
                p.SetFloat("_Surface", 1f); p.SetFloat("_Blend", 2f);
                p.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); p.SetFloat("_DstBlend", (float)BlendMode.One);
                p.SetFloat("_ZWrite", 0f); p.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); p.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                p.renderQueue = (int)RenderQueue.Transparent; EditorUtility.SetDirty(p);
            }
        }

        static void SetupScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Nova Vanguard] Created " + ScenePath);
        }

        static void SetupBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
