using UnityEngine;
using DialogueEditor;

public class VRNPCTrigger : MonoBehaviour
{
    public NPCConversation conversation;

    private bool playerNear = false;

    void Update()
    {
        if (playerNear && Input.GetButtonDown("Fire1")) // or VR input
        {
            ConversationManager.Instance.StartConversation(conversation);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        playerNear = false;
    }
}