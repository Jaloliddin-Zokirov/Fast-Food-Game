using UnityEngine;

// Kompyuter: ingredient buyurtma qilish va yaxshilanishlar do'koni.
public class ShopComputer : Interactable
{
    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        return Loc.T("act.computer");
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        GameManager.I.OpenShop();
    }
}
