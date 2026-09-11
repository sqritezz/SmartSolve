using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnapMedium : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButtonMedium powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 3;
    public int objectiveIndex = 0;

    [Header("Randomized Slot Support")]
    public bool isActiveSlot = true;
    public AudioSource wrongSlotSound;
    public PerformanceTracker performanceTracker;

    [Header("Requires Clean RAM")]
    [Tooltip("If true, dirty RAM cannot be inserted -- it must be cleaned first")]
    public bool requireClean = true;
    public AudioSource dirtyRejectSound;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RAM") || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        RamMedium ramMedium = other.GetComponent<RamMedium>();

        // Block insertion if RAM isn't clean yet
        if (requireClean && ramMedium != null && !ramMedium.isClean)
        {
            if (dirtyRejectSound != null)
                dirtyRejectSound.Play();
            if (performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
            return;
        }

        if (!isActiveSlot)
        {
            // Wrong slot for this playthrough
            if (wrongSlotSound != null)
                wrongSlotSound.Play();
            if (performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
            return;
        }

        currentRam = other.gameObject;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        other.transform.SetParent(snapPoint, true);
        other.transform.localPosition = Vector3.zero;
        other.transform.localRotation = Quaternion.identity;
        other.transform.localScale = Vector3.one;

        if (powerButton != null)
            powerButton.isRamFixed = true;

        if (ramMedium != null)
            ramMedium.isInserted = true;

        if (clickSound != null)
            clickSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            currentRam = null;

            RamMedium ramMedium = other.GetComponent<RamMedium>();
            if (ramMedium != null)
                ramMedium.isInserted = false;

            if (powerButton != null)
                powerButton.isRamFixed = false;
        }
    }
}