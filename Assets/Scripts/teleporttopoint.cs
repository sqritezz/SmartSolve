using System.Collections;
using UnityEngine;

public class TeleportToPoint : MonoBehaviour
{
    public Transform xrRig;
    public Transform pcRoom;

    [Header("Menu Room")]
    [Tooltip("Where the player lands in the menu room (e.g. MenuPlatform)")]
    public Transform menuRoomPoint;
    [Tooltip("Replaces Intro To Menu: move to the menu room automatically when the game starts")]
    public bool goToMenuRoomOnStart = true;
    [Tooltip("Seconds to wait first (the old Intro Duration). 0 = right away.")]
    public float menuRoomStartDelay = 5f;

    [Header("RAM Points")]
    public Transform easyPoint;
    public Transform mediumPoint;
    public Transform hardPoint;

    [Header("PSU Points")]
    public Transform psuEasyPoint;
    public Transform psuMediumPoint;
    public Transform psuHardPoint;

    [Header("Stage3 Points (rename once decided)")]
    public Transform stage3EasyPoint;
    public Transform stage3MediumPoint;
    public Transform stage3HardPoint;

    public GameObject mainMenu;
    public GameObject home;

    [Header("Hint Button")]
    public GameObject hintButton;
    public HintSystem hintSystem;

    [Header("Per-Stage Hint Data")]
    public PerformanceTracker easyRamTracker;
    public string[] easyRamHints;

    public PerformanceTracker mediumRamTracker;
    public string[] mediumRamHints;

    public PerformanceTracker hardRamTracker;
    public string[] hardRamHints;

    public PerformanceTracker easyPsuTracker;
    public string[] easyPsuHints;

    public PerformanceTracker mediumPsuTracker;
    public string[] mediumPsuHints;

    public PerformanceTracker hardPsuTracker;
    public string[] hardPsuHints;

    [Header("Stage3 Hint Data (rename once decided)")]
    public PerformanceTracker easyStage3Tracker;
    public string[] easyStage3Hints;

    public PerformanceTracker mediumStage3Tracker;
    public string[] mediumStage3Hints;

    public PerformanceTracker hardStage3Tracker;
    public string[] hardStage3Hints;

    void Start()
    {
        home.SetActive(true);
        mainMenu.SetActive(false);

        if (hintButton != null)
            hintButton.SetActive(false);

        if (goToMenuRoomOnStart && menuRoomPoint != null)
            StartCoroutine(GoToMenuRoomAfterIntro());
    }

    // Same as the old Intro To Menu: wait, then move to the menu room
    // (panels stay as they are, so the warning/home screen still works)
    private IEnumerator GoToMenuRoomAfterIntro()
    {
        if (menuRoomStartDelay > 0f)
            yield return new WaitForSeconds(menuRoomStartDelay);
        MoveRig(menuRoomPoint);
    }

    // Teleports to the menu room and opens the main menu.
    // Use this for the Hard levels' NEXT button (or any Back to Menu).
    public void TeleportMenuRoom()
    {
        MoveRig(menuRoomPoint);
        home.SetActive(false);
        mainMenu.SetActive(true);

        if (hintButton != null)
            hintButton.SetActive(false);
    }

    // Moves the player and faces the point's direction.
    // The Character Controller blocks teleporting, so it is switched off for a moment.
    private void MoveRig(Transform point)
    {
        if (xrRig == null || point == null) return;

        CharacterController cc = xrRig.GetComponent<CharacterController>();
        bool ccWasOn = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        xrRig.position = point.position;
        xrRig.rotation = Quaternion.Euler(0f, point.eulerAngles.y, 0f);

        if (cc != null) cc.enabled = ccWasOn;
    }

    public void OpenMainMenu()
    {
        home.SetActive(false);
        mainMenu.SetActive(true);

        if (hintButton != null)
            hintButton.SetActive(false);
    }

    private void ShowHintFor(PerformanceTracker tracker, string[] hints)
    {
        if (hintSystem != null)
            hintSystem.SetHints(tracker, hints);

        if (hintButton != null)
            hintButton.SetActive(true);
    }

    public void TeleportPCRoom()
    {
        xrRig.position = pcRoom.position;
        mainMenu.SetActive(false);
        home.SetActive(false);

        if (hintButton != null)
            hintButton.SetActive(false);
    }

    public void TeleportEasy()
    {
        xrRig.position = easyPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyRamTracker, easyRamHints);
    }

    public void TeleportMedium()
    {
        xrRig.position = mediumPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumRamTracker, mediumRamHints);
    }

    public void TeleportHard()
    {
        xrRig.position = hardPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardRamTracker, hardRamHints);
    }

    public void TeleportPsuEasy()
    {
        xrRig.position = psuEasyPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyPsuTracker, easyPsuHints);
    }

    public void TeleportPsuMedium()
    {
        xrRig.position = psuMediumPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumPsuTracker, mediumPsuHints);
    }

    public void TeleportPsuHard()
    {
        xrRig.position = psuHardPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardPsuTracker, hardPsuHints);
    }

    public void TeleportStage3Easy()
    {
        xrRig.position = stage3EasyPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyStage3Tracker, easyStage3Hints);
    }

    public void TeleportStage3Medium()
    {
        xrRig.position = stage3MediumPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumStage3Tracker, mediumStage3Hints);
    }

    public void TeleportStage3Hard()
    {
        xrRig.position = stage3HardPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardStage3Tracker, hardStage3Hints);
    }
}