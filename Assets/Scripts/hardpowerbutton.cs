using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// PSU HARD - "The PSU is dead: replace it"
// Attach to the PC POWER BUTTON (needs collider + XR Simple Interactable).
// Replaces PSUPowerButtonHard on this object.
//
// Steps (checked in order, ticks the checklist):
//   1  Press the power button        -> sparks (dead PSU)
//   2  Turn off the PSU switch
//   3  Unplug the old 24-pin
//   4  Unscrew all PSU screws
//   5  Remove the faulty PSU
//   6  Install the new PSU
//   7  Screw the new PSU in
//   8  Plug the new PSU's 24-pin into the motherboard
//   9  Turn on the NEW PSU's switch
//   10 Press the power button        -> PC starts
// Each PSU has its own switch: the old one leaves with the old PSU,
// the new one comes with the new PSU (set its Is On OFF - brand new).
// IMPORTANT: leave both PSU Switches' own Checklist fields EMPTY.
public class PSUHardManager : MonoBehaviour
{
    public enum Step
    {
        PressPowerFirst, SwitchOff, UnplugOld24Pin, Unscrew, RemoveOldPsu,
        InstallNewPsu, ScrewIn, PlugNew24Pin, SwitchOn, PressPowerFinal, Done
    }

    [Header("Screen")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Header("Parts")]
    [Tooltip("The OLD PSU's switch (turned off at the start)")]
    public PSUSwitch1 psuSwitch;
    [Tooltip("The NEW PSU's switch (turned on at the end)")]
    public PSUSwitch1 newPsuSwitch;
    public PullOutConnector oldConnector24Pin;
    public PSUScrew[] screws;
    public PSUOldUnit oldPsu;
    [Tooltip("Hidden at start, shown once the old PSU is actually out of the case (e.g. the New PSU). Untick them in the Hierarchy.")]
    public GameObject[] showWhenOldRemoved;
    private bool shownNew = false;
    public PSUSlot psuSlot;
    public PullOutConnector newConnector24Pin;

    [Header("Effects")]
    [Tooltip("Sparks when pressing power while the dead PSU is still in")]
    public ShortCircuitEffect1 shortCircuitEffect;
    [Tooltip("Optional: the NEW PSU's fan blades, spins when the PC runs")]
    public Transform newPsuFan;
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 900f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip beepSound;
    public AudioClip successSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-10, in order")]
    public int[] stepGroupIndices = { 1, 2, 4, 5, 6, 7, 8, 9, 11, 12 };
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPowerFirst;
    [Tooltip("What this script is waiting for right now")]
    public string status = "";
    public bool pressedOnce = false;
    public bool pcRunning = false;

    private XRBaseInteractable interactable;

    private bool OldSwitchOn => psuSwitch == null || !psuSwitch.IsOff;
    private bool NewSwitchOn => newPsuSwitch == null || !newPsuSwitch.IsOff;

    // Power exists only from the PSU that's actually in the case
    private bool SwitchOn
    {
        get
        {
            if (NewInstalled) return NewSwitchOn;
            if (oldPsu != null && oldPsu.isRemoved) return false; // bay is empty
            return OldSwitchOn;
        }
    }
    // New PSU counts as installed only after the old one is really out (ignores a wrong saved value)
    private bool NewInstalled => psuSlot != null && psuSlot.isInstalled && (oldPsu == null || oldPsu.isRemoved);
    private bool NewPlugged => newConnector24Pin != null && newConnector24Pin.isSeated;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(args => PressPowerButton());

