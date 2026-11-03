using UnityEngine;

// Menyular: bosh menyu, pauza, do'kon kompyuteri, kun yakuni va bankrotlik oynasi.
public partial class GameUI
{
    // Sozlamalar bloki balandligi (sezgirlik, ovoz, til)
    const float SettingsHeight = 210f;

    bool confirmNewGame;
    int savedDay = -1;

    void Dim(float alpha)
    {
        Fill(new Rect(0f, 0f, width, RefHeight), new Color(0f, 0f, 0f, alpha));
    }

    Rect CenterRect(float w, float h)
    {
        return new Rect((width - w) / 2f, (RefHeight - h) / 2f, w, h);
    }

    bool Button(Rect r, string label)
    {
        return GUI.Button(r, label, button);
    }

    void Settings(float x, float y, float w)
    {
        GUI.Label(new Rect(x, y, w, 30f), Loc.F("settings.sensitivity", PlayerController.Sensitivity.ToString("0.0")), textCenter);
        float sensitivity = GUI.HorizontalSlider(new Rect(x + 40f, y + 36f, w - 80f, 20f), PlayerController.Sensitivity, 0.3f, 4f);
        if (!Mathf.Approximately(sensitivity, PlayerController.Sensitivity))
        {
            PlayerController.Sensitivity = sensitivity;
            PlayerPrefs.SetFloat("sensitivity", sensitivity);
        }

        GUI.Label(new Rect(x, y + 66f, w, 30f), Loc.F("settings.volume", Mathf.RoundToInt(SoundManager.Volume * 100f)), textCenter);
        float volume = GUI.HorizontalSlider(new Rect(x + 40f, y + 102f, w - 80f, 20f), SoundManager.Volume, 0f, 1f);
        if (!Mathf.Approximately(volume, SoundManager.Volume)) SoundManager.SetVolume(volume);

        // Til: har bir tugma o'z tilida yozilgan, tanlangani yorqin rangda
        GUI.Label(new Rect(x, y + 132f, w, 30f), Loc.T("settings.language"), textCenter);
        float half = (w - 70f) / 2f;
        if (GUI.Button(new Rect(x + 30f, y + 166f, half, 42f), "English", Loc.Current == Language.English ? button : buttonOff))
            Loc.Current = Language.English;
        if (GUI.Button(new Rect(x + 40f + half, y + 166f, half, 42f), "O'zbekcha", Loc.Current == Language.Uzbek ? button : buttonOff))
            Loc.Current = Language.Uzbek;
    }

    void DrawMainMenu(GameManager gm)
    {
        Dim(0.15f);
        var panel = new Rect(60f, 90f, 540f, 900f);
        RoundPanel(panel, new Color(0.06f, 0.05f, 0.05f, 0.8f));

        float cx = panel.x;
        float cw = panel.width;
        GUI.Label(new Rect(cx, 130f, cw, 90f), "LAVASH", title);
        GUI.Label(new Rect(cx, 205f, cw, 90f), "& BURGER", title);
        GUI.Label(new Rect(cx, 300f, cw, 44f), "Fast Food Simulator", heading);

        bool hasSave = SaveData.Exists();
        if (hasSave && savedDay < 0) savedDay = SaveData.Load().day;

        float w = 420f;
        float x = cx + (cw - w) / 2f;
        float y = 376f;
        if (hasSave)
        {
            if (Button(new Rect(x, y, w, 62f), Loc.F("menu.continue", savedDay)))
            {
                confirmNewGame = false;
                savedDay = -1;
                gm.ContinueGame();
            }
            y += 76f;
        }

        if (Button(new Rect(x, y, w, 62f), Loc.T(confirmNewGame ? "menu.confirmNew" : "menu.newGame")))
        {
            if (hasSave && !confirmNewGame)
            {
                confirmNewGame = true;
            }
            else
            {
                confirmNewGame = false;
                savedDay = -1;
                gm.NewGame();
            }
        }
        y += 76f;

        if (Button(new Rect(x, y, w, 62f), Loc.T("menu.quit"))) Application.Quit();
        y += 100f;

        RoundPanel(new Rect(x - 20f, y - 12f, w + 40f, SettingsHeight + 10f), new Color(1f, 1f, 1f, 0.06f));
        Settings(x, y, w);

        GUI.Label(new Rect(cx, panel.yMax - 56f, cw, 30f), Loc.T("menu.footer"), small);
    }

