using UnityEngine;
using DialogueEditor;

public class NPCInteraction : MonoBehaviour
{
    public NPCConversation conversation;

    void Update()
    {
        transform.LookAt(Camera.main.transform);
    }

    public void StartDialogue()
    {
        ConversationManager.Instance.StartConversation(conversation);
    }
}