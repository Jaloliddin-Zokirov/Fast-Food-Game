using UnityEngine;

// Tablo: do'konni ochadi, tanaffusga yopadi va ish vaqti tugagach yopib kunni yakunlaydi; holatini rang bilan ko'rsatadi.
public class OpenSign : Interactable
{
    public Renderer panel;
    public TextMesh label;

    GamePhase shown = (GamePhase)(-1);

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
        shown = (GamePhase)(-1);
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.Phase == shown) return;

        shown = gm.Phase;
        bool open = shown == GamePhase.Open;
        bool prep = shown == GamePhase.Prep;
        Color color = open ? new Color(0.2f, 0.75f, 0.3f) : prep ? new Color(1f, 0.6f, 0.1f) : new Color(0.8f, 0.15f, 0.15f);
        if (panel != null) panel.sharedMaterial = Util.Mat(color);
        if (label != null) label.text = Loc.T(open ? "sign.open" : prep ? "sign.prep" : "sign.closed");
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        switch (GameManager.I.Phase)
        {
            case GamePhase.Prep:
            case GamePhase.Break: return Loc.T("act.openShop");
            case GamePhase.Open: return Loc.T("act.breakShop");
            case GamePhase.AfterHours: return Loc.T("act.endDay");
        }
        return null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        var gm = GameManager.I;
        switch (gm.Phase)
        {
            case GamePhase.Prep: return Loc.T("sign.infoPrep");
            case GamePhase.Open: return Loc.F("sign.infoOpen", gm.ClockText);
            case GamePhase.Break: return Loc.F("sign.infoBreak", gm.ClockText);
            case GamePhase.AfterHours: return Loc.T(gm.customers.InsideCount > 0 ? "sign.infoAfterBusy" : "sign.infoAfter");
        }
        return null;
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        GameManager.I.UseSign();
    }
}
