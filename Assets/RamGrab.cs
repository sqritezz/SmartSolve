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

        grab.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        hasBeenRemoved = true;

        powerButton.isRamInstalled = false;

        StartCoroutine(EnablePhysics());
    }

    IEnumerator EnablePhysics()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
    }

    public void OnInserted()
    {
        if (hasBeenRemoved)
        {
            powerButton.isRamInstalled = true;
            Debug.Log("RAM inserted via socket - FIXED");
        }
    }
}