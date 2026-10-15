using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

// FINAL CHALLENGE - "The PC is completely dead. Fix it." (NO checklist, NO hints)
// One PC with THREE hidden problems, one from each stage:
//   PSU : the 24-pin connector is unplugged          -> pressing power does NOTHING
//   RAM : the RAM stick is loose (not seated)        -> PC turns on but BEEPS, no picture
//   GPU : monitor cable is in the MOTHERBOARD port   -> PC boots but monitor says "No Signal"
// The player has to read the symptoms and fix them in a sensible order.
//
// Attach to the PC POWER BUTTON (collider + XR Simple Interactable + ClickOnly).
//
// Safety: grabbing the 24-pin or the RAM while the PSU switch is ON = wrong attempt
// (the part is dropped). Putting the monitor cable back into the motherboard port = wrong attempt.
// When everything is fixed and the PC is on -> desktop shows, onAllFixed fires.
//
// Audio: ONE Audio Source (leave empty = uses the one on this object).
public class FinalChallengeManager : MonoBehaviour
{
    [Header("Monitor screens")]
    public GameObject offScreen;
    public GameObject noSignalScreen;
    public GameObject desktopScreen;

    [Header("PC running feedback (optional)")]
    public Transform[] fans;
    public Vector3 fanAxis = Vector3.forward;
    public float fanSpeed = 900f;
    public GameObject[] runningLights;

    [Header("PSU")]
    [Tooltip("The PSU's ON/OFF switch at the back. Starts ON.")]
    public PSUSwitch1 psuSwitch;
    [Tooltip("The 24-pin connector (XR Grab Interactable + Rigidbody + Collider). Starts UNPLUGGED, hanging loose.")]
    public XRGrabInteractable connector24Pin;
    [Tooltip("Empty at the motherboard's 24-pin socket. The connector takes this position/rotation when plugged.")]
    public Transform socket24PinSpot;

    [Header("RAM")]
    [Tooltip("The RAM stick (XR Grab Interactable + Rigidbody + Collider). Starts LOOSE (place it a bit raised / tilted in the slot).")]
    public XRGrabInteractable ramStick;
    [Tooltip("Empty at the RAM slot - the fully seated position/rotation.")]
    public Transform ramSlotSpot;

    [Header("Monitor cable")]
    [Tooltip("The monitor cable plug (XR Grab Interactable + Rigidbody + Collider). Starts in the MOTHERBOARD port.")]
    public XRGrabInteractable monitorCable;
    public Transform motherboardPortSpot;
    public Transform gpuPortSpot;

    [Tooltip("How close (meters) a part must get to its spot to snap in")]
    public float snapDistance = 0.05f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip powerOnSound;
    public AudioClip powerOffSound;
    [Tooltip("Played when power is pressed but nothing happens (24-pin unplugged)")]
    public AudioClip deadClickSound;
    [Tooltip("RAM error beeps - loops while the PC is on with loose RAM")]
    public AudioClip ramBeepSound;
    public AudioClip plugSound;
    public AudioClip unplugSound;
    public AudioClip wrongSound;
    public AudioClip successSound;

    [Header("UI (optional)")]
    [Tooltip("Shows 'Problems fixed: 1/3'. Leave empty for a harder no-hint mode.")]
    public TMP_Text progressText;
    [Tooltip("Shows the time taken")]
    public TMP_Text timerText;
    [Tooltip("Shown for a moment when a part is grabbed while the PSU is ON")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("Events")]
    [Tooltip("Fires once when the PC boots to the desktop with all 3 problems fixed (show explanation / rewards here)")]
    public UnityEvent onAllFixed;
    [Tooltip("Optional: call StartChallenge() from the NPC dialogue instead of starting the timer right away")]
    public bool startTimerOnStart = false;

    [Header("State (read-only)")]
    public bool pcOn = false;
    public bool pinIn = false;
    public bool ramIn = false;
    public int cablePort = 1; // 0 = none, 1 = motherboard, 2 = GPU
    public bool finished = false;
    public float timeTaken = 0f;

