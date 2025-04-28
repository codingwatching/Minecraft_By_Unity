using UnityEngine;
using MC_Chunk;

namespace MC_Chunk
{
    public class MC_Chunk_Renderer : MonoBehaviour
    { 
        public void CreateChunkMesh(ChunkMeshData _MeshData, string _ChunkParent, out GameObject _outObj)
        {
            GameObject chunkObject = new GameObject(_MeshData._name);
            _outObj = chunkObject;
            GameObject chunkparent = GameObject.Find(_ChunkParent);
            if (chunkparent != null)
            {
                chunkObject.transform.parent = chunkparent.transform;
            }
            else
            {
                Debug.LogWarning("找不到渲染区块的父类地址，已暂存于ChunkRenderer下");
                chunkObject.transform.parent = this.transform;
            }
            

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

}

