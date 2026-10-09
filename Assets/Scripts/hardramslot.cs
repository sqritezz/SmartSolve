using System.Collections.Generic;
using UnityEngine;

// One per HARD RAM slot. Works like Easy/Medium now: the slot doesn't decide
// anything by itself - when the player lets go of a RAM, HardRamGrab finds the
// CLOSEST slot and asks it what to do:
//   - Broken RAM (tag "RAM")         -> goes into any empty slot
//   - Working RAM (tag "WorkingRAM") -> only into the active slot, otherwise
//                                       it's a mistake and the RAM glides back
public class HardRamSlot : MonoBehaviour
{
    // All Hard slots in the scene (HardRamGrab searches these)
    public static readonly List<HardRamSlot> AllSlots = new List<HardRamSlot>();

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

    public bool IsFilled => currentRam != null;

    private GameObject currentRam;
    private Collider slotCollider;

    private void Awake()
    {
        if (snapPoint == null) snapPoint = transform;
        slotCollider = GetComponent<Collider>();
    }

    private void OnEnable() { if (!AllSlots.Contains(this)) AllSlots.Add(this); }
    private void OnDisable() { AllSlots.Remove(this); }

    // Is the RAM (at this world position) inside or near this slot?
    public bool Contains(Vector3 ramPos, float extraRadius)
    {
        if (slotCollider != null && slotCollider.bounds.Contains(ramPos)) return true;
        return Vector3.Distance(ramPos, snapPoint.position) <= extraRadius;
    }

    public float DistanceTo(Vector3 ramPos)
    {
        return Vector3.Distance(ramPos, snapPoint.position);
    }

    // Can this RAM go in here right now?
    public bool CanAccept(GameObject ram)
    {
        if (IsFilled || ram == null) return false;
        if (ram.CompareTag("RAM")) return true;                 // broken RAM: any empty slot
        if (ram.CompareTag("WorkingRAM")) return isActiveSlot;  // working RAM: only the right slot
        return false;
    }

    // Wrong slot: count the mistake (the sound is played by HardRamGrab on the way back)
    public void RegisterWrong()
    {
        if (performanceTracker != null)
            performanceTracker.RegisterWrongAttempt();
    }

    public void PlayWrongSound()
    {
        if (wrongSlotSound != null)
            wrongSlotSound.Play();
    }

    // Correct: snap it in and report to the manager + checklist
    public void Accept(GameObject ram)
    {
        bool isBroken = ram.CompareTag("RAM");
        SnapRam(ram);

        if (isBroken)
        {
            if (hardPCManager != null) hardPCManager.BrokenRamInserted();
            if (checklist != null && checklist.IsCurrentStep(brokenRamGroupIndex, brokenRamObjectiveIndex))
                checklist.CompleteObjective(brokenRamGroupIndex, brokenRamObjectiveIndex);
        }
        else
        {
            if (hardPCManager != null) hardPCManager.WorkingRamInserted();
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
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
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