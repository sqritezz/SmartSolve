using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to an EMPTY object placed exactly between the green and black holes on
// the 24-pin connector's face, as a CHILD of the connector (so it moves with it).
// Its green (Y) arrow must point OUT of the connector face.
//
// Release the BENT paperclip near it -> it snaps in, legs in the holes.
// Only works when the 24-pin is unplugged and the PSU is OFF.
// Grab the clip again to pull it out.
public class PaperclipSlot : MonoBehaviour
{
    [Header("Paperclip")]
    [Tooltip("The U-shaped paperclip")]
    public XRGrabInteractable bentPaperclip;
    [Tooltip("How close (meters) the clip must be released to snap in")]
    public float snapDistance = 0.05f;

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
    public AudioSource insertSound;
    public AudioSource removeSound;
    public AudioSource warningSound;

    [Header("State (read-only)")]
    public bool hasClip = false;

    private Rigidbody clipRb;

    private void Awake()
    {
        if (bentPaperclip != null)
        {
            clipRb = bentPaperclip.GetComponent<Rigidbody>();
            bentPaperclip.selectEntered.AddListener(OnClipGrabbed);
            bentPaperclip.selectExited.AddListener(OnClipReleased);
        }
    }

    private void Start()
    {
        if (unplugFirstMessage != null) unplugFirstMessage.SetActive(false);
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
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
        if (removeSound != null) removeSound.Play();
    }

    private void OnClipReleased(SelectExitEventArgs args)
    {
        StartCoroutine(AfterRelease());
    }

    private IEnumerator AfterRelease()
    {
        yield return null; // let XR Grab finish releasing

        bool near = Vector3.Distance(bentPaperclip.transform.position, transform.position) <= snapDistance;

        if (near)
        {
            if (connector != null && connector.isSeated)
            {
                ShowMessage(unplugFirstMessage);
            }
            else if (psuSwitch != null && !psuSwitch.IsOff)
            {
                if (warningSound != null) warningSound.Play();
                ShowMessage(powerOnWarning);
                if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            }
            else
            {
                SnapIn();
                yield break;
            }
        }

        DropClip();
    }

    private void SnapIn()
    {
        if (clipRb != null) { clipRb.isKinematic = true; clipRb.useGravity = false; }
        bentPaperclip.transform.SetPositionAndRotation(transform.position, transform.rotation);
        hasClip = true;
        if (insertSound != null) insertSound.Play();
    }

    private void DropClip()
    {
        if (clipRb != null) { clipRb.isKinematic = false; clipRb.useGravity = true; }
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
