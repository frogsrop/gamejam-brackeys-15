using UnityEngine;
using UnityEngine.UI;

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
    }
}
