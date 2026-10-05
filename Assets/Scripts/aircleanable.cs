using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Attach to the PSU FAN parent object (replaces PSUFanCleaner).
// Needs a collider (a trigger Box Collider around the fan is easiest).
// Cleaned ONLY by CompressedAirCan spraying at it - and only while the
// PSU switch is OFF. Spraying while it's ON shows a warning and counts
// as a wrong attempt. Touching it with the eraser counts as "wrong tool".
public class AirCleanable : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Every mesh piece of the fan (same 7 pieces as before)")]
    public Renderer[] dustyRenderers;
    public Color dirtyColor = new Color(0.55f, 0.5f, 0.4f);
    public Color cleanColor = Color.white;

    [Tooltip("Optional dust cloud particles that puff out while being sprayed")]
    public ParticleSystem dustCloud;

    [Header("Cleaning")]
    [Tooltip("Seconds of spraying needed to fully clean")]
    public float sprayTimeNeeded = 3f;

    [Tooltip("Set automatically by PSUEasyOverheat - leave unchecked")]
    public bool canBeCleaned = false;

    [Header("Safety - PSU must be OFF")]
    public PSUSwitch1 psuSwitch;
    public AudioSource warningSound;
    [Tooltip("e.g. a text: \"Turn off the PSU switch before cleaning!\"")]
    public GameObject powerOnWarningMessage;

    [Header("Not ready yet (PC not tested)")]
    [Tooltip("e.g. a text: \"Test the PC with the power button first.\"")]
    public GameObject notReadyMessage;

    [Header("Wrong Tool")]
    public string wrongToolTag = "Eraser";
    public AudioSource wrongToolSound;
    [Tooltip("e.g. a text: \"An eraser can't remove dust from a fan. Use compressed air.\"")]
    public GameObject wrongToolMessage;

    [Header("Message Timing")]
    public float messageShowTime = 2.5f;

    [Header("Audio")]
    public AudioSource cleanSound;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("Events")]
    public UnityEvent onCleaned;

    [Header("State (read-only)")]
    public bool isClean = false;
    [Range(0f, 1f)] public float cleanProgress = 0f;

    private float lastSprayTime = -999f;
    private float lastUnsafeSprayTime = -999f;
    private float lastNotReadySprayTime = -999f;
    private float wrongToolCooldownUntil = -999f;

    private static readonly string[] ColorProperties = { "_BaseColor", "_Color", "baseColorFactor" };

    private class TintTarget
    {
        public Material material;
        public string property;
        public Color originalColor;
    }

    private readonly List<TintTarget> tintTargets = new List<TintTarget>();

    private void Awake()
    {
        CacheMaterials();
    }

    private void Start()
    {
        isClean = false;
        cleanProgress = 0f;
        ApplyColor();

        if (powerOnWarningMessage != null) powerOnWarningMessage.SetActive(false);
        if (notReadyMessage != null) notReadyMessage.SetActive(false);
        if (wrongToolMessage != null) wrongToolMessage.SetActive(false);
    }

    private void Update()
    {
        // Stop the dust cloud shortly after the air stops hitting the fan
        if (dustCloud != null && dustCloud.isPlaying && Time.time - lastSprayTime > 0.15f)
            dustCloud.Stop();
    }

    // Called every frame by CompressedAirCan while it sprays at this object
    public void ReceiveAir(float deltaTime)
    {
        if (isClean) return;

        // Safety first: PSU still on
        if (psuSwitch != null && !psuSwitch.IsOff)
        {
            // Counts once per spray "burst", not every frame
            if (Time.time - lastUnsafeSprayTime > 0.5f)
            {
                if (warningSound != null) warningSound.Play();
                ShowMessage(powerOnWarningMessage);
                if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            }
            lastUnsafeSprayTime = Time.time;
            return;
        }

        // PC hasn't been tested yet
        if (!canBeCleaned)
        {
            if (Time.time - lastNotReadySprayTime > 0.5f)
                ShowMessage(notReadyMessage);
            lastNotReadySprayTime = Time.time;
            return;
        }

        lastSprayTime = Time.time;
        if (dustCloud != null && !dustCloud.isPlaying) dustCloud.Play();

        float step = sprayTimeNeeded > 0f ? deltaTime / sprayTimeNeeded : 1f;
        cleanProgress = Mathf.Clamp01(cleanProgress + step);
        ApplyColor();

        if (cleanProgress >= 1f)
            FinishCleaning();
    }

    private void FinishCleaning()
    {
        if (isClean) return;
        isClean = true;
        cleanProgress = 1f;
        ApplyColor();

        if (dustCloud != null) dustCloud.Stop();
        if (cleanSound != null) cleanSound.Play();

        onCleaned?.Invoke();
    }

    // ---------- Wrong tool (eraser) ----------
    private void OnTriggerEnter(Collider other) { CheckWrongTool(other); }
    private void OnCollisionEnter(Collision collision) { CheckWrongTool(collision.collider); }

    private void CheckWrongTool(Collider other)
    {
        if (isClean || string.IsNullOrEmpty(wrongToolTag)) return;
        if (Time.time < wrongToolCooldownUntil) return;

        bool isWrongTool = other.CompareTag(wrongToolTag) ||
                           (other.attachedRigidbody != null &&
                            other.attachedRigidbody.CompareTag(wrongToolTag));
        if (!isWrongTool) return;

        wrongToolCooldownUntil = Time.time + messageShowTime;

        if (wrongToolSound != null) wrongToolSound.Play();
        ShowMessage(wrongToolMessage);
        if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
    }

    // ---------- Helpers ----------
    private void ShowMessage(GameObject message)
    {
        if (message == null) return;
        StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }

    private void CacheMaterials()
    {
        tintTargets.Clear();
        if (dustyRenderers == null) return;

        foreach (var r in dustyRenderers)
        {
            if (r == null) continue;

            foreach (var mat in r.materials)
            {
                foreach (var prop in ColorProperties)
                {
                    if (mat.HasProperty(prop))
                    {
                        tintTargets.Add(new TintTarget
                        {
                            material = mat,
                            property = prop,
                            originalColor = mat.GetColor(prop)
                        });
                        break;
                    }
                }
            }
        }
    }

    private void ApplyColor()
    {
        Color tint = Color.Lerp(dirtyColor, cleanColor, cleanProgress);

        foreach (var t in tintTargets)
        {
            if (t.material == null) continue;
            Color c = t.originalColor * tint;
            c.a = t.originalColor.a;
            t.material.SetColor(t.property, c);
        }
    }
}