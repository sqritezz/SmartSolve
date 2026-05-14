using UnityEngine;
using UnityEngine.InputSystem;

public class VRSettingsToggle : MonoBehaviour
{
    public GameObject settingsPanel;

    public InputActionProperty toggleSettingsAction;

    private bool isOpen = false;

    void Update()
    {
        if (toggleSettingsAction.action.WasPressedThisFrame())
        {
            isOpen = !isOpen;
            settingsPanel.SetActive(isOpen);
        }
    }
}