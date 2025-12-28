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
                ShowGameStateUI(" Kazandınız!"); // Süresiz
                break;
            case GameState.Locked:
                ShowGameStateUI("Kapı kilitli.", 1f);
                break;

            case GameState.Fail:
                Time.timeScale = 0f;
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
