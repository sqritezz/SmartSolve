using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    public GameObject easyScenario;
    public GameObject mediumScenario;
    public GameObject hardScenario;

    public void Back()
    {
        SceneManager.LoadScene(1);
    }

    public void Easy()
    {
        easyScenario.SetActive(true);
        mediumScenario.SetActive(false);
        hardScenario.SetActive(false);
    }
    public void Medium()
    {
        easyScenario.SetActive(false);
        mediumScenario.SetActive(true);
        hardScenario.SetActive(false);
    }
    public void Hard()
    {
        easyScenario.SetActive(false);
        mediumScenario.SetActive(false);
        hardScenario.SetActive(true);
    }
}
