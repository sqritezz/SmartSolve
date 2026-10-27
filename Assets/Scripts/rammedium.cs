using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// RAM Medium: rub the eraser on the RAM's gold contacts to clean them.
// Like the PSU fan cleaning, it cleans GRADUALLY: keep rubbing for
// "Clean Time Needed" seconds and the dirt fades away little by little.
// Stopping halfway keeps the progress.
public class RamMedium : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer ramRenderer;
    public Material dirtyMat;
    public Material cleanMat;

    [Header("Cleaning speed")]
    [Tooltip("Seconds of rubbing needed to fully clean the RAM")]
    public float cleanTimeNeeded = 3f;
    [Tooltip("How close (meters) the eraser must be to count as rubbing")]
    public float touchDistance = 0.04f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    [Tooltip("Plays once when fully clean")]
    public AudioClip cleanSound;
    [Tooltip("Optional: loops while rubbing (e.g. a scrubbing sound)")]
    public AudioClip rubbingSound;

    [Header("State")]
    public bool isClean = false;
    public bool isInserted = false;
    [Range(0f, 1f)] public float cleanProgress = 0f;

    [Header("Eraser Detection")]
    public string eraserTag = "Eraser";

    [Header("Checklist Integration - Cleaning")]
    public SequentialChecklist checklist;
    public int groupIndex = 1;
    public int objectiveIndex = 1;

    [Header("Checklist Integration - First Pickup")]
    [Tooltip("Completed the first time the player grabs this RAM. Set pickupGroupIndex to -1 to disable.")]
    public int pickupGroupIndex = 1;
    public int pickupObjectiveIndex = 0;

    private bool hasBeenPickedUp = false;
    private float lastTouchTime = -999f;
    private Material liveMat;            // our own copy, blended dirty -> clean
    private bool rubbingLoop = false;
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    private void Start()
    {
        isClean = false;
        cleanProgress = 0f;
        if (ramRenderer != null && dirtyMat != null)
        {
            liveMat = new Material(dirtyMat);
            ramRenderer.material = liveMat;
        }
        ApplyLook();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hasBeenPickedUp) return;
        hasBeenPickedUp = true;

        if (checklist != null && pickupGroupIndex >= 0 &&
            checklist.IsCurrentStep(pickupGroupIndex, pickupObjectiveIndex))
        {
            checklist.CompleteObjective(pickupGroupIndex, pickupObjectiveIndex);
        }
    }

    // Physics touches (when they fire)
    private void OnCollisionStay(Collision collision) { Touch(collision.collider); }
    private void OnTriggerStay(Collider other) { Touch(other); }

    private void Touch(Collider other)
    {
        if (other.CompareTag(eraserTag) ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(eraserTag)))
            lastTouchTime = Time.time;
    }

    private void Update()
    {
        if (isClean) { StopRubLoop(); return; }

        // Distance check - works even when both objects are held (no physics events)
        CheckNearbyEraser();

        bool rubbing = Time.time - lastTouchTime < 0.1f;
        if (rubbing)
        {
            cleanProgress += Time.deltaTime / Mathf.Max(0.1f, cleanTimeNeeded);
            StartRubLoop();
            ApplyLook();
            if (cleanProgress >= 1f) FinishCleaning();
        }
        else StopRubLoop();
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

    private void FinishCleaning()
    {
        cleanProgress = 1f;
        isClean = true;
        StopRubLoop();
        ApplyLook();
        if (ramRenderer != null && cleanMat != null) ramRenderer.material = cleanMat;

        if (audioSource != null && cleanSound != null)
            audioSource.PlayOneShot(cleanSound);

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    // Blends the dirty material toward the clean one by cleanProgress
    private void ApplyLook()
    {
        if (ramRenderer == null) return;
        if (liveMat != null && dirtyMat != null && cleanMat != null)
            liveMat.Lerp(dirtyMat, cleanMat, cleanProgress);
        else
            ramRenderer.material = cleanProgress >= 1f ? cleanMat : dirtyMat;
    }

    private void StartRubLoop()
    {
        if (rubbingLoop || audioSource == null || rubbingSound == null) return;
        audioSource.clip = rubbingSound;
        audioSource.loop = true;
        audioSource.Play();
        rubbingLoop = true;
    }

    private void StopRubLoop()
    {
        if (!rubbingLoop || audioSource == null) return;
        audioSource.Stop();
        audioSource.loop = false;
        rubbingLoop = false;
    }
}