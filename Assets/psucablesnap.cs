using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the cable port/socket trigger zone on the PSU.
public class PSUCableSnap : MonoBehaviour
{
    public Transform snapPoint;
    public AudioSource clickSound;

    [Header("Power Button Link")]
    public PSUPowerButtonMedium powerButton;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 2;
    public int objectiveIndex = 0;

    [Header("Cable Detection")]
    [Tooltip("Tag the loose cable object with this")]
    public string cableTag = "PSUCable";

    private GameObject currentCable;

    private void OnTriggerEnter(Collider other)
    {
        if (currentCable != null) return;
        if (!other.CompareTag(cableTag)) return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected) return;

        currentCable = other.gameObject;

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

        if (clickSound != null)
            clickSound.Play();

        if (powerButton != null)
            powerButton.isCableConnected = true;

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}