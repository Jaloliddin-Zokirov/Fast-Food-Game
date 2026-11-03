using UnityEngine;

// O'yin holati. Har kun oxirida PlayerPrefs ga JSON ko'rinishida saqlanadi.
[System.Serializable]
public class SaveData
{
    const string Key = "LavashBurger_Save_v1";
    const int CurrentVersion = 1; // 1 — pul dollarda (0 — eski so'mli saqlash)

    public int day = 1;
    public long money = GameData.StartMoney;
    public int[] stock;
    public int[] spoiled; // muzlatgichda aynib qolgan (hali tashlanmagan) mahsulotlar
    public bool[] upgrades;
    public int totalServed;
    public int totalLost;
    public int version; // ataylab boshlang'ich qiymatsiz: eski saqlashlarda 0 bo'lib qoladi

    public static SaveData NewGame()
    {
        var data = new SaveData();
        data.stock = (int[])GameData.StartStock.Clone();
        data.spoiled = new int[GameData.StockCount];
        data.upgrades = new bool[GameData.UpgradeCount];
        data.version = CurrentVersion;
        return data;
    }

    public static bool Exists()
    {
        return PlayerPrefs.HasKey(Key);
    }

    public static SaveData Load()
    {
        if (!Exists()) return NewGame();
        SaveData data = null;
        try
        {
            data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Saqlangan o'yinni o'qib bo'lmadi: " + e.Message);
        }
        if (data == null) return NewGame();

        // Eski yoki buzilgan saqlashlarni tuzatish
        // Yangi zaxira turlari qo'shilganda eskilari saqlanadi, yangilari boshlang'ich miqdorda beriladi
        if (data.stock == null || data.stock.Length != GameData.StockCount)
        {
            var fixedStock = (int[])GameData.StartStock.Clone();
            if (data.stock != null)
                for (int i = 0; i < data.stock.Length && i < fixedStock.Length; i++) fixedStock[i] = data.stock[i];
            data.stock = fixedStock;
        }
        if (data.spoiled == null || data.spoiled.Length != GameData.StockCount)
        {
            var fixedSpoiled = new int[GameData.StockCount];
            if (data.spoiled != null)
                for (int i = 0; i < data.spoiled.Length && i < fixedSpoiled.Length; i++) fixedSpoiled[i] = data.spoiled[i];
            data.spoiled = fixedSpoiled;
        }
        if (data.upgrades == null || data.upgrades.Length != GameData.UpgradeCount)
        {
            var fixedUpgrades = new bool[GameData.UpgradeCount];
            if (data.upgrades != null)
                for (int i = 0; i < data.upgrades.Length && i < fixedUpgrades.Length; i++)
                    fixedUpgrades[i] = data.upgrades[i];
            data.upgrades = fixedUpgrades;
        }
        if (data.day < 1) data.day = 1;

        // Eski saqlashlarda pul so'mda edi — dollarga o'tkazamiz (o'yindagi kurs: $1 = 3 750 so'm)
        if (data.version < 1)
        {
            data.money = (long)System.Math.Round(data.money / 3750.0);
            data.version = CurrentVersion;
        }
        return data;
    }

    public static void Delete()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    public void Save()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    public bool Has(Upgrade upgrade)
    {
        return upgrades[(int)upgrade];
    }
}
