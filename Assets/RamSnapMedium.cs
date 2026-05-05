using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnapMedium : MonoBehaviour
{
    public Transform snapPoint;
    public string ramTag = "RAM";

    private GameObject currentRam;

    [Header("Monitor")]
    public Renderer monitorRenderer;
    public int materialIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ramTag) && currentRam == null)
        {
            XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();

            if (grab != null && !grab.isSelected)
            {
                currentRam = other.gameObject;

                other.transform.SetParent(null, true);
                other.transform.position = snapPoint.position;
                other.transform.rotation = snapPoint.rotation;
                other.transform.localScale = Vector3.one;

                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                other.transform.SetParent(transform, true);

                if (monitorRenderer != null)
                    monitorRenderer.materials[materialIndex].color = Color.white;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ramTag))
        {
            currentRam = null;

            if (monitorRenderer != null)
                monitorRenderer.materials[materialIndex].color = Color.black;
        }
    }
}