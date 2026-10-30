using UnityEngine;

// OPTIONAL. Only add this to a fan that still spins the wrong way.
// While playing, change "Axis" between X, Y and Z until the fan spins flat
// like a real fan (you can change it live). Tick Reverse to spin the other way.
public class FanSpinAxis : MonoBehaviour
{
    public enum Axis { Auto, X, Y, Z }
    public Axis axis = Axis.Auto;
    public bool reverse = false;
}
