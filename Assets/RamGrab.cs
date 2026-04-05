using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamGrab : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grab.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        transform.SetParent(null);
        transform.position += Vector3.up * 0.01f;
        StartCoroutine(EnablePhysics());
    }

    IEnumerator EnablePhysics()
    {
        yield return new WaitForFixedUpdate();
        rb.isKinematic = false;
    }
}