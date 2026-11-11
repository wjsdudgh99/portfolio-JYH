using System.Collections;
using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    public int Hp = 7;
    public float Speed = 3.0f;

    public GameObject EnemyBulletPrefab;
    public float FireRate = 1.5f;
    private float lastFireTime = 0f;

    private float targetY = 3.5f;
    private bool isArrived = false;

    private float leftX;
    private float rightX;
    private Vector3 targetPosition;
    public float PatrolRange = 1.5f;

    public GameObject PowerUpItemPrefab;
    public GameObject DeathEffectPrefab;

    private Enemy2Animation Enemy2Anim;

    private bool isHeadingRightFirst = true;

    void Awake()
    {
        float startX = transform.position.x;

        if (startX < 0)
        {
            leftX = startX;
            rightX = startX + (PatrolRange * 2f); 
            isHeadingRightFirst = true;           
        }
        else
        {
            leftX = startX - (PatrolRange * 2f); 
            rightX = startX;
            isHeadingRightFirst = false;     
        }

        Enemy2Anim = GetComponent<Enemy2Animation>();
    }

    void Update()
    {
        if (isArrived == false)
        {
            Vector3 moveVector = new Vector3(0, -1f, 0);
            transform.position += moveVector * Speed * Time.deltaTime;

            if (transform.position.y <= targetY)
            {
                transform.position = new Vector3(transform.position.x, targetY, 0f);
                isArrived = true;

                StartCoroutine(PatrolRoutine());
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);
        }

        if (Time.time >= lastFireTime + FireRate)
        {
            if (EnemyBulletPrefab != null)
            {
                FireThreeWay();
                lastFireTime = Time.time;
            }
        }
    }

    private IEnumerator PatrolRoutine()
    {
        targetPosition = transform.position;

        bool skipRightFirst = !isHeadingRightFirst;

        while (true)
        {
            if (skipRightFirst == false)
            {
                if (Enemy2Anim != null) Enemy2Anim.Enemy2Anim(1);

                targetPosition = new Vector3(rightX, targetY, 0f);
                while (transform.position.x < rightX - 0.05f)
                {
                    yield return null;
                }

                if (Enemy2Anim != null) Enemy2Anim.Enemy2Anim(0);
                yield return new WaitForSeconds(1.0f);
            }

            skipRightFirst = false;

            if (Enemy2Anim != null) Enemy2Anim.Enemy2Anim(-1);

            targetPosition = new Vector3(leftX, targetY, 0f);
            while (transform.position.x > leftX + 0.05f)
            {
                yield return null;
            }

            if (Enemy2Anim != null) Enemy2Anim.Enemy2Anim(0);
            yield return new WaitForSeconds(1.0f);
        }
    }

    void FireThreeWay()
    {
        Quaternion centerRotation = Quaternion.Euler(0, 0, 180f);
        Quaternion leftRotation = Quaternion.Euler(0, 0, 160f);
        Quaternion rightRotation = Quaternion.Euler(0, 0, 200f);

        Instantiate(EnemyBulletPrefab, transform.position, centerRotation);
        Instantiate(EnemyBulletPrefab, transform.position, leftRotation);
        Instantiate(EnemyBulletPrefab, transform.position, rightRotation);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("PlayerBullet"))
        {
            Destroy(collider.gameObject);
            Hp -= 1;

            if (Hp <= 0)
            {
                GameManager.Instance.AddScore(300); 

                GameManager.Instance.DeadEnemyCount++;

                float dice = Random.Range(0f, 100f);

                if (dice <= 6.5f || GameManager.Instance.DeadEnemyCount >= 10)
                {
                    Instantiate(PowerUpItemPrefab, transform.position, Quaternion.identity);

                    GameManager.Instance.DeadEnemyCount = 0;
                }

                if (DeathEffectPrefab != null)
                {
                    Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
            }
        }
    }
}