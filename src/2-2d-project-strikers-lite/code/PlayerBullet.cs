using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerBullet : MonoBehaviour
{
    public float Speed = 8.0f;
    public int Damage = 1;
    public GameObject BulletPrefab;

    void Update()
    {
        Vector3 moveVector = new Vector3(0, 1f, 0);
        moveVector = moveVector * Speed * Time.deltaTime;
        transform.position += moveVector;

        if (transform.position.y > 6.0f)
        {
            Destroy(gameObject);
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Instantiate(BulletPrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}