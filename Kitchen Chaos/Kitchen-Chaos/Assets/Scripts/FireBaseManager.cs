using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FireBaseManager : MonoBehaviour
{
    DatabaseReference reference;
    private List<FirebaseRecipeData> activeRecipe;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogError($"Firebase initialization failed: {task.Exception}");
                return;
            }
            reference = FirebaseDatabase.DefaultInstance.RootReference;

            RemoveAllRecipeData();
            FetchRecipeData();
            ListenForRecipeDataChanges();
            IsFullOrder(false);
        });
       
    }

    void ListenForRecipeDataChanges()
    {
        reference.Child("recipeData").ChildAdded += Order;
    }

    private void Order(object sender, ChildChangedEventArgs args)
    {
        FetchRecipeData();
        UpdateRecipe();
    }

    private void UpdateRecipe()
    {
        if (activeRecipe.Count <= 0)
        {
            return;
        }
        DeliveryManager.Instance.OrderRecipe(activeRecipe[activeRecipe.Count - 1].value, activeRecipe[activeRecipe.Count - 1].key);
    }

    void FetchRecipeData()
    {
        activeRecipe = new List<FirebaseRecipeData>();
        reference.Child("recipeData").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to fetch recipe data: {task.Exception}");
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot != null && snapshot.Exists)
            {
                foreach (DataSnapshot childSnapshot in snapshot.Children)
                {
                    string key = childSnapshot.Key;
                    string value = childSnapshot.Value.ToString();

                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(key));
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                    DateTime dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, timeZone);

                    string formattedDateTime = dateTime.ToString("dd/MM/yyyy HH:mm:ss");

                    FirebaseRecipeData recipeData = new FirebaseRecipeData(formattedDateTime, value);
                    activeRecipe.Add(recipeData);

                    Debug.Log($"Recipe key: {key}, value: {value}");
                }
                Debug.Log("????: " + activeRecipe.Count);
                UpdateRecipe();
            }
            else
            {
                Debug.LogWarning("No recipe data found.");
            }
        });
    }

    void RemoveAllRecipeData()
    {
        reference.Child("recipeData").RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to remove all recipe data: {task.Exception}");
                return;
            }

            Debug.Log("All recipe data removed successfully.");
        });
    }

    public void IsFullOrder(bool isFullOrder)
    {
        reference.Child("isFullOrder").SetValueAsync(isFullOrder).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to update IsFullOrder: {task.Exception}");
                return;
            }

            Debug.Log("IsFullOrder updated successfully.");
        });
    }
}

[System.Serializable]
public class FirebaseRecipeData
{
    public string key;
    public string value;

    public FirebaseRecipeData(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}
