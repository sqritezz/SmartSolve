using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

// Put this on any object that should be CLICKED with the trigger, not grabbed
// (power buttons, PSU switches, CPU lid, NPC, doorbell button, fan button...).
// It needs the object's XR Simple Interactable (or any XR interactable).
//  - Grip can no longer press it.
//  - The ray still highlights it (hover/descriptions keep working).
//  - TriggerClicker presses it when the player pulls the trigger on it.
// Your existing scripts that listen to selectEntered keep working unchanged.
public class ClickOnly : MonoBehaviour, IXRSelectFilter
{
    [Tooltip("Leave empty to use the interactable on this object")]
    public XRBaseInteractable interactable;

    public bool canProcess => isActiveAndEnabled;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null) interactable.selectFilters.Add(this);
    }

    private void OnDisable()
    {
        if (interactable != null) interactable.selectFilters.Remove(this);
    }

    // Blocks normal (grip) selection
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable target)
    {
        return false;
    }

    // Called by TriggerClicker
    public void Click(IXRSelectInteractor interactor)
    {
        if (interactable == null || !interactable.isActiveAndEnabled) return;

        // Respect the objective lock (warns if it's not this object's step yet)
        ObjectiveLock objectiveLock = GetComponent<ObjectiveLock>();
        if (objectiveLock != null && !objectiveLock.CheckAndWarn()) return;

        var enterArgs = new SelectEnterEventArgs
        {
            interactorObject = interactor,
            interactableObject = interactable,
            manager = interactable.interactionManager
        };
        interactable.selectEntered.Invoke(enterArgs);

        var exitArgs = new SelectExitEventArgs
        {
            interactorObject = interactor,
            interactableObject = interactable,
            manager = interactable.interactionManager,
            isCanceled = false
        };
        interactable.selectExited.Invoke(exitArgs);
    }
}