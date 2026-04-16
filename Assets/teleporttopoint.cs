using UnityEngine;

public class TeleportToPoint : MonoBehaviour
{
    public Transform xrRig;

    public Transform easyPoint;
    public Transform mediumPoint;
    public Transform hardPoint;

    public GameObject mainMenu;

    public void TeleportEasy()
    {
        xrRig.position = easyPoint.position;

        mainMenu.SetActive(false);
    }

    public void TeleportMedium()
    {
        xrRig.position = mediumPoint.position;

        mainMenu.SetActive(false);
    }

    public void TeleportHard()
    {
        xrRig.position = hardPoint.position;

        mainMenu.SetActive(false);
    }
}