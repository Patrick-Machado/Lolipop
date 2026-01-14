using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

    [Header("Audio")]
    [SerializeField] AudioSource asCardFlip;
    [SerializeField] AudioSource asWrongCard;
    [SerializeField] AudioSource asRightCard;
    [SerializeField] AudioSource asVictory;
    [SerializeField] AudioSource asGameOver;

    [Header("Scoring System")]
    [SerializeField] private int baseMatchScore = 100;
    [SerializeField] private int baseMismatchPenalty = 10;
    [SerializeField] private int comboBonus = 50;

    [Header("Game State")]
    [SerializeField] int score = 0;
    [SerializeField] int currentCombo = 0;
    [SerializeField] int totalMatches = 0;
    [SerializeField] int moveCount = 0;
    private int totalPairs = 0;
    private bool gameWon = false;

    [Header("UI References")]
    [SerializeField] TMPro.TextMeshProUGUI textScore;
    [SerializeField] TMPro.TextMeshProUGUI textCombo;
    [SerializeField] GameObject VictoryGO;
    [SerializeField] GameObject GameObverGO;

    private CardController[,] cardGrid;
    private List<CardController> pendingMatchQueue = new List<CardController>();
    private float lastClickTime = 0f;
    private const float CLICK_DELAY = 0.15f;

    [SerializeField] int lifes = 10;
    [SerializeField] TMPro.TextMeshProUGUI lifesText;
    public static bool notPlaying = false;

    void Start()
    {
        ValidateGridSize();
        GenerateGrid();
        SetupCardPairs();
        PositionCards();

        totalPairs = (rows * columns) / 2;

        if (!LoadGame())
        {
            Debug.Log("Starting new game");
        }
        notPlaying = false;
        lifesText.text = "Lifes: " + lifes;
    }

    private void ValidateGridSize()
    {
        int totalCards = rows * columns;
        if (totalCards % 2 != 0) columns++;

        int neededPairs = (rows * columns) / 2;
        if (frontSprites.Count < neededPairs)
            Debug.LogError($"Need {neededPairs} sprites, but only have {frontSprites.Count}");
    }

    private void GenerateGrid()
    {
        cardGrid = new CardController[rows, columns];
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                CardController card = cardObj.GetComponent<CardController>();
                cardGrid[r, c] = card;

                // Subscribe to events
                card.OnCardClicked += HandleCardClicked;
                card.OnFlipComplete += HandleFlipComplete;
            }
        }
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

        // Shuffle
        for (int i = 0; i < cardPairs.Count; i++)
        {
            int temp = cardPairs[i];
            int randomIndex = UnityEngine.Random.Range(i, cardPairs.Count);
            cardPairs[i] = cardPairs[randomIndex];
            cardPairs[randomIndex] = temp;
        }

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                cardGrid[r, c].Initialize(index, cardPairs[index], frontSprites[cardPairs[index]], backSprite);
                index++;
            }
        }
    }

    private void PositionCards()
    {
        Camera mainCamera = Camera.main;
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;
        float availableWidth = screenWidth - (margin.x * 2);
        float availableHeight = screenHeight - (margin.y * 2);

        float cardSize = 60f;// Mathf.Min(availableWidth / columns, availableHeight / rows) * 0.9f;

        float gridWidth = (columns * cardSize) + ((columns - 1) * spacing.x);
        float gridHeight = (rows * cardSize) + ((rows - 1) * spacing.y);

        Vector2 startPos = new Vector2(-gridWidth / 2 + cardSize / 2, gridHeight / 2 - cardSize / 2);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Vector2 pos = new Vector2(startPos.x + c * (cardSize + spacing.x), startPos.y - r * (cardSize + spacing.y));
                cardGrid[r, c].transform.localPosition = pos;
                cardGrid[r, c].transform.localScale = new Vector3(cardSize, cardSize, 1f);
            }
        }
    }

    private void HandleCardClicked(CardController card)
    {
        // Global click delay
        if (Time.time - lastClickTime < CLICK_DELAY) return;

        // Basic state check
        if (card.IsMatched() || card.IsFlipped() || card.IsAnimating()) return;

        // Ensure this card isn't already waiting in the logic queue
        if (pendingMatchQueue.Contains(card)) return;

        asCardFlip.Play();
        lastClickTime = Time.time;
        card.FlipCard(); // Order the physical flip
    }

    private void HandleFlipComplete(CardController card)
    {
        if (card.IsFlipped())
        {
            pendingMatchQueue.Add(card);

            // Once we have a pair, take them OUT of the queue immediately
            // so the next flip creates a fresh pair.
            if (pendingMatchQueue.Count >= 2)
            {
                CardController c1 = pendingMatchQueue[0];
                CardController c2 = pendingMatchQueue[1];
                pendingMatchQueue.RemoveRange(0, 2);

                StartCoroutine(CheckMatchSequence(c1, c2));
            }
        }
    }


    private IEnumerator CheckMatchSequence(CardController c1, CardController c2)
    {
        moveCount++;
        yield return new WaitForSeconds(0.5f); // Duration to view the pair

        if (c1.GetPairIndex() == c2.GetPairIndex())
        {
            // MATCH FOUND
            currentCombo++;
            score += baseMatchScore + (currentCombo * comboBonus);
            totalMatches++;

            c1.SetMatched();
            c2.SetMatched();
            asRightCard.Play();

            CheckWinCondition();
        }
        else
        {
            // MISMATCH
            currentCombo = 0;
            score = Mathf.Max(0, score - baseMismatchPenalty);
            asWrongCard.Play();

            lifes--;
            if (lifes <= 0)
            { lifes = 0; }
            lifesText.text = "Lifes: " + lifes;
            if (lifes <= 0)
            {
                asGameOver.Play();
                GameObverGO.SetActive(true);
                notPlaying = true;
            }


            c1.FlipCard(); // Flip back
            c2.FlipCard(); // Flip back
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (textScore) textScore.text = "Score: " + score;
        if (textCombo) textCombo.text = "Combo: " + currentCombo;
    }

    private void CheckWinCondition()
    {
        if (totalMatches >= totalPairs)
        {
            gameWon = true;
            asVictory.Play();
            if (VictoryGO) VictoryGO.SetActive(true);
            notPlaying = true;
            SaveGame();
        }
    }

    public void Replay() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void SaveGame()

    {

        // Prepare data arrays

        int totalCards = rows * columns;

        int[] cardPairs = new int[totalCards];

        bool[] matchedStates = new bool[totalCards];



        // Collect card data

        int index = 0;

        for (int row = 0; row < rows; row++)

        {

            for (int col = 0; col < columns; col++)

            {

                cardPairs[index] = cardGrid[row, col].GetPairIndex();

                matchedStates[index] = cardGrid[row, col].IsMatched();

                index++;

            }

        }



        // Create save data

        SaveData saveData = new SaveData(

            score, moveCount, totalMatches,

            rows, columns, cardPairs, matchedStates

        );



        // Convert to JSON

        string jsonData = JsonUtility.ToJson(saveData);



        // Save to PlayerPrefs (simple solution)

        PlayerPrefs.SetString("CardGame_Save", jsonData);

        PlayerPrefs.SetInt("CardGame_HasSave", 1);

        PlayerPrefs.Save();



        Debug.Log("Game saved successfully!");

    }

    public bool LoadGame()

    {

        if (PlayerPrefs.GetInt("CardGame_HasSave", 0) == 0)

        {

            Debug.Log("No saved game found. Starting fresh.");

            return false;

        }



        try

        {

            string jsonData = PlayerPrefs.GetString("CardGame_Save", "");

            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);



            // Restore game state

            score = saveData.savedScore;

            //moveCount = saveData.savedMoves;

            //totalMatches = saveData.savedMatches;



            textScore.text = "Score: " + score;



            Debug.Log($"Game loaded! Score: {score}, Moves: {moveCount}, Saved on: {saveData.saveDate}");

            return true;

        }

        catch (Exception e)

        {

            Debug.LogError($"Failed to load game: {e.Message}");

            return false;

        }

    }


}

