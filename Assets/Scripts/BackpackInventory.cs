using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[Serializable]
public class OreStack
{
    public OreDefinition definition;
    public string displayName;
    public int unitValueCents;
    public Sprite icon;
    public int count;
}

public class BackpackInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 5;
    [SerializeField] private Text counterText;
    [SerializeField] private Text moneyText;

    private readonly List<OreStack> oreStacks = new List<OreStack>();
    private int moneyCents;

    public int OreCount
    {
        get
        {
            int total = 0;
            foreach (OreStack stack in oreStacks) total += stack.count;
            return total;
        }
    }
    public int Capacity => capacity;
    public int MoneyCents => moneyCents;
    public IReadOnlyList<OreStack> Stacks => oreStacks;
    public bool IsInventoryOpen { get; private set; }
    public string OreBreakdown
    {
        get
        {
            List<string> entries = new List<string>();
            foreach (OreStack stack in oreStacks)
            {
                if (stack.count > 0)
                {
                    entries.Add($"{stack.count} {stack.displayName} (${stack.unitValueCents / 100f:0.00} each)");
                }
            }

            return entries.Count > 0 ? string.Join(", ", entries) : "No ore";
        }
    }

    private void Awake()
    {
        capacity = Mathf.Max(1, capacity);
        if (moneyText == null)
        {
            GameObject moneyObject = GameObject.Find("Money");
            if (moneyObject != null)
            {
                moneyText = moneyObject.GetComponent<Text>();
            }
        }

        UpdateCounter();
        UpdateMoney();
    }

    public void SetInventoryOpen(bool isOpen)
    {
        IsInventoryOpen = isOpen;
    }

    public bool TryAddOre(int amount)
    {
        if (amount <= 0 || OreCount + amount > capacity)
        {
            return false;
        }

        for (int i = 0; i < amount; i++)
        {
            AddOreToStack("Small Stone", 5, null);
        }

        UpdateCounter();
        return true;
    }

    public bool TryAddOre(OreSize size)
    {
        int value = OrePickup.GetSaleValueCents(size);
        string sizeName = size == OreSize.Large ? "Large" : size == OreSize.Medium ? "Medium" : "Small";
        return TryAddOre($"{sizeName} Stone", value, null);
    }

    public bool TryAddOre(string displayName, int unitValueCents, Sprite icon)
    {
        return TryAddOre(null, displayName, unitValueCents, icon);
    }

    public bool TryAddOre(OreDefinition definition, string displayName, int unitValueCents, Sprite icon)
    {
        if (string.IsNullOrWhiteSpace(displayName) || unitValueCents < 0 || OreCount >= capacity)
        {
            return false;
        }

        AddOreToStack(definition, displayName, unitValueCents, icon);
        UpdateCounter();
        return true;
    }

    public bool TryRemoveOre(int amount)
    {
        if (amount <= 0 || amount > OreCount)
        {
            return false;
        }

        int remaining = amount;
        for (int i = oreStacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            int removed = Mathf.Min(oreStacks[i].count, remaining);
            oreStacks[i].count -= removed;
            remaining -= removed;
            if (oreStacks[i].count == 0) oreStacks.RemoveAt(i);
        }

        UpdateCounter();
        return true;
    }

    public int CalculateSaleValueCents(OreCatalog catalog = null)
    {
        int totalCents = 0;
        foreach (OreStack stack in oreStacks)
        {
            if (catalog == null || catalog.Contains(stack.definition))
            {
                totalCents += stack.count * stack.unitValueCents;
            }
        }

        return totalCents;
    }

    public bool TrySellAllOre(out int soldAmount, out int earningsCents, OreCatalog catalog = null)
    {
        soldAmount = 0;
        earningsCents = 0;

        int sellableCount = 0;
        foreach (OreStack stack in oreStacks)
        {
            if (catalog == null || catalog.Contains(stack.definition))
            {
                sellableCount += stack.count;
            }
        }

        if (sellableCount <= 0)
        {
            return false;
        }

        soldAmount = sellableCount;
        earningsCents = CalculateSaleValueCents(catalog);
        if (catalog == null)
        {
            oreStacks.Clear();
        }
        else
        {
            oreStacks.RemoveAll(stack => catalog.Contains(stack.definition));
        }

        moneyCents += earningsCents;
        UpdateCounter();
        UpdateMoney();
        return true;
    }

    private void UpdateCounter()
    {
        if (counterText != null)
        {
            counterText.text = $"ORE   {OreCount} / {capacity}";
        }
    }

    private void AddOreToStack(string displayName, int unitValueCents, Sprite icon)
    {
        AddOreToStack(null, displayName, unitValueCents, icon);
    }

    private void AddOreToStack(OreDefinition definition, string displayName, int unitValueCents, Sprite icon)
    {
        foreach (OreStack stack in oreStacks)
        {
            if (stack.definition == definition && stack.displayName == displayName && stack.unitValueCents == unitValueCents)
            {
                stack.count++;
                return;
            }
        }

        oreStacks.Add(new OreStack
        {
            definition = definition,
            displayName = displayName,
            unitValueCents = unitValueCents,
            icon = icon,
            count = 1
        });
    }

    private void UpdateMoney()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${moneyCents / 100f:0.00}";
        }
    }
}