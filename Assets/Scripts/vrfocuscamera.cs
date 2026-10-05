using UnityEngine;

public class VRCameraFocus : MonoBehaviour
{
    public Transform cameraTransform;

    private bool focusing = false;
    private Transform target;

    public float speed = 5f;

    void Update()
    {
        if (focusing && target != null)
        {
            Vector3 direction = target.position - cameraTransform.position;

            Quaternion lookRotation = Quaternion.LookRotation(direction);

            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                lookRotation,
                Time.deltaTime * speed
            );
        }
    }

    public void FocusOn(Transform npc)
    {
        target = npc;
        focusing = true;
    }

    public void StopFocus()
    {
        focusing = false;
        target = null;
    }
}