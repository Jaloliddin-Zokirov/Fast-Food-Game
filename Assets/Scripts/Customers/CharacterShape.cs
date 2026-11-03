using UnityEngine;

// Kenney Mini Characters juda "chibi": boshi katta, oyog'i kalta. Odam bo'yiga kattalashtirilganda boshlar
// ulkan bo'lib qoladi, shuning uchun animatsiyadan keyin suyaklar cho'ziladi: oyoq va tana uzunroq, bosh kichikroq,
// gavda biroz ingichkaroq. Bolalarda kamroq cho'ziladi — ular katta boshli, kalta oyoqli bo'lib qoladi.
public class CharacterShape : MonoBehaviour
{
    // Tik turgandagi bo'y, oyoq uzunligi va o'tirganda tosdan bosh tepasigacha (metr)
    public float Height { get; private set; }
    public float LegLength { get; private set; }
    public float SeatedTop { get; private set; }
    // O'tirganda stul suyanchig'iga kirib ketmasligi uchun oldinga siljish
    public float SitForward { get; private set; }

    public bool seated;

    Transform model, legLeft, legRight, torso, head;
    float legStretch, torsoStretch, headSize, width;
    float lift;
    Vector3 basePosition;

    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    public void Setup(Transform modelRoot, float targetHeight, float legs, float body, float headScale, float widthScale)
    {
        model = modelRoot;
        legLeft = Find(model, "leg-left");
        legRight = Find(model, "leg-right");
        torso = Find(model, "torso");
        head = Find(model, "head");
        legStretch = legs;
        torsoStretch = body;
        headSize = headScale;
        width = widthScale;

        // Asl (bind) holatdagi o'lchamlar model birligida: son bo'g'imi, bo'yin va bosh tepasi
        model.localScale = Vector3.one;
        Bounds bind = ModelLib.LocalBounds(model.gameObject, model);
        float hip = legLeft != null ? model.InverseTransformPoint(legLeft.position).y : bind.size.y * 0.26f;
        float neck = head != null ? model.InverseTransformPoint(head.position).y : bind.size.y * 0.51f;
        float top = bind.max.y;
        if (legLeft == null || torso == null || head == null) legStretch = torsoStretch = headSize = 1f;

        float stretched = hip * legStretch + (neck - hip) * torsoStretch + (top - neck) * headSize;
        float s = targetHeight / stretched;
        model.localScale = new Vector3(s * width, s, s * width);
        basePosition = new Vector3(-bind.center.x * s * width, -bind.min.y * s, -bind.center.z * s * width);
        lift = hip * (legStretch - 1f) * s;

        Height = targetHeight;
        LegLength = hip * legStretch * s;
        SeatedTop = ((neck - hip) * torsoStretch + (top - neck) * headSize + 0.03f) * s;
        SitForward = 0.045f * s;
        LateUpdate();
    }

    // Animatsiya suyaklarni har kadrda o'zgartiradi, shuning uchun cho'zish undan keyin qo'llanadi
    void LateUpdate()
    {
        if (model == null) return;
        if (legLeft != null) legLeft.localScale = new Vector3(1f, legStretch, 1f);
        if (legRight != null) legRight.localScale = new Vector3(1f, legStretch, 1f);
        if (torso != null) torso.localScale = new Vector3(1f, torsoStretch, 1f);
        // Bosh yumaloq qolishi uchun tana va gavda masshtabi qoplanadi
        if (head != null) head.localScale = new Vector3(headSize / width, headSize / torsoStretch, headSize / width);
        // Tik turganda oyoqlar uzaygani uchun model ko'tariladi; o'tirganda tos o'rindiqda qoladi
        model.localPosition = basePosition + Vector3.up * (seated ? 0f : lift);
    }
}
