using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardController : MonoBehaviour
{
    [Header("Card Sprites")]
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite frontSprite;

    [Header("Animation Settings")]
    [SerializeField] private float flipDuration = 0.5f;

    [Header("State")]
    [SerializeField] private bool isFlipped = false;
    [SerializeField] private bool isMatched = false;
    [SerializeField] private bool isAnimating = false;

    // Components
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // Card values
    private int cardId;
    private int pairIndex;

    // Events
    public System.Action<CardController> OnCardClicked;
    public System.Action<CardController> OnFlipComplete;

    // Click protection
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.3f;

    #region Initialization

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(4.8f, 4.8f);

        SetCardBack();
    }

    public void Initialize(int id, int pairIdx, Sprite front, Sprite back)
    {
        cardId = id;
        pairIndex = pairIdx;
        frontSprite = front;
        backSprite = back;
        spriteRenderer.sprite = back;

        ResetCard();
    }

    #endregion

    #region Public Methods

    public void FlipCard()
    {
        // Prevent rapid clicks
        if (Time.time - lastClickTime < CLICK_COOLDOWN) return;
        lastClickTime = Time.time;

        // Don't flip if already matched or animating
        if (isMatched || isAnimating) return;

        // Notify click
        OnCardClicked?.Invoke(this);

        // Start flip animation
        StartCoroutine(FlipAnimation(!isFlipped));
    }

    public void ResetCard()
    {
        isFlipped = false;
        isMatched = false;
        SetCardBack();
        spriteRenderer.color = Color.white;
        transform.localScale = Vector3.one;
        
    }

    public void SetMatched()
    {
        isMatched = true;
        // Visual feedback for match
        spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
    }

    #endregion

    #region Animation - SIMPLE 3D ROTATION

    private IEnumerator FlipAnimation(bool flipToFront)
    {
        if (isAnimating) yield break;

        isAnimating = true;

        // Disable collider during animation to prevent multiple clicks
        boxCollider.enabled = false;

        float elapsedTime = 0f;
        float startRotation = flipToFront ? 0f : 180f;
        float endRotation = flipToFront ? 180f : 0f;

        // Start rotation
        transform.rotation = Quaternion.Euler(0, startRotation, 0);

        bool spriteSwitched = false;

        while (elapsedTime < flipDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / flipDuration;
            float currentRotation = Mathf.Lerp(startRotation, endRotation, progress);

            transform.rotation = Quaternion.Euler(0, currentRotation, 0);

            // Switch sprite at 90 degrees (mid-flip)
            if (!spriteSwitched && Mathf.Abs(currentRotation - 90f) < 5f)
            {
                if (flipToFront)
                    SetCardFront();
                else
                    SetCardBack();
                spriteSwitched = true;
            }

            yield return null;
        }

        // Ensure final state
        transform.rotation = Quaternion.Euler(0, endRotation, 0);

        if (flipToFront)
            SetCardFront();
        else
            SetCardBack();

        isFlipped = flipToFront;
        isAnimating = false;

        // Re-enable collider
        boxCollider.enabled = true;

        // Notify completion
        OnFlipComplete?.Invoke(this);
    }

    #endregion

    #region Sprite Management

    private void SetCardBack()
    {
        if (spriteRenderer != null && backSprite != null)
            spriteRenderer.sprite = backSprite;
    }

    private void SetCardFront()
    {
        if (spriteRenderer != null && frontSprite != null)
            spriteRenderer.sprite = frontSprite;
    }

    #endregion

    #region Properties & Utility

    void OnMouseDown()
    {
        FlipCard();
    }

    public int GetCardId() => cardId;
    public int GetPairIndex() => pairIndex;
    public bool IsFlipped() => isFlipped;
    public bool IsMatched() => isMatched;
    public bool IsAnimating() => isAnimating;

    #endregion
}