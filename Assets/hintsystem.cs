using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// Attach this to a "Call for Help" button object in the scene (needs an
// XRSimpleInteractable + Collider so the player can select it with their
// ray), or hook UseHint() to a UI Button's OnClick() instead.
public class HintSystem : MonoBehaviour
{
    [Header("Limits")]
    public int maxHints = 2;
    private int hintsUsed = 0;

    [Header("Hint Content")]
    [Tooltip("One hint message per use, in order. If the player uses more hints than there are messages, the last one repeats.")]
    [TextArea(1, 3)]
    public string[] hintMessages;

    [Header("Display")]
    public GameObject hintPanel;
    public TMP_Text hintText;
    public float displayDuration = 4f;

    [Header("Scoring Integration")]
    public PerformanceTracker performanceTracker;

    [Header("UI Feedback (optional)")]
    public TMP_Text hintsRemainingText;

    private void Awake()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);

        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnButtonPressed);

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

        string message = hintMessages.Length > 0
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
        if (hintPanel != null) hintPanel.SetActive(true);

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
}