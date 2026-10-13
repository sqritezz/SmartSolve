using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to an EMPTY object placed exactly between the two holes/terminals
// (24-pin green + black holes, or the bell's two terminals in the tutorial).
// Its green (Y) arrow must point OUT of the face.
//
// Touch the green spot with the bent paperclip / test wire -> it snaps in
// right away (no need to let go). Releasing it near the spot also works.
// Only works when the 24-pin is unplugged and the PSU is OFF
// (leave Connector and Psu Switch empty in the tutorial).
// Grab it again to pull it out.
//
// Audio: ONE Audio Source (leave empty = uses the one on this object),
// and the sounds are clips you drag in.
public class PaperclipSlot : MonoBehaviour
{
    [Header("Paperclip")]
    [Tooltip("The U-shaped paperclip (or the test wire in the tutorial)")]
    public XRGrabInteractable bentPaperclip;
    [Tooltip("How close (meters) any part of the clip must get to snap in")]
    public float snapDistance = 0.05f;
    [Tooltip("Snap in as soon as the clip touches the spot, even while still holding it")]
    public bool snapOnTouch = true;
    [Tooltip("Optional, makes snapping even easier: a collider around the target. Touching it counts as near.")]
    public Collider snapZone;

    [Header("Rules")]
    public PullOutConnector connector;
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;

    [Header("Messages (optional)")]
    [Tooltip("e.g. \"Unplug the 24-pin from the motherboard first.\"")]
    public GameObject unplugFirstMessage;
    [Tooltip("e.g. \"Turn off the PSU switch before inserting the paperclip!\"")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip insertSound;
    public AudioClip removeSound;
    public AudioClip warningSound;

    [Header("State (read-only)")]
    public bool hasClip = false;

    private Rigidbody clipRb;
    private Collider[] clipColliders;
    private bool armed = true;        // must move away after pulling it out before it can snap again
    private bool snappingNow = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (bentPaperclip != null)
        {
            clipRb = bentPaperclip.GetComponent<Rigidbody>();
            // true = also finds colliders while the bent clip is still hidden
            clipColliders = bentPaperclip.GetComponentsInChildren<Collider>(true);
            bentPaperclip.selectEntered.AddListener(OnClipGrabbed);
            bentPaperclip.selectExited.AddListener(OnClipReleased);
        }
    }

    private void Start()
    {
        if (unplugFirstMessage != null) unplugFirstMessage.SetActive(false);
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
    }

    private void Update()
    {
        // Snap on touch while the player is still holding the clip
        if (!snapOnTouch || hasClip || bentPaperclip == null || !bentPaperclip.isSelected) return;

        bool near = IsNear();
        if (!near) { armed = true; return; }
        if (!armed) return;
        armed = false; // one try per touch

        if (!RulesOk()) return;

        // Take it out of the hand -> OnClipReleased -> snaps in
        snappingNow = true;
        var hand = bentPaperclip.firstInteractorSelecting;
        var manager = bentPaperclip.interactionManager;
        if (hand != null && manager != null) manager.SelectExit(hand, bentPaperclip);
        snappingNow = false;
        SnapIn();
    }

    private void LateUpdate()
    {
        // Keep the clip locked in the holes while inserted
        if (hasClip && bentPaperclip != null && !bentPaperclip.isSelected)
            bentPaperclip.transform.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private void OnClipGrabbed(SelectEnterEventArgs args)
    {
        if (!hasClip) return;
        hasClip = false;
        armed = false; // pull it away first, so it doesn't snap straight back
        Play(removeSound);
    }

    private void OnClipReleased(SelectExitEventArgs args)
    {
        if (snappingNow) return;
        StartCoroutine(AfterRelease());
    }

    private IEnumerator AfterRelease()
    {
        yield return null; // let XR Grab finish releasing
        if (hasClip) yield break;

        if (IsNear() && RulesOk())
        {
            SnapIn();
            yield break;
        }

        DropClip();
    }

    // Shows the right warning and returns false if the clip isn't allowed in yet
    private bool RulesOk()
    {
        if (connector != null && connector.isSeated)
        {
            ShowMessage(unplugFirstMessage);
            return false;
        }
        if (psuSwitch != null && !psuSwitch.IsOff)
        {
            Play(warningSound);
            ShowMessage(powerOnWarning);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            return false;
        }
        return true;
    }

    // Is ANY part of the clip close to the spot?
    private bool IsNear()
    {
        Vector3 spot = transform.position;
        if (Vector3.Distance(bentPaperclip.transform.position, spot) <= snapDistance) return true;

        if (clipColliders == null || clipColliders.Length == 0)
            clipColliders = bentPaperclip.GetComponentsInChildren<Collider>(true);

        float d2 = snapDistance * snapDistance;
        Bounds zone = snapZone != null ? snapZone.bounds : default;

        foreach (var c in clipColliders)
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
            Bounds b = c.bounds;
            if (b.SqrDistance(spot) <= d2) return true;
            if (snapZone != null && b.Intersects(zone)) return true;
        }
        return false;
    }

    private void SnapIn()
    {
        if (clipRb != null)
        {
            if (!clipRb.isKinematic)
            {
                clipRb.linearVelocity = Vector3.zero;
                clipRb.angularVelocity = Vector3.zero;
            }
            clipRb.isKinematic = true;
            clipRb.useGravity = false;
        }
        bentPaperclip.transform.SetPositionAndRotation(transform.position, transform.rotation);
        hasClip = true;
        Play(insertSound);
    }

    private void DropClip()
    {
        if (clipRb != null) { clipRb.isKinematic = false; clipRb.useGravity = true; }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[PaperclipSlot] A sound slot is broken. Set it to None and drag the clip in again.", this);
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