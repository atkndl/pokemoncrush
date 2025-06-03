using UnityEngine;
using System.Collections;
using UnityEngine.UI; // UI bileşenleri için
using UnityEngine.VFX; // Visual Effect için

public class BoardManager : MonoBehaviour
{
    public LevelData[] levels; // Tüm seviyeler (Inspector'dan atanacak)
    private LevelData currentLevel; // Şu anki seviye
    private int remainingMoves; // Kalan hamle sayısı
    private int obstacleRemaining; // Kalan engel sayısı (şu an 0)
    private int currentLevelIndex; // Şu anki seviye indeksi
    private float timeRemaining; // Kalan zaman
    private int score; // Oyuncu puanı
    private bool gameOver; // Oyun bitiş durumu
    private bool isProcessingSwap; // Swap işlemi sırasında başka işlemlerin çalışmasını engellemek için

    public int boardSize = 7;
    public GameObject[] pokemonPrefabs; // Normal Pokémon'lar
    public GameObject pokeTopuPrefab; // Poké Topu prefab'ı
    public GameObject obstaclePrefab; // Engel prefab'ı (şimdilik kullanılmıyor)
    public Transform boardParent;
    public float gridSpacing = 0.65f;
    private GameObject[,] board;
    private Vector2 boardOffset;

    // Animasyon ve efektler
    public float swapAnimationDuration = 0.3f;
    public float matchFadeDuration = 0.2f;
    public float dropAnimationDuration = 0.4f;
    public float pokeTopiEffectDelay = 0.05f;
    public VisualEffect matchParticleEffect; // Eşleşme için partikül efekti
    public VisualEffect comboParticleEffect; // Kombolar için ek efekt
    public AudioClip matchSound; // Eşleşme sesi
    public AudioClip comboSound; // Kombo sesi
    public AudioClip unlockSound; // Kilit açma sesi (şimdilik kullanılmıyor)
    public AudioClip timeWarningSound; // Zaman uyarısı sesi
    public AudioClip gameOverSound; // Oyun bitti sesi
    public AudioClip loseSound; // 1 yıldız için kaybetme sesi
    public AudioClip winSound; // 2 veya 3 yıldız için kazanma sesi
    private AudioSource audioSource; // Ses efektleri için
    private AudioSource musicSource; // Ana müzik için

    // Kamera titreşim efekti
    private Vector3 originalCameraPosition;
    private float shakeDuration = 0.3f;
    private float shakeMagnitude = 0.1f;

    // UI bileşenleri
    public TMPro.TextMeshProUGUI movesText; // Kalan hamle sayısı
    public TMPro.TextMeshProUGUI scoreText; // Toplam puan
    public TMPro.TextMeshProUGUI timeText; // Kalan süre
    public Image timeProgressRing; // Dairesel süre çemberi
    public Button toggleInfoButton; // Bilgi panelini aç/kapat butonu
    public GameObject infoPanel; // Bilgi paneli
    public TMPro.TextMeshProUGUI gameOverText; // Oyun bitti ekranı
    public GameObject gameOverPanel; // Oyun bittiğinde altta çıkacak panel
    public Image[] starImages; // 3 yıldız için Image dizisi (Inspector'dan atanacak)
    public Sprite activeStarSprite; // Turuncu yıldız görseli
    public Sprite inactiveStarSprite; // Sönük yıldız görseli
    private int starsEarned; // Kazanılan yıldız sayısı

    void Start()
    {
        // Ses efektleri için AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        // Ana müzik için ayrı bir AudioSource
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true; // Müziği döngüye al
        musicSource.Play(); // Oyun başladığında ana müziği çal (müzik dosyasını Inspector'dan atayacaksın)

        currentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1) - 1;
        if (currentLevelIndex < 0 || currentLevelIndex >= levels.Length)
        {
            Debug.LogError("Geçersiz seviye indeksi: " + currentLevelIndex);
            currentLevelIndex = 0;
        }

