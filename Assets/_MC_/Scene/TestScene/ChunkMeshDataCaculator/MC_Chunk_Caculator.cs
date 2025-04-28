using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MC_Chunk 
{

    #region DataStruct

    /// <summary>
    /// 存储一个区块必备的所有数据
    /// </summary>
    public class ChunkInfo
    {
        //设置
        public ChunkSetting _chunkSetting;

        //数据
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
    /// 存储单个方块的数据
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
    /// 存储整个区块的网格数据
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
    /// 存储整个区块的设置
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


        #region 周期函数

        static MC_Chunk_Caculator()
        {
            GetChunkRenderer();
            InitChunkData();
        }

        #endregion


        #region 区块生成

        private static Dictionary<Vector3, ChunkInfo> AllChunks;

        //初始化_AllChunks
        public static void InitChunkData()
        {
            
        }

        #region Create-GenChunk

        /// <summary>
        /// 首次生成区块，由World调用
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
        /// 编辑区块，由玩家触发，World调用
        /// </summary>
        /// <param name="_EditBlockInfo"></param>
        /// <param name="_chunkInfo"></param>
        public static void GenChunk_Edit(Vector3 _ChunkPos, ChunkSetting _chunkSetting, Dictionary<Vector3Int, BlockInfo> _EditBlockInfo, out ChunkInfo _chunkInfo)
        {
            _chunkInfo = new ChunkInfo();

            //判断Vector目标chunk是否存在
            //计算MeshData

        }

        /// <summary>
        /// 刷新区块
        /// 用于材质改变时等等重新生成
        /// </summary>
        /// <param name="_ChunkPos"></param>
        public static void GenChunk_Flash(Vector3 _ChunkPos)
        {

        }

        #endregion

        #region Create-InfoData

        //计算Chunk中BlockInfo数据
        private static void CaculateStructData()
        {

        }

        //修改数据
        private static void EditBlockInfo(Dictionary<Vector3, BlockInfo> _EditBlockInfo)
        {

        }


        #endregion

        #region Create-MeshData

        //计算Chunk所有的MeshData
        private static void CaculateGridData()
        {

        }

        //对于单个Block的Mesh计算
        private static void GenOneBlockMeshData(Vector3 _RelaPos)
        {
            //检查六个方向
            for (int _direct = 0; _direct < VoxelData.faceChecks.Length; _direct++)
            {
                //提前返回-如果不生成面
                if (!CheckFaceGen(_RelaPos, _direct))
                    return;

                GenFace(_RelaPos, _direct);
            }
        }

        //对于单个面的计算
        private static void GenFace(Vector3 _RelaPos, int _FaceCheckDirect)
        {

        }


        #endregion

        #region Create-ChunkRender

        //场景区块渲染器
        private static string _ChunkRenderObjectPATH = "MC_ChunkRenderer";
        private static MC_Chunk_Renderer Chunk_Renderer;

        // 获取单例对象
        public static MC_Chunk_Renderer GetChunkRenderer()
        {
            // 如果单例已经存在，直接返回
            if (Chunk_Renderer != null)
                return Chunk_Renderer;

            // 查找场景中的对象
            GameObject chunkRenderObject = GameObject.Find(_ChunkRenderObjectPATH);

            // 如果对象未找到，输出错误
            if (chunkRenderObject == null)
            {
                Debug.LogError($"Object at path {_ChunkRenderObjectPATH} not found!");
                return null;
            }

            // 获取该对象上的 MC_Chunk_Renderer 脚本
            Chunk_Renderer = chunkRenderObject.GetComponent<MC_Chunk_Renderer>();

            // 如果没有找到脚本，输出错误
            if (Chunk_Renderer == null)
            {
                Debug.LogError("MC_Chunk_Renderer component not found on the object!");
                return null;
            }

            // 返回单例实例
            return Chunk_Renderer;
        }

        #endregion

        #endregion


        #region 区块刷新

        private static HashSet<Vector3> WaitToFlashchunkList;

        #endregion


        #region 区块操作

        /// <summary>
        /// 清除所有区块信息
        /// </summary>
        public static void ClearAllChunk()
        {
            AllChunks.Clear();
        }

        /// <summary>
        /// 获取ChunkInfo
        /// </summary>
        public static ChunkInfo GetChunkInfo(Vector3 _chunkPos)
        {
            if (AllChunks.TryGetValue(_chunkPos, out ChunkInfo _resultChunkInfo))
                return _resultChunkInfo;
            else
                return null;
        }

        /// <summary>
        /// 修改ChunkInfo
        /// 修改之前可以先get值，修改后再传回去
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

        //是否可以生成面
        private static bool CheckFaceGen(Vector3 _RelaPos, int _FaceCheckDirect)
        {
            return true;
        }

        #endregion
    }
}