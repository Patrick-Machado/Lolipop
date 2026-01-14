using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    [SerializeField] private Sprite backSprite; // Back of cards
    [SerializeField] private List<Sprite> frontSprites = new List<Sprite>(); // Fruit sprites

    // Game state
    private CardController[,] cardGrid;
    private List<CardController> flippedCards = new List<CardController>();
    private bool isCheckingMatch = false;


    public static bool canClickAgain = true;
    #region Initialization

    void Start()
    {
        ValidateGridSize();
        GenerateGrid();
        SetupCardPairs();
        PositionCards();
    }

    private void ValidateGridSize()
    {
        int totalCards = rows * columns;

        // Ensure even number of cards for pairs
        if (totalCards % 2 != 0)
        {
            Debug.LogWarning($"Grid size {rows}x{columns} creates odd number of cards ({totalCards}). Adding one more column.");
            columns++;
        }

        // Ensure we have enough unique sprites
        int neededPairs = totalCards / 2;
        if (frontSprites.Count < neededPairs)
        {
            Debug.LogError($"Need at least {neededPairs} unique sprites but only have {frontSprites.Count}");
            // In production, you'd want to handle this better
        }
    }

    #endregion

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

        // Card will be initialized later with proper sprites
        cardGrid[row, col] = card;

        // Subscribe to events
        card.OnCardClicked += HandleCardClicked;
        card.OnFlipComplete += HandleFlipComplete;
    }

    private void SetupCardPairs()
    {
        int totalCards = rows * columns;
        List<int> cardPairs = new List<int>();

        // Create pairs
        for (int i = 0; i < totalCards / 2; i++)
        {
            int spriteIndex = i % frontSprites.Count;
            cardPairs.Add(spriteIndex);
            cardPairs.Add(spriteIndex);
        }

        // Shuffle the pairs
        ShuffleList(cardPairs);

        // Assign to cards
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

# region card Positioning & Scaling

    private void PositionCards()
    {
        if (cardContainer == null) return;

        // Get container bounds (you can use Screen.width/height for screen space)
        Camera mainCamera = Camera.main;
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // Account for margins
        float availableWidth = screenWidth - (margin.x * 2);
        float availableHeight = screenHeight - (margin.y * 2);

        // Calculate card size to fit grid
        float cardWidth = 60f; //(availableWidth - ((columns - 1) * spacing.x)) / columns;
        float cardHeight = 60f; //(availableHeight - ((rows - 1) * spacing.y)) / rows;

        // Use the smaller dimension to maintain aspect ratio
        float cardSize = Mathf.Min(cardWidth, cardHeight);

        // Calculate starting position (centered)
        float gridWidth = (columns * cardSize) + ((columns - 1) * spacing.x);
        float gridHeight = (rows * cardSize) + ((rows - 1) * spacing.y);

        Vector2 startPos = new Vector2(
            -gridWidth / 2 + cardSize / 2,
            gridHeight / 2 - cardSize / 2
        );

        // Position each card
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

    #region Game Logic

    private void HandleCardClicked(CardController card)
    {
        if (isCheckingMatch || card.IsMatched()) return;

        // Limit to 2 cards flipped at once
        if (flippedCards.Count >= 2)
        {
            StartCoroutine(CheckMatch());
            return;
        }

        // Don't allow flipping the same card twice
        if (!flippedCards.Contains(card))
        {
            flippedCards.Add(card);
        }
    }

    private void HandleFlipComplete(CardController card)
    {
        // Check for match when 2 cards are flipped
        if (flippedCards.Count == 2 && !flippedCards.Contains(card))
        {
            flippedCards.Add(card);
        }

        if (flippedCards.Count == 2)
        {
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        isCheckingMatch = true;
        GridManager.canClickAgain = true;

        // Wait a moment to show the cards
        yield return new WaitForSeconds(0.5f);

        if (flippedCards.Count == 2)
        {
            CardController card1 = flippedCards[0];
            CardController card2 = flippedCards[1];

            if (card1.GetPairIndex() == card2.GetPairIndex())
            {
                // Match found
                card1.SetMatched();
                card2.SetMatched();
                Debug.Log("Match found!");

                // Play match sound here
            }
            else
            {
                // No match - flip back
                yield return new WaitForSeconds(0.5f);
                card1.FlipCard();
                card2.FlipCard();

                // Play mismatch sound here
            }
        }

        flippedCards.Clear();
        isCheckingMatch = false;
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

    #region Editor Integration

#if UNITY_EDITOR
    /*void OnValidate()
    {
        // Clamp values in editor
        rows = Mathf.Max(2, rows);
        columns = Mathf.Max(2, columns);

        // Ensure even number of cards in editor
        if ((rows * columns) % 2 != 0)
        {
            columns++;
        }
    }*/

    [ContextMenu("Test 2x2 Grid")]
    private void Test2x2() => RegenerateGrid(2, 2);

    [ContextMenu("Test 3x3 Grid")]
    private void Test3x3() => RegenerateGrid(3, 3);

    [ContextMenu("Test 5x6 Grid")]
    private void Test5x6() => RegenerateGrid(5, 6);
#endif

    #endregion
}