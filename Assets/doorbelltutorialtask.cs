using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// PSU MEDIUM TUTORIAL - "The doorbell won't ring"
// Attach to the DOORBELL BUTTON (needs collider + XR Simple Interactable + ClickOnly).
// Reuses PaperclipSlot (test wire) and FrontPanelPlug (button wire plug).
//
// Steps (checked in order, like PSUMediumManager):
//   1 Press the doorbell button          - nothing happens
//   2 Connect the test wire to the bell  - bell rings = bell works
//   3 Remove the test wire
//   4 Move the button's wire plug to the BELL terminals
//   5 Press the doorbell button          - bell rings
// When done, onTaskComplete fires (wire to TutorialFlowManager.OnCablePlugged()).
//
// Works with ClickOnly: the button is NOT grabbable. Point the ray at it and
// pull the TRIGGER to press it. This script reads the trigger itself, so it
// works even if TriggerClicker misses the button.
// Audio: ONE Audio Source (leave empty = uses the one on this object), sounds are clips.
public class DoorbellTutorialTask : MonoBehaviour
{
    public enum Step { PressButtonFirst, ConnectTestWire, RemoveTestWire, FixButtonWires, PressButtonFinal, Done }

    [Header("Parts")]
    [Tooltip("PaperclipSlot placed between the bell's two terminals (its Bent Paperclip = the test wire)")]
    public PaperclipSlot testWireSlot;
    [Tooltip("FrontPanelPlug on the button's wire plug (LIGHT spot = start, BELL spot = correct)")]
    public FrontPanelPlug buttonWirePlug;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip bellSound;
    public AudioClip buttonClickSound;

    [Header("Bell Feedback")]
    [Tooltip("Optional: the bell (or its hammer) shakes while ringing")]
    public Transform bellToShake;
    public float shakeTime = 0.8f;
    public float shakeAngle = 8f;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-5 (group 0 is 'speak to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5 };
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager.OnCablePlugged()")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.PressButtonFirst;
    public bool pressedOnce = false;
    public bool bellTestPassed = false;
    public bool finalRing = false;

    private XRBaseInteractable button;
    private bool lastClipState = false;
    private Coroutine shakeRoutine;
    private Quaternion bellStartRotation;
    private float lastPressTime = -1f;
    private bool leftWasDown, rightWasDown;

    private bool TestWireIn => testWireSlot != null && testWireSlot.hasClip;
    private bool WiresCorrect => buttonWirePlug != null && buttonWirePlug.IsCorrect;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();

        // ClickOnly fires selectEntered when the player trigger-clicks the button
        if (button != null)
            button.selectEntered.AddListener(OnSelected);

        if (bellToShake != null)
            bellStartRotation = bellToShake.localRotation;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        PressButton();
    }

    private void Update()
    {
        CheckTriggerClick();

        // Test wire just connected -> the bell rings by itself
        bool clipIn = TestWireIn;
        if (clipIn && !lastClipState)
        {
            Ring();
            bellTestPassed = true;
        }
        lastClipState = clipIn;

        TryAdvance();
    }

    // Ray pointing at the button + trigger pulled -> press it
    private void CheckTriggerClick()
    {
        bool leftDown = TriggerDown(XRNode.LeftHand);
        bool rightDown = TriggerDown(XRNode.RightHand);
        bool newPress = (leftDown && !leftWasDown) || (rightDown && !rightWasDown);
        leftWasDown = leftDown;
        rightWasDown = rightDown;

        if (newPress && button != null && button.isHovered)
            PressButton();
    }

    private static bool TriggerDown(XRNode hand)
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);
        return device.isValid &&
               device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) &&
               pressed;
    }

    // ---------- Doorbell button ----------
    // Also callable from a UnityEvent if needed
    public void PressButton()
    {
        // Ignore a double fire from the same click
        if (Time.time - lastPressTime < 0.2f) return;
        lastPressTime = Time.time;

        Play(buttonClickSound);
        pressedOnce = true;

        if (WiresCorrect && !TestWireIn)
        {
            Ring();
            // Counts even if the checklist is still switching to step 5
            finalRing = true;
        }
        // otherwise nothing happens - the symptom
    }

    private void Ring()
    {
        Play(bellSound);
        if (bellToShake != null)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(Shake());
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try
        {
            audioSource.PlayOneShot(clip);
        }
        catch (System.Exception)
        {
            // A broken sound slot (e.g. "Type mismatch") must never stop the button from working
            Debug.LogWarning("[DoorbellTutorialTask] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private IEnumerator Shake()
    {
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            float angle = Mathf.Sin(t * 60f) * shakeAngle * (1f - t / shakeTime);
            bellToShake.localRotation = bellStartRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        bellToShake.localRotation = bellStartRotation;
        shakeRoutine = null;
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

        if (currentStep == Step.Done)
            onTaskComplete?.Invoke();
    }

    private bool IsStepDone(Step step)
    {
        switch (step)
        {
            case Step.PressButtonFirst: return pressedOnce;
            case Step.ConnectTestWire: return bellTestPassed;
            case Step.RemoveTestWire: return !TestWireIn;
            case Step.FixButtonWires: return WiresCorrect;
            case Step.PressButtonFinal: return finalRing;
        }
        return false;
    }
}