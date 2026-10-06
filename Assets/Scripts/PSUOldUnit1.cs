using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the OLD (faulty) PSU (needs Rigidbody + XR Grab Interactable + Collider).
// It can only be pulled out when:
//   - the PSU switch is OFF
//   - its 24-pin is unplugged from the motherboard
//   - all its screws are removed
// Otherwise it stays in place, shows a message and counts as a wrong attempt.
// Once removed, it drops normally when released (put it on the table/floor).
//
// IMPORTANT: on this PSU's XR Grab Interactable, set the "Colliders" list to
// ONLY the PSU's own collider, so its 24-pin connector (a child) keeps its own grab.
public class PSUOldUnit : MonoBehaviour
{
    [Header("Parts")]
    public PSUSlot psuSlot;
    public PSUSwitch1 psuSwitch;
    [Tooltip("This PSU's 24-pin connector")]
    public PullOutConnector connector24Pin;
    public PSUScrew[] screws;

    [Header("Feedback")]
    public PerformanceTracker performanceTracker;
    public GameObject powerOnWarning;     // "Turn off the PSU switch first!"
    public GameObject unplugCableMessage; // "Unplug the 24-pin first."
    public GameObject removeScrewsMessage;// "Remove all the screws first."
    public float messageShowTime = 2.5f;
    public AudioSource warningSound;
    public AudioSource removeSound;

    [Header("State (read-only)")]
    public bool isRemoved = false;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform startParent;
    private Vector3 startLocalPos;
    private Quaternion startLocalRot;
    private bool forceDropping = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        startParent = transform.parent;
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;

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
        if (unplugCableMessage != null) unplugCableMessage.SetActive(false);
        if (removeScrewsMessage != null) removeScrewsMessage.SetActive(false);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isRemoved) return;

        GameObject blockMessage = null;
        if (psuSwitch != null && !psuSwitch.IsOff) blockMessage = powerOnWarning;
        else if (connector24Pin != null && connector24Pin.isSeated) blockMessage = unplugCableMessage;
        else if (AnyScrewFastened()) blockMessage = removeScrewsMessage;

        if (blockMessage != null || (psuSwitch != null && !psuSwitch.IsOff))
        {
            if (warningSound != null) warningSound.Play();
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            if (blockMessage != null) StartCoroutine(ShowMessage(blockMessage));
            StartCoroutine(ForceDrop());
            return;
        }

        // Allowed - it comes out
        isRemoved = true;
        transform.SetParent(null, true);
        if (psuSlot != null) psuSlot.ClearSlot();
        if (removeSound != null) removeSound.Play();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (forceDropping || !isRemoved) return;
        StartCoroutine(EnableGravityNextFrame());
    }

    private IEnumerator EnableGravityNextFrame()
    {
        yield return null;
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
    }

    private bool AnyScrewFastened()
    {
        if (screws == null) return false;
        foreach (var s in screws)
            if (s != null && s.IsFastened) return true;
        return false;
    }

    private IEnumerator ForceDrop()
    {
        forceDropping = true;
        grab.enabled = false;
        yield return null;
        grab.enabled = true;

        // Snap back exactly where it was
        transform.SetParent(startParent, true);
        transform.localPosition = startLocalPos;
        transform.localRotation = startLocalRot;
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        forceDropping = false;
    }

    private IEnumerator ShowMessage(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }
}
