using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using System;
using Homebrew;
using MCEntity;
using static MC_UtilityFunctions;


//全局游戏状态
public enum Game_State
{

    Start, Loading, Playing, Pause, Ending,

}

public enum GameMode
{

    Creative, Survival,

}


public enum DrawMode
{

    Block,Bush,Torch,Air,Water,SnowPower,HalfBrick,Door,Tool,Null,

}


public enum Facing2d
{
    None, front,back, left, right,
}

//public enum WorldType
//{
//    默认, 超平坦世界, 草原群系, 高原群系, 沙漠群系, 沼泽群系, 密林群系,
//}

public enum FaceCheck_Enum
{
    isSolid, appointType, appointDrawmode
}


public enum BlockClassfy
{
    全部方块 = 0,
    建筑方块 = 1,
    功能性方块 = 2,
    工具 = 3,
    食物 = 4,
    禁用 = 5,
}



public class World : MonoBehaviour
{
    //[Header("Debug")]
    //public bool 低区块模式; private bool hasExec_低区块模式 = true;
    //public bool 无黑夜模式; private bool hasExec_无黑夜模式 = true;
    [HideInInspector] public bool 是否生成Chunk侧面 = false;

    [Header("引用")]
    public ManagerHub managerhub;
    public CanvasManager canvasManager;
    public Player player;

    [Header("世界存档")]
    [HideInInspector] public bool isFinishSaving = false;
    [HideInInspector] public String savingPATH = ""; //存档根目录
    [HideInInspector] public WorldSetting worldSetting;
    [HideInInspector] public List<SavingData> TheSaving = new List<SavingData>(); //读取的存档
    [HideInInspector] public List<EditStruct> EditNumber = new List<EditStruct>(); //玩家数据
    //public List<SavingData> savingDatas = new List<SavingData>();//最终保存数据

    [Header("游戏状态")]
    [ReadOnly]public Game_State game_state = Game_State.Start;
    [ReadOnly] public GameMode game_mode = GameMode.Survival;
    [HideInInspector] public bool isLoadSaving = false;
    //public bool SuperPlainMode = false; 


    [Header("Material-方块类型 + 工具类型")]
    public Material material;
    public Material material_Water;
    public BlockType[] blocktypes;


    [Header("World-渲染设置")]
    [Tooltip("4就是边长为4*16的正方形")] public int renderSize = 5;        //渲染区块半径,即renderSize*16f
    [Tooltip("2就是接近2*16的时候开始刷新区块")] public float StartToRender = 1f;
    public float DestroySize = 7f;


    [Header("Cave-洞穴系统")]
    //noise3d_scale越大洞穴破损越严重，越大越好
    //cave_width越小洞穴平均宽度变小，不能太大，不然全是洞
    //public bool debug_CanLookCave = false;
    public float noise3d_scale = 0.085f;
    public float cave_width = 0.45f;


    [Header("群系特征概率和数据(值越大范围越小)")]
    public float 氧气浓度OxygenDensity;
    public float 三维密度Density3d;
    public float 干燥程度Aridity;
    public float 空气湿度MoistureLevel;
    public BiomeNoiseSystem[] biomenoisesystems;


    [Header("地质分层与概率系统(n%)(矿物为万分之n)")]
    public TerrainLayerProbabilitySystem terrainLayerProbabilitySystem;


    //玩家
    [Header("Player-玩家脚底坐标")]
    public Transform PlayerFoot;
    public Vector3 Start_Position = new Vector3(1600f, 127f, 1600f);


    //全部Chunk位置
    public Dictionary<Vector3, Chunk> Allchunks = new Dictionary<Vector3, Chunk>();
    private readonly object Allchunks_Lock = new object();


    //等待添加队列
    //private List<chunkWithsequence> WatingToCreateChunks = new List<chunkWithsequence>();
    private List<Vector3> WatingToCreate_Chunks = new List<Vector3>();


    //等待删除队列
    private List<Vector3> WatingToRemove_Chunks = new List<Vector3>();
    private Chunk obj;

    //纹理集
    public Texture2D BlocksatlasTexture;

    //协程
    [Header("Corountine-协程延迟时间")]
    public float InitCorountineDelay = 1f;
    public float CreateCoroutineDelay = 0.5f;
    public float RemoveCoroutineDelay = 0.5f;
    public float RenderDelay = 0.1f;
    public int Mesh_0_TaskCount = 0;


    [Header("水面流动渲染线程")]
    private Thread myThread_Water;
    public int Delay_RenderFlowWater = 5;
    public float MaxDistant_RenderFlowWater = 5;


    [Header("[暂时未启用]强加载渲染线程(延迟为毫秒)")]
    public Thread myThread_Render;
    public int Delay_RenderMesh = 1000;
    public ConcurrentQueue<Chunk> WaitToRender_New = new ConcurrentQueue<Chunk>();



    //生成方向
    private Vector3 Center_Now;
    private Vector3 Center_direction; //这个代表了方向


    //计时
    public float InitStartTime;
    public float InitEndTime;

    //UI Manager
    //[HideInInspector]
    //public float initprogress = 0f;


    //Chunks父级
    [HideInInspector] public GameObject Chunks;


    //一次性代码
    bool hasExec = true;
    bool hasExec_SetSeed = true;


    //Create && Remove 协程
    Coroutine CreateCoroutine;
    Coroutine RemoveCoroutine;


    //Render_0 && Render_1 协程
    [HideInInspector] public bool RenderLock = false;
    public ConcurrentQueue<Chunk> WaitToRender = new ConcurrentQueue<Chunk>();
    public ConcurrentQueue<Chunk> WaitToRender_temp = new ConcurrentQueue<Chunk>();
    Coroutine Render_Coroutine;


    //Threading
    [HideInInspector] public bool MeshLock = false;
    public ConcurrentQueue<Chunk> WaitToCreateMesh = new ConcurrentQueue<Chunk>();
    Coroutine Mesh_Coroutine;

    //Flash
    public ConcurrentQueue<Chunk> WaitToFlashChunkQueue = new ConcurrentQueue<Chunk>();

    //Init
    //[HideInInspector] public bool InitError = false;


    //----------------------------------周期函数---------------------------------------




    private void Start()
    {

        InitWorldManager();

    }

    public void InitWorldManager()
    {
        hasExec_RandomPlayerLocation = true;
        game_mode = GameMode.Survival;


        isLoadSaving = false;
        是否生成Chunk侧面 = managerhub.是否生成Chunk侧面;

        // 使用 persistentDataPath 作为根目录
        savingPATH = Path.Combine(Application.persistentDataPath);

        // 确保目录存在
        if (!Directory.Exists(savingPATH))
        {
            Directory.CreateDirectory(savingPATH);
        }

        // 打印目录路径以确认
        //Debug.Log("存档目录: " + savingPATH);


        //初始化



        game_state = Game_State.Start;
        TheSaving = new List<SavingData>();
        EditNumber = new List<EditStruct>();
        //savingDatas = new List<SavingData>();
        renderSize = 10;
        StartToRender = 1f;
        DestroySize = 7f;
        Start_Position = new Vector3(1600f, 127f, 1600f);
        Allchunks = new Dictionary<Vector3, Chunk>();
        WatingToCreate_Chunks = new List<Vector3>();
        WatingToRemove_Chunks = new List<Vector3>();
        myThread_Render = null;
        WaitToRender_New = new ConcurrentQueue<Chunk>();
        //是否生成Chunk侧面 = false;
        Center_Now = Vector3.zero;
        Center_direction = Vector3.zero;
        hasExec = true;
        hasExec_SetSeed = true;
        CreateCoroutine = null;
        RemoveCoroutine = null;
        Render_Coroutine = null;
        Mesh_Coroutine = null;
        MeshLock = false;
        WaitToCreateMesh = new ConcurrentQueue<Chunk>();
        RenderLock = false;
        WaitToRender = new ConcurrentQueue<Chunk>();
        WaitToRender_temp = new ConcurrentQueue<Chunk>();
        WaitToFlashChunkQueue = new ConcurrentQueue<Chunk>();

        //Self
        if (Chunks == null)
        {
            Chunks = new GameObject();
            Chunks.name = "Chunks";
            Chunks.transform.SetParent(GameObject.Find("Environment").transform);
        }

        // 销毁 Chunks 大纲目录下的所有子物体
        foreach (Transform child in Chunks.transform)
        {
            Destroy(child.gameObject);
        }

        //-------顺序不能变化------------------
        terrainLayerProbabilitySystem.Seed = UnityEngine.Random.Range(0, 100000000);
        worldSetting = new WorldSetting(terrainLayerProbabilitySystem.Seed);
        UnityEngine.Random.InitState(worldSetting.seed);
        //-------------------------------------

        PointSaving = "";

        //初始化计数时间
        InitStartTime = 0f;
        InitEndTime = 0f;
}

