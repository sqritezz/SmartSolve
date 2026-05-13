using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    public Transform xrRig;

    public void TeleportTo(Transform target)
    {
        xrRig.position = target.position;
        xrRig.rotation = target.rotation;
    }
}