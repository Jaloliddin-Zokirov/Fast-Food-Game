using System.Globalization;
using UnityEngine;

// Tartib muhim: birinchi 10 tasi (Bun..Extinguisher) — sotib olinadigan zaxira, saqlashdagi stock[] indekslari bilan bir xil.
public enum ItemType { Bun, Patty, Cheese, Tomato, LavashBread, Meat, Sauce, PaperBag, CoffeeBeans, Extinguisher, Burger, Lavash, Coffee, DirtyPlate }

public enum CookState { Raw, Cooked, Burnt }

// Tayyor taom nimaga solingan: shu yerda yeyish uchun — tarelka, olib ketish uchun — qog'oz paket
public enum Container { None, Plate, Bag }

public enum OrderType { TakeAway, DineIn }

public enum Upgrade { GrillSlots, FastCooking, SecondPress, ThirdBoard, Advertising, Decor }

// O'yin balansi (narxlar, vaqtlar, retseptlar) shu faylda jamlangan.
public static class GameData
{
    public const int IngredientCount = 7; // Bun..Sauce — oziq-ovqat
    public const int StockCount = 10;     // + qog'oz paket, qahva doni, o't o'chirgich

    // O'yin puli — AQSh dollari (butun sonlarda). Bir xaridda nechta keladi va narxi:
    public static readonly int[] PackAmount = { 10, 10, 10, 10, 10, 10, 10, 20, 10, 1 };
    public static readonly int[] PackPrice = { 7, 18, 5, 4, 7, 20, 4, 3, 6, 12 };
    public static readonly int[] StartStock = { 20, 10, 10, 20, 10, 10, 10, 20, 10, 1 };

    public const int BurgerPrice = 8;
    public const int LavashPrice = 9;
    public const int CoffeePrice = 3;
    public const long StartMoney = 70;
    public const int PlateCount = 6; // tarelkalar sotib olinmaydi — yuvib qayta ishlatiladi

    public const float DayLengthSeconds = 300f; // 09:00 dan 21:00 gacha
    public const int OpenHour = 9;
    public const int CloseHour = 21;

    public const float WashSeconds = 3f;  // bitta tarelkani yuvish
    public const float BrewSeconds = 5f;  // bitta qahva
    public const float EatSeconds = 10f;  // mijoz stolda ovqatlanishi
    public const float FridgeWarnSeconds = 10f;   // muzlatgich eshigi shuncha ochiq tursa — signal
    public const float FridgeSpoilSeconds = 25f;  // shundan keyin go'sht aynay boshlaydi
    public const float SpoilEverySeconds = 3f;    // eshik ochiq turguncha har shuncha soniyada bittadan
    public const float FireAt = 2.6f;     // kuygan go'sht grilda yana ~4-5 soniya qolsa (daraja 2.6), yonib ketadi

    public const int UpgradeCount = 6;
    public static readonly int[] UpgradePrices = { 80, 65, 55, 40, 95, 55 };

    public static readonly ItemType[] BurgerFillings = { ItemType.Patty, ItemType.Cheese, ItemType.Tomato };
    public static readonly ItemType[] LavashFillings = { ItemType.Meat, ItemType.Tomato, ItemType.Sauce };

    // Nomlar va yaxshilanishlar matni tanlangan tilda (Loc jadvalidan)
    public static string Name(ItemType t)
    {
        return Loc.T("item." + t);
    }

    public static string UpgradeName(int index)
    {
        return Loc.T("upgrade." + index);
    }

    public static string UpgradeDescription(int index)
    {
        return Loc.T("upgrade." + index + ".desc");
    }

    public static bool IsIngredient(ItemType t)
    {
        return (int)t < IngredientCount;
    }

    public static bool IsProduct(ItemType t)
    {
        return t == ItemType.Burger || t == ItemType.Lavash || t == ItemType.Coffee;
    }

    // Burger va lavash tarelkaga yoki paketga solinadi; qahva o'z stakanida beriladi
    public static bool NeedsContainer(ItemType t)
    {
        return t == ItemType.Burger || t == ItemType.Lavash;
    }

    // Muzlatgichda saqlanadigan, ochiq qolsa aynaydigan mahsulotlar
    public static bool CanSpoil(ItemType t)
    {
        return t == ItemType.Patty || t == ItemType.Meat;
    }

    public static bool IsCookable(ItemType t)
    {
        return t == ItemType.Patty || t == ItemType.Meat || t == ItemType.Lavash;
    }

    public static int Price(ItemType t)
    {
        if (t == ItemType.Burger) return BurgerPrice;
        if (t == ItemType.Lavash) return LavashPrice;
        if (t == ItemType.Coffee) return CoffeePrice;
        return 0;
    }

    // Xomdan tayyor holatgacha ketadigan vaqt (soniya)
    public static float CookSeconds(ItemType t)
    {
        switch (t)
        {
            case ItemType.Patty: return 9f;
            case ItemType.Meat: return 7f;
            case ItemType.Lavash: return 5f;
        }
        return 1f;
    }

    // Tayyor bo'lgandan keyin kuyib qolguncha vaqt (soniya)
    public static float BurnSeconds(ItemType t)
    {
        switch (t)
        {
            case ItemType.Patty: return 8f;
            case ItemType.Meat: return 7f;
            case ItemType.Lavash: return 6f;
        }
        return 1f;
    }

    public static ItemType[] FillingsFor(ItemType baseType)
    {
        return baseType == ItemType.Bun ? BurgerFillings : LavashFillings;
    }

    public static int RentForDay(int day)
    {
        return Mathf.Min(50, 15 + 3 * (day - 1));
    }

    // Har ikki tilda bir xil: $8, $1,250, -$15
    public static string Money(long amount)
    {
        string digits = System.Math.Abs(amount).ToString("N0", CultureInfo.InvariantCulture);
        return (amount < 0 ? "-$" : "$") + digits;
    }
}
