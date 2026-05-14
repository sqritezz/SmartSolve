using UnityEngine;
using TMPro;

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Questions")]
    public GameObject[] questions; // Question1 to Question10 only

    [Header("Final Score Text")]
    public TextMeshProUGUI scoreText; // Drag your (score) text here

    [Header("Result Panel")]
    public GameObject resultPanel;

    private int currentQuestion = 0;
    private int score = 0;

    // -1 = unanswered, 0 = wrong, 1 = correct
    private int[] answers;

    void Start()
    {
        answers = new int[questions.Length];

        for (int i = 0; i < answers.Length; i++)
            answers[i] = -1;

        // Hide result panel at start
        if (resultPanel != null)
            resultPanel.SetActive(false);

        ShowQuestion(0);
        UpdateScore();
    }

    // Called by answer buttons
    public void AnswerQuestion(bool isCorrect)
    {
        int newAnswer = isCorrect ? 1 : 0;

        // Remove previous correct score if changing answer
        if (answers[currentQuestion] == 1)
            score--;

        answers[currentQuestion] = newAnswer;

        if (isCorrect)
            score++;

        UpdateScore();

        // Auto next or show result
        if (currentQuestion < questions.Length - 1)
        {
            ShowQuestion(currentQuestion + 1);
        }
        else
        {
            ShowResult();
        }
    }

    public void NextQuestion()
    {
        if (currentQuestion < questions.Length - 1)
            ShowQuestion(currentQuestion + 1);
    }

    public void PreviousQuestion()
    {
        if (currentQuestion > 0)
            ShowQuestion(currentQuestion - 1);
    }

    void ShowQuestion(int index)
    {
        for (int i = 0; i < questions.Length; i++)
        {
            questions[i].SetActive(i == index);
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        currentQuestion = index;
    }

    void ShowResult()
    {
        for (int i = 0; i < questions.Length; i++)
        {
            questions[i].SetActive(false);
        }

        if (resultPanel != null)
            resultPanel.SetActive(true);

        UpdateScore();
    }

    void UpdateScore()
    {
        if (scoreText != null)
        {
            scoreText.text = score + "/" + questions.Length;
        }
    }

    // Optional restart button
    public void RestartQuiz()
    {
        score = 0;

        for (int i = 0; i < answers.Length; i++)
            answers[i] = -1;

        ShowQuestion(0);
        UpdateScore();
    }
}