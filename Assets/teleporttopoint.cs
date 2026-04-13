using UnityEngine;

public class TeleportToPoint : MonoBehaviour
{
    public Transform xrRig;

    public Transform easyPoint;
    public Transform mediumPoint;
    public Transform hardPoint;

    public void TeleportEasy()
    {
        xrRig.position = easyPoint.position;
    }

    public void TeleportMedium()
    {
        xrRig.position = mediumPoint.position;
    }

    public void TeleportHard()
    {
        xrRig.position = hardPoint.position;
    }
}