    private void FixedUpdate()
    {

        //Mesh线程常驻
        CreateMeshCoroutineManager();


        //渲染线程常驻
        RenderCoroutineManager();



        //初始化地图
        if (game_state == Game_State.Loading)
        {

            if (hasExec_SetSeed)
            {
                InitStartTime = Time.time;
                //获取当前模式
                //if (canvasManager.currentWorldType == 6)
                //{
                //    //SuperPlainMode = true;
                //}

                //worldSetting.worldtype = canvasManager.currentWorldType

                //开始初始化
                Update_CenterChunks(true);

                hasExec_SetSeed = false;
            }


        }


        //游戏开始
        if (game_state == Game_State.Playing)
        {

            if (hasExec)
            {
                
                LockMouse(true);
                hasExec = false;
            }


            //玩家移动刷新
            //如果大于16f
            if (GetVector3Length(PlayerFoot.transform.position - Center_Now) > (StartToRender * 16f) && GetVector3Length(PlayerFoot.transform.position - Center_Now) <= ((StartToRender + 1) * 16f))
            {

                //更新Center
                Center_direction = VtoNormal(PlayerFoot.transform.position - Center_Now);
                Center_Now += Center_direction * TerrainData.ChunkWidth;

                //添加Chunk
                AddtoCreateChunks(Center_direction);
                AddtoRemoveChunks(Center_direction);

            }
            //玩家移动过远距离
            else if (GetVector3Length(PlayerFoot.transform.position - Center_Now) > ((StartToRender + 1) * 16f))
            {



                Update_CenterWithNoInit();




            }


        }

    }

  

    void OnApplicationQuit()
    {

        //结束游戏
        game_state = Game_State.Ending;

        //print("Quit");
        //RenderSettings.skybox.SetFloat("_Exposure", 1f);

        //等待Water线程
        if (myThread_Water != null && myThread_Water.IsAlive)
        {

            myThread_Water.Join(); // 等待线程安全地终止

        }

        //等待Render线程
        if (myThread_Render != null && myThread_Render.IsAlive)
        {

            myThread_Render.Join(); // 等待线程安全地终止

        }

    }

    private void Update()
    {
        if (managerhub.低区块模式 && managerhub.hasExec_低区块模式)
        {
            renderSize = 2;

            managerhub.hasExec_低区块模式 = false;
        }

        if (managerhub.无黑夜模式 && managerhub.hasExec_无黑夜模式)
        {

            managerhub.timeManager.gameObject.SetActive(false);

            managerhub.hasExec_无黑夜模式 = false;
        }

        if (game_state == Game_State.Playing)
        {
            _ReferUpdate_CheckShowEntityHitbox();
        }
    }


    bool hasExec_ShowEntityHitbox = true;
    void _ReferUpdate_CheckShowEntityHitbox()
    {

        if (player.ShowEntityHitbox)
        {
            if (hasExec_ShowEntityHitbox)
            {

                SwitchAllEntityHitbox(true);

                hasExec_ShowEntityHitbox = false;
            }
        }
        else
        {
            if (hasExec_ShowEntityHitbox == false)
            {
                SwitchAllEntityHitbox(false);
                hasExec_ShowEntityHitbox = true;
            }
        }

        
    }

    //检查所有碰撞盒，并打开他们的hitbox选项
    void SwitchAllEntityHitbox(bool _bool)
    {
        foreach (var item in AllEntity)
        {
            item._obj.GetComponent<MC_Collider_Component>().isDrawHitBox = _bool;
        }
    }


    //---------------------------------------------------------------------------------------










    //----------------------------------World Options---------------------------------------




    //主菜单地图
    //public void Start_Screen_Init()
    //{

    //    Chunk chunk_temp = new Chunk(new Vector3(5, 0, 2), this ,true, false);
    //    //Chunk chunk_temp1 = new Chunk(new Vector3(3, 0, 2), this, true);
    //    //Chunk chunk_temp2 = new Chunk(new Vector3(3, 0, 2), this, true);


    //    //for (float x = 0; x < 5; x ++)
    //    //{
    //    //    for (float z = 0; z < 5; z++)
    //    //    {
    //    //        Chunk chunk_temp3 = new Chunk(new Vector3(x, 0, z), this, true);
    //    //    }
    //    //}
    //    //GameObject chunkGameObject = new GameObject("TheMenuChunk");
    //    //Chunk chunk = chunkGameObject.AddComponent<Chunk>();
    //    //chunk.InitChunk(new Vector3(0, 0, 0), this);

    //}




    //初始化地图
    public Coroutine Init_MapCoroutine;
    bool hasExec_RandomPlayerLocation = true;
    IEnumerator Init_Map_Thread(bool _isInitPlayerLocation)
    {


        //确定玩家圈养中心点
        if (isLoadSaving)
        {

            Center_Now = new Vector3(GetRealChunkLocation(worldSetting.playerposition).x, 0, GetRealChunkLocation(worldSetting.playerposition).z);

        }
        else
        {
            if (hasExec_RandomPlayerLocation)
            {
                player.RandomPlayerLocaiton();
                hasExec_RandomPlayerLocation = false;
            }
            //print(PlayerFoot.transform.position);
            Center_Now = new Vector3(GetRealChunkLocation(PlayerFoot.transform.position).x, 0, GetRealChunkLocation(PlayerFoot.transform.position).z);

        }




        //写一个协程，清除或者隐藏过远的区块


        //print($"Center:{Center_Now}");
        //print($"Foot:{PlayerFoot.transform.position}, ChunkFoot:{GetChunkLocation(PlayerFoot.transform.position)}");
        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.position = Center_Now;
        //sphere.transform.localScale = new Vector3(2f, 2f, 2f);
        float temp = 0f;

        for (int x = -renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x < renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x++)
        {

            for (int z = -renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z < renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z++)
            {

                //Create
                CreateBaseChunk(new Vector3(x, 0, z));

                //剩余进度计算
                float max = renderSize * renderSize * 4;
                temp++;
                canvasManager.Initprogress = Mathf.Lerp(0f, 0.9f, temp / max);

                yield return new WaitForSeconds(InitCorountineDelay);
            }

        }

        //yield return new WaitForSeconds(0.5f);

        //重新初始化玩家位置，防止穿模
        if (_isInitPlayerLocation)
        {
            player.InitPlayerLocation();
        }
        

        //游戏开始
        yield return new WaitForSeconds(0.5f);
        canvasManager.Initprogress = 1f;

        //开启面优化协程
        StartCoroutine(Chunk_Optimization());

        StartCoroutine(FlashChunkCoroutine());

        //开启持续渲染水体流动线程
        //myThread_Water = new Thread(new ThreadStart(Thread_AwaysUpdate_Water));
        //myThread_Water.Start();

        //开启渲染Mesh线程
        //myThread_Render = new Thread(new ThreadStart(Thread_RenderMesh));
        //myThread_Render.Start();

        Init_MapCoroutine = null;
    }


    Coroutine Init_Map_Thread_NoInit_Coroutine;

    public void Update_CenterWithNoInit()
    {
        if (Init_Map_Thread_NoInit_Coroutine == null)
        {
            //print("玩家移动太快！Center_Now已更新");
            Init_Map_Thread_NoInit_Coroutine = StartCoroutine(Init_Map_Thread_NoInit());

            //managerhub.timeManager.UpdateDayFogDistance();
            HideFarChunks();
        }
    }

    IEnumerator Init_Map_Thread_NoInit()
    {

        Center_Now = new Vector3(GetRealChunkLocation(PlayerFoot.transform.position).x, 0, GetRealChunkLocation(PlayerFoot.transform.position).z);

        for (int x = -renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x < renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x++)
        {

            for (int z = -renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z < renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z++)
            {

                //Create
                CreateBaseChunk(new Vector3(x, 0, z));

                yield return new WaitForSeconds(InitCorountineDelay);
            }

        }

        Init_Map_Thread_NoInit_Coroutine = null;

    }


    //更新中心区块
    public void Update_CenterChunks(bool _isInitPlayerLocation)
    {
        //print("更新中心区块");
        //update加载中心区块
        if (Init_MapCoroutine == null)
        {
            Init_MapCoroutine = StartCoroutine(Init_Map_Thread(_isInitPlayerLocation));
        }


    }


    public void HideFarChunks()
    {
        foreach (var temp in Allchunks)
        {
            if (GetVector3Length(PlayerFoot.transform.position - temp.Value.myposition) > (StartToRender * 16f))
            {
                temp.Value.HideChunk();
            }
        }
    }



    //清除过远区块
    IEnumerator Update_FarChunks()
    {

        //如果过远就隐藏
        yield return null;

    }



    //优化Chunk面数协程
    //本质上是把BaseChunk全部重新生成一遍

    public IEnumerator Chunk_Optimization()
    {

        foreach (var Chunk in Allchunks)
        {

            WaitToCreateMesh.Enqueue(Chunk.Value);

        }



        yield return new WaitForSeconds(1f);

    }


    //负责把接收的到的Chunk用多线程刷新
    public IEnumerator FlashChunkCoroutine()
    {

        while (true)
        {

            if (WaitToFlashChunkQueue.TryDequeue(out Chunk chunktemp))
            {
                Thread myThread = new Thread(new ThreadStart(chunktemp.UpdateChunkMesh_WithSurround));
                myThread.Start();
            }


            yield return null;
        }



    }



    //一直更新水的线程
    //void Thread_AwaysUpdate_Water()
    //{
    //    int 次数 = 0;
    //    int 个数 = 0;

    //    //一直循环
    //    while (game_state != Game_State.Ending)
    //    {

    //        lock (Allchunks_Lock)
    //        {

    //            //遍历所有AllChunks
    //            foreach (var chunktemp in Allchunks)
    //            {

    //                //如果区块包含水，且在12区块内，则更新
    //                if (chunktemp.Value.iHaveWater && GetVector3Length(chunktemp.Value.myposition - Center_Now) > MaxDistant_RenderFlowWater)
    //                {

    //                    chunktemp.Value.Always_updateWater();
    //                    //print($"刷新了{chunktemp.Value.name}");

    //                    个数++;

    //                }

    //            }

    //        }






