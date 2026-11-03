using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Vizual effektlar: post-processing (rang, bloom, vinyetka) va zarrachalar (bug', tutun, uchqun, tangalar).
public static class Effects
{
    static Material particleMaterial;

    public static void SetupPostProcessing(Camera cam, Transform parent)
    {
        var data = cam.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        var go = new GameObject("PostProcessing");
        go.transform.SetParent(parent, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.55f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.65f);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.08f);
        color.contrast.Override(14f);
        color.saturation.Override(18f);

        var white = profile.Add<WhiteBalance>(true);
        white.temperature.Override(7f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.24f);
        vignette.smoothness.Override(0.45f);
    }

    static Material ParticleMaterial()
    {
        if (particleMaterial == null)
        {
            var source = Resources.Load<Material>("ParticleMaterial");
            particleMaterial = source != null ? new Material(source) : Util.NewMat(Color.white);
            particleMaterial.mainTexture = TextureGen.SoftDot();
        }
        return particleMaterial;
    }

    static ParticleSystem NewSystem(string name, Transform parent, Vector3 position, bool world)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        if (world) go.transform.position = position;
        else go.transform.localPosition = position;
        var ps = go.AddComponent<ParticleSystem>();
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return ps;
    }

    // Grildagi bug' / tutun. Rangi va kuchini CookStation boshqaradi (emission.rateOverTime).
    public static ParticleSystem Steam(Transform parent, Vector3 localPos)
    {
        var ps = NewSystem("Steam", parent, localPos, false);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.03f;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.09f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.2f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));

        ps.gameObject.SetActive(true);
        return ps;
    }

    // Yonayotgan go'sht: alanga zarrachalari va miltillovchi to'q sariq yorug'lik
    public static GameObject Fire(Transform slot)
    {
        var root = new GameObject("Fire");
        root.transform.SetParent(slot, false);
        root.transform.localPosition = Vector3.up * 0.03f;

        var ps = NewSystem("Flames", root.transform, Vector3.zero, false);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
        // 1 dan yorqin rang — bloom tufayli alanga porlab turadi
        main.startColor = new Color(2.4f, 1.25f, 0.35f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.25f;
        main.maxParticles = 160;

        var emission = ps.emission;
        emission.rateOverTime = 90f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.09f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.12f), 0.35f), new GradientColorKey(new Color(0.75f, 0.12f, 0.04f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        ps.gameObject.SetActive(true);

        var light = Util.Node("FireLight", root.transform, Vector3.up * 0.3f).AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.15f);
        light.range = 2.6f;
        light.intensity = 1.3f;
        light.shadows = LightShadows.None;
        light.gameObject.AddComponent<FireFlicker>();
        return root;
    }

    // O't o'chirgich ko'pigi: yumshoq, kengayib so'nadigan oq bulutchalar
    public static void Foam(Vector3 position)
    {
        var ps = NewSystem("Foam", null, position, true);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(0.97f, 0.98f, 1f, 0.95f);
        main.gravityModifier = 0.12f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)60), new ParticleSystem.Burst(0.15f, (short)40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.92f, 0.95f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        ps.gameObject.SetActive(true);
    }

    // Rakovinadagi suv tomchilari (yuvish paytida emission yoqiladi)
    public static ParticleSystem Water(Transform parent, Vector3 localPos)
    {
        var ps = NewSystem("Water", parent, localPos, false);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = new Color(0.8f, 0.92f, 1f, 0.85f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ps.gameObject.SetActive(true);
        return ps;
    }

    // Bir martalik uchqunlar (burger yopilganda, lavash o'ralganda)
    public static void Sparkle(Vector3 position, Color color)
    {
        Burst("Sparkle", position, color, 18, 0.9f, 0.025f, 0.05f, -0.2f);
    }

    // To'lov paytida oltin tangalar
    public static void Coins(Vector3 position)
    {
        Burst("Coins", position, new Color(1f, 0.82f, 0.2f), 30, 2.4f, 0.07f, 0.12f, 0.9f);
    }

    static void Burst(string name, Vector3 position, Color color, int count, float speed, float minSize, float maxSize, float gravity)
    {
        var ps = NewSystem(name, null, position, true);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color * 2.2f;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = gradient;

        ps.gameObject.SetActive(true);
    }
}
