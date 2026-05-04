using UnityEngine;

public class RamSlotSnap : MonoBehaviour
{
    public Transform snapPoint;
    public PowerButton powerButton;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RAM"))
        {
            other.transform.position = snapPoint.position;
            other.transform.rotation = snapPoint.rotation;
            other.transform.SetParent(snapPoint);

            powerButton.isRamInstalled = true;
            Debug.Log("RAM Installed Properly!");
        }
    }
}