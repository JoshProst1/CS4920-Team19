using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mining/Ore Catalog", fileName = "OreCatalog")]
public class OreCatalog : ScriptableObject
{
    [SerializeField] private OreDefinition[] ores = new OreDefinition[0];

    public IReadOnlyList<OreDefinition> Ores => ores;

    public bool Contains(OreDefinition definition)
    {
        if (definition == null) return false;

        foreach (OreDefinition ore in ores)
        {
            if (ore == definition) return true;
        }

        return false;
    }
}