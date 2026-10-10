using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the PSU's physical rocker switch object.
// Requires an XRSimpleInteractable + Collider on the same object.
//
// Checklist steps:
//   Checklist Integration      -> e.g. "Turn OFF the PSU switch"
//   Second Checklist Step      -> e.g. "Turn the PSU switch back ON"
//   More Checklist Steps       -> as many extra off/on steps as the level needs
//                                 (e.g. RAM Hard: off, on, off, on)
public class PSUSwitch1 : MonoBehaviour
{
    // Lets other scripts check whether it's safe to touch internals
    public static PSUSwitch1 Instance;
    public bool IsOff => !isOn;

    [Header("State")]
    public bool isOn = false;

    [Header("Visual (optional)")]
    public Transform switchVisual;
    public Vector3 onRotation = new Vector3(0, 0, -15);
    public Vector3 offRotation = new Vector3(0, 0, 15);

    [Header("Events -- hook up whatever THIS level needs")]
    public UnityEvent onTurnedOn;
    public UnityEvent onTurnedOff;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Checked = turning ON completes this step. Unchecked = turning OFF completes it.")]
    public bool completeOnTurningOn = true;
    public int groupIndex = 1;
    public int objectiveIndex = 0;

    [Header("Second Checklist Step (optional)")]
    [Tooltip("Tick to use a second step, e.g. turning the switch back ON after the RAM reseat")]
    public bool useSecondStep = false;
    [Tooltip("Checked = turning ON completes this step. Unchecked = turning OFF completes it.")]
    public bool secondCompleteOnTurningOn = true;
    public int secondGroupIndex = 0;
    public int secondObjectiveIndex = 0;

    [System.Serializable]
    public class SwitchStep
    {
        [Tooltip("Just a note for yourself, e.g. 'Turn off again'")]
        public string label;
        [Tooltip("Checked = turning ON completes this step. Unchecked = turning OFF completes it.")]
        public bool completeOnTurningOn = true;
        public int groupIndex;
        public int objectiveIndex;
    }

    [Header("More Checklist Steps (optional)")]
    [Tooltip("Press + for each extra off/on step")]
    public SwitchStep[] moreSteps = new SwitchStep[0];

    [Header("Audio")]
    public AudioSource toggleSound;

    [Header("Requires talking to NPC first")]
    [Tooltip("If true, the switch can't be toggled until MarkNpcTalkedTo() has been called")]
    public bool requireNpcFirst = false;
    private bool npcTalkedTo = false;
    [TextArea(1, 2)]
    public string blockedMessage = "Talk to the NPC first before touching the PSU switch.";

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
            if (ObjectiveWarning.Instance != null)
                ObjectiveWarning.Instance.Show(blockedMessage);
            else
                Debug.Log(blockedMessage);
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

        if (checklist == null) return;

        // Every step that is the CURRENT checklist step and matches
        // this direction (on/off) gets completed
        TryComplete(completeOnTurningOn, groupIndex, objectiveIndex);

        if (useSecondStep)
            TryComplete(secondCompleteOnTurningOn, secondGroupIndex, secondObjectiveIndex);

        if (moreSteps != null)
            foreach (var step in moreSteps)
                if (step != null)
                    TryComplete(step.completeOnTurningOn, step.groupIndex, step.objectiveIndex);
    }

    private void TryComplete(bool onTurningOn, int group, int objective)
    {
        bool matches = onTurningOn ? isOn : !isOn;
        if (matches && checklist.IsCurrentStep(group, objective))
            checklist.CompleteObjective(group, objective);
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