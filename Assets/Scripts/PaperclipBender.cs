using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the PLIERS (needs XR Grab Interactable).
//
// How to bend:
//   1 Hold the straight paperclip in one hand
//   2 Hold the pliers in the other hand
//   3 Bring the pliers close to the paperclip and pull the TRIGGER on the pliers hand
//   -> the straight clip is swapped for the bent clip, which stays in the same hand.
//
// Also stops the paperclips from falling through the floor: if one drops
// below "Fall Limit Y", it's put back on its respawn spot.
//
// PSUMediumManager reads "isBent", so the checklist keeps working.
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class PaperclipBender : MonoBehaviour
{
    [Header("Paperclips")]
    [Tooltip("The straight paperclip (paper_clip)")]
    public XRGrabInteractable straightPaperclip;
    [Tooltip("The bent paperclip (BentPaperclip) - starts hidden")]
    public XRGrabInteractable bentPaperclip;

    [Header("Bending Rules")]
    [Tooltip("The paperclip must be held in the other hand")]
    public bool requireClipHeld = true;
    [Tooltip("Optional: empty object at the tip of the pliers. Empty = uses the pliers' position.")]
    public Transform pliersTip;
    [Tooltip("How close (meters) the pliers must be to the clip. 0 = no distance check.")]
    public float bendDistance = 0.3f;

    [Header("Messages (optional)")]
    [Tooltip("e.g. \"Hold the paperclip with your other hand, then pull the trigger.\"")]
    public GameObject holdClipMessage;
    public float messageShowTime = 2.5f;

    [Header("Falling Fix")]
    [Tooltip("If a paperclip drops below this height (world Y), it's put back")]
    public float fallLimitY = -1f;
    [Tooltip("Where the straight clip goes back. Empty = where it started.")]
    public Transform straightRespawn;
    [Tooltip("Where the bent clip goes back. Empty = where it was bent.")]
    public Transform bentRespawn;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip bendSound;
    public AudioClip errorSound;

    [Header("Events")]
    public UnityEvent onBent;

    [Header("State (read-only)")]
    public bool isBent = false;

    private XRGrabInteractable pliers;
    private Vector3 straightStartPos, bentPos;
    private Quaternion straightStartRot, bentRot;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        pliers = GetComponent<XRGrabInteractable>();

        if (pliers != null) pliers.activated.AddListener(OnPliersTrigger);

        if (straightPaperclip != null)
        {
            straightStartPos = straightPaperclip.transform.position;
            straightStartRot = straightPaperclip.transform.rotation;
            NoTunneling(straightPaperclip);
        }

        if (bentPaperclip != null)
        {
            NoTunneling(bentPaperclip);
            bentPaperclip.throwOnDetach = false;
            if (!isBent) bentPaperclip.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (holdClipMessage != null) holdClipMessage.SetActive(false);
    }

    private void OnDestroy()
    {
        if (pliers != null) pliers.activated.RemoveListener(OnPliersTrigger);
    }

    // Trigger pulled while holding the pliers
    private void OnPliersTrigger(ActivateEventArgs args)
    {
        if (isBent || straightPaperclip == null || bentPaperclip == null) return;

        bool clipHeld = straightPaperclip.isSelected;
        Vector3 tip = pliersTip != null ? pliersTip.position : transform.position;
        bool closeEnough = bendDistance <= 0f ||
                           Vector3.Distance(tip, straightPaperclip.transform.position) <= bendDistance;

        if ((requireClipHeld && !clipHeld) || !closeEnough)
        {
            Play(errorSound);
            ShowMessage(holdClipMessage);
            return;
        }

        StartCoroutine(Bend());
    }

    private IEnumerator Bend()
    {
        isBent = true;
        Play(bendSound);

        // Who is holding the straight clip?
        IXRSelectInteractor hand = straightPaperclip.isSelected ? straightPaperclip.firstInteractorSelecting : null;
        var manager = straightPaperclip.interactionManager;

        Transform s = straightPaperclip.transform;
        bentPos = s.position;
        bentRot = s.rotation;

        // Let go of the straight clip and hide it
        if (hand != null && manager != null)
            manager.SelectExit(hand, straightPaperclip);
        straightPaperclip.gameObject.SetActive(false);

        // Show the bent clip in the same spot
        bentPaperclip.transform.SetPositionAndRotation(bentPos, bentRot);
        bentPaperclip.gameObject.SetActive(true);

        yield return null; // let the bent clip register with XR

        // Put it straight into the same hand
        if (hand != null && manager != null && bentPaperclip.isActiveAndEnabled)
            manager.SelectEnter(hand, bentPaperclip);
        else
            Freeze(bentPaperclip); // not held -> don't let it drop

        onBent?.Invoke();
    }

    private void Update()
    {
        // Falling-through-the-floor safety
        if (straightPaperclip != null && straightPaperclip.gameObject.activeInHierarchy &&
            !straightPaperclip.isSelected && straightPaperclip.transform.position.y < fallLimitY)
        {
            Respawn(straightPaperclip, straightRespawn, straightStartPos, straightStartRot);
        }

        if (bentPaperclip != null && bentPaperclip.gameObject.activeInHierarchy &&
            !bentPaperclip.isSelected && bentPaperclip.transform.position.y < fallLimitY)
        {
            Respawn(bentPaperclip, bentRespawn, bentPos, bentRot);
        }
    }

    private void Respawn(XRGrabInteractable clip, Transform spot, Vector3 pos, Quaternion rot)
    {
        if (spot != null) { pos = spot.position; rot = spot.rotation; }
        clip.transform.SetPositionAndRotation(pos, rot);
        Freeze(clip);
    }

    // Stop it in place (it falls normally again after the player grabs and drops it)
    private static void Freeze(XRGrabInteractable clip)
    {
        var rb = clip.GetComponent<Rigidbody>();
        if (rb == null) return;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Small, fast objects fall through thin floors - this stops that
    private static void NoTunneling(XRGrabInteractable clip)
    {
        var rb = clip.GetComponent<Rigidbody>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[PaperclipBender] A sound slot is broken. Set it to None and drag the clip in again.", this);
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