    private XRBaseInteractable button;
    private bool leftWasDown, rightWasDown;
    private float lastPressTime = -1f;
    private bool timerRunning = false;
    private bool snappingNow = false;
    private bool pinArmed = true, ramArmed = true, cableArmed = true;
    private bool cableMoved = false;

    private bool SwitchOn => psuSwitch == null || !psuSwitch.IsOff;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        button = GetComponent<XRBaseInteractable>();
        if (button == null) button = GetComponentInParent<XRBaseInteractable>();
        if (button != null) button.selectEntered.AddListener(_ => PressPower());

        if (psuSwitch != null) psuSwitch.onTurnedOff.AddListener(OnSwitchOff);

        Setup(connector24Pin);
        Setup(ramStick);
        Setup(monitorCable);
    }

    private void Setup(XRGrabInteractable g)
    {
        if (g == null) return;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true; rb.useGravity = false; // stays where you placed it until grabbed
        }
        g.throwOnDetach = false;
        g.selectEntered.AddListener(_ => OnGrabbed(g));
        g.selectExited.AddListener(_ => OnReleased(g));
    }

    private void Start()
    {
        pcOn = false; pinIn = false; ramIn = false; finished = false;
        cablePort = 1; cableMoved = false; timeTaken = 0f;
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
        Place(monitorCable, motherboardPortSpot);
        if (startTimerOnStart) StartChallenge();
        UpdatePC();
    }

    // Call from the NPC dialogue's On Dialogue Finished (optional)
    public void StartChallenge() { timerRunning = true; }

    private void Update()
    {
        CheckTriggerClick();

        CheckSnap(connector24Pin, socket24PinSpot, ref pinArmed, () => pinIn = true, () => pinIn);
        CheckSnap(ramStick, ramSlotSpot, ref ramArmed, () => ramIn = true, () => ramIn);
        CheckCableSnap();

        if (pcOn && fans != null)
            foreach (var f in fans) if (f != null) f.Rotate(fanAxis, fanSpeed * Time.deltaTime, Space.Self);

        if (timerRunning && !finished)
        {
            timeTaken += Time.deltaTime;
            if (timerText != null)
                timerText.text = $"{(int)(timeTaken / 60)}:{(int)(timeTaken % 60):00}";
        }
    }

    private void LateUpdate()
    {
        if (pinIn) Hold(connector24Pin, socket24PinSpot);
        if (ramIn) Hold(ramStick, ramSlotSpot);
        if (cablePort == 1) Hold(monitorCable, motherboardPortSpot);
        if (cablePort == 2) Hold(monitorCable, gpuPortSpot);
    }

    private void Hold(XRGrabInteractable g, Transform spot)
    {
        if (g != null && !g.isSelected) Place(g, spot);
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
        timerRunning = true;

        // No power reaches the board: switch off OR 24-pin unplugged -> nothing happens
        if (!SwitchOn || !pinIn)
        {
            Play(deadClickSound);
            return;
        }

        pcOn = !pcOn;
        Play(pcOn ? powerOnSound : powerOffSound);
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
        bool posts = pcOn && ramIn;               // RAM OK -> no beeps, PC boots
        bool picture = posts && cablePort == 2;   // cable in GPU -> desktop

        if (offScreen != null) offScreen.SetActive(!pcOn);
        if (noSignalScreen != null) noSignalScreen.SetActive(pcOn && !picture);
        if (desktopScreen != null) desktopScreen.SetActive(picture);
        if (runningLights != null) foreach (var l in runningLights) if (l != null) l.SetActive(pcOn);

        // RAM beeps loop while on with loose RAM
        if (audioSource != null)
        {
            bool beep = pcOn && !ramIn && ramBeepSound != null;
            if (beep && !(audioSource.isPlaying && audioSource.clip == ramBeepSound))
            {
                audioSource.clip = ramBeepSound; audioSource.loop = true; audioSource.Play();
            }
            else if (!beep && audioSource.clip == ramBeepSound && audioSource.isPlaying)
            {
                audioSource.Stop(); audioSource.loop = false;
            }
        }

        UpdateProgress();

        if (picture && pinIn && !finished)
        {
            finished = true;
            timerRunning = false;
            Play(successSound);
            onAllFixed?.Invoke();
        }
    }

    private void UpdateProgress()
    {
        if (progressText == null) return;
        int n = (pinIn ? 1 : 0) + (ramIn ? 1 : 0) + (cablePort == 2 ? 1 : 0);
        progressText.text = LanguageManager.Pick("Problems fixed: ", "Naayos na problema: ") + n + "/3";
    }

    // ---------- Grabbing parts ----------
    private void OnGrabbed(XRGrabInteractable g)
    {
        timerRunning = true;

        // Inside parts need the PSU switched OFF first
        if (g != monitorCable && SwitchOn)
        {
            Play(wrongSound);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            if (powerOnWarning != null) StartCoroutine(ShowMessage(powerOnWarning));
            StartCoroutine(ForceDrop(g));
            return;
        }

        if (g == connector24Pin && pinIn) { pinIn = false; pinArmed = false; Play(unplugSound); }
        else if (g == ramStick && ramIn) { ramIn = false; ramArmed = false; Play(unplugSound); }
        else if (g == monitorCable && cablePort != 0) { cablePort = 0; cableArmed = false; cableMoved = true; Play(unplugSound); }

        UpdatePC();
    }

    private IEnumerator ForceDrop(XRGrabInteractable g)
    {
        g.enabled = false;
        yield return null;
        g.enabled = true;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
    }

    private void OnReleased(XRGrabInteractable g)
    {
        if (snappingNow) return;
        StartCoroutine(DropNextFrame(g));
    }

    private IEnumerator DropNextFrame(XRGrabInteractable g)
    {
        yield return null;
        if (g == connector24Pin && pinIn) yield break;
        if (g == ramStick && ramIn) yield break;
        if (g == monitorCable && cablePort != 0) yield break;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
    }

    // ---------- Snapping ----------
    private void CheckSnap(XRGrabInteractable g, Transform spot, ref bool armed, System.Action setIn, System.Func<bool> isIn)
    {
        if (g == null || spot == null || !g.isSelected || isIn()) return;
        if (!IsNear(g, spot)) { armed = true; return; }
        if (!armed) return;
        armed = false;

        Release(g);
        setIn();
        Place(g, spot);
        Play(plugSound);
        UpdatePC();
    }

    private void CheckCableSnap()
    {
        if (monitorCable == null || !monitorCable.isSelected || cablePort != 0) return;

        int port = IsNear(monitorCable, gpuPortSpot) ? 2 : IsNear(monitorCable, motherboardPortSpot) ? 1 : 0;
        if (port == 0) { cableArmed = true; return; }
        if (!cableArmed) return;
        cableArmed = false;

        Release(monitorCable);
        cablePort = port;
        Place(monitorCable, port == 2 ? gpuPortSpot : motherboardPortSpot);
        Play(plugSound);

        if (port == 1 && cableMoved)
        {
            Play(wrongSound);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
        }
        UpdatePC();
    }

    private void Release(XRGrabInteractable g)
    {
        snappingNow = true;
        var hand = g.firstInteractorSelecting;
        var mgr = g.interactionManager;
        if (hand != null && mgr != null) mgr.SelectExit(hand, g);
        snappingNow = false;
    }

    private bool IsNear(XRGrabInteractable g, Transform spot)
    {
        if (spot == null) return false;
        Vector3 p = spot.position;
        if (Vector3.Distance(g.transform.position, p) <= snapDistance) return true;
        float d2 = snapDistance * snapDistance;
        foreach (var c in g.GetComponentsInChildren<Collider>())
            if (c.enabled && c.bounds.SqrDistance(p) <= d2) return true;
        return false;
    }

    private void Place(XRGrabInteractable g, Transform spot)
    {
        if (g == null || spot == null) return;
        var rb = g.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true; rb.useGravity = false;
        }
        g.transform.SetPositionAndRotation(spot.position, spot.rotation);
    }

    // ---------- Helpers ----------
    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[FinalChallengeManager] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private IEnumerator ShowMessage(GameObject m)
    {
        m.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        m.SetActive(false);
    }
}