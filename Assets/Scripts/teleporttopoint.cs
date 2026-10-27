using System.Collections;
using UnityEngine;

public class TeleportToPoint : MonoBehaviour
{
    // Name of the level the player is in now (matches QuizGate quiz names, e.g. "RAM Medium")
    public static string CurrentLevel = "";

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
    public Transform easyRAMPoint;
    public Transform mediumRAMPoint;
    public Transform hardRAMPoint;

    [Header("PSU Points")]
    public Transform easyPSUPoint;
    public Transform mediumPSUPoint;
    public Transform hardPSUPoint;

    [Header("GPU Points")]
    public Transform easyGPUPoint;
    public Transform mediumGPUPoint;
    public Transform hardGPUPoint;

    [Header("Final Stage Point")]
    public Transform finalPoint;

    public GameObject mainMenu;
    public GameObject home;

    [Header("Hint Button")]
    public GameObject hintButton;
    public HintSystem hintSystem;

    [Header("RAM Hint Data")]
    public PerformanceTracker easyRAMTracker;
    public string[] easyRAMHints;

    public PerformanceTracker mediumRAMTracker;
    public string[] mediumRAMHints;

    public PerformanceTracker hardRAMTracker;
    public string[] hardRAMHints;

    [Header("PSU Hint Data")]
    public PerformanceTracker easyPSUTracker;
    public string[] easyPSUHints;

    public PerformanceTracker mediumPSUTracker;
    public string[] mediumPSUHints;

    public PerformanceTracker hardPSUTracker;
    public string[] hardPSUHints;

    [Header("GPU Hint Data")]
    public PerformanceTracker easyGPUTracker;
    public string[] easyGPUHints;

    public PerformanceTracker mediumGPUTracker;
    public string[] mediumGPUHints;

    public PerformanceTracker hardGPUTracker;
    public string[] hardGPUHints;

    [Header("Final Stage Hint Data")]
    [Tooltip("OFF = no hints in the Final stage: pressing B only shows the message below and costs no star")]
    public bool finalAllowHints = false;
    public PerformanceTracker finalTracker;
    public string[] finalHints;
    [TextArea(1, 3)]
    public string finalNoHintMessage = "No hints in the Final Challenge. Trust your skills!";

    [Header("Outside a level (menu room / PC room)")]
    [TextArea(1, 3)]
    public string noLevelHintMessage = "Hints only work inside a level.";

    void Start()
    {
        home.SetActive(true);
        mainMenu.SetActive(false);

        if (hintButton != null)
            hintButton.SetActive(false);

        ClearHints();

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
        CurrentLevel = "";
        MoveRig(menuRoomPoint);
        home.SetActive(false);
        mainMenu.SetActive(true);
        ClearHints();

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
        ClearHints();

        if (hintButton != null)
            hintButton.SetActive(false);
    }

    // Outside a level: B still works, but only shows a message and costs nothing
    private void ClearHints()
    {
        if (hintSystem != null)
            hintSystem.SetHints(null, new string[] { noLevelHintMessage });
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
        CurrentLevel = "";
        xrRig.position = pcRoom.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ClearHints();

        if (hintButton != null)
            hintButton.SetActive(false);
    }

    public void TeleportRAMEasy()
    {
        CurrentLevel = "RAM Easy";
        xrRig.position = easyRAMPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyRAMTracker, easyRAMHints);
    }

    public void TeleportRAMMedium()
    {
        CurrentLevel = "RAM Medium";
        xrRig.position = mediumRAMPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumRAMTracker, mediumRAMHints);
    }

    public void TeleportRAMHard()
    {
        CurrentLevel = "RAM Hard";
        xrRig.position = hardRAMPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardRAMTracker, hardRAMHints);
    }

    public void TeleportPSUEasy()
    {
        CurrentLevel = "PSU Easy";
        xrRig.position = easyPSUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyPSUTracker, easyPSUHints);
    }

    public void TeleportPSUMedium()
    {
        CurrentLevel = "PSU Medium";
        xrRig.position = mediumPSUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumPSUTracker, mediumPSUHints);
    }

    public void TeleportPSUHard()
    {
        CurrentLevel = "PSU Hard";
        xrRig.position = hardPSUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardPSUTracker, hardPSUHints);
    }

    public void TeleportGPUEasy()
    {
        CurrentLevel = "GPU Easy";
        xrRig.position = easyGPUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(easyGPUTracker, easyGPUHints);
    }

    public void TeleportGPUMedium()
    {
        CurrentLevel = "GPU Medium";
        xrRig.position = mediumGPUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(mediumGPUTracker, mediumGPUHints);
    }

    public void TeleportGPUHard()
    {
        CurrentLevel = "GPU Hard";
        xrRig.position = hardGPUPoint.position;
        mainMenu.SetActive(false);
        home.SetActive(false);
        ShowHintFor(hardGPUTracker, hardGPUHints);
    }

    public void TeleportFinal()
    {
        CurrentLevel = "Final";
        MoveRig(finalPoint);
        mainMenu.SetActive(false);
        home.SetActive(false);

        if (finalAllowHints)
            ShowHintFor(finalTracker, finalHints);
        else
            ShowHintFor(null, new string[] { finalNoHintMessage }); // no tracker = no star lost
    }
}