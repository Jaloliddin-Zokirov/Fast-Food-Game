using System.Collections.Generic;
using UnityEngine;

// Ovqat modellari: Kenney Food Kit (CC0) modellari va kod bilan yasalgan yumaloq shakllar.
// Har bir model markazi — pastki qismi (y = 0), shuning uchun stolga yoki taxtaga qo'yish oson.
public static class ItemFactory
{
    public static readonly Color BunColor = new Color(0.9f, 0.62f, 0.32f);
    public static readonly Color BunInside = new Color(0.98f, 0.86f, 0.62f);
    public static readonly Color SauceColor = new Color(0.82f, 0.1f, 0.08f);
    public static readonly Color PaperColor = new Color(0.97f, 0.96f, 0.92f);

    // Pishish ranglari: Kenney modellarida tekstura rangiga ko'paytiriladi, lavashda esa to'g'ridan-to'g'ri rang
    public static void CookColors(ItemType type, out Color raw, out Color cooked, out Color burnt)
    {
        switch (type)
        {
            case ItemType.Patty:
                raw = new Color(1.35f, 0.78f, 0.82f);
                cooked = Color.white;
                burnt = new Color(0.28f, 0.24f, 0.22f);
                return;
            case ItemType.Meat:
                raw = new Color(1.3f, 0.82f, 0.86f);
                cooked = Color.white;
                burnt = new Color(0.3f, 0.26f, 0.24f);
                return;
            default:
                raw = new Color(0.97f, 0.9f, 0.72f);
                cooked = new Color(0.9f, 0.66f, 0.34f);
                burnt = new Color(0.22f, 0.15f, 0.09f);
                return;
        }
    }

    public static Item Create(ItemType type)
    {
        return Create(type, 0f);
    }

    public static Item Create(ItemType type, float cook)
    {
        var go = new GameObject(GameData.Name(type));
        var item = go.AddComponent<Item>();
        item.type = type;

        // Ko'rinish "Content" ichida — tarelka yoki paket qo'shilganda uni surish/yashirish oson
        var content = Util.Node("Content", go.transform, Vector3.zero).transform;
        var tinted = new List<Material>();
        BuildVisual(type, content, tinted);
        if (GameData.IsCookable(type))
        {
            Color raw, cooked, burnt;
            CookColors(type, out raw, out cooked, out burnt);
            item.SetupTint(tinted, raw, cooked, burnt);
        }
        item.SetCook(cook);
        FitCollider(go);
        return item;
    }

    static void FitCollider(GameObject go)
    {
        Bounds b = ModelLib.LocalBounds(go, go.transform);
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        box.center = b.center;
        box.size = new Vector3(Mathf.Max(0.12f, b.size.x + 0.02f), Mathf.Max(0.08f, b.size.y + 0.02f), Mathf.Max(0.12f, b.size.z + 0.02f));
    }

