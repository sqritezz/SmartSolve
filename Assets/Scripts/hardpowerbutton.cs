using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HardPowerButton : MonoBehaviour
{
    public HardPCManager hardPCManager;

    private XRBaseInteractable interactable;

    void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);
    }

    void OnPressed(SelectEnterEventArgs args)
    {
        if (hardPCManager != null)
            hardPCManager.PressPowerButton();
    }
}