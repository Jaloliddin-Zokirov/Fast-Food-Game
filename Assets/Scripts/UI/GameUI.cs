using System.Collections.Generic;
using UnityEngine;

// Butun interfeys IMGUI (OnGUI) orqali chiziladi — sahnada Canvas yoki shrift sozlash shart emas.
// Hamma o'lchamlar 1080p bo'yicha yozilgan va ekranga moslab kattalashtiriladi.
public partial class GameUI : MonoBehaviour
{
    public static GameUI I;

    const float RefHeight = 1080f;

    class ToastMessage
    {
        public string text;
        public bool error;
        public float time;
    }

    class FloatingText
    {
        public Vector3 position;
        public string text;
        public Color color;
        public float time;
    }

    static readonly Color Panel = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color Accent = new Color(1f, 0.8f, 0.25f);
    static readonly Color Good = new Color(0.35f, 0.9f, 0.4f);
    static readonly Color Bad = new Color(1f, 0.35f, 0.3f);
    static readonly Color Muted = new Color(0.8f, 0.8f, 0.8f);

    readonly List<ToastMessage> toasts = new List<ToastMessage>();
    readonly List<FloatingText> floats = new List<FloatingText>();

    float scale = 1f;
    float width = 1920f;
    bool stylesReady;
    GUIStyle text, textCenter, small, bold, title, heading, button, buttonOff, panelStyle;

    void Awake()
    {
        I = this;
    }

    public void Toast(string message, bool error)
    {
        var toast = new ToastMessage();
        toast.text = message;
        toast.error = error;
        toast.time = Time.unscaledTime;
        toasts.Add(toast);
        if (toasts.Count > 4) toasts.RemoveAt(0);
    }

    public void FloatText(Vector3 position, string message, Color color)
    {
        var f = new FloatingText();
        f.position = position;
        f.text = message;
        f.color = color;
        f.time = Time.time;
        floats.Add(f);
    }

    void Update()
    {
        toasts.RemoveAll(t => Time.unscaledTime - t.time > 3f);
        floats.RemoveAll(f => Time.time - f.time > 1.8f);
    }

    static GUIStyle MakeStyle(GUIStyle source, int size, TextAnchor anchor, Color color, FontStyle fontStyle)
    {
        var style = new GUIStyle(source);
        style.fontSize = size;
        style.alignment = anchor;
        style.normal.textColor = color;
        style.fontStyle = fontStyle;
        style.wordWrap = true;
        style.richText = true;
        return style;
    }

