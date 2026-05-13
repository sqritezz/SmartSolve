using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayQuit : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene(2);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public GameObject eulaPanel;

    void Awake()
    {
        if (PlayerPrefs.GetInt("EULA_Accepted", 0) == 1)
        {
            eulaPanel.SetActive(false);
        }
        else
        {
            eulaPanel.SetActive(true);
        }
    }

    public void AcceptEULA()
    {
        PlayerPrefs.SetInt("EULA_Accepted", 1);
        PlayerPrefs.Save();
        eulaPanel.SetActive(false);
    }

    public void DeclineEULA()
    {
        Application.Quit();
    }
}
