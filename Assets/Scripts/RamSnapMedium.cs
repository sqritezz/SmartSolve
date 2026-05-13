using UnityEngine;

public class RamSnapMedium : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButtonMedium powerButton;
    public AudioSource clickSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RAM"))
            return;

        RAMMedium ram = other.GetComponent<RAMMedium>();

        if (ram == null)
            return;

        if (!ram.isClean)
        {
            Debug.Log("RAM is still dirty.");
            return;
        }

        Rigidbody rb = other.GetComponent<Rigidbody>();

        other.transform.SetParent(null, true);
        other.transform.position = snapPoint.position;
        other.transform.rotation = snapPoint.rotation;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        ram.isInserted = true;

        if (clickSound != null)
            clickSound.Play();

        if (powerButton != null)
            powerButton.FixRAM();

        Debug.Log("RAM INSERTED AND FIXED");
    }
}