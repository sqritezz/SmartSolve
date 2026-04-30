using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCClick : MonoBehaviour
{
    public GameObject dialogueUI;
    public TMP_Text dialogueText;

    void OnMouseDown()
    {
        dialogueUI.SetActive(true);
        dialogueText.text = "Hello traveler!";
    }
}