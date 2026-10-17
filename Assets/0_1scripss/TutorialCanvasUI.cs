using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialCanvasUI : MonoBehaviour
{
    public static TutorialCanvasUI Instance { get; private set; }
    public static bool BlocksGameplay => Instance != null && Instance.GuideOpen;
    public static bool PointerOverUI => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    public player player;
    public LevelOne level;
    public bool showGuideOnStart = true;
    public GameObject guidePanel, completionPanel;
    public RectTransform guideContent;
    public Text healthText, statusText, contextText, continueText;
    public Button guideButton, continueButton, restartButton;
    public bool GuideOpen => guidePanel != null && guidePanel.activeSelf;
    bool paused;
    float previousTimeScale;
    bool hasStarted;
    void Awake() { Instance = this; }
    void Start()
    {
        guideButton.onClick.AddListener(() => SetGuide(!GuideOpen));
        continueButton.onClick.AddListener(() => SetGuide(false));
        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        guidePanel.SetActive(false);
        completionPanel.SetActive(false);
        SetGuide(showGuideOnStart);
        Refresh();
    }
    void Update()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.hKey.wasPressedThisFrame) SetGuide(!GuideOpen);
            else if (GuideOpen && (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) SetGuide(false);
        }
        Refresh();
    }
    void Refresh()
    {
        if (player == null || level == null) return;
        var canvasSize = ((RectTransform)transform).rect.size;
        guideContent.localScale = Vector3.one * Mathf.Min(1, (canvasSize.x - 32) / 960, (canvasSize.y - 32) / 600);
        healthText.text = "VIDA  " + player.Health + " / " + player.maxHealth;
        string status = player.WeaponName;
        if (player.MinigunRemaining > 0) status += "  " + Mathf.CeilToInt(player.MinigunRemaining) + " s";
        if (player.WeaponName == "AK") status += "  " + Mathf.CeilToInt(player.AKRemaining) + " s";
        if (player.CitrusRemaining > 0) status += "   |   Citrus " + Mathf.CeilToInt(player.CitrusRemaining) + " s";
        if (player.ImmunityRemaining > 0) status += "   |   Inmune " + Mathf.CeilToInt(player.ImmunityRemaining) + " s";
        if (player.DoubleJump) status += "   |   Doble salto " + Mathf.CeilToInt(player.FeatherRemaining) + " s";
        statusText.text = status;
        string context = level.CurrentNotice;
        if (context == null && !GuideOpen && !level.Completed)
        {
            float nearest = 3;
            foreach (var power in FindObjectsByType<PowerUp>())
            {
                float distance = Vector2.Distance(player.transform.position, power.transform.position);
                if (distance < nearest) { nearest = distance; context = power.Title; }
            }
        }
        contextText.text = context ?? "Llega al granero. Recoge los poderes para practicar.";
        completionPanel.SetActive(level.Completed && !GuideOpen);
    }
    public void SetGuide(bool open)
    {
        if (open)
        {
            if (!paused) { previousTimeScale = Time.timeScale; Time.timeScale = 0; paused = true; }
            continueText.text = hasStarted ? "Continuar  [Enter]" : "Empezar  [Enter]";
        }
        else
        {
            if (paused) { Time.timeScale = previousTimeScale; paused = false; }
            hasStarted = true;
        }
        guidePanel.SetActive(open);
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(open ? continueButton.gameObject : null);
    }
    void OnDestroy()
    {
        if (paused) Time.timeScale = previousTimeScale;
        if (Instance == this) Instance = null;
    }
}
