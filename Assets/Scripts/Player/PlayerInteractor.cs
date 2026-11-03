using UnityEngine;

// Ekran markazidan nur (raycast) otib, qaralayotgan narsa bilan ishlaydi va qo'ldagi narsani ushlab turadi.
public class PlayerInteractor : MonoBehaviour
{
    public Camera cam;
    public Transform holdPoint;
    public float reach = 2.7f;

    public Item Held { get; private set; }
    public Interactable Target { get; private set; }
    public string Prompt { get; private set; }
    public string Info { get; private set; }

    Vector3 targetPoint;
    Vector3 placePoint;
    bool canPlaceFree;

    void Update()
    {
        if (GameManager.I == null || !GameManager.I.PlayerControlEnabled)
        {
            Target = null;
            Prompt = null;
            Info = null;
            return;
        }

        Scan();
        if (GameInput.InteractPressed()) DoInteract();
    }

    void Scan()
    {
        Target = null;
        Prompt = null;
        Info = null;
        canPlaceFree = false;

        RaycastHit hit;
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Ignore)) return;

        var interactable = hit.collider.GetComponentInParent<Interactable>();
        if (interactable != null && interactable != Held)
        {
            Target = interactable;
            targetPoint = hit.point;
            Prompt = interactable.GetPrompt(this, hit.point);
            Info = interactable.GetInfo(this, hit.point);
        }
        else if (Held != null && hit.normal.y > 0.6f)
        {
            canPlaceFree = true;
            placePoint = hit.point;
            Prompt = Loc.F("act.putDown", Held.DisplayName);
        }
    }

    void DoInteract()
    {
        // Harakat mumkin bo'lmasa ham Interact chaqiriladi — har bir obyekt sababini o'zi aytadi.
        if (Target != null)
        {
            Target.Interact(this, targetPoint);
        }
        else if (canPlaceFree && Held != null)
        {
            var item = Release();
            item.transform.position = placePoint;
            item.transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            item.SetFree(true);
            SoundManager.Play(Sfx.Place);
        }
    }

    public void PickUp(Item item)
    {
        if (Held != null) return;
        Held = item;
        item.SetFree(false);
        item.transform.SetParent(holdPoint, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        SoundManager.Play(Sfx.Pickup);
    }

    // Qo'ldagi narsani bo'shatadi (dunyoga chiqaradi) va qaytaradi.
    public Item Release()
    {
        var item = Held;
        Held = null;
        if (item != null) item.transform.SetParent(null, true);
        return item;
    }

    public void DestroyHeld()
    {
        if (Held == null) return;
        Destroy(Held.gameObject);
        Held = null;
    }
}
