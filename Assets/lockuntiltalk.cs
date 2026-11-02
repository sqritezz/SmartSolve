using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Put this on the PC (e.g. "System Unit" or "NPCBelongings") of a level/tutorial.
// EVERYTHING under it (power button, PSU switch, lid, RAM, cables, ports...) can't be
// pointed at, clicked or grabbed until the checklist reaches "Unlock At Group"
// (usually 1 = the step right after "Talk to your friend").
//
// It doesn't turn anything off - it only blocks the ray/hands - so scripts like
// RequirePSUOn keep working normally.
public class LockUntilTalked : MonoBehaviour
{
    [Header("Unlock when")]
    [Tooltip("This level's checklist (tutorial checklist for a tutorial, real checklist for the real scene)")]
    public SequentialChecklist checklist;
    [Tooltip("Unlocks once the checklist reaches this group. 1 = right after 'Talk to the NPC' (group 0)")]
    public int unlockAtGroup = 1;

    [Header("State (read-only)")]
    public bool isLocked = true;

    private readonly Dictionary<XRBaseInteractable, InteractionLayerMask> saved =
        new Dictionary<XRBaseInteractable, InteractionLayerMask>();
    private float nextCheck;

    private void OnEnable()
    {
        isLocked = true;
        LockAll();
    }

    private void OnDisable()
    {
        RestoreAll();
    }

    private void Update()
    {
        if (!isLocked || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.25f;

        if (ReachedUnlockGroup()) { Unlock(); return; }

        LockAll();   // also catches parts that appear later (e.g. the new PSU / GPU)
    }

    // Can also be called from an event (e.g. NPCDialogue's end event)
    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        RestoreAll();
    }

    private bool ReachedUnlockGroup()
    {
        if (checklist == null) return false;
        for (int g = unlockAtGroup; g < unlockAtGroup + 40; g++)
            for (int o = 0; o < 10; o++)
                if (checklist.IsCurrentStep(g, o)) return true;
        return false;
    }

    private void LockAll()
    {
        foreach (var i in GetComponentsInChildren<XRBaseInteractable>(true))
        {
            if (i == null || saved.ContainsKey(i)) continue;
            saved[i] = i.interactionLayers;
            if (i.isSelected && i.interactionManager != null)
            {
                var holders = new List<IXRSelectInteractor>(i.interactorsSelecting);
                foreach (var h in holders) i.interactionManager.SelectExit(h, i);
            }
            i.interactionLayers = 0;   // nothing can hover, click or grab it
        }
    }

    private void RestoreAll()
    {
        foreach (var kv in saved)
            if (kv.Key != null) kv.Key.interactionLayers = kv.Value;
        saved.Clear();
    }
}