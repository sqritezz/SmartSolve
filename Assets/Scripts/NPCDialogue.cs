using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// Attach this directly to the NPC GameObject.
// Requires an XRSimpleInteractable on the same object (or a child) so the
// player can point their ray at the NPC and pull the trigger to talk.
//
// IMPORTANT: if this NPC has more than one NPCDialogue component on it
// (e.g. one for a tutorial intro, one for the real briefing), only ONE
// should have isActiveDialogue = true at any given moment -- otherwise
// both will respond to the same click. A flow manager script flips this
// flag when switching phases.
[RequireComponent(typeof(XRSimpleInteractable))]
public class NPCDialogue : MonoBehaviour
{
    [Header("Phase Gating")]
    [Tooltip("Only one NPCDialogue component on this NPC should be active at a time. Inactive ones ignore clicks entirely.")]
    public bool isActiveDialogue = true;

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

    [Header("Checklist Integration")]
    [Tooltip("Completed once the player has finished this NPC's dialogue")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;

    [Header("Gate the PSU switch on this conversation (optional)")]
    [Tooltip("If assigned, this NPC talking marks the PSU switch as unlocked")]
    public PSUSwitch1 psuSwitchToUnlock;

    [Header("Runs when this dialogue finishes (optional)")]
    [Tooltip("Hook up phase transitions here, e.g. a flow manager's OnTutorialDialogueFinished()")]
    public UnityEngine.Events.UnityEvent onDialogueFinished;

    private int currentLine = -1;
    private bool isTalking = false;
    private bool hasCompletedOnce = false;
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
        if (!isActiveDialogue) return; // this dialogue isn't the current phase -- ignore the click

        if (!isTalking)
            StartDialogue();
        else
            AdvanceLine();
    }

    public void StartDialogue()
    {
        if (!isActiveDialogue) return;

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

        if (!hasCompletedOnce)
        {
            hasCompletedOnce = true;

            if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
                checklist.CompleteObjective(groupIndex, objectiveIndex);

            if (psuSwitchToUnlock != null)
                psuSwitchToUnlock.MarkNpcTalkedTo();

            onDialogueFinished?.Invoke();
        }
    }
}