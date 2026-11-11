using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        int horizontal = 0;

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal -= 1;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal += 1;
        }

        if (anim.GetInteger("Direction") != horizontal)
        {
            anim.SetInteger("Direction", horizontal);
        }
    }
}
