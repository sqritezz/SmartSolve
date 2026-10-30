using UnityEngine;
using TMPro;

// Put this on the level card's Time text or Quiz text (level select menu).
// Shows the player's BEST time or BEST quiz score for that level.
// The saving/loading code is in LevelResults.cs - do NOT copy it into this file.
[RequireComponent(typeof(TMP_Text))]
public class LevelResultText : MonoBehaviour
{
    public enum Show { Time, Quiz }

    [Tooltip("Same name used on the level's Performance Tracker / Quiz Result Saver, e.g. RAM Easy")]
    public string levelName = "RAM Easy";
    public Show show = Show.Time;

    [Header("Text ({0} = the result)")]
    public string format = "Time: {0}";
    public string formatFilipino = "Oras: {0}";
    [Tooltip("Shown when the level hasn't been finished yet")]
    public string notYet = "--";

    private TMP_Text text;

    private void Awake() { text = GetComponent<TMP_Text>(); }

    private void OnEnable()
    {
        LevelResults.OnChanged += Refresh;
        LanguageManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LevelResults.OnChanged -= Refresh;
        LanguageManager.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        string value = notYet;

        if (show == Show.Time)
        {
            if (LevelResults.TryGetTime(levelName, out float s)) value = LevelResults.FormatTime(s);
        }
        else
        {
            if (LevelResults.TryGetQuiz(levelName, out string q)) value = q;
        }

        string f = LanguageManager.Pick(format, formatFilipino);
        if (string.IsNullOrEmpty(f) || !f.Contains("{0}")) f = (f ?? "") + "{0}";  // empty Format = just the result
        text.text = string.Format(f, value);
    }
}