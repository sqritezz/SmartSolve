using UnityEngine;
using DialogueEditor;

public class InteractStartDialogue : MonoBehaviour
{
    public NPCConversation conversation;
    private bool started = false;

    public void Interact()
    {
        if (!started)
        {
            started = true;
            ConversationManager.Instance.StartConversation(conversation);
        }
    }
}