using System.Collections;
using UnityEngine;

public class CardController : MonoBehaviour
{
    private Sprite backSprite;
    private Sprite frontSprite;
    private float flipDuration = 0.4f;

    private bool isFlipped = false;
    private bool isMatched = false;
    private bool isAnimating = false;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private int cardId;
    private int pairIndex;

    public System.Action<CardController> OnCardClicked;
    public System.Action<CardController> OnFlipComplete;

    [SerializeField] AudioSource asCardFlip;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        boxCollider = GetComponent<BoxCollider2D>();
        if (!boxCollider) boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(4.8f, 4.8f); // Adjust based on your sprite pixels
    }

    public void Initialize(int id, int pairIdx, Sprite front, Sprite back)
    {
        cardId = id;
        pairIndex = pairIdx;
        frontSprite = front;
        backSprite = back;
        ResetCard();
    }

    public void FlipCard()
    {
        if (isMatched || isAnimating) return;
        StartCoroutine(FlipAnimation(!isFlipped));
        asCardFlip.Play();
    }

    public void ResetCard()
    {
        isFlipped = false;
        isMatched = false;
        isAnimating = false;
        spriteRenderer.sprite = backSprite;
        spriteRenderer.color = Color.white;
        transform.rotation = Quaternion.identity;
    }

    public void SetMatched()
    {
        isMatched = true;
        spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    }

    private IEnumerator FlipAnimation(bool flipToFront)
    {
        isAnimating = true;
        float elapsedTime = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion endRot = transform.rotation * Quaternion.Euler(0, 180, 0);

        bool spriteSwitched = false;

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flipDuration;

            // Apply rotation
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            // Switch sprite midway (at 90 degrees)
            if (!spriteSwitched && t >= 0.5f)
            {
                spriteRenderer.sprite = flipToFront ? frontSprite : backSprite;
                spriteSwitched = true;
            }
            yield return null;
        }

        transform.rotation = endRot;
        isFlipped = flipToFront;
        isAnimating = false;

        OnFlipComplete?.Invoke(this);
    }

    void OnMouseDown()
    {
        if (GridManager.notPlaying) return;
        // Simply notify the manager. Don't flip yet.
        FlipCard();
        OnCardClicked?.Invoke(this);
    }

    public int GetPairIndex() => pairIndex;
    public bool IsFlipped() => isFlipped;
    public bool IsMatched() => isMatched;
    public bool IsAnimating() => isAnimating;
}