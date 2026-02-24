using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Add to a GameObject in the WinLose scene. Reads GameManager.LastGameWon (set before the scene was loaded)
/// and GameManager.LastScreenshot (optional). Shows win/lose UI and the screenshot if assigned.
/// </summary>
public class WinLoseSceneController : MonoBehaviour
{
    [Tooltip("Optional: show when the player won. Enable this GameObject if LastGameWon is true.")]
    [SerializeField] private GameObject winRoot;
    [Tooltip("Optional: show when the player lost. Enable this GameObject if LastGameWon is false.")]
    [SerializeField] private GameObject loseRoot;
    [Tooltip("Optional: show the screenshot passed from the game (e.g. GhostReveal photo).")]
    [SerializeField] private RawImage screenshotDisplay;
    [Tooltip("Optional: Restart button (top right). If null, one is created at runtime.")]
    [SerializeField] private Button restartButton;

    [SerializeField] private string startingScreenSceneName = "StartingScreen";
    [SerializeField] private string restartButtonText = "Restart";

    void Start()
    {
        bool won = GameManager.LastGameWon;
        if (winRoot != null)
            winRoot.SetActive(won);
        if (loseRoot != null)
            loseRoot.SetActive(!won);

        if (screenshotDisplay != null && GameManager.LastScreenshot != null)
        {
            screenshotDisplay.texture = GameManager.LastScreenshot;
            screenshotDisplay.gameObject.SetActive(true);
        }

        if (restartButton == null)
            restartButton = CreateRestartButton();
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    Button CreateRestartButton()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;

        var go = new GameObject("RestartButton");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(120f, 40f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = restartButtonText;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    void OnRestartClicked()
    {
        SceneManager.LoadScene(startingScreenSceneName);
    }
}
