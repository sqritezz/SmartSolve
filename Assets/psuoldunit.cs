using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PSUOldUnit : MonoBehaviour
{
    public PSUPowerButtonHard powerButton;
    public PSUSlot psuSlot;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 2;
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

        if (psuSlot != null)
            psuSlot.ClearSlot();

        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }
}