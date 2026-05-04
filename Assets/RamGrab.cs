using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    public PowerButton powerButton;

    private bool hasBeenRemoved = false;

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
        transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = false;

        powerButton.isRamInstalled = false;
    }

    IEnumerator EnablePhysics()
    {
        yield return new WaitForFixedUpdate();

        rb.isKinematic = false;
        rb.useGravity = true;
    }
}