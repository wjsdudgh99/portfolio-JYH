using UnityEngine;

public class DeadAnimation : MonoBehaviour
{
    public float DestroyTime = 0.5f;

    void Start()
    {
        Destroy(gameObject, DestroyTime);
    }

    void Update()
    {
        
    }
}
