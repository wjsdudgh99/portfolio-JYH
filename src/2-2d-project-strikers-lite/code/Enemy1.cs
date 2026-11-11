using UnityEngine;

public class NormalEnemy : MonoBehaviour
{
    public float Speed = 3.0f;

    public GameObject HpUpItemPrefab;
    public GameObject DeathEffectPrefab;

    void Update()
    {
        Vector3 moveVector = new Vector3(0, -1f, 0);
        moveVector = moveVector * Speed * Time.deltaTime;
        transform.position += moveVector;

        if (transform.position.y < -6.0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("PlayerBullet"))
        {
            GameManager.Instance.AddScore(100); 

            float dice2 = Random.Range(0f, 100f);

            if (dice2 <= 5f || GameManager.Instance.DeadEnemyCount >= 10)
            {
                Instantiate(HpUpItemPrefab, transform.position, Quaternion.identity);

                GameManager.Instance.DeadEnemyCount = 0;
            }

            if (DeathEffectPrefab != null)
            {
                Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject); 

            Destroy(collider.gameObject);
        }
    }
}