using UnityEngine;
using TMPro;
using System.Collections;

public class HardPCManager : MonoBehaviour
{
    [Header("Monitor")]
    public GameObject screenOff;
    public GameObject screenOn;

    [Header("Audio")]
    public AudioSource beepSound;

    [Header("NPC Dialogue")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float shutdownDelay = 3f;

    private bool pcFixed = false;
    private bool brokenRamInserted = true;
    private bool brokenRamCleaned = false;
    private bool workingRamInserted = false;
    private bool isBooting = false;

    void Start()
    {
        ShowScreenOff();
        HideDialogue();
    }

    public void PressPowerButton()
    {
        if (isBooting) return;

        StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        isBooting = true;

        ShowScreenOn();

        if (pcFixed)
        {
            StopBeep();
            ShowDialogue("The computer is now working properly.");
            isBooting = false;
            yield break;
        }

        PlayBeep();

        yield return new WaitForSeconds(shutdownDelay);

        ShowScreenOff();

        if (brokenRamInserted && !brokenRamCleaned)
        {
            ShowDialogue("The PC still has RAM trouble. Try removing and reseating the RAM.");
        }
        else if (brokenRamInserted && brokenRamCleaned)
        {
            ShowDialogue("Cleaning did not fix the issue. The RAM may be damaged. Replace it with a new one.");
        }
        else if (!brokenRamInserted && !workingRamInserted)
        {
            ShowDialogue("The RAM is missing. Insert a working RAM.");
        }

        isBooting = false;
    }

    public void BrokenRamRemoved()
    {
        brokenRamInserted = false;
        ShowDialogue("You removed the RAM. Try reseating it first.");
    }

    public void BrokenRamInserted()
    {
        brokenRamInserted = true;

        if (!brokenRamCleaned)
        {
            ShowDialogue("RAM reseated. Try turning on the PC again.");
        }
        else
        {
            ShowDialogue("Clean RAM inserted, but it may still be defective. Try powering on.");
        }
    }

    public void BrokenRamCleaned()
    {
        brokenRamCleaned = true;
        ShowDialogue("RAM contacts cleaned. Insert it back and test the PC.");
    }

    public void WorkingRamInserted()
    {
        workingRamInserted = true;
        pcFixed = true;
        StopBeep();
        ShowScreenOn();
        ShowDialogue("New RAM installed. The PC is fixed!");
    }

    void ShowScreenOn()
    {
        screenOn.SetActive(true);
        screenOff.SetActive(false);
    }

    void ShowScreenOff()
    {
        screenOn.SetActive(false);
        screenOff.SetActive(true);
    }

    void PlayBeep()
    {
        if (beepSound != null && !beepSound.isPlaying)
            beepSound.Play();
    }

    void StopBeep()
    {
        if (beepSound != null && beepSound.isPlaying)
            beepSound.Stop();
    }

    void ShowDialogue(string message)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = message;
    }

    void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}