using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to a 24-pin connector (needs Rigidbody + XR Grab Interactable + Box Collider).
// Grab it and let go:
//  - released near the motherboard socket -> plugs in (seated)
//  - released anywhere else               -> rests at the "Unplugged Spot" (no falling)
// Grabbing it while the PSU is ON is a safety mistake: dropped + wrong attempt.
// It also can't be moved while a paperclip is inserted.
//
// PSU Medium: leave Seated Spot empty and Start Unplugged off.
// PSU Hard (new PSU's connector): set Seated Spot to an empty object at the
// motherboard socket and check Start Unplugged. PSUHardManager turns
// "Allow Plug In" on once the new PSU is installed.
//
// Models whose pivot is far away from the mesh (e.g. a part cut out of a whole
// PC model) are handled: the hand holds the CENTER of the collider, and the
// plug-in check uses the collider center too.
//
// Audio: ONE Audio Source (leave empty = uses the one on this object),
// and the sounds are clips you drag in.
public class PullOutConnector : MonoBehaviour
{
    [Header("Positions")]
    [Tooltip("Empty object where the connector rests when unplugged. NOT the socket - put it beside the case.")]
    public Transform unpluggedSpot;
    [Tooltip("Optional: empty object at the motherboard socket. Empty = where the connector starts.")]
    public Transform seatedSpot;
    [Tooltip("Optional: the point you must touch to plug in (e.g. the old connector's 24Pin Seated Spot). Empty = uses Seated Spot.")]
    public Transform detectSpot;
    [Tooltip("Starts unplugged (e.g. the new PSU's connector)")]
    public bool startUnplugged = false;
    [Tooltip("How close (meters) to the socket it must be released to plug in")]
    public float snapDistance = 0.08f;
    [Tooltip("Turned on/off by a manager. When off, it can't be plugged in.")]
    public bool allowPlugIn = true;

    [Header("Grab Fix")]
    [Tooltip("Hand holds the middle of the connector instead of the model's far-away pivot. Only used if the XR Grab Interactable has no Attach Transform.")]
    public bool autoGrabPoint = true;
    [Tooltip("ON = the Unplugged Spot marks where the MIDDLE of the connector rests. Turn on if the connector ends up floating away from the Unplugged Spot.")]
    public bool placeByCenter = true;

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
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip unplugSound;
    public AudioClip plugSound;
    public AudioClip warningSound;

    [Header("Messages")]
    public float messageShowTime = 2.5f;

    [Header("State (read-only)")]
    public bool isSeated = true;
    [Tooltip("While holding it: how far (meters) the connector is from the socket. Must go below Snap Distance.")]
    public float distanceToSocket = -1f;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Transform originalParent;
    private Vector3 seatedLocalPos;
    private Quaternion seatedLocalRot;
    private Vector3 centerLocal;          // middle of the collider, in this object's local space
    private bool forceDropping = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        originalParent = transform.parent;
        seatedLocalPos = transform.localPosition;
        seatedLocalRot = transform.localRotation;

