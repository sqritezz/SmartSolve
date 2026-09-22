using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the practice sponge. Same grab/release physics fix proven on
// RamGrab.cs and CableGrab.cs -- kinematic/no-gravity at rest, dynamic
// while held, gravity restored on release.
public class SpongeGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.useGravity = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        rb.isKinematic = false;
        rb.useGravity = true; // let it fall normally when let go, always
    }
}