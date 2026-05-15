using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardBrokenRam : MonoBehaviour
{
    public HardPCManager hardPCManager;
    public HardRamSlot ramSlot;

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

        if (ramSlot != null)
            ramSlot.ClearSlot();

        if (hardPCManager != null)
            hardPCManager.BrokenRamRemoved();
    }
}