using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnap : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButton powerButton;
    public AudioSource clickSound;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RAM") || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();

        // Do not snap while player is still holding RAM
        if (grab != null && grab.isSelected)
            return;

        currentRam = other.gameObject;

        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Snap exactly to slot
        other.transform.SetParent(snapPoint, true);
        other.transform.localPosition = Vector3.zero;
        other.transform.localRotation = Quaternion.identity;
        other.transform.localScale = Vector3.one;

        if (powerButton != null)
            powerButton.isRamFixed = true;

        if (clickSound != null)
            clickSound.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            currentRam = null;

            if (powerButton != null)
                powerButton.isRamFixed = false;
        }
    }
}