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
    public AudioClip snapClip;
    [Tooltip("Prints how far the case is from its spot when you let go (for testing)")]
    public bool logSnapDistance = false;

    [Header("Cleanable Parts")]
    public PracticeCleanable bladesCleanable;
    public PracticeCleanable caseCleanable;

    [Header("Spinning Blades")]
    public Transform spinningBlades;
    public Vector3 spinAxis = Vector3.forward;
    public float maxSpinSpeed = 720f;

    [Tooltip("Seconds to spin up / spin down")]
    public float spinChangeTime = 1f;

    [Header("Audio (ONE Audio Source for everything)")]
    [Tooltip("Leave empty to use the Audio Source on this object (one is added if missing)")]
    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    [Tooltip("Looping hum while the fan is on")]
    public AudioClip fanHumClip;
    [Range(0f, 1f)] public float fanHumVolume = 0.6f;
    [Tooltip("The hum only plays when the player is within this many meters of the fan (so it can't be heard from other levels)")]
    public float humHearDistance = 6f;
    [Tooltip("Plays when the button is pressed at the wrong time")]
    public AudioClip errorClip;

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
    private Vector3 caseOriginalCenterLocal;

    private void Awake()
    {
        SetupAudio();

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

        StartHum();

        if (caseRigidbody != null)
        {
            Transform t = caseRigidbody.transform;
            caseOriginalParent = t.parent;
            caseOriginalLocalPos = t.localPosition;
            caseOriginalLocalRot = t.localRotation;

            // Remember where the case's VISIBLE middle sits (its pivot may be far away)
            Vector3 center = CaseVisualCenter();
            caseOriginalCenterLocal = caseOriginalParent != null
                ? caseOriginalParent.InverseTransformPoint(center)
                : center;

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
        UpdateHum();

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
            StopHum();

            CompleteStep(Step.TurnOff);
            currentStep = Step.RemoveCase;

            if (caseGrab != null) caseGrab.enabled = true; // case can now be removed
        }
        else if (currentStep == Step.TurnOn)
        {
            PlayClick();
            isFanOn = true;
            StartHum();

            CompleteStep(Step.TurnOn);
            currentStep = Step.Done;

            onTaskComplete?.Invoke();
        }
        else
        {
            // Pressed while the fan is open (or after done) - fan must stay off
            PlayClip(errorClip);
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

    // Compares the case's VISIBLE middle with where it was on the fan,
    // so it snaps even if the case is held tilted or its pivot is far away
    private bool IsCaseNearOriginalSpot()
    {
        Vector3 originalCenter = caseOriginalParent != null
            ? caseOriginalParent.TransformPoint(caseOriginalCenterLocal)
            : caseOriginalCenterLocal;

        float distance = Vector3.Distance(CaseVisualCenter(), originalCenter);
        if (logSnapDistance)
            Debug.Log("[PracticeFanTask] Case is " + distance.ToString("F2") + " m from its spot (snaps within " + snapDistance + ")");
        return distance <= snapDistance;
    }

    private Vector3 CaseVisualCenter()
    {
        Renderer[] renderers = caseRigidbody.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return caseRigidbody.transform.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.center;
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

        PlayClip(snapClip);

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
        PlayClip(buttonClickClip);
    }

    // ---------- Audio (one Audio Source) ----------
    private AudioSource humSource; // made automatically, copies the main source's settings

    private void SetupAudio()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null) SetupAudio();
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    // The hum loops, so it runs on a hidden copy of the Audio Source
    // (so stopping it never cuts off the click sounds)
    private bool humWanted = false;

    // Fan is on -> hum is allowed (it still only plays when the player is close)
    private void StartHum()
    {
        humWanted = true;
        UpdateHum();
    }

    private void UpdateHum()
    {
        bool playerNear = Camera.main != null &&
            Vector3.Distance(Camera.main.transform.position, transform.position) <= humHearDistance;
        bool shouldPlay = humWanted && playerNear && fanHumClip != null;

        if (!shouldPlay)
        {
            if (humSource != null && humSource.isPlaying) humSource.Stop();
            return;
        }

        if (audioSource == null) SetupAudio();

        if (humSource == null)
        {
            GameObject go = new GameObject("Fan Hum (auto)");
            go.transform.SetParent(audioSource.transform, false);
            humSource = go.AddComponent<AudioSource>();
            humSource.playOnAwake = false;
            humSource.loop = true;
            humSource.spatialBlend = audioSource.spatialBlend;
            humSource.minDistance = audioSource.minDistance;
            humSource.maxDistance = audioSource.maxDistance;
            humSource.rolloffMode = audioSource.rolloffMode;
            humSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        }

        humSource.clip = fanHumClip;
        humSource.volume = audioSource.volume * fanHumVolume;
        if (!humSource.isPlaying) humSource.Play();
    }

    private void StopHum()
    {
        humWanted = false;
        if (humSource != null) humSource.Stop();
    }
}