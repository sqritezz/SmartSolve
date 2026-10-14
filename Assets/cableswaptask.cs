using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// GPU MEDIUM TUTORIAL - "The TV picture is glitchy"
// The HDMI cable is in the RIGHT port, but the cable itself is damaged -> replace it.
//
// Attach to the TV's POWER BUTTON (needs collider + XR Simple Interactable + ClickOnly).
//
// Steps (checked in order, ticks the checklist):
//   1 Turn on the TV                  - glitchy picture
//   2 Check the cable and port        - point at the port / cable (or grab the old cable)
//   3 Unplug the damaged cable        - grab it out of HDMI 1
//   4 Plug in the new cable           - touch the NEW plug to HDMI 1, it snaps in
//   5 Check the TV                    - TV on + new cable in -> clear picture
// When done, onTaskComplete fires (wire to TutorialFlowManager.OnCablePlugged).
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class CableSwapTutorialTask : MonoBehaviour
{
    public enum Step { TurnOnTV, CheckCable, UnplugOld, PlugNew, CheckScreen, Done }

    [Header("Screens (children of the TV)")]
    [Tooltip("Quad with TV_Glitch.png (shown while the damaged cable is in)")]
    public GameObject glitchScreen;
    [Tooltip("Quad with TV_NoSignal.png (shown when no cable is plugged in) - optional")]
    public GameObject noSignalScreen;
    [Tooltip("Quad with TV_Picture.png (clear picture)")]
    public GameObject pictureScreen;

    [Header("Cables")]
    [Tooltip("The damaged cable's plug - starts plugged into HDMI 1")]
    public XRGrabInteractable oldCable;
    [Tooltip("The new cable's plug - starts on the table")]
    public XRGrabInteractable newCable;
    [Tooltip("Empty at the HDMI 1 port. Blue Z arrow points OUT of the TV.")]
    public Transform portSpot;
    [Tooltip("How close (meters) any part of a plug must get to the port to snap in")]
    public float snapDistance = 0.05f;

    [Header("Things to check (step 2)")]
    [Tooltip("XR Simple Interactables the player can point at (the port, the old cable...). Optional.")]
    public XRBaseInteractable[] thingsToCheck;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerSound;
    public AudioClip plugSound;
    public AudioClip unplugSound;
    public AudioClip successSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-5 (group 0 is 'talk to the NPC'). Use -1 to skip ticking a step.")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5 };
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager.OnCablePlugged")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.TurnOnTV;
    public bool tvOn = false;
    public bool checkedCable = false;
    public bool oldRemoved = false;
    public bool pictureShown = false;

    // Which cable is in the port: null = empty
    private XRGrabInteractable plugged;
    private XRBaseInteractable button;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool snappingNow = false;
    private XRGrabInteractable justRemoved; // must move away before it can snap again

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(_ => PressPower());

        SetupCable(oldCable);
        SetupCable(newCable);
    }

    private void SetupCable(XRGrabInteractable c)
    {
        if (c == null) return;
        var rb = c.GetComponent<Rigidbody>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        c.throwOnDetach = false;
        c.selectEntered.AddListener(_ => OnGrabbed(c));
        c.selectExited.AddListener(_ => OnReleased(c));
    }

    private void Start()
    {
        currentStep = Step.TurnOnTV;
        tvOn = false; checkedCable = false; oldRemoved = false; pictureShown = false;
        plugged = oldCable;
        PlacePlugged();
        UpdateScreen();
    }

    private void Update()
    {
        CheckTriggerClick();
        CheckLooking();
        CheckSnap(newCable);
        CheckSnap(oldCable);
        TryAdvance();
    }

    private void LateUpdate()
    {
        if (plugged != null && !plugged.isSelected) PlacePlugged();
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
        if (Time.time - lastPressTime < 0.2f) return;
        lastPressTime = Time.time;
        tvOn = !tvOn;
        Play(powerSound);
        UpdateScreen();
    }

    private void UpdateScreen()
    {
        bool clear = tvOn && plugged != null && plugged == newCable;
        bool glitch = tvOn && plugged != null && plugged == oldCable;
        bool none = tvOn && plugged == null;

        if (glitchScreen != null) glitchScreen.SetActive(glitch);
        if (noSignalScreen != null) noSignalScreen.SetActive(none);
        if (pictureScreen != null) pictureScreen.SetActive(clear);

        if (clear && !pictureShown) { pictureShown = true; Play(successSound); }
    }

    // ---------- Step 2 ----------
    private void CheckLooking()
    {
        if (checkedCable || !tvOn || thingsToCheck == null) return;
        foreach (var t in thingsToCheck)
            if (t != null && t.isHovered) { checkedCable = true; return; }
    }

    // ---------- Cables ----------
    private void OnGrabbed(XRGrabInteractable c)
    {
        if (plugged != c) return;
        plugged = null;
        justRemoved = c;
        if (c == oldCable) { oldRemoved = true; checkedCable = true; }
        Play(unplugSound);
        UpdateScreen();
    }

    private void OnReleased(XRGrabInteractable c)
    {
        if (snappingNow || plugged == c) return;
        StartCoroutine(DropNextFrame(c));
    }

    private IEnumerator DropNextFrame(XRGrabInteractable c)
    {
        yield return null;
        if (plugged == c) yield break;
        var rb = c.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
    }

    private void CheckSnap(XRGrabInteractable c)
    {
        if (c == null || !c.isSelected || plugged != null) return;

        bool near = IsNear(c);
        if (!near) { if (justRemoved == c) justRemoved = null; return; }
        if (justRemoved == c) return; // just pulled out, move away first

        snappingNow = true;
        var hand = c.firstInteractorSelecting;
        var mgr = c.interactionManager;
        if (hand != null && mgr != null) mgr.SelectExit(hand, c);
        snappingNow = false;

        plugged = c;
        PlacePlugged();
        Play(plugSound);
        UpdateScreen();
    }

    private bool IsNear(XRGrabInteractable c)
    {
        if (portSpot == null) return false;
        Vector3 p = portSpot.position;
        if (Vector3.Distance(c.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var col in c.GetComponentsInChildren<Collider>())
            if (col.enabled && col.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private void PlacePlugged()
    {
        if (plugged == null || portSpot == null) return;
        var rb = plugged.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true; rb.useGravity = false;
        }
        plugged.transform.SetPositionAndRotation(portSpot.position, portSpot.rotation);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[CableSwapTutorialTask] A sound slot is broken. Set it to None and drag the clip in again.", this);
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
            case Step.CheckCable: return checkedCable;
            case Step.UnplugOld: return oldRemoved;
            case Step.PlugNew: return plugged == newCable;
            case Step.CheckScreen: return pictureShown;
        }
        return false;
    }
}