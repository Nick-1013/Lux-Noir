using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public int coinCount;

    [Header("UI")]
    public TMP_Text coinText; // assign a TextMeshPro - Text (UI) component in the Inspector
    public GameObject coinUIParent; // parent GameObject for the whole coin UI (will be moved behind screens, not disabled)

    [Header("Optional refs")]
    public GameManagerScript gameManager; // optional, will be auto-found if left null

    // Cached canvas & original sorting order for restoring
    private Canvas coinCanvas;
    private int originalSortingOrder;
    private bool hasCoinCanvas;

    void Start()
    {
        coinCount = 0;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManagerScript>();

        if (coinUIParent != null)
        {
            coinCanvas = coinUIParent.GetComponentInParent<Canvas>();
            if (coinCanvas != null)
            {
                hasCoinCanvas = true;
                originalSortingOrder = coinCanvas.sortingOrder;
            }
        }
    }

    void Update()
    {
        // Update text (number before colon)
        if (coinText != null)
            coinText.text = coinCount.ToString() + ": ";

        if (coinUIParent == null) return;

        bool anyScreenActive = false;
        if (gameManager != null)
        {
            anyScreenActive =
                (gameManager.pauseMenuUI != null && gameManager.pauseMenuUI.activeSelf) ||
                (gameManager.gameOverUI != null && gameManager.gameOverUI.activeSelf) ||
                (gameManager.gameWinUI != null && gameManager.gameWinUI.activeSelf);
        }

        if (anyScreenActive)
        {
            // Try to move coin UI behind by lowering sibling index if same parent
            if (AreSameParent(coinUIParent, gameManager))
            {
                coinUIParent.transform.SetAsFirstSibling();
            }
            else if (hasCoinCanvas)
            {
                // Find an active UI screen Canvas to compare sorting order with
                Canvas activeScreenCanvas = GetActiveScreenCanvas();
                if (activeScreenCanvas != null)
                {
                    // Put coin canvas behind active screen canvas
                    coinCanvas.sortingOrder = activeScreenCanvas.sortingOrder - 1;
                }
                else
                {
                    // Fallback: ensure coin UI is drawn earlier in its parent
                    coinUIParent.transform.SetAsFirstSibling();
                }
            }
            else
            {
                // Fallback: set as first sibling in its parent (often makes it render behind other siblings)
                coinUIParent.transform.SetAsFirstSibling();
            }
        }
        else
        {
            // Restore to original ordering (bring to front)
            if (hasCoinCanvas)
            {
                coinCanvas.sortingOrder = originalSortingOrder;
            }
            else
            {
                coinUIParent.transform.SetAsLastSibling();
            }
        }
    }

    // Helper: check if coinUIParent and at least one of the UIs share the same parent transform
    private bool AreSameParent(GameObject coinUI, GameManagerScript gm)
    {
        if (coinUI == null || gm == null) return false;
        Transform parent = coinUI.transform.parent;
        if (parent == null) return false;

        if (gm.pauseMenuUI != null && gm.pauseMenuUI.transform.parent == parent) return true;
        if (gm.gameOverUI != null && gm.gameOverUI.transform.parent == parent) return true;
        if (gm.gameWinUI != null && gm.gameWinUI.transform.parent == parent) return true;

        return false;
    }

    // Helper: returns first active screen Canvas (pause, gameOver, gameWin) or null
    private Canvas GetActiveScreenCanvas()
    {
        if (gameManager == null) return null;

        if (gameManager.pauseMenuUI != null && gameManager.pauseMenuUI.activeSelf)
            return gameManager.pauseMenuUI.GetComponentInParent<Canvas>();
        if (gameManager.gameOverUI != null && gameManager.gameOverUI.activeSelf)
            return gameManager.gameOverUI.GetComponentInParent<Canvas>();
        if (gameManager.gameWinUI != null && gameManager.gameWinUI.activeSelf)
            return gameManager.gameWinUI.GetComponentInParent<Canvas>();

        return null;
    }
}