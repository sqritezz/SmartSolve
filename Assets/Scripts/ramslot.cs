using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnap : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButton powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    [Header("Randomized Slot Support")]
    [Tooltip("Set automatically by RamSlotRandomizer if used.")]
    public bool isActiveSlot = true;
    [Tooltip("Optional: played when RAM is placed in a WRONG slot")]
    public AudioSource wrongSlotSound;
    [Tooltip("Optional: counts a wrong-slot attempt toward the star rating")]
    public PerformanceTracker performanceTracker;

    private GameObject currentRam;
    private bool warnedThisOverlap = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RAM") || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        if (!isActiveSlot)
        {
            // Wrong slot for this playthrough -- don't snap, just flag the mistake
            if (wrongSlotSound != null)
                wrongSlotSound.Play();
            if (performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
            return;
        }

        currentRam = other.gameObject;

        // Capture the RAM's current visual (world) size BEFORE reparenting,
        // so whatever size you already tuned it to stays exactly the same
        // after it becomes a child of the Snap Point.
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

        // Solve for whatever local scale reproduces that same world size
        // under this Snap Point's own scale.
        Vector3 parentLossy = snapPoint.lossyScale;
        other.transform.localScale = new Vector3(
            parentLossy.x != 0f ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
            parentLossy.y != 0f ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
            parentLossy.z != 0f ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
        );

        if (powerButton != null)
            powerButton.isRamFixed = true;

        RamGrab ramGrab = other.GetComponent<RamGrab>();
        if (ramGrab != null)
        {
            ramGrab.MarkFixedForever();
        }

        if (clickSound != null)
            clickSound.Play();

        // Mark this objective complete, only if it's currently the active pending one
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

        if (!isActiveSlot)
        {
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

        OnTriggerEnter(other); // reuse the exact same snap logic
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            currentRam = null;
            warnedThisOverlap = false;

            RamGrab ramGrab = other.GetComponent<RamGrab>();
            if (ramGrab == null && powerButton != null)
            {
                powerButton.isRamFixed = false;
            }
        }
    }
}