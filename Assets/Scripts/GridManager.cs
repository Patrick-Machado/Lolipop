using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 spacing = new Vector2(0.1f, 0.1f);
    [SerializeField] private Vector2 margin = new Vector2(0.5f, 0.5f);

    [Header("Card References")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("Sprite Sheet Management")]
    [SerializeField] private Sprite backSprite;
    [SerializeField] private List<Sprite> frontSprites = new List<Sprite>();

    // Game state
    private CardController[,] cardGrid;
    private List<CardController> flippedCards = new List<CardController>();
    private Coroutine matchCheckCoroutine;

    // Click tracking
    private float lastClickTime = 0f;
    private const float CLICK_DELAY = 0.2f;


    [SerializeField] AudioSource asFlLipCard;
    [SerializeField] AudioSource asWrongCard;
    [SerializeField] AudioSource asRightCard;
    [SerializeField] AudioSource asVictory;
    [SerializeField] AudioSource asGameOver;


    [Header("Scoring System")]
    [SerializeField] private int baseMatchScore = 100;
    [SerializeField] private int baseMismatchPenalty = 10;
    [SerializeField] private int comboBonus = 50;
    [SerializeField] private int moveCount = 0;

    [Header("Current Score")]
    [SerializeField] int score = 0;
    [SerializeField] int currentCombo = 0;
    [SerializeField] int totalMatches = 0;

    public int GetScore() => score;
    public int GetMoves() => moveCount;
    public int GetMatches() => totalMatches;
    public int GetCombo() => currentCombo;

    [Header("Game Completion")]
    [SerializeField] private int totalPairs = 0;
    private bool gameWon = false;


    [SerializeField] TMPro.TextMeshProUGUI textScore;
    [SerializeField] TMPro.TextMeshProUGUI textCombo;
    [SerializeField] GameObject VictoryGO;

    public void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    void Start()
    {
        ValidateGridSize();
        GenerateGrid();
        SetupCardPairs();
        PositionCards();

        // Calculate total pairs
        totalPairs = (rows * columns) / 2;
        Debug.Log($"Game started. Find {totalPairs} pairs to win!");
    }

    private void ValidateGridSize()
    {
        int totalCards = rows * columns;

        if (totalCards % 2 != 0)
        {
            Debug.LogWarning($"Grid size {rows}x{columns} creates odd number of cards ({totalCards}). Adding one more column.");
            columns++;
        }

        int neededPairs = totalCards / 2;
        if (frontSprites.Count < neededPairs)
        {
            Debug.LogError($"Need at least {neededPairs} unique sprites but only have {frontSprites.Count}");
        }
    }

    #region Grid Generation

    private void GenerateGrid()
    {
        cardGrid = new CardController[rows, columns];
        ClearExistingCards();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                CreateCard(row, col);
            }
        }
    }

    private void CreateCard(int row, int col)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card prefab is not assigned!");
            return;
        }

        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        cardObj.name = $"Card_{row}_{col}";

        CardController card = cardObj.GetComponent<CardController>();
        if (card == null)
        {
            Debug.LogError("Card prefab doesn't have CardController component!");
            Destroy(cardObj);
            return;
        }

        cardGrid[row, col] = card;
        card.OnCardClicked += HandleCardClicked;
        card.OnFlipComplete += HandleFlipComplete;
    }

    private void SetupCardPairs()
    {
        int totalCards = rows * columns;
        List<int> cardPairs = new List<int>();

        for (int i = 0; i < totalCards / 2; i++)
        {
            int spriteIndex = i % frontSprites.Count;
            cardPairs.Add(spriteIndex);
            cardPairs.Add(spriteIndex);
        }

        ShuffleList(cardPairs);

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int pairIndex = cardPairs[index];
                Sprite frontSprite = frontSprites[pairIndex];

                cardGrid[row, col].Initialize(
                    index,
                    pairIndex,
                    frontSprite,
                    backSprite
                );

                index++;
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    #endregion

    #region Card Positioning & Scaling

    private void PositionCards()
    {
        if (cardContainer == null) return;

        Camera mainCamera = Camera.main;
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        float availableWidth = screenWidth - (margin.x * 2);
        float availableHeight = screenHeight - (margin.y * 2);

        float cardWidth = 60f;
        float cardHeight = 60f;

        float cardSize = Mathf.Min(cardWidth, cardHeight);

        float gridWidth = (columns * cardSize) + ((columns - 1) * spacing.x);
        float gridHeight = (rows * cardSize) + ((rows - 1) * spacing.y);

        Vector2 startPos = new Vector2(
            -gridWidth / 2 + cardSize / 2,
            gridHeight / 2 - cardSize / 2
        );

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                CardController card = cardGrid[row, col];
                if (card == null) continue;

                Vector2 position = new Vector2(
                    startPos.x + col * (cardSize + spacing.x),
                    startPos.y - row * (cardSize + spacing.y)
                );

                card.transform.localPosition = position;
                card.transform.localScale = new Vector3(cardSize, cardSize, 1f);
            }
        }
    }

    #endregion

    #region Game Logic - SIMPLE AND RELIABLE

    private void HandleCardClicked(CardController card)
    {
        // Prevent rapid clicks on the same card
        if (Time.time - lastClickTime < CLICK_DELAY) return;
        lastClickTime = Time.time;

        // Basic validation
        if (card.IsMatched() || card.IsFlipped() || card.IsAnimating()) return;

        // If we already have 2 cards flipped and are checking them,
        // we should wait for that check to complete before allowing more flips
        if (flippedCards.Count >= 2 && matchCheckCoroutine != null) return;

        // Play sound
        asFlLipCard.Play();

        // Flip the card
        card.FlipCard();
    }

    private void HandleFlipComplete(CardController card)
    {
        // Add card to flipped list if it's facing front
        if (card.IsFlipped() && !flippedCards.Contains(card))
        {
            flippedCards.Add(card);
        }
        // Remove card if it's facing back (after mismatch)
        else if (!card.IsFlipped() && flippedCards.Contains(card))
        {
            flippedCards.Remove(card);
        }

        // If we have 2 cards flipped, check for match
        if (flippedCards.Count == 2)
        {
            // Don't start a new check if one is already running
            if (matchCheckCoroutine == null)
            {
                matchCheckCoroutine = StartCoroutine(CheckMatch());
            }
        }
    }

    private IEnumerator CheckMatch()
    {
        //isCheckingMatch = true;
        moveCount++; // Count this as a move

        // Make a copy of the flipped cards to work with
        CardController[] cardsToCheck = new CardController[2];
        cardsToCheck[0] = flippedCards[0];
        cardsToCheck[1] = flippedCards[1];

        // Clear the list immediately so new cards can be flipped
        flippedCards.Clear();

        // Ensure we have 2 different cards
        if (cardsToCheck[0] == cardsToCheck[1])
        {
            Debug.LogError("Same card twice! This shouldn't happen.");
            matchCheckCoroutine = null;
            yield break;
        }

        // Wait a moment to show the cards
        yield return new WaitForSeconds(0.5f);

        // Check for match
        bool isMatch = cardsToCheck[0].GetPairIndex() == cardsToCheck[1].GetPairIndex();

        if (isMatch)
        {
            // Match found
            currentCombo++;
            int matchScore = baseMatchScore + (currentCombo * comboBonus);
            score += matchScore;
            totalMatches++;
            textScore.text = "Score: " + score;
            textCombo.text = "Combo: : " + currentCombo;

            Debug.Log($"Match! +{matchScore} points (Combo: x{currentCombo}) Total: {score}");

            Debug.Log($"Match! Pair index: {cardsToCheck[0].GetPairIndex()}");
            cardsToCheck[0].SetMatched();
            cardsToCheck[1].SetMatched();

            // Play sound
            asRightCard.Play();

            // Check for win condition after match
            CheckWinCondition();
        }
        else
        {
            currentCombo = 0;
            // No match - flip back
            score = Mathf.Max(0, score - baseMismatchPenalty);
            Debug.Log($"No match! -{baseMismatchPenalty} points. Total: {score}");
            textScore.text = "Score: " + score;
            textCombo.text = "Combo: : " + currentCombo;


            // Flip both cards back
            cardsToCheck[0].FlipCard();
            cardsToCheck[1].FlipCard();

            asWrongCard.Play(); // Play sound

            // Wait for flip back to complete (optional)
            yield return new WaitForSeconds(0.6f);
        }

        // Clear the coroutine reference
        matchCheckCoroutine = null;
    }

    private void CheckWinCondition()
    {
        if (gameWon) return;

        // Count matched cards
        int matchedCount = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (cardGrid[row, col].IsMatched())
                {
                    matchedCount++;
                }
            }
        }

        // Check if all cards are matched (2 per pair)
        if (matchedCount == rows * columns)
        {
            gameWon = true;
            Debug.Log($"VICTORY! All {totalPairs} pairs found!");
            Debug.Log($"Final Score: {score} | Moves: {moveCount} | Accuracy: {(float)totalMatches / moveCount * 100:F1}%");

            // Trigger victory sound (you'll add this)
            asVictory.Play();

            VictoryGO.SetActive(true);

        }
    }



    #endregion

    #region Utility Methods

    private void ClearExistingCards()
    {
        if (cardContainer == null) return;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void RegenerateGrid(int newRows, int newColumns)
    {
        rows = newRows;
        columns = newColumns;

        ValidateGridSize();
        GenerateGrid();
        SetupCardPairs();
        PositionCards();
    }

    #endregion
}