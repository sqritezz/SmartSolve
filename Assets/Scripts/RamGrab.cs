using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// On the RAM (needs Rigidbody + XR Grab Interactable).
// When the player lets go, the RAM picks the CLOSEST RamSnap slot it is in:
//  - correct slot -> snaps in with a click and is locked
//  - wrong slot   -> pauses, plays the wrong sound ONCE, glides back home
//  - no slot      -> glides back home quietly
// It never falls or floats.
public class RamGrab : MonoBehaviour
{
    public PowerButton powerButton;

    [Header("Slot Detection")]
    [Tooltip("How close (meters) to a slot's snap point the RAM must be released to count as 'in' that slot. Raise if it's too strict.")]
    public float slotRadius = 0.1f;

    [Header("Return When Not Snapped")]
    [Tooltip("Optional: a fixed spot to return to (e.g. on the table). Empty = back to where it was picked up.")]
    public Transform returnSpot;
    [Tooltip("Seconds it stays still before gliding back")]
    public float waitBeforeReturn = 0.35f;
    [Tooltip("Seconds the glide back takes")]
    public float returnDuration = 0.35f;
    public AudioSource returnSound;

    [Header("Debug")]
    [Tooltip("Prints which slot was chosen and how far away it was")]
    public bool logSlotChoice = false;

    public bool IsFixed => fixedForever;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool fixedForever = false;
    private Coroutine releaseRoutine;

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
        if (fixedForever) return;

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

        transform.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = false;

        if (powerButton != null)
            powerButton.isRamFixed = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (fixedForever) return;
        StopMotion();
        releaseRoutine = StartCoroutine(HandleRelease());
    }

    private IEnumerator HandleRelease()
    {
        yield return null; // let XR Grab finish releasing
        StopMotion();

        RamSnap slot = FindClosestSlot();

        // Correct slot -> snap right away
        if (slot != null && slot.isActiveSlot && !slot.IsFilled)
        {
            if (logSlotChoice) Debug.Log("[RamGrab] Snapped into " + slot.name);
            slot.SnapRam(this);
            releaseRoutine = null;
            yield break;
        }

        bool wrongSlot = slot != null;
        if (wrongSlot)
        {
            if (logSlotChoice) Debug.Log("[RamGrab] Wrong slot: " + slot.name);
            slot.RegisterWrong();
        }
        else if (logSlotChoice)
        {
            Debug.Log("[RamGrab] Not in any slot");
        }

        // Stay still for a moment (another slot type, e.g. Medium's, may still snap it)
        float t = 0f;
        while (t < waitBeforeReturn)
        {
            if (fixedForever) { releaseRoutine = null; yield break; }
            StopMotion();
            t += Time.deltaTime;
            yield return null;
        }
        if (fixedForever) { releaseRoutine = null; yield break; }

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

    // The single closest slot the RAM is in - overlapping slots can't both claim it
    private RamSnap FindClosestSlot()
    {
        Vector3 pos = transform.position;
        RamSnap best = null;
        float bestDist = float.MaxValue;

        foreach (RamSnap s in RamSnap.AllSlots)
        {
            if (s == null || !s.isActiveAndEnabled) continue;
            if (!s.Contains(pos, slotRadius)) continue;

            float d = s.DistanceTo(pos);
            if (d < bestDist) { bestDist = d; best = s; }
        }

        if (logSlotChoice && best != null)
            Debug.Log("[RamGrab] Closest slot: " + best.name + " at " + bestDist.ToString("F3") + " m");

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

    public void MarkFixedForever()
    {
        fixedForever = true;

        if (releaseRoutine != null) { StopCoroutine(releaseRoutine); releaseRoutine = null; }

        // Lock it in so it can't be pulled out and left floating
        if (grab != null) grab.enabled = false;

        if (powerButton != null)
            powerButton.isRamFixed = true;
    }
}