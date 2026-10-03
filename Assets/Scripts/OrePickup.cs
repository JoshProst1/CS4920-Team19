using UnityEngine;

public enum OreSize
{
    Small,
    Medium,
    Large
}

public class OrePickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private OreSize size = OreSize.Small;
    [SerializeField] private OreDefinition oreDefinition;
    [SerializeField, Min(0.5f)] private float pickupTriggerRadius = 2.2f;
    [SerializeField, Min(0f)] private float linearDamping = 1.5f;
    [SerializeField, Min(0f)] private float angularDamping = 3f;

    private Vector3 originalScale;
    private bool hasBeenPickedUp;

    public OreSize Size => size;
    public string DisplayName => $"{GetSizeName(size)} {BaseDisplayName}";
    public int SaleValueCents => BaseValueCents * GetSizeValueMultiplier(size);
    public Sprite Icon => oreDefinition != null ? oreDefinition.Icon : null;

    private string BaseDisplayName => oreDefinition != null ? oreDefinition.DisplayName : "Stone";
    private int BaseValueCents => oreDefinition != null ? oreDefinition.ValuePerUnitCents : 5;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (oreDefinition == null)
        {
            oreDefinition = Resources.Load<OreDefinition>("Ores/Stone");
        }
        ApplyOreMaterial();

        Rigidbody oreRigidbody = GetComponent<Rigidbody>();
        if (oreRigidbody != null)
        {
            oreRigidbody.isKinematic = false;
            oreRigidbody.useGravity = true;
            oreRigidbody.linearDamping = linearDamping;
            oreRigidbody.angularDamping = angularDamping;
        }

        SphereCollider pickupTrigger = GetComponent<SphereCollider>();
        if (pickupTrigger != null && pickupTrigger.isTrigger)
        {
            SphereCollider physicalCollider = gameObject.AddComponent<SphereCollider>();
            physicalCollider.radius = pickupTrigger.radius;
            physicalCollider.center = pickupTrigger.center;
            physicalCollider.isTrigger = false;
            pickupTrigger.radius = Mathf.Max(pickupTrigger.radius, pickupTriggerRadius);
        }
    }

    public void RandomizeSize()
    {
        float roll = Random.value;
        float scaleMultiplier;

        if (roll < 0.6f)
        {
            size = OreSize.Small;
            scaleMultiplier = 0.7f;
        }
        else if (roll < 0.9f)
        {
            size = OreSize.Medium;
            scaleMultiplier = 1f;
        }
        else
        {
            size = OreSize.Large;
            scaleMultiplier = 1.6f;
        }

        transform.localScale = originalScale * scaleMultiplier;
    }

    public static int GetSaleValueCents(OreSize oreSize)
    {
        return 5 * GetSizeValueMultiplier(oreSize);
    }

    public void SetOreDefinition(OreDefinition definition)
    {
        if (definition != null)
        {
            oreDefinition = definition;
            ApplyOreMaterial();
        }
    }

    public void SetDropMaterial(Material material)
    {
        if (material == null || (oreDefinition != null && oreDefinition.VisualMaterial != null)) return;

        Renderer oreRenderer = GetComponent<Renderer>();
        if (oreRenderer != null)
        {
            oreRenderer.sharedMaterial = material;
        }
    }

    private void ApplyOreMaterial()
    {
        if (oreDefinition == null || oreDefinition.VisualMaterial == null) return;

        Renderer oreRenderer = GetComponent<Renderer>();
        if (oreRenderer != null)
        {
            oreRenderer.sharedMaterial = oreDefinition.VisualMaterial;
        }
    }

    private static int GetSizeValueMultiplier(OreSize oreSize)
    {
        switch (oreSize)
        {
            case OreSize.Medium: return 2;
            case OreSize.Large: return 5;
            default: return 1;
        }
    }

    private static string GetSizeName(OreSize oreSize)
    {
        switch (oreSize)
        {
            case OreSize.Medium: return "Medium";
            case OreSize.Large: return "Large";
            default: return "Small";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryPickup(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryPickup(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryPickup(collision.collider);
    }

    private void TryPickup(Collider other)
    {
        if (hasBeenPickedUp
            || other == null
            || !(other.CompareTag(playerTag) || other.GetComponentInParent<PlayerMining>() != null))
        {
            return;
        }

        BackpackInventory inventory = other.GetComponentInParent<BackpackInventory>();
        if (inventory != null && inventory.TryAddOre(oreDefinition, DisplayName, SaleValueCents, Icon))
        {
            hasBeenPickedUp = true;
            foreach (Collider pickupCollider in GetComponents<Collider>())
            {
                pickupCollider.enabled = false;
            }

            Rigidbody oreRigidbody = GetComponent<Rigidbody>();
            if (oreRigidbody != null)
            {
                oreRigidbody.detectCollisions = false;
            }

            Destroy(gameObject);
        }
    }
}
