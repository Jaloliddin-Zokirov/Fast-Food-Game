using System.Collections.Generic;
using UnityEngine;

// Gril va lavash pressi: joylarga qo'yilgan narsalar vaqt o'tishi bilan pishadi (va kuyadi).
// Grilda kuygan go'sht uzoq qolsa yonib ketadi — uni faqat o't o'chirgich bilan o'chirish mumkin.
public class CookStation : Interactable
{
    public static readonly List<CookStation> All = new List<CookStation>();

    public string nameKey; // Loc kaliti, masalan "station.grill"
    public ItemType[] accepts;
    public Transform[] slots;
    public Transform lid;
    public float lidOpenAngle = -70f; // qopqoq orqa sharnir atrofida X o'qi bo'yicha ochiladi
    public float speed = 1f;
    public bool canCatchFire;

    public int ActiveSlots { get; private set; }

    Item[] items;
    GameObject[] fires;
    AudioSource sizzle;
    AudioSource fireSound;
    ParticleSystem steam;
    float lidAngle = -70f;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    public string StationName
    {
        get { return Loc.T(nameKey); }
    }

    public bool OnFire
    {
        get
        {
            if (items == null) return false;
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].onFire) return true;
            return false;
        }
    }

    public void Init(string stationNameKey, ItemType[] acceptedTypes, Transform[] slotPoints)
    {
        nameKey = stationNameKey;
        accepts = acceptedTypes;
        slots = slotPoints;
        items = new Item[slots.Length];
        fires = new GameObject[slots.Length];
        SetActiveSlots(slots.Length);
        if (SoundManager.I != null)
        {
            sizzle = SoundManager.I.CreateSizzle(gameObject);
            if (canCatchFire) fireSound = SoundManager.I.CreateFireLoop(gameObject);
        }

        Vector3 center = Vector3.zero;
        for (int i = 0; i < slots.Length; i++) center += slots[i].localPosition;
        steam = Effects.Steam(transform, center / slots.Length + Vector3.up * 0.03f);
    }

    public void SetActiveSlots(int count)
    {
        ActiveSlots = Mathf.Clamp(count, 1, slots.Length);
        for (int i = 0; i < slots.Length; i++) slots[i].gameObject.SetActive(i < ActiveSlots);
    }

    public Item ItemAt(int index)
    {
        return items[index];
    }

    public Transform SlotAt(int index)
    {
        return slots[index];
    }

    void Update()
    {
        bool cooking = false;
        int cookingCount = 0, burntCount = 0, burningCount = 0;
        for (int i = 0; i < ActiveSlots; i++)
        {
            var item = items[i];
            if (item == null) continue;
            cooking = true;
            if (item.onFire)
            {
                burningCount++;
                continue; // yonayotgan narsa endi pishmaydi — faqat o'chirish kerak
            }
            if (item.State == CookState.Burnt) burntCount++;
            else cookingCount++;

            CookState before = item.State;
            item.AddCookTime(Time.deltaTime * speed);
            CookState after = item.State;
            if (before != after)
            {
                if (after == CookState.Cooked) SoundManager.Play(Sfx.Ready, 0.6f);
                else if (after == CookState.Burnt) SoundManager.Play(Sfx.Burn);
            }
            if (canCatchFire && !item.extinguished && item.cook >= GameData.FireAt) Ignite(i);
        }

        // Pishayotganda oq bug', kuyganda qora tutun, yonganda quyuq tutun
        if (steam != null)
        {
            var emission = steam.emission;
            emission.rateOverTime = burningCount > 0 ? 28f : burntCount > 0 ? 16f : cookingCount * 6f;
            var main = steam.main;
            main.startColor = burntCount + burningCount > 0 ? new Color(0.14f, 0.12f, 0.11f, 0.9f) : new Color(1f, 1f, 1f, 0.5f);
        }
        if (fireSound != null)
        {
            fireSound.volume = Mathf.MoveTowards(fireSound.volume, burningCount > 0 ? 0.7f : 0f, Time.deltaTime * 2f);
            if (fireSound.volume > 0.001f)
            {
                if (!fireSound.isPlaying) fireSound.Play();
            }
            else if (fireSound.isPlaying)
            {
                fireSound.Stop();
            }
        }

        if (sizzle != null)
        {
            sizzle.volume = Mathf.MoveTowards(sizzle.volume, cooking ? 0.45f : 0f, Time.deltaTime);
            if (sizzle.volume > 0.001f)
            {
                if (!sizzle.isPlaying) sizzle.Play();
            }
            else if (sizzle.isPlaying)
            {
                sizzle.Stop();
            }
        }

        if (lid != null)
        {
            lidAngle = Mathf.MoveTowards(lidAngle, cooking ? 0f : lidOpenAngle, Time.deltaTime * 160f);
            lid.localRotation = Quaternion.Euler(lidAngle, 0f, 0f);
        }
    }

    void Ignite(int index)
    {
        items[index].onFire = true;
        fires[index] = Effects.Fire(slots[index]);
        SoundManager.Play(Sfx.Alarm);
        GameManager.I.Toast(Loc.T("fire.alert"), true);
    }

    void PutOut()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].onFire)
            {
                items[i].onFire = false;
                items[i].extinguished = true;
                Effects.Foam(slots[i].position + Vector3.up * 0.1f);
            }
            if (fires[i] != null) Destroy(fires[i]);
            fires[i] = null;
        }
    }

    bool Accepts(ItemType type)
    {
        return System.Array.IndexOf(accepts, type) >= 0;
    }

    string AcceptsText()
    {
        var names = new string[accepts.Length];
        for (int i = 0; i < accepts.Length; i++) names[i] = GameData.Name(accepts[i]);
        return string.Join(", ", names);
    }

    // Nuqtaga eng yaqin bo'sh (yoki band) joy; topilmasa -1.
    int Nearest(Vector3 point, bool occupied)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < ActiveSlots; i++)
        {
            if ((items[i] != null) != occupied) continue;
            float d = (slots[i].position - point).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        if (player.Held != null)
        {
            if (player.Held.type == ItemType.Extinguisher) return OnFire ? Loc.T("fire.putOut") : null;
            if (!Accepts(player.Held.type) || player.Held.spoiled || Nearest(point, false) < 0) return null;
            return Loc.F("act.place", player.Held.DisplayName);
        }
        int index = Nearest(point, true);
        if (index < 0 || items[index].onFire) return null;
        return Loc.F("act.take", items[index].DisplayName);
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        if (OnFire && (player.Held == null || player.Held.type != ItemType.Extinguisher)) return Loc.T("fire.tooHot");
        if (player.Held != null && player.Held.type == ItemType.Extinguisher) return StationName;
        if (player.Held != null && !Accepts(player.Held.type)) return Loc.F("station.onlyAccepts", StationName, AcceptsText());
        if (player.Held != null && Nearest(point, false) < 0) return Loc.T("station.full");
        return StationName;
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        if (player.Held != null && player.Held.type == ItemType.Extinguisher)
        {
            if (!OnFire)
            {
                GameManager.I.Toast(Loc.T("fire.nothing"), true);
                return;
            }
            PutOut();
            player.DestroyHeld(); // o't o'chirgich bir martalik
            SoundManager.Play(Sfx.Spray);
            GameManager.I.Toast(Loc.T("fire.out"), false);
            return;
        }

        if (player.Held != null)
        {
            if (!Accepts(player.Held.type))
            {
                GameManager.I.Toast(Loc.F("station.onlyAccepts", StationName, AcceptsText()), true);
                return;
            }
            if (player.Held.spoiled)
            {
                GameManager.I.Toast(Loc.T("spoiled.reject"), true);
                return;
            }
            int free = Nearest(point, false);
            if (free < 0)
            {
                GameManager.I.Toast(Loc.F("station.fullToast", StationName), true);
                return;
            }
            var item = player.Release();
            item.transform.SetParent(slots[free], false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            items[free] = item;
            SoundManager.Play(Sfx.Place);
            return;
        }

        int taken = Nearest(point, true);
        if (taken < 0) return;
        if (items[taken].onFire)
        {
            GameManager.I.Toast(Loc.T("fire.tooHot"), true);
            return;
        }
        var picked = items[taken];
        items[taken] = null;
        player.PickUp(picked);
    }

    public void ClearAll()
    {
        if (items == null) return;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) Destroy(items[i].gameObject);
            items[i] = null;
            if (fires[i] != null) Destroy(fires[i]);
            fires[i] = null;
        }
    }
}
