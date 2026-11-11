using UnityEngine;
using UnityEngine.InputSystem;

public class BossAnimation : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
       anim = GetComponent<Animator>(); 
    }

    public void BossBulletAnim(bool isTrue)
    {
        anim.SetBool("isShooting", isTrue);
    }

    public void BossLaserAnim(bool isTrue)
    {
        anim.SetBool("isLasing", isTrue);
    }

    public void BossLaserBeam(bool isTrue)
    {
        anim.SetBool("isBeam", isTrue);
    }
}
