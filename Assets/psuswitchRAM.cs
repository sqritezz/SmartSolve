using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Generic, reusable power/PSU switch -- works across ANY level (RAM Easy/
// Medium/Hard, PSU Easy/Medium/Hard, etc.) without modification, since it
// no longer references a specific PowerButton script type. Instead, wire
// whatever that level needs into OnTurnedOn / OnTurnedOff below.
public class PSUSwitch1 : MonoBehaviour
{
    // Lets other scripts (RamGrab, case-opening, etc.) check whether it's
    // safe to touch internals: PSUSwitch.Instance.IsOff
    public static PSUSwitch1 Instance;
    public bool IsOff => !isOn;

    [Header("State")]
    public bool isOn = false;

    [Header("Visual (optional)")]
    [Tooltip("Rotates to show the flip -- can be this same object, or a separate child mesh")]
    public Transform switchVisual;
    public Vector3 onRotation = new Vector3(0, 0, -15);
    public Vector3 offRotation = new Vector3(0, 0, 15);

    [Header("Events -- hook up whatever THIS level needs")]
    [Tooltip("Runs whenever the switch is turned ON")]
    public UnityEvent onTurnedOn;
    [Tooltip("Runs whenever the switch is turned OFF")]
    public UnityEvent onTurnedOff;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Check this if turning the switch ON completes the objective (e.g. PSU Medium). Uncheck it if turning OFF completes it instead (e.g. a safety step before opening the case).")]
    public bool completeOnTurningOn = true;
    public int groupIndex = 1;
    public int objectiveIndex = 0;

    [Header("Audio")]
    public AudioSource toggleSound;

    [Header("Requires talking to NPC first")]
    [Tooltip("If true, the switch can't be toggled until MarkNpcTalkedTo() has been called")]
    public bool requireNpcFirst = false;
    private bool npcTalkedTo = false;
    [TextArea(1, 2)]
    public string blockedMessage = "Talk to the technician first before touching the PSU switch.";

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        Instance = this;

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
        if (requireNpcFirst && !npcTalkedTo)
        {
            Debug.Log(blockedMessage); // swap for your hint/mentor popup if you have one
            return;
        }

        isOn = !isOn;
        ApplyVisual();

        if (toggleSound != null)
            toggleSound.Play();

        if (isOn)
            onTurnedOn?.Invoke();
        else
            onTurnedOff?.Invoke();

        bool shouldComplete = completeOnTurningOn ? isOn : !isOn;
        if (shouldComplete && checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
        {
            checklist.CompleteObjective(groupIndex, objectiveIndex);
        }
    }

    // Call this from NPCDialogue once the conversation finishes
    public void MarkNpcTalkedTo()
    {
        npcTalkedTo = true;
    }

    private void ApplyVisual()
    {
        if (switchVisual == null) return;
        switchVisual.localEulerAngles = isOn ? onRotation : offRotation;
    }
}