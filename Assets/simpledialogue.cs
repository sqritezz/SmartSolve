using UnityEngine;
using TMPro;

public class SimpleDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [TextArea]
    public string[] lines;

    private int index = 0;
    private bool isActive = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue()
    {
        isActive = true;
        index = 0;
        dialoguePanel.SetActive(true);
        dialogueText.text = lines[index];
    }

    public void NextLine()
    {
        index++;

        if (index < lines.Length)
        {
            dialogueText.text = lines[index];
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        isActive = false;
        dialoguePanel.SetActive(false);
    }

    public bool IsActive()
    {
        return isActive;
    }
}