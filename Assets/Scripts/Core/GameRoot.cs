using UnityEngine;

// Sahnadagi yagona obyekt: Play bosilganda butun o'yin dunyosini kod orqali quradi.
public class GameRoot : MonoBehaviour
{
    void Awake()
    {
        if (GameManager.I != null)
        {
            Destroy(gameObject);
            return;
        }
        WorldBuilder.Build(transform);
    }

    // Sahnada GameRoot bo'lmasa ham (masalan, yangi bo'sh sahnada Play bosilsa) o'yin baribir ishga tushadi.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (GameManager.I != null) return;

        var cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++) cameras[i].gameObject.SetActive(false);
        var lights = Object.FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++) lights[i].gameObject.SetActive(false);

        new GameObject("Game").AddComponent<GameRoot>();
    }
}
