using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Question Panels")]
    public GameObject[] questionPanels;

    [Header("Correct Buttons")]
    public Button[] correctButtons; // drag 1 correct button per question

    [Header("Result")]
    public GameObject resultPanel;
    public TextMeshProUGUI scoreText;

    private int currentQuestion = 0;
    private int score = 0;

    void Start()
    {
        score = 0;
        currentQuestion = 0;

        resultPanel.SetActive(false);

        foreach (GameObject panel in questionPanels)
            panel.SetActive(false);

        ShowQuestion(0);
    }

    void ShowQuestion(int index)
    {
        questionPanels[index].SetActive(true);

        Button[] buttons = questionPanels[index].GetComponentsInChildren<Button>(true);

        foreach (Button btn in buttons)
        {
            Button clickedButton = btn;

            clickedButton.onClick.RemoveAllListeners();
            clickedButton.onClick.AddListener(() => Answer(clickedButton));
        }
    }

    void Answer(Button clickedButton)
    {
        if (clickedButton == correctButtons[currentQuestion])
        {
            score++;
            Debug.Log("Correct!");
        }
        else
        {
            Debug.Log("Wrong!");
        }

        questionPanels[currentQuestion].SetActive(false);
        currentQuestion++;

        if (currentQuestion < questionPanels.Length)
            ShowQuestion(currentQuestion);
        else
            ShowResult();
    }

    void ShowResult()
    {
        resultPanel.SetActive(true);
        scoreText.text = score + "/" + questionPanels.Length;
    }
}