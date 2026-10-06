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
    public PSUSlot psuSlot;
    public PullOutConnector newConnector24Pin;

    [Header("Effects")]
    [Tooltip("Sparks when pressing power while the dead PSU is still in")]
    public ShortCircuitEffect shortCircuitEffect;
    [Tooltip("Optional: the NEW PSU's fan blades, spins when the PC runs")]
    public Transform newPsuFan;
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 900f;

    [Header("Audio")]
    public AudioSource beepSound;
    public AudioSource successSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-10, in order")]
    public int[] stepGroupIndices = { 1, 2, 4, 5, 6, 7, 8, 9, 11, 12 };
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPowerFirst;
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
    private bool NewInstalled => psuSlot != null && psuSlot.isInstalled;
    private bool NewPlugged => newConnector24Pin != null && newConnector24Pin.isSeated;

    private void Awake()
    {
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
        ShowScreen(false);
        if (newConnector24Pin != null) newConnector24Pin.allowPlugIn = false;
    }

    private void Update()
    {
        // New PSU unlocks the screws going back in and its cable plugging in
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
        if (beepSound != null) beepSound.Play();
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
            if (successSound != null) successSound.Play();
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

        int i = (int)currentStep;
        int group = i < stepGroupIndices.Length ? stepGroupIndices[i] : -1;

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

    private void ShowScreen(bool on)
    {
        if (screenOn != null) screenOn.SetActive(on);
        if (screenOff != null) screenOff.SetActive(!on);
    }
}