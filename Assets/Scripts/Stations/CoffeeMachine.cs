using System.Collections.Generic;
using UnityEngine;

// Qahva mashinasi: bitta porsiya qahva donidan bitta stakan qahva tayyorlaydi.
public class CoffeeMachine : Interactable
{
    public static readonly List<CoffeeMachine> All = new List<CoffeeMachine>();

    public Transform cupPoint;
    public MeshRenderer idleMug; // mashinaning o'z bo'sh krujkasi — stakan turganda yashiriladi

    Item cup;
    bool brewing;
    float progress;
    ParticleSystem steam;

    // 0..1 — tayyorlanmoqda, 1 — tayyor, -1 — bo'sh (UI dagi chiziq uchun)
    public float Progress
    {
        get
        {
            if (cup == null) return -1f;
            return brewing ? progress : 1f;
        }
    }

    public Vector3 IndicatorPoint
    {
        get { return cupPoint.position + Vector3.up * 0.3f; }
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        steam = Effects.Steam(cupPoint, Vector3.up * 0.12f);
    }

    int Beans
    {
        get { return GameManager.I.Data.stock[(int)ItemType.CoffeeBeans]; }
    }

    void Update()
    {
        if (idleMug != null && idleMug.enabled != (cup == null)) idleMug.enabled = cup == null;
        if (steam != null)
        {
            var emission = steam.emission;
            emission.rateOverTime = brewing ? 8f : 0f;
        }
        if (!brewing || cup == null) return;

        progress += Time.deltaTime / GameData.BrewSeconds;
        if (progress < 1f) return;
        brewing = false;
        SoundManager.Play(Sfx.Ready, 0.7f);
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        if (player.Held != null) return null;
        if (cup != null && !brewing) return Loc.F("act.take", cup.DisplayName);
        if (cup == null) return Loc.T("coffee.brew");
        return null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        if (brewing) return Loc.T("coffee.brewing");
        if (cup == null && Beans <= 0) return Loc.T("coffee.noBeans");
        return Loc.F("coffee.info", Beans);
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var gm = GameManager.I;
        if (player.Held != null)
        {
            gm.Toast(Loc.T("msg.handsFull"), true);
            return;
        }
        if (cup != null)
        {
            if (brewing) return;
            var ready = cup;
            cup = null;
            player.PickUp(ready);
            return;
        }
        if (Beans <= 0)
        {
            gm.Toast(Loc.T("coffee.noBeans"), true);
            return;
        }

        gm.Data.stock[(int)ItemType.CoffeeBeans]--;
        cup = ItemFactory.Create(ItemType.Coffee);
        cup.SetFree(false);
        cup.transform.SetParent(cupPoint, false);
        cup.transform.localPosition = Vector3.zero;
        cup.transform.localRotation = Quaternion.identity;
        brewing = true;
        progress = 0f;
        SoundManager.Play(Sfx.Brew);
    }

    public void ClearAll()
    {
        if (cup != null) Destroy(cup.gameObject);
        cup = null;
        brewing = false;
    }
}
