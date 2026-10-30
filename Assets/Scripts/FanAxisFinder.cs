using System.Collections.Generic;
using UnityEngine;

// Helper used by the GPU managers: finds the right spin axis for EACH fan by itself.
// A fan is flat, so it spins around its THINNEST side. All the fan's meshes
// (frame, blades, hub) are measured together in the fan's own space, so it works
// however the model is rotated or split into parts.
public static class FanAxisFinder
{
    private static readonly Dictionary<Transform, Vector3> cache = new Dictionary<Transform, Vector3>();
    private static readonly Dictionary<Transform, Vector3> centers = new Dictionary<Transform, Vector3>();

    // Spins the fan around its OWN MIDDLE (not its pivot point), so it never wobbles
    // or swings around even if the model's pivot is off-center.
    public static void Spin(Transform fan, float degrees)
    {
        if (fan == null) return;

        Vector3 axis = GetAxis(fan);
        FanSpinAxis manual = fan.GetComponent<FanSpinAxis>();
        if (manual != null && manual.axis != FanSpinAxis.Axis.Auto)
            axis = manual.axis == FanSpinAxis.Axis.X ? Vector3.right
                 : manual.axis == FanSpinAxis.Axis.Y ? Vector3.up : Vector3.forward;
        if (manual != null && manual.reverse) degrees = -degrees;

        Vector3 worldCenter = fan.position + fan.rotation * centers[fan];
        fan.RotateAround(worldCenter, fan.rotation * axis, degrees);
    }

    public static Vector3 GetAxis(Transform fan)
    {
        if (cache.TryGetValue(fan, out Vector3 axis)) return axis;

        // Size of ALL meshes under the fan, measured in the fan's own (local) space
        bool found = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;
        foreach (var mf in fan.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                // mesh space -> world -> fan local (unscaled by the fan's own scale)
                Vector3 w = mf.transform.TransformPoint(corner);
                Vector3 l = Quaternion.Inverse(fan.rotation) * (w - fan.position);
                if (!found) { min = max = l; found = true; }
                else { min = Vector3.Min(min, l); max = Vector3.Max(max, l); }
            }
        }

        axis = Vector3.forward;
        centers[fan] = found ? (min + max) * 0.5f : Vector3.zero;
        if (found)
        {
            Vector3 s = max - min;
            axis = (s.x <= s.y && s.x <= s.z) ? Vector3.right
                 : (s.y <= s.z) ? Vector3.up : Vector3.forward;
        }

        Debug.Log("[FanAxisFinder] " + fan.name + " spins around its local " +
                  (axis == Vector3.right ? "X" : axis == Vector3.up ? "Y" : "Z") + " axis", fan);
        cache[fan] = axis;
        return axis;
    }
}