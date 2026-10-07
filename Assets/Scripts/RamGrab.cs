using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// On the RAM (needs Rigidbody + XR Grab Interactable).
// When the player lets go and it did NOT snap into the correct slot
// (wrong slot, or dropped anywhere), the RAM no longer falls.
// It waits a moment (so the wrong-slot sound can play), then glides back
// to where it was picked up from - or to the Return Spot, if one is set.
public class RamGrab : MonoBehaviour
{
    public PowerButton powerButton;

    [Header("Return When Not Snapped")]
    [Tooltip("Optional: a fixed spot to return to (e.g. on the table). Empty = back to where it was picked up.")]
    public Transform returnSpot;
    [Tooltip("Seconds to wait after letting go before returning (lets the slot check and the wrong sound play)")]
    public float waitBeforeReturn = 0.35f;
    [Tooltip("Seconds the glide back takes")]
    public float returnDuration = 0.35f;
    public AudioSource returnSound;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool fixedForever = false;
    private Coroutine returnRoutine;

    // Where it was when picked up
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
        // Stop a return that's in progress if they grab it again
        if (returnRoutine != null) { StopCoroutine(returnRoutine); returnRoutine = null; }
        else if (!fixedForever) RememberHome(); // remember where it was taken from

        transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.useGravity = false;

        if (powerButton != null && !fixedForever)
            powerButton.isRamFixed = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (fixedForever) return; // correctly snapped - RamSnap owns it now

        // Hold it still in place (no falling) while the slots check it
        StopMotion();
        returnRoutine = StartCoroutine(ReturnIfNotSnapped());
    }

    private IEnumerator ReturnIfNotSnapped()
    {
        float t = 0f;
        while (t < waitBeforeReturn)
        {
            // XR Grab may restore physics on release - keep it frozen
            StopMotion();
            if (fixedForever) { returnRoutine = null; yield break; }
            t += Time.deltaTime;
            yield return null;
        }

        if (fixedForever) { returnRoutine = null; yield break; }

        // Glide back
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        if (returnSound != null) returnSound.Play();

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

        // Land exactly at home
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
        returnRoutine = null;
    }

    private void GetHomePose(out Vector3 pos, out Quaternion rot)
    {
        if (returnSpot != null)
        {
            pos = returnSpot.position;
            rot = returnSpot.rotation;
            return;
        }

        if (homeParent != null)
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

        if (returnRoutine != null) { StopCoroutine(returnRoutine); returnRoutine = null; }

        if (powerButton != null)
            powerButton.isRamFixed = true;
    }
}