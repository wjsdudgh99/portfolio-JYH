using UnityEngine;

public class Enemy2Animation : MonoBehaviour
{
    private Animator anim;
    private int currentLiveState = 999;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Enemy2Anim(int stateNumber)
    {
        if (anim == null) return;

        if (currentLiveState == stateNumber) return;

        currentLiveState = stateNumber;

        anim.SetInteger("Move", stateNumber);
    }
}
