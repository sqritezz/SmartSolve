using System.Collections.Generic;
using UnityEngine;

// Put this on the PAUSE panel (Ypanelbtn).
// While the panel is open:
//   - game time stops (timer, physics, NPC walking, fans...)
//   - every animation freezes (the NPC stops breathing)
//   - sounds pause
//   - the world turns GRAY, the pause panel stays bright on top
// The headset, hands and ray keep working, so the buttons can still be clicked.
public class PauseWhileOpen : MonoBehaviour
{
    [Tooltip("Also pause sounds (music, loops) while paused")]
    public bool pauseAudio = true;

    [Header("Gray screen")]
    public bool grayScreen = true;
    [Tooltip("Gray color + how dark (alpha)")]
    public Color grayColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    [Tooltip("The SmartSolve/PauseDim shader (found automatically if empty)")]
    public Shader dimShader;
    [Tooltip("How far the gray starts (meters). Keep it a bit FARTHER than the pause panel, so the panel and ray stay bright.")]
    public float grayDistance = 1.4f;

    public static bool IsPaused { get; private set; }

    private readonly List<Animator> frozen = new List<Animator>();
    private readonly List<float> speeds = new List<float>();
    private GameObject dimObject;
    private Material dimMat;

    private void OnEnable()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;

        FreezeAnimators();
        if (grayScreen) ShowGray(true);
    }

    private void OnDisable()
    {
        Resume();
    }

    private void OnDestroy()
    {
        if (IsPaused) Resume();
        if (dimObject != null) Destroy(dimObject);
    }

    private void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        for (int i = 0; i < frozen.Count; i++)
            if (frozen[i] != null) frozen[i].speed = speeds[i];
        frozen.Clear();
        speeds.Clear();

        ShowGray(false);
    }

    // Stops ALL animations (some animators ignore timeScale), except the player's hands
    private void FreezeAnimators()
    {
        frozen.Clear(); speeds.Clear();
        Transform playerRoot = Camera.main != null ? Camera.main.transform.root : null;
        foreach (var a in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (a == null || !a.isActiveAndEnabled) continue;
            if (playerRoot != null && a.transform.IsChildOf(playerRoot)) continue;
            if (a.transform.IsChildOf(transform)) continue;      // this panel's own buttons
            frozen.Add(a);
            speeds.Add(a.speed);
            a.speed = 0f;
        }
    }

    private void ShowGray(bool on)
    {
        if (!on) { if (dimObject != null) dimObject.SetActive(false); return; }

        Camera cam = Camera.main;
        if (cam == null) return;

        if (dimObject == null)
        {
            if (dimShader == null) dimShader = Shader.Find("SmartSolve/PauseDim");
            if (dimShader == null)
            {
                Debug.LogWarning("[PauseWhileOpen] PauseDim shader not found - put PauseDim.shader in Assets.", this);
                return;
            }
            dimMat = new Material(dimShader);

            // a big see-through ball around the head; everything beyond it looks gray
            dimObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dimObject.name = "Pause Gray";
            Destroy(dimObject.GetComponent<Collider>());
            dimObject.transform.SetParent(cam.transform, false);
            dimObject.transform.localPosition = Vector3.zero;
            dimObject.transform.localScale = Vector3.one * (grayDistance * 2f);
            var r = dimObject.GetComponent<MeshRenderer>();
            r.sharedMaterial = dimMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        dimMat.SetColor("_Color", grayColor);
        dimObject.transform.localScale = Vector3.one * (grayDistance * 2f);

        dimObject.SetActive(true);
    }
}