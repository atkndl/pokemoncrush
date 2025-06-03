using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData", order = 1)] // Yeni LevelData oluşturma menüsü
public class LevelData : ScriptableObject // ScriptableObject sınıfından türetiliyor
{
    public int levelNumber; // Seviye numarası
    public int moveLimit; // Hamle sınırı
    public int boardSize; // Tahta boyutu
    public int lockedPokemonCount; // Kilitli Pokémon sayısı
    public int[] lockedPokemonPositionsX; // Kilitli Pokémon X koordinatları
    public int[] lockedPokemonPositionsY; // Kilitli Pokémon Y koordinatları
    public int obstacleCount; // Engel sayısı
    public int[] obstaclePositionsX; // Engel X koordinatları
    public int[] obstaclePositionsY; // Engel Y koordinatları
    [Range(30f, 300f)] // Süre sınırı (30-300 saniye)
    public float timeLimit = 120f; // Seviye süre limiti

    // Yeni eklenen yıldız eşikleri
    public int oneStarScoreThreshold = 50;   // 1 yıldız için minimum puan
    public int twoStarScoreThreshold = 100;  // 2 yıldız için minimum puan
    public int threeStarScoreThreshold = 150; // 3 yıldız için minimum puan
}