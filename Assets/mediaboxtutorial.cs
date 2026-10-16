using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU HARD TUTORIAL - "The media box has no power"
// The TV and the HDMI cable are fine, but the media box's power adapter is unplugged,
// so the box is dead (light off) and the TV shows "No Signal".
//
// Attach to the TV's POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
//
// Steps (checked in order, ticks the checklist):
//   1 Turn on the TV                - "No Signal"
//   2 Check the media box           - point at it: its light is OFF
//   3 Plug in the power adapter     - touch the DC plug to the media box's power port, it snaps in
//   4 Check the TV                  - box light turns green, picture appears
// When done, onTaskComplete fires (wire to TutorialFlowManager.OnCablePlugged).
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class MediaBoxPowerTutorialTask : MonoBehaviour
{
    public enum Step { TurnOnTV, CheckBox, PlugPower, CheckScreen, Done }

    [Header("TV screens")]
    public GameObject noSignalScreen;
    public GameObject pictureScreen;

    [Header("Media box")]
    [Tooltip("The media box's XR Simple Interactable (pointing at it = checking it)")]
    public XRBaseInteractable mediaBox;
    [Tooltip("Green light shown while the box has power")]
    public GameObject boxLightOn;
    [Tooltip("Optional: dark light shown while the box has no power")]
    public GameObject boxLightOff;

    [Header("Power adapter")]
    [Tooltip("The adapter's DC plug (XR Grab Interactable + Rigidbody + Collider). Starts on the table.")]
    public XRGrabInteractable dcPlug;
    [Tooltip("Empty at the media box's power port. Blue Z arrow points OUT of the box.")]
    public Transform powerPortSpot;
    public float snapDistance = 0.05f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerSound;
    public AudioClip plugSound;
    public AudioClip unplugSound;
    public AudioClip successSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-4 (group 0 is 'talk to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4 };
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager.OnCablePlugged")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.TurnOnTV;
    public bool tvOn = false;
    public bool boxChecked = false;
    public bool powered = false;
    public bool pictureShown = false;

    private XRBaseInteractable button;
    private Rigidbody plugRb;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool armed = true, snappingNow = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(_ => PressPower());

        if (dcPlug != null)
        {
            plugRb = dcPlug.GetComponent<Rigidbody>();
            if (plugRb != null) plugRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            dcPlug.throwOnDetach = false;
            dcPlug.selectEntered.AddListener(_ => OnPlugGrabbed());
            dcPlug.selectExited.AddListener(_ => OnPlugReleased());
        }
    }

    private void Start()
    {
        currentStep = Step.TurnOnTV;
        tvOn = boxChecked = powered = pictureShown = false;
        UpdateAll();
    }

    private void Update()
    {
        CheckTriggerClick();
        if (!boxChecked && tvOn && mediaBox != null && mediaBox.isHovered) boxChecked = true;
        CheckPlugSnap();
        TryAdvance();
    }

    private void LateUpdate()
    {
        if (powered && dcPlug != null && !dcPlug.isSelected) PlacePlug();
    }

    // ---------- TV power ----------
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
        if (tvOn) return;                              // already on: extra clicks do nothing
        if (Time.time - lastPressTime < 0.5f) return;
        lastPressTime = Time.time;
        tvOn = true;
        Play(powerSound);
        UpdateAll();
    }

    private void UpdateAll()
    {
        if (boxLightOn != null) boxLightOn.SetActive(powered);
        if (boxLightOff != null) boxLightOff.SetActive(!powered);

        bool picture = tvOn && powered;
        if (noSignalScreen != null) noSignalScreen.SetActive(tvOn && !picture);
        if (pictureScreen != null) pictureScreen.SetActive(picture);

        if (picture && !pictureShown) { pictureShown = true; Play(successSound); }
    }

    // ---------- DC plug ----------
    private void OnPlugGrabbed()
    {
        boxChecked = true; // they found the loose adapter
        if (!powered) return;
        powered = false;
        armed = false;
        Play(unplugSound);
        UpdateAll();
    }

    private void OnPlugReleased()
    {
        if (snappingNow || powered) return;
        StartCoroutine(DropNextFrame());
    }

    private IEnumerator DropNextFrame()
    {
        yield return null;
        if (powered || plugRb == null) yield break;
        plugRb.isKinematic = false; plugRb.useGravity = true;
    }

    private void CheckPlugSnap()
    {
        if (dcPlug == null || !dcPlug.isSelected || powered || powerPortSpot == null) return;

        bool near = IsNear();
        if (!near) { armed = true; return; }
        if (!armed) return;
        armed = false;

        snappingNow = true;
        var hand = dcPlug.firstInteractorSelecting;
        var mgr = dcPlug.interactionManager;
        if (hand != null && mgr != null) mgr.SelectExit(hand, dcPlug);
        snappingNow = false;

        powered = true;
        PlacePlug();
        Play(plugSound);
        UpdateAll();
    }

    private bool IsNear()
    {
        Vector3 p = powerPortSpot.position;
        if (Vector3.Distance(dcPlug.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in dcPlug.GetComponentsInChildren<Collider>())
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
        dcPlug.transform.SetPositionAndRotation(powerPortSpot.position, powerPortSpot.rotation);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[MediaBoxPowerTutorialTask] A sound slot is broken. Set it to None and drag the clip in again.", this);
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
            int nextGroup = i + 1 < stepGroupIndices.Length ? stepGroupIndices[i + 1] : -1;
            if (IsStepDone(currentStep) && nextGroup >= 0 && checklist.IsCurrentStep(nextGroup, objectiveIndex))
                currentStep++;
            return;
        }

        if (!IsStepDone(currentStep)) return;

        if (checklist != null && group >= 0)
            checklist.CompleteObjective(group, objectiveIndex);

        currentStep++;
        if (currentStep == Step.Done) onTaskComplete?.Invoke();
    }

    private bool IsStepDone(Step step)
    {
        switch (step)
        {
            case Step.TurnOnTV: return tvOn;
            case Step.CheckBox: return boxChecked;
            case Step.PlugPower: return powered;
            case Step.CheckScreen: return pictureShown;
        }
        return false;
    }
}