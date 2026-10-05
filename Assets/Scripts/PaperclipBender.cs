using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the STRAIGHT paperclip (needs Rigidbody + XR Grab Interactable + Collider).
// Touch it with the pliers -> it is swapped for the BENT (U-shaped) paperclip
// in the same spot. If the player was holding it, the bent one stays in their hand.
public class PaperclipBender : MonoBehaviour
{
    [Header("Bent Version")]
    [Tooltip("The U-shaped paperclip. Set it INACTIVE in the Hierarchy.")]
    public GameObject bentPaperclip;

    [Header("Bending Tool")]
    [Tooltip("Tag on the pliers. Create this tag in Unity first.")]
    public string toolTag = "Pliers";

    [Header("Audio")]
    public AudioSource bendSound;

    [Header("Events")]
    public UnityEvent onBent;

    [Header("State (read-only)")]
    public bool isBent = false;

    private XRGrabInteractable straightGrab;
    private XRGrabInteractable bentGrab;
    private Renderer[] myRenderers;

    private void Awake()
    {
        straightGrab = GetComponent<XRGrabInteractable>();
        myRenderers = GetComponentsInChildren<Renderer>();

        if (bentPaperclip != null)
        {
            bentGrab = bentPaperclip.GetComponent<XRGrabInteractable>();
            bentPaperclip.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other) { CheckTool(other); }
    private void OnCollisionEnter(Collision collision) { CheckTool(collision.collider); }

    private void CheckTool(Collider other)
    {
        if (isBent) return;

        bool isTool = other.CompareTag(toolTag) ||
                      (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(toolTag));
        if (isTool)
            StartCoroutine(Bend());
    }

    private IEnumerator Bend()
    {
        isBent = true;

        // Remember who was holding the straight clip
        IXRSelectInteractor hand = null;
        XRInteractionManager manager = null;
        if (straightGrab != null && straightGrab.isSelected)
        {
            hand = straightGrab.firstInteractorSelecting;
            manager = straightGrab.interactionManager;
        }

        // Hide the straight clip right away
        foreach (var r in myRenderers) if (r != null) r.enabled = false;

        // Show the bent clip in the same spot
        if (bentPaperclip != null)
        {
            bentPaperclip.transform.SetPositionAndRotation(transform.position, transform.rotation);
            bentPaperclip.SetActive(true);
        }

        if (bendSound != null)
        {
            bendSound.transform.SetParent(null, true); // keep playing after this object hides
            bendSound.Play();
        }

        onBent?.Invoke();

        // Move the grab from the straight clip to the bent clip
        if (hand != null && manager != null)
        {
            manager.SelectExit(hand, straightGrab);
            yield return null;
            if (bentGrab != null && bentGrab.isActiveAndEnabled)
                manager.SelectEnter(hand, bentGrab);
        }

        yield return null;
        gameObject.SetActive(false);
    }
}
