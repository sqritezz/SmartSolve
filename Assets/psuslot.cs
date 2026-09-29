using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PSUSlot : MonoBehaviour
{
    public Transform snapPoint;
    public PSUPowerButtonHard powerButton;
    public AudioSource clickSound;

    [Header("Checklist Integration - New PSU installed")]
    public SequentialChecklist checklist;
    public int groupIndex = 2;
    public int objectiveIndex = 2;

    [Header("Tag Detection")]
    public string newPsuTag = "NewPSU";

    private GameObject currentPsu;

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
        if (currentPsu != null) return;
        if (!other.CompareTag(newPsuTag)) return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) return;
        if (grab.isSelected) return;

        GameObject psuObject = grab.gameObject;
        currentPsu = psuObject;

        Rigidbody rb = psuObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        psuObject.transform.SetParent(snapPoint, false);
        psuObject.transform.localPosition = Vector3.zero;
        psuObject.transform.localRotation = Quaternion.identity;

        if (clickSound != null)
            clickSound.Play();

        if (powerButton != null)
            powerButton.newPsuInstalled = true;

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    public void ClearSlot()
    {
        currentPsu = null;
    }
}