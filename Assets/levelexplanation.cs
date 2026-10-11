using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ONE per level (RAM Easy, RAM Medium, RAM Hard, PSU Easy...).
// Only holds that level's TEXT. When a checklist step (group) is finished,
// it sends the matching explanation to the shared ExplanationPanel.
// No changes to other scripts needed - it watches the checklist by itself.
public class LevelExplanations : MonoBehaviour
{
    [System.Serializable]
    public class Explanation
    {
        [Tooltip("Shows after this checklist GROUP is finished")]
        public int afterGroupIndex;
        [Tooltip("Seconds to wait before showing (e.g. let the PC shut down first)")]
        public float delay = 0f;
        public string title = "What happened?";
        [TextArea(2, 5)] public string why;
        [TextArea(2, 5)] public string howToFix;
    }

    [Header("This level's checklist")]
    public SequentialChecklist checklist;

    [Header("Explanations (press + for each)")]
    public List<Explanation> explanations = new List<Explanation>();

    private const int MaxGroups = 40;
    private const int MaxObjectives = 10;

    private int lastGroup = -1;
    private float nextCheck;
    private readonly HashSet<int> alreadyShown = new HashSet<int>();

    private void Update()
    {
        if (checklist == null || Time.time < nextCheck) return;
        nextCheck = Time.time + 0.15f;

        int current = GetCurrentGroup();
        if (current == lastGroup) return;

        // A group is finished when it stops being the current step
        if (lastGroup >= 0)
            OnGroupFinished(lastGroup);

        lastGroup = current;
    }

    private void OnGroupFinished(int group)
    {
        if (alreadyShown.Contains(group)) return;

        foreach (var e in explanations)
        {
            if (e != null && e.afterGroupIndex == group)
            {
                alreadyShown.Add(group);
                StartCoroutine(SendAfterDelay(e));
                return;
            }
        }
    }

    private IEnumerator SendAfterDelay(Explanation e)
    {
        if (e.delay > 0f) yield return new WaitForSeconds(e.delay);

        if (ExplanationPanel.Instance != null)
            ExplanationPanel.Instance.Show(e.title, e.why, e.howToFix);
        else
            Debug.LogWarning("[LevelExplanations] No ExplanationPanel in the scene.");
    }

    private int GetCurrentGroup()
    {
        for (int g = 0; g < MaxGroups; g++)
            for (int o = 0; o < MaxObjectives; o++)
                if (checklist.IsCurrentStep(g, o)) return g;
        return -1;
    }
}