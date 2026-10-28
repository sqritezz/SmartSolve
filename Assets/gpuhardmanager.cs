using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU HARD - "The graphics card has no power" (PCIe power cable unplugged)
// Attach to the PC POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
//
// Steps (checked in order, ticks the checklist):
//   1 Press the power button              -> fans spin (except GPU), monitor "No Signal"
//   2 Check the monitor cable at the back -> point at the GPU port / monitor cable
//   3 Open the side cover                 (ticked by the lid's Door Slide - External)
//   4 Look at the graphics card           -> point at it: its fans are NOT spinning
//   5 Turn off the PSU switch
//   6 Plug the PCIe power cable           -> touch the plug to the GPU's power socket
//   7 Close the side cover                (ticked by the lid's Door Slide - External)
//   8 Turn on the PSU switch
//   9 Press the power button              -> GPU fans spin, desktop appears
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class GPUHardManager : MonoBehaviour
{
    public enum Step
    {
        PressPowerFirst, CheckCable, OpenCover, LookAtGpu, SwitchOff,
        PlugPcie, CloseCover, SwitchOn, PressPowerFinal, Done
    }

    [Header("Monitor screens")]
    public GameObject noSignalScreen;
    public GameObject desktopScreen;
    public GameObject offScreen;

    [Header("Fans")]
    [Tooltip("Fans that spin whenever the PC runs (CPU fan, PSU fan, case fans)")]
    public Transform[] pcFans;
    [Tooltip("The graphics card's fans - only spin once the PCIe cable is plugged in")]
    public Transform[] gpuFans;
    [Tooltip("Each fan finds its own spin axis (its thinnest side). Untick to use Fan Axis for all.")]
    public bool autoFanAxis = true;
    public Vector3 fanAxis = Vector3.forward;
    public float fanSpeed = 900f;
    [Tooltip("Power LED etc. on while the PC runs")]
    public GameObject[] runningLights;
    [Tooltip("Optional: GPU's own LED, on only when it has power and the PC runs")]
    public GameObject gpuLight;

    [Header("Parts")]
    [Tooltip("The PSU switch (PSU Switch 1). Leave its own Checklist fields EMPTY.")]
    public PSUSwitch1 psuSwitch;
    [Tooltip("Pointing at any of these counts as checking the monitor cable (GPU port, monitor cable plug)")]
    public XRBaseInteractable[] cableChecks;
    [Tooltip("The graphics card's XR Simple Interactable - pointing at it counts as looking at it")]
    public XRBaseInteractable graphicsCard;
    [Tooltip("The PCIe power cable plug (XR Grab Interactable + Rigidbody + Collider). Starts loose inside the case.")]
    public XRGrabInteractable pciePlug;
    [Tooltip("Empty at the graphics card's PowerSocket. Green Y arrow points OUT of the socket.")]
    public Transform pcieSocketSpot;
    public float snapDistance = 0.05f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerOnSound;
    public AudioClip powerOffSound;
    public AudioClip plugSound;
    public AudioClip unplugSound;
    public AudioClip successSound;
    public AudioClip wrongSound;

    [Header("Messages (optional)")]
    [Tooltip("e.g. \"Turn off the PSU switch before touching the cables!\"")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-9 (group 0 is 'talk to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
    public bool openCoverIsExternal = true;
    public bool closeCoverIsExternal = true;
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPowerFirst;
    public bool pcOn = false;
    public bool pressedOnce = false;
    public bool cableChecked = false;
    public bool gpuLooked = false;
    public bool pcieIn = false;
    public bool fixedShown = false;

    private XRBaseInteractable button;
    private Rigidbody plugRb;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool armed = true, snappingNow = false;

    private bool SwitchOn => psuSwitch == null || !psuSwitch.IsOff;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(_ => PressPower());

        if (psuSwitch != null) psuSwitch.onTurnedOff.AddListener(OnSwitchOff);

        if (pciePlug != null)
        {
            plugRb = pciePlug.GetComponent<Rigidbody>();
            if (plugRb != null) plugRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            pciePlug.throwOnDetach = false;
            pciePlug.selectEntered.AddListener(_ => OnPlugGrabbed());
            pciePlug.selectExited.AddListener(_ => OnPlugReleased());
        }
    }

    private void Start()
    {
        currentStep = Step.PressPowerFirst;
        pcOn = pressedOnce = cableChecked = gpuLooked = pcieIn = fixedShown = false;
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
        if (plugRb != null) { plugRb.isKinematic = true; plugRb.useGravity = false; } // stays where placed
        UpdatePC();
    }

    private void Update()
    {
        CheckTriggerClick();

        if (!cableChecked && pressedOnce && AnyHovered(cableChecks)) cableChecked = true;
        if (!gpuLooked && pcOn && currentStep >= Step.LookAtGpu && graphicsCard != null && graphicsCard.isHovered)
            gpuLooked = true;

        CheckPlugSnap();

        if (pcOn)
        {
            Spin(pcFans);
            if (pcieIn) Spin(gpuFans);
        }

        TryAdvance();
    }

    private void LateUpdate()
    {
        if (pcieIn && pciePlug != null && !pciePlug.isSelected) PlacePlug();
    }

    private void Spin(Transform[] fans)
    {
        if (fans == null) return;
        foreach (var f in fans)
            if (f == null) continue;
            else if (autoFanAxis) FanAxisFinder.Spin(f, fanSpeed * Time.deltaTime);
            else f.Rotate(fanAxis, fanSpeed * Time.deltaTime, Space.Self);
    }

    private static bool AnyHovered(XRBaseInteractable[] list)
    {
        if (list == null) return false;
        foreach (var i in list) if (i != null && i.isHovered) return true;
        return false;
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
        if (!SwitchOn) return; // no power at all

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
        bool picture = pcOn && pcieIn;

        if (offScreen != null) offScreen.SetActive(!pcOn);
        if (noSignalScreen != null) noSignalScreen.SetActive(pcOn && !picture);
        if (desktopScreen != null) desktopScreen.SetActive(picture);

        if (runningLights != null) foreach (var l in runningLights) if (l != null) l.SetActive(pcOn);
        if (gpuLight != null) gpuLight.SetActive(picture);

        if (picture && !fixedShown) { fixedShown = true; Play(successSound); }
    }

    // ---------- PCIe plug ----------
    private void OnPlugGrabbed()
    {
        if (SwitchOn)
        {
            Play(wrongSound);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            if (powerOnWarning != null) StartCoroutine(ShowMessage(powerOnWarning));
            StartCoroutine(ForceDrop());
            return;
        }
        if (!pcieIn) return;
        pcieIn = false;
        armed = false;
        Play(unplugSound);
        UpdatePC();
    }

    private IEnumerator ForceDrop()
    {
        pciePlug.enabled = false;
        yield return null;
        pciePlug.enabled = true;
        if (pcieIn) PlacePlug();
        else if (plugRb != null) { plugRb.isKinematic = true; plugRb.useGravity = false; }
    }

    private void OnPlugReleased()
    {
        if (snappingNow || pcieIn) return;
        StartCoroutine(DropNextFrame());
    }

    private IEnumerator DropNextFrame()
    {
        yield return null;
        if (pcieIn || plugRb == null) yield break;
        plugRb.isKinematic = false; plugRb.useGravity = true;
    }

    private void CheckPlugSnap()
    {
        if (pciePlug == null || !pciePlug.isSelected || pcieIn || pcieSocketSpot == null) return;

        bool near = IsNear();
        if (!near) { armed = true; return; }
        if (!armed) return;
        armed = false;

        snappingNow = true;
        var hand = pciePlug.firstInteractorSelecting;
        var mgr = pciePlug.interactionManager;
        if (hand != null && mgr != null) mgr.SelectExit(hand, pciePlug);
        snappingNow = false;

        pcieIn = true;
        PlacePlug();
        Play(plugSound);
        UpdatePC();
    }

    private bool IsNear()
    {
        Vector3 p = pcieSocketSpot.position;
        if (Vector3.Distance(pciePlug.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in pciePlug.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private void PlacePlug()
    {
        if (plugRb != null)
        {
            if (!plugRb.isKinematic) { plugRb.linearVelocity = Vector3.zero; plugRb.angularVelocity = Vector3.zero; }
            plugRb.isKinematic = true; plugRb.useGravity = false;
        }
        pciePlug.transform.SetPositionAndRotation(pcieSocketSpot.position, pcieSocketSpot.rotation);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[GPUHardManager] A sound slot is broken. Set it to None and drag the clip in again.", this);
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
            case Step.CheckCable: return cableChecked;
            case Step.LookAtGpu: return gpuLooked;
            case Step.SwitchOff: return !SwitchOn;
            case Step.PlugPcie: return pcieIn;
            case Step.SwitchOn: return SwitchOn && pcieIn;
            case Step.PressPowerFinal: return fixedShown;
        }
        return false;
    }
}