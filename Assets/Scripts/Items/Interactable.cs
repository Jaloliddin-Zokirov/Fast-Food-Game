using UnityEngine;

// O'yinchi qarab turib E bosishi mumkin bo'lgan har qanday narsa.
public abstract class Interactable : MonoBehaviour
{
    // E bosilganda nima bo'lishini bildiruvchi matn. null bo'lsa — hozir harakat yo'q.
    public abstract string GetPrompt(PlayerInteractor player, Vector3 point);

    // Qo'shimcha ma'lumot satri (masalan, zaxira soni yoki xato sababi).
    public virtual string GetInfo(PlayerInteractor player, Vector3 point)
    {
        return null;
    }

    public abstract void Interact(PlayerInteractor player, Vector3 point);
}
