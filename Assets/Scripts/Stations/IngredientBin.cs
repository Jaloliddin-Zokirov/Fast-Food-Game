using System.Collections.Generic;
using UnityEngine;

// Ingredient qutisi: har olganda zaxiradan 1 ta kamayadi. Muzlatgichdagi qutilarda aynigan mahsulot
// bo'lsa, avval o'sha chiqadi — uni axlatga tashlash kerak.
public class IngredientBin : Interactable
{
    public ItemType type;
    public GameObject samples;
    public TextMesh label;

    int shownStock = -1;
    int shownSpoiled = -1;
    List<Material>[] sampleMaterials;
    List<Color>[] sampleColors;

    void OnEnable()
    {
        Loc.Changed += OnLanguageChanged;
    }

    void OnDisable()
    {
        Loc.Changed -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        shownStock = -1;
    }

    void Update()
    {
        if (GameManager.I == null || GameManager.I.Data == null) return;
        int stock = GameManager.I.Data.stock[(int)type];
        int spoiled = Spoiled;
        if (stock == shownStock && spoiled == shownSpoiled) return;

        shownStock = stock;
        shownSpoiled = spoiled;
        if (samples != null) samples.SetActive(stock + spoiled > 0);
        TintSamples(stock, spoiled);
        if (label != null)
        {
            string text = GameData.Name(type) + "\n" + Loc.F("bin.count", stock);
            if (spoiled > 0) text += "\n<color=#cc2222>" + Loc.F("bin.spoiled", spoiled) + "</color>";
            label.text = text;
            label.color = stock > 0 ? new Color(0.15f, 0.12f, 0.1f) : new Color(0.85f, 0.1f, 0.1f);
        }
    }

    int Spoiled
    {
        get { return GameManager.I.Data.spoiled[(int)type]; }
    }

    // Muzlatgich javonidagi namunalarning bir qismi aynigan ulushiga qarab ko'kimtir rangga bo'yaladi.
    // Bu namunalarning materiallari o'ziga xos (ItemFactory nusxalagan), shuning uchun boshqa narsalarga ta'sir qilmaydi.
    void TintSamples(int fresh, int spoiled)
    {
        if (samples == null || !GameData.CanSpoil(type)) return;
        var root = samples.transform;
        if (sampleMaterials == null)
        {
            sampleMaterials = new List<Material>[root.childCount];
            sampleColors = new List<Color>[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                sampleMaterials[i] = new List<Material>();
                sampleColors[i] = new List<Color>();
                foreach (var r in root.GetChild(i).GetComponentsInChildren<Renderer>(true))
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) continue;
                        sampleMaterials[i].Add(m);
                        sampleColors[i].Add(m.color);
                    }
            }
        }

        int total = fresh + spoiled;
        int bad = total > 0 ? Mathf.CeilToInt(sampleMaterials.Length * spoiled / (float)total) : 0;
        for (int i = 0; i < sampleMaterials.Length; i++)
            for (int j = 0; j < sampleMaterials[i].Count; j++)
                sampleMaterials[i][j].color = i < bad ? Item.SpoiledColor : sampleColors[i][j];
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        if (player.Held == null) return Loc.F("act.take", Spoiled > 0 ? Loc.T("item." + type.ToString().ToLower() + "Spoiled") : GameData.Name(type));
        if (CanPutBack(player.Held)) return Loc.T("act.putBack");
        return null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        int stock = GameManager.I.Data.stock[(int)type];
        if (Spoiled > 0) return Loc.F("bin.inStockSpoiled", stock, Spoiled);
        if (stock <= 0) return Loc.F("bin.empty", GameData.Name(type));
        return Loc.F("bin.inStock", stock);
    }

    bool CanPutBack(Item held)
    {
        return held.type == type && held.cook <= 0f && !held.spoiled;
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var data = GameManager.I.Data;
        int index = (int)type;
        if (player.Held == null)
        {
            if (data.spoiled[index] > 0)
            {
                data.spoiled[index]--;
                var bad = ItemFactory.Create(type);
                bad.SetSpoiled();
                player.PickUp(bad);
                return;
            }
            if (data.stock[index] <= 0)
            {
                GameManager.I.Toast(Loc.F("bin.ranOut", GameData.Name(type)), true);
                return;
            }
            data.stock[index]--;
            player.PickUp(ItemFactory.Create(type));
        }
        else if (player.Held.spoiled && player.Held.type == type)
        {
            GameManager.I.Toast(Loc.T("fridge.noSpoiledBack"), true);
        }
        else if (CanPutBack(player.Held))
        {
            data.stock[index]++;
            player.DestroyHeld();
            SoundManager.Play(Sfx.Place);
        }
        else
        {
            GameManager.I.Toast(Loc.T("msg.handsFull"), true);
        }
    }
}
