using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardBrokenRam : MonoBehaviour
{
    public HardPCManager hardPCManager;

    [Tooltip("All possible slots in the scene -- whichever one is currently holding this RAM will clear itself automatically")]
    public HardRamSlot[] allSlots;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 1;
    public int objectiveIndex = 0;

    private XRGrabInteractable grab;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        transform.SetParent(null, true);

        if (allSlots != null)
        {
            foreach (var slot in allSlots)
            {
                if (slot != null)
                    slot.ClearSlotIfHolding(gameObject);
            }
        }

        if (hardPCManager != null)
            hardPCManager.BrokenRamRemoved();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}