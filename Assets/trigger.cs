using UnityEngine;

public class NPCTrigger : MonoBehaviour
{
    public NPCController npc;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            npc.Talk();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            npc.StopTalk();
        }
    }
}