using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    public PowerButton powerButton;

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

        if (powerButton != null && !fixedForever)
            powerButton.isRamFixed = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (fixedForever) return; // already correctly snapped, RamSnap owns it now

        // Force these regardless of what Throw On Detach or anything else
        // set them to right before this listener ran.
        rb.isKinematic = false;
        rb.useGravity = true;

        Debug.Log("[RamGrab] After release fix. isKinematic=" + rb.isKinematic + " useGravity=" + rb.useGravity);
    }

    public void MarkFixedForever()
    {
        fixedForever = true;

        if (powerButton != null)
            powerButton.isRamFixed = true;
    }
}