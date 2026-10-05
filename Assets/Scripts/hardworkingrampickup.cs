using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the "working ram" object on the table.
public class HardWorkingRamPickup : MonoBehaviour
{
    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 2;
    public int objectiveIndex = 1;

    private XRGrabInteractable grab;
    private bool hasBeenPickedUp = false;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hasBeenPickedUp) return;
        hasBeenPickedUp = true;

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}