using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Press a controller button to show / hide a panel AND/OR run anything you like
// (On Pressed event). No Input Actions setup needed - the button is chosen here.
// Put it on an object that is ALWAYS active (e.g. an empty "Controller Buttons"),
// NOT on the panel itself (a hidden panel can't listen for the button).
// Add one component per button / purpose.
public class ControllerButtonPanel : MonoBehaviour
{
    public enum ControllerButton
    {
        // Face buttons
        X_LeftHand,
        Y_LeftHand,
        A_RightHand,
        B_RightHand,
        // Menu (Meta: only the LEFT controller has one you can use)
        Menu_LeftHand,
        // Thumbstick pressed down (click)
        ThumbstickClick_LeftHand,
        ThumbstickClick_RightHand,
        // Careful: these are also used to grab / click things
        Trigger_LeftHand,
        Trigger_RightHand,
        Grip_LeftHand,
        Grip_RightHand
    }

    [Header("Button")]
    public ControllerButton button = ControllerButton.X_LeftHand;

    [Header("Panel (optional)")]
    [Tooltip("The panel to show / hide. Leave empty if this button only runs On Pressed.")]
    public GameObject panel;
    [Tooltip("On = press to open, press again to close. Off = press only opens it.")]
    public bool toggle = true;
    [Tooltip("Optional: other panels to hide when this one opens (so they don't overlap)")]
    public GameObject[] hideWhenOpened;

    [Header("Placement (optional)")]
    [Tooltip("Move the panel in front of the player each time it opens")]
    public bool placeInFrontOfPlayer = false;
    public float distance = 0.8f;
    public float heightOffset = -0.1f;

    [Serializable]
    public class BackStep
    {
        [Tooltip("When THIS panel is showing...")]
        public GameObject whenThisIsOpen;
        [Tooltip("...hide it and show THIS one instead (leave empty to just close it)")]
        public GameObject goBackTo;
        [Tooltip("Optional: also run this (e.g. teleport back to the menu)")]
        public UnityEvent alsoDo;
    }

    [Header("BACK without Back buttons (optional)")]
    [Tooltip("List your panels here. Pressing the button closes the first one that is open and goes back to the panel you chose. Put the 'deepest' panels at the top.")]
    public BackStep[] backSteps;

    [Header("Press a UI Button (optional)")]
    [Tooltip("UI Buttons this controller button can press. Only the first one that is VISIBLE right now gets pressed, so you can list every panel's Back button here.")]
    public Button[] uiButtonsToPress;

    [Header("Other Actions (optional)")]
    [Tooltip("Runs every time the button is pressed (e.g. teleport home, use hint, play a sound)")]
    public UnityEvent onPressed;
    [Tooltip("Runs when the panel opens")]
    public UnityEvent onOpened;
    [Tooltip("Runs when the panel closes")]
    public UnityEvent onClosed;

    [Header("Audio (optional)")]
    public AudioSource openSound;
    public AudioSource closeSound;

    private InputAction action;

    private void Awake()
    {
        action = new InputAction("ControllerButton", InputActionType.Button, GetBinding(button));
    }

    private void OnEnable()
    {
        action.performed += OnButtonPressed;
        action.Enable();
    }

    private void OnDisable()
    {
        action.performed -= OnButtonPressed;
        action.Disable();
    }

    private void OnDestroy()
    {
        action.Dispose();
    }

    private void OnButtonPressed(InputAction.CallbackContext ctx)
    {
        onPressed?.Invoke();

        if (GoBack()) return;          // went back one panel - done
        if (PressVisibleUIButton()) return;

        if (panel == null) return;

        bool open = !panel.activeSelf;
        if (!toggle) open = true;

        if (open) Open();
        else Close();
    }

    // Closes the first listed panel that is open and shows the one before it
    public bool GoBack()
    {
        if (backSteps == null) return false;

        foreach (BackStep step in backSteps)
        {
            if (step == null || step.whenThisIsOpen == null) continue;
            if (!step.whenThisIsOpen.activeInHierarchy) continue;

            step.whenThisIsOpen.SetActive(false);
            if (step.goBackTo != null) step.goBackTo.SetActive(true);
            step.alsoDo?.Invoke();

            if (closeSound != null) closeSound.Play();
            return true; // only one step back per press
        }
        return false;
    }

    // Presses the first listed UI Button that is on screen and clickable
    private bool PressVisibleUIButton()
    {
        if (uiButtonsToPress == null) return false;

        foreach (Button b in uiButtonsToPress)
        {
            if (b == null) continue;
            if (!b.gameObject.activeInHierarchy || !b.IsInteractable()) continue;

            b.onClick.Invoke();
            return true; // only one per press
        }
        return false;
    }

    public void Open()
    {
        if (panel == null) return;

        if (hideWhenOpened != null)
            foreach (GameObject other in hideWhenOpened)
                if (other != null && other != panel) other.SetActive(false);

        if (placeInFrontOfPlayer && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = cam.forward;

            Vector3 pos = cam.position + flatForward * distance + Vector3.up * heightOffset;
            panel.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.position, Vector3.up));
        }

        bool wasOpen = panel.activeSelf;
        panel.SetActive(true);
        if (!wasOpen)
        {
            if (openSound != null) openSound.Play();
            onOpened?.Invoke();
        }
    }

    public void Close()
    {
        if (panel == null || !panel.activeSelf) return;
        panel.SetActive(false);
        if (closeSound != null) closeSound.Play();
        onClosed?.Invoke();
    }

    private static string GetBinding(ControllerButton b)
    {
        switch (b)
        {
            case ControllerButton.X_LeftHand: return "<XRController>{LeftHand}/{PrimaryButton}";
            case ControllerButton.Y_LeftHand: return "<XRController>{LeftHand}/{SecondaryButton}";
            case ControllerButton.A_RightHand: return "<XRController>{RightHand}/{PrimaryButton}";
            case ControllerButton.B_RightHand: return "<XRController>{RightHand}/{SecondaryButton}";
            case ControllerButton.Menu_LeftHand: return "<XRController>{LeftHand}/{MenuButton}";
            case ControllerButton.ThumbstickClick_LeftHand: return "<XRController>{LeftHand}/{Primary2DAxisClick}";
            case ControllerButton.ThumbstickClick_RightHand: return "<XRController>{RightHand}/{Primary2DAxisClick}";
            case ControllerButton.Trigger_LeftHand: return "<XRController>{LeftHand}/{TriggerButton}";
            case ControllerButton.Trigger_RightHand: return "<XRController>{RightHand}/{TriggerButton}";
            case ControllerButton.Grip_LeftHand: return "<XRController>{LeftHand}/{GripButton}";
            case ControllerButton.Grip_RightHand: return "<XRController>{RightHand}/{GripButton}";
        }
        return "<XRController>{LeftHand}/{PrimaryButton}";
    }
}