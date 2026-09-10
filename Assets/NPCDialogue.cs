using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// Attach this directly to the NPC GameObject.
// Requires an XRSimpleInteractable on the same object (or a child) so the
// player can point their ray at the NPC and pull the trigger to talk.
[RequireComponent(typeof(XRSimpleInteractable))]
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Content")]
    [TextArea(2, 4)]
    public string[] lines;

    [Header("UI References")]
    [Tooltip("The dialogue canvas/panel that appears while talking")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text speakerNameText;
    public string speakerName = "NPC";

    [Header("Animation (optional)")]
    [Tooltip("Leave empty if you don't have Talk/Idle animator triggers set up")]
    public Animator animator;
    public string talkTriggerName = "Talk";
    public string idleTriggerName = "Idle";

    private int currentLine = -1;
    private bool isTalking = false;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnInteract);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnInteract(SelectEnterEventArgs args)
    {
        if (!isTalking)
            StartDialogue();
        else
            AdvanceLine();
    }

    public void StartDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning(gameObject.name + ": NPCDialogue has no lines assigned.");
            return;
        }

        isTalking = true;
        currentLine = -1;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speakerName;

        if (animator != null && !string.IsNullOrEmpty(talkTriggerName))
            animator.SetTrigger(talkTriggerName);

        AdvanceLine();
    }

    // Hook this to a "Next" UI button too, if you want one in the dialogue panel
    public void AdvanceLine()
    {
        currentLine++;

        if (currentLine >= lines.Length)
        {
            EndDialogue();
            return;
        }

        if (dialogueText != null)
            dialogueText.text = lines[currentLine];
    }

    public void EndDialogue()
    {
        isTalking = false;
        currentLine = -1;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (animator != null && !string.IsNullOrEmpty(idleTriggerName))
            animator.SetTrigger(idleTriggerName);
    }
}