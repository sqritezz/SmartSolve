using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

// Put ONE on each controller's Ray Interactor object (left and right).
// Pulling the TRIGGER while pointing at an object with ClickOnly presses it.
// Also announces "trigger pressed" to anything listening (e.g. the warning
// screen's "click anywhere to skip").
public class TriggerClicker : MonoBehaviour
{
    // Any trigger press, anywhere
    public static event Action OnAnyTriggerPressed;

    [Tooltip("The trigger action, e.g. XRI RightHand Interaction/Activate (or .../Activate Value)")]
    public InputActionReference triggerAction;

    [Tooltip("Where the ray starts. Leave empty to use this object.")]
    public Transform rayOrigin;

    public float maxDistance = 30f;
    [Tooltip("Layers the click ray can hit")]
    public LayerMask clickLayers = ~0;

    public AudioSource clickSound;

    private IXRSelectInteractor interactor;

    private void Awake()
    {
        if (rayOrigin == null) rayOrigin = transform;
        interactor = GetComponent<IXRSelectInteractor>();
    }

    private void OnEnable()
    {
        if (triggerAction != null) triggerAction.action.Enable();
    }

    private void Update()
    {
        if (triggerAction == null) return;
        if (!triggerAction.action.WasPressedThisFrame()) return;

        OnAnyTriggerPressed?.Invoke();

        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit,
                            maxDistance, clickLayers, QueryTriggerInteraction.Collide))
        {
            ClickOnly target = hit.collider.GetComponentInParent<ClickOnly>();
            if (target != null)
            {
                if (clickSound != null) clickSound.Play();
                target.Click(interactor);
            }
        }
    }
}
