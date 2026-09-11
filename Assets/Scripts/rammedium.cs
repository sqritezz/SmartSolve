using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamMedium : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer ramRenderer;
    public Material dirtyMat;
    public Material cleanMat;
    public AudioSource cleanSound;

    [Header("State")]
    public bool isClean = false;
    public bool isInserted = false;

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

    private void Awake()
    {
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    private void Start()
    {
        isClean = false;
        ApplyMaterial();
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

    private void OnCollisionEnter(Collision collision)
    {
        TryClean(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryClean(other);
    }

    private void TryClean(Collider other)
    {
        if (isClean) return;
        if (!other.CompareTag(eraserTag)) return;

        isClean = true;
        ApplyMaterial();

        if (cleanSound != null)
            cleanSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void ApplyMaterial()
    {
        if (ramRenderer == null) return;
        ramRenderer.material = isClean ? cleanMat : dirtyMat;
    }
}