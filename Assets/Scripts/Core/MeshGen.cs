using System.Collections.Generic;
using UnityEngine;

// Kod orqali yumaloq shakllar yasash: qirralari yumaloq quti, aylanma (lathe) jismlar — gumbaz, disk va h.k.
// Bir xil parametrli meshlar keshlanadi, shuning uchun ko'p nusxada ham xotira tejaladi.
public static class MeshGen
{
    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    // ---------- Yumaloq qirrali quti ----------

    // size — to'liq o'lcham, radius — qirra yumaloqligi. UV koordinatalari metrda (teksturalar bir xil zichlikda takrorlanadi).
    public static Mesh RoundedBox(Vector3 size, float radius)
    {
        string key = "box" + size.x + "_" + size.y + "_" + size.z + "_" + radius;
        Mesh cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        Vector3 half = size * 0.5f;
        float r = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
        int steps = r > 0f ? 3 : 0;
        Vector3 inner = new Vector3(half.x - r, half.y - r, half.z - r);
        float[] xs = Coords(half.x, r, steps);
        float[] ys = Coords(half.y, r, steps);
        float[] zs = Coords(half.z, r, steps);

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        // Har bir yuz: o'q bo'ylab ikki koordinata to'ri, uchinchisi ±half
        AddFace(verts, normals, uvs, tris, xs, ys, half, inner, r, 2, 1f);
        AddFace(verts, normals, uvs, tris, xs, ys, half, inner, r, 2, -1f);
        AddFace(verts, normals, uvs, tris, xs, zs, half, inner, r, 1, 1f);
        AddFace(verts, normals, uvs, tris, xs, zs, half, inner, r, 1, -1f);
        AddFace(verts, normals, uvs, tris, ys, zs, half, inner, r, 0, 1f);
        AddFace(verts, normals, uvs, tris, ys, zs, half, inner, r, 0, -1f);

        var mesh = Build("RoundedBox", verts, normals, uvs, tris);
        cache[key] = mesh;
        return mesh;
    }

    static float[] Coords(float h, float r, int steps)
    {
        var list = new List<float>();
        for (int k = 0; k <= steps; k++)
        {
            float a = steps == 0 ? 0f : Mathf.PI * 0.5f * k / steps;
            list.Add(-(h - r) - r * Mathf.Cos(a));
        }
        for (int k = steps; k >= 0; k--)
        {
            float a = steps == 0 ? 0f : Mathf.PI * 0.5f * k / steps;
            float v = (h - r) + r * Mathf.Cos(a);
            if (Mathf.Abs(v - list[list.Count - 1]) > 1e-5f) list.Add(v);
        }
        return list.ToArray();
    }

