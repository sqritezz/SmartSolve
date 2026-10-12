using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// ONE in the whole scene. Attach this to the "Call for Help" button object
// (needs an XRSimpleInteractable + Collider), or hook UseHint() to a UI
// Button's OnClick() instead.
//
// Per-level hint content comes from TeleportToPoint, which calls SetHints()
// with that stage's tracker + hint list every time you teleport into it.
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
    public float displayDuration = 4f;

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

        if (hintPanel != null)
            hintPanel.SetActive(false);

        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnButtonPressed);

        UpdateRemainingText();
    }

    // Call this instead of setting performanceTracker/hintMessages directly
    // (TeleportToPoint.ShowHintFor uses this) - it also resets the hint
    // count, so arriving at a new stage always starts with a fresh "Hints left".
    public void SetHints(PerformanceTracker tracker, string[] hints)
    {
        performanceTracker = tracker;
        hintMessages = hints;
        hintsUsed = 0;
        UpdateRemainingText();
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        UseHint();
    }

    // Call this directly from a UI Button's OnClick() if you're not using
    // an XRSimpleInteractable.
    public void UseHint()
    {
        if (hintsUsed >= maxHints)
        {
            ShowMessage("No hints left -- you've got this!");
            return;
        }

        string message = (hintMessages != null && hintMessages.Length > 0)
            ? hintMessages[Mathf.Min(hintsUsed, hintMessages.Length - 1)]
            : "Try checking each part carefully.";

        ShowMessage(message);

        hintsUsed++;
        if (performanceTracker != null)
            performanceTracker.RegisterHintUsed();

        UpdateRemainingText();
    }

    private void ShowMessage(string message)
    {
        if (hintText != null) hintText.text = message;

        if (hintPanel != null)
        {
            PlaceInFrontOfPlayer();
            hintPanel.SetActive(true);
        }

        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), displayDuration);
    }

    private void HideMessage()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);
    }

    private void UpdateRemainingText()
    {
        if (hintsRemainingText != null)
            hintsRemainingText.text = "Hints left: " + (maxHints - hintsUsed);
    }

    private void PlaceInFrontOfPlayer()
    {
        if (playerHead == null || hintPanel == null) return;

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = playerHead.up;
        forward.Normalize();

        Vector3 pos = playerHead.position + forward * distanceFromPlayer;
        pos.y = playerHead.position.y + heightOffset;

        hintPanel.transform.position = pos;
        hintPanel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}