using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Loyiha birinchi marta ochilganda o'yin sahnasini yaratadi, Build Settings ga qo'shadi va ochadi.
[InitializeOnLoad]
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string MaterialPath = "Assets/Resources/BaseMaterial.mat";

    static ProjectSetup()
    {
        EditorApplication.delayCall += EnsureScene;
    }

    [MenuItem("Lavash Burger/O'yin sahnasini qayta yaratish")]
    static void RecreateScene()
    {
        CreateScene();
    }

    // Buyruq qatoridan: Unity.exe -batchmode -projectPath . -executeMethod ProjectSetup.BatchSetup -quit
    public static void BatchSetup()
    {
        EnsureBaseMaterial();
        EnsureEffectMaterials();
        if (File.Exists(ScenePath)) AddToBuildSettings();
        else CreateScene();
    }

    // Zarrachalar (tutun, tangalar) va porlovchi chiroqlar uchun materiallar. Ular Resources da turgani
    // uchun kerakli shader variantlari build'ga albatta kiradi.
    static void EnsureEffectMaterials()
    {
        if (GraphicsSettings.currentRenderPipeline == null) return;

        if (!File.Exists("Assets/Resources/ParticleMaterial.mat"))
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var particle = new Material(shader);
                particle.SetFloat("_Surface", 1f);
                particle.SetFloat("_Blend", 0f);
                particle.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                particle.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                particle.SetFloat("_ZWrite", 0f);
                particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                particle.SetOverrideTag("RenderType", "Transparent");
                particle.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(particle, "Assets/Resources/ParticleMaterial.mat");
            }
        }

        if (!File.Exists("Assets/Resources/WorldTextMaterial.mat"))
        {
            var textShader = Shader.Find("LavashBurger/WorldText");
            if (textShader != null) AssetDatabase.CreateAsset(new Material(textShader), "Assets/Resources/WorldTextMaterial.mat");
        }

        if (!File.Exists("Assets/Resources/GlowMaterial.mat"))
        {
            var glow = new Material(GraphicsSettings.currentRenderPipeline.defaultShader);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", Color.white);
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(glow, "Assets/Resources/GlowMaterial.mat");
        }
        AssetDatabase.SaveAssets();
    }

    // Util.BaseMaterial shu materialni yuklaydi; shader build'ga tushishi uchun kerak.
    // URP yoqilgan bo'lsa uning standart (Lit) shaderi, aks holda Standard ishlatiladi.
    static void EnsureBaseMaterial()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        var shader = pipeline != null ? pipeline.defaultShader : Shader.Find("Standard");
        if (shader == null) return;

        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            if (existing.shader == shader) return;
            existing.shader = shader;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var material = new Material(shader);
        material.SetFloat(pipeline != null ? "_Smoothness" : "_Glossiness", 0.15f);
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
    }

    static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureBaseMaterial();
        EnsureEffectMaterials();
        if (File.Exists(ScenePath))
        {
            AddToBuildSettings();
            return;
        }
        CreateScene();
    }

    static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Game").AddComponent<GameRoot>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        PlayerSettings.productName = "Lavash & Burger Simulator";
        Debug.Log("Lavash & Burger: o'yin sahnasi yaratildi (" + ScenePath + "). Play tugmasini bosing!");
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
            if (scenes[i].path == ScenePath) return;

        // O'yin sahnasi birinchi bo'lishi kerak — build shu sahnadan boshlanadi
        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        list[0] = new EditorBuildSettingsScene(ScenePath, true);
        for (int i = 0; i < scenes.Length; i++) list[i + 1] = scenes[i];
        EditorBuildSettings.scenes = list;
    }
}
