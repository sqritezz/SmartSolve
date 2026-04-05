using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XROffsetgrabinteractable : XRGrabInteractable
{
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!attachTransform)
        {
            GameObject attachpoint = new GameObject("Game Offset Pivot");
            attachpoint.transform.SetParent(transform, false);
            attachTransform = attachpoint.transform;
        }
        else
        {
            initialLocalPos = attachTransform.localPosition;
            initialLocalRot = attachTransform.localRotation;
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRDirectInteractor)
        {
            attachTransform.position = args.interactorObject.transform.position;
            attachTransform.rotation = args.interactorObject.transform.rotation;
        }
        else
        {
            attachTransform.localPosition = initialLocalPos;
            attachTransform.localRotation = initialLocalRot;
        }

            base.OnSelectEntered(args);
    }
}
