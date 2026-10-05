using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// PSU MEDIUM - "Is it really the PSU?"
// Attach to the PC POWER BUTTON (needs collider + XR Simple Interactable).
// Replaces PowerButtonMedium on this object.
//
// Checks each step in order and ticks the checklist when it's done:
//   1  Press the power button (nothing happens)
//   2  Turn off the PSU switch
//   3  Unplug the 24-pin from the motherboard
//   4  Bend the paperclip with the pliers
//   5  Insert the paperclip into the green and black holes
//   6  Turn on the PSU switch -> PSU fan spins = PSU works
//   7  Turn off the PSU switch and remove the paperclip
//   8  Plug the 24-pin back in
//   9  Move the power button cable to PWR SW (pins 6-8)
//   10 Turn on the PSU switch
//   11 Press the power button -> PC starts
//
// Steps are checked against the CURRENT state, so doing something early is fine:
// it just ticks as soon as that step comes up.
// IMPORTANT: leave the PSU Switch's own Checklist field EMPTY.
public class PSUMediumManager : MonoBehaviour
{
    public enum Step
    {
        PressPowerFirst, SwitchOff, Unplug24Pin, BendClip, InsertClip, TestPSU,
        SwitchOffRemoveClip, Replug24Pin, FixPowerCable, SwitchOn, PressPowerFinal, Done
    }

    [Header("Screen")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Header("Parts")]
    public PSUSwitch1 psuSwitch;
    public PullOutConnector connector24Pin;
    public PaperclipSlot paperclipSlot;
    public PaperclipBender paperclipBender;
    public FrontPanelPlug powerSwitchPlug;

    [Header("PSU Fan")]
    [Tooltip("The PSU's fan blades - spins during the paperclip test and when the PC runs")]
    public Transform psuFan;
    public Vector3 spinAxis = Vector3.forward;
    public float spinSpeed = 900f;
    [Tooltip("Optional looping fan sound (3D, Loop on, Play On Awake off)")]
    public AudioSource fanSound;

    [Header("Audio")]
    public AudioSource beepSound;
    public AudioSource successSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-11, in order (group 0 is 'speak to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPowerFirst;
    public bool pressedOnce = false;
    public bool pcRunning = false;
    public bool psuTestPassed = false;

    private XRBaseInteractable interactable;

    private bool SwitchOn => psuSwitch == null || !psuSwitch.IsOff;
    private bool Seated => connector24Pin == null || connector24Pin.isSeated;
    private bool ClipIn => paperclipSlot != null && paperclipSlot.hasClip;

    // PSU fan spins when the paperclip test is running, or when the PC is on
    private bool FanSpinning => SwitchOn && ((!Seated && ClipIn) || pcRunning);

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(args => PressPowerButton());

        if (psuSwitch != null)
            psuSwitch.onTurnedOff.AddListener(OnSwitchTurnedOff);
    }

    private void Start()
    {
        ShowScreen(false);
    }

    private void Update()
    {
        // PSU fan
        bool spinning = FanSpinning;
        if (spinning && psuFan != null)
            psuFan.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
        if (fanSound != null)
        {
            if (spinning && !fanSound.isPlaying) fanSound.Play();
            else if (!spinning && fanSound.isPlaying) fanSound.Stop();
        }

        TryAdvance();
    }

    // ---------- Power button ----------
    public void PressPowerButton()
    {
        if (beepSound != null) beepSound.Play();
        if (pcRunning) return;

        bool canStart = SwitchOn && Seated && !ClipIn &&
                        powerSwitchPlug != null && powerSwitchPlug.IsCorrect;

        if (canStart)
        {
            pcRunning = true;
            ShowScreen(true);
            if (successSound != null) successSound.Play();
            return;
        }

        // Pressing again with power on but nothing fixed counts as a mistake
        if (pressedOnce && SwitchOn && Seated && !ClipIn && performanceTracker != null)
            performanceTracker.RegisterWrongAttempt();

        pressedOnce = true; // nothing happens - that's the symptom
    }

    private void OnSwitchTurnedOff()
    {
        if (pcRunning)
        {
            pcRunning = false;
            ShowScreen(false);
        }
    }

    // ---------- Step checking ----------
    private void TryAdvance()
    {
        if (currentStep == Step.Done) return;

        int stepIndex = (int)currentStep;
        int group = stepIndex < stepGroupIndices.Length ? stepGroupIndices[stepIndex] : -1;

        // Wait until the checklist is showing this step
        if (checklist != null && group >= 0 && !checklist.IsCurrentStep(group, objectiveIndex))
            return;

        if (!IsStepDone(currentStep)) return;

        if (checklist != null && group >= 0)
            checklist.CompleteObjective(group, objectiveIndex);

        currentStep++;
    }

    private bool IsStepDone(Step step)
    {
        switch (step)
        {
            case Step.PressPowerFirst: return pressedOnce;
            case Step.SwitchOff: return !SwitchOn;
            case Step.Unplug24Pin: return !Seated;
            case Step.BendClip: return paperclipBender == null || paperclipBender.isBent;
            case Step.InsertClip: return ClipIn;
            case Step.TestPSU:
                if (FanSpinning && !Seated && ClipIn) psuTestPassed = true;
                return psuTestPassed;
            case Step.SwitchOffRemoveClip: return !SwitchOn && !ClipIn;
            case Step.Replug24Pin: return Seated;
            case Step.FixPowerCable: return powerSwitchPlug != null && powerSwitchPlug.IsCorrect;
            case Step.SwitchOn: return SwitchOn;
            case Step.PressPowerFinal: return pcRunning;
        }
        return false;
    }

    private void ShowScreen(bool on)
    {
        if (screenOn != null) screenOn.SetActive(on);
        if (screenOff != null) screenOff.SetActive(!on);
    }
}