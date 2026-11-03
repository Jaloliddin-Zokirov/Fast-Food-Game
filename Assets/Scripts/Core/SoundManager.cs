using UnityEngine;

public enum Sfx { Pickup, Place, Ready, Cash, Error, Chime, Trash, Burn, Spray, Bag, Plate, Water, Brew, Alarm, Door, Beep }

// Barcha tovushlar kod orqali sintez qilinadi — tashqi audio fayl kerak emas.
public class SoundManager : MonoBehaviour
{
    const int Rate = 44100;
    const float TwoPi = Mathf.PI * 2f;

    public static SoundManager I;
    public static float Volume = 0.8f;

    AudioSource source;
    AudioClip[] clips;
    AudioClip sizzleClip;
    AudioClip fireClip;
    readonly System.Random random = new System.Random(7);

    void Awake()
    {
        I = this;
        Volume = PlayerPrefs.GetFloat("volume", 0.8f);
        AudioListener.volume = Volume;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
        clips[(int)Sfx.Pickup] = Make("pickup", 0.09f, t => Sine(500f + 2500f * t, t) * Decay(t, 30f) * 0.35f);
        clips[(int)Sfx.Place] = Make("place", 0.14f, t => (Sine(150f - 300f * t, t) * 0.8f + Noise() * 0.3f) * Decay(t, 35f) * 0.6f);
        clips[(int)Sfx.Ready] = Make("ready", 0.6f, t => (Sine(1320f, t) + 0.5f * Sine(1980f, t) + 0.25f * Sine(2640f, t)) * Decay(t, 7f) * 0.25f);
        clips[(int)Sfx.Cash] = Make("cash", 0.55f, t =>
        {
            float first = Sine(1568f, t) * Decay(t, 9f);
            float second = t > 0.09f ? Sine(2093f, t - 0.09f) * Decay(t - 0.09f, 8f) : 0f;
            return (first + second + Noise() * Decay(t, 25f) * 0.3f) * 0.3f;
        });
        clips[(int)Sfx.Error] = Make("error", 0.32f, t =>
        {
            bool on = t < 0.13f || (t > 0.17f && t < 0.3f);
            return on ? Mathf.Sign(Sine(140f, t)) * 0.16f : 0f;
        });
        clips[(int)Sfx.Chime] = Make("chime", 0.75f, t =>
            (t < 0.28f ? Sine(988f, t) * Decay(t, 5f) : Sine(784f, t - 0.28f) * Decay(t - 0.28f, 5f)) * 0.3f);

        float lowPass = 0f;
        clips[(int)Sfx.Trash] = Make("trash", 0.3f, t =>
        {
            lowPass += (Noise() - lowPass) * 0.15f;
            return lowPass * Decay(t, 10f) * 0.9f;
        });

        float previous = 0f;
        clips[(int)Sfx.Burn] = Make("burn", 0.6f, t =>
        {
            float n = Noise();
            float high = n - previous;
            previous = n;
            return high * Decay(t, 4f) * 0.2f + Sine(300f - 200f * t, t) * Decay(t, 6f) * 0.12f;
        });

        // O't o'chirgich: kuchli shivillash
        float sprayPrev = 0f;
        clips[(int)Sfx.Spray] = Make("spray", 0.9f, t =>
        {
            float n = Noise();
            float high = n - sprayPrev * 0.6f;
            sprayPrev = n;
            float envelope = Mathf.Clamp01(t * 25f) * (t < 0.65f ? 1f : Decay(t - 0.65f, 14f));
            return high * envelope * 0.32f;
        });

        // Qog'oz paket shitirlashi: qisqa-qisqa xirillashlar
        clips[(int)Sfx.Bag] = Make("bag", 0.35f, t =>
        {
            float gate = Mathf.PerlinNoise(t * 90f, 0.3f) > 0.55f ? 1f : 0.15f;
            return Noise() * gate * Decay(t, 7f) * 0.35f;
        });

        // Chinni tarelka jiringlashi
        clips[(int)Sfx.Plate] = Make("plate", 0.4f, t =>
            (Sine(2350f, t) * 0.6f + Sine(3710f, t) * 0.35f + Sine(5230f, t) * 0.2f) * Decay(t, 14f) * 0.22f
            + Noise() * Decay(t, 120f) * 0.25f);

        // Oqayotgan suv
        float waterLow = 0f;
        clips[(int)Sfx.Water] = Make("water", 0.7f, t =>
        {
            waterLow += (Noise() - waterLow) * 0.3f;
            float bubble = Sine(600f + 400f * Mathf.PerlinNoise(t * 30f, 0.7f), t) * 0.15f;
            float envelope = Mathf.Clamp01(t * 12f) * Decay(Mathf.Max(0f, t - 0.4f), 8f);
            return (waterLow * 0.5f + bubble) * envelope * 0.5f;
        });

        // Qahva mashinasi: g'uvullash va bug' shivillashi
        float brewPrev = 0f;
        clips[(int)Sfx.Brew] = Make("brew", 1.3f, t =>
        {
            float n = Noise();
            float hiss = (n - brewPrev) * 0.12f;
            brewPrev = n;
            float hum = (Sine(110f, t) * 0.5f + Sine(220f, t) * 0.25f) * (0.7f + 0.3f * Mathf.Sin(TwoPi * 9f * t));
            float envelope = Mathf.Clamp01(t * 8f) * (t < 1f ? 1f : Decay(t - 1f, 10f));
            return (hum * 0.18f + hiss) * envelope;
        });

        // Yong'in signali: ikki tonli qisqa ovoz
        clips[(int)Sfx.Alarm] = Make("alarm", 0.9f, t =>
        {
            float frequency = ((int)(t / 0.15f)) % 2 == 0 ? 1180f : 880f;
            float on = (t % 0.15f) < 0.12f ? 1f : 0f;
            return Mathf.Sign(Sine(frequency, t)) * on * 0.11f;
        });

        // Muzlatgich eshigi: past tovushli "tuk" va havo shovqini
        float doorLow = 0f;
        clips[(int)Sfx.Door] = Make("door", 0.4f, t =>
        {
            doorLow += (Noise() - doorLow) * 0.08f;
            return Sine(90f - 40f * t, t) * Decay(t, 18f) * 0.45f + doorLow * Decay(t, 9f) * 0.6f;
        });

        // Muzlatgich eshigi ochiq qolganda: ikki qisqa ingichka "pip"
        clips[(int)Sfx.Beep] = Make("beep", 0.3f, t =>
        {
            bool on = t < 0.08f || (t > 0.14f && t < 0.22f);
            return on ? Sine(2600f, t) * 0.12f : 0f;
        });

        float sizzlePrev = 0f;
        sizzleClip = Make("sizzle", 2f, t =>
        {
            float n = Noise();
            float high = n - sizzlePrev;
            sizzlePrev = n;
            float crackle = random.NextDouble() < 0.002 ? Noise() * 0.8f : 0f;
            return (high * 0.22f + crackle) * (0.75f + 0.25f * Mathf.Sin(TwoPi * 3f * t));
        }, false);

        // Yonayotgan olov: past gurillash va chirsillash
        float fireLow = 0f;
        fireClip = Make("fire", 2f, t =>
        {
            fireLow += (Noise() - fireLow) * 0.04f;
            float crackle = random.NextDouble() < 0.004 ? Noise() : 0f;
            return fireLow * 1.4f * (0.8f + 0.2f * Mathf.Sin(TwoPi * 2f * t)) + crackle * 0.7f;
        }, false);
    }

