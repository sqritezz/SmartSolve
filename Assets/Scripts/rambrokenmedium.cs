using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardBrokenRam : MonoBehaviour
{
    public HardPCManager hardPCManager;

    private XRGrabInteractable grab;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hardPCManager != null)
            hardPCManager.BrokenRamRemoved();
    }
}