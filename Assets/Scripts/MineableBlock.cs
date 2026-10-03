using UnityEngine;

public class MineableBlock : MonoBehaviour
{
    [Header("Block Attributes")]
    [SerializeField] private string blockName = "Stone";
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private GameObject oreDropPrefab;
    [SerializeField] private OreDefinition oreDefinition;
    [SerializeField] private float fallDestroyDelay = 5f;

    private float currentHealth;
    private bool isBroken;

    public GameObject OreDropPrefab => oreDropPrefab;
    public OreDefinition OreDefinition => oreDefinition;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isBroken || damage <= 0f) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            BreakBlock();
        }
    }

    public void ConfigureAsMountainVoxel(GameObject dropPrefab, float destroyDelay, OreDefinition definition = null)
    {
        blockName = "Mountain Stone";
        oreDropPrefab = dropPrefab;
        oreDefinition = definition;
        fallDestroyDelay = destroyDelay;
        currentHealth = maxHealth;
    }

    private void BreakBlock()
    {
        isBroken = true;

        if (oreDropPrefab == null)
        {
            oreDropPrefab = Resources.Load<GameObject>("Ore_Sphere");
        }

        GameObject selectedDropPrefab = oreDefinition != null && oreDefinition.PickupPrefab != null
            ? oreDefinition.PickupPrefab
            : oreDropPrefab;
        if (selectedDropPrefab != null && selectedDropPrefab.GetComponent<OrePickup>() == null)
        {
            Debug.LogWarning($"Pickup prefab {selectedDropPrefab.name} needs an OrePickup component; using the default ore sphere instead.", selectedDropPrefab);
            selectedDropPrefab = oreDropPrefab;
        }

        if (selectedDropPrefab != null)
        {
            Material blockMaterial = GetComponent<Renderer>()?.sharedMaterial;
            GameObject droppedOre = Instantiate(selectedDropPrefab, transform.position, Quaternion.identity);
            OrePickup orePickup = droppedOre.GetComponent<OrePickup>();
            if (orePickup != null)
            {
                orePickup.SetOreDefinition(oreDefinition);
                orePickup.SetDropMaterial(blockMaterial);
                orePickup.RandomizeSize();
            }
            else
            {
                Debug.LogWarning($"Pickup prefab {selectedDropPrefab.name} needs an OrePickup component.", selectedDropPrefab);
            }
        }

        Collider blockCollider = GetComponent<Collider>();
        if (blockCollider == null)
        {
            Debug.LogWarning($"{name} needs a Collider to fall correctly.", this);
        }

        Rigidbody blockRigidbody = GetComponent<Rigidbody>();
        if (blockRigidbody == null)
        {
            blockRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        blockRigidbody.isKinematic = false;
        blockRigidbody.useGravity = true;
        Destroy(gameObject, fallDestroyDelay);
    }
}
