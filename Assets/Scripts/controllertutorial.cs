using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

// CONTROLLER TUTORIAL - teaches the controls before the first level.
// A panel floats in front of the player with one instruction at a time.
// The next instruction only appears after the player actually DOES it
// (walks, turns, pulls the trigger, grabs something...).
// Until it's finished, the objects in "Hide Until Done" stay hidden (e.g. the NPC),
// so the level can't be played yet.
//
// Put it on an empty object (e.g. "ControllerTutorial" under EasySpawnPoint (RAM)).
// It starts by itself when the player enters "Start In Level" (e.g. RAM Easy),
// and only runs ONCE per game.
public class ControllerTutorial : MonoBehaviour
{
    public enum Action
    {
        PressContinue,   // just reading - the player pulls the TRIGGER to go on
        Walk,            // left joystick: move a little
        Turn,            // right joystick: turn around
        PressTrigger,    // pull the trigger (either hand)
        PressGrip,       // squeeze the grip (either hand)
        PointAt,         // point the ray at "Target"
        Grab,            // grab "Target" (needs XR Grab Interactable)
        PressA, PressB, PressX, PressY
    }

    [System.Serializable]
    public class Step
    {
        public Action action = Action.PressContinue;
        [TextArea(2, 4)] public string text;
        [TextArea(2, 4)] public string textFilipino;
        [Tooltip("Only for Point At / Grab: the object to point at or grab")]
        public XRBaseInteractable target;
    }

    [Header("When to start")]
    [Tooltip("Starts when the player enters this level (same name as TeleportToPoint uses). Empty = starts right away.")]
    public string startInLevel = "RAM Easy";
    [Tooltip("Show it only the first time in this game session")]
    public bool onlyOnce = true;

