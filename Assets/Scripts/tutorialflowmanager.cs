using UnityEngine;
using TMPro;

// Coordinates the full tutorial flow:
// intro dialogue -> practice task -> follow-up dialogue unlocks -> real game begins.
public class TutorialFlowManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject tutorialPanel;
    public GameObject objectivesPanel;

    [Tooltip("Optional: the Tutorial Panel's text, so we can update it once the practice task is done")]
    public TMP_Text tutorialPanelText;
    [TextArea(1, 2)]
    public string taskDoneMessage = "TUTORIAL - Great! Now go speak to the NPC again.";

    [Header("Azer's three dialogue components")]
    public NPCDialogue introDialogue;
    public NPCDialogue followUpDialogue;
    public NPCDialogue realDialogue;

    // Hard lock: once true, the real dialogue can never be re-locked or
    // skipped around, and OnTutorialComplete can never fire more than once.
    private bool practiceTaskDone = false;
    private bool tutorialFullyComplete = false;

    private void Start()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (objectivesPanel != null) objectivesPanel.SetActive(false);

        if (introDialogue != null) introDialogue.isActiveDialogue = true;
        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = false;
        if (realDialogue != null) realDialogue.isActiveDialogue = false;
    }

    // Wire this to the practice task's completion event (CableSnap's
    // onPluggedIn, PlateClean's onCleaned, or PracticeRamSlotSwap's
    // onSuccess -- same method works for all three).
    public void OnCablePlugged()
    {
        if (practiceTaskDone) return; // can't re-trigger
        practiceTaskDone = true;

        if (introDialogue != null) introDialogue.isActiveDialogue = false;
        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = true;

        if (tutorialPanelText != null)
            tutorialPanelText.text = taskDoneMessage;
    }

    // Wire this ONLY to the follow-up dialogue's onDialogueFinished.
    // Guarded so it physically cannot do anything unless the practice
    // task was actually completed first, and cannot fire twice.
    public void OnTutorialComplete()
    {
        if (tutorialFullyComplete) return; // already done, ignore repeats
        if (!practiceTaskDone) return;      // practice task skipped -- refuse to unlock

        tutorialFullyComplete = true;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (objectivesPanel != null) objectivesPanel.SetActive(true);

        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = false;
        if (realDialogue != null) realDialogue.isActiveDialogue = true;
    }
}