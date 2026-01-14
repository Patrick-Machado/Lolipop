using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteSheetLoader : MonoBehaviour
{
    [SerializeField] private Texture2D spriteSheet;
    [SerializeField] private Vector2Int spriteSize = new Vector2Int(256, 256);

    private List<Sprite> loadedSprites = new List<Sprite>();

    void Start()
    {
        if (spriteSheet != null)
        {
            LoadSpritesFromSheet();
        }
    }

    public void LoadSpritesFromSheet()
    {
        loadedSprites.Clear();

        if (spriteSheet == null)
        {
            Debug.LogError("Sprite sheet is not assigned!");
            return;
        }

        int columns = spriteSheet.width / spriteSize.x;
        int rows = spriteSheet.height / spriteSize.y;

        for (int y = rows - 1; y >= 0; y--)
        {
            for (int x = 0; x < columns; x++)
            {
                Rect spriteRect = new Rect(
                    x * spriteSize.x,
                    y * spriteSize.y,
                    spriteSize.x,
                    spriteSize.y
                );

                Sprite sprite = Sprite.Create(
                    spriteSheet,
                    spriteRect,
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                loadedSprites.Add(sprite);

                // Stop if we have 6 sprites (5 fruits + 1 back)
                if (loadedSprites.Count >= 6) break;
            }
            if (loadedSprites.Count >= 6) break;
        }

        Debug.Log($"Loaded {loadedSprites.Count} sprites from sheet");
    }

    public List<Sprite> GetFrontSprites()
    {
        // Return first 5 sprites for fruits
        if (loadedSprites.Count >= 5)
        {
            return loadedSprites.GetRange(0, 5);
        }
        return new List<Sprite>();
    }

    public Sprite GetBackSprite()
    {
        // Return the 6th sprite as back
        if (loadedSprites.Count >= 6)
        {
            return loadedSprites[5];
        }
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("Load and Assign to Grid")]
    private void LoadAndAssign()
    {
        LoadSpritesFromSheet();

        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null)
        {
            // This would require exposing a method in GridManager to set sprites
            // gridManager.SetSprites(GetFrontSprites(), GetBackSprite());
        }
    }
#endif
}