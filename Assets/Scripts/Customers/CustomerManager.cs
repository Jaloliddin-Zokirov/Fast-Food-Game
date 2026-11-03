using System.Collections.Generic;
using UnityEngine;

// Stol yonidagi bitta o'rindiq (shu yerda yeydigan mijozlar uchun)
public class Seat
{
    public Vector3 chair;      // stul o'rni (polda)
    public float yaw;          // o'tirganda qaraydigan tomon — stolga
    public float seatHeight;   // o'rindiq balandligi
    public Vector3 table;      // stol markazi, ustki yuzasi balandligida
    public float tableRadius;
    public Vector3[] route;    // eshikdan kirgandan keyin stulgacha bo'lgan yo'l (stulning o'zisiz)
    public bool taken;

    // Stolda shu mijozning taomlari turadigan joylar: birinchisi ro'parasida, keyingilari ikki yonida
    public Vector3 MealPoint(int index)
    {
        Vector3 toChair = chair - table;
        toChair.y = 0f;
        toChair.Normalize();
        float[] angles = { 0f, 58f, -58f };
        float angle = angles[Mathf.Clamp(index, 0, angles.Length - 1)];
        float distance = Mathf.Max(0.15f, tableRadius - 0.17f);
        return table + Quaternion.Euler(0f, angle, 0f) * toChair * distance;
    }
}

