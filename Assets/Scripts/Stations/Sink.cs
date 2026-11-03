using System.Collections.Generic;
using UnityEngine;

// Rakovina: iflos tarelkani yuvadi, yuvilgan tarelka toza tarelkalar to'plamiga qaytadi.
public class Sink : Interactable
{
    public static readonly List<Sink> All = new List<Sink>();

    public Transform plateSpot;

    Item washing;
    float progress;
    ParticleSystem water;

    // 0..1 — yuvilmoqda, -1 — bo'sh (UI dagi chiziq uchun)
    public float Progress
    {
        get { return washing != null ? progress : -1f; }
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        water = Effects.Water(plateSpot, Vector3.up * 0.26f);
    }

    void Update()
    {
        if (water != null)
        {
            var emission = water.emission;
            emission.rateOverTime = washing != null ? 80f : 0f;
        }
        if (washing == null) return;

        progress += Time.deltaTime / GameData.WashSeconds;
        if (progress < 1f) return;

        Destroy(washing.gameObject);
        washing = null;
        GameManager.I.CleanPlates++;
        SoundManager.Play(Sfx.Plate);
    }

    public override string GetPrompt(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        return held != null && held.type == ItemType.DirtyPlate && washing == null ? Loc.T("sink.wash") : null;
    }

    public override string GetInfo(PlayerInteractor player, Vector3 point)
    {
        return Loc.T(washing != null ? "sink.busy" : "sink.info");
    }

    public override void Interact(PlayerInteractor player, Vector3 point)
    {
        var held = player.Held;
        if (held == null) return;
        if (held.type != ItemType.DirtyPlate)
        {
            GameManager.I.Toast(Loc.T("sink.onlyPlates"), true);
            return;
        }
        if (washing != null)
        {
            GameManager.I.Toast(Loc.T("sink.busy"), true);
            return;
        }

        washing = player.Release();
        washing.transform.SetParent(plateSpot, false);
        washing.transform.localPosition = Vector3.zero;
        washing.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        progress = 0f;
        SoundManager.Play(Sfx.Water);
    }

    public void ClearAll()
    {
        if (washing != null) Destroy(washing.gameObject);
        washing = null;
    }
}