        currentLevel = levels[currentLevelIndex];
        boardSize = currentLevel.boardSize;
        remainingMoves = currentLevel.moveLimit;
        obstacleRemaining = currentLevel.obstacleCount;
        timeRemaining = currentLevel.timeLimit;
        score = 0;
        gameOver = false;
        isProcessingSwap = false;

        // UI bileşenlerini başlat
        if (toggleInfoButton != null) toggleInfoButton.onClick.AddListener(ToggleInfoPanel);
        if (infoPanel != null) infoPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        UpdateUI();

        Debug.Log("Seviye " + (currentLevelIndex + 1) + " başlatılıyor. Hamle sınırı: " + remainingMoves +
                  ", Süre: " + timeRemaining + "s, Engeller: " + obstacleRemaining);

        board = new GameObject[boardSize, boardSize];
        boardOffset = new Vector2(-((boardSize - 1) * gridSpacing) / 2, -((boardSize - 1) * gridSpacing) / 2);
        originalCameraPosition = Camera.main.transform.position;

        GenerateBoard();
        StartCoroutine(AutoMatchCheck());
        StartCoroutine(TimeCountdown());
    }

    void UpdateUI()
    {
        if (movesText != null) movesText.text = " " + remainingMoves;
        if (scoreText != null) scoreText.text = " " + score;
        if (timeText != null) timeText.text = Mathf.Ceil(timeRemaining).ToString("F0") + "s";
        if (timeProgressRing != null)
        {
            float progress = timeRemaining / currentLevel.timeLimit;
            timeProgressRing.fillAmount = progress;
            if (timeRemaining <= 10f && timeProgressRing.color != Color.red)
            {
                timeProgressRing.color = Color.red;
            }
            else if (timeRemaining > 10f && timeProgressRing.color != Color.white)
            {
                timeProgressRing.color = Color.white;
            }
        }
        if (gameOverText != null) gameOverText.gameObject.SetActive(gameOver);
        if (gameOver && gameOverText != null)
        {
            gameOverText.text = "Game Over!\nFinal Score: " + score;
            gameOverPanel.SetActive(true);
            UpdateStars();
        }
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void UpdateStars()
    {
        if (score >= currentLevel.threeStarScoreThreshold)
            starsEarned = 3;
        else if (score >= currentLevel.twoStarScoreThreshold)
            starsEarned = 2;
        else if (score >= currentLevel.oneStarScoreThreshold)
            starsEarned = 1;
        else
            starsEarned = 0;

        if (starImages != null && starImages.Length == 3)
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].sprite = i < starsEarned ? activeStarSprite : inactiveStarSprite;
                }
                else
                {
                    Debug.LogWarning($"starImages[{i}] null, lütfen Inspector'dan atayın!");
                }
            }
        }

        Debug.Log($"Seviye {currentLevelIndex + 1} bitti! Puan: {score}, Kazanılan yıldız: {starsEarned}");
    }

    void ToggleInfoPanel()
    {
        if (infoPanel != null)
        {
            bool isActive = infoPanel.activeSelf;
            infoPanel.SetActive(!isActive);
            audioSource.PlayOneShot(matchSound, 0.7f); // Ses düzeyi 0.7
        }
    }

    void GenerateBoard()
    {
        bool validBoard = false;
        int maxAttempts = 200;
        int attempts = 0;

        while (!validBoard && attempts < maxAttempts)
        {
            validBoard = true;
            ClearBoard();
            for (int x = 0; x < boardSize; x++)
            {
                for (int y = 0; y < boardSize; y++)
                {
                    SpawnPokemon(x, y);
                }
            }

            for (int i = 0; i < currentLevel.obstacleCount; i++)
            {
                int x = currentLevel.obstaclePositionsX[i];
                int y = currentLevel.obstaclePositionsY[i];
                if (x >= 0 && x < boardSize && y >= 0 && y < boardSize && board[x, y] != null)
                {
                    GameObject pokemon = board[x, y];
                    Vector2 position = new Vector2(x * gridSpacing, y * gridSpacing) + boardOffset;
                    GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity, boardParent);
                    obstacle.name = "Obstacle_" + x + "_" + y;
                    Obstacle obstacleScript = obstacle.GetComponent<Obstacle>();
                    if (obstacleScript != null)
                    {
                        obstacleScript.SetHiddenPokemon(pokemon);
                        obstacleScript.SetTransparent(true);
                        board[x, y] = obstacle;
                        Destroy(pokemon);
                    }
                    else
                    {
                        Debug.LogError("Obstacle prefab'ında Obstacle scripti bulunamadı!");
                        Destroy(obstacle);
                    }
                }
            }

            for (int i = 0; i < currentLevel.lockedPokemonCount; i++)
            {
                int x = currentLevel.lockedPokemonPositionsX[i];
                int y = currentLevel.lockedPokemonPositionsY[i];
                if (x >= 0 && x < boardSize && y >= 0 && y < boardSize && board[x, y] != null && !IsObstacle(x, y))
                {
                    PokemonPiece piece = board[x, y].GetComponent<PokemonPiece>();
                    if (piece != null)
                    {
                        piece.isLocked = true;
                    }
                }
            }

            if (HasMatch())
            {
                validBoard = false;
                attempts++;
            }
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Tahta oluşturulamadı, eşleşmeler mevcut!");
        }
    }

    void SpawnPokemon(int x, int y)
    {
        if (board[x, y] != null) return;

        int randomIndex = Random.Range(0, pokemonPrefabs.Length);
        GameObject pokemonPrefab = pokemonPrefabs[randomIndex];
        Vector2 spawnPosition = new Vector2(x * gridSpacing, y * gridSpacing) + boardOffset;
        GameObject pokemon = Instantiate(pokemonPrefab, spawnPosition, Quaternion.identity, boardParent);
        pokemon.name = pokemonPrefab.name + "_" + x + "_" + y;
        board[x, y] = pokemon;
    }

    void SpawnPokeTopi(int x, int y, string pokemonType)
    {
        if (board[x, y] != null) Destroy(board[x, y]);

        Vector2 spawnPosition = new Vector2(x * gridSpacing, y * gridSpacing) + boardOffset;
        GameObject pokeTopi = Instantiate(pokeTopuPrefab, spawnPosition, Quaternion.identity, boardParent);
        PokeTopi pokeTopiScript = pokeTopi.GetComponent<PokeTopi>();
        if (pokeTopiScript != null) pokeTopiScript.SetPokemonType(pokemonType);
        pokeTopi.name = "PokeTopi_" + pokemonType + "_" + x + "_" + y;
        board[x, y] = pokeTopi;
    }

    bool HasMatch()
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] == null || IsObstacle(x, y)) continue;

                if (x <= boardSize - 3)
                {
                    if (CheckSamePokemonType(board[x, y], board[x + 1, y], board[x + 2, y]))
                        return true;
                }
                if (y <= boardSize - 3)
                {
                    if (CheckSamePokemonType(board[x, y], board[x, y + 1], board[x, y + 2]))
                        return true;
                }
                if (x <= boardSize - 4)
                {
                    if (CheckSamePokemonType(board[x, y], board[x + 1, y], board[x + 2, y], board[x + 3, y]))
                        return true;
                }
                if (y <= boardSize - 4)
                {
                    if (CheckSamePokemonType(board[x, y], board[x, y + 1], board[x, y + 2], board[x, y + 3]))
                        return true;
                }
                if (x <= boardSize - 2 && y <= boardSize - 2)
                {
                    if (CheckSamePokemonType(board[x, y], board[x + 1, y], board[x, y + 1], board[x + 1, y + 1]))
                        return true;
                }
            }
        }
        return false;
    }

    public bool FindMatch(out int[] matchCoords, out int matchCount)
    {
        matchCoords = new int[8];
        matchCount = 0;

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] == null || IsObstacle(x, y)) continue;

                if (x <= boardSize - 4 && CheckSamePokemonType(board[x, y], board[x + 1, y], board[x + 2, y], board[x + 3, y]))
                {
                    matchCoords[0] = x; matchCoords[1] = y;
                    matchCoords[2] = x + 1; matchCoords[3] = y;
                    matchCoords[4] = x + 2; matchCoords[5] = y;
                    matchCoords[6] = x + 3; matchCoords[7] = y;
                    matchCount = 4;
                    return true;
                }
                if (y <= boardSize - 4 && CheckSamePokemonType(board[x, y], board[x, y + 1], board[x, y + 2], board[x, y + 3]))
                {
                    matchCoords[0] = x; matchCoords[1] = y;
                    matchCoords[2] = x; matchCoords[3] = y + 1;
                    matchCoords[4] = x; matchCoords[5] = y + 2;
                    matchCoords[6] = x; matchCoords[7] = y + 3;
                    matchCount = 4;
                    return true;
                }
                if (x <= boardSize - 2 && y <= boardSize - 2 && CheckSamePokemonType(board[x, y], board[x + 1, y], board[x, y + 1], board[x + 1, y + 1]))
                {
                    matchCoords[0] = x; matchCoords[1] = y;
                    matchCoords[2] = x + 1; matchCoords[3] = y;
                    matchCoords[4] = x; matchCoords[5] = y + 1;
                    matchCoords[6] = x + 1; matchCoords[7] = y + 1;
                    matchCount = 4;
                    return true;
                }
            }
        }

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] == null || IsObstacle(x, y)) continue;

                if (x <= boardSize - 3 && CheckSamePokemonType(board[x, y], board[x + 1, y], board[x + 2, y]))
                {
                    matchCoords[0] = x; matchCoords[1] = y;
                    matchCoords[2] = x + 1; matchCoords[3] = y;
                    matchCoords[4] = x + 2; matchCoords[5] = y;
                    matchCount = 3;
                    return true;
                }
                if (y <= boardSize - 3 && CheckSamePokemonType(board[x, y], board[x, y + 1], board[x, y + 2]))
                {
                    matchCoords[0] = x; matchCoords[1] = y;
                    matchCoords[2] = x; matchCoords[3] = y + 1;
                    matchCoords[4] = x; matchCoords[5] = y + 2;
                    matchCount = 3;
                    return true;
                }
            }
        }

        return false;
    }

    bool CheckSamePokemonType(GameObject pokemon1, GameObject pokemon2)
    {
        if (pokemon1 == null || pokemon2 == null || IsObstacleFromPosition(pokemon1) || IsObstacleFromPosition(pokemon2)) return false;
        return GetPokemonType(pokemon1) == GetPokemonType(pokemon2);
    }

    bool CheckSamePokemonType(GameObject pokemon1, GameObject pokemon2, GameObject pokemon3)
    {
        if (pokemon1 == null || pokemon2 == null || pokemon3 == null ||
            IsObstacleFromPosition(pokemon1) || IsObstacleFromPosition(pokemon2) || IsObstacleFromPosition(pokemon3)) return false;
        string type1 = GetPokemonType(pokemon1);
        return type1 == GetPokemonType(pokemon2) && type1 == GetPokemonType(pokemon3);
    }

    bool CheckSamePokemonType(GameObject pokemon1, GameObject pokemon2, GameObject pokemon3, GameObject pokemon4)
    {
        if (pokemon1 == null || pokemon2 == null || pokemon3 == null || pokemon4 == null ||
            IsObstacleFromPosition(pokemon1) || IsObstacleFromPosition(pokemon2) || IsObstacleFromPosition(pokemon3) || IsObstacleFromPosition(pokemon4)) return false;
        string type1 = GetPokemonType(pokemon1);
        return type1 == GetPokemonType(pokemon2) && type1 == GetPokemonType(pokemon3) && type1 == GetPokemonType(pokemon4);
    }

    string GetPokemonType(GameObject pokemon)
    {
        if (pokemon == null) return "Unknown";
        if (IsObstacleFromPosition(pokemon))
        {
            Obstacle obstacle = pokemon.GetComponent<Obstacle>();
            if (obstacle != null && obstacle.HasHiddenPokemon())
            {
                return GetPokemonType(obstacle.GetHiddenPokemon());
            }
            return "Obstacle";
        }
        PokeTopi pokeTopiScript = pokemon.GetComponent<PokeTopi>();
        if (pokeTopiScript != null) return pokeTopiScript.GetPokemonType();

        string fullName = pokemon.name;
        if (fullName.Contains("(Clone)")) fullName = fullName.Substring(0, fullName.IndexOf("(Clone)"));
        if (fullName.Contains("_")) fullName = fullName.Substring(0, fullName.IndexOf("_"));
        return fullName;
    }

    bool IsObstacle(int x, int y)
    {
        if (board[x, y] == null) return false;
        return board[x, y].name.StartsWith("Obstacle");
    }

    bool IsObstacleFromPosition(GameObject obj)
    {
        return obj != null && obj.name.StartsWith("Obstacle");
    }

    void ClearBoard()
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] != null)
                {
                    Destroy(board[x, y]);
                    board[x, y] = null;
                }
            }
        }
    }

    public bool TrySwap(int x1, int y1, int x2, int y2)
    {
        if (gameOver || isProcessingSwap || x1 < 0 || x1 >= boardSize || y1 < 0 || y1 >= boardSize ||
            x2 < 0 || x2 >= boardSize || y2 < 0 || y2 >= boardSize ||
            IsObstacle(x1, y1) || IsObstacle(x2, y2))
        {
            Debug.LogWarning("Invalid swap coordinates, obstacle, game over, or swap in progress!");
            return false;
        }

        GameObject pokemon1 = board[x1, y1];
        GameObject pokemon2 = board[x2, y2];

        if (pokemon1 == null || pokemon2 == null)
        {
            Debug.LogWarning("Cannot swap with empty cell!");
            return false;
        }

        isProcessingSwap = true;

        board[x1, y1] = pokemon2;
        board[x2, y2] = pokemon1;

        Vector2 pos1 = new Vector2(x1 * gridSpacing, y1 * gridSpacing) + boardOffset;
        Vector2 pos2 = new Vector2(x2 * gridSpacing, y2 * gridSpacing) + boardOffset;

        StartCoroutine(SwapAnimation(pokemon1, pos2, pokemon2, pos1, () =>
        {
            bool isValidMove = false;
            PokeTopi pokeTopi1 = pokemon1.GetComponent<PokeTopi>();
            PokeTopi pokeTopi2 = pokemon2.GetComponent<PokeTopi>();

            if ((pokeTopi1 != null && pokemon2.GetComponent<PokemonPiece>() != null) ||
                (pokeTopi2 != null && pokemon1.GetComponent<PokemonPiece>() != null))
            {
                Debug.Log("Poketopu ile swap tespit edildi!");
                GameObject pokeTopiObj = pokeTopi1 != null ? pokemon1 : pokemon2;
                string targetPokemonType = pokeTopi1 != null ? GetPokemonType(pokemon2) : GetPokemonType(pokemon1);
                int pokeTopiX = pokeTopi1 != null ? x1 : x2;
                int pokeTopiY = pokeTopi1 != null ? y1 : y2;
                StartCoroutine(ActivatePokeTopiEffect(targetPokemonType, pokeTopiObj, pokeTopiX, pokeTopiY, x1, y1, x2, y2));
                isValidMove = true;
            }
            else if (HasMatch())
            {
                Debug.Log("Eşleşme bulundu! Hamle geçerli.");
                StartCoroutine(HandleMatches());
                isValidMove = true;
            }
            else
            {
                Debug.Log("Geçersiz hamle, eşleşme oluşmadı! Hamle hakkı düşmedi.");
                board[x1, y1] = pokemon1;
                board[x2, y2] = pokemon2;
                StartCoroutine(SwapAnimation(pokemon1, pos1, pokemon2, pos2, () =>
                {
                    isProcessingSwap = false;
                }));
                PokemonPiece piece1 = pokemon1.GetComponent<PokemonPiece>();
                PokemonPiece piece2 = pokemon2.GetComponent<PokemonPiece>();
                if (piece1 != null) piece1.ResetScale();
                if (piece2 != null) piece2.ResetScale();
                return;
            }

            if (isValidMove)
            {
                remainingMoves--;
                Debug.Log($"Hamle hakkı düşürüldü: {remainingMoves} hamle kaldı.");
                UpdateUI();
                CheckGameOver();
            }
            isProcessingSwap = false;
        }));

        return true;
    }

    private void CheckGameOver()
    {
        if (remainingMoves <= 0 || timeRemaining <= 0)
        {
            StartCoroutine(DelayedEndGame());
        }
    }

    private IEnumerator DelayedEndGame()
    {
        yield return new WaitForSeconds(matchFadeDuration + dropAnimationDuration + 0.1f);
        EndGame();
    }

    private void EndGame()
{
    gameOver = true;

    // Ana müziği durdur
    if (musicSource != null && musicSource.isPlaying)
    {
        musicSource.Stop();
        Debug.Log("Ana müzik durduruldu.");
    }
    else
    {
        Debug.LogWarning("musicSource null veya zaten durmuş!");
    }

    // Game Over sesini çal
    if (gameOverSound != null)
    {
        audioSource.PlayOneShot(gameOverSound, 0.8f);
        Debug.Log("Game Over sesi çalınıyor.");
    }

    // Yıldızları güncelle (bu zaten UI ve yıldızları gösteriyor)
    UpdateUI();

    // Game Over sesinin bitmesini bekleyip ardından Win/Lose sesini çal
    StartCoroutine(PlayResultSoundAfterGameOver());
}

