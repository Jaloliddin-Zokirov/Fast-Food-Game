using UnityEngine;

// 3D yozuv (TextMesh): til almashganda matnini o'zi yangilaydi.
public class LocalizedLabel : MonoBehaviour
{
    System.Func<string> text;
    TextMesh mesh;

    public static TextMesh Attach(TextMesh mesh, System.Func<string> text)
    {
        var label = mesh.gameObject.AddComponent<LocalizedLabel>();
        label.mesh = mesh;
        label.text = text;
        label.Refresh();
        return mesh;
    }

    void OnEnable()
    {
        Loc.Changed += Refresh;
    }

    void OnDisable()
    {
        Loc.Changed -= Refresh;
    }

    void Refresh()
    {
        if (mesh != null && text != null) mesh.text = text();
    }
}
