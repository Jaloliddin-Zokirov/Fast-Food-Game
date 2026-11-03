using UnityEngine;

// Axlat qutisi: qo'ldagi narsani yo'q qiladi. Tarelkadagi taom tashlanganda tarelka qo'lda qoladi
// (iflos bo'lib) — tarelkalar tashlanmaydi, yuviladi.
public class TrashBin : Interactable
{
    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held == null || held.type == ItemType.DirtyPlate) return null;
        return Loc.F("act.trash", held.DisplayName);
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        if (player.Held == null) return Loc.T("trash.info");
        return player.Held.type == ItemType.DirtyPlate ? Loc.T("trash.plate") : null;
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held == null) return;
        if (held.type == ItemType.DirtyPlate)
        {
            GameManager.I.Toast(Loc.T("trash.plate"), true);
            return;
        }

        bool plated = held.container == Container.Plate;
        player.DestroyHeld();
        SoundManager.Play(Sfx.Trash);
        if (plated) player.PickUp(ItemFactory.Create(ItemType.DirtyPlate));
    }
}