// Do'kon ochiq paytda mijozlarni chaqiradi, kassa oldidagi joylar va stollardagi o'rindiqlarni boshqaradi.
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager I;

    const float CharacterYaw = 0f;

    // Yurish animatsiyasi tezligi: qadam oyoq uzunligiga mos bo'lishi uchun (tezlik / oyoq uzunligi ga ko'paytiriladi)
    const float WalkStride = 0.22f;

    // Mijoz turi bo'yicha ko'rinish va yurish: bo'y (metr), tezlik, sabr, oyoq/tana cho'zilishi, bosh, gavda eni
    struct Kind
    {
        public float minHeight, maxHeight, speed, patience, legs, body, head, width;

        public Kind(float minHeight, float maxHeight, float speed, float patience, float legs, float body, float head, float width)
        {
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            this.speed = speed;
            this.patience = patience;
            this.legs = legs;
            this.body = body;
            this.head = head;
            this.width = width;
        }
    }

    // Bolalar past, katta boshli va shoshqaloq; keksalar sekin yuradi, lekin sabrliroq
    static readonly Kind[] Kinds =
    {
        new Kind(1.0f, 1.25f, 2f, 0.85f, 1.3f, 1.1f, 0.9f, 0.95f),     // Child
        new Kind(1.65f, 1.85f, 1.8f, 1f, 1.9f, 1.45f, 0.68f, 0.8f),    // Adult
        new Kind(1.55f, 1.7f, 1.15f, 1.25f, 1.7f, 1.35f, 0.7f, 0.85f)  // Elder
    };

    public Vector3[] spots;
    public readonly List<Seat> seats = new List<Seat>();
    public Vector3 outside = new Vector3(0f, 0f, 12f);
    public Vector3 doorOutside = new Vector3(0f, 0f, 8f);
    public Vector3 doorInside = new Vector3(0f, 0f, 5.6f);

    static readonly string[] Characters =
    {
        "male-a", "male-b", "male-c", "male-d", "male-e", "male-f",
        "female-a", "female-b", "female-c", "female-d", "female-e", "female-f"
    };

    bool[] taken;
    float nextSpawn;

    void Awake()
    {
        I = this;
    }

    public void Init(Vector3[] spotPositions)
    {
        spots = spotPositions;
        taken = new bool[spots.Length];
    }

    public void AddSeat(Seat seat)
    {
        seats.Add(seat);
    }

    public Seat SeatAt(int index)
    {
        return seats[index];
    }

    // Ichkarida xizmat kutayotgan yoki ovqatlanayotgan mijozlar (chiqib ketayotganlar hisoblanmaydi)
    public int InsideCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < Customer.All.Count; i++)
                if (Customer.All[i].state != CustomerState.Leaving) count++;
            return count;
        }
    }

    // Do'kon ochilganda (ertalab yoki tanaffusdan keyin) birinchi mijoz tez keladi
    public void OnShopOpened()
    {
        nextSpawn = 2f;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.Phase != GamePhase.Open) return;

        nextSpawn -= Time.deltaTime;
        if (nextSpawn > 0f) return;

        int spot = RandomFreeSpot();
        int seat = RandomFreeSeat();
        if (spot < 0 && seat < 0)
        {
            nextSpawn = 1f;
            return;
        }

        // Taxminan har ikkinchi mijoz bo'sh stol bo'lsa o'tirib yeydi
        bool dineIn = seat >= 0 && (spot < 0 || Random.value < 0.45f);
        Spawn(dineIn ? OrderType.DineIn : OrderType.TakeAway, dineIn ? seat : spot);
        nextSpawn = NextInterval();
    }

    int RandomFreeSpot()
    {
        var free = new List<int>();
        for (int i = 0; i < taken.Length; i++)
            if (!taken[i]) free.Add(i);
        return free.Count == 0 ? -1 : free[Random.Range(0, free.Count)];
    }

    // Bo'sh va toza (iflos tarelka yoki qolgan taom turmagan) o'rindiq
    int RandomFreeSeat()
    {
        var free = new List<int>();
        for (int i = 0; i < seats.Count; i++)
            if (!seats[i].taken && SeatClean(seats[i])) free.Add(i);
        return free.Count == 0 ? -1 : free[Random.Range(0, free.Count)];
    }

    static bool SeatClean(Seat seat)
    {
        Vector3 center = seat.MealPoint(0);
        for (int i = 0; i < Item.All.Count; i++)
            if (Vector3.Distance(Item.All[i].transform.position, center) < 0.45f) return false;
        return true;
    }

    float NextInterval()
    {
        var data = GameManager.I.Data;
        float interval = Mathf.Max(9f, 30f * Mathf.Pow(0.9f, data.day - 1));
        if (data.Has(Upgrade.Advertising)) interval *= 0.75f;
        return Random.Range(interval * 0.7f, interval * 1.3f);
    }

    static CustomerKind RandomKind()
    {
        float r = Random.value;
        if (r < 0.22f) return CustomerKind.Child;
        if (r < 0.4f) return CustomerKind.Elder;
        return CustomerKind.Adult;
    }

    // Buyurtma: ba'zilar faqat qahva ichadi, ko'pchilik burger yoki lavash oladi (ba'zan qahva bilan).
    // Bolalar qahva ichmaydi; 3-kundan boshlab kattalar ikkita taom ham buyurishi mumkin.
    static List<ItemType> MakeOrder(int day, CustomerKind kind)
    {
        var wanted = new List<ItemType>();
        bool child = kind == CustomerKind.Child;
        if (!child && Random.value < 0.12f)
        {
            wanted.Add(ItemType.Coffee);
            return wanted;
        }

        int mains = 1;
        if (!child && day >= 3 && Random.value < Mathf.Min(0.45f, 0.15f * (day - 2))) mains = 2;
        for (int i = 0; i < mains; i++) wanted.Add(Random.value < 0.5f ? ItemType.Burger : ItemType.Lavash);
        if (!child && Random.value < 0.3f) wanted.Add(ItemType.Coffee);
        return wanted;
    }

    void Spawn(OrderType type, int index)
    {
        var data = GameManager.I.Data;
        CustomerKind kind = RandomKind();
        var look = Kinds[(int)kind];
        var wanted = MakeOrder(data.day, kind);

        int mains = 0;
        for (int i = 0; i < wanted.Count; i++)
            if (wanted[i] != ItemType.Coffee) mains++;
        float patience = Mathf.Max(45f, 80f - 3f * (data.day - 1)) + 25f * Mathf.Max(0, mains - 1);
        if (wanted.Contains(ItemType.Coffee) && mains > 0) patience += 10f;
        if (type == OrderType.DineIn) patience += 10f; // stolgacha olib borish kerak
        patience *= look.patience;
        if (data.Has(Upgrade.Decor)) patience *= 1.3f;

        var path = new List<Vector3> { doorOutside, doorInside };
        if (type == OrderType.DineIn)
        {
            var seat = seats[index];
            seat.taken = true;
            path.AddRange(seat.route);
            path.Add(seat.chair);
        }
        else
        {
            taken[index] = true;
            Vector3 spot = spots[index];
            path.Add(new Vector3(spot.x, 0f, spot.z + 1.6f));
            path.Add(spot);
        }

        Animation anim;
        List<Material> materials;
        CharacterShape shape;
        var customer = BuildCustomer(outside, look, out anim, out materials, out shape);
        customer.Setup(type, kind, index, path, wanted, patience, look.speed, anim, materials, shape);
        SoundManager.Play(Sfx.Chime, 0.6f);
    }

    public List<Vector3> ExitPath(Vector3 from)
    {
        return new List<Vector3> { new Vector3(from.x, 0f, from.z + 1.6f), doorInside, doorOutside, outside };
    }

    public List<Vector3> ExitPathFromSeat(int index)
    {
        var route = seats[index].route;
        var path = new List<Vector3>();
        for (int i = route.Length - 1; i >= 0; i--) path.Add(route[i]);
        path.Add(doorInside);
        path.Add(doorOutside);
        path.Add(outside);
        return path;
    }

    public void FreeSpot(int index)
    {
        if (taken != null && index >= 0 && index < taken.Length) taken[index] = false;
    }

    public void FreeSeat(int index)
    {
        if (index >= 0 && index < seats.Count) seats[index].taken = false;
    }

    public void ClearAll()
    {
        var customers = Customer.All.ToArray();
        for (int i = 0; i < customers.Length; i++)
        {
            customers[i].gameObject.SetActive(false);
            Destroy(customers[i].gameObject);
        }
        if (taken != null)
            for (int i = 0; i < taken.Length; i++) taken[i] = false;
        for (int i = 0; i < seats.Count; i++) seats[i].taken = false;
    }

    static Customer BuildCustomer(Vector3 position, Kind look, out Animation anim, out List<Material> materials, out CharacterShape shape)
    {
        var root = new GameObject("Mijoz");
        root.transform.position = position;

        string character = Characters[Random.Range(0, Characters.Length)];
        var holder = ModelLib.Spawn("Characters/character-" + character, root.transform, Vector3.zero, CharacterYaw, 1f);
        materials = ModelLib.OwnMaterials(holder);

        float height = Random.Range(look.minHeight, look.maxHeight);
        shape = root.AddComponent<CharacterShape>();
        var model = holder.transform.Find("Model");
        if (model != null) shape.Setup(model, height, look.legs, look.body, look.head, look.width);

        anim = holder.GetComponentInChildren<Animation>();
        if (anim != null)
        {
            if (anim["walk"] != null)
            {
                anim["walk"].wrapMode = WrapMode.Loop;
                anim["walk"].speed = WalkStride * look.speed / Mathf.Max(0.2f, shape.LegLength);
            }
            if (anim["idle"] != null) anim["idle"].wrapMode = WrapMode.Loop;
            if (anim["sit"] != null) anim["sit"].wrapMode = WrapMode.ClampForever;
        }

        var collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, height * 0.5f, 0f);
        collider.height = height;
        collider.radius = Mathf.Clamp(height * 0.17f, 0.2f, 0.3f);
        if (GameManager.I != null && GameManager.I.player != null) GameManager.I.player.IgnoreCollisionsWith(root);
        return root.AddComponent<Customer>();
    }
}
