using System.Collections.Generic;
using UnityEngine;

// Prep — ertalabki tayyorgarlik (soat 09:00 da turadi), Open — do'kon ochiq, Break — kun o'rtasida yopiq (tanaffus),
// AfterHours — ish vaqti tugagan, o'yinchi do'konni yopishini kutadi
public enum GamePhase { Menu, Prep, Open, Break, AfterHours, Summary, GameOver }

public class DayStats
{
    public int served;
    public int lost;
    public long revenue;
    public long tips;
    public long spent;
    public long rent;
}

// O'yin oqimi: menyu -> tayyorgarlik -> do'kon ochiq (istalgancha tanaffus bilan) -> ish vaqti tugadi -> kun yakuni.
// Kun o'zi tugamaydi: 21:00 dan keyin yangi mijoz kelmaydi, o'yinchi ishlarini tugatib, tablodan do'konni yopadi.
public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public PlayerController player;
    public PlayerInteractor interactor;
    public CookStation grill;
    public CookStation press;
    public List<AssemblyBoard> boards = new List<AssemblyBoard>();
    public CustomerManager customers;
    public Sink sink;
    public CoffeeMachine coffee;
    public Fridge fridge;
    public Vector3 playerSpawn;
    public float playerSpawnYaw;
    public Vector3 menuViewPos;
    public float menuViewYaw;
    public bool HelpOpen;

    // Toza tarelkalar soni (har kuni ertalab hammasi toza bo'ladi)
    public int CleanPlates;

    public SaveData Data { get; private set; }
    public GamePhase Phase { get; private set; }
    public bool Paused { get; private set; }
    public bool ShopOpen { get; private set; }
    public DayStats Today { get; private set; }
    public float DayTime { get; private set; }
    public int SummaryDay { get; private set; }

    void Awake()
    {
        I = this;
        Phase = GamePhase.Menu;
        Today = new DayStats();
    }

    public bool InGame
    {
        get { return Phase == GamePhase.Prep || Phase == GamePhase.Open || Phase == GamePhase.Break || Phase == GamePhase.AfterHours; }
    }

    // Grilda olov bor — mijozlar tezroq sabrsizlanadi
    public bool FireActive
    {
        get { return (grill != null && grill.OnFire) || (press != null && press.OnFire); }
    }

    public bool PlayerControlEnabled
    {
        get { return InGame && !Paused && !ShopOpen; }
    }

    public string ClockText
    {
        get
        {
            float hours = GameData.OpenHour + DayTime / GameData.DayLengthSeconds * (GameData.CloseHour - GameData.OpenHour);
            int h = (int)hours;
            int m = (int)((hours - h) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }
    }

    void Update()
    {
        if (GameInput.PausePressed())
        {
            if (ShopOpen) CloseShop();
            else if (InGame) SetPaused(!Paused);
        }
        if (PlayerControlEnabled && GameInput.HelpPressed()) HelpOpen = !HelpOpen;

        bool locked = PlayerControlEnabled;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        // Do'kon bir marta ochilgach soat yuraveradi — tanaffusda ham
        if (Phase == GamePhase.Open || Phase == GamePhase.Break)
        {
            DayTime += Time.deltaTime;
            if (DayTime >= GameData.DayLengthSeconds)
            {
                DayTime = GameData.DayLengthSeconds;
                Phase = GamePhase.AfterHours;
                SoundManager.Play(Sfx.Chime);
                Toast(Loc.T("msg.closingTime"), false);
            }
        }
    }

    public void NewGame()
    {
        SaveData.Delete();
        Data = SaveData.NewGame();
        Data.Save();
        StartPrep();
    }

    public void ContinueGame()
    {
        Data = SaveData.Load();
        StartPrep();
    }

    void StartPrep()
    {
        ResetWorld();
        Today = new DayStats();
        DayTime = 0f;
        CleanPlates = GameData.PlateCount;
        Phase = GamePhase.Prep;
        SetPaused(false);
        ShopOpen = false;
        HelpOpen = Data.day == 1;
        ApplyUpgrades();
        player.Teleport(playerSpawn, playerSpawnYaw);
    }

    // Tablo: ertalab va tanaffusda — ochish, ish vaqtida — tanaffusga yopish, ish vaqti tugagach — kunni yakunlash
    public void UseSign()
    {
        switch (Phase)
        {
            case GamePhase.Prep:
            case GamePhase.Break:
                OpenForBusiness();
                break;
            case GamePhase.Open:
                CloseForBreak();
                break;
            case GamePhase.AfterHours:
                FinishDay();
                break;
        }
    }

    public void OpenForBusiness()
    {
        if (Phase != GamePhase.Prep && Phase != GamePhase.Break) return;
        bool firstTime = Phase == GamePhase.Prep;
        Phase = GamePhase.Open;
        HelpOpen = false;
        customers.OnShopOpened();
        SoundManager.Play(Sfx.Chime);
        Toast(Loc.T(firstTime ? "msg.opened" : "msg.reopened"), false);
    }

    // Kun o'rtasida yopish: ichkaridagi mijozlarga xizmat davom etadi, yangilari kelmaydi, soat esa yuraveradi
    void CloseForBreak()
    {
        if (Phase != GamePhase.Open) return;
        Phase = GamePhase.Break;
        SoundManager.Play(Sfx.Chime, 0.7f);
        Toast(Loc.T("msg.break"), false);
    }

    void FinishDay()
    {
        if (Phase != GamePhase.AfterHours) return;
        if (customers.InsideCount > 0)
        {
            Toast(Loc.T("msg.customersInside"), true);
            return;
        }
        EndDay();
    }

    void EndDay()
    {
        ResetWorld();
        SummaryDay = Data.day;
        Today.rent = GameData.RentForDay(Data.day);
        Data.money -= Today.rent;

        if (Data.money < 0)
        {
            Phase = GamePhase.GameOver;
            SaveData.Delete();
            SoundManager.Play(Sfx.Error);
            return;
        }

        Phase = GamePhase.Summary;
        Data.day++;
        Data.Save();
        SoundManager.Play(Sfx.Cash);
    }

    public void NextDay()
    {
        if (Phase == GamePhase.Summary) StartPrep();
    }

    public void QuitToMenu()
    {
        ResetWorld();
        SetPaused(false);
        ShopOpen = false;
        HelpOpen = false;
        Phase = GamePhase.Menu;
        player.Teleport(menuViewPos, menuViewYaw);
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    public void OpenShop()
    {
        if (InGame) ShopOpen = true;
    }

    public void CloseShop()
    {
        ShopOpen = false;
    }

    public void OnCustomerPaid(Customer customer, int bill, int tip)
    {
        Data.money += bill + tip;
        Data.totalServed++;
        Today.revenue += bill;
        Today.tips += tip;
        Today.served++;
        SoundManager.Play(Sfx.Cash);

        string text = "+" + GameData.Money(bill + tip);
        if (tip > 0) text += "\n" + Loc.F("msg.tip", GameData.Money(tip));
        GameUI.I.FloatText(customer.HeadPosition, text, new Color(0.3f, 1f, 0.4f));
    }

    public void OnCustomerLost(Customer customer)
    {
        Data.totalLost++;
        Today.lost++;
        SoundManager.Play(Sfx.Error);
        GameUI.I.FloatText(customer.HeadPosition, Loc.T("msg.left"), new Color(1f, 0.35f, 0.3f));
        Toast(Loc.T("msg.customerLeft"), false);
    }

    public bool BuyPack(int ingredient)
    {
        int price = GameData.PackPrice[ingredient];
        if (Data.money < price)
        {
            Toast(Loc.T("msg.noMoney"), true);
            return false;
        }
        Data.money -= price;
        Data.stock[ingredient] += GameData.PackAmount[ingredient];
        Today.spent += price;
        SoundManager.Play(Sfx.Cash, 0.6f);
        SaveIfPreparing();
        return true;
    }

    public bool BuyUpgrade(Upgrade upgrade)
    {
        int index = (int)upgrade;
        if (Data.upgrades[index]) return false;
        int price = GameData.UpgradePrices[index];
        if (Data.money < price)
        {
            Toast(Loc.T("msg.noMoney"), true);
            return false;
        }
        Data.money -= price;
        Data.upgrades[index] = true;
        Today.spent += price;
        ApplyUpgrades();
        SoundManager.Play(Sfx.Cash);
        Toast(Loc.F("msg.installed", GameData.UpgradeName(index)), false);
        SaveIfPreparing();
        return true;
    }

    // Do'kon ochilishidan oldingi xaridlar darhol saqlanadi. Kun davomida esa faqat kun oxirida
    // (aks holda kun o'rtasidagi tushum saqlanib, kunni qayta o'ynab pul ko'paytirish mumkin bo'lardi).
    void SaveIfPreparing()
    {
        if (Phase == GamePhase.Prep) Data.Save();
    }

    void ApplyUpgrades()
    {
        float speed = Data.Has(Upgrade.FastCooking) ? 1.5f : 1f;
        grill.speed = speed;
        press.speed = speed;
        grill.SetActiveSlots(Data.Has(Upgrade.GrillSlots) ? 4 : 2);
        press.SetActiveSlots(Data.Has(Upgrade.SecondPress) ? 2 : 1);
        if (boards.Count > 2) boards[2].gameObject.SetActive(Data.Has(Upgrade.ThirdBoard));
    }

    void ResetWorld()
    {
        interactor.DestroyHeld();
        grill.ClearAll();
        press.ClearAll();
        for (int i = 0; i < boards.Count; i++) boards[i].ClearAll();
        customers.ClearAll();
        if (sink != null) sink.ClearAll();
        if (coffee != null) coffee.ClearAll();
        if (fridge != null) fridge.Close();

        var items = Item.All.ToArray();
        for (int i = 0; i < items.Length; i++) Destroy(items[i].gameObject);
    }

    public void Toast(string message, bool error)
    {
        if (GameUI.I != null) GameUI.I.Toast(message, error);
        if (error) SoundManager.Play(Sfx.Error, 0.6f);
    }
}
