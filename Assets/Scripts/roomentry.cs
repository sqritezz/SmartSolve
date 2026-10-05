using UnityEngine;

// Put this on an empty GameObject with a trigger Collider covering the
// room's entrance/spawn spot. The moment the player's rig enters it,
// Guidelines tracking begins -- so nothing counts as "moved" from the
// initial teleport out of the main menu.
[RequireComponent(typeof(Collider))]
public class RoomEntryDetector : MonoBehaviour
{
    public GuidelineTracker guidelineTracker;
    [Tooltip("Tag used by your XR Origin/player rig's collider")]
    public string playerTag = "Player";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;
        if (guidelineTracker != null)
            guidelineTracker.BeginTracking();
    }
}