    void DrawPause(GameManager gm)
    {
        Dim(0.6f);
        var r = CenterRect(520f, 660f);
        RoundPanel(r, new Color(0.08f, 0.08f, 0.1f, 0.95f));
        GUI.Label(new Rect(r.x, r.y + 20f, r.width, 50f), Loc.T("pause.title"), heading);

        float x = r.x + 50f;
        float w = r.width - 100f;
        if (Button(new Rect(x, r.y + 86f, w, 58f), Loc.T("pause.resume"))) gm.SetPaused(false);

        Settings(x, r.y + 166f, w);

        if (Button(new Rect(x, r.y + 410f, w, 58f), Loc.T("pause.mainMenu"))) gm.QuitToMenu();
        GUI.Label(new Rect(x, r.y + 470f, w, 28f), Loc.T("pause.notSaved"), small);
        if (Button(new Rect(x, r.y + 520f, w, 58f), Loc.T("pause.quit"))) Application.Quit();
    }

    void DrawShop(GameManager gm)
    {
        Dim(0.5f);
        var r = CenterRect(1180f, 940f);
        RoundPanel(r, new Color(0.08f, 0.1f, 0.14f, 0.97f));
        GUI.Label(new Rect(r.x, r.y + 16f, r.width, 50f), Loc.T("shop.title"), heading);
        text.normal.textColor = Accent;
        text.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(r.x, r.y + 20f, r.width - 30f, 40f), GameData.Money(gm.Data.money), text);
        text.alignment = TextAnchor.UpperLeft;
        text.normal.textColor = Color.white;

        float colW = (r.width - 90f) / 2f;
        float leftX = r.x + 30f;
        float rightX = leftX + colW + 30f;
        float top = r.y + 84f;

        // Chapda: oziq-ovqat, ostida jihozlar (paket, qahva doni, o't o'chirgich)
        GUI.Label(new Rect(leftX, top, colW, 34f), Loc.T("shop.ingredients"), bold);
        float rowY = top + 40f;
        for (int i = 0; i < GameData.StockCount; i++)
        {
            if (i == GameData.IngredientCount)
            {
                rowY += 6f;
                GUI.Label(new Rect(leftX, rowY, colW, 34f), Loc.T("shop.supplies"), bold);
                rowY += 40f;
            }
            ShopRow(gm, i, leftX, rowY, colW);
            rowY += 62f;
        }

        GUI.Label(new Rect(rightX, top, colW, 34f), Loc.T("shop.upgrades"), bold);
        for (int i = 0; i < GameData.UpgradeCount; i++)
        {
            float y = top + 40f + i * 77f;
            RoundPanel(new Rect(rightX, y, colW, 69f), new Color(1f, 1f, 1f, 0.07f));
            GUI.Label(new Rect(rightX + 14f, y + 4f, 300f, 28f), GameData.UpgradeName(i), text);
            small.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(rightX + 14f, y + 34f, 320f, 26f), GameData.UpgradeDescription(i), small);
            small.alignment = TextAnchor.MiddleCenter;

