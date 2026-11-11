using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Boss Stats")]
    public int Hp = 500;                 
    public float Speed = 1.5f;   
    private bool isDead = false;

    [Header("Prefabs")]
    //public GameObject EnemyBulletPrefab; 
    //public GameObject BossMissilePrefab; 
    public GameObject BossBulletPrefab;
    public GameObject LaserBeamObject;
    public GameObject DeathEffectPrefab;

    [Header("Attack Settings")]
    public float FireRate = 1.5f;        
    private float lastFireTime = 0f;

    [Header("Movement Settings")]
    private float targetY = 3.5f;       
    private bool isArrived = false;

    [Header("BossFirePoint")]
    public Transform FirePoint;

    private float leftX = -2.0f;
    private float rightX = 2.0f;
    private Vector3 targetPosition;

    private int attackPattern = 0;
    private int patternRepeatCount = 0;
    private BossAnimation bossAnim;

    void Start()
    {
        targetPosition = transform.position;

        bossAnim = GetComponent<BossAnimation>();
    }

    void Update()
    {
        if (isArrived == false)
        {
            Vector3 moveVector = new Vector3(0, -1f, 0);
            transform.position += moveVector * Speed * Time.deltaTime;

            if (transform.position.y <= targetY)
            {
                transform.position = new Vector3(0f, targetY, 0f);
                isArrived = true;

                StartCoroutine(BossPatrolRoutine());
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Speed * Time.deltaTime);
        }

        if (isArrived && Time.time >= lastFireTime + FireRate)
        {
            FireBossWeapon();
        }
    }

    private IEnumerator BossPatrolRoutine()
    {
        while (true)
        {
            targetPosition = new Vector3(leftX, targetY, 0f);
            while (transform.position.x > leftX + 0.05f)
            {
                yield return null;
            }
            yield return new WaitForSeconds(1.0f);

            targetPosition = new Vector3(rightX, targetY, 0f);
            while (transform.position.x < rightX - 0.05f)
            {
                yield return null;
            }
            // 오른쪽 도착 후 1초 대기
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator BurstShotPattern()
    {
        bossAnim.BossBulletAnim(true);

        for (int i = 0; i < 3; ++i)
        {
            if (BossBulletPrefab != null)
            {
                Quaternion FourOClockRot = Quaternion.Euler(0, 0, 210f);
                Quaternion FiveOClockRot = Quaternion.Euler(0, 0, 195f);
                Quaternion SixOClockRot = Quaternion.Euler(0, 0, 180f);
                Quaternion SevenOClockRot = Quaternion.Euler(0, 0, 165f);
                Quaternion EightOClockRot = Quaternion.Euler(0, 0, 150f);

                Instantiate(BossBulletPrefab, FirePoint.position, FourOClockRot);
                Instantiate(BossBulletPrefab, FirePoint.position, FiveOClockRot);
                Instantiate(BossBulletPrefab, FirePoint.position, SixOClockRot);
                Instantiate(BossBulletPrefab, FirePoint.position, SevenOClockRot);
                Instantiate(BossBulletPrefab, FirePoint.position, EightOClockRot);
            }

            yield return new WaitForSeconds(0.2f);
        }

        bossAnim.BossBulletAnim(false);
    }

    private IEnumerator ClockwiseShotPattern()
    {
        bossAnim.BossBulletAnim(true);

        float currentAngle = 120f;

        for (int i = 0; i < 12; ++i)
        {
            if (BossBulletPrefab != null)
            {
                Quaternion rot = Quaternion.Euler(0, 0, currentAngle);

                Instantiate(BossBulletPrefab, FirePoint.position, rot);
            }

            currentAngle += 12f;

            yield return new WaitForSeconds(0.1f);
        }

        bossAnim.BossBulletAnim(false);
    }

    private IEnumerator CounterClockwiseShotPattern()
    {
        bossAnim.BossBulletAnim(true);

        float currentAngle = 240f;

        for (int i = 0; i < 12; ++i)
        {
            if (BossBulletPrefab != null)
            {
                Quaternion rot = Quaternion.Euler(0, 0, currentAngle);

                Instantiate(BossBulletPrefab, FirePoint.position, rot);
            }

            currentAngle -= 12f;

            yield return new WaitForSeconds(0.1f);
        }

        bossAnim.BossBulletAnim(false);
    }

    private IEnumerator LaserBeamPattern()
    {
        bossAnim.BossLaserAnim(true);
        bossAnim.BossLaserBeam(true);

        GameObject spawnedLaser = null;

        yield return new WaitForSeconds(0.0f);

        if (LaserBeamObject != null)
        {
            spawnedLaser = Instantiate
                (LaserBeamObject, FirePoint.position, Quaternion.identity, transform);

            spawnedLaser.transform.SetParent(FirePoint);

            spawnedLaser.transform.localPosition = Vector3.zero;

            Animator laserAnimator = spawnedLaser.GetComponentInChildren<Animator>();
        }

        yield return new WaitForSeconds(1.5f);

        if (spawnedLaser != null)
        {
            Destroy(spawnedLaser);
        }

        bossAnim.BossLaserAnim(false);
        bossAnim.BossLaserBeam(false);
    }

    void FireBossWeapon()
    {
        if (attackPattern == 0)
        {
            StartCoroutine(BurstShotPattern());
        }
        else if (attackPattern == 1)
        {
            StartCoroutine(ClockwiseShotPattern());
        }
        else if (attackPattern == 2)
        {
            StartCoroutine(CounterClockwiseShotPattern());
        }
        else if (attackPattern == 3)
        {
            StartCoroutine(LaserBeamPattern());
            
        }
        patternRepeatCount += 1;

        if (patternRepeatCount == 3)
        {
            patternRepeatCount = 0;

            attackPattern = Random.Range(0, 4);

            lastFireTime = Time.time + 0.5f;
        }

        else
        {
            lastFireTime = Time.time + 0.05f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (isDead == true) return;

        if (collider.gameObject.CompareTag("PlayerBullet"))
        {
            Destroy(collider.gameObject); 

            Hp -= 1; 

            GameManager.Instance.AddScore(10);

            if (Hp <= 0)
            {
                isDead = true;
                Debug.Log("보스 처치");

                GameManager.Instance.AddScore(5000); 

                if (DeathEffectPrefab != null)
                {
                    GameObject eff = Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);

                    eff.transform.position = new Vector3(eff.transform.position.x, eff.transform.position.y, 0f);

                    Animator effAnim = eff.GetComponent<Animator>();
                    if (effAnim != null)
                    {
                        effAnim.Play("Boss DeathEffect", 0, 0.0f);
                    }
                }

                GameManager.Instance.TriggerVictoryClear();

                Destroy(gameObject);

            }
        }
    }
}