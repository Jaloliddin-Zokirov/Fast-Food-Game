using System.Collections.Generic;
using UnityEngine;

// Butun restoranni quradi: teksturali bino, Kenney mebellari (CC0), stansiyalar, o'yinchi va menejerlar.
// Oshxona z < 0 tomonda, mijozlar zali z > 0 tomonda. Kassa peshtaxtasi z = 0.5 da.
public static class WorldBuilder
{
    // Kenney mebellarining old tomoni model ichida -Z ga qaragan; +Z ga (o'yinchi/mijozga) qaratish uchun 180 gradus
    const float FrontYaw = 180f;
    const float CounterTop = 0.94f;
    const float ChairSeatHeight = 0.355f;  // Kenney chairRounded o'rindig'i (furniture masshtabida)
    const float DiningAisle = 4.05f;       // ikki qator stol orasidagi yo'lak

    static readonly Color Steel = new Color(0.72f, 0.74f, 0.77f);
    static readonly Color DarkSteel = new Color(0.25f, 0.26f, 0.29f);
    static readonly Color Red = new Color(0.85f, 0.2f, 0.14f);
    static readonly Color Text = new Color(0.15f, 0.12f, 0.1f);
    static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
    static readonly Quaternion FacingKitchenBack = Quaternion.Euler(0f, 180f, 0f); // o'yinchi -z ga qaraganda o'qiladi

    static Material floorKitchen, floorDining, wallKitchen, wallDining, ceiling, woodTrim;

    public static void Build(Transform root)
    {
        var managers = Util.Node("Managers", root, Vector3.zero);
        managers.AddComponent<SoundManager>();
        var gm = managers.AddComponent<GameManager>();
        managers.AddComponent<GameUI>();
        managers.AddComponent<IconRenderer>();
        var customers = managers.AddComponent<CustomerManager>();
        gm.customers = customers;

        CreateMaterials();
        var world = Util.Node("World", root, Vector3.zero).transform;
        BuildLighting(world);
        BuildRoom(world);
        BuildOutside(world);
        BuildKitchen(world);
        BuildServingCounter(world);
        BuildDining(world, customers);
        BuildSigns(world);
        BuildBins(world);
        gm.fridge = BuildFridge(world, new Vector3(-5.3f, 0f, -6.12f));
        gm.sink = BuildSink(world, new Vector3(-4.05f, 0f, -5.93f));
        gm.coffee = BuildCoffeeMachine(world, new Vector3(4.05f, 0f, -6.05f));

        gm.grill = BuildGrill(world, new Vector3(-2.7f, 0f, -2.6f));
        gm.press = BuildPress(world, new Vector3(-0.9f, 0f, -2.6f));
        gm.boards = new List<AssemblyBoard>
        {
            BuildBoard(world, new Vector3(0.75f, 0f, -2.6f)),
            BuildBoard(world, new Vector3(1.95f, 0f, -2.6f)),
            BuildBoard(world, new Vector3(3.15f, 0f, -2.6f))
        };
        BuildTrash(world, new Vector3(4.75f, 0f, -2.6f));
        BuildContainerStation(world, Container.Plate, new Vector3(-3.6f, 0f, -2.6f));
        BuildContainerStation(world, Container.Bag, new Vector3(-1.8f, 0f, -2.6f));
        BuildExtinguisherStand(world, new Vector3(-4.5f, 0f, -2.6f));
        BuildComputer(world, new Vector3(5.4f, 0f, -5.93f));
        BuildOpenSign(world, new Vector3(-5.3f, 0f, 0.3f));

        customers.Init(new[] { new Vector3(-2.6f, 0f, 1.45f), new Vector3(0f, 0f, 1.45f), new Vector3(2.6f, 0f, 1.45f) });

        gm.playerSpawn = new Vector3(0f, 0.05f, -1.0f);
        gm.playerSpawnYaw = 180f;
        // Bosh menyu fonida restoran zaldan ko'rinadi
        gm.menuViewPos = new Vector3(6.9f, 0.05f, 5.95f);
        gm.menuViewYaw = -135f;
        BuildPlayer(root, gm);
        gm.player.Teleport(gm.menuViewPos, gm.menuViewYaw);
        Effects.SetupPostProcessing(gm.interactor.cam, root);
    }

    static void CreateMaterials()
    {
        floorKitchen = Util.Mat(Color.white, TextureGen.CheckerTiles(new Color(0.93f, 0.91f, 0.86f), new Color(0.33f, 0.37f, 0.43f)), 1.25f);
        floorDining = Util.Mat(Color.white, TextureGen.WoodPlanks(new Color(0.8f, 0.57f, 0.37f)), 0.5f);
        wallKitchen = Util.Mat(Color.white, TextureGen.SubwayTiles(new Color(0.96f, 0.96f, 0.95f), new Color(0.68f, 0.68f, 0.68f)), 2f);
        wallDining = Util.Mat(Color.white, TextureGen.Plaster(new Color(0.98f, 0.87f, 0.7f)), 1f);
        ceiling = Util.Mat(new Color(0.97f, 0.96f, 0.93f));
        woodTrim = Util.Mat(Color.white, TextureGen.WoodPlanks(new Color(0.58f, 0.38f, 0.23f)), 1f);
    }

    // Kenney mebeli: facing — qaysi tomonga qarashi (0 = +Z)
    static GameObject Furniture(string name, Transform parent, Vector3 pos, float facing, bool collider)
    {
        var go = ModelLib.Spawn("Furniture/" + name, parent, pos, facing + FrontYaw, ModelLib.Furniture);
        if (collider) AddBoundsCollider(go);
        return go;
    }

    static void AddBoundsCollider(GameObject holder)
    {
        var model = holder.transform.Find("Model");
        if (model == null) return;
        Bounds b = ModelLib.LocalBounds(model.gameObject, holder.transform);
        Util.AddBox(holder, b.center, b.size);
    }

