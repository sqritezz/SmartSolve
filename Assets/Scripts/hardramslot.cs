using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardRamSlot : MonoBehaviour
{
    public Transform snapPoint;
    public HardPCManager hardPCManager;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        if (currentRam != null) return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();

        if (grab != null && grab.isSelected)
            return;

        if (other.CompareTag("RAM"))
        {
            SnapRam(other.gameObject);
            hardPCManager.BrokenRamInserted();
        }

        if (other.CompareTag("WorkingRAM"))
        {
            SnapRam(other.gameObject);
            hardPCManager.WorkingRamInserted();
        }
    }

    void SnapRam(GameObject ram)
    {
        currentRam = ram;

        Rigidbody rb = ram.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        ram.transform.SetParent(snapPoint, true);
        ram.transform.position = snapPoint.position;
        ram.transform.rotation = snapPoint.rotation;
        ram.transform.localScale = Vector3.one;
    }
}