using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class ChippedOreMeshSetup
{
    private const string MeshPath = "Assets/Models/ChippedStone.asset";
    private const string PrefabPath = "Assets/Prefabs/Ore_Sphere.prefab";

    static ChippedOreMeshSetup()
    {
        EditorApplication.delayCall += EnsureChippedOreMesh;
    }

    [MenuItem("Tools/Mining/Apply Chipped Ore Shape")]
    private static void EnsureChippedOreMesh()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Mesh chippedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (chippedMesh == null)
        {
            chippedMesh = BuildChippedMesh();
            AssetDatabase.CreateAsset(chippedMesh, MeshPath);
            AssetDatabase.SaveAssets();
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabContents == null)
        {
            Debug.LogError("Could not open Ore_Sphere prefab to assign the chipped mesh.");
            return;
        }

        MeshFilter meshFilter = prefabContents.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("Ore_Sphere prefab needs a MeshFilter to display the chipped mesh.");
            PrefabUtility.UnloadPrefabContents(prefabContents);
            return;
        }

        if (meshFilter.sharedMesh != chippedMesh)
        {
            meshFilter.sharedMesh = chippedMesh;
            PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
    }

    private static Mesh BuildChippedMesh()
    {
        const float goldenRatio = 1.61803398875f;
        List<Vector3> baseVertices = new List<Vector3>
        {
            new Vector3(-1f, goldenRatio, 0f), new Vector3(1f, goldenRatio, 0f),
            new Vector3(-1f, -goldenRatio, 0f), new Vector3(1f, -goldenRatio, 0f),
            new Vector3(0f, -1f, goldenRatio), new Vector3(0f, 1f, goldenRatio),
            new Vector3(0f, -1f, -goldenRatio), new Vector3(0f, 1f, -goldenRatio),
            new Vector3(goldenRatio, 0f, -1f), new Vector3(goldenRatio, 0f, 1f),
            new Vector3(-goldenRatio, 0f, -1f), new Vector3(-goldenRatio, 0f, 1f)
        };

        int[] baseTriangles =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };

        for (int i = 0; i < baseVertices.Count; i++)
        {
            baseVertices[i] = baseVertices[i].normalized;
        }

        List<int> triangles = new List<int>(baseTriangles);
        Subdivide(baseVertices, triangles);

        System.Random random = new System.Random(18731);
        float[] radii = new float[baseVertices.Count];
        for (int i = 0; i < radii.Length; i++)
        {
            double roll = random.NextDouble();
            radii[i] = roll < 0.12
                ? 0.72f + (float)random.NextDouble() * 0.12f
                : 0.88f + (float)random.NextDouble() * 0.28f;
        }

        List<Vector3> vertices = new List<Vector3>(triangles.Count);
        List<Vector2> uv = new List<Vector2>(triangles.Count);
        List<int> flatTriangles = new List<int>(triangles.Count);

        foreach (int sourceIndex in triangles)
        {
            Vector3 direction = baseVertices[sourceIndex];
            Vector3 point = Vector3.Scale(direction * radii[sourceIndex], new Vector3(1.06f, 0.84f, 0.94f));
            vertices.Add(point);

            float u = Mathf.Atan2(direction.z, direction.x) / (2f * Mathf.PI) + 0.5f;
            float v = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI + 0.5f;
            uv.Add(new Vector2(u, v));
            flatTriangles.Add(flatTriangles.Count);
        }

        Mesh mesh = new Mesh
        {
            name = "Chipped Stone",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(flatTriangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void Subdivide(List<Vector3> vertices, List<int> triangles)
    {
        Dictionary<long, int> midpointCache = new Dictionary<long, int>();
        List<int> refined = new List<int>(triangles.Count * 4);

        int Midpoint(int first, int second)
        {
            int low = Mathf.Min(first, second);
            int high = Mathf.Max(first, second);
            long key = ((long)low << 32) + high;
            if (midpointCache.TryGetValue(key, out int found)) return found;

            int index = vertices.Count;
            vertices.Add((vertices[first] + vertices[second]).normalized);
            midpointCache.Add(key, index);
            return index;
        }

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int first = triangles[i];
            int second = triangles[i + 1];
            int third = triangles[i + 2];
            int a = Midpoint(first, second);
            int b = Midpoint(second, third);
            int c = Midpoint(third, first);

            refined.AddRange(new[]
            {
                first, a, c,
                second, b, a,
                third, c, b,
                a, b, c
            });
        }

        triangles.Clear();
        triangles.AddRange(refined);
    }
}
