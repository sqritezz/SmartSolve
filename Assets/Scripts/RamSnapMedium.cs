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
    private bool warnedThisOverlap = false;

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
            if (!warnedThisOverlap)
            {
                if (dirtyRejectSound != null)
                    dirtyRejectSound.Play();
                if (performanceTracker != null)
                    performanceTracker.RegisterWrongAttempt();
                warnedThisOverlap = true;
            }
            return;
        }

        if (!isActiveSlot)
        {
            // Wrong slot for this playthrough
            if (!warnedThisOverlap)
            {
                if (wrongSlotSound != null)
                    wrongSlotSound.Play();
                if (performanceTracker != null)
                    performanceTracker.RegisterWrongAttempt();
                warnedThisOverlap = true;
            }
            return;
        }

        currentRam = other.gameObject;

        // Capture the RAM's current visual (world) size BEFORE reparenting,
        // so whatever size it already is stays exactly the same after it
        // becomes a child of the Snap Point (protects against non-uniform
        // scale on the Snap Point's parent chain).
        Vector3 desiredWorldScale = other.transform.lossyScale;

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

        Vector3 parentLossy = snapPoint.lossyScale;
        other.transform.localScale = new Vector3(
            parentLossy.x != 0f ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
            parentLossy.y != 0f ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
            parentLossy.z != 0f ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
        );

        if (powerButton != null)
            powerButton.isRamFixed = true;

        if (ramMedium != null)
            ramMedium.isInserted = true;

        RamGrab ramGrab = other.GetComponent<RamGrab>();
        if (ramGrab != null)
        {
            ramGrab.MarkFixedForever();
        }

        if (clickSound != null)
            clickSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("RAM") || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        OnTriggerEnter(other); // reuse the exact same logic (clean check, active slot check, snap)
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            currentRam = null;
            warnedThisOverlap = false;

            RamMedium ramMedium = other.GetComponent<RamMedium>();
            if (ramMedium != null)
                ramMedium.isInserted = false;

            if (powerButton != null)
                powerButton.isRamFixed = false;
        }
    }
}