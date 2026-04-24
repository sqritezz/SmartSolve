using UnityEngine;

public class DoorSlide : MonoBehaviour
{
    public Vector3 openOffset = new Vector3(1.5f, 0, 0); // move left
    public float speed = 2f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + openOffset;
    }

    void Update()
    {
        if (isOpen)
        {
            transform.position = Vector3.Lerp(transform.position, openPos, Time.deltaTime * speed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, closedPos, Time.deltaTime * speed);
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}