using UnityEngine;
using UnityEngine.UI;

// Quiz before the next level.
// Reward panel "Quiz" button -> reward panel hides, the quiz panel appears.
// When the quiz is finished -> "Next Level" button appears in the quiz panel.
// "Next Level" -> does exactly what the old "Next" button did (teleport).
//
// Which quiz? Each level's checklist tells it when the level is done:
//   Checklist > On All Steps Complete () -> QuizGate.SetCurrentQuiz (0 = first quiz, 1 = second...)
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
    }

    // Quiz button -> On Click ()
    public void OpenQuiz()
    {
        current = (quizzes != null && currentQuizIndex >= 0 && currentQuizIndex < quizzes.Length)
                  ? quizzes[currentQuizIndex] : null;

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
        if (current == null) return;
        finished = true;
        if (current.nextLevelButton != null) current.nextLevelButton.SetActive(true);
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