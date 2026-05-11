using UnityEngine;
using DialogueEditor;

public class StepByStepDialogue : MonoBehaviour
{
    public NPCConversation step1;
    public NPCConversation step2;
    public NPCConversation step3;
    public NPCConversation step4;

    void Start()
    {
        ConversationManager.Instance.StartConversation(step1);
    }

    public void Step1Done()
    {
        ConversationManager.Instance.StartConversation(step2);
    }

    public void Step2Done()
    {
        ConversationManager.Instance.StartConversation(step3);
    }

    public void Step3Done()
    {
        ConversationManager.Instance.StartConversation(step4);
    }
}