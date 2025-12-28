using UnityEngine;
using TMPro;
using System.Collections; // Coroutine için

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        None,
        Playing,
        HasItem,
        Locked,
        Win,
        Fail,
        Paused
    }

    public GameState CurrentState { get; private set; } = GameState.None;

    [Header("UI References")]
    [SerializeField] private GameObject gameStatePanel;
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject restartPanel;

    [Header("Game Objects")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private GameObject artifactObject;
    [SerializeField] private GameObject doorObject;


    private Coroutine messageCoroutine;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        FindUIReferences();
        SetGameState(GameState.None);
        Time.timeScale = 0f;

        // Sahne ilk yüklendiğinde start menüsünü tekrar göster
        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);

        if (restartPanel != null)
            restartPanel.SetActive(false);

        if (gameStatePanel != null)
            gameStatePanel.SetActive(false);
    }

    public void OnStartButtonPressed()
    {
        StartGame();
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);
    }

    public void OnRestartButtonPressed()
    {
        // Oyun zamanını sıfırla
        Time.timeScale = 0f;

        // GameState sıfırla
        SetGameState(GameState.None, true);

        // Oyuncuyu sıfırla
        if (player != null)
            player.ResetPlayer();

        // Objeleri tekrar görünür yap
        if (artifactObject != null)
            artifactObject.SetActive(true);

        if (doorObject != null)
            doorObject.SetActive(true);

        // Panelleri ayarla
        if (restartPanel != null)
            restartPanel.SetActive(false);

        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);

        if (gameStatePanel != null)
            gameStatePanel.SetActive(false);
    }



    public void StartGame()
    {
        SetGameState(GameState.Playing);
    }

    public void SetGameState(GameState newState, bool force = false)
    {
        if (!force && CurrentState == newState)
            return;

        CurrentState = newState;
        Debug.Log("Game State Changed: " + CurrentState);

        switch (CurrentState)
        {

            case GameState.Playing:
                Time.timeScale = 1f;
                HideGameStateUI();
                break;

            case GameState.HasItem:
                ShowGameStateUI(" Ürün alındı.", 2f); // 2 saniye göster
                break;

            case GameState.Win:
                Time.timeScale = 0f;
                restartPanel.SetActive(true);
                ShowGameStateUI(" Kazandınız!"); // Süresiz
                break;
            case GameState.Locked:
                ShowGameStateUI("Kapı kilitli.", 1f);
                break;

            case GameState.Fail:
                Time.timeScale = 0f;
                restartPanel.SetActive(true);
                ShowGameStateUI(" Yakalandınız!"); // Süresiz
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                ShowGameStateUI(" Oyun Duraklatıldı");
                break;
        }
    }

    private void ShowGameStateUI(string message, float duration = -1f)
    {
        if (gameStatePanel != null && gameStateText != null)
        {
            // Önceki Coroutine varsa iptal et
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            gameStateText.text = message;
            gameStatePanel.SetActive(true);

            // Eğer süre pozitifse gizlemeyi başlat
            if (duration > 0)
            {
                messageCoroutine = StartCoroutine(HideAfterSeconds(duration));
            }
        }
    }

    private void FindUIReferences()
    {
        if (gameStatePanel == null)
            gameStatePanel = GameObject.Find("GameStatePanel");

        if (gameStateText == null)
            gameStateText = GameObject.Find("GameStateText")?.GetComponent<TextMeshProUGUI>();

        if (startMenuPanel == null)
            startMenuPanel = GameObject.Find("Start");

        if (restartPanel == null)
            restartPanel = GameObject.Find("Restart");
    }


    private IEnumerator HideAfterSeconds(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // timeScale'den etkilenmesin
        HideGameStateUI();
    }

    private void HideGameStateUI()
    {
        if (gameStatePanel != null)
        {
            gameStatePanel.SetActive(false);
        }
    }

    public bool IsGameOver()
    {
        return CurrentState == GameState.Win || CurrentState == GameState.Fail;
    }
}
