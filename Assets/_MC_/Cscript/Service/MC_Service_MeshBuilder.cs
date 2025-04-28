using MC_Chunk;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MC_Service_MeshBuilder : MonoBehaviour
{
    public void CreateChunkMesh(ChunkMeshData _MeshData, GameObject _Parent)
    {
        GameObject chunkObject = new GameObject(_MeshData._name);
        chunkObject.transform.parent = _Parent.transform;

        MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = _MeshData._name;
        mesh.vertices = _MeshData._vertices.ToArray();
        mesh.uv = _MeshData._uvs.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(_MeshData._triangles, 0);
        mesh.SetTriangles(_MeshData._triangles_Water, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        meshRenderer.sharedMaterials = new Material[]
        {
                MC_Runtime_StaticData.Instance.TerrainMatData.material_Terrain,
                MC_Runtime_StaticData.Instance.TerrainMatData.material_Water
        };
    }
}
