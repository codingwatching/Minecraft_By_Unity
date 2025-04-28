using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MC_Chunk 
{

    #region DataStruct

    /// <summary>
    /// �洢һ������ر�����������
    /// </summary>
    public class ChunkInfo
    {
        //����
        public ChunkSetting _chunkSetting;

        //����
        public Dictionary<Vector3Int, BlockInfo> _blockInfos;
        public ChunkMeshData _chunkMeshData;
        
        //Object
        public GameObject _chunkObject;

        public ChunkInfo()
        {
            _blockInfos = new Dictionary<Vector3Int, BlockInfo>();
            _chunkMeshData = new ChunkMeshData();
            _chunkSetting = new ChunkSetting();
            _chunkObject = null;
        }

    }

    /// <summary>
    /// �洢�������������
    /// </summary>
    public class BlockInfo
    {
        public byte _blockType;
        public MC_Static_NewChunk.BlockOriented _blockOriented;

        public BlockInfo()
        {
            _blockType = VoxelData.Air;
            _blockOriented = MC_Static_NewChunk.BlockOriented.North;
        }

        public BlockInfo(byte blockType, MC_Static_NewChunk.BlockOriented blockOriented)
        {
            _blockType = blockType;
            _blockOriented = blockOriented;
        }
    }

    /// <summary>
    /// �洢�����������������
    /// </summary>
    public class ChunkMeshData
    {
        public string _name;
        public List<Vector3> _vertices;
        public List<int> _triangles;
        public List<int> _triangles_Water;
        public List<Vector2> _uvs;

        public ChunkMeshData()
        {
            _vertices = new List<Vector3>();
            _triangles = new List<int>();
            _triangles_Water = new List<int>();
            _uvs = new List<Vector2>();
        }

        public ChunkMeshData(string name, List<Vector3> vertices, List<int> triangles, List<int> triangles_Water, List<Vector2> uvs)
        {
            _name = name;
            _vertices = vertices;
            _triangles = triangles;
            _triangles_Water = triangles_Water;
            _uvs = uvs;
        }
    }

    /// <summary>
    /// �洢�������������
    /// </summary>
    public class ChunkSetting
    {
        public Vector3 _size;
        public int _seed;

        public ChunkSetting()
        {
            _size = new Vector3(2, 2, 2);
            _seed = 0;
        }

        public ChunkSetting(Vector3 size, int seed)
        {
            _size = size;
            _seed = seed;
        }
    }

    #endregion



    public static class MC_Chunk_Caculator
    {


        #region ���ں���

        static MC_Chunk_Caculator()
        {
            GetChunkRenderer();
            InitChunkData();
        }

        #endregion


        #region ��������

        private static Dictionary<Vector3, ChunkInfo> AllChunks;

        //��ʼ��_AllChunks
        public static void InitChunkData()
        {
            
        }

        #region Create-GenChunk

        /// <summary>
        /// �״��������飬��World����
        /// </summary>
        /// <param name="_ChunkPos"></param>
        /// <param name="_chunkSetting"></param>
        /// <param name="_chunkInfo"></param>
        public static void GenChunk_Init(Vector3 _ChunkPos, ChunkSetting _chunkSetting, out ChunkInfo _chunkInfo)
        {
            _chunkInfo = new ChunkInfo();

            CaculateStructData();
            CaculateGridData();
            Chunk_Renderer.CreateChunkMesh(_chunkInfo._chunkMeshData, "", out var _chunkObj);
            AllChunks[_ChunkPos]._chunkObject = _chunkObj;
        }

        /// <summary>
        /// �༭���飬����Ҵ�����World����
        /// </summary>
        /// <param name="_EditBlockInfo"></param>
        /// <param name="_chunkInfo"></param>
        public static void GenChunk_Edit(Vector3 _ChunkPos, ChunkSetting _chunkSetting, Dictionary<Vector3Int, BlockInfo> _EditBlockInfo, out ChunkInfo _chunkInfo)
        {
            _chunkInfo = new ChunkInfo();

            //�ж�VectorĿ��chunk�Ƿ����
            //����MeshData

        }

        /// <summary>
        /// ˢ������
        /// ���ڲ��ʸı�ʱ�ȵ���������
        /// </summary>
        /// <param name="_ChunkPos"></param>
        public static void GenChunk_Flash(Vector3 _ChunkPos)
        {

        }

        #endregion

        #region Create-InfoData

        //����Chunk��BlockInfo����
        private static void CaculateStructData()
        {

        }

        //�޸�����
        private static void EditBlockInfo(Dictionary<Vector3, BlockInfo> _EditBlockInfo)
        {

        }


        #endregion

        #region Create-MeshData

        //����Chunk���е�MeshData
        private static void CaculateGridData()
        {

        }

        //���ڵ���Block��Mesh����
        private static void GenOneBlockMeshData(Vector3 _RelaPos)
        {
            //�����������
            for (int _direct = 0; _direct < VoxelData.faceChecks.Length; _direct++)
            {
                //��ǰ����-�����������
                if (!CheckFaceGen(_RelaPos, _direct))
                    return;

                GenFace(_RelaPos, _direct);
            }
        }

        //���ڵ�����ļ���
        private static void GenFace(Vector3 _RelaPos, int _FaceCheckDirect)
        {

        }


        #endregion

        #region Create-ChunkRender

        //����������Ⱦ��
        private static string _ChunkRenderObjectPATH = "MC_ChunkRenderer";
        private static MC_Chunk_Renderer Chunk_Renderer;

        // ��ȡ��������
        public static MC_Chunk_Renderer GetChunkRenderer()
        {
            // ��������Ѿ����ڣ�ֱ�ӷ���
            if (Chunk_Renderer != null)
                return Chunk_Renderer;

            // ���ҳ����еĶ���
            GameObject chunkRenderObject = GameObject.Find(_ChunkRenderObjectPATH);

            // �������δ�ҵ����������
            if (chunkRenderObject == null)
            {
                Debug.LogError($"Object at path {_ChunkRenderObjectPATH} not found!");
                return null;
            }

            // ��ȡ�ö����ϵ� MC_Chunk_Renderer �ű�
            Chunk_Renderer = chunkRenderObject.GetComponent<MC_Chunk_Renderer>();

            // ���û���ҵ��ű����������
            if (Chunk_Renderer == null)
            {
                Debug.LogError("MC_Chunk_Renderer component not found on the object!");
                return null;
            }

            // ���ص���ʵ��
            return Chunk_Renderer;
        }

        #endregion

        #endregion


        #region ����ˢ��

        private static HashSet<Vector3> WaitToFlashchunkList;

        #endregion


        #region �������

        /// <summary>
        /// �������������Ϣ
        /// </summary>
        public static void ClearAllChunk()
        {
            AllChunks.Clear();
        }

        /// <summary>
        /// ��ȡChunkInfo
        /// </summary>
        public static ChunkInfo GetChunkInfo(Vector3 _chunkPos)
        {
            if (AllChunks.TryGetValue(_chunkPos, out ChunkInfo _resultChunkInfo))
                return _resultChunkInfo;
            else
                return null;
        }

        /// <summary>
        /// �޸�ChunkInfo
        /// �޸�֮ǰ������getֵ���޸ĺ��ٴ���ȥ
        /// </summary>
        /// <param name="_chunkPos"></param>
        /// <param name="_chunkInfo"></param>
        public static void EditChunkInfo(Vector3 _chunkPos, ChunkInfo _chunkInfo)
        {

        }


        public static void HideChunk()
        {

        }

        #endregion


        #region Tools

        //�Ƿ����������
        private static bool CheckFaceGen(Vector3 _RelaPos, int _FaceCheckDirect)
        {
            return true;
        }

        #endregion
    }
}