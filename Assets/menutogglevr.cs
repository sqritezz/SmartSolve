using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggleVR : MonoBehaviour
{
    public GameObject mainMenu;
    public InputActionProperty toggleButton;

    void OnEnable()
    {
        toggleButton.action.Enable();
    }

    void OnDisable()
    {
        toggleButton.action.Disable();
    }

    void Update()
    {
        if (toggleButton.action.WasPressedThisFrame())
        {
            mainMenu.SetActive(!mainMenu.activeSelf);
        }
    }
}