    //        //休眠5秒钟
    //        次数++;
    //        print($"第{次数}次刷新，一共刷新{个数}个");
    //        个数 = 0;
    //        Thread.Sleep(Delay_RenderFlowWater * 1000);

    //    }

    //    Debug.LogError("Water线程中止");

    //}




    //--------------------------------------------------------------------------------------





    //-----------------------------------Create 协程----------------------------------------




    //添加到等待添加队列
    void AddtoCreateChunks(Vector3 add_vec)
    {

        //ForWard
        if (add_vec == new Vector3(0, 0, 1))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) + Center_direction * (renderSize - 1);

            //新增Chunk
            for (int i = -renderSize; i < renderSize; i++)
            {
                //CreateChunk(add_vec + new Vector3((float)i, 0, 0));

                WatingToCreate_Chunks.Add(add_vec + new Vector3((float)i, 0, 0));


            }

            //呼叫里侧Chunk更新
            for (int i = -renderSize; i < renderSize; i++)
            {
                //CreateChunk(add_vec + new Vector3((float)i, 0, 0));

                lock (Allchunks_Lock)
                {

                    if (Allchunks.TryGetValue(add_vec + new Vector3((float)i, 0, -1), out Chunk chunktemp))
                        WaitToCreateMesh.Enqueue(chunktemp);

                }





            }

        }

        //Back
        if (add_vec == new Vector3(0, 0, -1))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) + Center_direction * (renderSize);

            for (int i = -renderSize; i < renderSize; i++)
            {

                WatingToCreate_Chunks.Add(add_vec + new Vector3((float)i, 0, 0));

            }

            //呼叫里侧Chunk更新
            for (int i = -renderSize; i < renderSize; i++)
            {

                //CreateChunk(add_vec + new Vector3((float)i, 0, 0));
                lock (Allchunks_Lock)
                {

                    if (Allchunks.TryGetValue(add_vec + new Vector3((float)i, 0, 1), out Chunk chunktemp))
                        WaitToCreateMesh.Enqueue(chunktemp);

                }




            }

        }

        //Left
        if (add_vec == new Vector3(-1, 0, 0))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) + Center_direction * (renderSize);

            for (int i = -renderSize; i < renderSize; i++)
            {

                //CreateChunk(add_vec + new Vector3(0, 0, (float)i));
                WatingToCreate_Chunks.Add(add_vec + new Vector3(0, 0, (float)i));

            }

            //呼叫里侧Chunk更新
            for (int i = -renderSize; i < renderSize; i++)
            {

                lock (Allchunks_Lock)
                {

                    //CreateChunk(add_vec + new Vector3((float)i, 0, 0));
                    if (Allchunks.TryGetValue(add_vec + new Vector3(1, 0, (float)i), out Chunk chunktemp))
                        WaitToCreateMesh.Enqueue(chunktemp);

                }



            }

        }



        //Right
        if (add_vec == new Vector3(1, 0, 0))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) + Center_direction * (renderSize - 1);

            for (int i = -renderSize; i < renderSize; i++)
            {

                //CreateChunk(add_vec + new Vector3(0, 0, (float)i));
                WatingToCreate_Chunks.Add(add_vec + new Vector3(0, 0, (float)i));

            }


            //呼叫里侧Chunk更新
            for (int i = -renderSize; i < renderSize; i++)
            {

                lock (Allchunks_Lock)
                {

                    //CreateChunk(add_vec + new Vector3((float)i, 0, 0));
                    if (Allchunks.TryGetValue(add_vec + new Vector3(-1, 0, (float)i), out Chunk chunktemp))
                        WaitToCreateMesh.Enqueue(chunktemp);

                }



            }

        }

        //Debug.Log("已经添加坐标");


        //判断是否启动协程
        //先两次添加再启动协程，后面数据多了一次启动协程
        if (WatingToCreate_Chunks.Count > 0 && CreateCoroutine == null)
        {

            CreateCoroutine = StartCoroutine(CreateChunksQueue());

        }


    }



    //协程：创造Chunk
    private IEnumerator CreateChunksQueue()
    {

        while (true)
        {

            yield return new WaitForSeconds(CreateCoroutineDelay);


            //如果队列中有数据，就读取
            //如果队列中没有数据，就关闭协程
            if (WatingToCreate_Chunks.Count > 0)
            {

                //如果查到的chunk已经存在，则唤醒
                //不存在则生成
                if (Allchunks.TryGetValue(WatingToCreate_Chunks[0], out obj))
                {

                    if (obj.isShow == false)
                    {

                        obj.ShowChunk();

                    }

                }
                else
                {

                    CreateChunk(WatingToCreate_Chunks[0]);

                }

                WatingToCreate_Chunks.RemoveAt(0);

            }
            else
            {

                CreateCoroutine = null;
                break;

            }



        }

    }



    //生成Chunk
    //BaseChunk不会进行自身剔除
    void CreateBaseChunk(Vector3 pos)
    {

        //先判断一下有没有
        if (Allchunks.ContainsKey(pos))
        {
            Allchunks[pos].ShowChunk();
            return;
        }

        //调用Chunk
        Vector3 _ChunkLocation = new Vector3(Mathf.FloorToInt(pos.x), 0, Mathf.FloorToInt(pos.z));
        Chunk _chunk_temp;


        //if (_ChunkLocation == new Vector3(195f,0,89f))
        //{
        //    print("");
        //}

        //调用Chunk
        if (ContainsChunkLocation(_ChunkLocation)) 
        {
            _chunk_temp = new Chunk(_ChunkLocation, managerhub, false, TheSaving[GetIndexOfChunkLocation(_ChunkLocation)].EditDataInChunkList);
        }
        else
        {
            _chunk_temp = new Chunk(_ChunkLocation, managerhub, false);
        }

        //GameObject chunkGameObject = new GameObject($"{Mathf.FloorToInt(pos.x)}, 0, {Mathf.FloorToInt(pos.z)}");
        //Chunk chunktemp = chunkGameObject.AddComponent<Chunk>();
        //chunktemp.InitChunk(new Vector3(0, 0, 0), this);

        //添加到字典
        Allchunks.Add(pos, _chunk_temp);

    }



    //非BaseChunk会进行Chunk面剔除
    void CreateChunk(Vector3 pos)
    {

        //先判断一下有没有
        if (Allchunks.ContainsKey(pos))
        {

            Allchunks[pos].ShowChunk();
            return;

        }


        Vector3 _ChunkLocation = new Vector3(Mathf.FloorToInt(pos.x), 0, Mathf.FloorToInt(pos.z));
        Chunk _chunk_temp;

        //调用Chunk
        if (ContainsChunkLocation(_ChunkLocation))
        {
            _chunk_temp = new Chunk(_ChunkLocation, managerhub, false, TheSaving[GetIndexOfChunkLocation(_ChunkLocation)].EditDataInChunkList);
        }
        else
        {
            _chunk_temp = new Chunk(_ChunkLocation, managerhub, false);
        }


        //GameObject chunkGameObject = new GameObject($"{Mathf.FloorToInt(pos.x)}, 0, {Mathf.FloorToInt(pos.z)}");
        //Chunk chunktemp = chunkGameObject.AddComponent<Chunk>();
        //chunktemp.InitChunk(new Vector3(0, 0, 0), this);

        //添加到字典
        Allchunks.Add(pos, _chunk_temp);

    }




    //--------------------------------------------------------------------------------------







    //-----------------------------------Remove 协程-----------------------------------------




    //添加到等待删除队列
    void AddtoRemoveChunks(Vector3 add_vec)
    {

        //ForWard
        if (add_vec == new Vector3(0, 0, 1))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) - Center_direction * (renderSize + 1);

            for (int i = -renderSize; i < renderSize; i++)
            {

                WatingToRemove_Chunks.Add(add_vec + new Vector3((float)i, 0, 0));

            }

        }



        //Back
        if (add_vec == new Vector3(0, 0, -1))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) - Center_direction * (renderSize);

            for (int i = -renderSize; i < renderSize; i++)
            {

                WatingToRemove_Chunks.Add(add_vec + new Vector3((float)i, 0, 0));

            }

        }



        //Left
        if (add_vec == new Vector3(-1, 0, 0))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) - Center_direction * (renderSize);

            for (int i = -renderSize; i < renderSize; i++)
            {

                WatingToRemove_Chunks.Add(add_vec + new Vector3(0, 0, (float)i));

            }

        }

        //Right
        if (add_vec == new Vector3(1, 0, 0))
        {

            add_vec = (Center_Now / TerrainData.ChunkWidth) - Center_direction * (renderSize + 1);

            for (int i = -renderSize; i < renderSize; i++)
            {

                WatingToRemove_Chunks.Add(add_vec + new Vector3(0, 0, (float)i));

            }

        }

        //判断是否启动协程
        //先两次添加再启动协程，后面数据多了一次启动协程
        if (WatingToRemove_Chunks.Count > 0 && RemoveCoroutine == null)
        {

            RemoveCoroutine = StartCoroutine(RemoveChunksQueue());

        }

    }


    //协程：删除ChunK
    private IEnumerator RemoveChunksQueue()
    {

        while (true)
        {

            yield return new WaitForSeconds(RemoveCoroutineDelay);


            //如果队列中有数据，就读取
            //如果队列中没有数据，就关闭协程
            if (WatingToRemove_Chunks.Count > 0)
            {

                if (Allchunks.TryGetValue(WatingToRemove_Chunks[0], out obj))
                {

                    Chunk_HideOrRemove(WatingToRemove_Chunks[0]);

                    WatingToRemove_Chunks.RemoveAt(0);

                }
                else
                {

                    WatingToRemove_Chunks.RemoveAt(0);
                    WatingToRemove_Chunks.Add(WatingToRemove_Chunks[0]);

                }


            }
            else
            {

                RemoveCoroutine = null;
                break;

            }



        }

    }



    void Chunk_HideOrRemove(Vector3 chunklocation)
    {

        //如果超出范围就卸载,否则就隐藏
        //if (GetVector3Length(PlayerFoot.transform.position - chunklocation) > (DestroySize * 16f))
        //{
        //    Allchunks.Remove(chunklocation);
        //    obj.DestroyChunk();


        //}
        //else
        //{
        //    obj.HideChunk();
        //}

        obj.HideChunk();


    }




    //---------------------------------------------------------------------------------------





    //-----------------------------------Render 协程-----------------------------------------




    //渲染协程池
    void RenderCoroutineManager()
    {
        // 如果等待渲染的队列不为空，并且没有正在运行的渲染协程
        if (WaitToRender.Count != 0 && Render_Coroutine == null)
        {
            //print($"启动渲染协程");
            Render_Coroutine = StartCoroutine(Render_0());
        }
    }

    // 一条渲染协程
    IEnumerator Render_0()
    {
        bool hasError = false;  // 标记是否发生异常

        while (true)
        {
            try
            {
                // 尝试从队列中取出要渲染的Chunk
                if (WaitToRender.TryDequeue(out Chunk chunktemp))
                {
                    //print($"{GetChunkLocation(chunktemp.myposition)}开始渲染");

                    // 如果Chunk已经准备好渲染，调用CreateMesh
                    if (chunktemp.isReadyToRender)
                    {
                        chunktemp.CreateMesh();
                    }
                }

                // 如果队列为空，停止协程
                if (WaitToRender.Count == 0)
                {
                    //print($"队列为空，停止协程");
                    Render_Coroutine = null;
                    RenderLock = false;
                    break;
                }
            }
            catch (System.Exception ex)
            {
                // 捕获异常，防止协程因异常终止
                Debug.LogError($"渲染协程出错: {ex.Message}\n{ex.StackTrace}");

                hasError = true;  // 标记发生错误
                break;  // 退出当前循环，等待后重新启动
            }

            // 正常情况等待一段时间以控制渲染频率
            yield return new WaitForSeconds(RenderDelay);
        }

        // 如果发生了异常，等待并重启协程
        if (hasError)
        {
            Render_Coroutine = null;  // 重置协程状态
            yield return new WaitForSeconds(1f);  // 等待一段时间
            RenderCoroutineManager();  // 重新启动渲染协程
        }
    }




    //单位为ms，根据每次渲染，动态改变渲染时间
    //void dynamicRandertime(float nowtime)
    //{

    //    if (nowtime > RenderDelay)
    //    {

    //        RenderDelay = nowtime;

    //    }

    //}



    //Mesh协程
    void CreateMeshCoroutineManager()
    {

        if (WaitToCreateMesh.Count != 0 && Mesh_Coroutine == null)
        {

            Mesh_Coroutine = StartCoroutine(Mesh_0());

        }

    }


    bool hasExec_CaculateInitTime = true;
    public float OneChunkRenderTime;
    IEnumerator Mesh_0()
    {

        while (true)
        {

            if (MeshLock == false)
            {

                MeshLock = true;

                WaitToCreateMesh.TryDequeue(out Chunk chunktemp);

                //print($"{GetChunkLocation(chunktemp.myposition)}添加到meshQueue");

                //Mesh线程
                Thread myThread = new Thread(new ThreadStart(chunktemp.UpdateChunkMesh_WithSurround));
                myThread.Start();

                if (WaitToCreateMesh.Count == 0)
                {
                    if (hasExec_CaculateInitTime)
                    {
                        //print("渲染完了");
                        InitEndTime = Time.time;

                        //renderSize * renderSize * 4是总区块数，2是因为面剔除渲染了两次
                        OneChunkRenderTime = (InitEndTime - InitStartTime) / (renderSize * renderSize * 4 * 2);

                        hasExec_CaculateInitTime = false;
                    }
                    
                    Mesh_Coroutine = null;
                    break;

                }




            }

            Mesh_0_TaskCount = WaitToCreateMesh.Count;
            //print("WaitToCreateMesh.Count");
            yield return new WaitForSeconds(RenderDelay);


        }







    }



    //新的渲染管线
    void Thread_RenderMesh()
    {

        while (game_state != Game_State.Ending)
        {

            if (WaitToRender_New.TryDequeue(out Chunk chunktemp))
            {

                if (chunktemp.isReadyToRender)
                {

                    chunktemp.CreateMesh();

                }

            }

            Thread.Sleep(Delay_RenderMesh);

        }

        Debug.LogError("Render线程中止");

    }



    //---------------------------------------------------------------------------------------






    //----------------------------------Player Options---------------------------------------

    //简单噪声
    public float GetSimpleNoise(int _x, int _z, Vector3 _myposition)
    {
        float smoothNoise = Mathf.Lerp((float)0, (float)1, Mathf.PerlinNoise((_x + _myposition.x) * 0.01f, (_z + _myposition.z) * 0.01f));
        return smoothNoise;
    }

    //简单偏移噪声，用来给树等分布用
    //_offset类似为Vector3(111f,222f)
    //_Scale噪声缩放:0.01为正常缩放,0.1为水下沙泥分布
    public float GetSimpleNoiseWithOffset(int _x, int _z, Vector3 _myposition, Vector2 _Offset, float _Scale)
    {
        float smoothNoise = Mathf.Lerp((float)0, (float)1, Mathf.PerlinNoise((_x + _myposition.x + _Offset.x) * _Scale, (_z + _myposition.z + _Offset.y) * _Scale));
        return smoothNoise;
    }

    //获得所在群系类型
    public byte GetBiomeType(int _x, int _z, Vector3 _myposition)
    {

        float _A = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(0f, 0f, 0f));
        float _B = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(123f, 0f, 456f));
        float _C = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(789f, 0f, 123f));
        float _D = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(456f, 0f, 789f));

        ////沙漠
        //if (_C >= 干燥程度Aridity)
        //{

        //    return 2;

        //}

        //else
        //{

        //    //高原
        //    if (_B >= 三维密度Density3d)
        //    {
        //        return 1;
        //    }

        //    //草原
        //    else if (_A >= 氧气浓度OxygenDensity)
        //    {

        //        if (_D >= 空气湿度MoistureLevel)
        //        {
        //            return 3;
        //        }
        //        else
        //        {
        //            return 0;
        //        }



        //    }
        //    else
        //    {
        //        //返回密林
        //        return 4;
        //    }

        //}

        //高原
        if (_B >= 三维密度Density3d)
        {
            return 1;
        }

        else
        {

            //沙漠
            if (_C >= 干燥程度Aridity)
            {

                return 2;

            }

            //草原
            else if (_A >= 氧气浓度OxygenDensity)
            {

                if (_D >= 空气湿度MoistureLevel)
                {
                    return 3;
                }
                else
                {
                    return 0;
                }



            }
            else
            {
                //返回密林
                return 4;
            }

        }


    }


    //根据给定参数和群系种类
    //变成给定的群系噪声
    public float GetTotalNoiseHigh_Biome(int _x, int _z, Vector3 _myposition, int _WorldType)
    {
        if (_x < 0 || _x > TerrainData.ChunkWidth || _z < 0 || _z > TerrainData.ChunkWidth)
        {
            print($"GetTotalNoiseHigh_Biome出界,{_x},{_z}");
            return 128f;
        }

        if (_WorldType == TerrainData.Biome_SuperPlain)
        {
            return 0f;
        }


        //默认
        if (_WorldType == TerrainData.Biome_Default)
        {
            //Noise
            float noise_1 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[0].Noise_Scale_123.x, (float)(_z + _myposition.z) * biomenoisesystems[0].Noise_Scale_123.x);
            float noise_2 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[0].Noise_Scale_123.y, (float)(_z + _myposition.z) * biomenoisesystems[0].Noise_Scale_123.y);
            float noise_3 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[0].Noise_Scale_123.z, (float)(_z + _myposition.z) * biomenoisesystems[0].Noise_Scale_123.z);
            float noise = Mathf.Lerp(0f, 1f, noise_1 * biomenoisesystems[0].Noise_Rank_123.x + noise_2 * biomenoisesystems[0].Noise_Rank_123.y + noise_3 * biomenoisesystems[0].Noise_Rank_123.z);
            float noise_High = Mathf.Lerp(biomenoisesystems[0].HighDomain.x, biomenoisesystems[0].HighDomain.y, noise);

            //数据定义
            int BiomeType = -1;
            float BiomeIntensity = 0f;
            float _A = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(0f, 0f, 0f));
            float _B = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(123f, 0f, 456f));
            float _C = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(789f, 0f, 123f));
            float _D = GetSimpleNoise((int)(_x + _myposition.x), (int)(_z + _myposition.z), new Vector3(456f, 0f, 789f));

            //获得当前群系
            //获得群系混合强度
            //高原
            if (_B >= 三维密度Density3d)
            {
                BiomeType = TerrainData.Biome_Plateau;
                BiomeIntensity = Mathf.InverseLerp(三维密度Density3d, 1f, _B);
            }
            else
            {

                if (_C >= 干燥程度Aridity)
                {
                    BiomeType = TerrainData.Biome_Dessert;
                    BiomeIntensity = Mathf.InverseLerp(干燥程度Aridity, 1f, _C);
                }
                //草原
                else if (_A >= 氧气浓度OxygenDensity)
                {
                    if (_D >= 空气湿度MoistureLevel)
                    {
                        BiomeType = TerrainData.Biome_Marsh;
                        BiomeIntensity = Mathf.InverseLerp(空气湿度MoistureLevel, 1f, _D);
                    }
                    else
                    {
                        BiomeType = TerrainData.Biome_Plain;
                        BiomeIntensity = Mathf.InverseLerp(氧气浓度OxygenDensity, 1f, _A);
                    }
                }
                else
                {
                    BiomeType = TerrainData.Biome_Plain;
                    BiomeIntensity = Mathf.InverseLerp(氧气浓度OxygenDensity, 1f, _A);
                }

            }

            //BiomeType = 1;

            //混合群系
            float Mixnoise_1 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[BiomeType].Noise_Scale_123.x, (float)(_z + _myposition.z) * biomenoisesystems[BiomeType].Noise_Scale_123.x);
            float Mixnoise_2 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[BiomeType].Noise_Scale_123.y, (float)(_z + _myposition.z) * biomenoisesystems[BiomeType].Noise_Scale_123.y);
            float Mixnoise_3 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[BiomeType].Noise_Scale_123.z, (float)(_z + _myposition.z) * biomenoisesystems[BiomeType].Noise_Scale_123.z);
            float Mixnoise = Mathf.Lerp(0f, 1f, noise_1 * biomenoisesystems[BiomeType].Noise_Rank_123.x + noise_2 * biomenoisesystems[BiomeType].Noise_Rank_123.y + noise_3 * biomenoisesystems[BiomeType].Noise_Rank_123.z);
            float Mixnoise_High = Mathf.Lerp(biomenoisesystems[BiomeType].HighDomain.x, biomenoisesystems[BiomeType].HighDomain.y, Mixnoise);

            float 增量噪声 = Mathf.Lerp(noise_High, Mixnoise_High, BiomeIntensity);

            return 增量噪声;
            //return noise_High + 增量噪声 * 增量噪声放大倍数; 
        }

        else
        {
            //Noise
            float noise_1 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[_WorldType].Noise_Scale_123.x, (float)(_z + _myposition.z) * biomenoisesystems[_WorldType].Noise_Scale_123.x);
            float noise_2 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[_WorldType].Noise_Scale_123.y, (float)(_z + _myposition.z) * biomenoisesystems[_WorldType].Noise_Scale_123.y);
            float noise_3 = Mathf.PerlinNoise((float)(_x + _myposition.x) * biomenoisesystems[_WorldType].Noise_Scale_123.z, (float)(_z + _myposition.z) * biomenoisesystems[_WorldType].Noise_Scale_123.z);
            float noise = Mathf.Lerp(0f, 1f, noise_1 * biomenoisesystems[_WorldType].Noise_Rank_123.x + noise_2 * biomenoisesystems[_WorldType].Noise_Rank_123.y + noise_3 * biomenoisesystems[_WorldType].Noise_Rank_123.z);
            float noise_High = Mathf.Lerp(biomenoisesystems[_WorldType].HighDomain.x, biomenoisesystems[_WorldType].HighDomain.y, noise);
            return noise_High;
        }


    }




    //指定方向寻址
    public Vector3 AddressingBlock(Vector3 _start, int _direct)
    {
        Vector3 _address = _start;
        //print($"start: {_address}");

        for (int i = 0;i < TerrainData.ChunkHeight; i ++)
        {
            byte _byte = GetBlockType(_address);
            if (_byte != VoxelData.Air)
            {
                //print($"坐标：{_address} , 碰到{_byte}");
                //添加一个方块踮脚
                if (_byte == VoxelData.Water)
                {
                    EditBlock(_address, VoxelData.Grass);
                }

                //Offset
                return _address + new Vector3(0.5f, 2f, 0.5f);
            }

            _address += VoxelData.faceChecks[_direct];
        }

        print("寻址失败");
        return _start;

    }


    //给定一个初始坐标和初始方向，朝着这个方向遍历ChunkHeight，返回一个非空气坐标
    public Vector3 LoopAndFindABestLocation(Vector3 _start, Vector3 _direct)
    {
        _direct.x = _direct.x > 0 ? 1 : 0;
        _direct.y = _direct.y > 0 ? 1 : 0;
        _direct.z = _direct.z > 0 ? 1 : 0;

        Vector3 _next = _start;

        //Loop
        for (int i = 0; i < TerrainData.ChunkHeight; i++)
        {
            // Check，如果当前位置的方块类型不是空气，返回该坐标
            if (GetBlockType(_next) != VoxelData.Air)
            {
                return _next;
            }

            // 累积移动位置
            _next += _direct; // 使用归一化的方向向量逐步移动
        }

        return _start; 
    }



    //Vector3 --> 大区块坐标
    public Vector3 GetChunkLocation(Vector3 vec)
    {

        return new Vector3((vec.x - vec.x % TerrainData.ChunkWidth) / TerrainData.ChunkWidth, 0, (vec.z - vec.z % TerrainData.ChunkWidth) / TerrainData.ChunkWidth);

    }


    public Vector3 GetRealChunkLocation(Vector3 vec)
    {

        return new Vector3(16f * ((vec.x - vec.x % TerrainData.ChunkWidth) / TerrainData.ChunkWidth), 0, 16f * ((vec.z - vec.z % TerrainData.ChunkWidth) / TerrainData.ChunkWidth));

    }

    //Vector3 --> 大区块对象
    public Chunk GetChunkObject(Vector3 pos)
    {

        Allchunks.TryGetValue(GetChunkLocation(pos), out Chunk chunktemp);
        return chunktemp;

    }


    //Vector3 --> 区块里的相对坐标
    public Vector3 GetRelalocation(Vector3 vec)
    {

        return new Vector3(Mathf.FloorToInt(vec.x % TerrainData.ChunkWidth), Mathf.FloorToInt(vec.y) % TerrainData.ChunkHeight, Mathf.FloorToInt(vec.z % TerrainData.ChunkWidth));

    }


    /// <summary>
    /// 返回方块类型,输入的是绝对坐标
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public byte GetBlockType(Vector3 pos)
    {
        //提前返回-找不到区块
        if (!Allchunks.TryGetValue(GetChunkLocation(pos), out Chunk chunktemp))
            return 0;

        //提前返回-超过单个区块边界
        if (isOutOfChunkRange(pos))
            return 0;

        //获取相对坐标
        Vector3 _vec = GetRelaPos(pos);

        //获取Block类型
        byte block_type = chunktemp.GetBlock((int)_vec.x, (int)_vec.y, (int)_vec.z).voxelType;

        //Return
        return block_type;

    }




    //---------------------------------------------------------------------------------------







    //------------------------------------工具------------------------------------------------

    //给定日期，将pointsaving修改为给定参数
    [HideInInspector] public String PointSaving = "";
    public void SelectSaving(String _PointSaving)
    {
        PointSaving = _PointSaving;

        //将进入选中的世界按钮点亮
        if (canvasManager.isClickSaving == false)
        {
            canvasManager.LightButton();
        }
        
    }

    // 将EditNumber归类
    public void ClassifyWorldData()
    {
        foreach (var edittemp in EditNumber)
        {
            // 获取当前修改所在的区块位置
            Vector3 _ChunkLocation = GetChunkLocation(edittemp.editPos);

            // 标记是否在 savingDatas 中找到相应的 ChunkLocation
            bool found = false;

            // 查找是否有相同的 ChunkLocation
            foreach (var savingtemp in TheSaving)
            {
                if (savingtemp.ChunkLocation == _ChunkLocation)
                {
                    // 如果找到了相应的 ChunkLocation，则添加相对位置和方块类型到 EditDataInChunk
                    savingtemp.EditDataInChunk[GetRelalocation(edittemp.editPos)] = edittemp.targetType;
                    found = true;
                    break;  // 找到后直接跳出循环
                }
            }

            // 如果没有找到对应的 ChunkLocation，则新建一个 SavingData 并添加到 savingDatas
            if (!found)
            {
                // 创建新的 EditDataInChunk 字典，并添加当前的相对位置和方块类型
                Dictionary<Vector3, byte> newEditDataInChunk = new Dictionary<Vector3, byte>();
                newEditDataInChunk[GetRelalocation(edittemp.editPos)] = edittemp.targetType;

                // 创建新的 SavingData 并添加到 savingDatas
                SavingData newSavingData = new SavingData(_ChunkLocation, newEditDataInChunk);
                TheSaving.Add(newSavingData);
            }
        }

        // 打印 savingDatas（可选，用于调试）
        //foreach (var savingtemp in savingDatas)
        //{
        //    Debug.Log($"Chunk: {savingtemp.ChunkLocation}, Edits: {savingtemp.EditDataInChunk.Count}");

        //    //for (int i = 0;i < savingtemp.EditDataInChunk.Count; i ++)
        //    //{
        //    //    Debug.Log($"Chunk: {savingtemp.ChunkLocation}, Edits: {savingtemp.EditDataInChunk.Count}");
        //    //} 
        //}

        //合并上次存档内容
        //MergeSavingDataLists();
        //savingDatas = TheSaving;
        SAVINGDATA(savingPATH);
    }


    //存档
    public void SAVINGDATA(string savePath)
    {
        // 更新存档结构体
        worldSetting.playerposition = player.transform.position;
        worldSetting.playerrotation = player.transform.rotation;
        worldSetting.gameMode = game_mode;
        string previouDate = worldSetting.date;
        worldSetting.date = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // 创建一个名为 "Saves" 的文件夹
        string savesFolderPath = Path.Combine(savePath, "Saves");
        if (!Directory.Exists(savesFolderPath))
        {
            Directory.CreateDirectory(savesFolderPath);
        }

        // 在 "Saves" 文件夹下创建一个以存档创建日期命名的文件夹
        string folderPath = Path.Combine(savesFolderPath, worldSetting.date);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 删除Saves文件夹中名字为previouDate的文件夹，如果能找到的话
        string oldFolderPath = Path.Combine(savesFolderPath, previouDate);
        if (Directory.Exists(oldFolderPath))
        {
            // 删除旧文件夹及其所有内容
            Directory.Delete(oldFolderPath, true);
        }

        // 将所有的 SavingData 的字典转换为列表
        foreach (var data in TheSaving)
        {
            data.EditDataInChunkList = new List<EditStruct>();
            foreach (var kvp in data.EditDataInChunk)
            {
                data.EditDataInChunkList.Add(new EditStruct(kvp.Key, kvp.Value));
            }
        }

        // 将 worldSetting 和 savingDatas 转换为 JSON 字符串
        string worldSettingJson = JsonUtility.ToJson(worldSetting, true);
        string savingDatasJson = JsonUtility.ToJson(new Wrapper<SavingData>(TheSaving), true);

        // 将 JSON 字符串保存到指定路径的文件夹中
        File.WriteAllText(Path.Combine(folderPath, "WorldSetting.json"), worldSettingJson);
        File.WriteAllText(Path.Combine(folderPath, "SavingDatas.json"), savingDatasJson);



         Debug.Log("数据已保存到: " + folderPath);
        isFinishSaving = true;


        // Debug
        //foreach (var data in TheSaving)
        //{
        //    Debug.Log("Chunk Location: " + data.ChunkLocation);
        //    foreach (var editData in data.EditDataInChunkList)
        //    {
        //        Debug.Log("Edit Position: " + editData.editPos + ", Target Type: " + editData.targetType);
        //    }
        //}
    }





    //读取全部存档
    public void LoadAllSaves(string savingPATH)
    {
        // 构造 Saves 目录的路径
        string savesFolderPath = Path.Combine(savingPATH, "Saves");

        // 检查 Saves 目录是否存在，如果不存在则创建它
        if (!Directory.Exists(savesFolderPath))
        {
            try
            {
                Directory.CreateDirectory(savesFolderPath);
                //Debug.Log($"Saves 文件夹已创建: {savesFolderPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建 Saves 文件夹时出错: {ex.Message}");
                return; // 创建文件夹失败时退出函数
            }
        }

        // 获取存档目录下的所有文件夹路径
        string[] saveDirectories;
        try
        {
            saveDirectories = Directory.GetDirectories(savesFolderPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"获取存档目录时出错: {ex.Message}");
            return; // 处理异常并退出函数
        }

        // 遍历每个存档文件夹
        foreach (string saveDirectory in saveDirectories)
        {
            // 输出当前存档文件夹名称
            string folderName = Path.GetFileName(saveDirectory);
            //Debug.Log($"Loading save from folder: {folderName}");

            // 调用 LoadData 函数读取当前存档
            try
            {
                WorldSetting _worldsetting = LoadWorldSetting(saveDirectory);
                canvasManager.NewWorldGenerate(
                    _worldsetting.name,
                    _worldsetting.date,
                    _worldsetting.gameMode,
                    _worldsetting.worldtype,
                    _worldsetting.seed
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载存档 {folderName} 时出错: {ex.Message}");
            }
        }

        //Debug.Log($"Total saves loaded: {saveDirectories.Length}");
    }


    //分析存档名字
    public WorldSetting LoadWorldSetting(string savePath)
    {
        // 构建 WorldSetting.json 文件的完整路径
        string worldSettingPath = Path.Combine(savePath, "WorldSetting.json");

        // 检查 WorldSetting.json 文件是否存在
        if (File.Exists(worldSettingPath))
        {
            // 读取 WorldSetting.json 文件的内容
            string worldSettingJson = File.ReadAllText(worldSettingPath);

            // 将 JSON 字符串反序列化为 WorldSetting 对象
            return JsonUtility.FromJson<WorldSetting>(worldSettingJson);
        }
        else
        {
            // 如果文件不存在，输出错误信息并返回 null
            Debug.LogError("找不到 WorldSetting.json 文件");
            return null;
        }
    }

    //加载存档
    public void LoadSavingData(string savePath)
    {
        isLoadSaving = true;

        // 构建 SavingDatas.json 文件的完整路径
        string savingDatasPath = Path.Combine(savePath, "SavingDatas.json");

        // 检查 SavingDatas.json 文件是否存在
        if (File.Exists(savingDatasPath))
        {
            // 读取 SavingDatas.json 文件的内容
            string savingDatasJson = File.ReadAllText(savingDatasPath);

            // 将 JSON 字符串反序列化为 Wrapper<SavingData> 对象
            Wrapper<SavingData> wrapper = JsonUtility.FromJson<Wrapper<SavingData>>(savingDatasJson);

            // 检查 wrapper 是否为 null
            if (wrapper != null && wrapper.Items != null)
            {
                // 将反序列化的 Items 列表赋值给 TheSaving
                TheSaving = wrapper.Items;

                // 遍历 TheSaving 列表，恢复每个 SavingData 对象中的字典
                foreach (var data in TheSaving)
                {
                    data.RestoreDictionary();

                    // Debug打印
                    //Debug.Log($"Chunk Location: {data.ChunkLocation}");
                    //foreach (var pair in data.EditDataInChunk)
                    //{
                    //    Debug.Log($"Position: {pair.Key}, Type: {pair.Value}");
                    //}
                }

                worldSetting = LoadWorldSetting(savePath);


                //更新一些参数
                game_mode = worldSetting.gameMode;



            }
            else
            {
                // 如果 wrapper 或 wrapper.Items 为 null，输出警告信息
                Debug.LogWarning("Wrapper<SavingData> 或 Items 列表为 null");
            }
        }
        else
        {
            // 如果文件不存在，输出错误信息
            Debug.LogError("找不到 SavingDatas.json 文件");
        }
    }




    //向量单位正交化
    Vector3 VtoNormal(Vector3 v)
    {

        if (v.x >= 0)
        {

            if (v.z >= 0)
            {

                if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
                {

                    return new Vector3(1, 0, 0);

                }
                else
                {

                    return new Vector3(0, 0, 1);

                }

            }
            else
            {

                if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
                {
                
                    return new Vector3(1, 0, 0);
                
                }
                else
                {
                
                    return new Vector3(0, 0, -1);
                
                }

            }

        }
        else
        {

            if (v.z >= 0)
            {
                
                if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
                {
                
                    return new Vector3(-1, 0, 0);
                
                }
                else
                {

                    return new Vector3(0, 0, 1);
                
                }

            }
            else
            {

                if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
                {

                    return new Vector3(-1, 0, 0);

                }
                else
                {

                    return new Vector3(-1, 0, 0);

                }

            }

        }

    }



    //求Vector3的2d长度
    float GetVector3Length(Vector3 vec)
    {
        Vector2 vector2 = new Vector2(vec.x, vec.z);
        return vector2.magnitude;
    }





    //------------------------------------ 保存建筑 --------------------------------------------------------------

    //目前有问题

    //用于记录建筑
    //recordData.pos存的是绝对坐标
    private List<EditStruct> recordData = new List<EditStruct>();

    //记录建筑
    public void RecordBuilding(Vector3 _Start, Vector3 _End)
    {
        recordData.Clear();


        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {

            }
        }
    }

    //释放建筑
    //根据0下标和end下标，计算其包含的区块，然后把数据丢给区块即可
    public void ReleaseBuilding(Vector3 place, List<EditStruct> _recordData)
    {

        Vector3 start = _recordData[0].editPos;
        Vector3 end = _recordData[_recordData.Count - 1].editPos;

        for (float x = start.x; x < end.x; x += 16f)
        {
            for (float z = start.z; z < end.z; z += 16f)
            {

            }
        }
    }


    #region 修改方块

    //外界修改方块
    public void EditBlock(Vector3 _pos, byte _target)
    {
        Allchunks[GetChunkLocation(_pos)].EditData(_pos,_target);
    }

    public void EditBlock(List<EditStruct> _editStructs)
    {
        List<Vector3> _ChunkLocations = new List<Vector3>();

        // 遍历_editStructs并存储ChunkLocations
        foreach (var item in _editStructs)
        {

            // 如果allchunks里没有pos.则_ChunkLocations添加
            if (Allchunks.ContainsKey(GetChunkLocation(item.editPos)))
            {
                if (!_ChunkLocations.Contains(GetChunkLocation(item.editPos)))
                {
                    _ChunkLocations.Add(GetChunkLocation(item.editPos));
                }


            }
            else
            {
                print($"区块不存在:{GetChunkLocation(item.editPos)}");

            }


        }

        // 遍历_ChunkLocations，将allchunk里的_ChunkLocations执行EditData
        foreach (var chunkLocation in _ChunkLocations)
        {
            Allchunks[chunkLocation].EditData(_editStructs);
        }

        // 打印找到的区块数量
        //print($"找到{_ChunkLocations.Count}个");
    }


    Coroutine editBlockCoroutine;
    public void EditBlock(List<EditStruct> _editStructs, float _time)
    {
        if (editBlockCoroutine == null)
        {
            editBlockCoroutine = StartCoroutine(Coroutine_editBlock(_editStructs,_time));
        }
        

    }

    IEnumerator Coroutine_editBlock(List<EditStruct> _editStructs, float _time)
    {
        foreach (var item in _editStructs)
        {
            //print("执行EditBlocks");
            Allchunks[GetChunkLocation(item.editPos)].EditData(item.editPos, item.targetType);

            yield return new WaitForSeconds(_time);
        }
        editBlockCoroutine = null;
    }

    #endregion

    //--------------------------------------------------------------------------------------------------------------

    //删除存档
    public void DeleteSave(string savepath)
    {
        if (Directory.Exists(savepath))
        {
            try
            {
                // 删除所有文件
                foreach (string file in Directory.GetFiles(savepath))
                {
                    File.Delete(file);
                    //Debug.Log($"Deleted file: {file}");
                }

                // 递归删除所有子目录
                foreach (string directory in Directory.GetDirectories(savepath))
                {
                    DeleteSave(directory);
                }

                // 删除空目录
                Directory.Delete(savepath);
                //Debug.Log("完成删除");
            }
            catch (Exception ex)
            {
                // 处理异常，如日志记录
                Debug.LogError($"删除目录 {savepath} 时出错: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"指定路径 {savepath} 不存在.");
        }
    }


    //合并存档
    //public void MergeSavingDataLists()
    //{
    //    // 创建一个新的列表用于存储合并后的数据
    //    List<SavingData> mergedSavingData = new List<SavingData>();

    //    // 将 TheSaving 中的数据加入到合并列表中
    //    foreach (var theSavingData in TheSaving)
    //    {
    //        // 在 mergedSavingData 中查找是否已经存在相同的 ChunkLocation
    //        var existingData = mergedSavingData.FirstOrDefault(sd => sd.ContainsChunkLocation(theSavingData.ChunkLocation));
    //        if (existingData == null)
    //        {
    //            // 如果没有找到相同 ChunkLocation 的数据，则直接添加
    //            mergedSavingData.Add(new SavingData(theSavingData.ChunkLocation, theSavingData.EditDataInChunk));
    //        }
    //        else
    //        {
    //            // 如果找到了，则更新已有的数据
    //            existingData.EditDataInChunk = new Dictionary<Vector3, byte>(theSavingData.EditDataInChunk);
    //        }
    //    }

    //    // 遍历 savingDatas，合并数据
    //    foreach (var savingData in savingDatas)
    //    {
    //        // 在 mergedSavingData 中查找是否已经存在相同的 ChunkLocation
    //        var existingData = mergedSavingData.FirstOrDefault(sd => sd.ContainsChunkLocation(savingData.ChunkLocation));
    //        if (existingData == null)
    //        {
    //            // 如果没有找到相同 ChunkLocation 的数据，则直接添加
    //            mergedSavingData.Add(new SavingData(savingData.ChunkLocation, savingData.EditDataInChunk));
    //        }
    //        else
    //        {
    //            // 如果找到了，则合并 EditDataInChunkList
    //            foreach (var editStruct in savingData.EditDataInChunkList)
    //            {
    //                existingData.EditDataInChunk[editStruct.editPos] = editStruct.targetType;
    //            }
    //        }
    //    }

    //    // 将合并后的数据赋值回 savingDatas
    //    savingDatas = mergedSavingData;
    //}



    //获取TheSaving的索引
    public int GetIndexOfChunkLocation(Vector3 location)
    {
        // 遍历 TheSaving 列表
        for (int i = 0; i < TheSaving.Count; i++)
        {
            var savingData = TheSaving[i];
            // 使用 SavingData 的 ContainsChunkLocation 方法检查 ChunkLocation
            if (savingData.ContainsChunkLocation(location))
            {
                return i; // 返回匹配项的索引
            }
        }

        return -1; // 如果没有找到匹配项，则返回 -1
    }


    //返回TheSaving是否包含ChunkLocation
    public bool ContainsChunkLocation(Vector3 location)
    {
        // 遍历 TheSaving 列表
        foreach (var savingData in TheSaving)
        {
            // 使用 SavingData 的 ContainsChunkLocation 方法检查 ChunkLocation
            if (savingData.ContainsChunkLocation(location))
            {
                return true; // 找到匹配的 ChunkLocation
            }
        }

        return false; // 没有找到匹配的 ChunkLocation
    }


    // 推送玩家更新的具体方块
    public List<EditStruct> WaitToAdd_EditList = new List<EditStruct>();
    public Coroutine updateEditNumberCoroutine;

    /// <summary>
    /// 注意返回绝对坐标
    /// </summary>
    /// <param name="RealPos"></param>
    /// <param name="targetBlocktype"></param>
    public void UpdateEditNumber(Vector3 RealPos, byte targetBlocktype)
    {
        // 将修改细节推送至World里
        // 转换RealPos为整型Vector3以便用作字典的key
        Vector3 intPos = new Vector3((int)RealPos.x, (int)RealPos.y, (int)RealPos.z);

        // 查找是否已经存在相同的editPos
        EditStruct existingEdit = EditNumber.Find(edit => edit.editPos == intPos);

        if (existingEdit != null)
        {
            // 如果存在，更新targetType
            existingEdit.targetType = targetBlocktype;
        }
        else
        {
            // 如果不存在，添加新的EditStruct
            //print($"Edit更新: {intPos} --- {targetBlocktype}");
            if (intPos.y >= 0)
            {
                EditNumber.Add(new EditStruct(intPos, targetBlocktype));
            }
            
        }
    }

    public void UpdateEditNumber(List<EditStruct> _EditList)
    {
        // 添加新的编辑列表到等待处理的队列尾部
        WaitToAdd_EditList.AddRange(_EditList);
        // 如果协程未运行，则启动协程
        if (updateEditNumberCoroutine == null)
        {
            updateEditNumberCoroutine = StartCoroutine(_updateEditNumberCoroutine());
        }
    }

    IEnumerator _updateEditNumberCoroutine()
    {
        // 每次处理的数量，避免卡顿
        int batchSize = 10;

        while (WaitToAdd_EditList.Count > 0)
        {
            // 每次取出最多 batchSize 个 EditStruct 从头部进行处理
            int count = Mathf.Min(batchSize, WaitToAdd_EditList.Count);

            for (int i = 0; i < count; i++)
            {
                // 取出列表中的第一个元素
                EditStruct edit = WaitToAdd_EditList[0];

                //基岩跳过
                if (edit.targetType != VoxelData.BedRock)
                {
                    // 将编辑项添加到 EditNumber 中
                    UpdateEditNumber(edit.editPos, edit.targetType);
                }
                else
                {
                    print("处理到基岩");
                }

                // 从头部移除已处理的项
                WaitToAdd_EditList.RemoveAt(0);
            }

            // 暂停一帧，避免一次性处理太多项导致卡顿
            yield return null;
        }

        // 处理完成后，将协程变量设为 null
        //print("null");
        updateEditNumberCoroutine = null;
    }



    //给定int，返回世界类型的中文
    public String GetWorldTypeString(int WorldType)
    {
        switch (WorldType)
        {
            case 0:
                return "草原群系";
            case 1:
                return "高原群系";
            case 2:
                return "沙漠群系";
            case 3:
                return "沼泽群系";
            case 4:
                return "密林群系";
            case 5:
                return "默认群系";
            case 6:
                return "超平坦世界";
            default:
                return "给定世界类型有误GetWorldTypeChinese";
        }
    }

    //给定游戏模式的中文
    public String GetGameModeString(GameMode gamemode)
    {
        if (gamemode == GameMode.Survival)
        {
            return "生存模式";
        }
        else
        {
            return "创造模式";
        }
    }


    #region 方块检测

    //对玩家碰撞盒的方块判断
    //true：有碰撞
    public bool CollisionCheckForVoxel(Vector3 pos)
    {
        //if (GetBlockType(pos) == VoxelData.Wood)
        //{
        //    print("");
        //}

       
        Vector3 realLocation = pos; //绝对坐标
        Vector3 relaLocation = GetRelalocation(realLocation);
        byte targetBlock = GetBlockType(realLocation);

        //出界判断(Chunk)
        if (!Allchunks.ContainsKey(GetChunkLocation(realLocation))) 
        {
            return true; 
        }
        
        //出界判断(Y)
        if (realLocation.y >= TerrainData.ChunkHeight || realLocation.y < 0) 
        {
            return false; 
        }

        //如果是自定义碰撞
        if (blocktypes[targetBlock].isSolid && blocktypes[targetBlock].isDIYCollision)
        {
            realLocation = CollisionOffset(realLocation, targetBlock);
            Vector3 OffsetrelaLocation = GetRelalocation(realLocation);

            if (OffsetrelaLocation != relaLocation)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        


        //返回固体还是空气
        return blocktypes[Allchunks[GetChunkLocation(realLocation)].voxelMap[(int)relaLocation.x, (int)relaLocation.y, (int)relaLocation.z].voxelType].isSolid;

    }

    //public float XOFFSET;
    //public float YOFFSET;
    //public float ZOFFSET;

    public Vector3 CollisionOffset(Vector3 _realPos, byte _targetType)
    {
        Vector3 _input = new Vector3(player.horizontalInput, player.Facing.y, player.verticalInput);
        float _x = _realPos.x; Vector2 _xRange = blocktypes[_targetType].CollosionRange.xRange; float _xOffset = _x - (int)_x; 
        float _y = _realPos.y; Vector2 _yRange = blocktypes[_targetType].CollosionRange.yRange; float _yOffset = _y - (int)_y; 
        float _z = _realPos.z; Vector2 _zRange = blocktypes[_targetType].CollosionRange.zRange; float _zOffset = _z - (int)_z;


        //X
        if (_input.x >= 0 || _xOffset < _xRange.x)
            _x -= _xRange.x;
        else if(_input.x < 0 || _xOffset > _xRange.y)
            _x += (1 - _xRange.y);


        //Y
        if (_input.y >= 0 || _yOffset < _yRange.x)
            _y -= _yRange.x;
        else if (_input.y < 0 || _yOffset > _yRange.y)
            _y += (1 - _yRange.y);

        //Z
        if (_input.z >= 0 || _zOffset < _zRange.x)
            _z -= _zRange.x;
        else if (_input.z < 0 || _zOffset > _zRange.y)
            _z += (1 - _zRange.y);

        return new Vector3(_x, _y, _z);
    }

    #endregion


    #region 工具

    //放置高亮方块的
    //用于眼睛射线的检测
    public bool RayCheckForVoxel(Vector3 pos)
    {

        Vector3 realLocation = pos; //绝对坐标
        Vector3 relaLocation = GetRelalocation(realLocation);
        byte targetBlock = GetBlockType(realLocation);

        if (!Allchunks.ContainsKey(GetChunkLocation(pos))) { return false; }

        //计算相对坐标
        Vector3 vec = GetRelalocation(new Vector3(pos.x, pos.y, pos.z));

        //判断XOZ上有没有出界
        if (!Allchunks.ContainsKey(GetChunkLocation(pos))) { return true; }

        //判断Y上有没有出界
        if (realLocation.y >= TerrainData.ChunkHeight) { return false; }


        //如果是自定义碰撞
        if (blocktypes[targetBlock].isDIYCollision)
        {
            realLocation = CollisionOffset(realLocation, targetBlock);
            Vector3 OffsetrelaLocation = GetRelalocation(realLocation);

            if (OffsetrelaLocation != relaLocation)
            {
                return false;
            }
            else
            {
                return true;
            }
        }



        //返回固体还是空气
        return blocktypes[Allchunks[GetChunkLocation(new Vector3(pos.x, pos.y, pos.z))].voxelMap[(int)vec.x, (int)vec.y, (int)vec.z].voxelType].canBeChoose;

    }

    // 分量乘法
    public Vector3 ComponentwiseMultiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    // 处理 Vector2 的分量乘法
    public Vector2 ComponentwiseMultiply(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x * b.x, a.y * b.y);
    }

    #endregion


    #region 实体管理

    [Foldout("实体管理", true)]
    [Header("实体父类")] public GameObject Entity_Parent;
    [Header("蒸汽粒子")] public GameObject Evaporation_Particle;
    [Header("实体预制体")] public GameObject[] Entity_Prefeb;
    [Header("活着的所有实体")] public List<EntityStruct> AllEntity = new List<EntityStruct>();
    [Header("最大实体数量")][SerializeField] private int maxSize = 100; // 默认值为100，可在Inspector中调整
    private int Unique_Id = 0; // 用于生成新的唯一ID

   
    /// <summary>
    /// 根据id寻找实体
    /// </summary>
    public GameObject FindEntity(int _id)
    {
        foreach (var entity in AllEntity)
        {
            if (entity._id == _id)
            {
                return entity._obj;
            }
        }
        Debug.LogWarning($"实体ID {_id} 不存在！");
        return null;
    }

    /// <summary>
    /// 设置最大容量
    /// </summary>
    /// <param name="newMaxSize">新的最大容量</param>
    public void SetMaxSize(int newMaxSize)
    {
        if (AllEntity.Count >= newMaxSize)
        {
            Debug.LogWarning("新设置的最大容量小于当前实体数量，调整无效！");
            return;
        }

        maxSize = newMaxSize;
        Debug.Log($"最大容量已调整为 {maxSize}。");
    }

    /// <summary>
    /// 添加实体到管理中
    /// </summary>
    /// <param name="_index">需要添加的预制体的下标</param>
    /// <param name="_Startpos">实体的起始位置</param>
    /// <returns>是否添加成功</returns>
    public bool AddEntity(int _index, Vector3 _Startpos, out EntityStruct _Result)
    {
        // 提起返回-检查实体数量是否达到最大值
        if (AllEntity.Count >= maxSize)
        {
            Debug.LogWarning("实体数量已达到最大值，无法添加新实体！");
            _Result = null;
            return false;
        }

        // 提起返回-检查下标是否有效
        if (_index < 0 || _index >= Entity_Prefeb.Length)
        {
            Debug.LogError("索引超出范围，请提供有效的预制体索引！");
            _Result = null;
            return false;
        }

        // 实例化预制体
        GameObject newEntity = Instantiate(Entity_Prefeb[_index]);

        // 提起返回-如果没有注册组件
        if (newEntity.GetComponent<MC_Registration_Component>() == null)
        {
            print($"{_index}实体未添加注册组件，无法添加到游戏中");
            _Result = null;
            Destroy(newEntity);
            return false;
        }

        // 将实例化的实体设置为子对象
        newEntity.transform.SetParent(Entity_Parent.transform);
        newEntity.transform.position = _Startpos;

        // 生成一个唯一的ID
        int entityId = Unique_Id++;

        //对实体进行注册
        newEntity.GetComponent<MC_Registration_Component>().RegistEntity(entityId);

        // 将新实例加入数据结构
        _Result = new EntityStruct(entityId, newEntity);
        AllEntity.Add(_Result);

        //Debug.Log($"实体 {newEntity.name} 已添加成功，ID为{entityId}！");
        
        return true;
    }

    /// <summary>
    /// 从管理中移除实体
    /// </summary>
    /// <param name="entity">需要移除的实体</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveEntity(GameObject entity)
    {
        EntityStruct entityToRemove = null;

        // 获取该实体对应的EntityStruct
        foreach (var entityStruct in AllEntity)
        {
            if (entityStruct._obj == entity)
            {
                entityToRemove = entityStruct;
                break;
            }
        }

        if (entityToRemove != null)
        {
            AllEntity.Remove(entityToRemove);
            Debug.Log($"实体 {entity.name} 已移除成功！");
            return true;
        }

        Debug.LogWarning("该实体不在管理中！");
        return false;
    }


    /// <summary>
    /// 从管理中移除实体
    /// </summary>
    /// <param name="entity">需要移除的实体</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveEntity(int _id)
    {
        EntityStruct entityToRemove = null;

        // 获取该实体对应的EntityStruct
        foreach (var entityStruct in AllEntity)
        {
            if (entityStruct._id == _id)
            {
                entityToRemove = entityStruct;
                break;
            }
        }

        if (entityToRemove != null)
        {
            AllEntity.Remove(entityToRemove);
            //Debug.Log($"实体 {_id} 已移除成功！");
            return true;
        }

        Debug.LogWarning("该实体不在管理中！");
        return false;
    }


    /// <summary>
    /// 检测当前实体数量
    /// </summary>
    /// <returns>实体数量</returns>
    public int GetEntityCount()
    {
        return AllEntity.Count;
    }

    /// <summary>
    /// 清空所有实体
    /// </summary>
    public void ClearEntities()
    {
        AllEntity.Clear();
        Debug.Log("所有实体已清空！");
    }

    /// <summary>
    /// 获取一定范围内的实体（基于位置距离计算）
    /// </summary>
    /// <param name="center">范围检测的球心</param>
    /// <param name="_r">检测范围的半径</param>
    /// <returns>范围内的实体列表</returns>
    public bool GetOverlapSphereEntity(Vector3 center, float _r, out List<EntityStruct> result)
    {
        result = new List<EntityStruct>();  // 初始化输出列表
        float sqrRadius = _r * _r; // 使用平方半径以避免重复开平方运算
        bool hasEntitiesInRange = false;  // 记录是否有实体在范围内

        foreach (var entityStruct in AllEntity)
        {
            if (entityStruct._obj == null)
                continue; // 防止空引用错误

            // 计算实体与球心的距离
            Vector3 offset = entityStruct._obj.transform.position - center;

            // 如果实体的平方距离小于等于检测范围的平方半径，添加到结果列表
            if (offset.sqrMagnitude <= sqrRadius)
            {
                result.Add(entityStruct);
                hasEntitiesInRange = true;  // 有实体在范围内
            }
        }

        return hasEntitiesInRange;
    }


    /// <summary>
    /// 重载，可以忽略自己
    /// </summary>
    /// <param name="center"></param>
    /// <param name="_r"></param>
    /// <param name="_myID"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool GetOverlapSphereEntity(Vector3 center, float _r, int _myID, out List<EntityStruct> result)
    {
        result = new List<EntityStruct>();  // 初始化输出列表
        float sqrRadius = _r * _r; // 使用平方半径以避免重复开平方运算
        bool hasEntitiesInRange = false;  // 记录是否有实体在范围内

        foreach (var entityStruct in AllEntity)
        {
            if (entityStruct._obj == null || entityStruct._id == _myID)
                continue; // 防止空引用错误，同时跳过自身

            // 计算实体与球心的距离
            Vector3 offset = entityStruct._obj.transform.position - center;

            // 如果实体的平方距离小于等于检测范围的平方半径，添加到结果列表
            if (offset.sqrMagnitude <= sqrRadius)
            {
                result.Add(entityStruct);
                hasEntitiesInRange = true;  // 有实体在范围内
            }
        }

        return hasEntitiesInRange;
    }




    #endregion



    


}



