using UnityEngine;

// RAM Hard: rub the eraser on the RAM's gold contacts to clean them.
// Cleans GRADUALLY like RAM Medium and the PSU fan: keep rubbing for
// "Clean Time Needed" seconds and the dirt fades away little by little.
// Stopping halfway keeps the progress.
public class HardRamCleaner : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer ramRenderer;
    public Material dirtyMaterial;
    public Material cleanMaterial;

    [Header("Cleaning speed")]
    [Tooltip("Seconds of rubbing needed to fully clean the RAM")]
    public float cleanTimeNeeded = 3f;
    [Tooltip("How close (meters) the eraser must be to count as rubbing")]
    public float touchDistance = 0.04f;

    [Header("Audio")]
    [Tooltip("Plays once when fully clean")]
    public AudioSource cleanSound;
    [Tooltip("Optional: loops while rubbing")]
    public AudioSource rubbingSound;

    [Header("Eraser Detection")]
    public string eraserTag = "Eraser";

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 1;
    public int objectiveIndex = 1;

    [Header("Events")]
    [Tooltip("Fires once when the RAM is fully clean (optional)")]
    public UnityEngine.Events.UnityEvent onCleaned;

    [Header("State (read-only)")]
    public bool isClean = false;
    [Range(0f, 1f)] public float cleanProgress = 0f;

    private float lastTouchTime = -999f;
    private Material liveMat;
    private readonly Collider[] hits = new Collider[16];

    private void Start()
    {
        isClean = false;
        cleanProgress = 0f;
        if (ramRenderer != null && dirtyMaterial != null)
        {
            liveMat = new Material(dirtyMaterial);
            ramRenderer.material = liveMat;
        }
        ApplyLook();
    }

    private void OnTriggerStay(Collider other) { Touch(other); }
    private void OnCollisionStay(Collision collision) { Touch(collision.collider); }

    private void Touch(Collider other)
    {
        if (other.CompareTag(eraserTag) ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(eraserTag)))
            lastTouchTime = Time.time;
    }

    private void Update()
    {
        if (isClean) { StopRub(); return; }

        // Distance check - works even when both objects are held
        CheckNearbyEraser();

        bool rubbing = Time.time - lastTouchTime < 0.1f;
        if (rubbing)
        {
            cleanProgress += Time.deltaTime / Mathf.Max(0.1f, cleanTimeNeeded);
            if (rubbingSound != null && !rubbingSound.isPlaying) { rubbingSound.loop = true; rubbingSound.Play(); }
            ApplyLook();
            if (cleanProgress >= 1f) CleanRam();
        }
        else StopRub();
    }

    private void CheckNearbyEraser()
    {
        if (ramRenderer == null) return;
        Bounds b = ramRenderer.bounds;
        b.Expand(touchDistance * 2f);
        int n = Physics.OverlapBoxNonAlloc(b.center, b.extents, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (hits[i] != null) Touch(hits[i]);
    }

    private void CleanRam()
    {
        if (isClean) return;
        isClean = true;
        cleanProgress = 1f;
        StopRub();
        ApplyLook();

        if (ramRenderer != null && cleanMaterial != null)
            ramRenderer.material = cleanMaterial;

        if (cleanSound != null)
            cleanSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);

        onCleaned?.Invoke();
    }

    private void StopRub()
    {
        if (rubbingSound != null && rubbingSound.isPlaying) rubbingSound.Stop();
    }

    private void ApplyLook()
    {
        if (ramRenderer == null) return;
        if (liveMat != null && dirtyMaterial != null && cleanMaterial != null)
            liveMat.Lerp(dirtyMaterial, cleanMaterial, cleanProgress);
        else
            ramRenderer.material = cleanProgress >= 1f ? cleanMaterial : dirtyMaterial;
    }
}