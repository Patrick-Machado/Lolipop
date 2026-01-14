using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int savedScore;
    public int savedMoves;
    public int savedMatches;
    public int savedRows;
    public int savedColumns;
    public int[] savedCardPairs; // Store pair indices for grid recreation
    public bool[] savedMatchedStates; // Which cards are already matched
    public string saveDate;

    // Constructor
    public SaveData(int score, int moves, int matches, int rows, int cols,
                    int[] cardPairs, bool[] matchedStates)
    {
        savedScore = score;
        savedMoves = moves;
        savedMatches = matches;
        savedRows = rows;
        savedColumns = cols;
        savedCardPairs = cardPairs;
        savedMatchedStates = matchedStates;
        saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}