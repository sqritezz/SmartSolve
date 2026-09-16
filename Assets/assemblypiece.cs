using UnityEngine;

// Attach this to each full reward item prefab/object in your build room scene
// (Monitor, Mouse, Keyboard, CPU, Desk, Chair). The partName MUST exactly match
// the name used in RewardManager.UnlockPart(...) and LevelCompleteManager's
// "Reward Part Name" field.
public class AssemblyPiece : MonoBehaviour
{
    public string partName;

    [HideInInspector] public bool isSnapped = false;

    // Called by AssemblySlot once this piece locks into its correct spot.
    public void OnSnapped()
    {
        isSnapped = true;
        Debug.Log(partName + " snapped into place!");
    }
}