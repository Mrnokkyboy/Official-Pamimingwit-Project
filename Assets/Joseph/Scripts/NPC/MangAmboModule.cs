using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class TieredDialogue
{
    public string tierName; // Must match the tierName in ReactiveOceanManager
    public DialogueLine[] lines;

    [Header("Milestone Reward")]
    public int rewardCoins;
    public ItemData rewardItem;
    public int rewardAmount = 1;
}

public class MangAmboModule : NPCModule
{
    [Header("Persistence ID")]
    [SerializeField] private string moduleSaveID = "MangAmbo_ClaimedTiers";

    [Header("Dialogue Content")]
    [SerializeField] private DialogueLine[] defaultDialogue;
    [SerializeField] private List<TieredDialogue> tieredDialogues = new List<TieredDialogue>();

    private HashSet<string> claimedTiers = new HashSet<string>();

    private void Awake()
    {
        LoadClaimedTiers();
    }

    public override string GetInteractionPrompt()
    {
        return "Speak with Mang Ambo [E]";
    }

    public override void OnInteract()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("MangAmboModule: DialogueManager instance not found in scene.");
            return;
        }

        DialogueLine[] linesToDisplay = defaultDialogue;
        TieredDialogue activeTierMatch = null;

        if (ReactiveOceanManager.Instance != null)
        {
            OceanTier currentTier = ReactiveOceanManager.Instance.GetCurrentTier();
            if (currentTier != null)
            {
                TieredDialogue match = tieredDialogues.Find(t => t.tierName == currentTier.tierName);
                bool alreadyClaimed = match != null && claimedTiers.Contains(match.tierName);

                if (match != null && !alreadyClaimed && match.lines != null && match.lines.Length > 0)
                {
                    linesToDisplay = match.lines;
                    activeTierMatch = match; // Only set active match if eligible for reward
                }
            }
        }

        DialogueManager.Instance.ShowDialogue(linesToDisplay, () => 
        {
            if (activeTierMatch != null)
            {
                TryGiveReward(activeTierMatch);
            }
        });
    }

    private void TryGiveReward(TieredDialogue tieredData)
    {
        if (claimedTiers.Contains(tieredData.tierName)) return;

        bool hasCoins = tieredData.rewardCoins > 0;
        bool hasItem = tieredData.rewardItem != null;

        if (!hasCoins && !hasItem) return;

        if (hasCoins)
        {
            PlayerWallet.Instance?.AddCoins(tieredData.rewardCoins);
        }

        if (hasItem && Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(tieredData.rewardItem, tieredData.rewardAmount);
        }

        claimedTiers.Add(tieredData.tierName);
        SaveClaimedTiers();

        UIManager.Instance?.ShowMessage("Mang Ambo gave you a reward for your efforts!");
    }

    private void SaveClaimedTiers()
    {
        string data = string.Join(",", claimedTiers);
        PlayerPrefs.SetString(moduleSaveID, data);
        PlayerPrefs.Save();
    }

    private void LoadClaimedTiers()
    {
        claimedTiers.Clear();
        if (PlayerPrefs.HasKey(moduleSaveID))
        {
            string data = PlayerPrefs.GetString(moduleSaveID);
            string[] tiers = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in tiers)
            {
                claimedTiers.Add(t);
            }
        }
    }
}