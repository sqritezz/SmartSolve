using UnityEngine;

// Same idea as RamSlotRandomizer, but for Medium's RamSnapMedium slots.
// Attach this to MediumSpawnPoint (RAM).
public class RamSlotRandomizerMedium : MonoBehaviour
{
    [Header("Candidate Slots")]
    public RamSnapMedium[] candidateSlots;

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
            Debug.LogWarning(gameObject.name + ": RamSlotRandomizerMedium has no candidateSlots assigned.");
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