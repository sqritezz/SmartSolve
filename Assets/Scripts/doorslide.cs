using UnityEngine;

public class DoorSlide : MonoBehaviour
{
    public Vector3 openOffset = new Vector3(1.5f, 0, 0); // move left
    public float speed = 2f;

    [Header("Checklist Integration - Opening")]
    public SequentialChecklist checklist;
    public int openGroupIndex = 0;
    public int openObjectiveIndex = 1;

    [Header("Checklist Integration - Closing")]
    [Tooltip("Set closeGroupIndex to -1 if there's no 'close the door' objective")]
    public int closeGroupIndex = -1;
    public int closeObjectiveIndex = 0;

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

        if (isOpen)
        {
            // Door just opened
            if (checklist != null && checklist.IsCurrentStep(openGroupIndex, openObjectiveIndex))
            {
                checklist.CompleteObjective(openGroupIndex, openObjectiveIndex);
            }
        }
        else
        {
            // Door just closed
            if (checklist != null && closeGroupIndex >= 0 &&
                checklist.IsCurrentStep(closeGroupIndex, closeObjectiveIndex))
            {
                checklist.CompleteObjective(closeGroupIndex, closeObjectiveIndex);
            }
        }
    }
}