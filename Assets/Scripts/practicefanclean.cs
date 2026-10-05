using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// PSU EASY TUTORIAL - practice task manager.
// Attach to the desk fan's root object (80s_desk_fan_low_poly).
// Runs the 6 steps in strict order:
//   0. Turn off the fan (click the button)      - blades spin down
//   1. Remove the fan case (grab it)            - only grabbable once fan is off
//   2. Clean the blades with the brush
//   3. Clean the fan case with the brush
//   4. Put the case back (release it near its original spot - it snaps in)
//   5. Turn the fan back on (click the button)  - blades spin clean
// When all 6 are done, onTaskComplete fires (wire to TutorialFlowManager).
public class PracticeFanTask : MonoBehaviour
{
    public enum Step { TurnOff, RemoveCase, CleanBlades, CleanCase, ReattachCase, TurnOn, Done }

    [Header("Fan Button (needs collider + XR Simple Interactable)")]
    public XRBaseInteractable fanButton;

    [Header("Fan Case (needs Rigidbody + XR Grab Interactable)")]
    public XRBaseInteractable caseGrab;
    public Rigidbody caseRigidbody;

    [Tooltip("If on, the case falls with gravity when released (and not snapped back)")]
    public bool dropCaseOnRelease = true;

    [Header("Case Reattach")]
    [Tooltip("How close (meters) the case must be to its original spot when released to snap back")]
    public float snapDistance = 0.25f;
    public AudioSource snapSound;

    [Header("Cleanable Parts")]
    public PracticeCleanable bladesCleanable;
    public PracticeCleanable caseCleanable;

    [Header("Spinning Blades")]
    public Transform spinningBlades;
    public Vector3 spinAxis = Vector3.forward;
    public float maxSpinSpeed = 720f;

    [Tooltip("Seconds to spin up / spin down")]
    public float spinChangeTime = 1f;

    [Header("Audio")]
    public AudioSource buttonClickSound;
    [Tooltip("Looping hum while the fan is on (set Loop on the AudioSource)")]
    public AudioSource fanHumSound;
    [Tooltip("Plays when the button is pressed at the wrong time")]
    public AudioSource errorSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index of step 0. Steps 1-5 use the next groups in order.")]
    public int firstGroupIndex = 0;
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager.OnCablePlugged()")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.TurnOff;
    public bool isFanOn = true;

    private float currentSpinSpeed;

    // Where the case sits on the fan, remembered at start
    private Transform caseOriginalParent;
    private Vector3 caseOriginalLocalPos;
    private Quaternion caseOriginalLocalRot;

    private void Awake()
    {
        if (fanButton != null)
            fanButton.selectEntered.AddListener(OnButtonPressed);

        if (caseGrab != null)
        {
            caseGrab.selectEntered.AddListener(OnCaseGrabbed);
            caseGrab.selectExited.AddListener(OnCaseReleased);
        }

        if (bladesCleanable != null)
            bladesCleanable.onCleaned.AddListener(OnBladesCleaned);

        if (caseCleanable != null)
            caseCleanable.onCleaned.AddListener(OnCaseCleaned);
    }

    private void Start()
    {
        currentStep = Step.TurnOff;
        isFanOn = true;
        currentSpinSpeed = maxSpinSpeed;

        if (fanHumSound != null) fanHumSound.Play();

        if (caseRigidbody != null)
        {
            Transform t = caseRigidbody.transform;
            caseOriginalParent = t.parent;
            caseOriginalLocalPos = t.localPosition;
            caseOriginalLocalRot = t.localRotation;

            caseRigidbody.isKinematic = true;
            caseRigidbody.useGravity = false;
        }

        // Case is locked in place until the fan is turned off
        if (caseGrab != null) caseGrab.enabled = false;

        if (bladesCleanable != null) bladesCleanable.SetCanBeCleaned(false);
        if (caseCleanable != null) caseCleanable.SetCanBeCleaned(false);
    }

    private void Update()
    {
        float target = isFanOn ? maxSpinSpeed : 0f;
        float rate = spinChangeTime > 0f ? maxSpinSpeed / spinChangeTime : float.MaxValue;
        currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, target, rate * Time.deltaTime);

