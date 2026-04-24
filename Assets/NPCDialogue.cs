using UnityEngine;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    public string message = "Do you want to learn Ram Testing?";

    public void ShowDialogue()
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = message;
    }

    public void OnYesClicked()
    {
        dialoguePanel.SetActive(false);
    }
}