    [Header("Panel")]
    [Tooltip("World-space canvas panel (keep it OFF in the Hierarchy)")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    [Tooltip("Optional: shows e.g. 'Step 2 / 8'")]
    public TMP_Text progressText;
    public string title = "Controller Tutorial";
    public string titleFilipino = "Gabay sa Controller";
    public float distance = 1.3f;
    public float heightOffset = -0.1f;
    [Tooltip("Panel slowly follows the player's view")]
    public bool followPlayer = true;

    [Header("Feedback")]
    public string doneText = "Great!";
    public string doneTextFilipino = "Magaling!";
    public float pauseBetweenSteps = 1.2f;
    public AudioSource successSound;

    [Header("Steps (in order)")]
    public Step[] steps =
    {
        new Step { action = Action.PressContinue,
            text = "Welcome to SmartSolve VR!\nLet's learn the controls first.\n\nPull the TRIGGER (index finger) to continue.",
            textFilipino = "Maligayang pagdating sa SmartSolve VR!\nAlamin muna natin ang mga kontrol.\n\nHilahin ang TRIGGER (hintuturo) para magpatuloy." },
        new Step { action = Action.Walk,
            text = "Push the LEFT joystick to walk.\nTry walking a few steps.",
            textFilipino = "Itulak ang KALIWANG joystick para maglakad.\nSubukang maglakad nang ilang hakbang." },
        new Step { action = Action.Turn,
            text = "Push the RIGHT joystick left or right to turn.",
            textFilipino = "Itulak pakaliwa o pakanan ang KANANG joystick para umikot." },
        new Step { action = Action.PressTrigger,
            text = "Point the ray and pull the TRIGGER to click buttons and talk to people.",
            textFilipino = "Itutok ang ray at hilahin ang TRIGGER para pumindot ng button at kumausap ng tao." },
        new Step { action = Action.PressGrip,
            text = "Squeeze the GRIP (middle finger) to grab objects.",
            textFilipino = "Pisilin ang GRIP (gitnang daliri) para humawak ng mga bagay." },
        new Step { action = Action.PressContinue,
            text = "Press B any time for a hint (it costs 1 star).\nPress Y to pause the game.\n\nPull the TRIGGER to continue.",
            textFilipino = "Pindutin ang B para sa hint (mababawasan ka ng 1 star).\nPindutin ang Y para i-pause ang laro.\n\nHilahin ang TRIGGER para magpatuloy." },
        new Step { action = Action.PressContinue,
            text = "You're ready! Talk to your friend to begin.\n\nPull the TRIGGER to start.",
            textFilipino = "Handa ka na! Kausapin ang iyong kaibigan para magsimula.\n\nHilahin ang TRIGGER para magsimula." },
    };

    [Header("Block the level until done")]
    [Tooltip("Hidden while the tutorial runs, shown when it's done (e.g. the NPC, the level's checklist)")]
    public GameObject[] hideUntilDone;
    public UnityEvent onTutorialComplete;

    [Header("Detection")]
    public float walkDistanceNeeded = 1f;
    public float turnAngleNeeded = 40f;

    public static bool Finished { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { Finished = false; }

    private bool running;
    private int index;
    private Transform rig, head;
    private Vector3 startPos;
    private float startYaw;
    private bool wasPressed;          // button held from the previous step must be released first
    private bool grabbedFlag;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (onlyOnce && Finished) return;
        if (hideUntilDone != null) foreach (var g in hideUntilDone) if (g != null) g.SetActive(false);
    }

    private void Update()
    {
        if (!running)
        {
            if (onlyOnce && Finished) return;
            bool levelOk = string.IsNullOrEmpty(startInLevel) ||
                           string.Equals(TeleportToPoint.CurrentLevel, startInLevel, System.StringComparison.OrdinalIgnoreCase);
            if (levelOk) Begin();
            return;
        }

        if (followPlayer) PlacePanel(false);
        if (index < 0 || index >= steps.Length) return;

        if (StepDone(steps[index])) StartCoroutine(NextStep());
    }

    private void Begin()
    {
        running = true;
        head = Camera.main != null ? Camera.main.transform : null;
        rig = head != null ? head.root : null;
        if (panel != null) panel.SetActive(true);
        PlacePanel(true);
        ShowStep(0);
    }

    private void ShowStep(int i)
    {
        index = i;
        if (index >= steps.Length) { Finish(); return; }

        Step s = steps[index];
        if (titleText != null) titleText.text = LanguageManager.Pick(title, titleFilipino);
        if (bodyText != null) bodyText.text = LanguageManager.Pick(s.text, s.textFilipino);
        if (progressText != null)
            progressText.text = LanguageManager.Pick("Step ", "Hakbang ") + (index + 1) + " / " + steps.Length;

        if (rig != null) { startPos = rig.position; startYaw = rig.eulerAngles.y; }
        wasPressed = AnyButton(CommonUsages.triggerButton) || AnyButton(CommonUsages.gripButton);
        grabbedFlag = false;
        if (s.target != null && s.action == Action.Grab)
        {
            s.target.selectEntered.RemoveListener(OnTargetGrabbed);
            s.target.selectEntered.AddListener(OnTargetGrabbed);
        }
    }

    private void OnTargetGrabbed(SelectEnterEventArgs a) { grabbedFlag = true; }

    private bool busy;
    private IEnumerator NextStep()
    {
        if (busy) yield break;
        busy = true;
        int done = index;
        index = -1;   // stop checking while showing "Great!"

        if (successSound != null) successSound.Play();
        if (bodyText != null) bodyText.text = LanguageManager.Pick(doneText, doneTextFilipino);
        yield return new WaitForSecondsRealtime(pauseBetweenSteps);

        busy = false;
        ShowStep(done + 1);
    }

    private void Finish()
    {
        running = false;
        Finished = true;
        if (panel != null) panel.SetActive(false);
        if (hideUntilDone != null) foreach (var g in hideUntilDone) if (g != null) g.SetActive(true);
        onTutorialComplete?.Invoke();
    }

    // ---------- Checks ----------
    private bool StepDone(Step s)
    {
        switch (s.action)
        {
            case Action.Walk:
                if (rig == null) return true;
                Vector3 d = rig.position - startPos; d.y = 0f;
                return d.magnitude >= walkDistanceNeeded;
            case Action.Turn:
                if (rig == null) return true;
                return Mathf.Abs(Mathf.DeltaAngle(startYaw, rig.eulerAngles.y)) >= turnAngleNeeded;
            case Action.PressContinue:
            case Action.PressTrigger: return NewPress(CommonUsages.triggerButton);
            case Action.PressGrip: return NewPress(CommonUsages.gripButton);
            case Action.PointAt: return s.target != null && s.target.isHovered;
            case Action.Grab: return grabbedFlag || (s.target != null && s.target.isSelected);
            case Action.PressA: return Pressed(XRNode.RightHand, CommonUsages.primaryButton);
            case Action.PressB: return Pressed(XRNode.RightHand, CommonUsages.secondaryButton);
            case Action.PressX: return Pressed(XRNode.LeftHand, CommonUsages.primaryButton);
            case Action.PressY: return Pressed(XRNode.LeftHand, CommonUsages.secondaryButton);
        }
        return false;
    }

    // True only for a fresh press (the button must be let go after the previous step)
    private bool NewPress(InputFeatureUsage<bool> button)
    {
        bool down = AnyButton(button);
        if (!down) { wasPressed = false; return false; }
        return !wasPressed;
    }

    private static bool AnyButton(InputFeatureUsage<bool> b)
    {
        return Pressed(XRNode.LeftHand, b) || Pressed(XRNode.RightHand, b);
    }

    private static bool Pressed(XRNode hand, InputFeatureUsage<bool> b)
    {
        var dev = InputDevices.GetDeviceAtXRNode(hand);
        return dev.isValid && dev.TryGetFeatureValue(b, out bool p) && p;
    }

    // ---------- Panel placement ----------
    private void PlacePanel(bool instant)
    {
        if (panel == null || head == null) return;
        Vector3 fwd = head.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 pos = head.position + fwd * distance;
        pos.y = head.position.y + heightOffset;
        Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);

        Transform t = panel.transform;
        if (instant) { t.SetPositionAndRotation(pos, rot); return; }
        float k = 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime);
        t.position = Vector3.Lerp(t.position, pos, k);
        t.rotation = Quaternion.Slerp(t.rotation, rot, k);
    }
}