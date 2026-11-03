using System.Collections.Generic;
using UnityEngine;

// Muzlatgich: kotlet va go'sht shu yerda saqlanadi. Ikki eshigi alohida ochiladi va yopiladi;
// javonlardagi go'shtni faqat eshik ochiq bo'lganda olish mumkin. Eshik uzoq ochiq qolsa ichi isiydi:
// avval signal chalinadi, keyin go'sht bittadan aynay boshlaydi (aynigani axlatga tashlanishi kerak).
public class Fridge : Interactable
{
    class Door
    {
        public Transform pivot;
        public float swing;     // ochilgandagi burilish burchagi
        public float side;      // chap eshik -1, o'ng eshik +1
        public IngredientBin bin; // ichidagi zaxira
        public Collider shelf;
        public bool open;
        public float amount;
        public float warmth;      // eshik ochiq turgani sari ortadi, yopilganda asta kamayadi (soniya)
        public float spoilTimer;
    }

    public Light lamp;

    readonly List<Door> doors = new List<Door>();
    float beepTimer;

    public void AddDoor(Transform pivot, float swing, IngredientBin bin)
    {
        var door = new Door();
        door.pivot = pivot;
        door.swing = swing;
        door.side = Mathf.Sign(pivot.localPosition.x);
        door.bin = bin;
        door.shelf = bin.GetComponent<Collider>();
        doors.Add(door);
        Apply();
    }

    // 0 — hammasi joyida, 1 — eshik uzoq ochiq turibdi, 2 — go'sht aynimoqda
    public int Warning
    {
        get
        {
            int level = 0;
            for (int i = 0; i < doors.Count; i++)
            {
                if (!doors[i].open) continue;
                if (doors[i].warmth >= GameData.FridgeSpoilSeconds) return 2;
                if (doors[i].warmth >= GameData.FridgeWarnSeconds) level = 1;
            }
            return level;
        }
    }

    void Update()
    {
        bool changed = false;
        for (int i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            float target = door.open ? 1f : 0f;
            if (Mathf.Approximately(door.amount, target)) continue;
            door.amount = Mathf.MoveTowards(door.amount, target, Time.deltaTime * 2.5f);
            changed = true;
        }
        if (changed) Apply();

        var gm = GameManager.I;
        if (gm == null || !gm.InGame) return;
        bool beep = false;
        for (int i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            if (!door.open)
            {
                door.warmth = Mathf.Max(0f, door.warmth - Time.deltaTime * 1.5f);
                door.spoilTimer = 0f;
                continue;
            }
            door.warmth += Time.deltaTime;
            if (door.warmth >= GameData.FridgeWarnSeconds) beep = true;
            if (door.warmth < GameData.FridgeSpoilSeconds) continue;

            door.spoilTimer -= Time.deltaTime;
            if (door.spoilTimer > 0f) continue;
            door.spoilTimer = GameData.SpoilEverySeconds;
            Spoil(door);
        }

        // Haqiqiy muzlatgichlar kabi ochiq qolsa pipillaydi
        if (beep)
        {
            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                beepTimer = 1.2f;
                SoundManager.Play(Sfx.Beep);
            }
        }
        else
        {
            beepTimer = 0f;
        }
    }

    // Bitta yangi mahsulot aynigan bo'ladi: u muzlatgichda qoladi va olinganda birinchi bo'lib chiqadi
    void Spoil(Door door)
    {
        var data = GameManager.I.Data;
        int index = (int)door.bin.type;
        if (data.stock[index] <= 0) return;
        data.stock[index]--;
        data.spoiled[index]++;
        GameManager.I.Toast(Loc.F("fridge.spoiled", GameData.Name(door.bin.type)), true);
    }

    void Apply()
    {
        float light = 0f;
        for (int i = 0; i < doors.Count; i++)
        {
            var door = doors[i];
            // Yumshoq harakat: boshida tez, oxirida sekin
            float eased = 1f - (1f - door.amount) * (1f - door.amount);
            door.pivot.localRotation = Quaternion.Euler(0f, door.swing * eased, 0f);
            if (door.shelf != null) door.shelf.enabled = door.amount > 0.6f;
            light = Mathf.Max(light, door.amount);
        }
        if (lamp != null)
        {
            lamp.intensity = light * 1.2f;
            lamp.enabled = light > 0.01f;
        }
    }

    // Qaralayotgan nuqta qaysi yarmida bo'lsa, o'sha eshik
    Door DoorAt(Vector3 point)
    {
        if (doors.Count == 0) return null;
        float x = transform.InverseTransformPoint(point).x;
        for (int i = 0; i < doors.Count; i++)
            if (doors[i].side < 0f == x < 0f) return doors[i];
        return doors[0];
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        var door = DoorAt(point);
        if (door == null) return null;
        return Loc.T(door.open ? "fridge.close" : "fridge.open");
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        return Loc.T("fridge.info");
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var door = DoorAt(point);
        if (door == null) return;
        door.open = !door.open;
        SoundManager.Play(Sfx.Door);
    }

    public void Close()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            doors[i].open = false;
            doors[i].amount = 0f;
            doors[i].warmth = 0f;
            doors[i].spoilTimer = 0f;
        }
        Apply();
    }
}
