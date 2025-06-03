using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public int hitsToBreak = 1; // Engelin kırılması için gereken eşleşme sayısı
    private int hitsTaken = 0; // Şu ana kadar alınan vuruş sayısı
    private BoardManager boardManager;
    private SpriteRenderer spriteRenderer;
    public AudioClip breakSound; // Engel kırılma sesi
    private GameObject hiddenPokemon; // Altında saklanan Pokémon

    private void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Şeffaf yap
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f); // %50 şeffaf
    }

    public void SetHiddenPokemon(GameObject pokemon)
    {
        hiddenPokemon = pokemon;
    }

    public GameObject GetHiddenPokemon()
    {
        return hiddenPokemon;
    }

    public bool HasHiddenPokemon()
    {
        return hiddenPokemon != null;
    }

    // Engelin çevresinde bir eşleşme olduğunda çağrılır
    public void TakeHit()
    {
        hitsTaken++;
        if (hitsTaken >= hitsToBreak)
        {
            BreakObstacle();
        }
        else
        {
            // Görsel olarak hasar aldığını göster (örneğin, renk değiştir)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, 0.5f), Color.red, (float)hitsTaken / hitsToBreak);
            }
        }
    }
    
    // Obstacle.cs içinde
    public void SetTransparent(bool isTransparent)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
            Color color = spriteRenderer.color;
            color.a = isTransparent ? 0.3f : 1f; // Şeffaf yap (0.3 opacity ile içini göster)
            spriteRenderer.color = color;
            }
    }
    private void BreakObstacle()
    {
        // Ses efekti çal
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (breakSound != null) audioSource.PlayOneShot(breakSound);

        // Engel yok edildikten sonra altındaki Pokémon açığa çıksın
        gameObject.SetActive(false); // Yok etme yerine devre dışı bırak, BoardManager bunu işleyecek
        Debug.Log("Engel kırıldı: " + gameObject.name);
    }
}