using UnityEngine;

public class PlayerMining : MonoBehaviour
{
    [Header("Mining Controls")]
    [SerializeField] private float mineRange = 4.5f;
    [SerializeField] private float miningDamage = 1f;
    [SerializeField] private float swingRate = 0.35f;
    [SerializeField] private LayerMask mineableLayer;
    [SerializeField, Min(0)] private int miningRadius;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private float nextSwingTime;
    private BackpackInventory backpackInventory;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        backpackInventory = GetComponent<BackpackInventory>();
        if (backpackInventory == null)
        {
            backpackInventory = gameObject.AddComponent<BackpackInventory>();
        }
    }

    private void Update()
    {
        if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked) return;
        if (backpackInventory != null && backpackInventory.IsInventoryOpen) return;

        if (Input.GetMouseButton(0) && Time.time >= nextSwingTime)
        {
            TryMine();
            nextSwingTime = Time.time + swingRate;
        }
    }

    private void TryMine()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, mineRange, mineableLayer))
        {
            MineableBlock block = hit.collider.GetComponentInParent<MineableBlock>();
            if (block != null)
            {
                MineArea(block);
            }
        }
    }

    public void SetMiningRadius(int radius)
    {
        miningRadius = Mathf.Max(0, radius);
    }

    private void MineArea(MineableBlock targetBlock)
    {
        if (miningRadius == 0)
        {
            targetBlock.TakeDamage(miningDamage);
            return;
        }

        float halfExtent = miningRadius + 0.49f;
        Collider[] nearbyColliders = Physics.OverlapBox(
            targetBlock.transform.position,
            Vector3.one * halfExtent,
            Quaternion.identity,
            mineableLayer);

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            MineableBlock nearbyBlock = nearbyCollider.GetComponentInParent<MineableBlock>();
            if (nearbyBlock != null)
            {
                nearbyBlock.TakeDamage(miningDamage);
            }
        }
    }
}