private IEnumerator PlayResultSoundAfterGameOver()
{
    // Game Over sesinin uzunluğunu bekle (eğer null ise 0 saniye varsayalım)
    float delay = gameOverSound != null ? gameOverSound.length : 0f;
    yield return new WaitForSeconds(delay + 0.1f); // Küçük bir ek süre

    // Yıldız sayısına göre kazanma veya kaybetme sesi çal
    if (starsEarned == 1 && loseSound != null)
    {
        audioSource.PlayOneShot(loseSound, 0.8f);
        Debug.Log("Lose sesi çalınıyor.");
    }
    else if (starsEarned >= 2 && winSound != null)
    {
        audioSource.PlayOneShot(winSound, 0.8f);
        Debug.Log("Win sesi çalınıyor.");
    }
}

    private IEnumerator SwapAnimation(GameObject obj1, Vector2 target1, GameObject obj2, Vector2 target2, System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        Vector2 start1 = obj1.transform.position;
        Vector2 start2 = obj2.transform.position;

        while (elapsedTime < swapAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / swapAnimationDuration;
            obj1.transform.position = Vector2.Lerp(start1, target1, t);
            obj2.transform.position = Vector2.Lerp(start2, target2, t);
            yield return null;
        }

        obj1.transform.position = target1;
        obj2.transform.position = target2;
        onComplete?.Invoke();
    }

    private IEnumerator HandleMatches()
    {
        if (gameOver) yield break;

        int[] matchCoords;
        int matchCount;
        int comboCount = 0;

        while (FindMatch(out matchCoords, out matchCount))
        {
            string matchedPokemonType = GetPokemonType(board[matchCoords[0], matchCoords[1]]);
            comboCount++;

            for (int i = 0; i < matchCount; i++)
            {
                int x = matchCoords[i * 2];
                int y = matchCoords[i * 2 + 1];
                if (board[x, y] == null || IsObstacle(x, y)) continue;

                if (matchParticleEffect != null)
                {
                    VisualEffect vfx = Instantiate(matchParticleEffect, board[x, y].transform.position, Quaternion.identity);
                    vfx.Play();
                    Destroy(vfx, 1f);
                }
                audioSource.PlayOneShot(matchSound, 0.7f); // Eşleşme sesi, ses düzeyi 0.7
                StartCoroutine(DestroyWithEffect(board[x, y]));
                board[x, y] = null;
            }

            score += matchCount == 3 ? 10 : 20;
            if (comboCount > 1 && comboParticleEffect != null)
            {
                VisualEffect comboVfx = Instantiate(comboParticleEffect, board[matchCoords[0], matchCoords[1]].transform.position, Quaternion.identity);
                comboVfx.Play();
                Destroy(comboVfx, 1f);
                audioSource.PlayOneShot(comboSound, 0.7f); // Kombo sesi, ses düzeyi 0.7
                score += 5 * comboCount;
                Debug.Log("Combo! +" + (5 * comboCount) + " bonus puan");
            }
            UpdateUI();

            yield return new WaitForSeconds(matchFadeDuration + 0.1f);

            yield return StartCoroutine(FillBoardWithDropAnimation());

            if (matchCount == 4)
            {
                int pokeTopiX = matchCoords[0];
                int pokeTopiY = matchCoords[1];
                SpawnPokeTopi(pokeTopiX, pokeTopiY, matchedPokemonType);
                Debug.Log("Poké Topu spawn edildi!");
            }

            if (remainingMoves <= 0 || timeRemaining <= 0)
            {
                CheckGameOver();
                yield break;
            }
        }

        if (comboCount > 1)
        {
            Debug.Log("Kombolar tamamlandı! Toplam puan: " + score);
        }
    }

    private IEnumerator AutoMatchCheck()
    {
        while (!gameOver)
        {
            if (HasMatch() && !isProcessingSwap)
            {
                Debug.Log("Otomatik eşleşme tespit edildi!");
                StartCoroutine(HandleMatches());
                yield return new WaitForSeconds(matchFadeDuration + dropAnimationDuration + 0.1f);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator TimeCountdown()
    {
        while (timeRemaining > 0 && !gameOver)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUI();
            if (timeRemaining <= 10f && !audioSource.isPlaying) audioSource.PlayOneShot(timeWarningSound, 0.6f); // Zaman uyarısı, ses düzeyi 0.6
            yield return null;
        }
        if (!gameOver) CheckGameOver();
    }

    private IEnumerator DestroyWithEffect(GameObject obj)
    {
        float elapsedTime = 0f;
        Vector3 originalScale = obj.transform.localScale;

        while (elapsedTime < matchFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / matchFadeDuration;
            obj.transform.localScale = Vector2.Lerp(originalScale, Vector2.zero, t);
            yield return null;
        }

        obj.transform.localScale = Vector3.one;
        Destroy(obj);
    }

    private IEnumerator FillBoardWithDropAnimation()
    {
        for (int x = 0; x < boardSize; x++)
        {
            int lowestEmptyY = -1;
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] == null)
                {
                    lowestEmptyY = y;
                    break;
                }
            }

            if (lowestEmptyY >= 0)
            {
                for (int y = lowestEmptyY + 1; y < boardSize; y++)
                {
                    if (board[x, y] != null && !IsObstacle(x, y))
                    {
                        Vector2 targetPos = new Vector2(x * gridSpacing, lowestEmptyY * gridSpacing) + boardOffset;
                        StartCoroutine(DropAnimation(board[x, y], targetPos));
                        board[x, lowestEmptyY] = board[x, y];
                        board[x, y] = null;
                        lowestEmptyY++;
                    }
                }
            }
        }

        yield return new WaitForSeconds(dropAnimationDuration);

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] == null)
                {
                    Vector2 spawnPos = new Vector2(x * gridSpacing, (boardSize + 1) * gridSpacing) + boardOffset;
                    SpawnPokemon(x, y);
                    GameObject newPokemon = board[x, y];
                    Vector2 targetPos = new Vector2(x * gridSpacing, y * gridSpacing) + boardOffset;
                    newPokemon.transform.position = spawnPos;
                    StartCoroutine(DropAnimation(newPokemon, targetPos));
                }
            }
        }

        yield return new WaitForSeconds(dropAnimationDuration);
    }

    private IEnumerator DropAnimation(GameObject obj, Vector2 targetPos)
    {
        float elapsedTime = 0f;
        Vector2 startPos = obj.transform.position;

        while (elapsedTime < dropAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropAnimationDuration;
            obj.transform.position = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        obj.transform.position = targetPos;
    }

    private IEnumerator ActivatePokeTopiEffect(string targetPokemonType, GameObject pokeTopiObj, int pokeTopiX, int pokeTopiY, int x1, int y1, int x2, int y2)
    {
        if (gameOver) yield break;

        Debug.Log("Poketopu aktif oldu! Hedef Pokémon: " + targetPokemonType);

        int targetX = pokeTopiObj == board[x1, y1] ? x1 : x2;
        int targetY = pokeTopiObj == board[x1, y1] ? y1 : y2;

        yield return StartCoroutine(PokeTopiFlashEffect(pokeTopiObj));
        StartCoroutine(ShakeCamera());

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                if (board[x, y] != null && !IsObstacle(x, y) && GetPokemonType(board[x, y]) == targetPokemonType && (x != targetX || y != targetY))
                {
                    if (matchParticleEffect != null)
                    {
                        VisualEffect vfx = Instantiate(matchParticleEffect, board[x, y].transform.position, Quaternion.identity);
                        vfx.Play();
                        Destroy(vfx, 1f);
                    }
                    audioSource.PlayOneShot(matchSound, 0.7f);
                    StartCoroutine(DestroyWithEffect(board[x, y]));
                    board[x, y] = null;
                    yield return new WaitForSeconds(pokeTopiEffectDelay);
                }
            }
        }

        board[targetX, targetY] = pokeTopiObj;

        yield return new WaitForSeconds(matchFadeDuration + 0.1f);
        yield return StartCoroutine(FillBoardWithDropAnimation());

        if (HasMatch())
        {
            StartCoroutine(HandleMatches());
        }
    }

    private IEnumerator PokeTopiFlashEffect(GameObject pokeTopiObj)
    {
        float elapsedTime = 0f;
        float flashDuration = 0.5f;
        Vector3 originalScale = pokeTopiObj.transform.localScale;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flashDuration;
            float scale = Mathf.Lerp(1f, 1.5f, Mathf.PingPong(t * 2, 1));
            pokeTopiObj.transform.localScale = originalScale * scale;
            yield return null;
        }

        pokeTopiObj.transform.localScale = originalScale;
    }

    private IEnumerator ShakeCamera()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentComplete = elapsedTime / shakeDuration;
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);

            float x = Random.value * 2.0f - 1.0f;
            float y = Random.value * 2.0f - 1.0f;
            x *= shakeMagnitude * damper;
            y *= shakeMagnitude * damper;

            Camera.main.transform.position = originalCameraPosition + new Vector3(x, y, 0);
            yield return null;
        }

        Camera.main.transform.position = originalCameraPosition;
    }

    public float GetGridSpacing()
    {
        return gridSpacing;
    }

    public Vector2 GetBoardOffset()
    {
        return boardOffset;
    }
}