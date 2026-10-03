using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
    [SerializeField] private BackpackInventory inventory;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text headingText;
    [SerializeField] private InventoryStackRow[] rows;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<BackpackInventory>();
        }
        if (inventory != null)
        {
            inventory.SetInventoryOpen(false);
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && panel != null)
        {
            bool open = !panel.activeSelf;
            panel.SetActive(open);
            if (inventory != null)
            {
                inventory.SetInventoryOpen(open);
            }
        }

        if (panel != null && panel.activeSelf)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (inventory == null) return;

        if (headingText != null)
        {
            headingText.text = $"BACKPACK   {inventory.OreCount} / {inventory.Capacity}";
        }

        for (int i = 0; i < rows.Length; i++)
        {
            OreStack stack = i < inventory.Stacks.Count ? inventory.Stacks[i] : null;
            if (rows[i] != null)
            {
                rows[i].SetStack(stack);
            }
        }
    }
}
