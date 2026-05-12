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
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        transform.SetParent(null, true);

        rb.isKinematic = false;
        rb.useGravity = false;

        if (powerButton != null && !fixedForever)
            powerButton.isRamFixed = false;
    }

    public void MarkFixedForever()
    {
        fixedForever = true;

        if (powerButton != null)
            powerButton.isRamFixed = true;
    }
}