using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

// Put this on any grabbable / clickable object that belongs to the objectives.
// It only works during the checklist steps listed in "Allowed Groups".
// Grabbing or clicking it during any other step is blocked and shows a warning
// ("Follow the objective"). Works for tutorials and real levels - just point
// it at the right checklist.
public class ObjectiveLock : MonoBehaviour, IXRSelectFilter
{
    [Tooltip("Leave empty to use the interactable on this object")]
    public XRBaseInteractable interactable;

    [Header("Checklist")]
    public SequentialChecklist checklist;
    [Tooltip("Group indexes (checklist steps) where this object may be used")]
    public int[] allowedGroups;
    [Tooltip("Also allow it in every step AFTER the last allowed group (e.g. a tool you keep using)")]
    public bool allowAfterLastGroup = false;

    [Header("Warning")]
    [Tooltip("Optional custom message. Empty = the default message from ObjectiveWarning.")]
    [TextArea(1, 3)] public string customMessage = "";

    public bool canProcess => isActiveAndEnabled;

    private float lastWarnTime = -999f;
    private const float WarnCooldown = 2f;
    private const int MaxGroups = 40;
    private const int MaxObjectives = 10;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null) interactable.selectFilters.Add(this);
    }

    private void OnDisable()
    {
        if (interactable != null) interactable.selectFilters.Remove(this);
    }

    // Grip grab attempts go through here
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable target)
    {
        // Already holding it -> never force-drop
        if (interactable != null && interactable.isSelected) return true;
        return CheckAndWarn();
    }

    // Returns true if allowed now. If not, shows the warning (with a cooldown).
    public bool CheckAndWarn()
    {
        if (IsAllowedNow()) return true;

        if (Time.time - lastWarnTime > WarnCooldown)
        {
            lastWarnTime = Time.time;

            if (ObjectiveWarning.Instance != null)
                ObjectiveWarning.Instance.Show(customMessage);
        }
        return false;
    }

    public bool IsAllowedNow()
    {
        if (checklist == null || allowedGroups == null || allowedGroups.Length == 0) return true;

        int current = GetCurrentGroup();
        if (current < 0) return true; // checklist finished or not running

        int last = -1;
        foreach (int g in allowedGroups)
        {
            if (g == current) return true;
            if (g > last) last = g;
        }

        return allowAfterLastGroup && current > last;
    }

    private int GetCurrentGroup()
    {
        for (int g = 0; g < MaxGroups; g++)
            for (int o = 0; o < MaxObjectives; o++)
                if (checklist.IsCurrentStep(g, o)) return g;
        return -1;
    }
}