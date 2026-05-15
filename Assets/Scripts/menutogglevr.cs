using UnityEngine;
using UnityEngine.InputSystem;

public class MenuToggleVR : MonoBehaviour
{
    public GameObject mainMenu;
    public InputActionReference toggleButton;

    private void OnEnable()
    {
        toggleButton.action.Enable();
        toggleButton.action.performed += ToggleMenu;
    }

    private void OnDisable()
    {
        toggleButton.action.performed -= ToggleMenu;
        toggleButton.action.Disable();
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        mainMenu.SetActive(!mainMenu.activeSelf);
        Debug.Log("VR MENU BUTTON PRESSED");
    }
}