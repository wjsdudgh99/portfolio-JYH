using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float Speed = 5.0f;

    void Update()
    {
        transform.position += transform.up * Speed * Time.deltaTime;

        if (transform.position.y < -6.0f || transform.position.x < -4.0f || transform.position.x > 4.0f)
        {
            Destroy(gameObject);
        }
    }
}