    // fixedAxis: 0=x, 1=y, 2=z yuz normali o'qi; a,b — qolgan ikki o'q koordinatalari (tartib bilan)
    static void AddFace(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
        float[] a, float[] b, Vector3 half, Vector3 inner, float r, int fixedAxis, float sign)
    {
        int start = verts.Count;
        for (int j = 0; j < b.Length; j++)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Vector3 v;
                if (fixedAxis == 2) v = new Vector3(a[i], b[j], half.z * sign);
                else if (fixedAxis == 1) v = new Vector3(a[i], half.y * sign, b[j]);
                else v = new Vector3(half.x * sign, a[i], b[j]);

                Vector3 c = new Vector3(Mathf.Clamp(v.x, -inner.x, inner.x), Mathf.Clamp(v.y, -inner.y, inner.y), Mathf.Clamp(v.z, -inner.z, inner.z));
                Vector3 n = v - c;
                n = n.sqrMagnitude > 1e-10f ? n.normalized : FaceNormal(fixedAxis, sign);
                Vector3 p = r > 0f ? c + n * r : v;
                verts.Add(p);
                normals.Add(r > 0f ? n : FaceNormal(fixedAxis, sign));
                uvs.Add(fixedAxis == 2 ? new Vector2(p.x, p.y) : fixedAxis == 1 ? new Vector2(p.x, p.z) : new Vector2(p.z, p.y));
            }
        }

        // Uchburchaklar tashqariga qaragan bo'lishi uchun aylanish yo'nalishi yuzga qarab tanlanadi
        bool flip = (fixedAxis == 2 && sign > 0f) || (fixedAxis == 1 && sign < 0f) || (fixedAxis == 0 && sign > 0f);
        int w = a.Length;
        for (int j = 0; j < b.Length - 1; j++)
        {
            for (int i = 0; i < w - 1; i++)
            {
                int v0 = start + j * w + i, v1 = v0 + 1, v2 = v0 + w, v3 = v2 + 1;
                if (flip)
                {
                    tris.Add(v0); tris.Add(v1); tris.Add(v2);
                    tris.Add(v1); tris.Add(v3); tris.Add(v2);
                }
                else
                {
                    tris.Add(v0); tris.Add(v2); tris.Add(v1);
                    tris.Add(v1); tris.Add(v2); tris.Add(v3);
                }
            }
        }
    }

    static Vector3 FaceNormal(int axis, float sign)
    {
        if (axis == 0) return new Vector3(sign, 0f, 0f);
        if (axis == 1) return new Vector3(0f, sign, 0f);
        return new Vector3(0f, 0f, sign);
    }

    // ---------- Aylanma jismlar ----------

    // profile: (radius, balandlik) nuqtalari pastki markazdan yuqoriga qarab. Ketma-ket ikki bir xil nuqta — o'tkir qirra.
    public static Mesh Lathe(string key, Vector2[] profile, int segments)
    {
        Mesh cached;
        if (cache.TryGetValue(key, out cached) && cached != null) return cached;

        int n = profile.Length;
        var profileNormals = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = (i > 0 && (profile[i - 1] - profile[i]).sqrMagnitude > 1e-12f) ? profile[i - 1] : profile[i];
            Vector2 next = (i < n - 1 && (profile[i + 1] - profile[i]).sqrMagnitude > 1e-12f) ? profile[i + 1] : profile[i];
            Vector2 t = next - prev;
            if (t.sqrMagnitude < 1e-12f) t = new Vector2(1f, 0f);
            t.Normalize();
            profileNormals[i] = new Vector2(t.y, -t.x);
        }

        float length = 0f;
        var dist = new float[n];
        for (int i = 1; i < n; i++)
        {
            length += (profile[i] - profile[i - 1]).magnitude;
            dist[i] = length;
        }

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int s = 0; s <= segments; s++)
        {
            float ang = Mathf.PI * 2f * s / segments;
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
            for (int i = 0; i < n; i++)
            {
                verts.Add(new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin));
                normals.Add(new Vector3(profileNormals[i].x * cos, profileNormals[i].y, profileNormals[i].x * sin).normalized);
                uvs.Add(new Vector2((float)s / segments, length > 0f ? dist[i] / length : 0f));
            }
        }
        for (int s = 0; s < segments; s++)
        {
            for (int i = 0; i < n - 1; i++)
            {
                int a = s * n + i, b = a + 1, c = a + n, d = c + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
        }

        var mesh = Build(key, verts, normals, uvs, tris);
        cache[key] = mesh;
        return mesh;
    }

    // Qirralari yumaloq disk (kotlet, non pastki qismi, likopcha va h.k.)
    public static Mesh Puck(float radius, float height, float bevel, int segments)
    {
        string key = "puck" + radius + "_" + height + "_" + bevel + "_" + segments;
        bevel = Mathf.Min(bevel, Mathf.Min(radius, height * 0.5f));
        var p = new List<Vector2>();
        p.Add(new Vector2(0f, 0f));
        for (int k = 0; k <= 4; k++)
        {
            float a = Mathf.PI * 0.5f * k / 4f;
            p.Add(new Vector2(radius - bevel + bevel * Mathf.Sin(a), bevel - bevel * Mathf.Cos(a)));
        }
        for (int k = 0; k <= 4; k++)
        {
            float a = Mathf.PI * 0.5f * k / 4f;
            p.Add(new Vector2(radius - bevel + bevel * Mathf.Cos(a), height - bevel + bevel * Mathf.Sin(a)));
        }
        p.Add(new Vector2(0f, height));
        return Lathe(key, p.ToArray(), segments);
    }

    // Tekis tagli gumbaz (burger nonining usti, sous tomchisi)
    public static Mesh Dome(float radius, float height, int segments)
    {
        string key = "dome" + radius + "_" + height + "_" + segments;
        var p = new List<Vector2>();
        p.Add(new Vector2(0f, 0f));
        p.Add(new Vector2(radius, 0f));
        p.Add(new Vector2(radius, 0f));
        for (int k = 1; k <= 10; k++)
        {
            float a = Mathf.PI * 0.5f * k / 10f;
            p.Add(new Vector2(radius * Mathf.Cos(a), height * Mathf.Sin(a)));
        }
        return Lathe(key, p.ToArray(), segments);
    }

    static Mesh Build(string name, List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris)
    {
        var mesh = new Mesh();
        mesh.name = name;
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    // ---------- Sahnaga qo'yish ----------

    public static GameObject Spawn(string name, Mesh mesh, Material material, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // Qattiq jism (devor, peshtaxta): mesh + BoxCollider
    public static GameObject Solid(string name, Vector3 size, float radius, Material material, Transform parent, Vector3 center)
    {
        var go = Spawn(name, RoundedBox(size, radius), material, parent, center);
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        return go;
    }
}
