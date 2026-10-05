using UnityEngine;

// Attach this to a manager object in the RAM stage (e.g. EasySpawnPoint (RAM)).
// Drag all the RamSnap-enabled slot objects into candidateSlots -- one is
// picked at random each time the level starts, and only that slot will
// accept the RAM fix. The others become "wrong slot" traps.
public class RamSlotRandomizer : MonoBehaviour
{
    [Header("Candidate Slots")]
    [Tooltip("All the RamSnap components for this stage's slots (e.g. RAM slot, RAM slot 1, RAM slot 2)")]
    public RamSnap[] candidateSlots;

    [Header("Debug")]
    [Tooltip("Force a specific slot index for testing. Set to -1 for true random.")]
    public int forceSlotIndex = -1;

    private void Start()
    {
        ChooseActiveSlot();
    }

    public void ChooseActiveSlot()
    {
        if (candidateSlots == null || candidateSlots.Length == 0)
        {
            Debug.LogWarning(gameObject.name + ": RamSlotRandomizer has no candidateSlots assigned.");
            return;
        }

        int chosen = forceSlotIndex >= 0 && forceSlotIndex < candidateSlots.Length
            ? forceSlotIndex
            : Random.Range(0, candidateSlots.Length);

        for (int i = 0; i < candidateSlots.Length; i++)
        {
            if (candidateSlots[i] != null)
                candidateSlots[i].isActiveSlot = (i == chosen);
        }
    }
}