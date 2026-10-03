using UnityEngine;

[CreateAssetMenu(menuName = "Mining/Ore Definition", fileName = "NewOre")]
public class OreDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Stone";
    [SerializeField, Min(0)] private int valuePerUnitCents = 5;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Material visualMaterial;
    [SerializeField] private Material veinMaterial;
    [SerializeField, Min(0)] private int minimumDepth;
    [SerializeField, Min(0f)] private float dropWeight = 100f;

    private Sprite generatedIcon;

    public string DisplayName => displayName;
    public int ValuePerUnitCents => valuePerUnitCents;
    public Sprite Icon
    {
        get
        {
            if (icon != null) return icon;
            if (generatedIcon != null) return generatedIcon;

            Texture texture = visualMaterial != null
                ? visualMaterial.mainTexture
                : pickupPrefab != null ? pickupPrefab.GetComponentInChildren<Renderer>()?.sharedMaterial?.mainTexture : null;
            if (texture is Texture2D texture2D)
            {
                generatedIcon = Sprite.Create(
                    texture2D,
                    new Rect(0f, 0f, texture2D.width, texture2D.height),
                    new Vector2(0.5f, 0.5f));
            }

            return generatedIcon;
        }
    }
    public GameObject PickupPrefab => pickupPrefab;
    public Material VisualMaterial => visualMaterial;
    public Material VeinMaterial => veinMaterial;
    public int MinimumDepth => minimumDepth;
    public float DropWeight => dropWeight;
}