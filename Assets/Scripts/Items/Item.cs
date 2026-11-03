using System.Collections.Generic;
using UnityEngine;

// Qo'lga olinadigan har qanday narsa: ingredient yoki tayyor mahsulot.
public class Item : Interactable
{
    public static readonly List<Item> All = new List<Item>();

    // Aynigan go'shtning ko'kimtir rangi (tekstura rangiga ko'paytiriladi)
    public static readonly Color SpoiledColor = new Color(0.4f, 0.7f, 0.32f);

    public ItemType type;
    public float cook; // 0..1 — pishmoqda, 1..2 — tayyor, 2 va undan ko'p — kuygan
    public Container container; // tayyor taom tarelkada yoki paketda
    public bool onFire;         // grilda yonmoqda
    public bool extinguished;   // o't o'chirgich bilan o'chirilgan — qayta yonmaydi
    public bool spoiled;        // ochiq muzlatgichda aynigan — faqat axlatga

    readonly List<Material> tintMaterials = new List<Material>();
    Color rawColor, cookedColor, burntColor;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void OnDestroy()
    {
        for (int i = 0; i < tintMaterials.Count; i++)
            if (tintMaterials[i] != null) Destroy(tintMaterials[i]);
    }

    public CookState State
    {
        get
        {
            if (cook < 1f) return CookState.Raw;
            if (cook < 2f) return CookState.Cooked;
            return CookState.Burnt;
        }
    }

    public string DisplayName
    {
        get
        {
            if (container == Container.Plate) return BaseName + " " + Loc.T("item.onPlate");
            if (container == Container.Bag) return BaseName + " " + Loc.T("item.inBag");
            return BaseName;
        }
    }

    string BaseName
    {
        get
        {
            string state = spoiled ? "Spoiled" : State == CookState.Raw ? "Raw" : State == CookState.Cooked ? "Cooked" : "Burnt";
            switch (type)
            {
                case ItemType.Patty: return Loc.T("item.patty" + state);
                case ItemType.Meat: return Loc.T("item.meat" + state);
                case ItemType.Lavash: return Loc.T("item.lavash" + state);
            }
            return GameData.Name(type);
        }
    }

    // Taomni tarelkaga qo'yadi, paketga soladi yoki qaytarib chiqaradi (ko'rinishi ham o'zgaradi)
    public void SetContainer(Container value)
    {
        if (container == value) return;
        container = value;
        ItemFactory.ApplyContainer(this);
    }

    public static Color CookColor(float cook, Color raw, Color cooked, Color burnt)
    {
        if (cook < 1f) return Color.Lerp(raw, cooked, cook);
        if (cook < 1.6f) return cooked;
        return Color.Lerp(cooked, burnt, (cook - 1.6f) / 0.4f);
    }

    // materials — shu narsaga tegishli (nusxalangan) materiallar; ranglar ularga ko'paytiruvchi sifatida beriladi
    public void SetupTint(List<Material> materials, Color raw, Color cooked, Color burnt)
    {
        rawColor = raw;
        cookedColor = cooked;
        burntColor = burnt;
        tintMaterials.AddRange(materials);
        UpdateTint();
    }

    public void SetSpoiled()
    {
        spoiled = true;
        UpdateTint();
    }

    public void SetCook(float value)
    {
        cook = value;
        UpdateTint();
    }

    public void AddCookTime(float seconds)
    {
        if (!GameData.IsCookable(type) || seconds <= 0f) return;
        float cookSeconds = GameData.CookSeconds(type);
        if (cook < 1f)
        {
            float need = (1f - cook) * cookSeconds;
            if (seconds < need)
            {
                cook += seconds / cookSeconds;
                seconds = 0f;
            }
            else
            {
                cook = 1f;
                seconds -= need;
            }
        }
        // Kuygandan keyin ham ortib boradi — grilda GameData.FireAt ga yetsa yonib ketadi
        if (seconds > 0f) cook = Mathf.Min(3f, cook + seconds / GameData.BurnSeconds(type));
        UpdateTint();
    }

    void UpdateTint()
    {
        if (tintMaterials.Count == 0) return;
        Color c = spoiled ? SpoiledColor : CookColor(cook, rawColor, cookedColor, burntColor);
        for (int i = 0; i < tintMaterials.Count; i++) tintMaterials[i].color = c;
    }

    // free = yerda/stolda erkin yotibdi (collider yoqiq). Qo'lda yoki stansiyada — o'chiq.
    public void SetFree(bool free)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = free;
        if (free && GameManager.I != null && GameManager.I.player != null) GameManager.I.player.IgnoreCollisionsWith(gameObject);
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        return player.Held == null ? Loc.F("act.take", DisplayName) : null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        return player.Held != null ? DisplayName : null;
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        if (player.Held == null) player.PickUp(this);
        else GameManager.I.Toast(Loc.T("msg.handsFull"), true);
    }
}