        if (psuSwitch != null)
            psuSwitch.onTurnedOff.AddListener(OnSwitchTurnedOff);
        if (newPsuSwitch != null)
            newPsuSwitch.onTurnedOff.AddListener(OnSwitchTurnedOff);
    }

    private void Start()
    {
        // Fresh start every time (ignores values saved in the Inspector)
        currentStep = Step.PressPowerFirst;
        pressedOnce = false;
        pcRunning = false;
        shownNew = false;
        ShowScreen(false);
        if (newConnector24Pin != null) newConnector24Pin.allowPlugIn = false;
    }

    private void Update()
    {
        // New PSU unlocks the screws going back in and its cable plugging in
        // Old PSU out of the case -> reveal the new one
        if (!shownNew && oldPsu != null && oldPsu.isRemoved)
        {
            shownNew = true;
            if (showWhenOldRemoved != null)
                foreach (var g in showWhenOldRemoved) if (g != null) g.SetActive(true);
        }

        bool installed = NewInstalled;
        if (screws != null)
            foreach (var s in screws) if (s != null) s.canFasten = installed;
        if (newConnector24Pin != null) newConnector24Pin.allowPlugIn = installed;

        if (pcRunning && newPsuFan != null)
            newPsuFan.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);

        TryAdvance();
    }

    // ---------- Power button ----------
    public void PressPowerButton()
    {
        Play(beepSound);
        if (pcRunning) return;

        if (!SwitchOn)
        {
            ShowScreen(false); // no power at all
            return;
        }

        bool oldStillIn = oldPsu != null && !oldPsu.isRemoved;

        if (oldStillIn)
        {
            // Dead PSU: sparks
            if (shortCircuitEffect != null) shortCircuitEffect.PlayEffect();
            if (pressedOnce && performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            pressedOnce = true;
            return;
        }

        if (NewInstalled && NewPlugged)
        {
            pcRunning = true;
            ShowScreen(true);
            Play(successSound);
            return;
        }

        // New PSU missing or not plugged in yet - nothing happens
        if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
        pressedOnce = true;
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

        if (checklist == null)
        {
            if (IsStepDone(currentStep)) currentStep++;
            status = Describe(currentStep);
            return;
        }

        // Follow whatever step the checklist is showing right now, so this script
        // can never fall out of sync (other scripts may tick steps too).
        for (int j = 0; j < stepGroupIndices.Length && j < (int)Step.Done; j++)
        {
            int g = stepGroupIndices[j];
            if (g < 0 || !checklist.IsCurrentStep(g, objectiveIndex)) continue;

            currentStep = (Step)j;
            status = Describe(currentStep);
            if (IsStepDone(currentStep))
            {
                checklist.CompleteObjective(g, objectiveIndex);
                currentStep = (Step)(j + 1);
            }
            return;
        }

        status = "Checklist is on a step this script doesn't handle (talk to NPC / open or close cover)";
    }

    // Shown in the Inspector (State -> Status) so you can see what it's waiting for
    private string Describe(Step step)
    {
        switch (step)
        {
            case Step.PressPowerFirst: return pressedOnce ? "Done" : "Waiting: press the power button (old PSU must have power: Old switch ON)";
            case Step.SwitchOff: return OldSwitchOn ? "Waiting: turn the OLD PSU switch off" : "Done";
            case Step.UnplugOld24Pin: return (oldConnector24Pin != null && oldConnector24Pin.isSeated) ? "Waiting: unplug the OLD 24-pin" : "Done";
            case Step.Unscrew:
                {
                    int n = 0, total = 0;
                    if (screws != null) foreach (var s in screws) if (s != null) { total++; if (s.IsRemoved) n++; }
                    return "Unscrew: " + n + "/" + total + " screws removed";
                }
            case Step.RemoveOldPsu: return (oldPsu != null && !oldPsu.isRemoved) ? "Waiting: pull the old PSU out" : "Done";
            case Step.InstallNewPsu: return NewInstalled ? "Done" : "Waiting: put the new PSU in the slot";
            case Step.ScrewIn:
                {
                    int n = 0, total = 0;
                    if (screws != null) foreach (var s in screws) if (s != null) { total++; if (s.IsFastened) n++; }
                    return "Screw in: " + n + "/" + total + " screws fastened";
                }
            case Step.PlugNew24Pin: return NewPlugged ? "Done" : "Waiting: plug in the NEW 24-pin";
            default: return "Waiting: " + step;
        }
    }

    private bool IsStepDone(Step step)
    {
        switch (step)
        {
            case Step.PressPowerFirst: return pressedOnce;
            case Step.SwitchOff: return !OldSwitchOn;
            case Step.UnplugOld24Pin: return oldConnector24Pin == null || !oldConnector24Pin.isSeated;
            case Step.Unscrew: return AllScrews(removed: true);
            case Step.RemoveOldPsu: return oldPsu == null || oldPsu.isRemoved;
            case Step.InstallNewPsu: return NewInstalled;
            case Step.ScrewIn: return AllScrews(removed: false);
            case Step.PlugNew24Pin: return NewPlugged;
            case Step.SwitchOn: return NewInstalled && NewSwitchOn;
            case Step.PressPowerFinal: return pcRunning;
        }
        return false;
    }

    private bool AllScrews(bool removed)
    {
        if (screws == null || screws.Length == 0) return true;
        foreach (var s in screws)
        {
            if (s == null) continue;
            if (removed && !s.IsRemoved) return false;
            if (!removed && !s.IsFastened) return false;
        }
        return true;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[PSUHardManager] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private void ShowScreen(bool on)
    {
        if (screenOn != null) screenOn.SetActive(on);
        if (screenOff != null) screenOff.SetActive(!on);
    }
}