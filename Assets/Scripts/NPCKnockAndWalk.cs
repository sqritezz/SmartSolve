using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// NPC intro:
//  1. Waits OUTSIDE the closed door (idle) while a knock SOUND plays.
//  2. Player opens the door -> knocking stops -> he WAVES once -> faces the player.
//  3. After the intro dialogue (call StartWalking) he walks along the waypoints
//     to his spot, then idles and faces the player.
// Put this on the NPC object that has the Animator.
public class NPCKnockAndWalk : MonoBehaviour
{
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

    [Header("Door")]
    [Tooltip("The part of the door that moves when it opens")]
    public Transform door;
    [Tooltip("How far (meters) the door must move to count as opened (rotation uses this x100 in degrees)")]
    public float doorOpenThreshold = 0.05f;

    [Header("Walking")]
    [Tooltip("Path from the door to his spot, in order. The LAST one is where he stays.")]
    public Transform[] waypoints;
    public float walkSpeed = 1.2f;
    public float turnSpeed = 6f;

    [Header("Facing")]
    [Tooltip("Turn to face the player after the door opens and after arriving")]
    public bool facePlayer = true;

    [Header("Events")]
    [Tooltip("Runs when the door opens (e.g. show a 'Talk to him' hint)")]
    public UnityEvent onDoorOpened;
    [Tooltip("Runs when he reaches his spot")]
    public UnityEvent onArrived;

    [Header("State (read-only)")]
    public bool isKnocking;
    public bool doorOpened;
    public bool isWalking;
    public bool hasArrived;

    private Transform player;
    private Vector3 doorStartPos;
    private Quaternion doorStartRot;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false; // the script moves him
    }

    private void Start()
    {
        if (Camera.main != null) player = Camera.main.transform;

        if (door != null)
        {
            doorStartPos = door.localPosition;
            doorStartRot = door.localRotation;
        }

        if (knockOnStart) StartKnocking();
    }

    private void Update()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;

        // Door opened -> stop knocking, wave
        if (!doorOpened && DoorIsOpen())
            OnDoorOpened();

        // After the door opens, keep facing the player (not while walking)
        if (doorOpened && !isWalking && facePlayer && player != null)
            TurnTowards(player.position);
    }

    // Can also be called from your door's script/event
    public void OnDoorOpened()
    {
        if (doorOpened) return;
        doorOpened = true;

        StopKnocking();

        if (animator != null && !string.IsNullOrEmpty(waveTrigger))
            animator.SetTrigger(waveTrigger);

        onDoorOpened?.Invoke();
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

    // Call this from the INTRO dialogue's "Runs when this dialogue finishes"
    public void StartWalking()
    {
        if (isWalking || hasArrived) return;
        if (waypoints == null || waypoints.Length == 0) return;

        StopKnocking();
        doorOpened = true;
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
        hasArrived = true;
        if (animator != null) animator.SetBool(walkingBool, false);

        Transform last = waypoints[waypoints.Length - 1];
        if (!facePlayer && last != null)
            transform.rotation = Quaternion.Euler(0f, last.eulerAngles.y, 0f);

        onArrived?.Invoke();
    }

    private bool DoorIsOpen()
    {
        if (door == null) return false;
        if (Vector3.Distance(door.localPosition, doorStartPos) > doorOpenThreshold) return true;
        if (Quaternion.Angle(door.localRotation, doorStartRot) > doorOpenThreshold * 100f) return true;
        return false;
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