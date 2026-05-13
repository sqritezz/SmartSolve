using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClick : MonoBehaviour
{
    public void BackButton()
    {
        SceneManager.LoadScene(1);
    }
}
