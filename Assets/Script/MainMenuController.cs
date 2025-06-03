using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public Button settingsButton;
    public Button exitButton;

    void Start()
    {
        // Butonlara tıklama olaylarını bağla
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("LevelMap");
    }

    void OnSettingsButtonClicked()
    {
        Debug.Log("Ayarlar ekranı henüz uygulanmadı.");
        // İleride ayarlar ekranı için bir sahne eklenebilir
    }

    void OnExitButtonClicked()
    {
        Application.Quit();
        Debug.Log("Oyun kapatıldı.");
    }
}