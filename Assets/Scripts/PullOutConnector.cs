using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the 24-pin connector (needs Rigidbody + XR Grab Interactable + Collider).
// Starts plugged into the motherboard. Grab it and let go:
//  - released near the socket  -> plugs back in (seated)
//  - released anywhere else    -> rests at the "Unplugged Spot" (no falling)
// Grabbing it while the PSU is ON is a safety mistake: it's dropped and counts
// as a wrong attempt. It also can't be moved while the paperclip is inserted.
public class PullOutConnector : MonoBehaviour
{
    [Header("Positions")]
    [Tooltip("Empty object where the connector rests once pulled out (just in front of the socket)")]
    public Transform unpluggedSpot;
    [Tooltip("How close (meters) to the socket it must be released to plug back in")]
    public float snapDistance = 0.08f;

    [Header("Safety")]
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;
    [Tooltip("e.g. \"Turn off the PSU switch before touching the cables!\"")]
    public GameObject powerOnWarning;

    [Header("Paperclip")]
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

    // XR Grab restores the Rigidbody/parent on release, so wait a frame
    private IEnumerator SettleNextFrame()
    {
        yield return null;

        Vector3 seatedWorld = originalParent != null
            ? originalParent.TransformPoint(seatedLocalPos)
            : seatedLocalPos;

        if (Vector3.Distance(transform.position, seatedWorld) <= snapDistance)
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

    private void PlaceSeated()
    {
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetParent(originalParent, false);
        transform.localPosition = seatedLocalPos;
        transform.localRotation = seatedLocalRot;
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