    void SetupStyles()
    {
        text = MakeStyle(GUI.skin.label, 22, TextAnchor.UpperLeft, Color.white, FontStyle.Normal);
        textCenter = MakeStyle(GUI.skin.label, 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Normal);
        small = MakeStyle(GUI.skin.label, 18, TextAnchor.MiddleCenter, Muted, FontStyle.Normal);
        bold = MakeStyle(GUI.skin.label, 24, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        title = MakeStyle(GUI.skin.label, 72, TextAnchor.MiddleCenter, Accent, FontStyle.Bold);
        heading = MakeStyle(GUI.skin.label, 34, TextAnchor.MiddleCenter, Accent, FontStyle.Bold);
        // CalcSize so'z ko'chirishni hisobga olmaydi, shuning uchun bir qatorli yozuvlarda o'chiramiz
        textCenter.wordWrap = false;
        small.wordWrap = false;

        button = MakeStyle(GUI.skin.button, 24, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        button.normal.background = TextureGen.RoundedGradient(12, new Color(1f, 0.6f, 0.2f), new Color(0.86f, 0.36f, 0.1f));
        button.hover.background = TextureGen.RoundedGradient(12, new Color(1f, 0.72f, 0.32f), new Color(0.95f, 0.46f, 0.16f));
        button.active.background = TextureGen.RoundedGradient(12, new Color(0.82f, 0.4f, 0.12f), new Color(0.7f, 0.3f, 0.06f));
        button.hover.textColor = Color.white;
        button.active.textColor = Color.white;
        button.border = new RectOffset(12, 12, 12, 12);

        buttonOff = MakeStyle(GUI.skin.button, 22, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.85f), FontStyle.Bold);
        buttonOff.normal.background = TextureGen.RoundedGradient(12, new Color(0.34f, 0.34f, 0.38f), new Color(0.22f, 0.22f, 0.25f));
        buttonOff.hover.background = TextureGen.RoundedGradient(12, new Color(0.44f, 0.44f, 0.48f), new Color(0.3f, 0.3f, 0.34f));
        buttonOff.active.background = buttonOff.normal.background;
        buttonOff.hover.textColor = Color.white;
        buttonOff.active.textColor = Color.white;
        buttonOff.border = new RectOffset(12, 12, 12, 12);

        // Burchaklari yumaloq panel (9-slice): rangi GUI.backgroundColor orqali beriladi
        panelStyle = new GUIStyle();
        panelStyle.normal.background = TextureGen.RoundedRect(14);
        panelStyle.border = new RectOffset(14, 14, 14, 14);
    }

    void OnGUI()
    {
        if (!stylesReady)
        {
            SetupStyles();
            stylesReady = true;
        }
        scale = Screen.height / RefHeight;
        width = Screen.width / scale;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        var gm = GameManager.I;
        if (gm == null) return;

        if (gm.InGame)
        {
            DrawWorldOverlays(gm);
            DrawHud(gm);
            if (gm.HelpOpen) DrawHelp();
        }
        DrawToasts();

        switch (gm.Phase)
        {
            case GamePhase.Menu: DrawMainMenu(gm); break;
            case GamePhase.Summary: DrawSummary(gm); break;
            case GamePhase.GameOver: DrawGameOver(gm); break;
        }
        if (gm.InGame && gm.ShopOpen) DrawShop(gm);
        if (gm.InGame && gm.Paused) DrawPause(gm);
    }

    // ---------- Yordamchi chizish funksiyalari ----------

    static void Fill(Rect r, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void RoundPanel(Rect r, Color color)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUI.Box(r, GUIContent.none, panelStyle);
        GUI.backgroundColor = old;
    }

    void LabelWithShadow(Rect r, string message, GUIStyle style, Color color)
    {
        Color old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), message, style);
        style.normal.textColor = color;
        GUI.Label(r, message, style);
        style.normal.textColor = old;
    }

    // Matn o'lchamiga mos fon bilan markazlashgan yozuv
    void CenteredBox(float centerX, float y, string message, GUIStyle style, Color background)
    {
        Vector2 size = style.CalcSize(new GUIContent(message));
        var r = new Rect(centerX - size.x / 2f - 16f, y, size.x + 32f, size.y + 12f);
        RoundPanel(r, background);
        GUI.Label(r, message, style);
    }

    bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
    {
        gui = Vector2.zero;
        if (cam == null) return false;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0.1f) return false;
        gui = new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
        return true;
    }

    // ---------- O'yin ichidagi elementlar ----------

    void DrawWorldOverlays(GameManager gm)
    {
        Camera cam = gm.interactor.cam;
        Vector2 p;

        for (int s = 0; s < CookStation.All.Count; s++)
        {
            var station = CookStation.All[s];
            for (int i = 0; i < station.ActiveSlots; i++)
            {
                var item = station.ItemAt(i);
                if (item == null) continue;
                Vector3 world = station.SlotAt(i).position + Vector3.up * 0.32f;
                if (Vector3.Distance(cam.transform.position, world) > 8f || !Visible(cam, world, station.transform) || !WorldToGui(cam, world, out p)) continue;
                DrawCookBar(p, item.cook, item.onFire);
            }
        }

        for (int s = 0; s < Sink.All.Count; s++)
        {
            var sink = Sink.All[s];
            if (sink.Progress < 0f) continue;
            Vector3 world = sink.plateSpot.position + Vector3.up * 0.45f;
            if (Vector3.Distance(cam.transform.position, world) > 8f || !Visible(cam, world, sink.transform) || !WorldToGui(cam, world, out p)) continue;
            DrawProgress(p, sink.Progress, new Color(0.35f, 0.7f, 1f), Loc.T("hud.washing"));
        }

        for (int m = 0; m < CoffeeMachine.All.Count; m++)
        {
            var machine = CoffeeMachine.All[m];
            float progress = machine.Progress;
            Vector3 world = machine.IndicatorPoint;
            if (progress < 0f || Vector3.Distance(cam.transform.position, world) > 8f || !Visible(cam, world, machine.transform) || !WorldToGui(cam, world, out p)) continue;
            if (progress >= 1f) LabelWithShadow(new Rect(p.x - 90f, p.y - 20f, 180f, 30f), Loc.T("hud.ready"), bold, Good);
            else DrawProgress(p, progress, new Color(0.8f, 0.55f, 0.3f), null);
        }

        for (int c = 0; c < Customer.All.Count; c++)
        {
            var customer = Customer.All[c];
            if (customer.state == CustomerState.Leaving || customer.order.Count == 0) continue;
            if (!Visible(cam, customer.HeadPosition, customer.transform) || !WorldToGui(cam, customer.HeadPosition, out p)) continue;
            DrawOrderBubble(p, customer);
        }

        for (int i = 0; i < floats.Count; i++)
        {
            var f = floats[i];
            float age = Time.time - f.time;
            if (!WorldToGui(cam, f.position + Vector3.up * age * 0.5f, out p)) continue;
            Color color = f.color;
            color.a = Mathf.Clamp01(1.5f - age);
            LabelWithShadow(new Rect(p.x - 200f, p.y - 40f, 400f, 80f), f.text, bold, color);
        }
    }

    // Kamera va nuqta orasida boshqa narsa (devor, peshtaxta) to'sib turmaganini tekshiradi
    static bool Visible(Camera cam, Vector3 world, Transform owner)
    {
        RaycastHit hit;
        if (!Physics.Linecast(cam.transform.position, world, out hit, ~0, QueryTriggerInteraction.Ignore)) return true;
        return hit.collider.transform.IsChildOf(owner) || owner.IsChildOf(hit.collider.transform);
    }

    void DrawProgress(Vector2 p, float fraction, Color color, string label)
    {
        var bg = new Rect(p.x - 42f, p.y - 8f, 84f, 16f);
        Fill(bg, new Color(0f, 0f, 0f, 0.75f));
        Fill(new Rect(bg.x + 2f, bg.y + 2f, (bg.width - 4f) * Mathf.Clamp01(fraction), bg.height - 4f), color);
        if (label != null) LabelWithShadow(new Rect(p.x - 100f, p.y - 34f, 200f, 26f), label, bold, Color.white);
    }

    void DrawCookBar(Vector2 p, float cook, bool onFire)
    {
        var bg = new Rect(p.x - 42f, p.y - 8f, 84f, 16f);
        var inner = new Rect(bg.x + 2f, bg.y + 2f, bg.width - 4f, bg.height - 4f);
        Fill(bg, new Color(0f, 0f, 0f, 0.75f));

        if (onFire)
        {
            bool flash = Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f;
            Fill(inner, flash ? new Color(1f, 0.55f, 0.1f) : new Color(0.8f, 0.15f, 0.05f));
            LabelWithShadow(new Rect(p.x - 100f, p.y - 34f, 200f, 26f), Loc.T("hud.fireTag"), bold, flash ? Accent : Bad);
        }
        else if (cook < 1f)
        {
            Fill(new Rect(inner.x, inner.y, inner.width * cook, inner.height), new Color(1f, 0.65f, 0.2f));
        }
        else if (cook < 2f)
        {
            Fill(inner, Good);
            Fill(new Rect(inner.x, inner.y, inner.width * (cook - 1f), inner.height), new Color(0.9f, 0.2f, 0.1f, 0.85f));
            LabelWithShadow(new Rect(p.x - 60f, p.y - 34f, 120f, 26f), Loc.T("hud.ready"), bold, Good);
        }
        else
        {
            Fill(inner, new Color(0.3f, 0.05f, 0.05f));
            LabelWithShadow(new Rect(p.x - 60f, p.y - 34f, 120f, 26f), Loc.T("hud.burnt"), bold, Bad);
        }
    }

    void DrawOrderBubble(Vector2 p, Customer customer)
    {
        float w = 184f;
        float row = 46f;
        float header = 24f;
        bool waiting = customer.state == CustomerState.Waiting;
        float h = 12f + header + row * customer.order.Count + (waiting ? 16f : 0f);
        var r = new Rect(p.x - w / 2f, p.y - h, w, h);
        RoundPanel(r, new Color(0.07f, 0.07f, 0.09f, 0.85f));

        // Shu yerda yeydigan — tarelkada, olib ketadigan — paketda
        bool dineIn = customer.orderType == OrderType.DineIn;
        small.normal.textColor = dineIn ? new Color(0.55f, 0.85f, 1f) : Accent;
        small.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(r.x, r.y + 5f, w, header), Loc.T(dineIn ? "hud.eatIn" : "hud.toGo"), small);
        small.fontStyle = FontStyle.Normal;
        small.normal.textColor = Muted;

        bold.alignment = TextAnchor.MiddleLeft;
        for (int i = 0; i < customer.order.Count; i++)
        {
            ItemType type = customer.order[i];
            float y = r.y + 6f + header + i * row;
            var icon = IconRenderer.Get(type);
            if (icon != null) GUI.DrawTexture(new Rect(r.x + 8f, y, 44f, 44f), icon);
            bold.normal.textColor = type == ItemType.Burger ? new Color(1f, 0.72f, 0.32f) : type == ItemType.Coffee ? new Color(0.9f, 0.7f, 0.5f) : new Color(1f, 0.93f, 0.62f);
            GUI.Label(new Rect(r.x + 58f, y, w - 62f, row), GameData.Name(type).ToUpper(), bold);
        }
        bold.alignment = TextAnchor.MiddleCenter;
        bold.normal.textColor = Color.white;

        if (waiting)
        {
            float frac = customer.PatienceFraction;
            var bar = new Rect(r.x + 12f, r.yMax - 15f, w - 24f, 7f);
            Fill(bar, new Color(0.25f, 0.25f, 0.25f));
            Fill(new Rect(bar.x, bar.y, bar.width * frac, bar.height), Color.Lerp(Bad, Good, frac));
        }
    }

    void DrawHud(GameManager gm)
    {
        string clock = gm.Phase == GamePhase.Prep ? "09:00" : gm.ClockText;
        RoundPanel(new Rect(16f, 16f, 380f, 122f), Panel);
        text.fontSize = 28;
        GUI.Label(new Rect(32f, 22f, 360f, 36f), Loc.F("hud.day", gm.Data.day, clock), text);
        text.normal.textColor = Accent;
        GUI.Label(new Rect(32f, 58f, 360f, 36f), GameData.Money(gm.Data.money), text);
        text.normal.textColor = Color.white;
        text.fontSize = 22;
        string phase = Loc.T("hud.phase." + gm.Phase);
        small.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(32f, 98f, 360f, 30f), phase + "   " + Loc.T("hud.helpHint"), small);
        small.alignment = TextAnchor.MiddleCenter;

        float stockW = 310f;
        float stockX = width - stockW - 16f;
        RoundPanel(new Rect(stockX, 16f, stockW, 50f + (GameData.StockCount + 1) * 30f), Panel);
        text.normal.textColor = Accent;
        GUI.Label(new Rect(stockX + 14f, 22f, 280f, 30f), Loc.T("hud.stock"), text);
        for (int i = 0; i < GameData.StockCount; i++)
        {
            int count = gm.Data.stock[i];
            Color color = count == 0 ? Bad : count < 3 ? Accent : Color.white;
            string name = GameData.Name((ItemType)i);
            // Muzlatgichda aynigan mahsulot bo'lsa, nomi yonida qizil bilan
            if (gm.Data.spoiled[i] > 0) name += " <color=#ff6655>(" + Loc.F("hud.spoiledCount", gm.Data.spoiled[i]) + ")</color>";
            StockRow(stockX, 54f + i * 30f, IconRenderer.Get((ItemType)i), name, count.ToString(), color);
        }
        // Tarelkalar sotib olinmaydi — yuvib qayta ishlatiladi
        int clean = gm.CleanPlates;
        StockRow(stockX, 54f + GameData.StockCount * 30f, IconRenderer.Plate, Loc.T("hud.plates"), clean + "/" + GameData.PlateCount, clean == 0 ? Bad : clean < 2 ? Accent : Color.white);
        text.normal.textColor = Color.white;

        // Ogohlantirishlar: yong'in va ochiq qolgan muzlatgich (bir-birining ostida)
        float alertY = 74f;
        bool flash = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f;
        if (gm.FireActive)
        {
            CenteredBox(width / 2f, alertY, Loc.T("hud.fire"), textCenter, flash ? new Color(0.85f, 0.1f, 0.05f, 0.92f) : new Color(0.45f, 0.05f, 0.02f, 0.92f));
            alertY += 54f;
        }
        int fridgeWarning = gm.fridge != null ? gm.fridge.Warning : 0;
        if (fridgeWarning == 2)
            CenteredBox(width / 2f, alertY, Loc.T("hud.fridgeSpoiling"), textCenter, flash ? new Color(0.75f, 0.2f, 0.05f, 0.92f) : new Color(0.45f, 0.1f, 0.02f, 0.92f));
        else if (fridgeWarning == 1)
            CenteredBox(width / 2f, alertY, Loc.T("hud.fridgeOpen"), textCenter, new Color(0.15f, 0.35f, 0.6f, 0.9f));

        if (gm.Phase == GamePhase.Prep)
            CenteredBox(width / 2f, 20f, Loc.T("hud.prepBanner"), textCenter, new Color(0.6f, 0.35f, 0.05f, 0.85f));
        else if (gm.Phase == GamePhase.Break)
            CenteredBox(width / 2f, 20f, Loc.T("hud.breakBanner"), textCenter, new Color(0.5f, 0.1f, 0.1f, 0.85f));
        else if (gm.Phase == GamePhase.AfterHours)
            CenteredBox(width / 2f, 20f, Loc.T(gm.customers.InsideCount > 0 ? "hud.afterHoursBusy" : "hud.afterHoursBanner"), textCenter, new Color(0.5f, 0.1f, 0.1f, 0.85f));

        if (gm.Paused || gm.ShopOpen) return;

        var interactor = gm.interactor;
        bool hasAction = interactor.Prompt != null;
        float size = hasAction ? 10f : 6f;
        Fill(new Rect(width / 2f - size / 2f, RefHeight / 2f - size / 2f, size, size), hasAction ? Good : new Color(1f, 1f, 1f, 0.8f));

        float y = RefHeight / 2f + 36f;
        if (hasAction)
        {
            CenteredBox(width / 2f, y, "[E]  " + interactor.Prompt, textCenter, new Color(0f, 0f, 0f, 0.65f));
            y += 46f;
        }
        if (interactor.Info != null) CenteredBox(width / 2f, y, interactor.Info, small, new Color(0f, 0f, 0f, 0.5f));

        if (interactor.Held != null)
            CenteredBox(width / 2f, RefHeight - 64f, Loc.F("hud.holding", interactor.Held.DisplayName), textCenter, Panel);
    }

    void StockRow(float x, float y, Texture2D icon, string name, string count, Color color)
    {
        if (icon != null) GUI.DrawTexture(new Rect(x + 10f, y - 1f, 30f, 30f), icon);
        text.normal.textColor = color;
        GUI.Label(new Rect(x + 46f, y, 200f, 28f), name, text);
        text.alignment = TextAnchor.UpperRight;
        GUI.Label(new Rect(x + 230f, y, 66f, 28f), count, text);
        text.alignment = TextAnchor.UpperLeft;
    }

    void DrawToasts()
    {
        float y = RefHeight - 120f;
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            var t = toasts[i];
            float age = Time.unscaledTime - t.time;
            float alpha = Mathf.Clamp01(3f - age);
            Color bg = t.error ? new Color(0.55f, 0.08f, 0.08f, 0.85f * alpha) : new Color(0f, 0f, 0f, 0.7f * alpha);
            textCenter.normal.textColor = new Color(1f, 1f, 1f, alpha);
            CenteredBox(width / 2f, y, t.text, textCenter, bg);
            textCenter.normal.textColor = Color.white;
            y -= 50f;
        }
    }

    void DrawHelp()
    {
        var r = new Rect(16f, 150f, 600f, 720f);
        RoundPanel(r, new Color(0f, 0f, 0f, 0.72f));
        GUI.Label(new Rect(r.x, r.y + 10f, r.width, 44f), Loc.T("help.title"), heading);
        string help = Loc.T("help.body");
        text.fontSize = 20;
        GUI.Label(new Rect(r.x + 22f, r.y + 64f, r.width - 44f, r.height - 70f), help, text);
        text.fontSize = 22;
    }
}
