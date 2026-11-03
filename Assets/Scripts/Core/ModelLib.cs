using System.Collections.Generic;
using UnityEngine;

// Resources/Models dagi 3D modellarni (Kenney, CC0) sahnaga qo'yish.
// Model har doim pastki markazi bo'yicha joylashtiriladi, shuning uchun FBX dagi pivot qayerda bo'lishi muhim emas.
public static class ModelLib
{
    // Kenney Furniture Kit modellarida peshtaxta balandligi 4.5 birlik — bizda 0.94 m
    public const float Furniture = 0.209f;

    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    public static GameObject Load(string path)
    {
        GameObject prefab;
        if (!cache.TryGetValue(path, out prefab))
        {
            prefab = Resources.Load<GameObject>("Models/" + path);
            if (prefab == null) Debug.LogWarning("Model topilmadi: Models/" + path);
            cache[path] = prefab;
        }
        return prefab;
    }

    // Modelni parent ichida localPos ga qo'yadi: yaw — Y o'qi bo'ylab burish, scale — masshtab.
    public static GameObject Spawn(string path, Transform parent, Vector3 localPos, float yaw, float scale)
    {
        return Spawn(path, parent, localPos, yaw, Vector3.one * scale);
    }

    public static GameObject Spawn(string path, Transform parent, Vector3 localPos, float yaw, Vector3 scale)
    {
        var holder = new GameObject(path);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;
        holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var prefab = Load(path);
        if (prefab == null)
        {
            Util.Prim(PrimitiveType.Cube, holder.transform, new Vector3(0f, 0.1f, 0f), Vector3.one * 0.2f, Color.magenta, false);
            return holder;
        }

        var model = Object.Instantiate(prefab, holder.transform, false);
        model.name = "Model";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // Ba'zi FBX larda ildizning o'z masshtabi bor (masalan, chiqindi qutisida ~0.47) — uni saqlab, ustiga ko'paytiramiz
        model.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);

        Bounds b = LocalBounds(model, holder.transform);
        model.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
        return holder;
    }

    // Modelni berilgan o'lchamga moslab qo'yadi: byHeight — balandlik bo'yicha, aks holda eng katta gorizontal o'lcham bo'yicha
    public static GameObject SpawnFit(string path, Transform parent, Vector3 localPos, float yaw, float size, bool byHeight)
    {
        var holder = Spawn(path, parent, localPos, yaw, 1f);
        var model = holder.transform.Find("Model");
        if (model == null) return holder;

        Bounds b = LocalBounds(model.gameObject, holder.transform);
        float current = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
        if (current > 1e-4f) model.localScale *= size / current;
        b = LocalBounds(model.gameObject, holder.transform);
        model.localPosition += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        return holder;
    }

    // Model meshlarining holder koordinatalaridagi chegarasi (aylanishni ham hisobga oladi)
    public static Bounds LocalBounds(GameObject model, Transform space)
    {
        bool any = false;
        var b = new Bounds();
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh != null) Encapsulate(ref b, ref any, filter.sharedMesh.bounds, filter.transform, space);
        foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (skinned.sharedMesh != null) Encapsulate(ref b, ref any, skinned.sharedMesh.bounds, skinned.transform, space);
        return b;
    }

    static void Encapsulate(ref Bounds b, ref bool any, Bounds local, Transform from, Transform space)
    {
        Vector3 min = local.min, max = local.max;
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
            Vector3 p = space.InverseTransformPoint(from.TransformPoint(corner));
            if (!any)
            {
                b = new Bounds(p, Vector3.zero);
                any = true;
            }
            else
            {
                b.Encapsulate(p);
            }
        }
    }

    // Modelning barcha materiallarini nusxalab, rangini ko'paytiruvchi sifatida o'zgartirish mumkin qiladi
    // (teksturali Kenney modellarini pishganda/kuyganda bo'yash uchun).
    public static List<Material> OwnMaterials(GameObject root)
    {
        var list = new List<Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                mats[i] = new Material(mats[i]);
                list.Add(mats[i]);
            }
            r.sharedMaterials = mats;
        }
        return list;
    }

    public static void SetShadows(GameObject root, bool cast)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = cast ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
