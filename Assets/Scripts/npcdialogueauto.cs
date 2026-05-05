using System.Collections;
using UnityEngine;
using TMPro;

public class NPCDialogueAuto : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Animator npcAnimator;

    [TextArea(2, 4)]
    public string[] dialogueLines;

    public float startDelay = 2f;
    public float timePerLine = 4f;
    public float endDelay = 2f;

    private bool isPlaying = false;

    void Start()
    {
        dialoguePanel.SetActive(false);
        StartCoroutine(StartDialogueAfterDelay());
    }

    IEnumerator StartDialogueAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        StartCoroutine(PlayDialogue());
    }

    IEnumerator PlayDialogue()
    {
        if (isPlaying) yield break;

        isPlaying = true;

        if (npcAnimator != null)
            npcAnimator.SetBool("IsTalking", true);

        dialoguePanel.SetActive(true);

        foreach (string line in dialogueLines)
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(timePerLine);
        }

        yield return new WaitForSeconds(endDelay);

        dialoguePanel.SetActive(false);

        if (npcAnimator != null)
            npcAnimator.SetBool("IsTalking", false);

        isPlaying = false;
    }
}