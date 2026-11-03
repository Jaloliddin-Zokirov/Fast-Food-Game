using System.Collections.Generic;
using UnityEngine;

// Yig'ish taxtasi. Asos (burger noni yoki lavash noni) qo'yiladi, keyin ichidagilar istalgan tartibda.
// Burger — ustiga yana non qo'yilganda yopiladi. Lavash — hammasi qo'shilgach, bo'sh qo'l bilan o'raladi.
public class AssemblyBoard : Interactable
{
    public Transform stackRoot;

    readonly List<ItemType> layers = new List<ItemType>();
    readonly List<float> cooks = new List<float>();
    readonly List<GameObject> visuals = new List<GameObject>();
    float height;

    bool IsBurger
    {
        get { return layers.Count > 0 && layers[0] == ItemType.Bun; }
    }

    bool LavashReady
    {
        get { return layers.Count > 0 && layers[0] == ItemType.LavashBread && Missing().Count == 0; }
    }

    bool HasFilling(ItemType type)
    {
        for (int i = 1; i < layers.Count; i++)
            if (layers[i] == type) return true;
        return false;
    }

    List<ItemType> Missing()
    {
        var missing = new List<ItemType>();
        if (layers.Count == 0) return missing;
        var fillings = GameData.FillingsFor(layers[0]);
        for (int i = 0; i < fillings.Length; i++)
            if (!HasFilling(fillings[i])) missing.Add(fillings[i]);
        return missing;
    }

    static string NameList(List<ItemType> types)
    {
        var names = new string[types.Count];
        for (int i = 0; i < types.Count; i++) names[i] = GameData.Name(types[i]);
        return string.Join(", ", names);
    }

    // null — qo'shish mumkin, aks holda nima uchun mumkin emasligi.
    string CheckAdd(Item item, out bool completes)
    {
        completes = false;
        if (GameData.IsProduct(item.type)) return Loc.T("board.giveProduct");
        if (item.spoiled) return Loc.T("spoiled.reject");
        if (layers.Count == 0)
        {
            if (item.type == ItemType.Bun || item.type == ItemType.LavashBread) return null;
            return Loc.T("board.needBase");
        }
        if (IsBurger && item.type == ItemType.Bun)
        {
            var missing = Missing();
            if (missing.Count > 0) return Loc.F("board.addBeforeClosing", NameList(missing));
            completes = true;
            return null;
        }
        if (System.Array.IndexOf(GameData.FillingsFor(layers[0]), item.type) < 0)
            return Loc.F(IsBurger ? "board.notForBurger" : "board.notForLavash", GameData.Name(item.type).ToLower());
        if (HasFilling(item.type)) return Loc.F("board.alreadyAdded", GameData.Name(item.type));
        if (GameData.IsCookable(item.type))
        {
            if (item.State == CookState.Raw) return Loc.T("board.raw");
            if (item.State == CookState.Burnt) return Loc.T("board.burnt");
        }
        return null;
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held != null)
        {
            bool completes;
            if (CheckAdd(held, out completes) != null) return null;
            return completes ? Loc.T("act.closeBurger") : Loc.F("act.add", held.DisplayName);
        }
        if (LavashReady) return Loc.T("act.rollLavash");
        if (layers.Count > 0) return Loc.F("act.takeBack", GameData.Name(layers[layers.Count - 1]));
        return null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held != null)
        {
            bool completes;
            string error = CheckAdd(held, out completes);
            if (error != null) return error;
        }
        if (layers.Count == 0) return Loc.T("board.empty");

        var missing = Missing();
        if (missing.Count == 0) return Loc.T(IsBurger ? "board.burgerReady" : "board.lavashReady");
        return Loc.F(IsBurger ? "board.burgerNeeds" : "board.lavashNeeds", NameList(missing));
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held != null)
        {
            bool completes;
            string error = CheckAdd(held, out completes);
            if (error != null)
            {
                GameManager.I.Toast(error, true);
                return;
            }

            var item = player.Release();
            AddLayer(item.type, item.cook);
            Destroy(item.gameObject);

            if (completes)
            {
                ClearAll();
                player.PickUp(ItemFactory.Create(ItemType.Burger));
                SoundManager.Play(Sfx.Ready);
                Effects.Sparkle(stackRoot.position, new Color(1f, 0.85f, 0.3f));
            }
            else
            {
                SoundManager.Play(Sfx.Place);
            }
            return;
        }

        if (LavashReady)
        {
            ClearAll();
            player.PickUp(ItemFactory.Create(ItemType.Lavash));
            return;
        }

        if (layers.Count > 0)
        {
            int last = layers.Count - 1;
            ItemType type = layers[last];
            float cook = cooks[last];
            Destroy(visuals[last]);
            layers.RemoveAt(last);
            cooks.RemoveAt(last);
            visuals.RemoveAt(last);
            height -= ItemFactory.LayerHeight(type, false);
            player.PickUp(ItemFactory.Create(type, cook));
        }
    }

    void AddLayer(ItemType type, float cook)
    {
        bool asTop = type == ItemType.Bun && layers.Count > 0;
        var visual = ItemFactory.BuildLayer(type, stackRoot, asTop, cook);
        visual.transform.localPosition = new Vector3(0f, height, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
        height += ItemFactory.LayerHeight(type, asTop);
        layers.Add(type);
        cooks.Add(cook);
        visuals.Add(visual);
    }

    void ClearLists()
    {
        layers.Clear();
        cooks.Clear();
        visuals.Clear();
        height = 0f;
    }

    public void ClearAll()
    {
        for (int i = 0; i < visuals.Count; i++)
            if (visuals[i] != null) Destroy(visuals[i]);
        ClearLists();
    }
}
