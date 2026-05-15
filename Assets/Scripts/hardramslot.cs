using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardRamSlot : MonoBehaviour
{
    public Transform snapPoint;
    public HardPCManager hardPCManager;
    public AudioSource clickSound;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        TrySnap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySnap(other);
    }

    void TrySnap(Collider other)
    {
        if (currentRam != null) return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) return;

        if (grab.isSelected) return;

        GameObject ramObject = grab.gameObject;

        if (ramObject.CompareTag("RAM"))
        {
            SnapRam(ramObject);
            hardPCManager.BrokenRamInserted();
        }
        else if (ramObject.CompareTag("WorkingRAM"))
        {
            SnapRam(ramObject);
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

        ram.transform.SetParent(snapPoint, false);
        ram.transform.localPosition = Vector3.zero;
        ram.transform.localRotation = Quaternion.identity;
        ram.transform.localScale = Vector3.one;

        if (clickSound != null)
            clickSound.Play();

        Debug.Log("RAM SNAPPED: " + ram.name);
    }

    public void ClearSlot()
    {
        currentRam = null;
    }
}