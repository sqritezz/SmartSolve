using UnityEngine;

// Attach to practiceplate (the trigger-collider object). Rubbing the
// practice sponge on it cleans it GRADUALLY, same as the real RAM
// (RamMedium.cs): keep rubbing for "Clean Time Needed" seconds and the
// dirt fades away. Stopping halfway keeps the progress.
// Fully isolated from the real RAM/checklist logic.
public class PlateClean : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer plateRenderer;
    public Material dirtyMat;
    public Material cleanMat;
    [Tooltip("Plays once when fully clean")]
    public AudioSource cleanSound;
    [Tooltip("Optional: loops while scrubbing")]
    public AudioSource rubbingSound;

    [Header("Cleaning speed")]
    [Tooltip("Seconds of scrubbing needed to fully clean the plate")]
    public float cleanTimeNeeded = 3f;
    [Tooltip("How close (meters) the sponge must be to count as scrubbing")]
    public float touchDistance = 0.04f;

    [Header("State")]
    public bool isClean = false;
    [Range(0f, 1f)] public float cleanProgress = 0f;

    [Header("Sponge Detection")]
    public string spongeTag = "practicesponge";

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    [Tooltip("Fires once the plate is successfully cleaned -- hook up the follow-up dialogue unlock here.")]
    public UnityEngine.Events.UnityEvent onCleaned;

    private float lastTouchTime = -999f;
    private Material liveMat;
    private readonly Collider[] hits = new Collider[16];

    private void Start()
    {
        isClean = false;
        cleanProgress = 0f;
        if (plateRenderer != null && dirtyMat != null)
        {
            liveMat = new Material(dirtyMat);
            plateRenderer.material = liveMat;
        }
        ApplyLook();
    }

    private void OnTriggerStay(Collider other) { Touch(other); }
    private void OnCollisionStay(Collision collision) { Touch(collision.collider); }

    private void Touch(Collider other)
    {
        if (other.CompareTag(spongeTag) ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(spongeTag)))
            lastTouchTime = Time.time;
    }

    private void Update()
    {
        if (isClean) { StopRub(); return; }

        CheckNearbySponge();

        bool rubbing = Time.time - lastTouchTime < 0.1f;
        if (rubbing)
        {
            cleanProgress += Time.deltaTime / Mathf.Max(0.1f, cleanTimeNeeded);
            if (rubbingSound != null && !rubbingSound.isPlaying) { rubbingSound.loop = true; rubbingSound.Play(); }
            ApplyLook();
            if (cleanProgress >= 1f) Finish();
        }
        else StopRub();
    }

    private void CheckNearbySponge()
    {
        if (plateRenderer == null) return;
        Bounds b = plateRenderer.bounds;
        b.Expand(touchDistance * 2f);
        int n = Physics.OverlapBoxNonAlloc(b.center, b.extents, hits, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (hits[i] != null) Touch(hits[i]);
    }

    private void Finish()
    {
        cleanProgress = 1f;
        isClean = true;
        StopRub();
        ApplyLook();
        if (plateRenderer != null && cleanMat != null) plateRenderer.material = cleanMat;

        if (cleanSound != null) cleanSound.Play();

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
        if (plateRenderer == null) return;
        if (liveMat != null && dirtyMat != null && cleanMat != null)
            liveMat.Lerp(dirtyMat, cleanMat, cleanProgress);
        else
            plateRenderer.material = cleanProgress >= 1f ? cleanMat : dirtyMat;
    }
}