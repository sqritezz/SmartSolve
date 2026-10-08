using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// NPC arrival at the door:
//  1. Waits OUTSIDE the closed door while a knock SOUND plays.
//     He can't be talked to yet.
//  2. Player opens the door -> knocking stops -> he WAVES -> the DOOR dialogue
//     starts by itself ("Hello, I am Azer." / "May I come in?").
//  3. When the door dialogue ends, he walks along the waypoints to his spot.
//  4. When he arrives, the INTRO dialogue (the tutorial one) is unlocked and the
//     player clicks him to continue talking. The door lines never repeat.
//
// Put this on the NPC root (NPC Easy (RAM)).
// The two NPCDialogue components go on the object you click (e.g. "boy").
// Do NOT wire StartWalking in the dialogue events - this script handles it.
public class NPCKnockAndWalk : MonoBehaviour
{
    [Header("Dialogues")]
    [Tooltip("NPCDialogue with ONLY the door lines (Hello, I am Azer. / May I come in?)")]
    public NPCDialogue doorDialogue;
    [Tooltip("The tutorial intro NPCDialogue (Thanks for letting me in... etc). Unlocked when he arrives.")]
    public NPCDialogue introDialogue;
    [Tooltip("Seconds after the door opens before he starts talking (lets the wave play)")]
    public float talkDelayAfterDoor = 1.2f;

    [Header("Animator")]
    public Animator animator;
    [Tooltip("Animator Trigger that plays the wave")]
    public string waveTrigger = "Wave";
    [Tooltip("Animator Bool that switches Idle <-> Walking")]
    public string walkingBool = "IsWalking";

    [Header("Knock Sound")]
    [Tooltip("Looping knock sound (Loop on, Play On Awake off, Spatial Blend 1)")]
    public AudioSource knockSound;
    [Tooltip("Start knocking when the level starts")]
    public bool knockOnStart = true;

    [Header("Walking")]
    [Tooltip("Path from the door to his spot, in order. The LAST one is where he stays.")]
    public Transform[] waypoints;
    public float walkSpeed = 1.2f;
    public float turnSpeed = 6f;

    [Header("Facing")]
    [Tooltip("Turn to face the player after the door opens and after arriving")]
    public bool facePlayer = true;

    [Header("Events")]
    [Tooltip("Runs when the door opens")]
    public UnityEvent onDoorOpened;
    [Tooltip("Runs when he reaches his spot")]
    public UnityEvent onArrived;

    [Header("State (read-only)")]
    public bool isKnocking;
    public bool doorOpened;
    public bool isWalking;
    public bool hasArrived;

    private Transform player;
    private bool doorTalkDone = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false; // the script moves him

        if (doorDialogue != null)
            doorDialogue.onDialogueFinished.AddListener(OnDoorDialogueFinished);
    }

    private void Start()
    {
        if (Camera.main != null) player = Camera.main.transform;
        if (knockOnStart) StartKnocking();
        StartCoroutine(LockDialoguesNextFrame());
    }

    // Runs one frame late so it wins over TutorialFlowManager turning the intro on
    private IEnumerator LockDialoguesNextFrame()
    {
        yield return null;
        if (!doorOpened && doorDialogue != null) doorDialogue.isActiveDialogue = false;
        if (!hasArrived && introDialogue != null) introDialogue.isActiveDialogue = false;
    }

    private void Update()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;

        // After the door opens, keep facing the player (not while walking)
        if (doorOpened && !isWalking && facePlayer && player != null)
            TurnTowards(player.position);
    }

    // Wired from DoorSwing -> On Opened
    public void OnDoorOpened()
    {
        if (doorOpened) return;
        doorOpened = true;

        StopKnocking();

        if (animator != null && !string.IsNullOrEmpty(waveTrigger))
            animator.SetTrigger(waveTrigger);

        onDoorOpened?.Invoke();
        StartCoroutine(StartDoorTalk());
    }

    private IEnumerator StartDoorTalk()
    {
        yield return new WaitForSeconds(talkDelayAfterDoor);

        if (doorDialogue != null)
        {
            if (introDialogue != null) introDialogue.isActiveDialogue = false;
            doorDialogue.isActiveDialogue = true;
            doorDialogue.StartDialogue();
        }
        else
        {
            StartWalking(); // no door dialogue set - just walk in
        }
    }

    private void OnDoorDialogueFinished()
    {
        if (doorTalkDone) return;
        doorTalkDone = true;

        if (doorDialogue != null) doorDialogue.isActiveDialogue = false; // never repeats
        StartWalking();
    }

    public void StartKnocking()
    {
        isKnocking = true;
        if (knockSound != null && !knockSound.isPlaying) knockSound.Play();
    }

    public void StopKnocking()
    {
        isKnocking = false;
        if (knockSound != null && knockSound.isPlaying) knockSound.Stop();
    }

    public void StartWalking()
    {
        if (isWalking || hasArrived) return;

        StopKnocking();
        doorOpened = true;

        if (waypoints == null || waypoints.Length == 0)
        {
            Arrive();
            return;
        }
        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        isWalking = true;
        if (animator != null) animator.SetBool(walkingBool, true);

        foreach (Transform wp in waypoints)
        {
            if (wp == null) continue;

            Vector3 target = new Vector3(wp.position.x, transform.position.y, wp.position.z);
            while (FlatDistance(transform.position, target) > 0.05f)
            {
                TurnTowards(target);
                transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        isWalking = false;
        if (animator != null) animator.SetBool(walkingBool, false);

        Transform last = waypoints[waypoints.Length - 1];
        if (!facePlayer && last != null)
            transform.rotation = Quaternion.Euler(0f, last.eulerAngles.y, 0f);

        Arrive();
    }

    private void Arrive()
    {
        hasArrived = true;
        if (introDialogue != null) introDialogue.isActiveDialogue = true; // player can talk now
        onArrived?.Invoke();
    }

    private void TurnTowards(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}