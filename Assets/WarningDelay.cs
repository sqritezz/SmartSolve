using UnityEngine;
using UnityEngine.SceneManagement;

public class WarningDelay : MonoBehaviour
{
    void Start()
    {
        Invoke("NextScene", 1f);
    }

    void NextScene()
    {
        SceneManager.LoadScene(1);
    }
}
