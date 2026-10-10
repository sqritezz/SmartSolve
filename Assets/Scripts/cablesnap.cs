using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to Cable Port. When the PSU cable (tag "PSUCable") is released
// inside the port's trigger, it snaps onto the Snap Point.
//
// "Fit inside the port": after snapping, the cable is placed at these
// values (relative to Snap Point), so it sits exactly how you fitted it.
public class CableSnap : MonoBehaviour
{
    public Transform snapPoint;
    public AudioSource clickSound;

    [Header("Fit inside the port")]
    [Tooltip("Cable's Position after you fitted it in Play mode (while it's a child of Cable Snap)")]
    public Vector3 fittedPosition = Vector3.zero;
    [Tooltip("Cable's Rotation after you fitted it in Play mode")]
    public Vector3 fittedRotation = Vector3.zero;
    [Tooltip("Tick to use the Fitted Scale below instead of keeping the cable's original size")]
    public bool useFittedScale = false;
    [Tooltip("Cable's Scale after you fitted it in Play mode")]
    public Vector3 fittedScale = Vector3.one;

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
            // Zero the velocity BEFORE making it kinematic (avoids the
            // "Setting velocity of a kinematic body" warning)
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        other.transform.SetParent(snapPoint, true);
        other.transform.localPosition = fittedPosition;
        other.transform.localRotation = Quaternion.Euler(fittedRotation);

        if (useFittedScale)
        {
            other.transform.localScale = fittedScale;
        }
        else
        {
            // Keep the cable's original real-world size
            Vector3 parentLossy = snapPoint.lossyScale;
            other.transform.localScale = new Vector3(
                parentLossy.x != 0f ? desiredWorldScale.x / parentLossy.x : desiredWorldScale.x,
                parentLossy.y != 0f ? desiredWorldScale.y / parentLossy.y : desiredWorldScale.y,
                parentLossy.z != 0f ? desiredWorldScale.z / parentLossy.z : desiredWorldScale.z
            );
        }

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