    // Tarelka (taom ustiga suriladi) yoki qog'oz paket (taom yashiriladi) ko'rinishini qo'llaydi
    public static void ApplyContainer(Item item)
    {
        var root = item.transform;
        var old = root.Find("Container");
        if (old != null)
        {
            old.name = "OldContainer";
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        var content = root.Find("Content");
        if (content != null)
        {
            content.gameObject.SetActive(item.container != Container.Bag);
            content.localPosition = Vector3.zero;
        }

        if (item.container == Container.Plate)
        {
            var plate = ModelLib.SpawnFit("Food/plate", root, Vector3.zero, 0f, 0.32f, false);
            plate.name = "Container";
            if (content != null) content.localPosition = new Vector3(0f, TopOf(plate) - 0.006f, 0f);
        }
        else if (item.container == Container.Bag)
        {
            ModelLib.SpawnFit("Food/bag", root, Vector3.zero, 0f, 0.3f, true).name = "Container";
        }
        FitCollider(item.gameObject);
    }

    static float TopOf(GameObject holder)
    {
        var model = holder.transform.Find("Model");
        return model != null ? ModelLib.LocalBounds(model.gameObject, holder.transform).max.y : 0.02f;
    }

    // Qizil o't o'chirgich: korpus, yorliq, klapan, dastak va shlang
    static void ExtinguisherModel(Transform parent)
    {
        var red = Util.Mat(new Color(0.85f, 0.1f, 0.08f));
        var black = Util.Mat(new Color(0.08f, 0.08f, 0.09f));
        var metal = Util.Mat(new Color(0.75f, 0.75f, 0.78f));
        MeshGen.Spawn("Body", MeshGen.Puck(0.065f, 0.38f, 0.03f, 24), red, parent, Vector3.zero);
        MeshGen.Spawn("Top", MeshGen.Dome(0.065f, 0.05f, 24), red, parent, new Vector3(0f, 0.38f, 0f));
        MeshGen.Spawn("Label", MeshGen.Puck(0.067f, 0.08f, 0.005f, 24), Util.Mat(new Color(0.95f, 0.95f, 0.92f)), parent, new Vector3(0f, 0.16f, 0f));
        MeshGen.Spawn("Valve", MeshGen.RoundedBox(new Vector3(0.04f, 0.05f, 0.04f), 0.01f), metal, parent, new Vector3(0f, 0.445f, 0f));
        MeshGen.Spawn("Handle", MeshGen.RoundedBox(new Vector3(0.13f, 0.015f, 0.03f), 0.006f), black, parent, new Vector3(0.03f, 0.475f, 0f));
        MeshGen.Spawn("Hose", MeshGen.RoundedBox(new Vector3(0.018f, 0.22f, 0.018f), 0.008f), black, parent, new Vector3(-0.075f, 0.33f, 0f));
        MeshGen.Spawn("Nozzle", MeshGen.RoundedBox(new Vector3(0.03f, 0.05f, 0.03f), 0.01f), black, parent, new Vector3(-0.075f, 0.2f, 0f));
    }

    // Iflos tarelka: tarelka va ustida dog'lar, ushoqlar
    static void DirtyPlateModel(Transform parent)
    {
        var plate = ModelLib.SpawnFit("Food/plate", parent, Vector3.zero, 0f, 0.32f, false);
        float top = TopOf(plate);
        MeshGen.Spawn("Stain", MeshGen.Puck(0.06f, 0.003f, 0.0015f, 16), Util.Mat(new Color(0.42f, 0.24f, 0.12f)), parent, new Vector3(0.03f, top - 0.004f, -0.02f));
        MeshGen.Spawn("Stain", MeshGen.Puck(0.035f, 0.003f, 0.0015f, 16), Util.Mat(SauceColor), parent, new Vector3(-0.06f, top - 0.004f, 0.04f));
        MeshGen.Spawn("Crumb", MeshGen.Puck(0.014f, 0.01f, 0.005f, 8), Util.Mat(BunColor), parent, new Vector3(0.07f, top - 0.002f, 0.06f));
        MeshGen.Spawn("Crumb", MeshGen.Puck(0.01f, 0.008f, 0.004f, 8), Util.Mat(BunColor), parent, new Vector3(-0.02f, top - 0.002f, -0.08f));
    }

    // Qahva doni qopchasi (do'kon ikonchasi uchun): jigarrang qopcha va ustida donlar
    static void CoffeeBeansModel(Transform parent)
    {
        var bag = ModelLib.SpawnFit("Food/bag", parent, Vector3.zero, 0f, 0.22f, true);
        foreach (var m in ModelLib.OwnMaterials(bag)) m.color = new Color(0.62f, 0.4f, 0.24f);
        var bean = Util.Mat(new Color(0.24f, 0.13f, 0.07f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.26f;
            var b = Util.Prim(PrimitiveType.Sphere, parent, new Vector3(Mathf.Cos(a) * 0.08f, 0.012f, Mathf.Sin(a) * 0.08f - 0.1f), new Vector3(0.03f, 0.018f, 0.022f), Color.white, false);
            b.GetComponent<Renderer>().sharedMaterial = bean;
            b.transform.localRotation = Quaternion.Euler(0f, a * 57f, 0f);
        }
    }

    static GameObject Model(string path, Transform parent, Vector3 pos, float yaw, Vector3 scale, List<Material> tinted)
    {
        var go = ModelLib.Spawn(path, parent, pos, yaw, scale);
        if (tinted != null) tinted.AddRange(ModelLib.OwnMaterials(go));
        return go;
    }

    // ---------- Kod bilan yasalgan qismlar ----------

    static void BunBottom(Transform parent, Vector3 pos)
    {
        MeshGen.Spawn("BunBottom", MeshGen.Puck(0.125f, 0.04f, 0.016f, 32), Util.Mat(BunColor), parent, pos);
        MeshGen.Spawn("Crumb", MeshGen.Puck(0.112f, 0.004f, 0.002f, 32), Util.Mat(BunInside), parent, pos + new Vector3(0f, 0.038f, 0f));
    }

    static void BunTop(Transform parent, Vector3 pos)
    {
        MeshGen.Spawn("BunTop", MeshGen.Dome(0.13f, 0.075f, 32), Util.Mat(BunColor), parent, pos);
        var seedMat = Util.Mat(new Color(0.99f, 0.95f, 0.82f));
        for (int i = 0; i < 9; i++)
        {
            float ang = i * 2.39996f;
            float rho = 0.25f + 0.55f * ((i * 7) % 9) / 9f;
            float x = Mathf.Cos(ang) * rho * 0.13f, z = Mathf.Sin(ang) * rho * 0.13f;
            float y = 0.075f * Mathf.Sqrt(Mathf.Max(0f, 1f - rho * rho));
            Vector3 normal = new Vector3(x / (0.13f * 0.13f), y / (0.075f * 0.075f), z / (0.13f * 0.13f)).normalized;
            var seed = Util.Prim(PrimitiveType.Sphere, parent, pos + new Vector3(x, y, z), new Vector3(0.011f, 0.006f, 0.019f), Color.white, false);
            seed.GetComponent<Renderer>().sharedMaterial = seedMat;
            seed.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            seed.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
        }
    }

    static void Flatbread(Transform parent, Vector3 pos, float yaw)
    {
        var mat = Util.Mat(Color.white, TextureGen.Flatbread());
        var bread = MeshGen.Spawn("Flatbread", MeshGen.Puck(0.2f, 0.008f, 0.004f, 40), mat, parent, pos);
        bread.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    static void SauceDollops(Transform parent, Vector3 pos)
    {
        var mat = Util.Mat(SauceColor);
        MeshGen.Spawn("Sauce", MeshGen.Dome(0.05f, 0.014f, 20), mat, parent, pos);
        MeshGen.Spawn("Sauce", MeshGen.Dome(0.03f, 0.01f, 16), mat, parent, pos + new Vector3(0.06f, 0f, 0.03f));
        MeshGen.Spawn("Sauce", MeshGen.Dome(0.028f, 0.009f, 16), mat, parent, pos + new Vector3(-0.05f, 0f, -0.04f));
    }

    static void LavashWrap(Transform parent, List<Material> tinted)
    {
        var bodyMat = Util.NewMat(Color.white);
        bodyMat.mainTexture = TextureGen.Flatbread();
        tinted.Add(bodyMat);
        var body = Util.Prim(PrimitiveType.Capsule, parent, new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.15f, 0.12f), Color.white, false);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        // Qog'oz o'ram va qizil chiziq
        var paper = Util.Prim(PrimitiveType.Cylinder, parent, new Vector3(0.075f, 0.06f, 0f), new Vector3(0.128f, 0.075f, 0.128f), PaperColor, false);
        paper.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        var stripe = Util.Prim(PrimitiveType.Cylinder, parent, new Vector3(0.04f, 0.06f, 0f), new Vector3(0.131f, 0.008f, 0.131f), new Color(0.85f, 0.15f, 0.12f), false);
        stripe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // ---------- Qo'ldagi / stoldagi ko'rinish ----------

    public static void BuildVisual(ItemType type, Transform parent, List<Material> tinted)
    {
        switch (type)
        {
            case ItemType.Bun:
                BunBottom(parent, Vector3.zero);
                BunTop(parent, new Vector3(0f, 0.042f, 0f));
                break;
            case ItemType.Patty:
                Model("Food/meat-patty", parent, Vector3.zero, 0f, Vector3.one * 0.8f, tinted);
                break;
            case ItemType.Cheese:
                Model("Food/cheese-cut", parent, Vector3.zero, 0f, new Vector3(0.62f, 0.62f, 0.85f), null);
                break;
            case ItemType.Tomato:
                Model("Food/tomato-slice", parent, Vector3.zero, 0f, new Vector3(1.15f, 0.7f, 1.15f), null);
                break;
            case ItemType.LavashBread:
                Flatbread(parent, Vector3.zero, 0f);
                break;
            case ItemType.Meat:
                for (int i = -1; i <= 1; i++)
                    Model("Food/bacon", parent, new Vector3(i * 0.05f, 0f, 0f), 0f, Vector3.one * 0.33f, tinted);
                break;
            case ItemType.Sauce:
                Model("Food/bottle-ketchup", parent, Vector3.zero, 0f, Vector3.one * 0.5f, null);
                break;
            case ItemType.Lavash:
                LavashWrap(parent, tinted);
                break;
            case ItemType.Burger:
                Model("Food/burger", parent, Vector3.zero, 0f, Vector3.one * 0.62f, null);
                break;
            case ItemType.Coffee:
                ModelLib.SpawnFit("Food/cup-coffee", parent, Vector3.zero, 0f, 0.095f, true);
                break;
            case ItemType.DirtyPlate:
                DirtyPlateModel(parent);
                break;
            case ItemType.Extinguisher:
                ExtinguisherModel(parent);
                break;
            case ItemType.PaperBag:
                ModelLib.SpawnFit("Food/bag", parent, Vector3.zero, 0f, 0.3f, true);
                break;
            case ItemType.CoffeeBeans:
                CoffeeBeansModel(parent);
                break;
        }
    }

    // Yig'ish taxtasidagi bitta qatlam. Burger noni pastda tekis, tepada gumbaz; sous — tomchilar.
    public static GameObject BuildLayer(ItemType type, Transform parent, bool asTop, float cook)
    {
        var root = Util.Node(GameData.Name(type), parent, Vector3.zero);
        if (type == ItemType.Bun)
        {
            if (asTop) BunTop(root.transform, Vector3.zero);
            else BunBottom(root.transform, Vector3.zero);
            return root;
        }
        if (type == ItemType.Sauce)
        {
            SauceDollops(root.transform, Vector3.zero);
            return root;
        }

        var tinted = new List<Material>();
        BuildVisual(type, root.transform, tinted);
        if (tinted.Count > 0)
        {
            Color raw, cooked, burnt;
            CookColors(type, out raw, out cooked, out burnt);
            Color c = Item.CookColor(cook, raw, cooked, burnt);
            for (int i = 0; i < tinted.Count; i++) tinted[i].color = c;
        }
        return root;
    }

    public static float LayerHeight(ItemType type, bool asTop)
    {
        switch (type)
        {
            case ItemType.Bun: return asTop ? 0.075f : 0.042f;
            case ItemType.Patty: return 0.05f;
            case ItemType.Cheese: return 0.013f;
            case ItemType.Tomato: return 0.022f;
            case ItemType.LavashBread: return 0.008f;
            case ItemType.Meat: return 0.03f;
            case ItemType.Sauce: return 0.012f;
        }
        return 0.03f;
    }

    // Ingredient lagani ichidagi ko'rgazma (zaxira bor bo'lsa ko'rinadi)
    public static void BuildBinDisplay(ItemType type, Transform parent)
    {
        switch (type)
        {
            case ItemType.Extinguisher:
                ExtinguisherModel(parent);
                break;
            case ItemType.Bun:
                for (int i = 0; i < 3; i++)
                {
                    var node = Util.Node("Bun", parent, new Vector3(-0.2f + i * 0.2f, 0f, (i % 2) * 0.12f - 0.05f)).transform;
                    BuildVisual(ItemType.Bun, node, null);
                }
                break;
            case ItemType.Patty:
                for (int i = 0; i < 3; i++)
                {
                    var p = Model("Food/meat-patty", parent, new Vector3(-0.2f + i * 0.2f, 0f, (i % 2) * 0.12f - 0.05f), i * 40f, Vector3.one * 0.8f, new List<Material>());
                    Color raw, cooked, burnt;
                    CookColors(ItemType.Patty, out raw, out cooked, out burnt);
                    foreach (var r in p.GetComponentsInChildren<Renderer>()) r.sharedMaterial.color = raw;
                }
                break;
            case ItemType.Cheese:
                ModelLib.Spawn("Food/cheese", parent, new Vector3(-0.12f, 0f, 0f), 20f, 0.42f);
                ModelLib.Spawn("Food/cheese-cut", parent, new Vector3(0.2f, 0f, -0.08f), 10f, new Vector3(0.62f, 0.62f, 0.85f));
                ModelLib.Spawn("Food/cheese-cut", parent, new Vector3(0.2f, 0.013f, 0.12f), -15f, new Vector3(0.62f, 0.62f, 0.85f));
                break;
            case ItemType.Tomato:
                ModelLib.Spawn("Food/tomato", parent, new Vector3(-0.2f, 0f, -0.08f), 0f, 0.6f);
                ModelLib.Spawn("Food/tomato", parent, new Vector3(0f, 0f, 0.1f), 50f, 0.6f);
                ModelLib.Spawn("Food/tomato-slice", parent, new Vector3(0.2f, 0f, -0.06f), 0f, new Vector3(1.15f, 0.7f, 1.15f));
                ModelLib.Spawn("Food/tomato-slice", parent, new Vector3(0.22f, 0.022f, -0.03f), 30f, new Vector3(1.15f, 0.7f, 1.15f));
                break;
            case ItemType.LavashBread:
                for (int i = 0; i < 5; i++) Flatbread(parent, new Vector3(0f, i * 0.009f, 0f), i * 35f);
                break;
            case ItemType.Meat:
                for (int i = 0; i < 5; i++)
                    ModelLib.Spawn("Food/bacon-raw", parent, new Vector3(-0.2f + i * 0.1f, 0f, (i % 2) * 0.06f), 0f, Vector3.one * 0.38f);
                break;
            case ItemType.Sauce:
                for (int i = 0; i < 3; i++)
                    ModelLib.Spawn("Food/bottle-ketchup", parent, new Vector3(-0.2f + i * 0.2f, 0f, (i % 2) * 0.1f - 0.03f), i * 25f, 0.55f);
                break;
        }
    }
}
