using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU MEDIUM - "The screen is full of glitches: the graphics card is broken"
// Attach to the PC POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
//
// Steps (checked in order, ticks the checklist):
//   1  Press the power button          -> artifacts on the monitor
//   2  Turn off the PSU switch
//   3  Open the side cover             (ticked by the lid's own Door Slide - set as External)
//   4  Unscrew the bracket screw(s)    (PSUScrew on the bracket screw)
//   5  Remove the broken graphics card (grab it out of the slot)
//   6  Install the new graphics card   (touch it to the slot, it snaps in)
//   7  Screw the bracket back in
//   8  Close the side cover            (ticked by the lid's own Door Slide - set as External)
//   9  Turn on the PSU switch
//   10 Press the power button          -> clear desktop
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class GPUMediumManager : MonoBehaviour
{
    public enum Step
    {
        PressPowerFirst, SwitchOff, OpenCover, Unscrew, RemoveOldGpu,
        InstallNewGpu, ScrewIn, CloseCover, SwitchOn, PressPowerFinal, Done
    }

    [Header("Monitor screens")]
    [Tooltip("Quad with Monitor_Artifacts.png")]
    public GameObject artifactScreen;
    [Tooltip("The working desktop (e.g. your screenOn)")]
    public GameObject desktopScreen;
    [Tooltip("Optional: black/off screen")]
    public GameObject offScreen;

    [Header("PC running feedback (optional)")]
    public Transform[] fans;
    public Vector3 fanAxis = Vector3.forward;
    public float fanSpeed = 900f;
    public GameObject[] runningLights;

    [Header("Parts")]
    [Tooltip("The PSU switch (PSU Switch 1). Leave its own Checklist fields EMPTY.")]
    public PSUSwitch1 psuSwitch;
    [Tooltip("The bracket screw(s) holding the graphics card (PSUScrew). Set their Psu Switch too.")]
    public PSUScrew[] bracketScrews;
    [Tooltip("The broken graphics card, sitting in the slot (XR Grab Interactable + Rigidbody + Collider)")]
    public XRGrabInteractable oldGpu;
    [Tooltip("The new graphics card, on the table (XR Grab Interactable + Rigidbody + Collider)")]
    public XRGrabInteractable newGpu;
    [Tooltip("Empty object where a graphics card sits in the slot (same position/rotation as the old card)")]
    public Transform gpuSlotSpot;
    [Tooltip("How close (meters) any part of the new card must get to the slot to snap in")]
    public float snapDistance = 0.08f;
    [Tooltip("How far (meters) the old card must be pulled from the slot to count as removed")]
    public float removeDistance = 0.12f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerOnSound;
    public AudioClip powerOffSound;
    public AudioClip installSound;   // e.g. snap sound
    public AudioClip removeSound;    // e.g. plug out
    public AudioClip successSound;
    public AudioClip wrongSound;

    [Header("Messages (optional)")]
    [Tooltip("e.g. \"Unscrew the bracket first!\"")]
    public GameObject unscrewFirstMessage;
    [Tooltip("e.g. \"Turn off the PSU switch before touching the parts!\"")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-10, in order (group 0 is 'talk to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    [Tooltip("Steps ticked by ANOTHER script (e.g. the lid's Door Slide). The manager waits for them instead of ticking.")]
    public bool openCoverIsExternal = true;
    public bool closeCoverIsExternal = true;
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPowerFirst;
    public bool pcOn = false;
    public bool pressedOnce = false;
    public bool oldRemoved = false;
    public bool newInstalled = false;
    public bool fixedShown = false;

    private XRBaseInteractable button;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool snappingNow = false;

    private bool SwitchOn => psuSwitch == null || !psuSwitch.IsOff;
    private bool ScrewsOut => AllScrews(removed: true);
    private bool ScrewsIn => AllScrews(removed: false);

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(_ => PressPower());

        if (psuSwitch != null) psuSwitch.onTurnedOff.AddListener(OnSwitchOff);

        SetupGpu(oldGpu, true);
        SetupGpu(newGpu, false);
    }

    private void SetupGpu(XRGrabInteractable g, bool isOld)
    {
        if (g == null) return;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        g.throwOnDetach = false;
        if (isOld) g.selectEntered.AddListener(_ => OnOldGrabbed());
        g.selectExited.AddListener(_ => OnGpuReleased(g));
    }

    private void Start()
    {
        currentStep = Step.PressPowerFirst;
        pcOn = false; pressedOnce = false; oldRemoved = false; newInstalled = false; fixedShown = false;

        if (unscrewFirstMessage != null) unscrewFirstMessage.SetActive(false);
        if (powerOnWarning != null) powerOnWarning.SetActive(false);

        // Old card sits locked in the slot
        if (oldGpu != null && gpuSlotSpot != null)
        {
            oldGpu.transform.SetPositionAndRotation(gpuSlotSpot.position, gpuSlotSpot.rotation);
            Freeze(oldGpu);
        }
        if (newGpu != null) Freeze(newGpu); // stays on the table until grabbed

        UpdatePC();
    }

    private void Update()
    {
        CheckTriggerClick();

        // Screws can only go back in once the new card is in
        if (bracketScrews != null)
            foreach (var s in bracketScrews) if (s != null) s.canFasten = newInstalled;

        // Old card: count as removed once pulled away from the slot
        if (!oldRemoved && oldGpu != null && gpuSlotSpot != null && oldGpu.isSelected &&
            Vector3.Distance(oldGpu.transform.position, gpuSlotSpot.position) > removeDistance)
        {
            oldRemoved = true;
            Play(removeSound);
        }

        CheckNewGpuSnap();

        if (pcOn && fans != null)
            foreach (var f in fans) if (f != null) f.Rotate(fanAxis, fanSpeed * Time.deltaTime, Space.Self);

        TryAdvance();
    }

    private void LateUpdate()
    {
        // Keep cards locked in the slot when they belong there
        if (gpuSlotSpot == null) return;
        if (!oldRemoved && oldGpu != null && !oldGpu.isSelected)
            oldGpu.transform.SetPositionAndRotation(gpuSlotSpot.position, gpuSlotSpot.rotation);
        if (newInstalled && newGpu != null && !newGpu.isSelected)
            newGpu.transform.SetPositionAndRotation(gpuSlotSpot.position, gpuSlotSpot.rotation);
    }

    // ---------- Power ----------
    private void CheckTriggerClick()
    {
        bool l = TriggerDown(XRNode.LeftHand), r = TriggerDown(XRNode.RightHand);
        bool newPress = (l && !leftWasDown) || (r && !rightWasDown);
        leftWasDown = l; rightWasDown = r;
        if (newPress && button != null && button.isHovered) PressPower();
    }

    private static bool TriggerDown(XRNode hand)
    {
        var d = InputDevices.GetDeviceAtXRNode(hand);
        return d.isValid && d.TryGetFeatureValue(CommonUsages.triggerButton, out bool p) && p;
    }

    public void PressPower()
    {
        if (Time.time - lastPressTime < 0.2f) return;
        lastPressTime = Time.time;

        if (!SwitchOn) return;              // no power at all

        bool cardIn = (!oldRemoved) || newInstalled;
        if (!pcOn && !cardIn)
        {
            // No graphics card in the slot - nothing to show
            Play(wrongSound);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            return;
        }

        pcOn = !pcOn;
        Play(pcOn ? powerOnSound : powerOffSound);
        if (pcOn) pressedOnce = true;
        UpdatePC();
    }

    private void OnSwitchOff()
    {
        if (!pcOn) return;
        pcOn = false;
        UpdatePC();
    }

    private void UpdatePC()
    {
        bool good = pcOn && newInstalled;
        bool bad = pcOn && !newInstalled;

        if (offScreen != null) offScreen.SetActive(!pcOn);
        if (artifactScreen != null) artifactScreen.SetActive(bad);
        if (desktopScreen != null) desktopScreen.SetActive(good);

        if (runningLights != null)
            foreach (var l in runningLights) if (l != null) l.SetActive(pcOn);

        if (good && !fixedShown) { fixedShown = true; Play(successSound); }
    }

    // ---------- Graphics cards ----------
    private void OnOldGrabbed()
    {
        if (oldRemoved) return;

        if (SwitchOn)
        {
            Warn(powerOnWarning);
            StartCoroutine(ForceDrop(oldGpu));
            return;
        }
        if (!ScrewsOut)
        {
            Warn(unscrewFirstMessage);
            StartCoroutine(ForceDrop(oldGpu));
        }
    }

    private void Warn(GameObject message)
    {
        Play(wrongSound);
        if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
        if (message != null) StartCoroutine(ShowMessage(message));
    }

    private IEnumerator ForceDrop(XRGrabInteractable g)
    {
        g.enabled = false;
        yield return null;
        g.enabled = true;
        if (gpuSlotSpot != null) g.transform.SetPositionAndRotation(gpuSlotSpot.position, gpuSlotSpot.rotation);
        Freeze(g);
    }

    private void OnGpuReleased(XRGrabInteractable g)
    {
        if (snappingNow) return;
        if (g == oldGpu && !oldRemoved) return;   // still in the slot (LateUpdate holds it)
        if (g == newGpu && newInstalled) return;
        StartCoroutine(DropNextFrame(g));
    }

    private IEnumerator DropNextFrame(XRGrabInteractable g)
    {
        yield return null;
        if ((g == newGpu && newInstalled) || (g == oldGpu && !oldRemoved)) yield break;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
    }

    private void CheckNewGpuSnap()
    {
        if (newInstalled || newGpu == null || !newGpu.isSelected || gpuSlotSpot == null) return;
        if (!IsNear(newGpu, gpuSlotSpot.position)) return;

        if (!oldRemoved)   // slot still taken
            return;

        snappingNow = true;
        var hand = newGpu.firstInteractorSelecting;
        var mgr = newGpu.interactionManager;
        if (hand != null && mgr != null) mgr.SelectExit(hand, newGpu);
        snappingNow = false;

        newInstalled = true;
        newGpu.transform.SetPositionAndRotation(gpuSlotSpot.position, gpuSlotSpot.rotation);
        Freeze(newGpu);
        Play(installSound);
    }

    private bool IsNear(XRGrabInteractable g, Vector3 p)
    {
        if (Vector3.Distance(g.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in g.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private static void Freeze(XRGrabInteractable g)
    {
        var rb = g.GetComponent<Rigidbody>();
        if (rb == null) return;
        if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.isKinematic = true; rb.useGravity = false;
    }

    private bool AllScrews(bool removed)
    {
        if (bracketScrews == null || bracketScrews.Length == 0) return true;
        foreach (var s in bracketScrews)
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
            Debug.LogWarning("[GPUMediumManager] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private IEnumerator ShowMessage(GameObject m)
    {
        m.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        m.SetActive(false);
    }

    // ---------- Step checking ----------
    private bool IsExternal(Step s) =>
        (s == Step.OpenCover && openCoverIsExternal) || (s == Step.CloseCover && closeCoverIsExternal);

    private void TryAdvance()
    {
        if (currentStep == Step.Done) return;

        int i = (int)currentStep;
        int group = i < stepGroupIndices.Length ? stepGroupIndices[i] : -1;
        int nextGroup = i + 1 < stepGroupIndices.Length ? stepGroupIndices[i + 1] : -1;

        // Steps ticked by another script: just wait until the checklist moves past them
        if (IsExternal(currentStep))
        {
            if (checklist == null || nextGroup < 0 || checklist.IsCurrentStep(nextGroup, objectiveIndex))
                currentStep++;
            return;
        }

        if (checklist != null && group >= 0 && !checklist.IsCurrentStep(group, objectiveIndex))
        {
            if (IsStepDone(currentStep) && nextGroup >= 0 && checklist.IsCurrentStep(nextGroup, objectiveIndex))
                currentStep++;
            return;
        }

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
            case Step.Unscrew: return ScrewsOut;
            case Step.RemoveOldGpu: return oldRemoved;
            case Step.InstallNewGpu: return newInstalled;
            case Step.ScrewIn: return ScrewsIn;
            case Step.SwitchOn: return SwitchOn && newInstalled;
            case Step.PressPowerFinal: return fixedShown;
        }
        return false;
    }
}