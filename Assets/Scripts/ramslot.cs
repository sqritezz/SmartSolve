using System.Collections.Generic;
using UnityEngine;

// One per RAM slot. The slot no longer decides anything by itself -
// when the player lets go of the RAM, RamGrab finds the CLOSEST slot and asks it
// to snap (correct slot) or to report a mistake (wrong slot).
// This stops overlapping slots from fighting over the RAM.
public class RamSnap : MonoBehaviour
{
    // All slots in the scene (RamGrab searches these)
    public static readonly List<RamSnap> AllSlots = new List<RamSnap>();

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
    [Tooltip("Played when the RAM is placed in this slot and it's the WRONG one")]
    public AudioSource wrongSlotSound;
    [Tooltip("Counts a wrong-slot attempt toward the star rating")]
    public PerformanceTracker performanceTracker;

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

    // Wrong slot: count the mistake (the sound is played by RamGrab on the way back)
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

        ram.MarkFixedForever();

        if (clickSound != null)
            clickSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}