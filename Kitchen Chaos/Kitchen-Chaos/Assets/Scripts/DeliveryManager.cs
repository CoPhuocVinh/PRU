using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public event EventHandler<string> OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;

    public static DeliveryManager Instance { get; private set; }

    [SerializeField] private RecipeListSO recipeListSO;
    [SerializeField] private List<RecipeSO> waitingRecipeSOList;
    [SerializeField] private List<RecipeSOUI> waitingRecipeSOListUI;
    private float spawnRecipeTimer;
    private float spawnRecipeTimerMax = 4f;
    private int waitingRecipesMax = 4;
    private int successfulRecipesAmount;

    [Header("Recipe Runtime")]
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;
    private FireBaseManager _fireBaseManager;

    private void Awake()
    {
        Instance = this;
        _fireBaseManager = GetComponent<FireBaseManager>();
        waitingRecipeSOList = new List<RecipeSO>();
        waitingRecipeSOListUI = new List<RecipeSOUI>();
    }

    public void OrderRecipe(string recipeName, string time)
    {
        RecipeSO orderRecipe = null;
        foreach (var recipe in recipeListSO.recipeSOList)
        {
            if (recipe.recipeName == recipeName)
            {
                orderRecipe = recipe;
                break;
            }
        }

        if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax)
        {
            RecipeSO waitingRecipeSO = orderRecipe;
            waitingRecipeSOList.Add(waitingRecipeSO);

            RecipeSOUI r = new RecipeSOUI(waitingRecipeSO, time);
            waitingRecipeSOListUI.Add(r);
            OnRecipeSpawned?.Invoke(this, time);
        }

        if (waitingRecipeSOList.Count >= waitingRecipesMax)
        {
            _fireBaseManager.IsFullOrder(true);
        }
        else
        {
            _fireBaseManager.IsFullOrder(false);
        }
    }

    public void DeliverRecipe(PlateKitchenObject plateKitchenObject)
    {
        for (int i = 0; i < waitingRecipeSOList.Count; i++)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count)
            {
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
                    {
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            ingredientFound = true;
                            break;
                        }
                    }
                    if (!ingredientFound)
                    {
                        plateContentsMatchesRecipe = false;
                    }
                }
                if (plateContentsMatchesRecipe)
                {
                    successfulRecipesAmount++;

                    waitingRecipeSOList.RemoveAt(i);
                    waitingRecipeSOListUI.RemoveAt(i);

                    OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                    OnRecipeSuccess?.Invoke(this, EventArgs.Empty);

                    return;
                }
            }
        }
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSOUI> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOListUI;
    }

    public int GetSuccessfulRecipesAmount()
    {
        return successfulRecipesAmount;
    }
}

[System.Serializable]
public class RecipeSOUI
{
    public RecipeSO recipeSO;
    public string time;

    public RecipeSOUI(RecipeSO recipeSO, string time)
    {
        this.recipeSO = recipeSO;
        this.time = time;
    }
}
