using UnityEngine;

// Put this on the same NPC as NPCKnockAndWalk.
// The NPC only starts knocking when the player is in his room (close enough).
// If the player leaves before opening the door, the knocking pauses,
// and starts again when they come back.
//
// IMPORTANT: untick "Knock On Start" on NPCKnockAndWalk.
public class KnockWhenPlayerNear : MonoBehaviour
{
    public NPCKnockAndWalk npc;

    [Tooltip("Main Camera. Empty = finds it automatically")]
    public Transform player;

    [Tooltip("Player must be within this many meters of the NPC for the knocking to start")]
    public float startDistance = 8f;

    [Tooltip("Seconds to wait after the player arrives before the first knock")]
    public float delay = 1f;

    private float nearSince = -1f;

    private void Awake()
    {
        if (npc == null) npc = GetComponent<NPCKnockAndWalk>();
    }

    private void Update()
    {
        if (npc == null) return;
        if (player == null && Camera.main != null) player = Camera.main.transform;
        if (player == null) return;

        // Door already opened -> knocking is finished for good
        if (npc.doorOpened)
        {
            enabled = false;
            return;
        }

        bool near = Vector3.Distance(player.position, npc.transform.position) <= startDistance;

        if (near)
        {
            if (nearSince < 0f) nearSince = Time.time;

            if (!npc.isKnocking && Time.time - nearSince >= delay)
                npc.StartKnocking();
        }
        else
        {
            nearSince = -1f;

            if (npc.isKnocking)
            {
                if (npc.knockSound != null) npc.knockSound.Stop();
                npc.isKnocking = false;
            }
        }
    }
}