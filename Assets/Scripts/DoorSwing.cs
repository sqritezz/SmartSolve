using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Put this on the door's HINGE object (see setup notes).
// Click the door with the trigger -> it swings open. Click again -> it closes
// (turn off "Can Close" to keep it open for good).
// Needs a Collider + XR Simple Interactable + ClickOnly on the door.
public class DoorSwing : MonoBehaviour
{
    [Tooltip("The XR Simple Interactable on the door (leave empty if it's on this object)")]
    public XRBaseInteractable interactable;

    [Header("Swing")]
    [Tooltip("How far it opens (degrees). Use a negative number to open the other way.")]
    public float openAngle = 90f;
    public float swingTime = 0.8f;
    public bool canClose = false;

    [Header("Audio")]
    public AudioSource openSound;
    public AudioSource closeSound;

    [Header("Events")]
    public UnityEvent onOpened;

    [Header("State (read-only)")]
    public bool isOpen;

    private Quaternion closedRot;
    private Coroutine routine;

    private void Awake()
    {
        closedRot = transform.localRotation;
        if (interactable == null) interactable = GetComponentInChildren<XRBaseInteractable>();
        if (interactable != null) interactable.selectEntered.AddListener(args => Toggle());
    }

    public void Toggle()
    {
        if (isOpen) { if (canClose) Close(); }
        else Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (openSound != null) openSound.Play();
        Swing(closedRot * Quaternion.Euler(0f, openAngle, 0f));
        onOpened?.Invoke();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (closeSound != null) closeSound.Play();
        Swing(closedRot);
    }

    private void Swing(Quaternion target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(SwingRoutine(target));
    }

    private IEnumerator SwingRoutine(Quaternion target)
    {
        Quaternion start = transform.localRotation;
        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t / swingTime));
            yield return null;
        }
        transform.localRotation = target;
        routine = null;
    }
}
