using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to an empty object at the PSU's spot in the case (needs a TRIGGER Collider).
// Snap Point = where the new PSU should sit (usually the old PSU's exact position).
// The new PSU (tagged "NewPSU") snaps in when released inside the trigger,
// but only after the old PSU has been removed.
public class PSUSlot : MonoBehaviour
{
    public Transform snapPoint;
    public PSUOldUnit oldUnit;
    public AudioSource clickSound;

    [Header("Tag Detection")]
    public string newPsuTag = "NewPSU";

    [Header("State (read-only)")]
    public bool isInstalled = false;

    private GameObject currentPsu;

    private void OnTriggerEnter(Collider other) { TrySnap(other); }
    private void OnTriggerStay(Collider other) { TrySnap(other); }

    private void TrySnap(Collider other)
    {
        if (isInstalled || currentPsu != null) return;
        if (oldUnit != null && !oldUnit.isRemoved) return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();
        if (grab == null || !grab.CompareTag(newPsuTag)) return;
        if (grab.isSelected) return; // wait until the player lets go

        GameObject psu = grab.gameObject;
        currentPsu = psu;

        Rigidbody rb = psu.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Transform target = snapPoint != null ? snapPoint : transform;
        psu.transform.SetParent(target, true);
        psu.transform.SetPositionAndRotation(target.position, target.rotation);

        // Lock it in - no pulling the new PSU back out
        grab.enabled = false;

        isInstalled = true;
        if (clickSound != null) clickSound.Play();
    }

    public void ClearSlot()
    {
        currentPsu = null;
    }
}
