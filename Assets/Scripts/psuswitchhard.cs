using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PSUSwitchHard : MonoBehaviour
{
    [Header("State")]
    public bool isOn = true;

    [Header("Visual (optional)")]
    public Transform switchVisual;
    public Vector3 onRotation = new Vector3(0, 0, -15);
    public Vector3 offRotation = new Vector3(0, 0, 15);

    [Header("Power Button Link")]
    public PSUPowerButtonHard powerButton;

    [Header("Checklist Integration - Turning OFF (safety step)")]
    public SequentialChecklist checklist;
    public int offGroupIndex = 1;
    public int offObjectiveIndex = 0;

    [Header("Checklist Integration - Turning back ON")]
    public int onGroupIndex = 3;
    public int onObjectiveIndex = 0;

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
        if (powerButton != null)
            powerButton.isSwitchOn = isOn;
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        isOn = !isOn;
        ApplyVisual();

        if (toggleSound != null)
            toggleSound.Play();

        if (powerButton != null)
            powerButton.isSwitchOn = isOn;

        if (checklist == null) return;

        if (!isOn && checklist.IsCurrentStep(offGroupIndex, offObjectiveIndex))
        {
            checklist.CompleteObjective(offGroupIndex, offObjectiveIndex);
        }
        else if (isOn && checklist.IsCurrentStep(onGroupIndex, onObjectiveIndex))
        {
            checklist.CompleteObjective(onGroupIndex, onObjectiveIndex);
        }
    }

    private void ApplyVisual()
    {
        if (switchVisual == null) return;
        switchVisual.localEulerAngles = isOn ? onRotation : offRotation;
    }
}