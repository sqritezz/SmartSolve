using UnityEngine;
using UnityEngine.Events;
using TMPro;

// Attach this to each Checklist panel (ChecklistEasy, ChecklistMed, ChecklistHard).
// Drag each objective's TMP text (Text (TMP) (1), Text (TMP) (2), etc. -- NOT the
// title text) into the "objectiveTexts" list, in the same order they appear on screen.
public class ChecklistManager : MonoBehaviour
{
    [System.Serializable]
    public class Objective
    {
        public TextMeshProUGUI text;
        [HideInInspector] public bool isComplete = false;
    }

    [Header("Objectives (in display order)")]
    public Objective[] objectives;

    [Header("Appearance")]
    public string checkmarkSymbol = "\u2714"; // ✔
    public Color completedColor = Color.green;

    [Header("Events")]
    [Tooltip("Fires once, the moment the LAST objective on this checklist is completed.")]
    public UnityEvent onAllObjectivesComplete;

    private bool alreadyFiredComplete = false;

    // Call this from other scripts when an objective is finished.
    // index = position in the objectives array (0 = first objective, 1 = second, etc.)
    public void CompleteObjective(int index)
    {
        if (index < 0 || index >= objectives.Length) return;

        Objective obj = objectives[index];
        if (obj.isComplete || obj.text == null) return;

        obj.isComplete = true;

        string original = obj.text.text.TrimStart();

        // Replace a leading "-" (with optional space) with the checkmark
        if (original.StartsWith("-"))
        {
            original = original.Substring(1).TrimStart();
        }

        obj.text.text = checkmarkSymbol + " " + original;
        obj.text.color = completedColor;

        // Fire the "all done" event exactly once
        if (!alreadyFiredComplete && AllComplete())
        {
            alreadyFiredComplete = true;
            onAllObjectivesComplete?.Invoke();
        }
    }

    // Optional: reset all objectives back to incomplete (e.g. when restarting a level)
    public void ResetChecklist()
    {
        alreadyFiredComplete = false;

        foreach (var obj in objectives)
        {
            if (obj.text == null) continue;

            obj.isComplete = false;
            string current = obj.text.text.TrimStart();

            if (current.StartsWith(checkmarkSymbol))
            {
                current = current.Substring(checkmarkSymbol.Length).TrimStart();
            }

            obj.text.text = "- " + current;
            obj.text.color = Color.white;
        }
    }

    // Optional helper: check if every objective on this checklist is done
    public bool AllComplete()
    {
        foreach (var obj in objectives)
        {
            if (!obj.isComplete) return false;
        }
        return true;
    }
}