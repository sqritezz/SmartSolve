using UnityEngine;

public class PCManager : MonoBehaviour
{
    public bool ramCleaned = false;
    public bool ramInserted = false;
    public bool isFixed = false;

    public GameObject monitorScreen;
    public AudioSource beepSound;

    public void PressPower()
    {
        if (!isFixed)
        {
            Debug.Log("ERROR: RAM issue");

            monitorScreen.SetActive(false);

            if (!beepSound.isPlaying)
                beepSound.Play();
        }
        else
        {
            Debug.Log("PC WORKING");

            monitorScreen.SetActive(true);
            beepSound.Stop();
        }
    }

    public void CheckFix()
    {
        if (ramCleaned && ramInserted)
        {
            isFixed = true;
            Debug.Log("FIXED!");
        }
        else
        {
            isFixed = false;
        }
    }
}