using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the practice slot. Teaches the swap mechanic: inserting the
// FAULTY practice RAM gets rejected (bounces back, no snap); inserting
// the GOOD practice RAM snaps in and succeeds.
public class PracticeRamSlotSwap : MonoBehaviour
{
    public Transform snapPoint;

    [Header("Feedback")]
    public AudioSource rejectSound;
    public AudioSource successSound;

    [Header("Events")]
    [Tooltip("Fires when the FAULTY practice RAM is tried")]
    public UnityEngine.Events.UnityEvent onRejected;
    [Tooltip("Fires when the GOOD practice RAM successfully snaps in")]
    public UnityEngine.Events.UnityEvent onSuccess;

    [Header("Checklist Integration (for the successful swap)")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    public string practiceRamTag = "PracticeRAM";

    private GameObject currentRam;
    private bool warnedThisOverlap = false;

    private void OnTriggerEnter(Collider other)
    {
        TryInsert(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentRam == null)
            TryInsert(other);
    }

    private void TryInsert(Collider other)
    {
        if (!other.CompareTag(practiceRamTag) || currentRam != null)
            return;

        XRGrabInteractable grab = other.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
            return;

        PracticeRamPart part = other.GetComponent<PracticeRamPart>();

        if (part != null && part.isFaulty)
        {
            // Faulty RAM -- reject it, don't snap. Its own grab script
            // (CableGrab.cs reused) will let it fall naturally on release
            // since it was never marked fixed forever.
            if (!warnedThisOverlap)
            {
                if (rejectSound != null)
                    rejectSound.Play();
                warnedThisOverlap = true;
                onRejected?.Invoke();
            }
            return;
        }

        // Good RAM -- snap it in, same proven scale-preserving pattern.
        currentRam = other.gameObject;

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

        if (successSound != null)
            successSound.Play();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);

        onSuccess?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == currentRam)
        {
            currentRam = null;
        }
        warnedThisOverlap = false;
    }
}