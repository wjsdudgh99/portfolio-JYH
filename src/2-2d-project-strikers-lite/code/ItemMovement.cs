using UnityEngine;

public class ItemMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public float Speed = 2.0f;

    void Update()
    {
        transform.position += Vector3.down * Time.deltaTime * Speed;
    }
}
