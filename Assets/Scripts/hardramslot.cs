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
        Transform t = ram.transform;

        // Keep the RAM's current world size, the SAME way RamSnapMedium does it.
        // We capture it BEFORE touching parent/rotation, because once we force
        // localRotation to identity below, whatever scale SetParent(true) auto-
        // computed (which was based on the RAM's OLD rotation) stops matching -
        // that mismatch is what was causing the stretched/sheared look.
        Vector3 desiredWorldScale = t.lossyScale;

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

        t.SetParent(snapPoint, true);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        // Now that rotation is locked to identity, recompute local scale directly
        // from the world size we want vs. the snap point's world scale. This is
        // a plain per-axis division (no rotation/shear involved), so it always
        // lands on the correct visual size regardless of what the snap point's
        // own scale looks like.
        Vector3 p = snapPoint.lossyScale;
        t.localScale = new Vector3(
            p.x != 0f ? desiredWorldScale.x / p.x : desiredWorldScale.x,
            p.y != 0f ? desiredWorldScale.y / p.y : desiredWorldScale.y,
            p.z != 0f ? desiredWorldScale.z / p.z : desiredWorldScale.z);

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