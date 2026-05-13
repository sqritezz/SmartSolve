using UnityEngine;
using System.Collections;

public class IntroToMenu : MonoBehaviour
{
    public Transform menuSpawnPoint;
    public float introDuration = 5f;

    IEnumerator Start()
    {
        // wait for intro
        yield return new WaitForSeconds(introDuration);

        // teleport player
        transform.position = menuSpawnPoint.position;
        transform.rotation = menuSpawnPoint.rotation;
    }
}