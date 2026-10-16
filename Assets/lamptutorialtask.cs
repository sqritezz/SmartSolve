using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// PSU HARD TUTORIAL - "The lamp bulb blew"
// Same idea as PSU Hard: a part burns out with sparks -> make it safe ->
// remove the broken part -> throw it away -> install the new one -> test.
//
// Attach to the LAMP SWITCH (needs collider + XR Simple Interactable + ClickOnly).
// Point the ray at the switch and pull the TRIGGER to flip it.
//
// Steps (checked in order):
//   1 Turn on the lamp                  - bulb flickers, pops, sparks, goes dark
//   2 Turn off the lamp switch          - safety first
//   3 Remove the old bulb               - grab it out of the socket
//   4 Throw the old bulb in the trash   - use TrashBin (it hides the bulb)
//   5 Put the new bulb in the socket
//   6 Turn on the lamp                  - it lights up!
// When done, onTaskComplete fires (wire to TutorialFlowManager).
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class LampTutorialTask : MonoBehaviour
{
    public enum Step { TurnOnFirst, TurnOff, RemoveOldBulb, TrashOldBulb, InstallNewBulb, TurnOnFinal, Done }

    [Header("Parts")]
    [Tooltip("The burnt bulb, sitting in the socket at the start (XR Grab Interactable)")]
    public XRGrabInteractable oldBulb;
    [Tooltip("The new bulb, on the table (XR Grab Interactable)")]
    public XRGrabInteractable newBulb;
    [Tooltip("Empty object inside the lamp where a bulb sits")]
    public Transform bulbSocket;
    [Tooltip("How close (meters) any part of the new bulb must get to the socket to snap in")]
    public float snapDistance = 0.08f;
    [Tooltip("ON = the socket moves to wherever you placed the Old Bulb in the editor, " +
             "so the bulbs keep the exact position/rotation you set (recommended)")]
    public bool useOldBulbPlacement = true;

    [Header("Light")]
    [Tooltip("The lamp's Light component (starts off)")]
    public Light lampLight;
    [Tooltip("Optional: spark particle effect, plays when the old bulb pops")]
    public ParticleSystem sparkEffect;

    [Header("Messages (optional)")]
    [Tooltip("e.g. \"Turn off the lamp before touching the bulb!\"")]
    public GameObject switchOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip switchSound;
    public AudioClip popSound;       // e.g. "short circuit"
    [Tooltip("Cut the flicker/pop sound after this many seconds (0 = play it fully)")]
    public float popMaxLength = 0.6f;
    [Tooltip("Fade-out time (seconds) at the cut")]
    public float popFadeOut = 0.1f;
    public AudioClip insertSound;    // e.g. "screwing" or "snap sound"
    public AudioClip warningSound;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    [Tooltip("Group index for each step 1-6 (group 0 is 'speak to the NPC')")]
    public int[] stepGroupIndices = { 1, 2, 3, 4, 5, 6 };
    public int objectiveIndex = 0;

    [Header("Events")]
    [Tooltip("Wire to TutorialFlowManager (like the doorbell tutorial)")]
    public UnityEvent onTaskComplete;

    [Header("State (read-only)")]
    public Step currentStep = Step.TurnOnFirst;
    public bool lampOn = false;
    public bool bulbBlown = false;
    public bool oldBulbRemoved = false;
    public bool newBulbIn = false;
    public bool finalLight = false;

    private XRBaseInteractable lampSwitch;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool newBulbArmed = true;

    private bool OldBulbTrashed => oldBulb == null || !oldBulb.gameObject.activeInHierarchy;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        lampSwitch = GetComponent<XRBaseInteractable>();
        if (lampSwitch == null) lampSwitch = GetComponentInParent<XRBaseInteractable>();
        if (lampSwitch != null) lampSwitch.selectEntered.AddListener(OnSwitchSelected);

        if (oldBulb != null)
        {
            oldBulb.selectEntered.AddListener(OnOldBulbGrabbed);
            oldBulb.selectExited.AddListener(OnOldBulbReleased);
            oldBulb.throwOnDetach = false;
        }
        if (newBulb != null)
        {
            newBulb.selectEntered.AddListener(OnNewBulbGrabbed);
            newBulb.selectExited.AddListener(OnNewBulbReleased);
        }
    }

    private void Start()
    {
        if (lampLight != null) lampLight.enabled = false;
        if (switchOnWarning != null) switchOnWarning.SetActive(false);

        // Use the Old Bulb's editor placement as the socket pose
        if (useOldBulbPlacement && oldBulb != null && bulbSocket != null)
            bulbSocket.SetPositionAndRotation(oldBulb.transform.position, oldBulb.transform.rotation);

        // Old bulb sits in the socket and doesn't fall
        if (oldBulb != null && bulbSocket != null)
        {
            oldBulb.transform.SetPositionAndRotation(bulbSocket.position, bulbSocket.rotation);
            Freeze(oldBulb);
        }
    }

    private void OnDestroy()
    {
        if (lampSwitch != null) lampSwitch.selectEntered.RemoveListener(OnSwitchSelected);
    }

    private void Update()
    {
        CheckTriggerClick();
        CheckNewBulbTouch();
        TryAdvance();
    }

    private void LateUpdate()
    {
        // Keep bulbs locked in the socket while they're in it
        if (bulbSocket == null) return;
        if (!oldBulbRemoved && oldBulb != null && !oldBulb.isSelected)
            oldBulb.transform.SetPositionAndRotation(bulbSocket.position, bulbSocket.rotation);
        if (newBulbIn && newBulb != null && !newBulb.isSelected)
            newBulb.transform.SetPositionAndRotation(bulbSocket.position, bulbSocket.rotation);
    }

    // ---------- Lamp switch ----------
    private void OnSwitchSelected(SelectEnterEventArgs args) => PressSwitch();

    private void CheckTriggerClick()
    {
        bool leftDown = TriggerDown(XRNode.LeftHand);
        bool rightDown = TriggerDown(XRNode.RightHand);
        bool newPress = (leftDown && !leftWasDown) || (rightDown && !rightWasDown);
        leftWasDown = leftDown;
        rightWasDown = rightDown;

        if (newPress && lampSwitch != null && lampSwitch.isHovered)
            PressSwitch();
    }

    private static bool TriggerDown(XRNode hand)
    {
        var device = InputDevices.GetDeviceAtXRNode(hand);
        return device.isValid &&
               device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) &&
               pressed;
    }

    // Also callable from a UnityEvent if needed
    public void PressSwitch()
    {
        if (Time.time - lastPressTime < 0.2f) return; // ignore double fire
        lastPressTime = Time.time;

        lampOn = !lampOn;
        Play(switchSound);

        if (!lampOn)
        {
            if (lampLight != null) lampLight.enabled = false;
            return;
        }

        if (!bulbBlown && !oldBulbRemoved)
        {
            StartCoroutine(BlowBulb());           // first time: the old bulb pops
        }
        else if (newBulbIn)
        {
            if (lampLight != null) lampLight.enabled = true;   // fixed!
            finalLight = true;
        }
        // otherwise: no working bulb -> stays dark
    }

    private IEnumerator BlowBulb()
    {
        // Flicker a few times, then pop
        for (int i = 0; i < 4; i++)
        {
            if (lampLight != null) lampLight.enabled = true;
            yield return new WaitForSeconds(0.08f + i * 0.04f);
            if (lampLight != null) lampLight.enabled = false;
            yield return new WaitForSeconds(0.06f);
        }
        PlayCut(popSound, popMaxLength, popFadeOut);
        if (sparkEffect != null) sparkEffect.Play();
        bulbBlown = true;
    }

    // ---------- Old bulb ----------
    private void OnOldBulbGrabbed(SelectEnterEventArgs args)
    {
        if (lampOn)
        {
            Play(warningSound);
            ShowMessage(switchOnWarning);
            StartCoroutine(ForceDrop(oldBulb));
            return;
        }
        if (!oldBulbRemoved)
        {
            oldBulbRemoved = true;
            Play(insertSound);
        }
    }

    // Once it's out of the lamp, let it fall normally (so it can be dropped in the trash)
    private void OnOldBulbReleased(SelectExitEventArgs args)
    {
        if (!oldBulbRemoved) return;
        StartCoroutine(EnableGravityNextFrame(oldBulb));
    }

    private IEnumerator EnableGravityNextFrame(XRGrabInteractable obj)
    {
        yield return null; // after XR Grab restores its own settings
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
    }

    // ---------- New bulb ----------
    private void OnNewBulbGrabbed(SelectEnterEventArgs args)
    {
        if (!newBulbIn) return;
        newBulbIn = false;     // pulled back out
        newBulbArmed = false;  // must move away before it can snap again
        finalLight = false;
        if (lampLight != null) lampLight.enabled = false;
    }

    private void OnNewBulbReleased(SelectExitEventArgs args)
    {
        if (!newBulbIn && newBulb != null)
            StartCoroutine(EnableGravityNextFrame(newBulb));
    }

    // Snap in as soon as the new bulb touches the socket
    private void CheckNewBulbTouch()
    {
        if (newBulbIn || newBulb == null || bulbSocket == null || !newBulb.isSelected) return;

        bool near = IsNear(newBulb);
        if (!near) { newBulbArmed = true; return; }
        if (!newBulbArmed) return;
        newBulbArmed = false;

        if (!oldBulbRemoved) return; // old bulb still in the socket

        if (lampOn)
        {
            Play(warningSound);
            ShowMessage(switchOnWarning);
            return;
        }

        var hand = newBulb.firstInteractorSelecting;
        var manager = newBulb.interactionManager;
        newBulbIn = true; // set first so OnNewBulbReleased doesn't drop it
        if (hand != null && manager != null) manager.SelectExit(hand, newBulb);
        newBulb.transform.SetPositionAndRotation(bulbSocket.position, bulbSocket.rotation);
        Freeze(newBulb);
        Play(insertSound);
    }

    private bool IsNear(XRGrabInteractable obj)
    {
        Vector3 spot = bulbSocket.position;
        if (Vector3.Distance(obj.transform.position, spot) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in obj.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(spot) <= d2) return true;
        return false;
    }

    // ---------- Helpers ----------
    private IEnumerator ForceDrop(XRGrabInteractable obj)
    {
        obj.enabled = false;  // disabling makes XR let go
        yield return null;
        obj.enabled = true;
        if (bulbSocket != null) obj.transform.SetPositionAndRotation(bulbSocket.position, bulbSocket.rotation);
        Freeze(obj);
    }

    private static void Freeze(XRGrabInteractable obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null) return;
        if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Plays one clip on its own extra Audio Source and cuts it off early (only that sound)
    private AudioSource cutSource;
    private Coroutine cutRoutine;
    private void PlayCut(AudioClip clip, float maxLength, float fade)
    {
        if (clip == null) return;
        if (maxLength <= 0f || audioSource == null) { Play(clip); return; }

        if (cutSource == null)
        {
            cutSource = gameObject.AddComponent<AudioSource>();
            cutSource.playOnAwake = false;
            cutSource.spatialBlend = audioSource.spatialBlend;
            cutSource.minDistance = audioSource.minDistance;
            cutSource.maxDistance = audioSource.maxDistance;
            cutSource.rolloffMode = audioSource.rolloffMode;
            cutSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        }
        if (cutRoutine != null) StopCoroutine(cutRoutine);
        cutSource.volume = audioSource.volume;
        cutSource.clip = clip;
        cutSource.Play();
        cutRoutine = StartCoroutine(CutAfter(maxLength, fade));
    }

    private IEnumerator CutAfter(float maxLength, float fade)
    {
        float startVol = cutSource.volume;
        yield return new WaitForSeconds(Mathf.Max(0f, maxLength - fade));
        for (float t = 0f; t < fade; t += Time.deltaTime)
        {
            cutSource.volume = Mathf.Lerp(startVol, 0f, t / fade);
            yield return null;
        }
        cutSource.Stop();
        cutSource.volume = startVol;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[LampTutorialTask] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private void ShowMessage(GameObject message)
    {
        if (message != null) StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }

    // ---------- Step checking ----------
    private void TryAdvance()
    {
        if (currentStep == Step.Done) return;

        int i = (int)currentStep;
        int group = i < stepGroupIndices.Length ? stepGroupIndices[i] : -1;

        if (checklist != null && group >= 0 && !checklist.IsCurrentStep(group, objectiveIndex))
        {
            // Another script (e.g. TrashBin) already ticked this step and the checklist
            // moved on -> catch up instead of getting stuck here
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
            case Step.TurnOnFirst: return bulbBlown;
            case Step.TurnOff: return !lampOn;
            case Step.RemoveOldBulb: return oldBulbRemoved;
            case Step.TrashOldBulb: return OldBulbTrashed;
            case Step.InstallNewBulb: return newBulbIn;
            case Step.TurnOnFinal: return finalLight;
        }
        return false;
    }
}