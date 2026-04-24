using UnityEngine;

public class NPCController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Talk()
    {
        animator.SetBool("isTalking", true);
    }

    public void StopTalk()
    {
        animator.SetBool("isTalking", false);
    }

    public void BreathingIdle()
    {
        animator.SetBool("isBreathing", true);
    }

    public void StopBreathingIdle()
    {
        animator.SetBool("isBreathing", false);
    }

    public void Waving()
    {
        animator.SetBool("isWaving", true);
    }

    public void StopWaving()
    {
        animator.SetBool("isWaving", false);
    }
}