using UnityEngine;
using DialogueEditor;

public class NPCInteraction : MonoBehaviour
{
    public NPCConversation conversation;

    public void StartDialogue()
    {
        ConversationManager.Instance.StartConversation(conversation);
    }
}