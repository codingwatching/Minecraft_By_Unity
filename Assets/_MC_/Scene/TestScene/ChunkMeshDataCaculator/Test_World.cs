using System.Collections.Generic;
using UnityEngine;

namespace MC_Chunk
{
    public class Test_World : MonoBehaviour
    {

        public GameObject ChunkParent;

        private void Awake()
        {
            
        }

        private void Start()
        {
            CreateChunk(Vector3.zero);
        }

        void CreateChunk(Vector3 _ChunkPos)
        {
            //Éú³ÉChunk
            ChunkSetting chunkSetting = new ChunkSetting(new Vector3(4f, 4f, 4f), 0);
            MC_Chunk_Caculator.GenChunk_Init(_ChunkPos, chunkSetting, out ChunkInfo _ResultChunkInfo);
        }

    }

}

