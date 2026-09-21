using UnityEngine;
using TMPro;

// Coordinates the full tutorial flow:
// intro dialogue -> plug the cable -> follow-up dialogue unlocks -> real game begins.
public class TutorialFlowManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject tutorialPanel;
    public GameObject objectivesPanel;

    [Tooltip("Optional: the Tutorial Panel's text, so we can update it once the cable is plugged in")]
    public TMP_Text tutorialPanelText;
    [TextArea(1, 2)]
    public string cablePluggedMessage = "TUTORIAL - Great! Now go speak to the NPC again.";

    [Header("Azer's three dialogue components")]
    public NPCDialogue introDialogue;
    public NPCDialogue followUpDialogue;
    public NPCDialogue realDialogue;

    private void Start()
    {
        // Starting state: tutorial intro phase
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (objectivesPanel != null) objectivesPanel.SetActive(false);

        if (introDialogue != null) introDialogue.isActiveDialogue = true;
        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = false;
        if (realDialogue != null) realDialogue.isActiveDialogue = false;
    }

    // Wire this to CableSnap's onPluggedIn event.
    public void OnCablePlugged()
    {
        if (introDialogue != null) introDialogue.isActiveDialogue = false;
        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = true;

        if (tutorialPanelText != null)
            tutorialPanelText.text = cablePluggedMessage;
    }

    // Wire this to the FOLLOW-UP dialogue's onDialogueFinished event.
    public void OnTutorialComplete()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (objectivesPanel != null) objectivesPanel.SetActive(true);

        if (followUpDialogue != null) followUpDialogue.isActiveDialogue = false;
        if (realDialogue != null) realDialogue.isActiveDialogue = true;
    }
}