using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

// Put this on any grabbable / clickable object that belongs to the objectives.
// It only works during the checklist steps listed in "Allowed Groups".
// Using it during any other step is blocked and shows a warning.
//
// The warning ONLY appears the moment the player PRESSES a button while
// pointing at / touching the object. Just looking at it with the ray,
// or sweeping the ray across it while holding a button, shows nothing.
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

    // Remembers whether each hovering hand/ray had its button pressed last frame
    private readonly Dictionary<IXRHoverInteractor, bool> wasPressed = new Dictionary<IXRHoverInteractor, bool>();

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
        wasPressed.Clear();
    }

    // XR checks this filter very often (even while just pointing at the object),
    // so it only blocks silently here. Never shows the warning.
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable target)
    {
        // Already holding it -> never force-drop
        if (interactable != null && interactable.isSelected) return true;
        return IsAllowedNow();
    }

    // Shows the warning only on a real NEW press while pointing at this object
    private void Update()
    {
        if (interactable == null) return;

        var hovering = interactable.interactorsHovering;

        // forget hands/rays that stopped pointing at this object
        if (wasPressed.Count > 0)
        {
            var stale = new List<IXRHoverInteractor>();
            foreach (var key in wasPressed.Keys)
                if (!hovering.Contains(key)) stale.Add(key);
            foreach (var key in stale) wasPressed.Remove(key);
        }

        foreach (var hover in hovering)
        {
            var select = hover as IXRSelectInteractor;
            if (select == null || select is XRSocketInteractor) continue;

            bool pressedNow = select.isSelectActive && !select.hasSelection;

            bool known = wasPressed.TryGetValue(hover, out bool pressedBefore);
            wasPressed[hover] = pressedNow;

            // First time we see this hand pointing here: just remember its state.
            // (Prevents a warning when sweeping the ray across while holding a button.)
            if (!known) continue;

            if (pressedNow && !pressedBefore && !IsAllowedNow())
                Warn();
        }
    }

    // Used by ClickOnly / trigger-click scripts. Returns true if allowed now,
    // otherwise shows the warning (with a cooldown) and returns false.
    public bool CheckAndWarn()
    {
        if (IsAllowedNow()) return true;
        Warn();
        return false;
    }

    private void Warn()
    {
        if (Time.time - lastWarnTime <= WarnCooldown) return;
        lastWarnTime = Time.time;

        if (ObjectiveWarning.Instance != null)
            ObjectiveWarning.Instance.Show(customMessage);
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