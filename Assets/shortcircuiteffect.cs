using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// SHORT CIRCUIT EFFECT - builds itself.
// 1. Put this on an EMPTY object at the PSU's vent (blue Z arrow = spark direction).
// 2. Right-click the component header -> "Build Effect".
//    It creates 3 child effects (arc flashes, sparks, smoke) + glow materials
//    (saved in Assets/ShortCircuitFX so they work in the Android build).
// 3. Right-click -> "Preview Effect" to see it in the editor.
// PSUHardManager calls PlayEffect() when power is pressed with the dead PSU in.
public class ShortCircuitEffect1 : MonoBehaviour
{
    [Header("Sound")]
    public AudioSource crackleSound;

    [Header("Size")]
    [Tooltip("Scales the whole effect. Raise if it looks too small next to your PSU.")]
    public float effectScale = 1f;

    [Header("Built Parts (filled automatically by Build Effect)")]
    public ParticleSystem arcFlash;
    public ParticleSystem sparks;
    public ParticleSystem smoke;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Material smokeMaterial;

    // ---------- Play ----------
    public void PlayEffect()
    {
        if (arcFlash == null || sparks == null || smoke == null)
            Build(false);

        if (arcFlash != null) arcFlash.Play(true);
        if (sparks != null) sparks.Play(true);
        if (smoke != null) smoke.Play(true);
        if (crackleSound != null) crackleSound.Play();
    }

    [ContextMenu("Preview Effect")]
    private void Preview() { PlayEffect(); }

    [ContextMenu("Build Effect")]
    private void BuildFromMenu() { Build(true); }

    // ---------- Build ----------
    private void Build(bool saveAssets)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (!c.name.StartsWith("FX_")) continue;
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }

        EnsureMaterials(saveAssets);

        arcFlash = MakeSystem("FX_ArcFlash");
        SetupArcFlash(arcFlash);

        sparks = MakeSystem("FX_Sparks");
        SetupSparks(sparks);

        smoke = MakeSystem("FX_Smoke");
        SetupSmoke(smoke);

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
    }

    private ParticleSystem MakeSystem(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local; // ignore the PSU's big scale
        return ps;
    }

    private static ParticleSystem.Burst B(float time, int count)
    {
        return new ParticleSystem.Burst(time, new ParticleSystem.MinMaxCurve(count));
    }

    // Quick blue-white arc flashes, popping 4 times
    private void SetupArcFlash(ParticleSystem ps)
    {
        float s = effectScale;
        var main = ps.main;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f * s, 0.35f * s);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.85f, 1f), new Color(1f, 1f, 1f));
        main.maxParticles = 20;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { B(0f, 2), B(0.12f, 1), B(0.27f, 2), B(0.45f, 1) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f * s;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, Color.white, 1f, 0f));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = glowMaterial;
    }

    // White-hot spark streaks that shoot out with each flash and fall
    private void SetupSparks(ParticleSystem ps)
    {
        float s = effectScale;
        var main = ps.main;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * s, 3.5f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f * s, 0.025f * s);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.9f), new Color(1f, 0.85f, 0.45f));
        main.gravityModifier = 1.5f;
        main.maxParticles = 300;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { B(0f, 28), B(0.12f, 12), B(0.27f, 22), B(0.45f, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.01f * s;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0.35f),
                new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f) },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch; // streaks
        r.velocityScale = 0.04f;
        r.lengthScale = 1.5f;
        r.sharedMaterial = glowMaterial;
    }

    // Dark smoke puff that rises after the sparks
    private void SetupSmoke(ParticleSystem ps)
    {
        float s = effectScale;
        var main = ps.main;
        main.duration = 0.8f;
        main.startDelay = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f * s, 0.3f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f * s, 0.28f * s);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.25f, 0.25f, 0.25f, 0.55f), new Color(0.4f, 0.4f, 0.4f, 0.45f));
        main.gravityModifier = -0.04f; // drifts up
        main.maxParticles = 30;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { B(0f, 4), B(0.2f, 3), B(0.4f, 3) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.02f * s;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = smokeMaterial;
    }

    private static Gradient Fade(Color a, Color b, float alphaStart, float alphaEnd)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
            new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) });
        return g;
    }

    // ---------- Materials ----------
    private void EnsureMaterials(bool saveAssets)
    {
        if (glowMaterial != null && smokeMaterial != null) return;

        Texture2D dot = MakeSoftDot(64);

        Shader additive = Shader.Find("Legacy Shaders/Particles/Additive");
        Shader alpha = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (additive == null) additive = Shader.Find("Particles/Standard Unlit");
        if (alpha == null) alpha = Shader.Find("Particles/Standard Unlit");

        glowMaterial = new Material(additive) { name = "SC_Glow" };
        glowMaterial.mainTexture = dot;

        smokeMaterial = new Material(alpha) { name = "SC_Smoke" };
        smokeMaterial.mainTexture = dot;

#if UNITY_EDITOR
        if (saveAssets && !Application.isPlaying)
        {
            const string folder = "Assets/ShortCircuitFX";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "ShortCircuitFX");

            AssetDatabase.CreateAsset(dot, AssetDatabase.GenerateUniqueAssetPath(folder + "/SC_SoftDot.asset"));
            AssetDatabase.CreateAsset(glowMaterial, AssetDatabase.GenerateUniqueAssetPath(folder + "/SC_Glow.mat"));
            AssetDatabase.CreateAsset(smokeMaterial, AssetDatabase.GenerateUniqueAssetPath(folder + "/SC_Smoke.mat"));
            AssetDatabase.SaveAssets();
        }
#endif
    }

    // Soft round glow dot texture
    private static Texture2D MakeSoftDot(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "SC_SoftDot";
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half, dy = (y - half) / half;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}