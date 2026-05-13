using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamGrabHard : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grab == null)
        {
            Debug.LogError("XRGrabInteractable missing on " + gameObject.name);
            return;
        }

        grab.selectEntered.AddListener(OnGrab);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        transform.SetParent(null);
        StartCoroutine(EnablePhysics());
    }

    IEnumerator EnablePhysics()
    {
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}