using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the PSU's physical rocker switch object.
// Requires an XRSimpleInteractable + Collider on the same object.
public class PSUSwitch : MonoBehaviour
{
    [Header("State")]
    public bool isOn = false;

    [Header("Visual (optional)")]
    [Tooltip("Rotates slightly to show the flip -- leave empty to skip")]
    public Transform switchVisual;
    public Vector3 onRotation = new Vector3(0, 0, -15);
    public Vector3 offRotation = new Vector3(0, 0, 15);

    [Header("Power Button Link")]
    public PSUPowerButtonMedium powerButton;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int groupIndex = 1;
    public int objectiveIndex = 0;

    [Header("Audio")]
    public AudioSource toggleSound;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);
    }

    private void Start()
    {
        ApplyVisual();
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        isOn = !isOn;
        ApplyVisual();

        if (toggleSound != null)
            toggleSound.Play();

        if (powerButton != null)
            powerButton.isSwitchOn = isOn;

        if (isOn && checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void ApplyVisual()
    {
        if (switchVisual == null) return;
        switchVisual.localEulerAngles = isOn ? onRotation : offRotation;
    }
}