using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// ONE in the whole scene (on Hint Manager). The controller's B button calls UseHint().
//
// Per-level hint content comes from TeleportToPoint, which calls SetHints()
// with that stage's tracker + hint list every time you teleport into it.
//
// Flow when B is pressed:
//   1. Confirm panel: "Use a hint? It will cost you 1 star."  ->  YES / NO
//      (leave Confirm Panel empty to skip this and show the hint right away)
//   2. YES -> the hint shows and STAYS until B is pressed again (or a Close button)
//   B while the confirm panel is open = NO. B while a hint is showing = close it.
// Outside a level / Final stage (no tracker) -> just shows the message, costs nothing.
public class HintSystem : MonoBehaviour
{
    public static HintSystem Instance { get; private set; }

    [Header("Limits")]
    public int maxHints = 2;
    private int hintsUsed = 0;

    [Header("Hint Content")]
    [Tooltip("Set per-stage by TeleportToPoint.ShowHintFor(). One hint message per use, in order.")]
    [TextArea(1, 3)]
    public string[] hintMessages;

    [Header("Display")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    [Tooltip("0 = the hint stays until B is pressed again. Above 0 = hides after that many seconds.")]
    public float displayDuration = 0f;

    [Header("Confirm (Yes / No)")]
    [Tooltip("Panel with the question + YES and NO buttons. YES OnClick -> HintSystem.ConfirmUseHint, NO OnClick -> HintSystem.CancelUseHint")]
    public GameObject confirmPanel;
    public TMP_Text confirmText;
    [TextArea(1, 3)]
    public string confirmMessage = "Use a hint? It will cost you 1 star.";
    [TextArea(1, 3)]
    public string confirmMessageFilipino = "Gagamit ng hint? Mababawasan ka ng 1 star.";

    [Header("Placement (in front of the player)")]
    [Tooltip("Main Camera. Empty = finds it automatically")]
    public Transform playerHead;
    public float distanceFromPlayer = 1.2f;
    public float heightOffset = -0.05f;

    [Header("Scoring Integration")]
    public PerformanceTracker performanceTracker;

    [Header("UI Feedback (optional)")]
    public TMP_Text hintsRemainingText;

    private void Awake()
    {
        Instance = this;

        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnButtonPressed);

        UpdateRemainingText();
    }

    // TeleportToPoint.ShowHintFor uses this - it also resets the hint count,
    // so arriving at a new stage always starts with a fresh "Hints left".
    public void SetHints(PerformanceTracker tracker, string[] hints)
    {
        performanceTracker = tracker;
        hintMessages = hints;
        hintsUsed = 0;
        HideMessage();
        CloseConfirm();
        UpdateRemainingText();
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        UseHint();
    }

    // B button / hint button
    public void UseHint()
    {
        // B while asking = NO
        if (confirmPanel != null && confirmPanel.activeSelf) { CancelUseHint(); return; }

        // B while a hint is showing = close it
        if (hintPanel != null && hintPanel.activeSelf) { HideMessage(); return; }

        // Not in a scored level (menu room, Final stage without hints): free message
        if (performanceTracker == null)
        {
            ShowMessage(hintMessages != null && hintMessages.Length > 0
                ? hintMessages[0]
                : LanguageManager.Pick("Try checking each part carefully.", "Suriin nang mabuti ang bawat piyesa."));
            return;
        }

        if (hintsUsed >= maxHints)
        {
            ShowMessage(LanguageManager.Pick("No hints left -- you've got this!", "Wala ka nang hint -- kaya mo 'yan!"));
            return;
        }

        if (confirmPanel != null) { OpenConfirm(); return; }

        GiveHint();
    }

    // YES button
    public void ConfirmUseHint()
    {
        CloseConfirm();
        GiveHint();
    }

    // NO button
    public void CancelUseHint()
    {
        CloseConfirm();
    }

    // Optional Close / OK button on the hint panel
    public void CloseHint()
    {
        HideMessage();
    }

    private void GiveHint()
    {
        string message = (hintMessages != null && hintMessages.Length > 0)
            ? hintMessages[Mathf.Min(hintsUsed, hintMessages.Length - 1)]
            : LanguageManager.Pick("Try checking each part carefully.", "Suriin nang mabuti ang bawat piyesa.");

        ShowMessage(message);

        hintsUsed++;
        if (performanceTracker != null)
            performanceTracker.RegisterHintUsed();

        UpdateRemainingText();
    }

    private void OpenConfirm()
    {
        if (confirmText != null)
        {
            int left = maxHints - hintsUsed;
            confirmText.text = LanguageManager.Pick(confirmMessage, confirmMessageFilipino) + "\n" +
                               LanguageManager.Pick("Hints left: ", "Natitirang hint: ") + left;
        }
        PlaceInFrontOfPlayer(confirmPanel);
        confirmPanel.SetActive(true);
    }

    private void CloseConfirm()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    private void ShowMessage(string message)
    {
        if (hintText != null) hintText.text = message;

        if (hintPanel != null)
        {
            PlaceInFrontOfPlayer(hintPanel);
            hintPanel.SetActive(true);
        }

        CancelInvoke(nameof(HideMessage));
        if (displayDuration > 0f)
            Invoke(nameof(HideMessage), displayDuration);
    }

    private void HideMessage()
    {
        CancelInvoke(nameof(HideMessage));
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private void UpdateRemainingText()
    {
        if (hintsRemainingText != null)
            hintsRemainingText.text = LanguageManager.Pick("Hints left: ", "Natitirang hint: ") + (maxHints - hintsUsed);
    }

    private void PlaceInFrontOfPlayer(GameObject panel)
    {
        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        if (playerHead == null || panel == null) return;

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = playerHead.up;
        forward.Normalize();

        Vector3 pos = playerHead.position + forward * distanceFromPlayer;
        pos.y = playerHead.position.y + heightOffset;

        panel.transform.position = pos;
        panel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}