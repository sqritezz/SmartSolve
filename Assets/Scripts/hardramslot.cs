using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardRamSlot : MonoBehaviour
{
    public Transform snapPoint;
    public HardPCManager hardPCManager;
    public AudioSource clickSound;

    [Header("Checklist Integration - Broken RAM placed")]
    public SequentialChecklist checklist;
    public int brokenRamGroupIndex = 1;
    public int brokenRamObjectiveIndex = 2;

    [Header("Checklist Integration - Working RAM placed")]
    public int workingRamGroupIndex = 2;
    public int workingRamObjectiveIndex = 2;

    [Header("Randomized Slot Support")]
    public bool isActiveSlot = true;
    public AudioSource wrongSlotSound;
    public PerformanceTracker performanceTracker;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        TrySnap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySnap(other);
    }

    void TrySnap(Collider other)
    {
        if (currentRam != null) return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) return;

        if (grab.isSelected) return;

        GameObject ramObject = grab.gameObject;

        if (ramObject.CompareTag("RAM"))
        {
            SnapRam(ramObject);
            hardPCManager.BrokenRamInserted();

            if (checklist != null && checklist.IsCurrentStep(brokenRamGroupIndex, brokenRamObjectiveIndex))
                checklist.CompleteObjective(brokenRamGroupIndex, brokenRamObjectiveIndex);
        }
        else if (ramObject.CompareTag("WorkingRAM"))
        {
            if (!isActiveSlot)
            {
                if (wrongSlotSound != null)
                    wrongSlotSound.Play();
                if (performanceTracker != null)
                    performanceTracker.RegisterWrongAttempt();
                return;
            }

            SnapRam(ramObject);
            hardPCManager.WorkingRamInserted();

            if (checklist != null && checklist.IsCurrentStep(workingRamGroupIndex, workingRamObjectiveIndex))
                checklist.CompleteObjective(workingRamGroupIndex, workingRamObjectiveIndex);
        }
    }

    void SnapRam(GameObject ram)
    {
        currentRam = ram;

        Rigidbody rb = ram.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ram.transform.SetParent(snapPoint, false);
        ram.transform.localPosition = Vector3.zero;
        ram.transform.localRotation = Quaternion.identity;
        ram.transform.localScale = Vector3.one;

        if (clickSound != null)
            clickSound.Play();

        Debug.Log("RAM SNAPPED: " + ram.name);
    }

    // Only clears this slot if it's actually holding the given RAM object.
    // Safe to call on every slot without knowing which one currently holds it.
    public void ClearSlotIfHolding(GameObject ram)
    {
        if (currentRam == ram)
            currentRam = null;
    }

    public void ClearSlot()
    {
        currentRam = null;
    }
}