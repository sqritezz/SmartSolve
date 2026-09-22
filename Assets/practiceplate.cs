using UnityEngine;

// Attach to practiceplate (the trigger-collider object). Rubbing the
// practice sponge on it swaps its material from dirty to clean, same
// pattern as RamMedium.cs's real cleaning mechanic, but fully isolated
// from the real RAM/checklist logic.
public class PlateClean : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer plateRenderer;
    public Material dirtyMat;
    public Material cleanMat;
    public AudioSource cleanSound;

    [Header("State")]
    public bool isClean = false;

    [Header("Sponge Detection")]
    public string spongeTag = "practicesponge";

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    [Tooltip("Fires once the plate is successfully cleaned -- hook up the follow-up dialogue unlock here.")]
    public UnityEngine.Events.UnityEvent onCleaned;

    private void Start()
    {
        ApplyMaterial();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryClean(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryClean(other);
    }

    private void TryClean(Collider other)
    {
        if (isClean) return;
        if (!other.CompareTag(spongeTag)) return;

        isClean = true;
        ApplyMaterial();

        if (cleanSound != null)
            cleanSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);

        onCleaned?.Invoke();
    }

    private void ApplyMaterial()
    {
        if (plateRenderer == null) return;
        plateRenderer.material = isClean ? cleanMat : dirtyMat;
    }
}