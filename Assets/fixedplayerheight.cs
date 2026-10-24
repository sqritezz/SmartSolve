using System.Collections;
using UnityEngine;
using UnityEngine.XR;

// Put this on the XR Origin (VR).
// Makes everyone see the game from the SAME eye height, tall or short,
// standing or sitting. It measures the player's real head height once at the
// start, then moves the Camera Offset up/down so the eyes end up at Eye Height.
// The player can still crouch and lean normally after that.
//
// Recalibrate: hold the LEFT controller's Y button for 1 second,
// or call Recalibrate() from a UI button (e.g. in Settings).
public class FixedPlayerHeight : MonoBehaviour
{
    [Tooltip("The 'Camera Offset' child of the XR Origin. Empty = found automatically.")]
    public Transform cameraOffset;
    [Tooltip("The Main Camera (the headset). Empty = found automatically.")]
    public Transform head;

    [Header("Height")]
    [Tooltip("Eye height in meters everyone will have (1.6 = average adult standing)")]
    public float eyeHeight = 1.6f;
    [Tooltip("Wait this long after start before measuring (the headset needs a moment)")]
    public float measureDelay = 1.0f;

    [Header("Recalibrate")]
    public bool holdYToRecalibrate = true;
    public float holdTime = 1f;

    [Header("State (read-only)")]
    public float measuredHeadHeight;
    public float appliedOffset;

    private bool calibrated = false;
    private float holdTimer = 0f;

    private void Awake()
    {
        if (cameraOffset == null)
        {
            var t = transform.Find("Camera Offset");
            if (t != null) cameraOffset = t;
        }
        if (head == null && Camera.main != null) head = Camera.main.transform;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(measureDelay);
        // wait until the headset is actually tracking
        float waited = 0f;
        while (head != null && head.localPosition.sqrMagnitude < 0.0001f && waited < 5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        Recalibrate();
    }

    // Call from a Settings button too
    public void Recalibrate()
    {
        if (cameraOffset == null || head == null)
        {
            Debug.LogWarning("[FixedPlayerHeight] Camera Offset or Main Camera not found.", this);
            return;
        }
        // head height above the Camera Offset = the player's real (tracked) height
        measuredHeadHeight = head.localPosition.y;
        appliedOffset = eyeHeight - measuredHeadHeight;
        calibrated = true;
        Apply();
    }

    // Lets a Settings slider change the height (e.g. 1.2 - 1.9)
    public void SetEyeHeight(float meters)
    {
        eyeHeight = meters;
        if (calibrated) { appliedOffset = eyeHeight - measuredHeadHeight; Apply(); }
    }

    private void LateUpdate()
    {
        // keep it applied (the XR Origin may reset the offset on its own)
        if (calibrated) Apply();

        if (!holdYToRecalibrate) return;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool yDown = left.isValid && left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool p) && p;
        if (yDown)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdTime) { holdTimer = -999f; Recalibrate(); }   // once per hold
        }
        else holdTimer = 0f;
    }

    private void Apply()
    {
        var p = cameraOffset.localPosition;
        if (Mathf.Abs(p.y - appliedOffset) > 0.0005f)
            cameraOffset.localPosition = new Vector3(p.x, appliedOffset, p.z);
    }
}