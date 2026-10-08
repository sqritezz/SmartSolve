using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Put this on an empty object (e.g. "TutorialPropReset (PSU Easy)").
// It remembers where each prop starts, and when ResetProps() is called
// (wire it to the follow-up dialogue's finish event), every prop goes back
// to its starting spot and size. If the player is still holding one, it is dropped
// from their hand first. Runs only once, so the tutorial stays "done".
public class TutorialPropReset : MonoBehaviour
{
    [Header("Props")]
    [Tooltip("Every object the player can move during the tutorial (brush, etc.)")]
    public Transform[] propsToReset;

    [Header("Timing")]
    [Tooltip("Seconds to wait after the dialogue ends before resetting")]
    public float delayBeforeReset = 0f;

    [Header("Audio (optional)")]
    public AudioSource resetSound;

    private class SavedProp
    {
        public Transform transform;
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Rigidbody rigidbody;
        public XRBaseInteractable interactable;
    }

    private readonly List<SavedProp> savedProps = new List<SavedProp>();
    private bool hasReset = false;

    private void Start()
    {
        savedProps.Clear();
        if (propsToReset == null) return;

        foreach (var prop in propsToReset)
        {
            if (prop == null) continue;

            savedProps.Add(new SavedProp
            {
                transform = prop,
                parent = prop.parent,
                localPosition = prop.localPosition,
                localRotation = prop.localRotation,
                localScale = prop.localScale,
                rigidbody = prop.GetComponent<Rigidbody>(),
                interactable = prop.GetComponent<XRBaseInteractable>()
            });
        }
    }

    // Wire this to the follow-up dialogue's "Runs when this dialogue finishes"
    public void ResetProps()
    {
        if (hasReset) return;
        hasReset = true;
        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        if (delayBeforeReset > 0f)
            yield return new WaitForSeconds(delayBeforeReset);

        // 1. Force-drop anything the player is still holding.
        //    Disabling an interactable makes XR release it.
        var forceDropped = new List<XRBaseInteractable>();
        foreach (var p in savedProps)
        {
            if (p.interactable != null && p.interactable.enabled && p.interactable.isSelected)
            {
                p.interactable.enabled = false;
                forceDropped.Add(p.interactable);
            }
        }

        // Wait a frame so XR finishes releasing and restores each Rigidbody's state
        yield return null;

        // 2. Put every prop back exactly where it started
        foreach (var p in savedProps)
        {
            if (p.transform == null) continue;

            if (p.rigidbody != null && !p.rigidbody.isKinematic)
            {
                p.rigidbody.linearVelocity = Vector3.zero;
                p.rigidbody.angularVelocity = Vector3.zero;
            }

            p.transform.SetParent(p.parent, false);
            p.transform.localPosition = p.localPosition;
            p.transform.localRotation = p.localRotation;
            p.transform.localScale = p.localScale; // back to its original size

            if (p.rigidbody != null)
            {
                p.rigidbody.position = p.transform.position;
                p.rigidbody.rotation = p.transform.rotation;
            }
        }

        // 3. Let the player grab the dropped props again
        foreach (var interactable in forceDropped)
        {
            if (interactable != null)
                interactable.enabled = true;
        }

        if (resetSound != null)
            resetSound.Play();
    }
}