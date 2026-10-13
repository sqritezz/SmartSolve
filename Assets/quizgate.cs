using UnityEngine;
using UnityEngine.UI;

// Quiz before the next level.
// Reward panel "Quiz" button -> reward panel hides, the quiz panel appears.
// When the quiz is finished -> "Next Level" button appears in the quiz panel.
// "Next Level" -> does exactly what the old "Next" button did (teleport).
//
// Which quiz? It goes in order by itself: after a quiz is finished, the next
// Quiz button press opens the next one (Easy -> Medium -> Hard...).
// Optional: Checklist > On All Steps Complete () -> QuizGate.SetCurrentQuiz (number)
// to force a specific quiz (e.g. when a level is picked from the menu).
//
// Put this on an object that is ALWAYS active (e.g. Reward Manager),
// NOT on the RewardPanel itself (it gets hidden).
public class QuizGate : MonoBehaviour
{
    [System.Serializable]
    public class LevelQuiz
    {
        public string name = "RAM Easy";
        [Tooltip("This level's quiz panel (e.g. RAMeasyQuiz)")]
        public GameObject quizPanel;
        [Tooltip("The 'Next Level' button inside this quiz panel (hidden until the quiz is done)")]
        public GameObject nextLevelButton;
        [Tooltip("Optional: an object your quiz turns ON when it ends (e.g. the score/result text). " +
                 "When it appears, Next Level shows by itself.")]
        public GameObject quizFinishedObject;
    }

    [Header("Reward Panel")]
    public GameObject rewardPanel;
    [Tooltip("The old Next button on the reward panel. It stays hidden; Next Level uses its On Click ().")]
    public Button oldNextButton;
    [Tooltip("The new Quiz button on the reward panel")]
    public GameObject quizButton;

    [Header("Quizzes (in level order)")]
    public LevelQuiz[] quizzes;

    [Header("State (read-only)")]
    public int currentQuizIndex = 0;

    private LevelQuiz current;
    private bool finished;

    private void Start()
    {
        if (oldNextButton != null) oldNextButton.gameObject.SetActive(false);
        if (quizButton != null) quizButton.SetActive(true);

        foreach (var q in quizzes)
        {
            if (q.quizPanel != null) q.quizPanel.SetActive(false);
            if (q.nextLevelButton != null) q.nextLevelButton.SetActive(false);
        }
    }

    private void Update()
    {
        // Auto-detect the end of the quiz
        if (current == null || finished) return;
        if (current.quizFinishedObject != null && current.quizFinishedObject.activeInHierarchy)
            QuizFinished();
    }

    // Each level's checklist -> On All Steps Complete () -> SetCurrentQuiz(its number)
    public void SetCurrentQuiz(int index)
    {
        currentQuizIndex = index;
        forced = true;
    }

    private bool forced = false;

    // A quiz counts as done when its Result is turned on
    private static bool IsDone(LevelQuiz q)
    {
        return q.quizFinishedObject != null && q.quizFinishedObject.activeSelf;
    }

    // First quiz in the list that hasn't been finished yet
    private int FirstUnfinished()
    {
        for (int i = 0; i < quizzes.Length; i++)
            if (quizzes[i] != null && !IsDone(quizzes[i])) return i;
        return -1;
    }

    // Quiz button -> On Click ()
    public void OpenQuiz()
    {
        if (quizzes == null) quizzes = new LevelQuiz[0];

        // Use the forced number if a checklist set one, otherwise the first unfinished quiz
        int index = forced ? currentQuizIndex : FirstUnfinished();
        forced = false;
        currentQuizIndex = index;

        current = (index >= 0 && index < quizzes.Length) ? quizzes[index] : null;

        // Make sure no other quiz panel is still showing
        foreach (var q in quizzes)
            if (q != null && q != current && q.quizPanel != null) q.quizPanel.SetActive(false);

        if (current == null || current.quizPanel == null)
        {
            Debug.LogWarning("[QuizGate] No quiz set for this level - going to the next level instead.");
            GoToNextLevel();
            return;
        }

        finished = false;
        if (rewardPanel != null) rewardPanel.SetActive(false);
        if (current.nextLevelButton != null) current.nextLevelButton.SetActive(false);
        current.quizPanel.SetActive(true);
    }

    // Call this from your quiz script when the last question is answered
    // (or let "Quiz Finished Object" do it automatically)
    public void QuizFinished()
    {
        if (current == null || finished) return;
        finished = true;
        if (current.nextLevelButton != null) current.nextLevelButton.SetActive(true);

        // Next time the Quiz button is pressed, open the NEXT level's quiz
        int i = System.Array.IndexOf(quizzes, current);
        if (i >= 0) currentQuizIndex = i + 1;
    }

    // Next Level button -> On Click ()
    public void GoToNextLevel()
    {
        if (current != null && current.quizPanel != null) current.quizPanel.SetActive(false);
        current = null;
        finished = false;

        // Do whatever the old Next button did (teleport to the next level)
        if (oldNextButton != null) oldNextButton.onClick.Invoke();
    }
}