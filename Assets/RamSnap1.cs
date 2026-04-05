using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnap1 : MonoBehaviour
{
    public Transform snapPoint; // exact position where RAM will go
    public string ramTag = "RAM";

    private GameObject currentRam;

    [Header("Monitor")]
    public Renderer monitorRenderer; // drag monitor here
    public int materialIndex = 0; // which material is the screen

    private void OnTriggerEnter1(Collider other)
    {
        if (other.CompareTag(ramTag) && currentRam == null)
        {
            XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();

            // Only snap if NOT being held
            if (grab != null && !grab.isSelected)
            {
                currentRam = other.gameObject;

                // Snap
                other.transform.position = snapPoint.position;
                other.transform.rotation = snapPoint.rotation;

                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                other.transform.SetParent(transform);

                if (monitorRenderer != null)
                {
                    monitorRenderer.materials[materialIndex].color = Color.brown;
                }
            }
        }
    }

    private void OnTriggerExit1(Collider other)
    {
        if (other.CompareTag(ramTag))
        {
            currentRam = null;

            if (monitorRenderer != null)
            {
                monitorRenderer.materials[materialIndex].color = Color.black; // or original color
            }
        }
    }
}