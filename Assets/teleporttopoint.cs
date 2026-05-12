using UnityEngine;

public class TeleportToPoint : MonoBehaviour
{
    public Transform xrRig;

    public Transform easyPoint;
    public Transform mediumPoint;
    public Transform hardPoint;

    public GameObject mainMenu;
    public GameObject home;

    void Start()
    {
        home.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OpenMainMenu()
    {
        home.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void TeleportEasy()
    {
        xrRig.position = easyPoint.position;

        mainMenu.SetActive(false);
        home.SetActive(false);
    }

    public void TeleportMedium()
    {
        xrRig.position = mediumPoint.position;

        mainMenu.SetActive(false);
        home.SetActive(false);
    }

    public void TeleportHard()
    {
        xrRig.position = hardPoint.position;

        mainMenu.SetActive(false);
        home.SetActive(false);
    }
}