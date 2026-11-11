using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public string MainMenu;

    [Header("Stage Settings")]
    public float SurviveTime = 60.0f; 
    private int playerScore = 0;      
    private bool isBossSpawned = false;
    private bool isGameOver = false;
    public bool IsBossStage { get; private set; } = false; 
    private bool IsGameStarted = false;
    public int DeadEnemyCount = 0;

    [Header("Cached References")]
    public GameObject EnemySpawner;   
    public GameObject BossPrefab;     

    [Header("UI Elements")]
    public TextMeshProUGUI TimerText; 
    public TextMeshProUGUI ScoreText; 
    public TextMeshProUGUI CountdownText;
    public TextMeshProUGUI LifeCountText;

    [Header("End Game UI Canvases")]
    public GameObject GameOverCanvas;
    public GameObject VictoryCanvas;

    [Header("Main Menu Button")]
    public Button GameOverMainMenuButton;
    public Button VictoryMainMenuButton;

    [Header("Retry Button")]
    public Button GameOverRetryButton;
    public Button VictoryRetryButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();

        GameOverMainMenuButton.onClick.AddListener(GoToMainMenu);
        VictoryMainMenuButton.onClick.AddListener(GoToMainMenu);

        if (EnemySpawner != null)
        {
            EnemySpawner.SetActive(false);
        }

        GameOverRetryButton.onClick.AddListener(Retry);
        VictoryRetryButton.onClick.AddListener(Retry);

        StartCoroutine(CountdownRoutine());
    }

    void Update()
    {
        if (isGameOver) return;

        if (IsGameStarted == false)
        {
            return;
        }

        if (SurviveTime > 0)
        {
            SurviveTime -= Time.deltaTime;

            if (TimerText != null)
            {
                TimerText.text = Mathf.Max(0, (int)SurviveTime) + "s";
            }
        }
        else if (isBossSpawned == false)
        {
            SpawnBossStage();
        }
    }

    public void UpdateLife(int currentLife)
    {
        if (LifeCountText != null)
        {
            LifeCountText.text = currentLife.ToString("  : " + currentLife);
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;

        playerScore += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (ScoreText != null)
        {
            ScoreText.text = playerScore.ToString("N0"); 
        }
    }

    private void SpawnBossStage()
    {
        isBossSpawned = true;
        IsBossStage = true; 

        if (TimerText != null)
        {
            TimerText.text = "BOSS";
            TimerText.color = Color.red;
        }

        if (EnemySpawner != null)
        {
            EnemySpawner.SetActive(false);
        }

        GameObject[] remainingEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in remainingEnemies)
        {
            Destroy(enemy);
        }

        if (BossPrefab != null)
        {
            Vector3 bossSpawnPos = new Vector3(0f, 4.0f, 0f);
            Instantiate(BossPrefab, bossSpawnPos, Quaternion.identity);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("GAME OVER");

        if (EnemySpawner != null)
        {
            EnemySpawner.SetActive(false);
        }

        StartCoroutine(GameOverDelay());
    }

    private IEnumerator GameOverDelay()
    {
        yield return new WaitForSeconds(2);

        if (GameOverCanvas != null)
        {
            GameOverCanvas.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void TriggerVictoryClear()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("VICTORY");

        if (EnemySpawner != null)
        {
            EnemySpawner.SetActive(false);
        }

        StartCoroutine(VictoryDelay());
    }

    private IEnumerator VictoryDelay()
    {
        yield return new WaitForSeconds(2);

        if (VictoryCanvas != null)
        {
            VictoryCanvas.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void GoToMainMenu()
    {
        Debug.Log("GoToMainMenu");

        Time.timeScale = 1f;

        SceneManager.LoadScene(MainMenu);
    }

    public void Retry()
    {
        Debug.Log("Retry");

        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator CountdownRoutine()
    {
        CountdownText.text = "3";
        yield return new WaitForSeconds(1f); ;

        CountdownText.text = "2";
        yield return new WaitForSeconds(1f);

        CountdownText.text = "1";
        yield return new WaitForSeconds(1f);

        CountdownText.text = "START";
        IsGameStarted = true;
        if (EnemySpawner != null)
        {
            EnemySpawner.SetActive(true);
        }
        yield return new WaitForSeconds(0.5f);

        CountdownText.text = "";
        yield return new WaitForSeconds(1f);
    }
}