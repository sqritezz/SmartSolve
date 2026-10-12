using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using System.Text;

// Attach this to the checklist panel (e.g. ChecklistEasy).
// Objectives are organized into GROUPS. Each group can contain one or
// more objective lines, all shown together. A group only advances to
// the next group once EVERY objective inside it has been completed.
public class SequentialChecklist : MonoBehaviour
{
    [System.Serializable]
    public class Group
    {
        [TextArea(1, 2)]
        public string[] objectives; // one or more lines shown together for this group
        [System.NonSerialized] public bool[] completed;
    }

    [Header("Display")]
    public TextMeshProUGUI stepText;

    [Header("Visibility (optional)")]
    [Tooltip("If set, use SetPanelVisible() to show/hide this instead of disabling the whole GameObject. Keeping this GameObject active (even while the panel is hidden) prevents null-reference errors from other scripts that query this checklist.")]
    public GameObject visualPanel;

    [Header("Groups (in order)")]
    public Group[] groups;

    [Header("Appearance")]
    public string checkmarkSymbol = "\u2714"; // check mark
    public string normalColorHex = "#FFFFFF";
    public string completedColorHex = "#00FF00";
    public float delayBeforeNextGroup = 1.2f;

    [Header("Events")]
    [Tooltip("Fires once, after the LAST group's objectives are all complete")]
    public UnityEvent onAllStepsComplete;

    private int currentGroupIndex = 0;
    private bool isTransitioning = false;
    private bool allComplete = false;

    private void Awake()
    {
        // Make sure every group has its completed[] array ready immediately,
        // even before Start() runs, so IsCurrentStep/CompleteObjective never
        // hit a null reference no matter what order/state things load in.
        EnsureAllGroupsInitialized();
    }

    private void Start()
    {
        ShowGroup(0);
    }

    private void EnsureAllGroupsInitialized()
    {
        if (groups == null) return;
        foreach (Group g in groups)
        {
            if (g.completed == null || g.completed.Length != g.objectives.Length)
                g.completed = new bool[g.objectives.Length];
        }
    }

    // Call this instead of gameObject.SetActive(false) when you just want to
    // hide the checklist UI (e.g. because the tutorial checklist is showing
    // instead). Keeping this component's GameObject active means its logic
    // keeps tracking progress correctly in the background.
    public void SetPanelVisible(bool visible)
    {
        if (visualPanel != null)
            visualPanel.SetActive(visible);
        else if (stepText != null)
            stepText.gameObject.SetActive(visible);
    }

    private void ShowGroup(int index)
    {
        if (groups == null || index < 0 || index >= groups.Length) return;

        currentGroupIndex = index;
        Group g = groups[index];
        if (g.completed == null || g.completed.Length != g.objectives.Length)
            g.completed = new bool[g.objectives.Length];

        RenderText();
    }

    private void RenderText()
    {
        if (stepText == null) return;
        if (groups == null || currentGroupIndex >= groups.Length) return;

        Group g = groups[currentGroupIndex];
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < g.objectives.Length; i++)
        {
            bool done = g.completed[i];
            string prefix = done ? checkmarkSymbol : "-";
            string color = done ? completedColorHex : normalColorHex;

            sb.Append("<color=").Append(color).Append(">")
              .Append(prefix).Append(" ").Append(g.objectives[i])
              .Append("</color>");

            if (i < g.objectives.Length - 1)
                sb.Append("\n");
        }

        stepText.text = sb.ToString();
    }

    // Returns true only if this exact group+objective is the one still
    // pending right now (so scripts can safely call CompleteObjective
    // without worrying about firing at the wrong time).
    public bool IsCurrentStep(int groupIndex, int objectiveIndex)
    {
        if (groups == null || groupIndex < 0 || groupIndex >= groups.Length) return false;
        if (allComplete || isTransitioning) return false;
        if (groupIndex != currentGroupIndex) return false;

        Group g = groups[groupIndex];
        if (g.completed == null || g.completed.Length != g.objectives.Length)
            g.completed = new bool[g.objectives.Length];

        if (objectiveIndex < 0 || objectiveIndex >= g.completed.Length) return false;

        return !g.completed[objectiveIndex];
    }

    // Call this from whatever script handles that specific objective's
    // action (e.g. PowerButton, RamSnap) once it succeeds.
    public void CompleteObjective(int groupIndex, int objectiveIndex)
    {
        if (!IsCurrentStep(groupIndex, objectiveIndex)) return;

        groups[groupIndex].completed[objectiveIndex] = true;
        RenderText();

        if (System.Array.TrueForAll(groups[groupIndex].completed, c => c))
        {
            StartCoroutine(AdvanceAfterDelay());
        }
    }

    private IEnumerator AdvanceAfterDelay()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(delayBeforeNextGroup);

        int next = currentGroupIndex + 1;
        if (next < groups.Length)
        {
            ShowGroup(next);
            isTransitioning = false;
        }
        else
        {
            allComplete = true;
            isTransitioning = false;
            onAllStepsComplete?.Invoke();
        }
    }

    public void ResetChecklist()
    {
        StopAllCoroutines();
        isTransitioning = false;
        allComplete = false;
        ShowGroup(0);
    }
}