        if (spinningBlades != null && currentSpinSpeed > 0f)
            spinningBlades.Rotate(spinAxis, currentSpinSpeed * Time.deltaTime, Space.Self);
    }

    // ---------- Steps 0 and 5: fan button ----------
    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        PressButton();
    }

    public void PressButton()
    {
        if (currentStep == Step.TurnOff)
        {
            PlayClick();
            isFanOn = false;
            if (fanHumSound != null) fanHumSound.Stop();

            CompleteStep(Step.TurnOff);
            currentStep = Step.RemoveCase;

            if (caseGrab != null) caseGrab.enabled = true; // case can now be removed
        }
        else if (currentStep == Step.TurnOn)
        {
            PlayClick();
            isFanOn = true;
            if (fanHumSound != null) fanHumSound.Play();

            CompleteStep(Step.TurnOn);
            currentStep = Step.Done;

            onTaskComplete?.Invoke();
        }
        else
        {
            // Pressed while the fan is open (or after done) - fan must stay off
            if (errorSound != null) errorSound.Play();
        }
    }

    // ---------- Step 1: remove the case ----------
    private void OnCaseGrabbed(SelectEnterEventArgs args)
    {
        if (currentStep != Step.RemoveCase) return;

        CompleteStep(Step.RemoveCase);
        currentStep = Step.CleanBlades;

        if (bladesCleanable != null) bladesCleanable.SetCanBeCleaned(true);
    }

    // ---------- Step 4 check happens on release ----------
    private void OnCaseReleased(SelectExitEventArgs args)
    {
        if (caseRigidbody != null)
            StartCoroutine(HandleCaseReleaseNextFrame());
    }

    // XR Grab restores the Rigidbody's old state and parent on release,
    // so wait a frame before snapping or turning gravity on.
    private IEnumerator HandleCaseReleaseNextFrame()
    {
        yield return null;
        if (caseRigidbody == null) yield break;

        if (currentStep == Step.ReattachCase && IsCaseNearOriginalSpot())
        {
            SnapCaseBack();
            yield break;
        }

        if (dropCaseOnRelease)
        {
            caseRigidbody.isKinematic = false;
            caseRigidbody.useGravity = true;
        }
    }

    private Vector3 CaseOriginalWorldPosition()
    {
        return caseOriginalParent != null
            ? caseOriginalParent.TransformPoint(caseOriginalLocalPos)
            : caseOriginalLocalPos;
    }

    private bool IsCaseNearOriginalSpot()
    {
        float distance = Vector3.Distance(caseRigidbody.transform.position, CaseOriginalWorldPosition());
        return distance <= snapDistance;
    }

    private void SnapCaseBack()
    {
        caseRigidbody.isKinematic = true;
        caseRigidbody.useGravity = false;

        Transform t = caseRigidbody.transform;
        t.SetParent(caseOriginalParent, false);
        t.localPosition = caseOriginalLocalPos;
        t.localRotation = caseOriginalLocalRot;

        // Lock the case again so it can't be pulled off after reattaching
        if (caseGrab != null) caseGrab.enabled = false;

        if (snapSound != null) snapSound.Play();

        CompleteStep(Step.ReattachCase);
        currentStep = Step.TurnOn;
    }

    // ---------- Step 2: clean the blades ----------
    private void OnBladesCleaned()
    {
        if (currentStep != Step.CleanBlades) return;

        CompleteStep(Step.CleanBlades);
        currentStep = Step.CleanCase;

        if (caseCleanable != null) caseCleanable.SetCanBeCleaned(true);
    }

    // ---------- Step 3: clean the case ----------
    private void OnCaseCleaned()
    {
        if (currentStep != Step.CleanCase) return;

        CompleteStep(Step.CleanCase);
        currentStep = Step.ReattachCase;
    }

    // ---------- Helpers ----------
    private void CompleteStep(Step step)
    {
        if (checklist == null) return;

        int group = firstGroupIndex + (int)step;
        if (checklist.IsCurrentStep(group, objectiveIndex))
            checklist.CompleteObjective(group, objectiveIndex);
    }

    private void PlayClick()
    {
        if (buttonClickSound != null) buttonClickSound.Play();
    }
}