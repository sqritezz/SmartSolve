using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU TUTORIAL - "The TV says NO SIGNAL"
// The TV has power, but the HDMI cable is in the wrong port (HDMI 2 while the TV shows HDMI 1).
//
// Attach to the TV's POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
// Point the ray at the button and pull the TRIGGER to turn the TV on/off.
//
// Steps (checked in order, ticks the checklist):
//   1 Turn on the TV                     - screen shows NO SIGNAL
//   2 Check the HDMI ports at the back   - point the ray at a port (or touch it)
//   3 Unplug the HDMI cable              - grab it out of HDMI 2
//   4 Plug it into HDMI 1                - touch the plug to the HDMI 1 port, it snaps in
//   5 Check the TV                       - TV on + cable in HDMI 1 -> picture appears
// When done, onTaskComplete fires (wire to TutorialFlowManager.OnCablePlugged, like the doorbell).
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class TVTutorialTask : MonoBehaviour
{
    public enum Step { TurnOnTV, CheckPorts, Unplug, PlugCorrect, CheckScreen, Done }

    [Header("Screens (children of the TV)")]
    [Tooltip("Quad with TV_NoSignal.png")]
    public GameObject noSignalScreen;
    [Tooltip("Quad with TV_Picture.png")]
    public GameObject pictureScreen;

    [Header("HDMI")]
    [Tooltip("The HDMI plug (XR Grab Interactable + Rigidbody + Collider)")]
    public XRGrabInteractable hdmiPlug;
    [Tooltip("Empty object at the HDMI 1 port. Blue Z arrow points OUT of the TV.")]
    public Transform hdmi1Spot;
    [Tooltip("Empty object at the HDMI 2 port. Blue Z arrow points OUT of the TV. The plug starts here.")]
    public Transform hdmi2Spot;
    [Tooltip("How close (meters) any part of the plug must get to a port to snap in")]
    public float snapDistance = 0.05f;

    [Header("Ports to check (step 2)")]
    [Tooltip("The ports' XR Simple Interactables (optional). Pointing the ray at one counts as checking.")]
    public XRBaseInteractable[] portsToCheck;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerSound;
    public AudioClip plugSound;
    public AudioClip unplugSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-5 (group 0 is 'talk to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5 };
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager.OnCablePlugged (same as the doorbell)")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.TurnOnTV;
    public bool tvOn = false;
    public bool portsChecked = false;
    public bool unplugged = false;
    public int pluggedPort = 2;   // 0 = not plugged, 1 = HDMI 1, 2 = HDMI 2
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

        if (hdmiPlug != null)
        {
            plugRb = hdmiPlug.GetComponent<Rigidbody>();
            if (plugRb != null) plugRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            hdmiPlug.throwOnDetach = false;
            hdmiPlug.selectEntered.AddListener(OnPlugGrabbed);
            hdmiPlug.selectExited.AddListener(OnPlugReleased);
        }
    }

    private void Start()
    {
        pluggedPort = 2;
        PlacePlug();
        UpdateScreen();
    }

    private void OnDestroy()
    {
        if (button != null) button.selectEntered.RemoveListener(OnButtonSelected);
    }

    private void Update()
    {
        CheckTriggerClick();
        CheckPortsHover();
        CheckPlugTouch();
        TryAdvance();
    }

    private void LateUpdate()
    {
        // Keep the plug locked in its port while plugged
        if (pluggedPort != 0 && hdmiPlug != null && !hdmiPlug.isSelected) PlacePlug();
    }

    // ---------- TV power ----------
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

    // Also callable from a UnityEvent
    public void PressPower()
    {
        if (tvOn) return;                              // already on: extra clicks do nothing
        if (Time.time - lastPressTime < 0.5f) return;  // ignore double fire
        lastPressTime = Time.time;

        tvOn = true;
        if (noSignalScreen == null && pictureScreen == null)
            Debug.LogWarning("[TVTutorialTask] TV turned on, but No Signal Screen and Picture Screen are empty - nothing to show!", this);
        Play(powerSound);
        UpdateScreen();
    }

    private void UpdateScreen()
    {
        bool picture = tvOn && pluggedPort == 1;
        if (noSignalScreen != null) noSignalScreen.SetActive(tvOn && !picture);
        if (pictureScreen != null) pictureScreen.SetActive(picture);
        if (picture) pictureShown = true;
    }

    // ---------- Step 2: look at the ports ----------
    private void CheckPortsHover()
    {
        if (portsChecked || !tvOn || portsToCheck == null) return;
        foreach (var p in portsToCheck)
            if (p != null && p.isHovered) { portsChecked = true; return; }
    }

    // ---------- HDMI plug ----------
    private void OnPlugGrabbed(SelectEnterEventArgs args)
    {
        if (pluggedPort == 0) return;
        pluggedPort = 0;
        unplugged = true;
        portsChecked = true;  // they found the cable, so they checked the ports
        armed = false;        // must move away before it can snap again
        Play(unplugSound);
        UpdateScreen();
    }

    private void OnPlugReleased(SelectExitEventArgs args)
    {
        if (snappingNow || pluggedPort != 0) return;
        StartCoroutine(DropNextFrame());
    }

    private IEnumerator DropNextFrame()
    {
        yield return null; // after XR Grab restores its settings
        if (pluggedPort != 0 || plugRb == null) yield break;
        plugRb.isKinematic = false;
        plugRb.useGravity = true;
    }

    // Snap as soon as the plug (held) touches a port
    private void CheckPlugTouch()
    {
        if (hdmiPlug == null || !hdmiPlug.isSelected || pluggedPort != 0) return;

        int port = 0;
        if (IsNear(hdmi1Spot)) port = 1;
        else if (IsNear(hdmi2Spot)) port = 2;

        if (port == 0) { armed = true; return; }
        if (!armed) return;
        armed = false;

        snappingNow = true;
        var hand = hdmiPlug.firstInteractorSelecting;
        var manager = hdmiPlug.interactionManager;
        if (hand != null && manager != null) manager.SelectExit(hand, hdmiPlug);
        snappingNow = false;

        pluggedPort = port;
        PlacePlug();
        Play(plugSound);
        UpdateScreen();
    }

    private bool IsNear(Transform spot)
    {
        if (spot == null) return false;
        Vector3 p = spot.position;
        if (Vector3.Distance(hdmiPlug.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in hdmiPlug.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private void PlacePlug()
    {
        Transform spot = pluggedPort == 1 ? hdmi1Spot : pluggedPort == 2 ? hdmi2Spot : null;
        if (spot == null || hdmiPlug == null) return;

        if (plugRb != null)
        {
            if (!plugRb.isKinematic) { plugRb.linearVelocity = Vector3.zero; plugRb.angularVelocity = Vector3.zero; }
            plugRb.isKinematic = true;
            plugRb.useGravity = false;
        }
        hdmiPlug.transform.SetPositionAndRotation(spot.position, spot.rotation);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[TVTutorialTask] A sound slot is broken. Set it to None and drag the clip in again.", this);
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

        if (currentStep == Step.Done)
            onTaskComplete?.Invoke();
    }

    private bool IsStepDone(Step step)
    {
        switch (step)
        {
            case Step.TurnOnTV: return tvOn;
            case Step.CheckPorts: return portsChecked;
            case Step.Unplug: return unplugged;
            case Step.PlugCorrect: return pluggedPort == 1;
            case Step.CheckScreen: return pictureShown;
        }
        return false;
    }
}