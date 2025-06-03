using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelMapController : MonoBehaviour
{
    public Button[] levelButtons; // Seviye düğümleri için butonlar
    private int unlockedLevel; // Oyuncunun ulaştığı en yüksek seviye

    void Start()
    {
        // Oyuncunun ilerlemesini yükle (örneğin, PlayerPrefs)
        unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        // Her seviye düğümünü yapılandır
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelIndex = i + 1;
            levelButtons[i].interactable = (levelIndex <= unlockedLevel);
            int capturedLevel = levelIndex; // Buton için seviye indeksini yakala
            levelButtons[i].onClick.AddListener(() => OnLevelButtonClicked(capturedLevel));
        }
    }

    void OnLevelButtonClicked(int level)
    {
        // Seçilen seviyeyi başlat
        PlayerPrefs.SetInt("CurrentLevel", level);
        SceneManager.LoadScene("GameScene");
    }
}