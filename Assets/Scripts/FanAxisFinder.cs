using System.Collections.Generic;
using UnityEngine;

// Helper used by the GPU managers: finds the right spin axis for EACH fan by itself.
// A fan is flat, so it spins around its THINNEST side. Works no matter how the
// fan model is rotated, so every fan turns like a real fan instead of tumbling.
public static class FanAxisFinder
{
    private static readonly Dictionary<Transform, Vector3> cache = new Dictionary<Transform, Vector3>();

    public static void Spin(Transform fan, float degrees)
    {
        if (fan == null) return;
        fan.Rotate(GetAxis(fan), degrees, Space.Self);
    }

    public static Vector3 GetAxis(Transform fan)
    {
        if (cache.TryGetValue(fan, out Vector3 axis)) return axis;

        axis = Vector3.forward;
        MeshFilter mf = fan.GetComponent<MeshFilter>();
        if (mf == null) mf = fan.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Vector3 s = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
            s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            Vector3 local = (s.x <= s.y && s.x <= s.z) ? Vector3.right
                          : (s.y <= s.z) ? Vector3.up : Vector3.forward;
            // mesh's thin axis -> expressed in the fan's own space
            Vector3 world = mf.transform.TransformDirection(local);
            axis = fan.InverseTransformDirection(world).normalized;
        }
        cache[fan] = axis;
        return axis;
    }
}
