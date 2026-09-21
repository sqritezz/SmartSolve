using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to Cable Port. Practice-mode socket: any correctly-tagged cable
// that reaches the port succeeds (no wrong-slot logic, this is a tutorial).
public class CableSnap : MonoBehaviour
{
    public Transform snapPoint;
    public AudioSource clickSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    [Tooltip("Optional: fires once the cable is successfully plugged in -- hook up the follow-up dialogue unlock here.")]
    public UnityEngine.Events.UnityEvent onPluggedIn;

    private GameObject currentCable;

    private void OnTriggerEnter(Collider other)
    {
        TrySnap(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentCable == null)
            TrySnap(other);
    }

    private void TrySnap(Collider other)
    {
        if (!other.CompareTag("PSUCable") || currentCable != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        currentCable = other.gameObject;

        Vector3 desiredWorldScale = other.transform.lossyScale;

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

        Vector3 parentLossy = snapPoint.lossyScale;
        other.transform.localScale = new Vector3(
            parentLossy.x != 0f ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
            parentLossy.y != 0f ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
            parentLossy.z != 0f ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
        );

        CableGrab cableGrab = other.GetComponent<CableGrab>();
        if (cableGrab != null)
        {
            cableGrab.MarkFixedForever();
        }

        if (clickSound != null)
            clickSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);

        onPluggedIn?.Invoke();
    }
}