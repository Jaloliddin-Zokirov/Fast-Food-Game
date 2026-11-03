using System.Collections.Generic;
using UnityEngine;

// Tarelkalar to'plami yoki qog'oz paketlar qutisi. Tayyor taomni shu yerda tarelkaga qo'yish
// (shu yerda yeydigan mijoz uchun) yoki paketga solish (olib ketadigan mijoz uchun) mumkin.
public class ContainerStation : Interactable
{
    public Container kind;

    readonly List<GameObject> stack = new List<GameObject>();
    int shown = -1;

    public void AddStackVisual(GameObject go)
    {
        stack.Add(go);
    }

    int Available
    {
        get
        {
            var gm = GameManager.I;
            if (gm == null || gm.Data == null) return 0;
            return kind == Container.Plate ? gm.CleanPlates : gm.Data.stock[(int)ItemType.PaperBag];
        }
    }

    void Update()
    {
        int count = Available;
        if (count == shown) return;
        shown = count;
        // Tarelkalar bittadan ko'rinadi, paketlar esa bor-yo'qligiga qarab
        for (int i = 0; i < stack.Count; i++) stack[i].SetActive(kind == Container.Plate ? i < count : count > 0);
    }

    bool CanPack(Item held)
    {
        if (!GameData.NeedsContainer(held.type) || held.container != Container.None) return false;
        return !GameData.IsCookable(held.type) || held.State == CookState.Cooked;
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held == null) return null;
        if (held.container == kind) return Loc.T(kind == Container.Plate ? "plate.remove" : "bag.remove");
        if (!CanPack(held)) return null;
        return Loc.F(kind == Container.Plate ? "plate.put" : "bag.put", held.DisplayName);
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held != null && held.type == ItemType.DirtyPlate) return Loc.T("plate.washFirst");
        return Loc.F(kind == Container.Plate ? "plate.info" : "bag.info", Available);
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        var gm = GameManager.I;
        if (held == null) return;

        // Qaytarib chiqarish: tarelka to'plamga qaytadi, paket esa yirtiladi
        if (held.container == kind)
        {
            if (kind == Container.Plate) gm.CleanPlates++;
            held.SetContainer(Container.None);
            SoundManager.Play(kind == Container.Plate ? Sfx.Plate : Sfx.Bag);
            return;
        }

        if (held.type == ItemType.DirtyPlate)
        {
            gm.Toast(Loc.T("plate.washFirst"), true);
            return;
        }
        if (!GameData.NeedsContainer(held.type))
        {
            gm.Toast(GameData.IsProduct(held.type) ? Loc.F("container.notNeeded", held.DisplayName) : Loc.T("container.onlyFood"), true);
            return;
        }
        if (held.container != Container.None)
        {
            gm.Toast(Loc.T("container.already"), true);
            return;
        }
        if (GameData.IsCookable(held.type) && held.State == CookState.Raw)
        {
            gm.Toast(Loc.T("customer.notPressed"), true);
            return;
        }
        if (GameData.IsCookable(held.type) && held.State == CookState.Burnt)
        {
            gm.Toast(Loc.T("board.burnt"), true);
            return;
        }
        if (Available <= 0)
        {
            gm.Toast(Loc.T(kind == Container.Plate ? "plate.none" : "bag.none"), true);
            return;
        }

        if (kind == Container.Plate) gm.CleanPlates--;
        else gm.Data.stock[(int)ItemType.PaperBag]--;
        held.SetContainer(kind);
        SoundManager.Play(kind == Container.Plate ? Sfx.Plate : Sfx.Bag);
    }
}
