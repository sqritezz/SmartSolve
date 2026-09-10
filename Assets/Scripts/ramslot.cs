using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RamSnap : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButton powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Which group this objective belongs to")]
    public int groupIndex = 1;
    [Tooltip("Which objective line within that group this is")]
    public int objectiveIndex = 0;

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

        // Mark this objective complete, only if it's currently the active pending one
        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
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