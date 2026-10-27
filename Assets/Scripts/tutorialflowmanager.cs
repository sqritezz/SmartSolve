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
    [TextArea(1, 2)]
    [Tooltip("Shown instead when the game language is Filipino")]
    public string taskDoneMessageFilipino = "Ayos! Ngayon, kausapin muli ang iyong kaibigan.";

    [Header("Azer's three dialogue components")]
    public NPCDialogue introDialogue;
    public NPCDialogue followUpDialogue;
    public NPCDialogue realDialogue;

    // Hard lock: once true, the real dialogue can never be re-locked or
    // skipped around, and OnTutorialComplete can never fire more than once.
    private bool practiceTaskDone = false;
    private bool tutorialFullyComplete = false;

    private void OnEnable() { LanguageManager.OnLanguageChanged += RefreshText; }
    private void OnDisable() { LanguageManager.OnLanguageChanged -= RefreshText; }

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

        RefreshText();
    }

    // Shows the "task done" message in the current language
    // (also updates if the language is changed while it's showing)
    private void RefreshText()
    {
        if (!practiceTaskDone || tutorialFullyComplete || tutorialPanelText == null) return;
        tutorialPanelText.text = LanguageManager.Pick(taskDoneMessage, taskDoneMessageFilipino);
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