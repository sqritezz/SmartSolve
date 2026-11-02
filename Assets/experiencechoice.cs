using UnityEngine;
using UnityEngine.Events;

// Put this on the "Are you knowledgeable in computer hardware?" panel.
//   YES button  On Click -> ExperienceChoice.ChooseExperienced
//   NO button   On Click -> ExperienceChoice.ChooseBeginner
//
// YES (experienced) -> highlights only in the TUTORIALS
// NO  (beginner)    -> highlights in the tutorials AND the real scenes
//
// TutorialHighlighter reads the answer by itself (its "Show For" setting).
public class ExperienceChoice : MonoBehaviour
{
    // The player's answer, readable from any script. Default = beginner (highlights everywhere).
    public static bool IsExperienced { get; private set; } = false;
    public static bool HasChosen { get; private set; } = false;

    [Tooltip("Optional: hide this panel after a button is pressed")]
    public GameObject panelToHide;

    [Tooltip("Optional: what to do after choosing (e.g. show the main menu)")]
    public UnityEvent onChosen;

    // Set by the Restart button: skip the question ONCE after restarting
    public static bool SkipQuestionOnce = false;

    // Unity may keep these values between Play sessions in the Editor - start clean
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsExperienced = false; HasChosen = false; SkipQuestionOnce = false; OnChanged = null;
    }

    private void Start()
    {
        if (SkipQuestionOnce)
        {
            SkipQuestionOnce = false;
            if (panelToHide != null) panelToHide.SetActive(false);
            onChosen?.Invoke();
        }
    }

    // Used by the Options menu's Indicators switch (IndicatorOption)
    public static void SetIndicatorsInRealScene(bool on)
    {
        IsExperienced = !on;
        HasChosen = true;
        OnChanged?.Invoke();
    }

    public static event System.Action OnChanged;

    public void ChooseExperienced() { Choose(true); }
    public void ChooseBeginner() { Choose(false); }

    private void Choose(bool experienced)
    {
        IsExperienced = experienced;
        HasChosen = true;
        OnChanged?.Invoke();
        Debug.Log("[ExperienceChoice] Player is " + (experienced ? "EXPERIENCED (highlights only in tutorials)"
                                                                 : "a BEGINNER (highlights in tutorials and real scenes)"));
        if (panelToHide != null) panelToHide.SetActive(false);
        onChosen?.Invoke();
    }
}