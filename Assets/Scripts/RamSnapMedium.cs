using UnityEngine;

public class RamSnapMedium : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButtonMedium powerButton;
    public AudioSource clickSound;

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
        RAMMedium ram = other.GetComponentInParent<RAMMedium>();

        if (ram == null)
            return;

        if (!ram.isClean)
        {
            Debug.Log("RAM is still dirty.");
            return;
        }

        Rigidbody rb = ram.GetComponent<Rigidbody>();

        ram.transform.SetParent(null, true);
        ram.transform.position = snapPoint.position;
        ram.transform.rotation = snapPoint.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        ram.isInserted = true;

        if (powerButton != null)
            powerButton.ramFixed = true;

        if (clickSound != null)
            clickSound.Play();

        Debug.Log("RAM INSERTED AND FIXED");
    }
}