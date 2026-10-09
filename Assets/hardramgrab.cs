using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// HARD level - put this on BOTH RAMs (the broken one AND the working one).
// Needs Rigidbody + XR Grab Interactable.
// When the player lets go, the RAM picks the CLOSEST Hard slot it is in:
//  - allowed slot -> snaps in (HardRamSlot handles manager + checklist)
//  - wrong slot   -> pauses, plays the wrong sound ONCE, glides back (NOT counted as a mistake)
//  - no slot      -> glides back quietly
// It never falls or floats.
public class HardRamGrab : MonoBehaviour
{
    [Header("Slot Detection")]
    [Tooltip("How close (meters) to a slot's snap point the RAM must be released to count as 'in' that slot")]
    public float slotRadius = 0.1f;

    [Header("Return When Not Snapped")]
    [Tooltip("Where it goes back to. BROKEN RAM: set a spot on the table (otherwise it would glide back into the slot it came out of). WORKING RAM: leave empty = back to where it was picked up.")]
    public Transform returnSpot;
    [Tooltip("Seconds it stays still before gliding back")]
    public float waitBeforeReturn = 0.35f;
    [Tooltip("Seconds the glide back takes")]
    public float returnDuration = 0.35f;
    public AudioSource returnSound;

    [Header("After Snapping")]
    [Tooltip("Lock it in the slot so it can't be grabbed again (tick for the WORKING RAM, untick for the BROKEN RAM)")]
    public bool lockWhenSnapped = false;

    [Header("Debug")]
    public bool logSlotChoice = false;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Coroutine releaseRoutine;
    private bool locked = false;

    private Transform homeParent;
    private Vector3 homeLocalPos;
    private Quaternion homeLocalRot;
    private Vector3 homeLocalScale;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;

        RememberHome();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void RememberHome()
    {
        homeParent = transform.parent;
        homeLocalPos = transform.localPosition;
        homeLocalRot = transform.localRotation;
        homeLocalScale = transform.localScale;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (locked) return;

        if (releaseRoutine != null)
        {
            // Grabbed again mid-glide: keep the original home
            StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }
        else
        {
            RememberHome();
        }

        // Taken out of a slot -> that slot is empty again
        foreach (HardRamSlot s in HardRamSlot.AllSlots)
            if (s != null) s.ClearSlotIfHolding(gameObject);

        transform.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (locked) return;
        StopMotion();
        releaseRoutine = StartCoroutine(HandleRelease());
    }

    private IEnumerator HandleRelease()
    {
        yield return null; // let XR Grab finish releasing
        StopMotion();

        HardRamSlot slot = FindClosestSlot();

        // Allowed slot -> snap right away
        if (slot != null && slot.CanAccept(gameObject))
        {
            if (logSlotChoice) Debug.Log("[HardRamGrab] Snapped into " + slot.name);
            slot.Accept(gameObject);
            if (lockWhenSnapped) Lock();
            releaseRoutine = null;
            yield break;
        }

        bool wrongSlot = slot != null;
        if (wrongSlot)
        {
            if (logSlotChoice) Debug.Log("[HardRamGrab] Wrong slot: " + slot.name);
            // Not counted as a mistake - it just plays the sound and goes back
        }
        else if (logSlotChoice)
        {
            Debug.Log("[HardRamGrab] Not in any slot");
        }

        // Stay still for a moment
        float t = 0f;
        while (t < waitBeforeReturn)
        {
            StopMotion();
            t += Time.deltaTime;
            yield return null;
        }

        // Wrong sound plays once, as it goes back
        if (wrongSlot) slot.PlayWrongSound();
        else if (returnSound != null) returnSound.Play();

        // Glide back
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float g = 0f;
        while (g < returnDuration)
        {
            g += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, g / returnDuration);
            GetHomePose(out Vector3 targetPos, out Quaternion targetRot);
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, targetPos, k),
                Quaternion.Slerp(startRot, targetRot, k));
            StopMotion();
            yield return null;
        }

        if (returnSpot != null)
        {
            transform.SetPositionAndRotation(returnSpot.position, returnSpot.rotation);
        }
        else
        {
            transform.SetParent(homeParent, true);
            transform.localPosition = homeLocalPos;
            transform.localRotation = homeLocalRot;
            transform.localScale = homeLocalScale;
        }

        StopMotion();
        releaseRoutine = null;
    }

    // The single closest EMPTY slot the RAM is in
    private HardRamSlot FindClosestSlot()
    {
        Vector3 pos = transform.position;
        HardRamSlot best = null;
        float bestDist = float.MaxValue;

        foreach (HardRamSlot s in HardRamSlot.AllSlots)
        {
            if (s == null || !s.isActiveAndEnabled || s.IsFilled) continue;
            if (!s.Contains(pos, slotRadius)) continue;

            float d = s.DistanceTo(pos);
            if (d < bestDist) { bestDist = d; best = s; }
        }

        if (logSlotChoice && best != null)
            Debug.Log("[HardRamGrab] Closest slot: " + best.name + " at " + bestDist.ToString("F3") + " m");

        return best;
    }

    private void GetHomePose(out Vector3 pos, out Quaternion rot)
    {
        if (returnSpot != null)
        {
            pos = returnSpot.position;
            rot = returnSpot.rotation;
        }
        else if (homeParent != null)
        {
            pos = homeParent.TransformPoint(homeLocalPos);
            rot = homeParent.rotation * homeLocalRot;
        }
        else
        {
            pos = homeLocalPos;
            rot = homeLocalRot;
        }
    }

    private void StopMotion()
    {
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void Lock()
    {
        locked = true;
        if (grab != null) grab.enabled = false;
    }
}