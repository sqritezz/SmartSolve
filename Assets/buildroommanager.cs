using UnityEngine;

// Attach this to a manager object in your "build room" scene.
// Fill in the pairs below -- pieceObject should start INACTIVE in the scene
// (sitting on a shelf, wherever). This script activates only the ones the
// player has actually earned, based on RewardManager.
public class BuildRoomManager : MonoBehaviour
{
    [System.Serializable]
    public class PieceEntry
    {
        [Tooltip("Must exactly match the part name used in RewardManager / LevelCompleteManager")]
        public string partName;

        [Tooltip("The reward object in the scene (starts inactive) that gets revealed once unlocked")]
        public GameObject pieceObject;
    }

    public PieceEntry[] pieces;

    private void Start()
    {
        if (RewardManager.Instance == null)
        {
            Debug.LogWarning("No RewardManager found in scene -- did you add it to your first/main scene?");
            return;
        }

        foreach (var entry in pieces)
        {
            if (entry.pieceObject == null) continue;

            bool unlocked = RewardManager.Instance.IsPartUnlocked(entry.partName);
            entry.pieceObject.SetActive(unlocked);
        }
    }
}