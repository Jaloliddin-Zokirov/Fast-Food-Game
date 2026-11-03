using System.Collections.Generic;
using UnityEngine;

// Kod orqali yasaladigan teksturalar: kafel, yog'och taxtalar, devor plitkalari, UI uchun yumaloq to'rtburchak va h.k.
public static class TextureGen
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    static Texture2D Make(string key, int size, bool mipmaps, System.Func<int, int, Color> pixel)
    {
        Texture2D cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = pixel(x, y);
        tex.SetPixels32(pixels);
        tex.wrapMode = mipmaps ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        tex.filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;
        tex.anisoLevel = mipmaps ? 4 : 0;
        tex.Apply(mipmaps, false);
        tex.name = key;
        cache[key] = tex;
        return tex;
    }

    static float Noise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y);
    }

    // Shaxmat kafel: bitta tekstura = 2x2 kafel (UV metrda bo'lgani uchun scale bilan o'lcham beriladi)
    public static Texture2D CheckerTiles(Color a, Color b)
    {
        return Make("checker" + a + b, 256, true, (x, y) =>
        {
            int tx = x / 128, ty = y / 128;
            int lx = x % 128, ly = y % 128;
            bool grout = lx < 3 || ly < 3;
            Color c = ((tx + ty) % 2 == 0) ? a : b;
            float n = 0.94f + 0.06f * Noise(x * 0.05f, y * 0.05f);
            c *= n;
            if (grout) c = Color.Lerp(c, new Color(0.55f, 0.55f, 0.55f), 0.6f);
            c.a = 1f;
            return c;
        });
    }

    // Yog'och pol taxtalari
    public static Texture2D WoodPlanks(Color baseColor)
    {
        return Make("planks" + baseColor, 512, true, (x, y) =>
        {
            int plank = y / 64;
            float offset = (plank * 137) % 512;
            float grain = Noise((x + offset) * 0.012f, y * 0.25f) * 0.5f + Noise((x + offset) * 0.05f, y * 0.9f) * 0.2f;
            float tone = 0.85f + 0.15f * Noise(plank * 3.1f, 0.5f);
            Color c = baseColor * (tone * (0.82f + grain * 0.35f));
            int ly = y % 64;
            int seam = (int)((x + offset) % 512);
            if (ly < 2 || seam < 2) c *= 0.6f;
            c.a = 1f;
            return c;
        });
    }

    // Oq "metro" plitkalari (oshxona devori)
    public static Texture2D SubwayTiles(Color tile, Color grout)
    {
        return Make("subway" + tile + grout, 256, true, (x, y) =>
        {
            int row = y / 32;
            int shift = (row % 2) * 32;
            int lx = (x + shift) % 64, ly = y % 32;
            if (lx < 2 || ly < 2) return grout;
            float shade = 1f - 0.08f * Mathf.Clamp01((ly - 24) / 8f) + 0.03f * Noise(x * 0.1f, y * 0.1f);
            Color c = tile * shade;
            c.a = 1f;
            return c;
        });
    }

    // Bo'yalgan devor: biroz dog'li
    public static Texture2D Plaster(Color color)
    {
        return Make("plaster" + color, 256, true, (x, y) =>
        {
            float n = Noise(x * 0.04f, y * 0.04f) * 0.06f + Noise(x * 0.2f, y * 0.2f) * 0.03f;
            Color c = color * (0.95f + n);
            c.a = 1f;
            return c;
        });
    }

    // Lavash noni: och sariq, ozgina qovurilgan dog'lar bilan
    public static Texture2D Flatbread()
    {
        return Make("flatbread", 128, true, (x, y) =>
        {
            float n = Noise(x * 0.12f, y * 0.12f);
            float spots = Mathf.Clamp01((Noise(x * 0.3f + 10f, y * 0.3f) - 0.62f) * 4f);
            Color c = Color.Lerp(new Color(0.92f, 0.82f, 0.6f), new Color(0.76f, 0.55f, 0.3f), spots * 0.75f);
            c *= 0.95f + n * 0.08f;
            c.a = 1f;
            return c;
        });
    }

    // Zarrachalar uchun yumshoq dumaloq nuqta
    public static Texture2D SoftDot()
    {
        return Make("softdot", 64, false, (x, y) =>
        {
            float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - d);
            return new Color(1f, 1f, 1f, a * a);
        });
    }

    // UI paneli: burchaklari yumaloq oq to'rtburchak (9-slice uchun, radius piksel)
    public static Texture2D RoundedRect(int radius)
    {
        int size = radius * 2 + 4;
        return Make("round" + radius, size, false, (x, y) =>
        {
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            float a = Mathf.Clamp01(radius - d + 0.5f);
            return new Color(1f, 1f, 1f, a);
        });
    }

    // Yuqoridan pastga quyuqlashadigan gradient (tugmalar uchun)
    public static Texture2D RoundedGradient(int radius, Color top, Color bottom)
    {
        int size = radius * 2 + 4;
        string key = "grad" + radius + top + bottom;
        return Make(key, size, false, (x, y) =>
        {
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            Color c = Color.Lerp(bottom, top, y / (float)(size - 1));
            c.a = Mathf.Clamp01(radius - d + 0.5f);
            return c;
        });
    }
}
