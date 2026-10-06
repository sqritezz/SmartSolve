using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to a 24-pin connector (needs Rigidbody + XR Grab Interactable + Collider).
// Grab it and let go:
//  - released near the motherboard socket -> plugs in (seated)
//  - released anywhere else               -> rests at the "Unplugged Spot" (no falling)
// Grabbing it while the PSU is ON is a safety mistake: dropped + wrong attempt.
// It also can't be moved while a paperclip is inserted.
//
// PSU Medium: leave Seated Spot empty and Start Unplugged off (works as before).
// PSU Hard (new PSU's connector): set Seated Spot to an empty object at the
// motherboard socket and check Start Unplugged. PSUHardManager turns
// "Allow Plug In" on once the new PSU is installed.
public class PullOutConnector : MonoBehaviour
{
    [Header("Positions")]
    [Tooltip("Empty object where the connector rests when unplugged")]
    public Transform unpluggedSpot;
    [Tooltip("Optional: empty object at the motherboard socket. Empty = where the connector starts.")]
    public Transform seatedSpot;
    [Tooltip("Starts unplugged (e.g. the new PSU's connector)")]
    public bool startUnplugged = false;
    [Tooltip("How close (meters) to the socket it must be released to plug in")]
    public float snapDistance = 0.08f;
    [Tooltip("Turned on/off by a manager. When off, it can't be plugged in.")]
    public bool allowPlugIn = true;

    [Header("Safety")]
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;
    [Tooltip("e.g. \"Turn off the PSU switch before touching the cables!\"")]
    public GameObject powerOnWarning;

    [Header("Paperclip (PSU Medium only)")]
    public PaperclipSlot paperclipSlot;
    [Tooltip("e.g. \"Remove the paperclip first.\"")]
    public GameObject removeClipMessage;

    [Header("Audio")]
    public AudioSource unplugSound;
    public AudioSource plugSound;
    public AudioSource warningSound;

    [Header("Messages")]
    public float messageShowTime = 2.5f;

    [Header("State (read-only)")]
    public bool isSeated = true;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 seatedLocalPos;
    private Quaternion seatedLocalRot;
    private bool forceDropping = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        originalParent = transform.parent;
        seatedLocalPos = transform.localPosition;
        seatedLocalRot = transform.localRotation;

        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    private void Start()
    {
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
        if (removeClipMessage != null) removeClipMessage.SetActive(false);

        if (startUnplugged)
        {
            isSeated = false;
            PlaceUnplugged();
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (psuSwitch != null && !psuSwitch.IsOff)
        {
            if (warningSound != null) warningSound.Play();
            ShowMessage(powerOnWarning);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            StartCoroutine(ForceDrop());
            return;
        }

        if (paperclipSlot != null && paperclipSlot.hasClip)
        {
            ShowMessage(removeClipMessage);
            StartCoroutine(ForceDrop());
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (forceDropping) return;
        StartCoroutine(SettleNextFrame());
    }

    private IEnumerator SettleNextFrame()
    {
        yield return null; // let XR Grab finish releasing

        GetSeatedPose(out Vector3 seatedPos, out _);

        if (allowPlugIn && Vector3.Distance(transform.position, seatedPos) <= snapDistance)
        {
            PlaceSeated();
            if (!isSeated) { isSeated = true; if (plugSound != null) plugSound.Play(); }
        }
        else
        {
            PlaceUnplugged();
            if (isSeated) { isSeated = false; if (unplugSound != null) unplugSound.Play(); }
        }
    }

    private IEnumerator ForceDrop()
    {
        forceDropping = true;
        grab.enabled = false;   // disabling makes XR let go
        yield return null;
        grab.enabled = true;
        if (isSeated) PlaceSeated(); else PlaceUnplugged();
        forceDropping = false;
    }

    private void GetSeatedPose(out Vector3 pos, out Quaternion rot)
    {
        if (seatedSpot != null)
        {
            pos = seatedSpot.position;
            rot = seatedSpot.rotation;
        }
        else if (originalParent != null)
        {
            pos = originalParent.TransformPoint(seatedLocalPos);
            rot = originalParent.rotation * seatedLocalRot;
        }
        else
        {
            pos = seatedLocalPos;
            rot = seatedLocalRot;
        }
    }

    private void PlaceSeated()
    {
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetParent(originalParent, true);
        GetSeatedPose(out Vector3 pos, out Quaternion rot);
        transform.SetPositionAndRotation(pos, rot);
    }

    private void PlaceUnplugged()
    {
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetParent(originalParent, true);
        if (unpluggedSpot != null)
            transform.SetPositionAndRotation(unpluggedSpot.position, unpluggedSpot.rotation);
    }

    private void ShowMessage(GameObject message)
    {
        if (message != null) StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }
}