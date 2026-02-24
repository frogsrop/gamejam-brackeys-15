using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartingScreenController : MonoBehaviour
{
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button aiGhostButton;
    [SerializeField] private Button directorButton;

    [SerializeField] private string aiGhostText = "A";
    [SerializeField] private string directorText = "B";

    private int _selectedMode;

    void Awake()
    {
        aiGhostButton.onClick.AddListener(OnAIGhostClicked);
        directorButton.onClick.AddListener(OnDirectorClicked);
        playButton.onClick.AddListener(OnPlayClicked);
    }

    void OnAIGhostClicked()
    {
        _selectedMode = 1;
        modeText.text = aiGhostText;
        modeText.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);
    }

    void OnDirectorClicked()
    {
        _selectedMode = 2;
        modeText.text = directorText;
        modeText.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);
    }

    void OnPlayClicked()
    {
        SceneManager.LoadScene(_selectedMode == 1 ? "Alternative" : "frogsrop");
    }
}
