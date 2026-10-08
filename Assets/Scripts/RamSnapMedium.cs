using System.Collections.Generic;
using UnityEngine;

// One per MEDIUM RAM slot. Works exactly like Easy's RamSnap now:
// the slot doesn't decide anything by itself - when the player lets go of the RAM,
// RamGrab finds the CLOSEST slot (Easy or Medium) and asks it to snap the RAM
// (correct slot + clean RAM) or to report a mistake (wrong slot / still dirty).
// Wrong or dirty -> the RAM glides back to where it was picked up.
public class RamSnapMedium : MonoBehaviour
{
    // All Medium slots in the scene (RamGrab searches these)
    public static readonly List<RamSnapMedium> AllSlots = new List<RamSnapMedium>();

    public Transform snapPoint;
    public PowerButtonMedium powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 3;
    public int objectiveIndex = 0;

    [Header("Randomized Slot Support")]
    [Tooltip("Set automatically by RamSlotRandomizer if used.")]
    public bool isActiveSlot = true;
    [Tooltip("Played when the RAM is placed in this slot and it's the WRONG one")]
    public AudioSource wrongSlotSound;
    [Tooltip("Counts a mistake toward the star rating")]
    public PerformanceTracker performanceTracker;

    [Header("Requires Clean RAM")]
    [Tooltip("If true, dirty RAM cannot be inserted -- it must be cleaned first")]
    public bool requireClean = true;
    [Tooltip("Played when DIRTY RAM is placed in a slot")]
    public AudioSource dirtyRejectSound;

    public bool IsFilled { get; private set; }

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

    // Is this RAM still dirty (and this slot needs it clean)?
    public bool IsDirty(RamGrab ram)
    {
        if (!requireClean || ram == null) return false;
        RamMedium ramMedium = ram.GetComponent<RamMedium>();
        return ramMedium != null && !ramMedium.isClean;
    }

    // Can this RAM go in right now?
    public bool CanAccept(RamGrab ram)
    {
        return isActiveSlot && !IsFilled && !IsDirty(ram);
    }

    // Mistake: count it (the sound is played by RamGrab on the way back)
    public void RegisterWrong()
    {
        if (performanceTracker != null)
            performanceTracker.RegisterWrongAttempt();
    }

    // Dirty RAM -> dirty sound, otherwise wrong-slot sound
    public void PlayWrongSound(RamGrab ram)
    {
        if (IsDirty(ram) && dirtyRejectSound != null)
            dirtyRejectSound.Play();
        else if (wrongSlotSound != null)
            wrongSlotSound.Play();
    }

    // Correct slot: snap and lock the RAM in
    public void SnapRam(RamGrab ram)
    {
        IsFilled = true;
        Transform t = ram.transform;

        // Keep the RAM's current world size after reparenting
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

        Vector3 p = snapPoint.lossyScale;
        t.localScale = new Vector3(
            p.x != 0f ? desiredWorldScale.x / p.x : desiredWorldScale.x,
            p.y != 0f ? desiredWorldScale.y / p.y : desiredWorldScale.y,
            p.z != 0f ? desiredWorldScale.z / p.z : desiredWorldScale.z);

        if (powerButton != null)
            powerButton.isRamFixed = true;

        RamMedium ramMedium = ram.GetComponent<RamMedium>();
        if (ramMedium != null)
            ramMedium.isInserted = true;

        ram.MarkFixedForever();

        if (clickSound != null)
            clickSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}