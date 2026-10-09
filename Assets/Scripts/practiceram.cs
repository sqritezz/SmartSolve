using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to each practice RAM stick.
//  - Marks whether this copy is the "faulty" or "good" one for the swap tutorial.
//  - When the player lets go and it did NOT snap into the slot (faulty RAM
//    rejected, or dropped anywhere), it glides back to the spot where it was
//    first picked up. It never falls or floats.
public class PracticeRamPart : MonoBehaviour
{
    public bool isFaulty = false;

    [Header("Return When Not Snapped")]
    [Tooltip("Optional fixed spot to return to. Empty = where it was first picked up.")]
    public Transform returnSpot;
    [Tooltip("Seconds to wait after letting go (gives the slot time to snap it)")]
    public float waitBeforeReturn = 0.35f;
    [Tooltip("Seconds the glide back takes")]
    public float returnDuration = 0.35f;
    public AudioSource returnSound;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Coroutine returnRoutine;

    private bool hasHome = false;
    private Transform homeParent;
    private Vector3 homeLocalPos;
    private Quaternion homeLocalRot;
    private Vector3 homeLocalScale;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Grabbed again mid-glide: just stop the glide
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        // Remember the spot of the FIRST pickup only
        if (!hasHome)
        {
            hasHome = true;
            homeParent = transform.parent;
            homeLocalPos = transform.localPosition;
            homeLocalRot = transform.localRotation;
            homeLocalScale = transform.localScale;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (!hasHome && returnSpot == null) return;
        if (returnRoutine != null) StopCoroutine(returnRoutine);
        returnRoutine = StartCoroutine(ReturnIfNotSnapped());
    }

    private IEnumerator ReturnIfNotSnapped()
    {
        Transform parentAtRelease = transform.parent;

        // Give the slot a moment to snap it
        float t = 0f;
        while (t < waitBeforeReturn)
        {
            if (Snapped(parentAtRelease)) { returnRoutine = null; yield break; }
            HoldStill();
            t += Time.deltaTime;
            yield return null;
        }
        if (Snapped(parentAtRelease)) { returnRoutine = null; yield break; }

        if (returnSound != null) returnSound.Play();

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
            HoldStill();
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

        HoldStill();
        returnRoutine = null;
    }

    // The slot snaps the good RAM by making it a child of its Snap Point
    private bool Snapped(Transform parentAtRelease)
    {
        return transform.parent != parentAtRelease;
    }

    // Stops it from falling or drifting while it waits / glides
    private void HoldStill()
    {
        if (rb == null) return;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
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
}