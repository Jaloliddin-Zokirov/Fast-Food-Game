using System.Collections.Generic;
using UnityEngine;

public enum CustomerState { Entering, Waiting, Eating, Leaving }

public enum CustomerKind { Child, Adult, Elder }

// Mijoz ikki xil bo'ladi:
//  - olib ketadigan (TakeAway): kassaga keladi, taomni qog'oz paketda olib ketadi;
//  - shu yerda yeydigan (DineIn): stolga o'tirib buyurtma beradi, taomni tarelkada oladi,
//    yeb bo'lgach stolda iflos tarelka qoldiradi.
// Sabri tugasa norozi bo'lib ketadi.
public class Customer : Interactable
{
    public static readonly List<Customer> All = new List<Customer>();

    public CustomerState state;
    public OrderType orderType;
    public CustomerKind kind;
    public readonly List<ItemType> order = new List<ItemType>();
    public int spot; // TakeAway — kassa oldidagi joy, DineIn — o'rindiq raqami
    public float patience;
    public float maxPatience;
    public int bill;

    readonly List<Vector3> path = new List<Vector3>();
    readonly List<Material> materials = new List<Material>();
    readonly List<Item> meal = new List<Item>(); // stolga qo'yilgan taomlar
    Animation anim;
    CapsuleCollider body;
    CharacterShape shape;
    float walkSpeed = 1.8f;
    float height = 1.7f;
    float pauseTime;
    float eatTime;
    bool seated;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void OnDestroy()
    {
        for (int i = 0; i < materials.Count; i++)
            if (materials[i] != null) Destroy(materials[i]);
    }

    public float PatienceFraction
    {
        get { return maxPatience > 0f ? Mathf.Clamp01(patience / maxPatience) : 0f; }
    }

    // O'tirganda bosh pastroq bo'ladi
    public Vector3 HeadPosition
    {
        get
        {
            if (seated)
            {
                var seat = CustomerManager.I.SeatAt(spot);
                return seat.chair + Vector3.up * (seat.seatHeight + shape.SeatedTop + 0.3f);
            }
            return transform.position + Vector3.up * (height + 0.3f);
        }
    }

    public void Setup(OrderType type, CustomerKind customerKind, int spotIndex, List<Vector3> enterPath, List<ItemType> wanted,
        float patienceSeconds, float speed, Animation animation, List<Material> modelMaterials, CharacterShape characterShape)
    {
        orderType = type;
        kind = customerKind;
        spot = spotIndex;
        path.AddRange(enterPath);
        order.AddRange(wanted);
        maxPatience = patienceSeconds;
        patience = patienceSeconds;
        walkSpeed = speed;
        anim = animation;
        materials.AddRange(modelMaterials);
        shape = characterShape;
        height = shape.Height;
        body = GetComponent<CapsuleCollider>();
        state = CustomerState.Entering;
        Play("walk");
    }

    void Play(string clip)
    {
        if (anim == null || anim[clip] == null || anim.IsPlaying(clip)) return;
        anim.CrossFade(clip, 0.2f);
    }

    void Update()
    {
        if (pauseTime > 0f)
        {
            pauseTime -= Time.deltaTime;
            return;
        }
        if (path.Count > 0) Walk();
        else if (state == CustomerState.Waiting) Wait();
        else if (state == CustomerState.Eating) Eat();
        else if (state == CustomerState.Leaving) Destroy(gameObject);
    }

