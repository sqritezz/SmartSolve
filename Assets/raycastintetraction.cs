using UnityEngine;

public class RaycastInteraction : MonoBehaviour
{
    public float rayDistance = 10f;
    public LineRenderer lineRenderer;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Vector3 endPoint = transform.position + transform.forward * rayDistance;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            endPoint = hit.point;

            Debug.Log("Ray script running on: " + gameObject.name);

            if (Input.GetKeyDown(KeyCode.E))
            {
                PowerButton button = hit.collider.GetComponentInParent<PowerButton>();

                if (button != null)
                {
                    Debug.Log("BUTTON PRESSED");
                }
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, endPoint);
        }

        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.red);
    }
}