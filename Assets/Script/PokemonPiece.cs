using UnityEngine;

public class PokemonPiece : MonoBehaviour
{
    private bool isSelected = false;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private BoardManager boardScript;
    private Vector2Int boardPosition;
    private Vector3 originalScale;
    public bool isLocked = false; // Kilitli durumu
    private SpriteRenderer spriteRenderer;
    public Sprite lockSprite; // Kilit görseli (Inspector'dan atanacak)

    private void Start()
    {
        boardScript = FindObjectOfType<BoardManager>();
        if (boardScript == null)
        {
            Debug.LogError("BoardManager bulunamadı! Lütfen sahnede bir BoardManager nesnesi olduğundan emin olun.");
        }
        originalScale = transform.localScale;
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateLockVisual(); // Kilit görselini güncelle

        PokeTopi pokeTopi = GetComponent<PokeTopi>();
        if (pokeTopi != null)
        {
            Debug.Log("Poketopu tespit edildi: " + gameObject.name + " sürükleme etkin.");
        }
        else
        {
            Debug.Log("Normal Pokémon tespit edildi: " + gameObject.name);
        }
    }

    private void UpdateLockVisual()
    {
        if (isLocked && lockSprite != null && spriteRenderer != null)
        {
            GameObject lockObj = new GameObject("Lock");
            lockObj.transform.SetParent(transform, false);
            SpriteRenderer lockRenderer = lockObj.AddComponent<SpriteRenderer>();
            lockRenderer.sprite = lockSprite;
            lockRenderer.sortingOrder = spriteRenderer.sortingOrder + 1; // Üstte kalsın
        }
    }

    private void OnMouseDown()
    {
        if (boardScript == null || isLocked) return; // Kilitliyse seçimi engelle
        isSelected = true;
        startPosition = transform.position;
        boardPosition = GetBoardPosition();
        transform.localScale = originalScale * 1.1f; // Sadece hafif büyüme
        Debug.Log("Nesne seçildi: " + gameObject.name + " at (" + boardPosition.x + ", " + boardPosition.y + ")");
    }

    private void OnMouseDrag()
    {
        if (isSelected)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = Vector2.Lerp(transform.position, mousePosition, 0.5f);
            Debug.Log("Nesne sürükleniyor: " + gameObject.name);
        }
    }

    private void OnMouseUp()
    {
        if (!isSelected || boardScript == null) return;

        isSelected = false;
        transform.localScale = originalScale; // Seçim bittiğinde orijinal boyuta dön

        targetPosition = transform.position;
        Vector2Int targetBoardPos = CalculateBoardPosition(targetPosition);

        Debug.Log("Nesne bırakıldı: " + gameObject.name + " hedef pozisyon (" + targetBoardPos.x + ", " + targetBoardPos.y + ")");

        if (targetBoardPos.x < 0 || targetBoardPos.x >= boardScript.boardSize ||
            targetBoardPos.y < 0 || targetBoardPos.y >= boardScript.boardSize)
        {
            transform.position = startPosition;
            Debug.Log("Geçersiz pozisyon, orijinal konuma geri dönüldü.");
            return;
        }

        int manhattanDistance = Mathf.Abs(targetBoardPos.x - boardPosition.x) +
                               Mathf.Abs(targetBoardPos.y - boardPosition.y);

        if (manhattanDistance == 1)
        {
            Debug.Log("Swap denendi: (" + boardPosition.x + ", " + boardPosition.y + ") ile (" + targetBoardPos.x + ", " + targetBoardPos.y + ")");
            boardScript.TrySwap(boardPosition.x, boardPosition.y, targetBoardPos.x, targetBoardPos.y);
        }
        else
        {
            transform.position = startPosition;
            Debug.Log("Geçersiz mesafe, orijinal konuma geri dönüldü.");
        }
    }

    public void ResetScale()
    {
        transform.localScale = originalScale;
    }

    public void Unlock()
    {
        isLocked = false;
        // Kilit görselini kaldır
        Transform lockTransform = transform.Find("Lock");
        if (lockTransform != null) Destroy(lockTransform.gameObject);
        Debug.Log("Pokémon kilidi açıldı: " + gameObject.name);
    }

    private Vector2Int GetBoardPosition()
    {
        return CalculateBoardPosition(transform.position);
    }

    private Vector2Int CalculateBoardPosition(Vector2 worldPosition)
    {
        if (boardScript == null) return Vector2Int.zero;

        Vector2 boardOffset = boardScript.GetBoardOffset();
        float gridSpacing = boardScript.GetGridSpacing();

        float normalizedX = worldPosition.x - boardOffset.x;
        float normalizedY = worldPosition.y - boardOffset.y;

        int boardX = Mathf.RoundToInt(normalizedX / gridSpacing);
        int boardY = Mathf.RoundToInt(normalizedY / gridSpacing);

        return new Vector2Int(boardX, boardY);
    }
}