    void Walk()
    {
        Play("walk");
        Vector3 target = path[0];
        Vector3 position = transform.position;
        Vector3 toTarget = target - position;
        toTarget.y = 0f;
        float step = walkSpeed * Time.deltaTime;

        if (toTarget.magnitude <= step)
        {
            transform.position = target;
            path.RemoveAt(0);
            if (path.Count == 0 && state == CustomerState.Entering)
            {
                state = CustomerState.Waiting;
                if (orderType == OrderType.DineIn) SitDown();
            }
        }
        else
        {
            transform.position = position + toTarget.normalized * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), Time.deltaTime * 10f);
        }
    }

    // Stulga o'tiradi: stolga qaraydi, tanasi o'rindiq balandligiga ko'tariladi
    void SitDown()
    {
        var seat = CustomerManager.I.SeatAt(spot);
        seated = true;
        shape.seated = true;
        transform.rotation = Quaternion.Euler(0f, seat.yaw, 0f);
        transform.position = seat.chair + transform.rotation * new Vector3(0f, seat.seatHeight, shape.SitForward);
        if (body != null)
        {
            float h = shape.SeatedTop + 0.15f;
            Vector3 center = transform.InverseTransformPoint(seat.chair + Vector3.up * (seat.seatHeight + h * 0.5f));
            body.center = center;
            body.height = h;
        }
        Play("sit");
    }

    void StandUp()
    {
        if (!seated) return;
        seated = false;
        shape.seated = false;
        transform.position = CustomerManager.I.SeatAt(spot).chair;
        if (body != null)
        {
            body.center = new Vector3(0f, height * 0.5f, 0f);
            body.height = height;
        }
    }

    void Wait()
    {
        if (seated)
        {
            Play("sit");
        }
        else
        {
            Play("idle");
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.back), Time.deltaTime * 6f);
        }

        // Grilda olov bo'lsa, tutundan bezovta bo'lib tezroq sabrsizlanadi
        float drain = GameManager.I.FireActive ? 2f : 1f;
        patience -= Time.deltaTime * drain;
        if (patience <= 0f)
        {
            GameManager.I.OnCustomerLost(this);
            Leave(false);
        }
    }

    // Stolda ovqatlanish: taom asta-sekin kamayadi, oxirida tarelka iflos bo'lib qoladi
    void Eat()
    {
        Play("sit");
        eatTime += Time.deltaTime;
        float k = Mathf.Clamp01(eatTime / GameData.EatSeconds);
        for (int i = 0; i < meal.Count; i++)
        {
            if (meal[i] == null || meal[i].type == ItemType.Coffee) continue;
            var content = meal[i].transform.Find("Content");
            if (content != null) content.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, k);
        }
        if (k < 1f) return;

        for (int i = 0; i < meal.Count; i++)
        {
            var food = meal[i];
            if (food == null) continue;
            if (food.container == Container.Plate)
            {
                var dirty = ItemFactory.Create(ItemType.DirtyPlate);
                dirty.transform.position = food.transform.position;
                dirty.transform.rotation = food.transform.rotation;
                dirty.SetFree(true);
            }
            Destroy(food.gameObject);
        }
        meal.Clear();
        Leave(true);
    }

    public void Leave(bool happy)
    {
        state = CustomerState.Leaving;
        if (!happy)
        {
            for (int i = 0; i < materials.Count; i++) materials[i].color = new Color(1f, 0.55f, 0.55f);
        }

        // Yeyilmay qolgan taom stolda qoladi — uni olib boshqa mijozga berish mumkin
        for (int i = 0; i < meal.Count; i++)
            if (meal[i] != null) meal[i].SetFree(true);
        meal.Clear();

        path.Clear();
        var manager = CustomerManager.I;
        if (orderType == OrderType.DineIn)
        {
            StandUp();
            path.AddRange(manager.ExitPathFromSeat(spot));
            manager.FreeSeat(spot);
            return;
        }

        // Ketishdan oldin qisqa ishora: xursand — "ha", norozi — "yo'q"
        string emote = happy ? "emote-yes" : "emote-no";
        if (anim != null && anim[emote] != null)
        {
            anim.CrossFade(emote, 0.1f);
            pauseTime = anim[emote].length * 1.2f;
        }
        path.AddRange(manager.ExitPath(transform.position));
        manager.FreeSpot(spot);
    }

    void PlaceOnTable(Item food)
    {
        var seat = CustomerManager.I.SeatAt(spot);
        food.transform.SetParent(null, true);
        food.transform.position = seat.MealPoint(meal.Count);
        food.transform.rotation = Quaternion.Euler(0f, seat.yaw + 180f, 0f);
        food.SetFree(false);
        meal.Add(food);
    }

    public string OrderText()
    {
        var names = new string[order.Count];
        for (int i = 0; i < order.Count; i++) names[i] = GameData.Name(order[i]);
        return string.Join(", ", names);
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        if (state != CustomerState.Waiting || player.Held == null || !GameData.IsProduct(player.Held.type)) return null;
        return Loc.F("act.serve", player.Held.DisplayName);
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        if (state == CustomerState.Entering) return Loc.T("customer.coming");
        if (state == CustomerState.Eating) return Loc.T("customer.eating");
        if (state == CustomerState.Leaving) return null;
        return Loc.F(orderType == OrderType.DineIn ? "customer.orderDineIn" : "customer.orderTakeAway", OrderText());
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var item = player.Held;
        if (state != CustomerState.Waiting || item == null) return;
        var gm = GameManager.I;

        if (!GameData.IsProduct(item.type))
        {
            gm.Toast(Loc.T("customer.notAsked"), true);
            return;
        }
        if (GameData.IsCookable(item.type) && item.State == CookState.Raw)
        {
            gm.Toast(Loc.T("customer.notPressed"), true);
            return;
        }
        if (GameData.IsCookable(item.type) && item.State == CookState.Burnt)
        {
            gm.Toast(Loc.T("customer.burnt"), true);
            return;
        }

        int index = order.IndexOf(item.type);
        if (index < 0)
        {
            gm.Toast(Loc.F("customer.notOrdered", GameData.Name(item.type).ToLower()), true);
            return;
        }

        // Shu yerda yeydiganga — tarelkada, olib ketadiganga — paketda
        if (GameData.NeedsContainer(item.type))
        {
            Container wanted = orderType == OrderType.DineIn ? Container.Plate : Container.Bag;
            if (item.container != wanted)
            {
                gm.Toast(Loc.T(orderType == OrderType.DineIn ? "customer.wantPlate" : "customer.wantBag"), true);
                return;
            }
        }

        order.RemoveAt(index);
        bill += GameData.Price(item.type);
        if (orderType == OrderType.DineIn) PlaceOnTable(player.Release());
        else player.DestroyHeld();
        SoundManager.Play(Sfx.Ready);

        if (order.Count > 0)
        {
            gm.Toast(Loc.F("customer.stillNeed", OrderText()), false);
            return;
        }

        int tip = Mathf.RoundToInt(bill * 0.3f * PatienceFraction);
        gm.OnCustomerPaid(this, bill, tip);
        Effects.Coins(HeadPosition - Vector3.up * 0.4f);
        if (orderType == OrderType.DineIn)
        {
            state = CustomerState.Eating;
            eatTime = 0f;
        }
        else
        {
            Leave(true);
        }
    }
}
