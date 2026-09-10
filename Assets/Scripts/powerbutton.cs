using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PowerButton : MonoBehaviour
{
    public GameObject screenOn;
    public GameObject screenOff;
    public float turnOffDelay = 3f;
    public bool isRamFixed = false;

    [Header("Checklist Integration")]
    public SequentialChecklist checklist;

    [Tooltip("Group/objective that completes only when the fix succeeds (isRamFixed is true)")]
    public int successGroupIndex = 1;
    public int successObjectiveIndex = 1;

    [Tooltip("Optional: group/objective that completes on ANY press, used for 'press to see what happens' steps (e.g. observing a short circuit). Set observeGroupIndex to -1 to disable.")]
    public int observeGroupIndex = -1;
    public int observeObjectiveIndex = 0;

    private XRBaseInteractable interactable;
    private Coroutine bootRoutine;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPowerPressed);
    }

    void Start()
    {
        screenOn.SetActive(false);
        screenOff.SetActive(true);
    }

    void OnPowerPressed(SelectEnterEventArgs args)
    {
        PressPowerButton();
    }

    public void PressPowerButton()
    {
        if (bootRoutine != null)
            StopCoroutine(bootRoutine);
        bootRoutine = StartCoroutine(BootMonitor());
    }

    IEnumerator BootMonitor()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);

        // "Press to see what happens" objective -- completes on any press, regardless of outcome
        if (checklist != null && observeGroupIndex >= 0 &&
            checklist.IsCurrentStep(observeGroupIndex, observeObjectiveIndex))
        {
            checklist.CompleteObjective(observeGroupIndex, observeObjectiveIndex);
        }

        if (!isRamFixed)
        {
            yield return new WaitForSeconds(turnOffDelay);
            screenOn.SetActive(false);
            screenOff.SetActive(true);
        }
        else
        {
            // Success case: fix was applied, screen stays on -- confirm objective complete
            if (checklist != null && checklist.IsCurrentStep(successGroupIndex, successObjectiveIndex))
            {
                checklist.CompleteObjective(successGroupIndex, successObjectiveIndex);
            }
        }
    }
}