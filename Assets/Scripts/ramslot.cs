using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnap : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButton powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public ChecklistManager checklistManager;
    [Tooltip("Index of the 'Re-attach the RAM' objective in the ChecklistManager's Objectives array")]
    public int ramObjectiveIndex = 0;

    private GameObject currentRam;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RAM") || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
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

        other.transform.SetParent(snapPoint, true);
        other.transform.localPosition = Vector3.zero;
        other.transform.localRotation = Quaternion.identity;
        other.transform.localScale = Vector3.one;

        if (powerButton != null)
            powerButton.isRamFixed = true;

        RamGrab ramGrab = other.GetComponent<RamGrab>();
        if (ramGrab != null)
        {
            ramGrab.MarkFixedForever();
        }

        if (clickSound != null)
            clickSound.Play();

        // Mark the "Re-attach the RAM" objective as complete
        if (checklistManager != null)
            checklistManager.CompleteObjective(ramObjectiveIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            currentRam = null;
            RamGrab ramGrab = other.GetComponent<RamGrab>();
            if (ramGrab == null && powerButton != null)
            {
                powerButton.isRamFixed = false;
            }
        }
    }
}