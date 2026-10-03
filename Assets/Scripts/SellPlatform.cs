using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SellPlatform : MonoBehaviour
{
    [SerializeField] private OreCatalog oreCatalog;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Text confirmationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private BackpackInventory nearbyInventory;
    private readonly HashSet<Collider> playerCollidersInRange = new HashSet<Collider>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConnectScenePlatform()
    {
        GameObject platform = GameObject.Find("Sell Platform");
        if (platform == null) return;

        SphereCollider platformSphere = platform.GetComponent<SphereCollider>();
        if (platformSphere != null)
        {
            platformSphere.isTrigger = false;
        }

        BoxCollider solidCollider = platform.GetComponents<BoxCollider>()
            .FirstOrDefault(collider => !collider.isTrigger);
        if (solidCollider == null)
        {
            solidCollider = platform.AddComponent<BoxCollider>();
        }
        solidCollider.isTrigger = false;
        solidCollider.size = new Vector3(1f, 10f, 1f);

        BoxCollider triggerCollider = platform.GetComponents<BoxCollider>()
            .FirstOrDefault(collider => collider.isTrigger);
        if (triggerCollider == null)
        {
            triggerCollider = platform.AddComponent<BoxCollider>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(3f, 30f, 3f);

        Rigidbody platformBody = platform.GetComponent<Rigidbody>();
        if (platformBody == null)
        {
            platformBody = platform.AddComponent<Rigidbody>();
        }
        platformBody.isKinematic = true;
        platformBody.useGravity = false;

        if (platform.GetComponent<SellPlatform>() == null)
        {
            platform.AddComponent<SellPlatform>();
        }
    }

    private void Update()
    {
        if (nearbyInventory != null
            && confirmationPanel != null
            && !confirmationPanel.activeSelf
            && Input.GetKeyDown(KeyCode.E))
        {
            ShowConfirmation();
        }
    }

    private void Awake()
    {
        if (oreCatalog == null)
        {
            oreCatalog = Resources.Load<OreCatalog>("Ores/OreCatalog");
        }

        if (confirmationPanel == null)
        {
            Debug.LogError("Assign the Sell Confirmation Panel from the Canvas to SellPlatform.", this);
            return;
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSale);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CancelSale);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        BackpackInventory inventory = other.GetComponentInParent<BackpackInventory>();
        if (inventory == null) return;

        playerCollidersInRange.Add(other);
        nearbyInventory = inventory;
        if (confirmationPanel != null && !confirmationPanel.activeSelf)
        {
            ShowConfirmation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerCollidersInRange.Remove(other) || nearbyInventory == null)
        {
            return;
        }

        foreach (Collider collider in playerCollidersInRange)
        {
            if (collider != null && collider.GetComponentInParent<BackpackInventory>() == nearbyInventory)
            {
                return;
            }
        }

        playerCollidersInRange.Clear();
        nearbyInventory = null;
        ClosePanel();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowConfirmation()
    {
        if (nearbyInventory == null || confirmationPanel == null) return;

        RefreshInventoryReference();
        confirmationPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ConfirmSale()
    {
        if (nearbyInventory != null
            && nearbyInventory.TrySellAllOre(out int soldAmount, out int earningsCents, oreCatalog))
        {
            Debug.Log($"Sold {soldAmount} stone for ${earningsCents / 100f:0.00}.", this);
        }

        ClosePanel();
    }

    private void CancelSale()
    {
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    private void RefreshInventoryReference()
    {
        if (confirmationText == null || nearbyInventory == null) return;

        int oreCount = nearbyInventory.OreCount;
        int saleValueCents = nearbyInventory.CalculateSaleValueCents();
        confirmationText.text = oreCount > 0
            ? $"Sell {oreCount} ore?\n{nearbyInventory.OreBreakdown}\nYou will get ${saleValueCents / 100f:0.00}"
            : "Your backpack is empty.\nCollect ore before selling.";

        if (confirmButton != null)
        {
            confirmButton.interactable = oreCount > 0;
        }
    }

}