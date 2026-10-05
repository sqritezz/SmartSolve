using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PSUPowerButtonHard : MonoBehaviour
{
    [Header("Visual Feedback")]
    public GameObject screenOn;
    public GameObject screenOff;

    [Header("Audio")]
    public AudioSource beepSound;

    [Header("State (set automatically by switch + PSU slot scripts)")]
    public bool isSwitchOn = true;
    public bool newPsuInstalled = false;

    [Header("Short Circuit Effect")]
    [Tooltip("Plays sparks/flicker when the player presses the button while the old PSU is still dead/installed")]
    public ShortCircuitEffect shortCircuitEffect;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;
    public int observeGroupIndex = 0;
    public int observeObjectiveIndex = 0;
    public int successGroupIndex = 3;
    public int successObjectiveIndex = 1;

    [Header("Scoring")]
    public PerformanceTracker performanceTracker;

    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);
    }

    void Start()
    {
        ShowOff();
    }

    void OnPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public bool IsPsuFixed()
    {
        return isSwitchOn && newPsuInstalled;
    }

    public void PressPowerButton()
    {
        if (beepSound != null)
            beepSound.Play();

        bool isObserveStep = checklist != null && checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex);

        if (isObserveStep)
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);

        if (IsPsuFixed())
        {
            ShowOn();

            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
        }
        else
        {
            ShowOff();

            // Old PSU is still in -- show sparks (mainly meaningful on the very first press)
            if (!newPsuInstalled && shortCircuitEffect != null)
                shortCircuitEffect.PlayEffect();

            if (!isObserveStep && performanceTracker != null)
                performanceTracker.RegisterWrongAttempt();
        }
    }

    void ShowOn()
    {
        if (screenOn != null) screenOn.SetActive(true);
        if (screenOff != null) screenOff.SetActive(false);
    }

    void ShowOff()
    {
        if (screenOn != null) screenOn.SetActive(false);
        if (screenOff != null) screenOff.SetActive(true);
    }
}