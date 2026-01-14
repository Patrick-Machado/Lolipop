using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CardController : MonoBehaviour
{
    [Header("Card Sprites")]
    [SerializeField] private Sprite backSprite; // The back of the card
    [SerializeField] private Sprite frontSprite; // The front image (banana, cherry, etc.)

    [Header("Animation Settings")]
    [SerializeField] private float flipDuration = 0.5f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("State")]
    [SerializeField] private bool isFlipped = false;
    [SerializeField] private bool isMatched = false;
    [SerializeField] private bool isAnimating = false;

    // Components
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // Card values
    private int cardId; // Unique ID for matching
    private int pairIndex; // Which fruit type this card represents

    // Animation coroutine reference
    private Coroutine flipCoroutine;

    // Events
    public System.Action<CardController> OnCardClicked;
    public System.Action<CardController> OnFlipComplete;


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
        // Set initial state
        SetCardBack();

    }

    public void Initialize(int id, int pairIdx, Sprite front, Sprite back)
    {
        cardId = id;
        pairIndex = pairIdx;
        frontSprite = front; //spriteRenderer.sprite = front;
        backSprite = back; spriteRenderer.sprite = back;

        // Reset to default state
        ResetCard();
    }

    #endregion

    #region Public Methods

    public void FlipCard()
    {
        if (isAnimating || isMatched) return;

        if (flipCoroutine != null)
            StopCoroutine(flipCoroutine);

        flipCoroutine = StartCoroutine(FlipAnimation(!isFlipped));
    }

    public void ResetCard(bool immediate = false)
    {
        if (flipCoroutine != null)
        {
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        isAnimating = false;
        isFlipped = false;
        isMatched = false;

        if (immediate)
        {
            SetCardBack();
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else
        {
            StartCoroutine(ResetCardAnimation());
        }
    }

    public void SetMatched()
    {
        isMatched = true;
        // You can add visual feedback here (change color, disable, etc.)
        spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    #endregion

    #region Animation

    private IEnumerator FlipAnimation(bool flipToFront)
    {
        if (isAnimating) yield break;

        isAnimating = true;
        OnCardClicked?.Invoke(this);

        float elapsedTime = 0f;

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = new Vector3(0.1f, transform.localScale.y, transform.localScale.z);

        // Phase 1: Shrink horizontally
        while (elapsedTime < flipDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (flipDuration / 2);
            float curveValue = flipCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        // Switch sprite at midpoint
        if (flipToFront)
            SetCardFront();
        else
            SetCardBack();

        // Phase 2: Expand horizontally
        elapsedTime = 0f;
        //startScale = transform.localScale;
        targetScale = new Vector3(0.1f, transform.localScale.y, transform.localScale.z);

        while (elapsedTime < flipDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (flipDuration / 2);
            float curveValue = flipCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(targetScale, startScale, curveValue);
            yield return null;
        }

        //transform.localScale = targetScale;
        isFlipped = flipToFront;
        isAnimating = false;


        OnFlipComplete?.Invoke(this);
    }

    private IEnumerator ResetCardAnimation()
    {
        if (isFlipped && !isMatched)
        {
            yield return FlipAnimation(false);
        }
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
        if (!isMatched && !isAnimating)
            FlipCard();
    }

    public int GetCardId() => cardId;
    public int GetPairIndex() => pairIndex;
    public bool IsFlipped() => isFlipped;
    public bool IsMatched() => isMatched;
    public bool IsAnimating() => isAnimating;

    public void SetInteractable(bool interactable)
    {
        if (boxCollider != null)
            boxCollider.enabled = interactable;
    }

    #endregion
}