            bool owned = gm.Data.upgrades[i];
            int price = GameData.UpgradePrices[i];
            GUI.enabled = !owned && gm.Data.money >= price;
            string label = owned ? Loc.T("shop.owned") : GameData.Money(price);
            if (Button(new Rect(rightX + colW - 220f, y + 13f, 206f, 42f), label)) gm.BuyUpgrade((Upgrade)i);
            GUI.enabled = true;
        }

        if (Button(new Rect(r.x + (r.width - 300f) / 2f, r.yMax - 70f, 300f, 54f), Loc.T("shop.close"))) gm.CloseShop();
    }

    void ShopRow(GameManager gm, int index, float x, float y, float w)
    {
        RoundPanel(new Rect(x, y, w, 56f), new Color(1f, 1f, 1f, 0.07f));
        var icon = IconRenderer.Get((ItemType)index);
        if (icon != null) GUI.DrawTexture(new Rect(x + 6f, y + 3f, 50f, 50f), icon);
        GUI.Label(new Rect(x + 64f, y + 3f, 240f, 28f), GameData.Name((ItemType)index), text);
        small.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(x + 64f, y + 29f, 240f, 24f), Loc.F("bin.inStock", gm.Data.stock[index]), small);
        small.alignment = TextAnchor.MiddleCenter;

        int price = GameData.PackPrice[index];
        GUI.enabled = gm.Data.money >= price;
        if (Button(new Rect(x + w - 236f, y + 7f, 222f, 42f), "+" + GameData.PackAmount[index] + "  " + GameData.Money(price)))
            gm.BuyPack(index);
        GUI.enabled = true;
    }

    void DrawSummaryLines(Rect r, string[] lines, float startY)
    {
        text.fontSize = 24;
        for (int i = 0; i < lines.Length; i++)
            GUI.Label(new Rect(r.x + 50f, startY + i * 38f, r.width - 100f, 36f), lines[i], text);
        text.fontSize = 22;
    }

    void DrawSummary(GameManager gm)
    {
        Dim(0.6f);
        var r = CenterRect(640f, 620f);
        RoundPanel(r, new Color(0.08f, 0.08f, 0.1f, 0.96f));
        GUI.Label(new Rect(r.x, r.y + 20f, r.width, 50f), Loc.F("summary.title", gm.SummaryDay), heading);

        var t = gm.Today;
        long profit = t.revenue + t.tips - t.spent - t.rent;
        string[] lines =
        {
            Loc.F("summary.served", t.served),
            Loc.F("summary.lost", t.lost),
            Loc.F("summary.revenue", GameData.Money(t.revenue)),
            Loc.F("summary.tips", GameData.Money(t.tips)),
            Loc.F("summary.purchases", (t.spent > 0 ? "-" : "") + GameData.Money(t.spent)),
            Loc.F("summary.rent", GameData.Money(t.rent)),
            "<b>" + Loc.T("summary.profit") + ":  <color=" + (profit >= 0 ? "#66ee77" : "#ff6655") + ">" + GameData.Money(profit) + "</color></b>",
            "<b>" + Loc.T("summary.balance") + ":  <color=#ffcc44>" + GameData.Money(gm.Data.money) + "</color></b>"
        };
        DrawSummaryLines(r, lines, r.y + 100f);
        GUI.Label(new Rect(r.x, r.yMax - 128f, r.width, 28f), Loc.T("summary.saved"), small);

        if (Button(new Rect(r.x + (r.width - 340f) / 2f, r.yMax - 90f, 340f, 60f), Loc.F("summary.next", gm.Data.day)))
            gm.NextDay();
    }

    void DrawGameOver(GameManager gm)
    {
        Dim(0.7f);
        var r = CenterRect(640f, 560f);
        RoundPanel(r, new Color(0.12f, 0.04f, 0.04f, 0.96f));
        title.normal.textColor = Bad;
        GUI.Label(new Rect(r.x, r.y + 20f, r.width, 90f), Loc.T("over.title"), title);
        title.normal.textColor = Accent;

        string[] lines =
        {
            Loc.T("over.rent"),
            Loc.F("over.days", gm.SummaryDay),
            Loc.F("over.served", gm.Data.totalServed),
            Loc.F("summary.lost", gm.Data.totalLost),
            Loc.F("over.balance", GameData.Money(gm.Data.money))
        };
        DrawSummaryLines(r, lines, r.y + 130f);

        float w = (r.width - 140f) / 2f;
        if (Button(new Rect(r.x + 50f, r.yMax - 100f, w, 60f), Loc.T("menu.newGame"))) gm.NewGame();
        if (Button(new Rect(r.x + 90f + w, r.yMax - 100f, w, 60f), Loc.T("pause.mainMenu"))) gm.QuitToMenu();
    }
}