        // Find the real middle of the connector
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box) centerLocal = box.center;
        else if (col != null) centerLocal = transform.InverseTransformPoint(col.bounds.center);
        else centerLocal = Vector3.zero;

        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (grab != null)
        {
            // Hand holds the middle of the connector, not the far-away pivot
            if (autoGrabPoint && grab.attachTransform == null)
            {
                Transform gp = new GameObject("Auto Grab Point").transform;
                gp.SetParent(transform, false);
                gp.localPosition = centerLocal;
                gp.localRotation = Quaternion.identity;
                grab.attachTransform = gp;
            }

            // Kinematic objects can't be thrown (removes the console warning)
            grab.throwOnDetach = false;

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
            Play(warningSound);
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

        Vector3 seatedCenter = DetectPoint();
        Vector3 currentCenter = transform.TransformPoint(centerLocal);

        if (allowPlugIn && DistanceTo(seatedCenter, currentCenter) <= snapDistance)
        {
            PlaceSeated();
            if (!isSeated) { isSeated = true; Play(plugSound); }
        }
        else
        {
            PlaceUnplugged();
            if (isSeated) { isSeated = false; Play(unplugSound); }
        }
    }

    // Snaps in while you're still holding it, as soon as it gets close to the socket
    private bool snappingNow = false;
    private void Update()
    {
        if (grab == null || !grab.isSelected || isSeated || forceDropping || snappingNow) return;

        Vector3 seatedCenter = DetectPoint();
        Vector3 currentCenter = transform.TransformPoint(centerLocal);
        distanceToSocket = DistanceTo(seatedCenter, currentCenter);

        if (!allowPlugIn) return;
        if (psuSwitch != null && !psuSwitch.IsOff) return;
        if (distanceToSocket > snapDistance) return;

        StartCoroutine(SnapInNow());
    }

    // Called by ConnectorSocketZone when the connector touches the socket
    private float lastBlockLog = -10f;
    public void SnapIn()
    {
        if (isSeated || forceDropping || snappingNow) return;
        string why = null;
        if (!allowPlugIn) why = "Allow Plug In is OFF (the new PSU doesn't count as installed yet - check PSU Slot -> Is Installed)";
        else if (psuSwitch != null && !psuSwitch.IsOff) why = "the PSU switch '" + psuSwitch.name + "' is ON - turn it off";
        if (why != null)
        {
            if (Time.time - lastBlockLog > 2f) { lastBlockLog = Time.time; Debug.LogWarning("[PullOutConnector] " + name + " touched the socket but can't plug in: " + why, this); }
            return;
        }
        Debug.Log("[PullOutConnector] " + name + " plugged in.", this);
        if (grab != null && grab.isSelected) StartCoroutine(SnapInNow());
        else { PlaceSeated(); isSeated = true; Play(plugSound); }
    }

    private IEnumerator SnapInNow()
    {
        snappingNow = true;
        forceDropping = true;           // skip the normal release logic
        grab.enabled = false;           // makes the hand let go
        yield return null;
        grab.enabled = true;
        PlaceSeated();
        isSeated = true;
        Play(plugSound);
        forceDropping = false;
        snappingNow = false;
    }

    private Vector3 DetectPoint()
    {
        if (detectSpot != null) return detectSpot.position;
        GetSeatedPose(out Vector3 p, out Quaternion r);
        return CenterAt(p, r);
    }

    // Closest distance from ANY part of the connector to the socket point (more forgiving in VR)
    private float DistanceTo(Vector3 socketPoint, Vector3 currentCenter)
    {
        float best = Vector3.Distance(currentCenter, socketPoint);
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (!c.enabled) continue;
            float d = Mathf.Sqrt(c.bounds.SqrDistance(socketPoint));
            if (d < best) best = d;
        }
        return best;
    }

    // Scene view (select the connector): yellow ball = where its middle must go to plug in
    private void OnDrawGizmosSelected()
    {
        if (seatedSpot == null) return;
        Vector3 c = Application.isPlaying
            ? CenterAt(seatedSpot.position, seatedSpot.rotation)
            : seatedSpot.position + seatedSpot.rotation * Vector3.Scale(
                  (GetComponent<BoxCollider>() != null ? GetComponent<BoxCollider>().center : Vector3.zero), transform.lossyScale);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(c, snapDistance);
    }

    // In the Inspector: put the connector exactly in the socket (Play mode stopped),
    // then click the 3 dots on this component -> "Save Seated Pose Here".
    [ContextMenu("Save Seated Pose Here")]
    private void SaveSeatedPoseHere()
    {
        Transform parent = seatedSpot != null ? seatedSpot.parent
                          : (transform.parent != null && transform.parent.parent != null ? transform.parent.parent : null);
        var spot = new GameObject(name + " Seated Spot").transform;
        spot.SetParent(parent, false);
        spot.SetPositionAndRotation(transform.position, transform.rotation);
        seatedSpot = spot;
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(spot.gameObject, "Save Seated Pose");
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[PullOutConnector] Saved seated pose as '" + spot.name + "' and set it as Seated Spot.", spot);
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

    // Where the middle of the connector would be if the pivot were at pos/rot
    private Vector3 CenterAt(Vector3 pos, Quaternion rot)
    {
        return pos + rot * Vector3.Scale(centerLocal, transform.lossyScale);
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
        if (unpluggedSpot == null) return;

        Quaternion rot = unpluggedSpot.rotation;
        Vector3 pos = unpluggedSpot.position;
        if (placeByCenter)
            pos -= rot * Vector3.Scale(centerLocal, transform.lossyScale);

        transform.SetPositionAndRotation(pos, rot);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[PullOutConnector] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
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