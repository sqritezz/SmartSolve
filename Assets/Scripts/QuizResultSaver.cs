using UnityEngine;
using TMPro;

// Put this on the quiz's RESULT text (the text that shows e.g. "Score: 4/5" when the quiz ends).
// When that text appears, the score is saved for this level, so the level menu can show it.
public class QuizResultSaver : MonoBehaviour
{
    [Tooltip("Same name as on the level card, e.g. RAM Easy")]
    public string levelName = "RAM Easy";
    [Tooltip("The result text. Empty = the text on this object")]
    public TMP_Text resultText;

    private string lastSaved;

    private void OnEnable()
    {
        if (resultText == null) resultText = GetComponent<TMP_Text>();
        lastSaved = null;
    }

    // Checks for a few frames, because the quiz may write the score just after showing it
    private void Update()
    {
        if (resultText == null) return;
        string t = resultText.text;
        if (string.IsNullOrEmpty(t) || t == lastSaved) return;
        lastSaved = t;
        LevelResults.SaveQuizFromText(levelName, t);
    }
}
