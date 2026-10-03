using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MountainVoxelizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void VoxelizeMountain()
    {
        if (!Application.isPlaying) return;

        GameObject[] mountainRoots = SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .Where(root => root.name.StartsWith("Mountain Exterior - Voxel Rock"))
            .ToArray();
        if (mountainRoots.Length == 0) return;

        GameObject mountainRoot = mountainRoots[0];
        List<Transform> oldChunks = mountainRoots
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(child => child.name != "Mountain Voxel" && child.GetComponent<Renderer>() != null)
            .Distinct()
            .ToList();
        List<Transform> activeChunks = oldChunks
            .Where(child => child.gameObject.activeInHierarchy)
            .ToList();
        if (activeChunks.Count == 0) return;

        MineableBlock sourceBlock = Resources.FindObjectsOfTypeAll<MineableBlock>()
            .FirstOrDefault(block => block.gameObject.scene.IsValid()
                && block.gameObject.scene.isLoaded
                && block.OreDropPrefab != null);
        GameObject oreDropPrefab = sourceBlock != null
            ? sourceBlock.OreDropPrefab
            : Resources.Load<GameObject>("Ore_Sphere");
        OreDefinition stoneDefinition = sourceBlock != null && sourceBlock.OreDefinition != null
            ? sourceBlock.OreDefinition
            : Resources.Load<OreDefinition>("Ores/Stone");

        if (oreDropPrefab == null)
        {
            Debug.LogError("Mountain voxelization stopped: Ore_Sphere could not be loaded from Resources.");
            return;
        }

        int mineableLayer = LayerMask.NameToLayer("Mineable");
        if (mineableLayer < 0)
        {
            Debug.LogError("Mountain voxelization stopped: the Mineable layer does not exist.");
            return;
        }

        List<Vector3> voxelPositions = new List<Vector3>();
        HashSet<Vector3Int> occupiedVoxels = new HashSet<Vector3Int>();
        foreach (Transform chunk in activeChunks)
        {
            Vector3 size = chunk.lossyScale;
            Vector3 minimum = chunk.position - size * 0.5f;

            for (int x = 0; x < Mathf.RoundToInt(size.x); x++)
            {
                for (int y = 0; y < Mathf.RoundToInt(size.y); y++)
                {
                    for (int z = 0; z < Mathf.RoundToInt(size.z); z++)
                    {
                        AddVoxel(minimum + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                    }
                }
            }
        }

        AddSmallEntrance(mountainRoot.transform, activeChunks, occupiedVoxels, voxelPositions);

        Material stoneMaterial = activeChunks[0].GetComponent<Renderer>()?.sharedMaterial;
        Material[] depthMaterials =
        {
            Resources.Load<Material>("MountainRockMaterials/MountainRock_Outer"),
            Resources.Load<Material>("MountainRockMaterials/MountainRock_Mid"),
            Resources.Load<Material>("MountainRockMaterials/MountainRock_Deep"),
            Resources.Load<Material>("MountainRockMaterials/MountainRock_Core")
        };

        Material fallbackMaterial = depthMaterials.FirstOrDefault(material => material != null)
            ?? stoneMaterial;
        if (fallbackMaterial == null)
        {
            Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (fallbackShader != null)
            {
                fallbackMaterial = new Material(fallbackShader);
                fallbackMaterial.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        for (int i = 0; i < depthMaterials.Length; i++)
        {
            if (depthMaterials[i] == null)
            {
                depthMaterials[i] = fallbackMaterial;
            }
        }

        if (depthMaterials.Any(material => material == null))
        {
            Debug.LogError("Mountain voxelization stopped: no valid rock material or fallback shader was available.");
            return;
        }

        Dictionary<Vector3Int, int> surfaceDepths = CalculateSurfaceDepths(occupiedVoxels);
        OreCatalog oreCatalog = Resources.Load<OreCatalog>("Ores/OreCatalog");
        OreDefinition[] oreDefinitions = oreCatalog != null && oreCatalog.Ores.Count > 0
            ? oreCatalog.Ores.Where(definition => definition != null).ToArray()
            : Resources.LoadAll<OreDefinition>("Ores");
        OreVeinRegion[] veinRegions = Object.FindObjectsByType<OreVeinRegion>(FindObjectsSortMode.None)
            .Where(region => region.gameObject.scene.IsValid() && region.gameObject.scene.isLoaded)
            .OrderByDescending(region => region.Priority)
            .ToArray();
        foreach (Vector3 position in voxelPositions)
        {
            GameObject voxel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            voxel.name = "Mountain Voxel";
            voxel.layer = mineableLayer;
            voxel.transform.SetParent(mountainRoot.transform, true);
            voxel.transform.position = position;
            voxel.transform.rotation = Quaternion.identity;
            voxel.transform.localScale = Vector3.one;

            Renderer voxelRenderer = voxel.GetComponent<Renderer>();
            Vector3Int key = PositionToKey(position);
            int depth = surfaceDepths.TryGetValue(key, out int measuredDepth) ? measuredDepth : 0;
            int materialIndex = depth == 0 ? 0 : depth <= 2 ? 1 : depth <= 5 ? 2 : 3;
            OreDefinition oreDefinition = null;
            foreach (OreVeinRegion region in veinRegions)
            {
                if (region.Contains(position))
                {
                    oreDefinition = region.Definition;
                    break;
                }
            }

            if (oreDefinition == null)
            {
                oreDefinition = ChooseOreForDepth(oreDefinitions, depth, stoneDefinition);
            }

            voxelRenderer.sharedMaterial = oreDefinition != null && oreDefinition.VeinMaterial != null
                ? oreDefinition.VeinMaterial
                : depthMaterials[materialIndex];

            MineableBlock mineableBlock = voxel.AddComponent<MineableBlock>();
            mineableBlock.ConfigureAsMountainVoxel(oreDropPrefab, 0.5f, oreDefinition);
        }

        foreach (Transform chunk in oldChunks)
        {
            Object.Destroy(chunk.gameObject);
        }
        for (int i = 1; i < mountainRoots.Length; i++)
        {
            Object.Destroy(mountainRoots[i]);
        }

        Debug.Log($"Mountain converted to {voxelPositions.Count} individual mineable voxels with a 2-by-2 entrance.");

        void AddVoxel(Vector3 position)
        {
            Vector3Int key = PositionToKey(position);
            if (occupiedVoxels.Add(key))
            {
                voxelPositions.Add(position);
            }
        }
    }

    private static Dictionary<Vector3Int, int> CalculateSurfaceDepths(HashSet<Vector3Int> occupiedVoxels)
    {
        Vector3Int[] neighbors =
        {
            new Vector3Int(2, 0, 0),
            new Vector3Int(-2, 0, 0),
            new Vector3Int(0, 2, 0),
            new Vector3Int(0, -2, 0),
            new Vector3Int(0, 0, 2),
            new Vector3Int(0, 0, -2)
        };

        Dictionary<Vector3Int, int> depths = new Dictionary<Vector3Int, int>(occupiedVoxels.Count);
        Queue<Vector3Int> queue = new Queue<Vector3Int>();

        foreach (Vector3Int voxel in occupiedVoxels)
        {
            if (neighbors.Any(offset => !occupiedVoxels.Contains(voxel + offset)))
            {
                depths.Add(voxel, 0);
                queue.Enqueue(voxel);
            }
        }

        while (queue.Count > 0)
        {
            Vector3Int voxel = queue.Dequeue();
            int nextDepth = depths[voxel] + 1;

            foreach (Vector3Int offset in neighbors)
            {
                Vector3Int neighbor = voxel + offset;
                if (occupiedVoxels.Contains(neighbor) && !depths.ContainsKey(neighbor))
                {
                    depths.Add(neighbor, nextDepth);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return depths;
    }

    private static Vector3Int PositionToKey(Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x * 2f),
            Mathf.RoundToInt(position.y * 2f),
            Mathf.RoundToInt(position.z * 2f));
    }

    private static OreDefinition ChooseOreForDepth(OreDefinition[] definitions, int depth, OreDefinition fallback)
    {
        float totalWeight = 0f;
        foreach (OreDefinition definition in definitions)
        {
            if (definition != null && definition.MinimumDepth <= depth)
            {
                totalWeight += definition.DropWeight;
            }
        }

        if (totalWeight <= 0f) return fallback;

        float roll = Random.value * totalWeight;
        foreach (OreDefinition definition in definitions)
        {
            if (definition == null || definition.MinimumDepth > depth || definition.DropWeight <= 0f)
            {
                continue;
            }

            roll -= definition.DropWeight;
            if (roll <= 0f) return definition;
        }

        return fallback;
    }

    private static void AddSmallEntrance(
        Transform mountainRoot,
        List<Transform> activeChunks,
        HashSet<Vector3Int> occupiedVoxels,
        List<Vector3> voxelPositions)
    {
        GameObject player = GameObject.Find("Player");
        Vector3 directionToPlayer = player != null
            ? player.transform.position - mountainRoot.position
            : Vector3.forward;

        Vector3 outward = Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.z)
            ? (directionToPlayer.x >= 0f ? Vector3.right : Vector3.left)
            : (directionToPlayer.z >= 0f ? Vector3.forward : Vector3.back);
        Vector3 right = Vector3.Cross(Vector3.up, outward);

        float frontPlane = float.NegativeInfinity;
        foreach (Transform chunk in activeChunks)
        {
            Vector3 size = chunk.lossyScale;
            float projectedHalfSize = Mathf.Abs(outward.x) * size.x * 0.5f
                + Mathf.Abs(outward.z) * size.z * 0.5f;
            frontPlane = Mathf.Max(frontPlane, Vector3.Dot(chunk.position, outward) + projectedHalfSize);
        }

        const int wallWidth = 6;
        const int wallHeight = 5;
        const int tunnelDepth = 8;

        for (int side = 0; side < wallWidth; side++)
        {
            float horizontalOffset = side - (wallWidth - 1) * 0.5f;
            for (int height = 0; height < wallHeight; height++)
            {
                bool tunnelOpening = side == 2 || side == 3;
                tunnelOpening &= height < 2;
                if (tunnelOpening) continue;

                for (int depth = 0; depth < tunnelDepth; depth++)
                {
                    Vector3 position = mountainRoot.position
                        + right * horizontalOffset
                        + Vector3.up * (height + 0.5f)
                        + outward * (frontPlane - Vector3.Dot(mountainRoot.position, outward) - depth - 0.5f);
                    Vector3Int key = new Vector3Int(
                        Mathf.RoundToInt(position.x * 2f),
                        Mathf.RoundToInt(position.y * 2f),
                        Mathf.RoundToInt(position.z * 2f));
                    if (occupiedVoxels.Add(key))
                    {
                        voxelPositions.Add(position);
                    }
                }
            }
        }
    }
}
