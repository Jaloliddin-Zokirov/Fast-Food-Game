using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// O'yin boshida har bir ovqat modelini alohida kamera bilan "suratga olib", UI uchun ikonchalar yasaydi.
// Shunda ikonchalar o'yindagi modellar bilan doim bir xil ko'rinadi va tashqi rasm kerak bo'lmaydi.
public class IconRenderer : MonoBehaviour
{
    public static IconRenderer I;

    const int IconLayer = 31;
    const int Size = 128;

    readonly Dictionary<ItemType, Texture2D> icons = new Dictionary<ItemType, Texture2D>();
    Texture2D plateIcon;

    void Awake()
    {
        I = this;
    }

    public static Texture2D Get(ItemType type)
    {
        Texture2D tex;
        return I != null && I.icons.TryGetValue(type, out tex) ? tex : null;
    }

    // Toza tarelka ikonchasi (zaxira panelidagi "tarelkalar" qatori uchun)
    public static Texture2D Plate
    {
        get { return I != null ? I.plateIcon : null; }
    }

    static void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
    }

    IEnumerator Start()
    {
        var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;

        var camGo = new GameObject("IconCamera");
        camGo.transform.SetParent(transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = 1 << IconLayer;
        cam.fieldOfView = 20f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 20f;
        cam.targetTexture = rt;
        cam.enabled = false;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        // Sahnadan ancha yuqorida — hech narsa soya tashlamaydi
        var stage = new Vector3(0f, 60f, 0f);
        ItemType[] types =
        {
            ItemType.Burger, ItemType.Lavash, ItemType.Coffee, ItemType.Bun, ItemType.Patty, ItemType.Cheese,
            ItemType.Tomato, ItemType.LavashBread, ItemType.Meat, ItemType.Sauce,
            ItemType.PaperBag, ItemType.CoffeeBeans, ItemType.Extinguisher
        };

        yield return null;
        // Oxirgi qadamda ovqat o'rniga toza tarelka suratga olinadi
        for (int i = 0; i <= types.Length; i++)
        {
            bool plate = i == types.Length;
            GameObject subject;
            if (plate)
            {
                subject = ModelLib.SpawnFit("Food/plate", transform, Vector3.zero, 0f, 0.3f, false);
                subject.transform.position = stage;
            }
            else
            {
                var item = ItemFactory.Create(types[i], types[i] == ItemType.Lavash ? 1.2f : 0f);
                item.SetFree(false);
                item.transform.position = stage;
                item.transform.rotation = Quaternion.Euler(0f, types[i] == ItemType.Lavash ? 25f : 20f, 0f);
                subject = item.gameObject;
            }
            SetLayer(subject, IconLayer);

            Bounds b = Util.RendererBounds(subject);
            float radius = Mathf.Max(0.05f, b.extents.magnitude);
            float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.82f;
            cam.transform.position = b.center + new Vector3(0f, 0.75f, -1f).normalized * distance;
            cam.transform.LookAt(b.center);
            cam.enabled = true;

            yield return new WaitForEndOfFrame();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            if (plate) plateIcon = tex;
            else icons[types[i]] = tex;

            cam.enabled = false;
            Destroy(subject);
            yield return null;
        }

        cam.targetTexture = null;
        Destroy(camGo);
        rt.Release();
        Destroy(rt);
    }
}
