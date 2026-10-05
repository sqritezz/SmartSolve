using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Attach to any part that can be cleaned with the practice brush
// (the fan blades, the fan case, etc.).
// The part starts dusty. While the brush touches it, the dust fades.
// It can only be cleaned once PracticeFanTask unlocks it (canBeCleaned).
// The object (or its Rigidbody parent) needs a collider.
public class PracticeCleanable : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Every mesh piece of this part")]
    public Renderer[] partRenderers;

    [Tooltip("Dusty tint multiplied over the original color")]
    public Color dirtyColor = new Color(0.55f, 0.5f, 0.4f);

    [Tooltip("Tint when clean (white = original color)")]
    public Color cleanColor = Color.white;

    [Header("Cleaning")]
    public string cleaningToolTag = "CleaningBrush";

    [Tooltip("Seconds the brush must touch this part to fully clean it (0 = instant)")]
    public float wipeTimeNeeded = 1.5f;

    [Tooltip("Set automatically by PracticeFanTask - leave unchecked")]
    public bool canBeCleaned = false;

    [Header("Audio")]
    public AudioSource cleanSound;

    [Header("Events")]
    public UnityEvent onCleaned;

    [Header("State (read-only)")]
    public bool isClean = false;
    [Range(0f, 1f)] public float cleanProgress = 0f;

    private float lastContactTime = -999f;

    // Works with URP/Standard (_BaseColor/_Color) and glTF (baseColorFactor) shaders
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
    }

    private void CacheMaterials()
    {
        tintTargets.Clear();
        if (partRenderers == null) return;

        foreach (var r in partRenderers)
        {
            if (r == null) continue;

            foreach (var mat in r.materials) // instanced copies, originals untouched
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

    public void SetCanBeCleaned(bool value)
    {
        canBeCleaned = value;
    }

    private void Update()
    {
        if (isClean || !canBeCleaned) return;

        bool isWiping = Time.time - lastContactTime < 0.1f;
        if (!isWiping) return;

        float step = wipeTimeNeeded > 0f ? Time.deltaTime / wipeTimeNeeded : 1f;
        cleanProgress = Mathf.Clamp01(cleanProgress + step);
        ApplyColor();

        if (cleanProgress >= 1f)
            FinishCleaning();
    }

    private void OnTriggerStay(Collider other)
    {
        CheckContact(other);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckContact(collision.collider);
    }

    private void CheckContact(Collider other)
    {
        if (isClean || !canBeCleaned) return;

        bool isBrush = other.CompareTag(cleaningToolTag) ||
                       (other.attachedRigidbody != null &&
                        other.attachedRigidbody.CompareTag(cleaningToolTag));

        if (isBrush)
            lastContactTime = Time.time;
    }

    private void FinishCleaning()
    {
        if (isClean) return;
        isClean = true;
        cleanProgress = 1f;
        ApplyColor();

        if (cleanSound != null)
            cleanSound.Play();

        onCleaned?.Invoke();
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