    // Ikki yo'lakdan ham o'qiladigan yozuv (har biri faqat o'z tomonidan ko'rinadi)
    static void TwoSidedLabel(string key, Transform parent, Vector3 pos, float height, Color color)
    {
        LocalizedLabel.Attach(Util.Label("", parent, pos, FacingKitchenBack, height, color), () => Loc.T(key));
        LocalizedLabel.Attach(Util.Label("", parent, pos, Quaternion.identity, height, color), () => Loc.T(key));
    }

    static void Solid(string name, Vector3 center, Vector3 size, Material mat, Transform parent)
    {
        MeshGen.Solid(name, size, 0f, mat, parent, center);
    }

    static void BuildLighting(Transform w)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.54f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.5f, 0.46f, 0.42f);
        RenderSettings.fog = false;

        var sun = Util.Node("Sun", w, Vector3.zero).AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
        sun.intensity = 1.25f;
        sun.color = new Color(1f, 0.95f, 0.86f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
    }

    static void BuildRoom(Transform w)
    {
        Solid("KitchenFloor", new Vector3(0f, -0.05f, -3f), new Vector3(16.4f, 0.1f, 7f), floorKitchen, w);
        Solid("DiningFloor", new Vector3(0f, -0.05f, 3.6f), new Vector3(16.4f, 0.1f, 6.2f), floorDining, w);

        Solid("BackWall", new Vector3(0f, 1.65f, -6.6f), new Vector3(16.4f, 3.3f, 0.2f), wallKitchen, w);
        Solid("WallL_Kitchen", new Vector3(-8.1f, 1.65f, -3.05f), new Vector3(0.2f, 3.3f, 7.1f), wallKitchen, w);
        Solid("WallR_Kitchen", new Vector3(8.1f, 1.65f, -3.05f), new Vector3(0.2f, 3.3f, 7.1f), wallKitchen, w);
        Solid("WallL_Dining", new Vector3(-8.1f, 1.65f, 3.6f), new Vector3(0.2f, 3.3f, 6.2f), wallDining, w);
        Solid("WallR_Dining", new Vector3(8.1f, 1.65f, 3.6f), new Vector3(0.2f, 3.3f, 6.2f), wallDining, w);
        Solid("FrontL", new Vector3(-4.6f, 1.65f, 6.6f), new Vector3(7.2f, 3.3f, 0.2f), wallDining, w);
        Solid("FrontR", new Vector3(4.6f, 1.65f, 6.6f), new Vector3(7.2f, 3.3f, 0.2f), wallDining, w);
        Solid("FrontTop", new Vector3(0f, 2.9f, 6.6f), new Vector3(2f, 0.8f, 0.2f), wallDining, w);

        // Zal devorlarining pastki qismi — yog'och panel
        Solid("PanelL", new Vector3(-7.98f, 0.5f, 3.6f), new Vector3(0.04f, 1f, 6.2f), woodTrim, w);
        Solid("PanelR", new Vector3(7.98f, 0.5f, 3.6f), new Vector3(0.04f, 1f, 6.2f), woodTrim, w);
        Solid("PanelFL", new Vector3(-4.6f, 0.5f, 6.48f), new Vector3(7.2f, 1f, 0.04f), woodTrim, w);
        Solid("PanelFR", new Vector3(4.6f, 0.5f, 6.48f), new Vector3(7.2f, 1f, 0.04f), woodTrim, w);

        // Derazalar va eshik romi
        var glass = Util.Glow(new Color(0.6f, 0.78f, 0.95f), 0.12f);
        var frame = Util.Mat(new Color(0.42f, 0.28f, 0.17f));
        foreach (float x in new[] { -4.5f, 4.5f })
        {
            MeshGen.Spawn("Window", MeshGen.RoundedBox(new Vector3(2.6f, 1.3f, 0.02f), 0f), glass, w, new Vector3(x, 1.85f, 6.5f));
            MeshGen.Spawn("WindowFrame", MeshGen.RoundedBox(new Vector3(2.75f, 0.08f, 0.08f), 0.02f), frame, w, new Vector3(x, 1.18f, 6.48f));
            MeshGen.Spawn("WindowFrame", MeshGen.RoundedBox(new Vector3(2.75f, 0.08f, 0.08f), 0.02f), frame, w, new Vector3(x, 2.52f, 6.48f));
            MeshGen.Spawn("WindowFrame", MeshGen.RoundedBox(new Vector3(0.08f, 1.42f, 0.08f), 0.02f), frame, w, new Vector3(x - 1.34f, 1.85f, 6.48f));
            MeshGen.Spawn("WindowFrame", MeshGen.RoundedBox(new Vector3(0.08f, 1.42f, 0.08f), 0.02f), frame, w, new Vector3(x + 1.34f, 1.85f, 6.48f));
            MeshGen.Spawn("WindowFrame", MeshGen.RoundedBox(new Vector3(0.05f, 1.3f, 0.05f), 0.015f), frame, w, new Vector3(x, 1.85f, 6.49f));
        }
        MeshGen.Spawn("DoorFrame", MeshGen.RoundedBox(new Vector3(0.12f, 2.5f, 0.26f), 0.03f), frame, w, new Vector3(-1.04f, 1.25f, 6.6f));
        MeshGen.Spawn("DoorFrame", MeshGen.RoundedBox(new Vector3(0.12f, 2.5f, 0.26f), 0.03f), frame, w, new Vector3(1.04f, 1.25f, 6.6f));
        MeshGen.Spawn("DoorFrame", MeshGen.RoundedBox(new Vector3(2.2f, 0.12f, 0.26f), 0.03f), frame, w, new Vector3(0f, 2.5f, 6.6f));
        MeshGen.Spawn("DoorMat", MeshGen.RoundedBox(new Vector3(1.6f, 0.015f, 0.9f), 0.007f), Util.Mat(new Color(0.55f, 0.15f, 0.12f)), w, new Vector3(0f, 0.008f, 5.9f));

        // O'yinchi eshikdan tashqariga chiqib ketmasligi uchun ko'rinmas to'siq (mijozlar fizikadan foydalanmaydi)
        Util.AddBox(Util.Node("DoorBlocker", w, new Vector3(0f, 1.25f, 6.6f)), Vector3.zero, new Vector3(2f, 2.5f, 0.3f));

        // Shift soya tashlamaydi — quyosh nuri ichkarini yoritishda davom etadi
        var ceilingGo = MeshGen.Spawn("Ceiling", MeshGen.RoundedBox(new Vector3(16.4f, 0.1f, 13.4f), 0f), ceiling, w, new Vector3(0f, 3.35f, 0f));
        ceilingGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void BuildOutside(Transform w)
    {
        MeshGen.Spawn("Ground", MeshGen.RoundedBox(new Vector3(80f, 0.1f, 80f), 0f), Util.Mat(new Color(0.45f, 0.62f, 0.32f)), w, new Vector3(0f, -0.1f, 0f));
        MeshGen.Spawn("Sidewalk", MeshGen.RoundedBox(new Vector3(18f, 0.04f, 3f), 0f), Util.Mat(new Color(0.72f, 0.71f, 0.68f)), w, new Vector3(0f, -0.02f, 8.3f));
        MeshGen.Spawn("Road", MeshGen.RoundedBox(new Vector3(60f, 0.03f, 6f), 0f), Util.Mat(new Color(0.25f, 0.25f, 0.27f)), w, new Vector3(0f, -0.03f, 13f));

        var trunk = Util.Mat(new Color(0.42f, 0.28f, 0.18f));
        var leaves = Util.Mat(new Color(0.28f, 0.6f, 0.3f));
        Vector3[] trees = { new Vector3(-6f, 0f, 9.2f), new Vector3(-10f, 0f, 8.6f), new Vector3(6.5f, 0f, 9.4f), new Vector3(10.5f, 0f, 8.8f), new Vector3(-3.5f, 0f, 17f), new Vector3(4f, 0f, 17.5f) };
        for (int i = 0; i < trees.Length; i++)
        {
            var t = Util.Node("Tree", w, trees[i]).transform;
            MeshGen.Spawn("Trunk", MeshGen.Puck(0.1f, 1.4f, 0.03f, 12), trunk, t, Vector3.zero);
            Util.Prim(PrimitiveType.Sphere, t, new Vector3(0f, 1.9f, 0f), new Vector3(1.5f, 1.3f, 1.5f), leaves.color, false).GetComponent<Renderer>().sharedMaterial = leaves;
            Util.Prim(PrimitiveType.Sphere, t, new Vector3(0.35f, 2.4f, 0.1f), new Vector3(1f, 0.9f, 1f), leaves.color, false).GetComponent<Renderer>().sharedMaterial = leaves;
        }
    }

    static void BuildKitchen(Transform w)
    {
        // Orqa peshtaxta (ingredientlar) — eshiklari o'yinchiga qaragan
        for (int i = 0; i < 8; i++)
            Furniture(i % 3 == 1 ? "kitchenCabinetDrawer" : "kitchenCabinet", w, new Vector3(-3.15f + 0.9f * i, 0f, -5.93f), 0f, true);
        Furniture("kitchenCabinet", w, new Vector3(4.05f, 0f, -5.93f), 0f, true);

        // O'rtadagi orolcha (gril o'rnida plita turadi)
        float[] xs = { -3.6f, -1.8f, -0.9f, 0f, 0.9f, 1.8f, 2.7f, 3.6f };
        for (int i = 0; i < xs.Length; i++)
            Furniture(i % 2 == 0 ? "kitchenCabinetDrawer" : "kitchenCabinet", w, new Vector3(xs[i], 0f, -2.6f), 0f, true);

        // Plita ustidagi so'rg'ich va uning quvuri
        Furniture("hoodModern", w, new Vector3(-2.7f, 2.05f, -2.6f), 0f, false);
        MeshGen.Spawn("Chimney", MeshGen.RoundedBox(new Vector3(0.28f, 0.46f, 0.28f), 0.02f), Util.Mat(Steel), w, new Vector3(-2.7f, 3.08f, -2.6f));

        // Shiftdagi chiroqlar
        Vector3[] lamps = { new Vector3(-3f, 0f, -4.2f), new Vector3(3f, 0f, -4.2f), new Vector3(0f, 0f, -1.3f), new Vector3(-4.5f, 0f, 3.6f), new Vector3(0f, 0f, 3.8f), new Vector3(4.5f, 0f, 3.6f) };
        for (int i = 0; i < lamps.Length; i++) Lamp(w, lamps[i]);
    }

    static void Lamp(Transform w, Vector3 pos)
    {
        var lamp = Furniture("lampSquareCeiling", w, new Vector3(pos.x, 3.3f - 0.48f, pos.z), 0f, false);
        var glow = Util.Glow(new Color(1f, 0.86f, 0.62f), 1.5f);
        foreach (var r in lamp.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.StartsWith("lamp")) mats[i] = glow;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var light = Util.Node("LampLight", w, new Vector3(pos.x, 2.55f, pos.z)).AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 7f;
        light.intensity = 1.1f;
        light.color = new Color(1f, 0.87f, 0.68f);
        light.shadows = LightShadows.None;
    }

    static void BuildServingCounter(Transform w)
    {
        // Kenney bar peshtaxtasi: balandligi 1 m bo'lishi uchun masshtab
        float scale = 1f / 4.2f;
        for (int i = 0; i < 9; i++)
            AddBoundsCollider(ModelLib.Spawn("Furniture/kitchenBar", w, new Vector3(-4.08f + 1.02f * i, 0f, 0.5f), FrontYaw, scale));
        AddBoundsCollider(ModelLib.Spawn("Furniture/kitchenBarEnd", w, new Vector3(-4.71f, 0f, 0.5f), FrontYaw + 180f, scale));
        AddBoundsCollider(ModelLib.Spawn("Furniture/kitchenBarEnd", w, new Vector3(4.71f, 0f, 0.5f), FrontYaw, scale));
        Furniture("plantSmall1", w, new Vector3(-4.2f, 1f, 0.5f), 0f, false);
        Furniture("plantSmall1", w, new Vector3(4.2f, 1f, 0.5f), 0f, false);
    }

    // Zal: har stolda ikkita o'rindiq — shu yerda yeydigan mijozlar o'tiradi
    static void BuildDining(Transform w, CustomerManager customers)
    {
        Vector3[] tables = { new Vector3(-5.6f, 0f, 3f), new Vector3(-5.6f, 0f, 5.1f), new Vector3(5.6f, 0f, 3f), new Vector3(5.6f, 0f, 5.1f) };
        for (int i = 0; i < tables.Length; i++)
        {
            var table = Furniture("tableRound", w, tables[i], 0f, true);
            Bounds top = ModelLib.LocalBounds(table.transform.Find("Model").gameObject, table.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 chair = tables[i] + new Vector3(side, 0f, 0f);
                float yaw = side < 0 ? 90f : -90f;
                Furniture("chairRounded", w, chair, yaw, true);

                var seat = new Seat();
                seat.chair = chair;
                seat.yaw = yaw;
                seat.seatHeight = ChairSeatHeight;
                seat.table = tables[i] + Vector3.up * top.max.y;
                seat.tableRadius = Mathf.Min(top.extents.x, top.extents.z);
                // Stullarga ikki stol orasidagi yo'lakdan kiriladi
                seat.route = new[] { new Vector3(Mathf.Sign(tables[i].x) * 3f, 0f, DiningAisle), new Vector3(chair.x, 0f, DiningAisle) };
                customers.AddSeat(seat);
            }
        }

        Vector3[] plants = { new Vector3(-7.5f, 0f, 6.05f), new Vector3(7.5f, 0f, 6.05f), new Vector3(-7.5f, 0f, 1.3f), new Vector3(7.5f, 0f, 1.3f), new Vector3(-7.5f, 0f, -0.4f) };
        for (int i = 0; i < plants.Length; i++) Furniture("pottedPlant", w, plants[i], i * 70f, true);
    }

    static void BuildSigns(Transform w)
    {
        MeshGen.Spawn("TitleFrame", MeshGen.RoundedBox(new Vector3(5.2f, 1f, 0.06f), 0.12f), Util.Mat(new Color(0.2f, 0.12f, 0.1f)), w, new Vector3(0f, 2.6f, -6.47f));
        MeshGen.Spawn("TitleBoard", MeshGen.RoundedBox(new Vector3(5f, 0.82f, 0.08f), 0.1f), Util.Mat(Red), w, new Vector3(0f, 2.6f, -6.44f));
        Util.Label("LAVASH & BURGER", w, new Vector3(0f, 2.6f, -6.39f), FacingKitchenBack, 0.42f, Yellow);

        // Kassa tepasidagi narxlar taxtasi (oshxona tomonga qaragan) va uning ilgaklari
        var dark = Util.Mat(new Color(0.16f, 0.15f, 0.15f));
        MeshGen.Spawn("MenuBoard", MeshGen.RoundedBox(new Vector3(4.4f, 0.62f, 0.06f), 0.08f), dark, w, new Vector3(0f, 2.45f, 0.5f));
        MeshGen.Spawn("Rod", MeshGen.Puck(0.012f, 0.6f, 0.004f, 8), dark, w, new Vector3(-1.9f, 2.76f, 0.5f));
        MeshGen.Spawn("Rod", MeshGen.Puck(0.012f, 0.6f, 0.004f, 8), dark, w, new Vector3(1.9f, 2.76f, 0.5f));
        System.Func<string> prices = () =>
            GameData.Name(ItemType.Burger).ToUpper() + "  " + GameData.Money(GameData.BurgerPrice) + "    " +
            GameData.Name(ItemType.Lavash).ToUpper() + "  " + GameData.Money(GameData.LavashPrice) + "    " +
            GameData.Name(ItemType.Coffee).ToUpper() + "  " + GameData.Money(GameData.CoffeePrice);
        LocalizedLabel.Attach(Util.Label("", w, new Vector3(0f, 2.45f, 0.465f), Quaternion.identity, 0.14f, Yellow), prices);
        LocalizedLabel.Attach(Util.Label("", w, new Vector3(0f, 2.45f, 0.535f), FacingKitchenBack, 0.14f, Yellow), prices);
    }

    // Orqa peshtaxtadagi ingredient lagan'lari (kotlet va go'sht esa muzlatgichda)
    static void BuildBins(Transform w)
    {
        ItemType[] order = { ItemType.Bun, ItemType.Cheese, ItemType.Tomato, ItemType.LavashBread, ItemType.Sauce };
        var trayMat = Util.Mat(Steel);
        var trayInner = Util.Mat(new Color(0.55f, 0.57f, 0.6f));
        var boardMat = Util.Mat(new Color(0.98f, 0.97f, 0.94f));
        for (int i = 0; i < order.Length; i++)
        {
            ItemType type = order[i];
            var go = Util.Node("Bin " + type, w, new Vector3(-2f + i, 0f, -5.93f));
            var bin = go.AddComponent<IngredientBin>();
            bin.type = type;
            Util.AddBox(go, new Vector3(0f, 1.12f, 0.05f), new Vector3(0.9f, 0.36f, 0.8f));
            MeshGen.Spawn("Tray", MeshGen.RoundedBox(new Vector3(0.84f, 0.05f, 0.72f), 0.02f), trayMat, go.transform, new Vector3(0f, CounterTop + 0.025f, 0.05f));
            MeshGen.Spawn("TrayInner", MeshGen.RoundedBox(new Vector3(0.78f, 0.01f, 0.66f), 0.004f), trayInner, go.transform, new Vector3(0f, CounterTop + 0.05f, 0.05f));

            bin.samples = Util.Node("Samples", go.transform, new Vector3(0f, CounterTop + 0.055f, 0.05f));
            ItemFactory.BuildBinDisplay(type, bin.samples.transform);

            MeshGen.Spawn("LabelBoard", MeshGen.RoundedBox(new Vector3(0.78f, 0.3f, 0.03f), 0.05f), boardMat, go.transform, new Vector3(0f, 1.32f, -0.42f));
            bin.label = Util.Label(GameData.Name(type), go.transform, new Vector3(0f, 1.32f, -0.4f), FacingKitchenBack, 0.085f, Text);
        }
    }

    static CookStation BuildGrill(Transform w, Vector3 position)
    {
        var go = Util.Node("Grill", w, position);
        Furniture("kitchenStoveElectric", go.transform, Vector3.zero, 0f, false);
        Util.AddBox(go, new Vector3(0f, 0.62f, 0f), new Vector3(0.9f, 1.24f, 0.94f));
        TwoSidedLabel("label.grill", go.transform, new Vector3(0f, 1.5f, 0.35f), 0.13f, Red);

        // Dastlabki 2 ta konfork old tomonda, yaxshilanishdan keyin orqadagilari qo'shiladi
        Vector3[] hobs =
        {
            new Vector3(-0.2f, CounterTop + 0.006f, 0.19f), new Vector3(0.2f, CounterTop + 0.006f, 0.19f),
            new Vector3(-0.2f, CounterTop + 0.006f, -0.19f), new Vector3(0.2f, CounterTop + 0.006f, -0.19f)
        };
        var hot = Util.Glow(new Color(1f, 0.2f, 0.05f), 1.1f);
        var slots = new Transform[hobs.Length];
        for (int i = 0; i < hobs.Length; i++)
        {
            slots[i] = Util.Node("Slot", go.transform, hobs[i]).transform;
            MeshGen.Spawn("Hob", MeshGen.Puck(0.15f, 0.004f, 0.002f, 32), hot, slots[i], new Vector3(0f, -0.004f, 0f));
        }

        var station = go.AddComponent<CookStation>();
        station.canCatchFire = true;
        station.Init("station.grill", new[] { ItemType.Patty, ItemType.Meat }, slots);
        return station;
    }

    static CookStation BuildPress(Transform w, Vector3 position)
    {
        var go = Util.Node("Press", w, position);
        Util.AddBox(go, new Vector3(0f, CounterTop + 0.22f, 0f), new Vector3(0.8f, 0.44f, 0.62f));
        var steel = Util.Mat(new Color(0.6f, 0.62f, 0.66f));
        var dark = Util.Mat(DarkSteel);
        MeshGen.Spawn("PressBase", MeshGen.RoundedBox(new Vector3(0.74f, 0.1f, 0.56f), 0.035f), steel, go.transform, new Vector3(0f, CounterTop + 0.05f, 0f));
        MeshGen.Spawn("PressPlate", MeshGen.RoundedBox(new Vector3(0.64f, 0.012f, 0.46f), 0.005f), dark, go.transform, new Vector3(0f, CounterTop + 0.104f, 0f));
        MeshGen.Spawn("HingePost", MeshGen.RoundedBox(new Vector3(0.04f, 0.17f, 0.05f), 0.012f), dark, go.transform, new Vector3(-0.3f, CounterTop + 0.18f, -0.27f));
        MeshGen.Spawn("HingePost", MeshGen.RoundedBox(new Vector3(0.04f, 0.17f, 0.05f), 0.012f), dark, go.transform, new Vector3(0.3f, CounterTop + 0.18f, -0.27f));
        MeshGen.Spawn("HingeRod", MeshGen.RoundedBox(new Vector3(0.64f, 0.035f, 0.035f), 0.015f), dark, go.transform, new Vector3(0f, CounterTop + 0.26f, -0.27f));
        TwoSidedLabel("label.press", go.transform, new Vector3(0f, 1.62f, 0f), 0.13f, Red);

        // Lavash ko'ndalang yotishi uchun joylar 90 gradusga burilgan
        float[] xs = { -0.16f, 0.16f };
        var warm = Util.Glow(new Color(1f, 0.25f, 0.06f), 0.7f);
        var slots = new Transform[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            slots[i] = Util.Node("Slot", go.transform, new Vector3(xs[i], CounterTop + 0.112f, 0.02f)).transform;
            slots[i].localRotation = Quaternion.Euler(0f, 90f, 0f);
            MeshGen.Spawn("Heat", MeshGen.RoundedBox(new Vector3(0.3f, 0.003f, 0.13f), 0.001f), warm, slots[i], Vector3.zero);
        }

        // Qopqoq orqa sharnirda: pishirish paytida yopiladi, bo'sh turganda ochiq
        var lid = Util.Node("Lid", go.transform, new Vector3(0f, CounterTop + 0.26f, -0.26f)).transform;
        lid.localRotation = Quaternion.Euler(-70f, 0f, 0f);
        MeshGen.Spawn("LidTop", MeshGen.RoundedBox(new Vector3(0.74f, 0.06f, 0.56f), 0.03f), steel, lid, new Vector3(0f, 0f, 0.28f));
        MeshGen.Spawn("Handle", MeshGen.RoundedBox(new Vector3(0.5f, 0.035f, 0.05f), 0.015f), dark, lid, new Vector3(0f, 0.035f, 0.55f));

        var station = go.AddComponent<CookStation>();
        station.lid = lid;
        station.Init("station.press", new[] { ItemType.Lavash }, slots);
        return station;
    }

    // Muzlatgich: ikki eshikli, chapda kotletlar, o'ngda go'sht. Har bir eshik alohida ochiladi.
    static Fridge BuildFridge(Transform w, Vector3 position)
    {
        const float W = 1.1f, D = 0.74f, H = 1.95f, T = 0.04f, Plinth = 0.08f;
        var go = Util.Node("Fridge", w, position);
        var root = go.transform;
        var shell = Util.Mat(new Color(0.86f, 0.88f, 0.9f));
        var lining = Util.Mat(new Color(0.97f, 0.98f, 1f));
        var dark = Util.Mat(DarkSteel);
        var shelfMat = Util.Mat(new Color(0.78f, 0.88f, 0.93f));
        float wallY = (H + Plinth) / 2f;

        MeshGen.Spawn("Plinth", MeshGen.RoundedBox(new Vector3(W - 0.04f, Plinth, D - 0.06f), 0.01f), dark, root, new Vector3(0f, Plinth / 2f, -0.03f));
        MeshGen.Solid("Back", new Vector3(W, H - Plinth, T), 0.01f, shell, root, new Vector3(0f, wallY, -D / 2f + T / 2f));
        MeshGen.Solid("SideL", new Vector3(T, H - Plinth, D), 0.01f, shell, root, new Vector3(-W / 2f + T / 2f, wallY, 0f));
        MeshGen.Solid("SideR", new Vector3(T, H - Plinth, D), 0.01f, shell, root, new Vector3(W / 2f - T / 2f, wallY, 0f));
        MeshGen.Solid("Top", new Vector3(W, T, D), 0.01f, shell, root, new Vector3(0f, H - T / 2f, 0f));
        MeshGen.Solid("Bottom", new Vector3(W, T, D), 0.005f, lining, root, new Vector3(0f, Plinth + T / 2f, 0f));
        MeshGen.Solid("Divider", new Vector3(T, H - Plinth, D), 0.01f, shell, root, new Vector3(0f, wallY, 0f));
        MeshGen.Spawn("Lining", MeshGen.RoundedBox(new Vector3(W - 2f * T, H - Plinth - 2f * T, 0.01f), 0f), lining, root, new Vector3(0f, wallY, -D / 2f + T + 0.006f));

        float[] shelves = { 0.55f, 1.0f, 1.45f };
        float compartment = W / 2f - 1.5f * T;
        for (int s = 0; s < shelves.Length; s++)
            for (int side = -1; side <= 1; side += 2)
                MeshGen.Spawn("Shelf", MeshGen.RoundedBox(new Vector3(compartment, 0.015f, D - T - 0.04f), 0.005f), shelfMat, root, new Vector3(side * W / 4f, shelves[s], 0f));

        var fridge = go.AddComponent<Fridge>();
        var lamp = Util.Node("FridgeLight", root, new Vector3(0f, 1.7f, 0.15f)).AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = 1.6f;
        lamp.color = new Color(0.85f, 0.95f, 1f);
        lamp.shadows = LightShadows.None;
        lamp.intensity = 0f;
        fridge.lamp = lamp;

        ItemType[] contents = { ItemType.Patty, ItemType.Meat };
        var boardMat = Util.Mat(new Color(0.98f, 0.97f, 0.94f));
        for (int d = 0; d < contents.Length; d++)
        {
            float side = d == 0 ? -1f : 1f;

            // Zaxira: bo'lim ichidagi collider (eshik ochiq bo'lgandagina yoqiladi) va javonlardagi namunalar
            var binGo = Util.Node("Bin " + contents[d], root, new Vector3(side * W / 4f, 0f, 0f));
            var bin = binGo.AddComponent<IngredientBin>();
            bin.type = contents[d];
            Util.AddBox(binGo, new Vector3(0f, 1.05f, 0.02f), new Vector3(compartment, 1f, D - T - 0.06f));
            bin.samples = Util.Node("Samples", binGo.transform, Vector3.zero);
            for (int s = 1; s < shelves.Length; s++) FridgeShelfFood(contents[d], bin.samples.transform, shelves[s] + 0.008f);

            // Eshik: ilmog'i tashqi chetda, ichki chetida tutqich, old tomonida nomi va soni
            var pivot = Util.Node("DoorPivot", root, new Vector3(side * W / 2f, Plinth + 0.01f, D / 2f + 0.03f)).transform;
            float doorW = W / 2f - 0.008f;
            float doorH = H - Plinth - 0.02f;
            var door = MeshGen.Spawn("Door", MeshGen.RoundedBox(new Vector3(doorW, doorH, 0.06f), 0.02f), shell, pivot, new Vector3(-side * doorW / 2f, doorH / 2f, 0f));
            Util.AddBox(door, Vector3.zero, new Vector3(doorW, doorH, 0.06f));
            MeshGen.Spawn("Handle", MeshGen.RoundedBox(new Vector3(0.03f, 0.55f, 0.035f), 0.012f), dark, pivot, new Vector3(-side * (doorW - 0.06f), doorH * 0.55f, 0.05f));
            MeshGen.Spawn("LabelBoard", MeshGen.RoundedBox(new Vector3(0.36f, 0.2f, 0.01f), 0.03f), boardMat, pivot, new Vector3(-side * doorW / 2f, 1.5f, 0.035f));
            bin.label = Util.Label("", pivot, new Vector3(-side * doorW / 2f, 1.5f, 0.042f), FacingKitchenBack, 0.06f, Text);
            fridge.AddDoor(pivot, side < 0f ? -105f : 105f, bin);
        }

        LocalizedLabel.Attach(Util.Label("", root, new Vector3(0f, H + 0.13f, D / 2f - 0.05f), FacingKitchenBack, 0.1f, Text), () => Loc.T("label.fridge"));
        return fridge;
    }

    // Muzlatgich javonidagi namunalar: ikkita xom kotlet yoki ikki dasta go'sht bo'lagi (ko'ndalang yotadi)
    static void FridgeShelfFood(ItemType type, Transform parent, float y)
    {
        bool meat = type == ItemType.Meat;
        for (int i = -1; i <= 1; i += 2)
        {
            var food = ItemFactory.BuildLayer(type, parent, false, 0f);
            food.transform.localPosition = new Vector3(i * 0.12f, y, 0.05f);
            food.transform.localRotation = Quaternion.Euler(0f, meat ? 90f + i * 12f : i * 25f, 0f);
            if (meat) food.transform.localScale = Vector3.one * 1.3f;
        }
    }

    // Orqa devordagi kichik yozuv taxtachasi (ingredient lagan'laridagi kabi)
    static void WallSign(string key, Transform parent, float y)
    {
        MeshGen.Spawn("LabelBoard", MeshGen.RoundedBox(new Vector3(0.62f, 0.18f, 0.03f), 0.05f), Util.Mat(new Color(0.98f, 0.97f, 0.94f)), parent, new Vector3(0f, y, -0.42f));
        LocalizedLabel.Attach(Util.Label("", parent, new Vector3(0f, y, -0.4f), FacingKitchenBack, 0.075f, Text), () => Loc.T(key));
    }

    // Rakovina: iflos tarelkalar shu yerda yuviladi
    static Sink BuildSink(Transform w, Vector3 position)
    {
        var go = Util.Node("Sink", w, position);
        Furniture("kitchenSink", go.transform, Vector3.zero, 0f, false);
        Util.AddBox(go, new Vector3(0f, (CounterTop + 0.12f) / 2f, 0f), new Vector3(0.9f, CounterTop + 0.12f, 0.9f));
        WallSign("label.sink", go.transform, 1.45f);
        var sink = go.AddComponent<Sink>();
        // Tarelka chanoq tubida, jo'mrak tagida turadi
        sink.plateSpot = Util.Node("PlateSpot", go.transform, new Vector3(0f, 0.68f, -0.05f)).transform;
        return sink;
    }

    // Qahva mashinasi orqa peshtaxtada. Kenney modelida jo'mrak tagida o'zining kulrang krujkasi ("mug") bor:
    // qahva tayyorlanayotganda u yashiriladi va aynan o'sha joyda o'yindagi qahvali stakan turadi.
    static CoffeeMachine BuildCoffeeMachine(Transform w, Vector3 position)
    {
        var go = Util.Node("CoffeeMachine", w, position);
        var model = Furniture("kitchenCoffeeMachine", go.transform, new Vector3(0f, CounterTop, 0f), 0f, false);
        Bounds b = ModelLib.LocalBounds(model.transform.Find("Model").gameObject, go.transform);
        Util.AddBox(go, b.center + new Vector3(0f, 0f, 0.05f), b.size + new Vector3(0.04f, 0.04f, 0.1f));
        WallSign("label.coffee", go.transform, Mathf.Max(1.6f, b.max.y + 0.2f));
        var machine = go.AddComponent<CoffeeMachine>();

        MeshRenderer mug = null;
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            if (filter.sharedMesh != null && filter.sharedMesh.name == "mug") mug = filter.GetComponent<MeshRenderer>();
        Vector3 spot = new Vector3(b.center.x, CounterTop + 0.073f, b.center.z + 0.09f);
        if (mug != null)
        {
            Bounds m = ModelLib.LocalBounds(mug.gameObject, go.transform);
            spot = new Vector3(m.center.x, m.min.y, m.center.z);
        }
        machine.idleMug = mug;
        machine.cupPoint = Util.Node("CupPoint", go.transform, spot).transform;
        // Dastasi o'ng tomonga (o'yinchiga ko'rinadigan qilib)
        machine.cupPoint.localRotation = Quaternion.Euler(0f, 90f, 0f);
        return machine;
    }

    // Toza tarelkalar to'plami yoki qog'oz paketlar: tayyor taom shu yerda tarelkaga qo'yiladi yoki paketga solinadi
    static void BuildContainerStation(Transform w, Container kind, Vector3 position)
    {
        bool plates = kind == Container.Plate;
        var go = Util.Node(plates ? "Plates" : "Bags", w, position);
        Util.AddBox(go, new Vector3(0f, CounterTop + 0.17f, 0f), new Vector3(0.6f, 0.34f, 0.6f));
        var station = go.AddComponent<ContainerStation>();
        station.kind = kind;
        if (plates)
        {
            for (int i = 0; i < GameData.PlateCount; i++)
                station.AddStackVisual(ModelLib.SpawnFit("Food/plate", go.transform, new Vector3(0f, CounterTop + i * 0.018f, 0f), i * 17f, 0.3f, false));
        }
        else
        {
            station.AddStackVisual(ModelLib.SpawnFit("Food/bag", go.transform, new Vector3(-0.12f, CounterTop, 0.04f), 12f, 0.3f, true));
            station.AddStackVisual(ModelLib.SpawnFit("Food/bag", go.transform, new Vector3(0.13f, CounterTop, -0.06f), -20f, 0.3f, true));
        }
        TwoSidedLabel(plates ? "label.plates" : "label.bags", go.transform, new Vector3(0f, 1.45f, 0f), 0.085f, Text);
    }

    // O't o'chirgich turadigan joy — grilning yonida. Ishlatilgani kompyuterdan qayta sotib olinadi.
    static void BuildExtinguisherStand(Transform w, Vector3 position)
    {
        var go = Util.Node("ExtinguisherStand", w, position);
        var dark = Util.Mat(DarkSteel);
        MeshGen.Spawn("Base", MeshGen.RoundedBox(new Vector3(0.32f, 0.04f, 0.32f), 0.015f), dark, go.transform, new Vector3(0f, 0.02f, 0f));
        MeshGen.Spawn("Post", MeshGen.RoundedBox(new Vector3(0.05f, 1.3f, 0.05f), 0.015f), dark, go.transform, new Vector3(-0.17f, 0.65f, 0f));
        MeshGen.Spawn("Sign", MeshGen.RoundedBox(new Vector3(0.66f, 0.17f, 0.03f), 0.04f), Util.Mat(Red), go.transform, new Vector3(0f, 1.25f, 0f));
        // Belgi ikki tomondan ham o'qiladi (oldi va orqasidagi yo'lakdan)
        LocalizedLabel.Attach(Util.Label("", go.transform, new Vector3(0f, 1.25f, 0.02f), FacingKitchenBack, 0.042f, Color.white), () => Loc.T("label.extinguisher"));
        LocalizedLabel.Attach(Util.Label("", go.transform, new Vector3(0f, 1.25f, -0.02f), Quaternion.identity, 0.042f, Color.white), () => Loc.T("label.extinguisher"));

        var bin = go.AddComponent<IngredientBin>();
        bin.type = ItemType.Extinguisher;
        Util.AddBox(go, new Vector3(0f, 0.38f, 0f), new Vector3(0.42f, 0.76f, 0.42f));
        bin.samples = Util.Node("Samples", go.transform, new Vector3(0.04f, 0.04f, 0f));
        ItemFactory.BuildBinDisplay(ItemType.Extinguisher, bin.samples.transform);
    }

    static AssemblyBoard BuildBoard(Transform w, Vector3 position)
    {
        var go = Util.Node("Board", w, position);
        Util.AddBox(go, new Vector3(0f, CounterTop + 0.15f, 0f), new Vector3(0.7f, 0.3f, 0.55f));
        ModelLib.Spawn("Food/cutting-board", go.transform, new Vector3(0f, CounterTop, 0f), 90f, 0.85f);
        var board = go.AddComponent<AssemblyBoard>();
        board.stackRoot = Util.Node("Stack", go.transform, new Vector3(0f, CounterTop + 0.051f, 0f)).transform;
        return board;
    }

    static void BuildTrash(Transform w, Vector3 position)
    {
        var go = Util.Node("Trash", w, position);
        Util.AddBox(go, new Vector3(0f, 0.48f, 0f), new Vector3(0.5f, 0.96f, 0.55f));
        Furniture("trashcan", go.transform, Vector3.zero, 0f, false);
        TwoSidedLabel("label.trash", go.transform, new Vector3(0f, 1.15f, 0f), 0.11f, Text);
        go.AddComponent<TrashBin>();
    }

    static void BuildComputer(Transform w, Vector3 position)
    {
        var go = Util.Node("Computer", w, position);
        Util.AddBox(go, new Vector3(0f, 0.65f, 0f), new Vector3(1.5f, 1.3f, 0.82f));
        Furniture("desk", go.transform, Vector3.zero, 0f, false);
        ModelLib.Spawn("Furniture/computerScreen", go.transform, new Vector3(0f, 0.8f, -0.18f), FrontYaw, 0.13f);
        ModelLib.Spawn("Furniture/computerKeyboard", go.transform, new Vector3(0f, 0.8f, 0.12f), FrontYaw, 0.13f);
        MeshGen.Spawn("Screen", MeshGen.RoundedBox(new Vector3(0.44f, 0.26f, 0.006f), 0.004f), Util.Glow(new Color(0.25f, 0.55f, 1f), 1.2f), go.transform, new Vector3(0f, 1.0f, -0.13f));
        LocalizedLabel.Attach(Util.Label("", go.transform, new Vector3(0f, 1.0f, -0.125f), FacingKitchenBack, 0.06f, Color.white), () => Loc.T("label.orders"));
        go.AddComponent<ShopComputer>();
    }

    static void BuildOpenSign(Transform w, Vector3 position)
    {
        var go = Util.Node("OpenSign", w, position);
        Util.AddBox(go, new Vector3(0f, 1f, 0f), new Vector3(0.7f, 2f, 0.3f));
        var dark = Util.Mat(DarkSteel);
        MeshGen.Spawn("Base", MeshGen.Puck(0.22f, 0.04f, 0.015f, 24), dark, go.transform, Vector3.zero);
        MeshGen.Spawn("Pole", MeshGen.Puck(0.03f, 1.2f, 0.01f, 16), dark, go.transform, Vector3.zero);
        var panel = MeshGen.Spawn("Panel", MeshGen.RoundedBox(new Vector3(0.76f, 0.42f, 0.06f), 0.06f), Util.Mat(Red), go.transform, new Vector3(0f, 1.4f, 0f));
        var sign = go.AddComponent<OpenSign>();
        sign.panel = panel.GetComponent<Renderer>();
        sign.label = Util.Label("", go.transform, new Vector3(0f, 1.4f, -0.035f), Quaternion.identity, 0.14f, Color.white);
    }

    static void BuildPlayer(Transform root, GameManager gm)
    {
        var go = new GameObject("Player");
        go.transform.SetParent(root, false);

        var cc = go.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.stepOffset = 0.3f;

        var pivot = Util.Node("CameraPivot", go.transform, new Vector3(0f, 1.62f, 0f));
        var cam = pivot.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 150f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
        pivot.AddComponent<AudioListener>();

        var hold = Util.Node("HoldPoint", pivot.transform, new Vector3(0.32f, -0.3f, 0.62f)).transform;

        var controller = go.AddComponent<PlayerController>();
        controller.cameraPivot = pivot.transform;
        var interactor = go.AddComponent<PlayerInteractor>();
        interactor.cam = cam;
        interactor.holdPoint = hold;

        gm.player = controller;
        gm.interactor = interactor;
    }
}
