using System.Collections.Generic;
using UnityEngine;

// Primitivlardan obyekt yasash, materiallar va 3D yozuvlar uchun yordamchi funksiyalar.
public static class Util
{
    static Material baseMaterial;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Font font;
    static Material textMaterial;

    // Barcha materiallar shu tayanch materialdan nusxalanadi. U Resources papkasida turgani uchun
    // shader build'ga albatta kiradi (aks holda tayyor .exe da hamma narsa pushti rangda chiqadi).
    static Material BaseMaterial()
    {
        if (baseMaterial == null) baseMaterial = Resources.Load<Material>("BaseMaterial");
        if (baseMaterial == null)
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(temp);
        }
        return baseMaterial;
    }

    public static Material NewMat(Color color)
    {
        var mat = new Material(BaseMaterial());
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
        return mat;
    }

    // Bir xil rangdagi materiallar qayta ishlatiladi.
    public static Material Mat(Color color)
    {
        return Mat(color, null);
    }

    // Teksturali material. Mesh UV lari metrda bo'lsa, tiling — metrga nechta tekstura takrorlanishi.
    public static Material Mat(Color color, Texture texture, float tiling = 1f)
    {
        Color32 c = color;
        string key = c.r + "," + c.g + "," + c.b + "," + c.a + (texture != null ? "|" + texture.name + texture.GetHashCode() + "|" + tiling : "");
        Material mat;
        if (!cache.TryGetValue(key, out mat) || mat == null)
        {
            mat = NewMat(color);
            if (texture != null)
            {
                mat.mainTexture = texture;
                mat.mainTextureScale = new Vector2(tiling, tiling);
            }
            cache[key] = mat;
        }
        return mat;
    }

    // Porlovchi material (chiroqlar, ekranlar) — bloom effekti bilan nur sochadi
    public static Material Glow(Color color, float intensity)
    {
        var source = Resources.Load<Material>("GlowMaterial");
        var mat = source != null ? new Material(source) : NewMat(color);
        mat.color = color;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
        }
        return mat;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(color);
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Box(Transform parent, Vector3 localPos, Vector3 scale, Color color)
    {
        return Prim(PrimitiveType.Cube, parent, localPos, scale, color, true);
    }

    public static GameObject Node(string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go;
    }

    public static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size)
    {
        var box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        return box;
    }

    // worldHeight — bitta qator matnning taxminiy balandligi (metr).
    public static TextMesh Label(string text, Transform parent, Vector3 localPos, Quaternion localRot, float worldHeight, Color color)
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        var renderer = go.AddComponent<MeshRenderer>();
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = font;
        tm.fontSize = 64;
        tm.characterSize = worldHeight * 10f / 64f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        renderer.sharedMaterial = TextMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return tm;
    }

    // Yozuvlar devor orqali ko'rinmasligi uchun chuqurlikni hisobga oladigan material (LavashBurger/WorldText).
    // Shrift teksturasi yangi harflar qo'shilganda qayta quriladi — shunda materialdagi teksturani ham yangilaymiz.
    static Material TextMaterial()
    {
        if (textMaterial != null) return textMaterial;
        var source = Resources.Load<Material>("WorldTextMaterial");
        if (source == null || font == null) return font != null ? font.material : null;

        textMaterial = new Material(source);
        textMaterial.mainTexture = font.material.mainTexture;
        Font.textureRebuilt += rebuilt =>
        {
            if (rebuilt == font && textMaterial != null) textMaterial.mainTexture = font.material.mainTexture;
        };
        return textMaterial;
    }

    public static Bounds RendererBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.1f);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