    float Noise()
    {
        return (float)(random.NextDouble() * 2.0 - 1.0);
    }

    static float Sine(float frequency, float t)
    {
        return Mathf.Sin(TwoPi * frequency * t);
    }

    static float Decay(float t, float rate)
    {
        return Mathf.Exp(-t * rate);
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float> wave, bool fadeOut = true)
    {
        int samples = Mathf.CeilToInt(seconds * Rate);
        var data = new float[samples];
        int fade = Rate / 200; // 5 ms
        for (int i = 0; i < samples; i++)
        {
            float value = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            if (fadeOut && i > samples - fade) value *= (samples - i) / (float)fade;
            data[i] = value;
        }
        var clip = AudioClip.Create(name, samples, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void Play(Sfx sfx, float volume = 1f)
    {
        if (I == null) return;
        I.source.PlayOneShot(I.clips[(int)sfx], volume);
    }

    public AudioSource CreateSizzle(GameObject host)
    {
        return CreateLoop(host, sizzleClip);
    }

    public AudioSource CreateFireLoop(GameObject host)
    {
        return CreateLoop(host, fireClip);
    }

    AudioSource CreateLoop(GameObject host, AudioClip clip)
    {
        var a = host.AddComponent<AudioSource>();
        a.clip = clip;
        a.loop = true;
        a.playOnAwake = false;
        a.volume = 0f;
        a.spatialBlend = 0.7f;
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 1.5f;
        a.maxDistance = 15f;
        return a;
    }

    public static void SetVolume(float volume)
    {
        Volume = volume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("volume", volume);
    }
}
