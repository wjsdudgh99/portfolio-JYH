using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject Enemy1Prefab; 
    public GameObject Enemy2Prefab; 

    public float SpawnRate = 1f;     
    private float lastSpawnTime = 0f;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsBossStage)
        {
            return;
        }

        if (Time.time >= lastSpawnTime + SpawnRate)
        {
            int randomEnemy = Random.Range(0, 2);

            float randomX = Random.Range(-3.0f, 3.0f);
            Vector3 spawnPosition = new Vector3(randomX, 6.0f, 0);

            if (randomEnemy == 0 && Enemy1Prefab != null)
            {
                Instantiate(Enemy1Prefab, spawnPosition, Quaternion.Euler(0f, 0f, 180f));
            }
            else if (randomEnemy == 1 && Enemy2Prefab != null)
            {
                Instantiate(Enemy2Prefab, spawnPosition, Quaternion.Euler(0f, 0f, 180f));
            }

            lastSpawnTime = Time.time;
        }
    }
}