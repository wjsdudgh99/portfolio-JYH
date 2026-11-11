using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public float Speed = 1.0f;
    public GameObject BulletPrefab; 
    public GameObject DeathEffectPrefab;


    public float FireRate = 0.2f;   
    private float lastFireTime = 0f; 

    private SpriteRenderer spriteRenderer;
    private bool isColliding = false;

    public int PlayerHp = 5;

    public bool IsDead = false;

    public int WeaponLevel = 1;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameManager.Instance.UpdateLife(PlayerHp);
    }

    void Update()
    {
        Vector3 moveVector = new Vector3(0, 0, 0);

        if (Keyboard.current.upArrowKey.isPressed)
        {
            moveVector.y = moveVector.y + 1f;
        }

        if (Keyboard.current.downArrowKey.isPressed)
        {
            moveVector.y = moveVector.y - 1f;
        }

        if (Keyboard.current.leftArrowKey.isPressed)
        {
            moveVector.x = moveVector.x - 1f;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            moveVector.x = moveVector.x + 1f;
        }

        moveVector = moveVector * Speed * Time.deltaTime;
        transform.position += moveVector;

        float clampedX = Mathf.Clamp(transform.position.x, -2.8f, 2.8f);
        float clampedY = Mathf.Clamp(transform.position.y, -4.8f, 4.8f);

        transform.position = new Vector3(clampedX, clampedY, 0f);

        if (Keyboard.current.spaceKey.isPressed)
        {
            if (Time.time >= lastFireTime + FireRate)
            {
                if (BulletPrefab != null)
                {
                    if (WeaponLevel == 1)
                    {
                        Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                    }

                    else if (WeaponLevel == 2)
                    {
                        Instantiate(BulletPrefab, transform.position + new Vector3(-0.1f, 0f, 0f), Quaternion.identity);
                        Instantiate(BulletPrefab, transform.position + new Vector3(0.1f, 0f, 0f), Quaternion.identity);
                    }

                    else if (WeaponLevel == 3)
                    {
                        Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                        Instantiate(BulletPrefab, transform.position + new Vector3(-0.2f, 0f, 0f), Quaternion.identity);
                        Instantiate(BulletPrefab, transform.position + new Vector3(0.2f, 0f, 0f), Quaternion.identity);
                    }

                    lastFireTime = Time.time;
                }
                else
                {
                    Debug.LogWarning("PlayerCombat: BulletPrefab이 인스펙터 창에 연결되지 않았습니다!");
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Item"))
        {
            Debug.Log("총알 강화 아이템을 먹음");
            WeaponLevel += 1;
            if (WeaponLevel > 3) WeaponLevel = 3;
            Destroy(collider.gameObject);
        }

        if (collider.gameObject.CompareTag("HpItem"))
        {
            Debug.Log("체력 회복 아이템을 먹음");
            PlayerHp += 1;
            if (PlayerHp > 9) PlayerHp = 9;
            Destroy(collider.gameObject);
            GameManager.Instance.UpdateLife(PlayerHp);
        }

        if (IsDead || isColliding) return;

        if (collider.gameObject.CompareTag("Obstacle") || collider.gameObject.CompareTag("Enemy"))
        {
            Destroy(collider.gameObject);
            ProcessDamage();
            return; 
        }

        if (collider.gameObject.CompareTag("BossLaser"))
        {
            Debug.Log("레이저에 맞음");
            ProcessDamage();
            return; 
        }
    }

    private void ProcessDamage()
    {
        isColliding = true;
        PlayerHp -= 1;
        GameManager.Instance.UpdateLife(PlayerHp);

        if (PlayerHp <= 0)
        {
            Debug.Log("죽음");
            IsDead = true;

            if (DeathEffectPrefab != null)
            {
                GameObject eff = Instantiate(DeathEffectPrefab, transform.position, Quaternion.identity);

                eff.transform.position =
                    new Vector3(eff.transform.position.x, eff.transform.position.y, 0f);

                Animator effAnim = eff.GetComponent<Animator>();
                if (effAnim != null)
                {
                    effAnim.Play("Player DeathEffect", 0, 0.0f);
                }
            }

            GameManager.Instance.TriggerGameOver();

            Destroy(gameObject);
        }
        else
        {
            Debug.Log("체력이 1 까임");
            StartCoroutine(CollisionEffect());
        }
    }

    private IEnumerator CollisionEffect()
    {
        isColliding = true;

        Color origin = spriteRenderer.color;
        Color effect = spriteRenderer.color;
        effect.a = 0.2f; 

        float term = 0.1f;

        for (int i = 0; i < 10; ++i)
        {
            spriteRenderer.color = effect;
            yield return new WaitForSeconds(term);
            spriteRenderer.color = origin;
            yield return new WaitForSeconds(term);
        }

        isColliding = false;
    }
}