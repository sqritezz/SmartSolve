using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// PSU EASY - Overheating. Attach to the PC POWER BUTTON
// (needs collider + XR Simple Interactable). Replaces PSUPowerButton.
//
// Symptom: while the PSU fan is dusty, the PC turns on, shows an
// overheating warning, then shuts itself off after a few seconds.
// Fix: turn the PSU switch OFF -> spray the fan with compressed air ->
// turn the switch back ON -> press the power button: the PC stays on.
//
// IMPORTANT: leave the PSU Switch's own Checklist field EMPTY.
// This script completes both switch steps (off AND on) itself.
public class PSUEasyOverheat : MonoBehaviour
{
    [Header("Screen")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Header("Overheat Shutdown")]
    [Tooltip("How long the PC runs before shutting down while the fan is dusty")]
    public float runTimeBeforeShutdown = 3f;
    [Tooltip("Shown on the monitor during the last seconds before shutdown, e.g. \"WARNING: System overheating\"")]
    public GameObject overheatMessage;
    [Tooltip("How many seconds before shutdown the warning appears")]
    public float overheatWarningTime = 1.5f;
    public AudioSource shutdownSound;

    [Header("Audio")]
    [Tooltip("Plays on every button press")]
    public AudioSource beepSound;
    [Tooltip("Optional: plays when the PC finally stays on")]
    public AudioSource successSound;

    [Header("Parts")]
    public PSUSwitch1 psuSwitch;
    public AirCleanable psuFan;

    [Header("PSU Fan Spin")]
    [Tooltip("The PSU FAN object - spins once it's clean and the PC is running")]
    public Transform fanToSpin;
    public Vector3 spinAxis = Vector3.forward;
    public float spinSpeed = 900f;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int observeGroupIndex = 1;    // Press the power button to test the PC
    public int switchOffGroupIndex = 2;  // Turn off the PSU switch
    public int cleanGroupIndex = 3;      // Clean the PSU fan with compressed air
    public int switchOnGroupIndex = 4;   // Turn the PSU switch back on
    public int successGroupIndex = 5;    // Press the power button again
    public int objectiveIndex = 0;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    [Header("State (read-only)")]
    public bool observeDone = false;
    public bool isBooting = false;
    public bool pcRunning = false;

    private XRBaseInteractable interactable;
    private Coroutine overheatRoutine;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);

        if (psuSwitch != null)
        {
            psuSwitch.onTurnedOff.AddListener(OnSwitchTurnedOff);
            psuSwitch.onTurnedOn.AddListener(OnSwitchTurnedOn);
        }

        if (psuFan != null)
            psuFan.onCleaned.AddListener(OnFanCleaned);
    }

    private void Start()
    {
        ShowOff();
        if (overheatMessage != null) overheatMessage.SetActive(false);
        UpdateFanLock();
    }

    private void Update()
    {
        bool fanRunning = pcRunning && psuFan != null && psuFan.isClean;
        if (fanRunning && fanToSpin != null)
            fanToSpin.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

    // ---------- Power button ----------
    private void OnPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        if (isBooting) return; // wait for the current overheat cycle to finish

        if (beepSound != null) beepSound.Play();

        if (pcRunning) return; // already fixed and running

        bool hasPower = psuSwitch == null || !psuSwitch.IsOff;
        if (!hasPower)
        {
            ShowOff(); // no power at all - nothing happens
            return;
        }

        if (psuFan != null && psuFan.isClean)
        {
            // FIXED: PC stays on
            pcRunning = true;
            ShowOn();
            if (successSound != null) successSound.Play();
            CompleteStep(successGroupIndex);
            return;
        }

        // Still dusty: testing again without fixing counts as a mistake
        if (observeDone && performanceTracker != null)
            performanceTracker.RegisterWrongAttempt();

        overheatRoutine = StartCoroutine(OverheatCycle());
    }

    private IEnumerator OverheatCycle()
    {
        isBooting = true;
        ShowOn();

        float quietTime = Mathf.Max(0f, runTimeBeforeShutdown - overheatWarningTime);
        yield return new WaitForSeconds(quietTime);

        if (overheatMessage != null) overheatMessage.SetActive(true);
        yield return new WaitForSeconds(overheatWarningTime);

        EndOverheatCycle(true);
    }

    private void EndOverheatCycle(bool playShutdownSound)
    {
        if (overheatMessage != null) overheatMessage.SetActive(false);
        ShowOff();
        if (playShutdownSound && shutdownSound != null) shutdownSound.Play();

        isBooting = false;
        overheatRoutine = null;

        if (!observeDone)
        {
            observeDone = true;
            CompleteStep(observeGroupIndex);
            UpdateFanLock();
        }
    }

    // ---------- PSU switch ----------
    private void OnSwitchTurnedOff()
    {
        // Cutting power mid-boot ends the cycle right away
        if (isBooting)
        {
            if (overheatRoutine != null) StopCoroutine(overheatRoutine);
            EndOverheatCycle(false);
        }

        if (pcRunning)
        {
            pcRunning = false;
            ShowOff();
        }

        CompleteStep(switchOffGroupIndex);
    }

    private void OnSwitchTurnedOn()
    {
        CompleteStep(switchOnGroupIndex);
    }

    // ---------- PSU fan ----------
    private void OnFanCleaned()
    {
        CompleteStep(cleanGroupIndex);
    }

    // Fan can only be cleaned after the player has seen the symptom
    private void UpdateFanLock()
    {
        if (psuFan != null)
            psuFan.canBeCleaned = observeDone;
    }

    // ---------- Helpers ----------
    private void CompleteStep(int groupIndex)
    {
        if (checklist != null && checklist.IsCurrentStep(groupIndex, objectiveIndex))
            checklist.CompleteObjective(groupIndex, objectiveIndex);
    }

    private void ShowOn()
    {
        if (screenOn != null) screenOn.SetActive(true);
        if (screenOff != null) screenOff.SetActive(false);
    }

    private void ShowOff()
    {
        if (screenOn != null) screenOn.SetActive(false);
        if (screenOff != null) screenOff.SetActive(true);
    }
}