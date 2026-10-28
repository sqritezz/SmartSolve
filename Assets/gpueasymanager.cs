using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU EASY - "The PC turns on but the monitor says No Signal"
// The monitor cable is plugged into the MOTHERBOARD's video port instead of the GRAPHICS CARD.
//
// Attach to the PC POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
// Point the ray at the button and pull the TRIGGER to turn the PC on/off.
//
// Steps (checked in order, ticks the checklist):
//   1 Press the power button                 - PC runs, monitor shows "No Signal"
//   2 Look inside the system unit            - point at the fans / GPU: everything is running
//   3 Check the ports at the back of the PC  - point the ray at a port (or grab the cable)
//   4 Unplug the monitor cable               - grab it out of the motherboard port
//   5 Plug it into the graphics card port    - touch the plug to the GPU port, it snaps in
//   6 Check the monitor                      - PC on + cable in GPU port -> desktop appears
// Plugging it back into the motherboard port counts as a wrong attempt.
// No need to turn the PC off: video cables are safe to plug in while it runs.
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class GPUEasyManager : MonoBehaviour
{
    public enum Step { PressPower, CheckInside, CheckPorts, Unplug, PlugGPU, CheckMonitor, Done }

    [Header("Monitor screens")]
    [Tooltip("Quad with Monitor_NoSignal.png (shown while the PC runs without a picture)")]
    public GameObject noSignalScreen;
    [Tooltip("The working desktop screen (e.g. your screenOn)")]
    public GameObject desktopScreen;
    [Tooltip("Optional: black/off screen shown while the PC is off")]
    public GameObject offScreen;

    [Header("PC running feedback (optional)")]
    [Tooltip("Fan blades that spin while the PC runs (CPU fan, case fan, GPU fans...)")]
    public Transform[] fans;
    [Tooltip("Each fan finds its own spin axis (its thinnest side). Untick to use Fan Axis for all.")]
    public bool autoFanAxis = true;
    public Vector3 fanAxis = Vector3.forward;
    public float fanSpeed = 900f;
    [Tooltip("Power LED / lights turned on while the PC runs")]
    public GameObject[] runningLights;

    [Header("Monitor cable")]
    [Tooltip("The monitor cable's plug (XR Grab Interactable + Rigidbody + Collider)")]
    public XRGrabInteractable cablePlug;
    [Tooltip("Empty at the MOTHERBOARD video port (blue Z arrow points OUT of the PC). The plug starts here.")]
    public Transform motherboardPortSpot;
    [Tooltip("Empty at the GRAPHICS CARD video port (blue Z arrow points OUT of the PC)")]
    public Transform gpuPortSpot;
    [Tooltip("How close (meters) any part of the plug must get to a port to snap in")]
    public float snapDistance = 0.05f;

    [Header("Look inside (step 2)")]
    [Tooltip("XR Simple Interactables inside the case (GPU, CPU fan...). Pointing at one while the PC runs = checked.")]
    public XRBaseInteractable[] insideToCheck;

    [Header("Ports to check (step 3)")]
    [Tooltip("The ports' XR Simple Interactables (optional). Pointing the ray at one counts as checking.")]
    public XRBaseInteractable[] portsToCheck;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerOnSound;     // e.g. pc open
    public AudioClip powerOffSound;    // e.g. shutdown
    public AudioClip plugSound;
    public AudioClip unplugSound;
    public AudioClip successSound;     // e.g. Windows startup / success
    public AudioClip wrongSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-6 (group 0 is 'talk to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5, 6 };
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("Events")]
    public UnityEvent onFixed;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressPower;
    public bool pcOn = false;
    public bool insideChecked = false;
    public bool portsChecked = false;
    public bool unplugged = false;
    public int pluggedPort = 1;   // 0 = not plugged, 1 = motherboard, 2 = graphics card
    public bool pictureShown = false;

    private XRBaseInteractable button;
    private Rigidbody plugRb;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool armed = true;
    private bool snappingNow = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(OnButtonSelected);

        if (cablePlug != null)
        {
            plugRb = cablePlug.GetComponent<Rigidbody>();
            if (plugRb != null) plugRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            cablePlug.throwOnDetach = false;
            cablePlug.selectEntered.AddListener(OnPlugGrabbed);
            cablePlug.selectExited.AddListener(OnPlugReleased);
        }
    }

    private void Start()
    {
        // Fresh start every time (ignores values saved in the Inspector)
        currentStep = Step.PressPower;
        pcOn = false; insideChecked = false; portsChecked = false; unplugged = false; pictureShown = false;
        pluggedPort = 1;
        PlacePlug();
        UpdatePC();
    }

    private void OnDestroy()
    {
        if (button != null) button.selectEntered.RemoveListener(OnButtonSelected);
    }

    private void Update()
    {
        CheckTriggerClick();
        if (!insideChecked && pcOn && insideToCheck != null)
            foreach (var t in insideToCheck) if (t != null && t.isHovered) { insideChecked = true; break; }
        CheckPortsHover();
        CheckPlugTouch();

        if (pcOn && fans != null)
            foreach (var f in fans)
                if (f == null) continue;
                else if (autoFanAxis) FanAxisFinder.Spin(f, fanSpeed * Time.deltaTime);
                else f.Rotate(fanAxis, fanSpeed * Time.deltaTime, Space.Self);

        TryAdvance();
    }

    private void LateUpdate()
    {
        if (pluggedPort != 0 && cablePlug != null && !cablePlug.isSelected) PlacePlug();
    }

    // ---------- Power button ----------
    private void OnButtonSelected(SelectEnterEventArgs args) => PressPower();

    private void CheckTriggerClick()
    {
        bool leftDown = TriggerDown(XRNode.LeftHand);
        bool rightDown = TriggerDown(XRNode.RightHand);
        bool newPress = (leftDown && !leftWasDown) || (rightDown && !rightWasDown);
        leftWasDown = leftDown;
        rightWasDown = rightDown;

        if (newPress && button != null && button.isHovered) PressPower();
    }

    private static bool TriggerDown(XRNode hand)
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);
        return device.isValid &&
               device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed;
    }

    public void PressPower()
    {
        if (Time.time - lastPressTime < 0.2f) return; // ignore double fire
        lastPressTime = Time.time;

        pcOn = !pcOn;
        Play(pcOn ? powerOnSound : powerOffSound);
        UpdatePC();
    }

    private void UpdatePC()
    {
        bool picture = pcOn && pluggedPort == 2;

        if (offScreen != null) offScreen.SetActive(!pcOn);
        if (noSignalScreen != null) noSignalScreen.SetActive(pcOn && !picture);
        if (desktopScreen != null) desktopScreen.SetActive(picture);

        if (runningLights != null)
            foreach (var l in runningLights) if (l != null) l.SetActive(pcOn);

        if (picture && !pictureShown)
        {
            pictureShown = true;
            Play(successSound);
            onFixed?.Invoke();
        }
    }

    // ---------- Step 2: look at the ports ----------
    private void CheckPortsHover()
    {
        if (portsChecked || !pcOn || portsToCheck == null || currentStep < Step.CheckPorts) return;
        foreach (var p in portsToCheck)
            if (p != null && p.isHovered) { portsChecked = true; return; }
    }

    // ---------- Monitor cable ----------
    private void OnPlugGrabbed(SelectEnterEventArgs args)
    {
        if (pluggedPort == 0) return;
        pluggedPort = 0;
        unplugged = true;
        portsChecked = true;  // they found the cable, so they checked the ports
        armed = false;        // must move away before it can snap again
        Play(unplugSound);
        UpdatePC();
    }

    private void OnPlugReleased(SelectExitEventArgs args)
    {
        if (snappingNow || pluggedPort != 0) return;
        StartCoroutine(DropNextFrame());
    }

    private IEnumerator DropNextFrame()
    {
        yield return null;
        if (pluggedPort != 0 || plugRb == null) yield break;
        plugRb.isKinematic = false;
        plugRb.useGravity = true;
    }

    private void CheckPlugTouch()
    {
        if (cablePlug == null || !cablePlug.isSelected || pluggedPort != 0) return;

        int port = 0;
        if (IsNear(gpuPortSpot)) port = 2;
        else if (IsNear(motherboardPortSpot)) port = 1;

        if (port == 0) { armed = true; return; }
        if (!armed) return;
        armed = false;

        snappingNow = true;
        var hand = cablePlug.firstInteractorSelecting;
        var manager = cablePlug.interactionManager;
        if (hand != null && manager != null) manager.SelectExit(hand, cablePlug);
        snappingNow = false;

        pluggedPort = port;
        PlacePlug();
        Play(plugSound);

        // Back into the motherboard port after unplugging = wrong
        if (port == 1 && unplugged)
        {
            Play(wrongSound);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
        }

        UpdatePC();
    }

    private bool IsNear(Transform spot)
    {
        if (spot == null) return false;
        Vector3 p = spot.position;
        if (Vector3.Distance(cablePlug.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in cablePlug.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private void PlacePlug()
    {
        Transform spot = pluggedPort == 1 ? motherboardPortSpot : pluggedPort == 2 ? gpuPortSpot : null;
        if (spot == null || cablePlug == null) return;

        if (plugRb != null)
        {
            if (!plugRb.isKinematic) { plugRb.linearVelocity = Vector3.zero; plugRb.angularVelocity = Vector3.zero; }
            plugRb.isKinematic = true;
            plugRb.useGravity = false;
        }
        cablePlug.transform.SetPositionAndRotation(spot.position, spot.rotation);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[GPUEasyManager] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    // ---------- Step checking ----------
    private void TryAdvance()
    {
        if (currentStep == Step.Done) return;

        int i = (int)currentStep;
        int group = i < stepGroupIndices.Length ? stepGroupIndices[i] : -1;

        if (checklist != null && group >= 0 && !checklist.IsCurrentStep(group, objectiveIndex))
        {
            // Another script already ticked this step and the checklist moved on -> catch up
            int nextGroup = i + 1 < stepGroupIndices.Length ? stepGroupIndices[i + 1] : -1;
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
            case Step.PressPower: return pcOn;
            case Step.CheckInside: return insideChecked || insideToCheck == null || insideToCheck.Length == 0;
            case Step.CheckPorts: return portsChecked;
            case Step.Unplug: return unplugged;
            case Step.PlugGPU: return pluggedPort == 2;
            case Step.CheckMonitor: return pictureShown;
        }
        return false;
    }
}