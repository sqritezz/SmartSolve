using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the PSU cable. Handles grab/release physics the same proven
// way as RamGrab.cs -- kinematic/no-gravity at rest, dynamic while held,
// and gravity correctly restored on release if it wasn't plugged in.
public class CableGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    private bool fixedForever = false;

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
        if (fixedForever) return; // already plugged in, CableSnap owns it now

        rb.isKinematic = false;
        rb.useGravity = true; // let it fall if it wasn't plugged in
    }

    public void MarkFixedForever()
    {
        fixedForever = true;
    }
}