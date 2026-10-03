using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OreVeinRegion : MonoBehaviour
{
    [SerializeField] private OreDefinition oreDefinition;
    [SerializeField] private int priority;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.9f, 0.85f);

    private BoxCollider regionCollider;

    public OreDefinition Definition => oreDefinition;
    public int Priority => priority;

    private void Awake()
    {
        regionCollider = GetComponent<BoxCollider>();
        regionCollider.isTrigger = true;
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (regionCollider == null) regionCollider = GetComponent<BoxCollider>();
        return oreDefinition != null && regionCollider != null && regionCollider.bounds.Contains(worldPosition);
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = gizmoColor;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;
    }
}
