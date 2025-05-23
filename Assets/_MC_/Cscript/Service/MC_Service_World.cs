using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;
using System.Collections;
using static MC_Static_Math;
using System.IO;
using Homebrew;

/// <summary>
/// 主要处理区块的加载和卸载
/// </summary>
public class MC_Service_World : MC_Tick_Base
{

    #region 周期函数

    MC_Service_Saving Service_Saving;
    Player player;

    private void Awake()
    {
        Service_Saving = managerhub.Service_Saving;
        player = managerhub.player;
    }

    public override void Handle_GameState_Tick()
    {
        base.Handle_GameState_Tick();
        ThreadUpdateController();
    }

    public override void Handle_GameState_Start_Once()
    {
        base.Handle_GameState_Start_Once();
        InitWorldManager();
        Service_Saving.isFinishUpdateEditNumber = false;

    }

    public override void Handle_GameState_Loading_Once()
    {
        base .Handle_GameState_Loading_Once();
        InitStartTime = Time.time;

        //开始初始化
        Update_CenterChunks(true);
    }

    public override void Handle_GameState_Playing_Once()
    {
        base.Handle_GameState_Playing_Once();
        MC_Static_Unity.LockMouse(true);
    }

    public override void Handle_GameState_Playing()
    {
        base.Handle_GameState_Playing();
        ChunkUpdateController();
    }

    void OnApplicationQuit()
    {

        //结束游戏
        MC_Runtime_DynamicData.instance.SetGameState(Game_State.Ending);

        //等待Render线程
        if (myThread_Render != null && myThread_Render.IsAlive)
            myThread_Render.Join(); // 等待线程安全地终止
    }

    /// <summary>
    /// Init
    /// </summary>
    public void InitWorldManager()
    {

        //Runtime
        MC_Runtime_DynamicData.instance.SetGameState(Game_State.Start);
        MC_Runtime_DynamicData.instance.SetGameMode(GameMode.Survival);

        //World
        renderSize = 10;
        StartToRender = 1f;
        DestroySize = 7f;
        InitStartTime = 0f;
        InitEndTime = 0f;
        Start_Position = new Vector3(1600f, 127f, 1600f);
        hasExec_RandomPlayerLocation = true;

        //数据结构
        Allchunks = new Dictionary<Vector3, Chunk>();
        myThread_Render = null;
        CreateCoroutine = null;
        RemoveCoroutine = null;
        Render_Coroutine = null;
        Mesh_Coroutine = null;
        MeshLock = false;
        RenderLock = false;
        WatingToCreate_Chunks = new List<Vector3>();
        WatingToRemove_Chunks = new List<Vector3>();
        WaitToCreateMesh = new ConcurrentQueue<Chunk>();
        WaitToRender = new ConcurrentQueue<Chunk>();
        WaitToRender_temp = new ConcurrentQueue<Chunk>();
        WaitToFlashChunkQueue = new ConcurrentQueue<Chunk>();
        WaitToRender_New = new ConcurrentQueue<Chunk>();

        //Chunks
        Center_Now = Vector3.zero;
        Center_direction = Vector3.zero;
        GameObject ChunkParent = SceneData.GetChunkParent();
        foreach (Transform child in ChunkParent.transform)
            Destroy(child.gameObject);

        //Debug
        isGenChunkSurrendFace = managerhub.是否生成Chunk侧面;
        if (managerhub.低区块模式)
            renderSize = 2;
        if (managerhub.无黑夜模式 && managerhub.Service_Time.gameObject.activeSelf)
            managerhub.Service_Time.gameObject.SetActive(false);
    }


    #endregion

    #region 变量

    [Foldout("渲染设置", true)]
    [Header("Chunk渲染半径(总区块=r^2*4)")] public int renderSize = 5;
    [Header("开始更新的最小区块跨度")] public float StartToRender = 1f;
    [Header("开始销毁的最大半径")] public float DestroySize = 7f;

    [Foldout("Debug", true)]
    [Header("是否生成Chunk侧面")] public bool isGenChunkSurrendFace = false;

    [Foldout("编辑器不需要看", true)]
    [Header("Player- 是否随机玩家坐标")] public bool hasExec_RandomPlayerLocation = true;
    [Header("Player- 玩家出生点")] public Vector3 Start_Position = new Vector3(1600f, 127f, 1600f);
    [Header("Chunk- ?不知道是个什么东西")] public Chunk obj;//?
    [Header("Chunk - 所有区块")] public Dictionary<Vector3, Chunk> Allchunks = new Dictionary<Vector3, Chunk>();
    [Header("Chunk - 区块渲染中心点")] public Vector3 Center_Now;
    [Header("Chunk - 区块渲染方向")] public Vector3 Center_direction; //这个代表了方向
    [Header("Struct - 生成队列")] public List<Vector3> WatingToCreate_Chunks = new List<Vector3>();
    [Header("Struct - 移除队列")] public List<Vector3> WatingToRemove_Chunks = new List<Vector3>();
    [Header("Struct - 等待刷新队列")] public ConcurrentQueue<Chunk> WaitToFlashChunkQueue = new ConcurrentQueue<Chunk>();
    [Header("Struct - 渲染队列0")] public ConcurrentQueue<Chunk> WaitToRender = new ConcurrentQueue<Chunk>();
    [Header("Struct - 渲染队列1")] public ConcurrentQueue<Chunk> WaitToCreateMesh = new ConcurrentQueue<Chunk>();
    [Header("Struct - 渲染队列2")] public ConcurrentQueue<Chunk> WaitToRender_temp = new ConcurrentQueue<Chunk>();
    [Header("Struct - 渲染队列3")] public ConcurrentQueue<Chunk> WaitToRender_New = new ConcurrentQueue<Chunk>();

    //变量
    public bool MeshLock = false;
    public bool RenderLock = false;
    public int Delay_RenderMesh = 1000;

    //协程
    Coroutine Render_Coroutine;
    Coroutine Mesh_Coroutine;
    Coroutine Init_Map_Thread_NoInit_Coroutine;
    Coroutine Init_MapCoroutine;
    Coroutine CreateCoroutine;
    Coroutine RemoveCoroutine;

    //计时
    float InitStartTime; //区块开始渲染时间
    float InitEndTime; //区块结束渲染时间

    //应该没问题
    readonly object Allchunks_Lock = new object();
    Thread myThread_Render;

    #endregion

    #region 初始化地图

    public IEnumerator Init_Map_Thread(bool _isInitPlayerLocation)
    {
        //确定Chunk中心点
        GetChunkCenterNow();

        //初始化区块并添加进度条
        float temp = 0f;
        for (int x = -renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x < renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x++)
        {
            for (int z = -renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z < renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z++)
            {
                CreateBaseChunk(new Vector3(x, 0, z));
                temp++;
                float max = renderSize * renderSize * 4;
                managerhub.canvasManager.Initprogress = Mathf.Lerp(0f, 0.9f, temp / max);
                yield return new WaitForSeconds(0.01f);
            }

        }

        //重新初始化玩家位置，防止穿模
        if (_isInitPlayerLocation)
        {
            managerhub.player.InitPlayerLocation();
        }


        //游戏开始
        yield return new WaitForSeconds(0.5f);
        managerhub.canvasManager.Initprogress = 1f;

        //开启面优化协程
        StartCoroutine(Chunk_Optimization());
        StartCoroutine(FlashChunkCoroutine());

        Init_MapCoroutine = null;
    }

    //确定Chunk中心点
    void GetChunkCenterNow()
    {

        if (Service_Saving.isLoadSaving)
        {
            Center_Now = new Vector3(GetRealChunkLocation(Service_Saving.worldSetting.playerposition).x, 0, GetRealChunkLocation(Service_Saving.worldSetting.playerposition).z);
            return;
        }


        if (hasExec_RandomPlayerLocation)
        {
            managerhub.player.RandomPlayerLocaiton();
            hasExec_RandomPlayerLocation = false;
        }
        //print(PlayerFoot.transform.position);
        Center_Now = new Vector3(GetRealChunkLocation(player.foot.transform.position).x, 0, GetRealChunkLocation(player.foot.transform.position).z);

    }

    #endregion

    #region 动态加载地图

    /// <summary>
    /// 更新区块
    /// </summary>
    void ChunkUpdateController()
    {
        //玩家移动刷新
        //如果大于16f
        if (Get2DLengthforVector3(player.foot.transform.position - Center_Now) > (StartToRender * 16f) && Get2DLengthforVector3(player.foot.transform.position - Center_Now) <= ((StartToRender + 1) * 16f))
        {

            //更新Center
            Center_direction = NormalizeToAxis(player.foot.transform.position - Center_Now);
            Center_Now += Center_direction * TerrainData.ChunkWidth;

            //添加Chunk
            AddtoCreateChunks(Center_direction);
            AddtoRemoveChunks(Center_direction);

        }
        //玩家移动过远距离
        else if (Get2DLengthforVector3(player.foot.transform.position - Center_Now) > ((StartToRender + 1) * 16f))
        {
            Update_CenterWithNoInit();
        }
    }


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

        Center_Now = new Vector3(GetRealChunkLocation(player.foot.transform.position).x, 0, GetRealChunkLocation(player.foot.transform.position).z);

        for (int x = -renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x < renderSize + (int)(Center_Now.x / TerrainData.ChunkWidth); x++)
        {

            for (int z = -renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z < renderSize + (int)(Center_Now.z / TerrainData.ChunkWidth); z++)
            {

                //Create
                CreateBaseChunk(new Vector3(x, 0, z));

                yield return new WaitForSeconds(0.01f);
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

    //清除过远区块
    public void HideFarChunks()
    {
        foreach (var temp in Allchunks)
        {
            if (Get2DLengthforVector3(player.foot.transform.position - temp.Value.myposition) > (StartToRender * 16f))
            {
                temp.Value.HideChunk();
            }
        }
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


    //添加到等待添加队列
    public void AddtoCreateChunks(Vector3 add_vec)
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

            yield return new WaitForSeconds(0.001f);


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
    public void CreateBaseChunk(Vector3 pos)
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
        if (Service_Saving.ContainsChunkLocation(_ChunkLocation))
        {
            _chunk_temp = new Chunk(_ChunkLocation,  false, Service_Saving.TheSaving[Service_Saving.GetIndexOfChunkLocation(_ChunkLocation)].EditDataInChunkList);
        }
        else
        {
            _chunk_temp = new Chunk(_ChunkLocation,  false);
        }

        //GameObject chunkGameObject = new GameObject($"{Mathf.FloorToInt(pos.x)}, 0, {Mathf.FloorToInt(pos.z)}");
        //Chunk chunktemp = chunkGameObject.AddComponent<Chunk>();
        //chunktemp.InitChunk(new Vector3(0, 0, 0), this);

        //添加到字典
        Allchunks.Add(_ChunkLocation, _chunk_temp);

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
        if (Service_Saving.ContainsChunkLocation(_ChunkLocation))
        {
            _chunk_temp = new Chunk(_ChunkLocation,  false, Service_Saving.TheSaving[Service_Saving.GetIndexOfChunkLocation(_ChunkLocation)].EditDataInChunkList);
        }
        else
        {
            _chunk_temp = new Chunk(_ChunkLocation,  false);
        }


        //GameObject chunkGameObject = new GameObject($"{Mathf.FloorToInt(pos.x)}, 0, {Mathf.FloorToInt(pos.z)}");
        //Chunk chunktemp = chunkGameObject.AddComponent<Chunk>();
        //chunktemp.InitChunk(new Vector3(0, 0, 0), this);

        //添加到字典
        Allchunks.Add(pos, _chunk_temp);

    }

    //添加到等待删除队列
    public void AddtoRemoveChunks(Vector3 add_vec)
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

            yield return new WaitForSeconds(0.001f);


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


    #endregion

    #region 线程和协程

    /// <summary>
    /// 时刻检测线程
    /// </summary>
    void ThreadUpdateController()
    {
        CreateMeshCoroutineManager(); //Mesh线程常驻
        RenderCoroutineManager();//渲染线程常驻
    }

    //Mesh协程
    public void CreateMeshCoroutineManager()
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

                //print($"{GetRelaChunkLocation(chunktemp.myposition)}添加到meshQueue");

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

            //Mesh_0_TaskCount = WaitToCreateMesh.Count;
            //print("WaitToCreateMesh.Count");
            yield return new WaitForSeconds(0.001f);


        }

    }



    //渲染协程池
    public void RenderCoroutineManager()
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
                    //print($"{GetRelaChunkLocation(chunktemp.myposition)}开始渲染");

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
            catch (Exception ex)
            {
                // 捕获异常，防止协程因异常终止
                Debug.LogError($"渲染协程出错: {ex.Message}\n{ex.StackTrace}");

                hasError = true;  // 标记发生错误
                break;  // 退出当前循环，等待后重新启动
            }

            // 正常情况等待一段时间以控制渲染频率
            yield return new WaitForSeconds(0.001f);
        }

        // 如果发生了异常，等待并重启协程
        if (hasError)
        {
            Render_Coroutine = null;  // 重置协程状态
            yield return new WaitForSeconds(1f);  // 等待一段时间
            RenderCoroutineManager();  // 重新启动渲染协程
        }
    }





    #endregion

    #region 隐藏区块(什么狗屎代码)

    void Chunk_HideOrRemove(Vector3 chunklocation)
    {
        obj.HideChunk();
    }

    #endregion

    #region 工具

    /// <summary>
    /// 大区块对象
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Chunk GetChunkObject(Vector3 pos)
    {

        Allchunks.TryGetValue(GetRelaChunkLocation(pos), out Chunk chunktemp);

        return chunktemp;

    }

    /// <summary>
    /// 获取区块对象
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="chunktemp"></param>
    /// <returns></returns>
    public bool TryGetChunkObject(Vector3 pos, out Chunk chunktemp)
    {

        if (Allchunks.TryGetValue(GetRelaChunkLocation(pos), out Chunk _chunktemp))
        {
            chunktemp = _chunktemp;
            return true;
        }

        chunktemp = null;
        return false;
    }


    /// <summary>
    /// 返回方块类型,输入的是绝对坐标
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public byte GetBlockType(Vector3 pos)
    {
        //提前返回-找不到区块
        if (!Allchunks.TryGetValue(GetRelaChunkLocation(pos), out Chunk chunktemp))
            return 0;

        //提前返回-超过单个区块边界
        if (MC_Static_Chunk.isOutOfChunkRange(pos))
            return 0;

        //获取相对坐标
        Vector3 _vec = GetRelaPos(pos);

        //获取Block类型
        byte block_type = chunktemp.GetBlock((int)_vec.x, (int)_vec.y, (int)_vec.z).voxelType;

        //Return
        return block_type;

    }


    /// <summary>
    /// 外界修改方块
    /// </summary>
    /// <param name="_pos"></param>
    /// <param name="_target"></param>
    public void EditBlock(Vector3 _pos, byte _target)
    {
        Allchunks[GetRelaChunkLocation(_pos)].EditData(_pos, _target);
    }

    /// <summary>
    /// 外界修改方块
    /// </summary>
    /// <param name="_editStructs"></param>
    public void EditBlock(List<EditStruct> _editStructs)
    {
        List<Vector3> _ChunkLocations = new List<Vector3>();

        // 遍历_editStructs并存储ChunkLocations
        foreach (var item in _editStructs)
        {

            // 如果allchunks里没有pos.则_ChunkLocations添加
            if (Allchunks.ContainsKey(GetRelaChunkLocation(item.editPos)))
            {
                if (!_ChunkLocations.Contains(GetRelaChunkLocation(item.editPos)))
                {
                    _ChunkLocations.Add(GetRelaChunkLocation(item.editPos));
                }


            }
            else
            {
                print($"区块不存在:{GetRelaChunkLocation(item.editPos)}");

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
            editBlockCoroutine = StartCoroutine(Coroutine_editBlock(_editStructs, _time));
        }


    }

    IEnumerator Coroutine_editBlock(List<EditStruct> _editStructs, float _time)
    {
        foreach (var item in _editStructs)
        {
            //print("执行EditBlocks");
            Allchunks[GetRelaChunkLocation(item.editPos)].EditData(item.editPos, item.targetType);

            yield return new WaitForSeconds(_time);
        }
        editBlockCoroutine = null;
    }

    /// <summary>
    /// 对玩家碰撞盒的方块判断
    /// true：有碰撞
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool CollisionCheckForVoxel(Vector3 pos)
    {

        Vector3 realLocation = pos; //绝对坐标
        Vector3 relaLocation = GetRelaPos(realLocation);
        byte targetBlock = GetBlockType(realLocation);

        //出界判断(Chunk)
        if (!Allchunks.ContainsKey(GetRelaChunkLocation(realLocation)))
            return true;

        //出界判断(Y)
        if (realLocation.y >= TerrainData.ChunkHeight || realLocation.y < 0)
            return false;

        //如果是自定义碰撞
        if (MC_Runtime_StaticData.Instance.ItemData.items[targetBlock].isSolid && MC_Runtime_StaticData.Instance.ItemData.items[targetBlock].isDIYCollision)
        {
            realLocation = CollisionOffset(realLocation, targetBlock);
            Vector3 OffsetrelaLocation = GetRelaPos(realLocation);

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
        return MC_Runtime_StaticData.Instance.ItemData.items[Allchunks[GetRelaChunkLocation(realLocation)].voxelMap[(int)relaLocation.x, (int)relaLocation.y, (int)relaLocation.z].voxelType].isSolid;

    }

    //public float XOFFSET;
    //public float YOFFSET;
    //public float ZOFFSET;

    public Vector3 CollisionOffset(Vector3 _realPos, byte _targetType)
    {
        Vector3 _input = new Vector3(managerhub.player.horizontalInput, managerhub.player.Facing.y, managerhub.player.verticalInput);
        float _x = _realPos.x; Vector2 _xRange = MC_Runtime_StaticData.Instance.ItemData.items[_targetType].CollosionRange.xRange; float _xOffset = _x - (int)_x;
        float _y = _realPos.y; Vector2 _yRange = MC_Runtime_StaticData.Instance.ItemData.items[_targetType].CollosionRange.yRange; float _yOffset = _y - (int)_y;
        float _z = _realPos.z; Vector2 _zRange = MC_Runtime_StaticData.Instance.ItemData.items[_targetType].CollosionRange.zRange; float _zOffset = _z - (int)_z;


        //X
        if (_input.x >= 0 || _xOffset < _xRange.x)
            _x -= _xRange.x;
        else if (_input.x < 0 || _xOffset > _xRange.y)
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

    /// <summary>
    /// 放置高亮方块的
    /// 用于眼睛射线的检测
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool RayCheckForVoxel(Vector3 pos)
    {

        Vector3 realLocation = pos; //绝对坐标
        Vector3 relaLocation = GetRelaPos(realLocation);
        byte targetBlock = GetBlockType(realLocation);

        if (!Allchunks.ContainsKey(GetRelaChunkLocation(pos))) { return false; }

        //计算相对坐标
        Vector3 vec = GetRelaPos(new Vector3(pos.x, pos.y, pos.z));

        //判断XOZ上有没有出界
        if (!Allchunks.ContainsKey(GetRelaChunkLocation(pos))) { return true; }

        //判断Y上有没有出界
        if (realLocation.y >= TerrainData.ChunkHeight) { return false; }


        //如果是自定义碰撞
        if (MC_Runtime_StaticData.Instance.ItemData.items[targetBlock].isDIYCollision)
        {
            realLocation = CollisionOffset(realLocation, targetBlock);
            Vector3 OffsetrelaLocation = GetRelaPos(realLocation);

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
        return MC_Runtime_StaticData.Instance.ItemData.items[Allchunks[GetRelaChunkLocation(new Vector3(pos.x, pos.y, pos.z))].voxelMap[(int)vec.x, (int)vec.y, (int)vec.z].voxelType].canBeChoose;

    }


    /// <summary>
    /// 指定方向寻址
    /// </summary>
    /// <param name="_start"></param>
    /// <param name="_direct"></param>
    /// <returns></returns>
    public Vector3 AddressingBlock(Vector3 _start, int _direct)
    {
        Vector3 _address = _start;
        //print($"start: {_address}");

        for (int i = 0; i < TerrainData.ChunkHeight; i++)
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

    /// <summary>
    /// 给定一个初始坐标和初始方向，朝着这个方向遍历ChunkHeight，返回一个非空气坐标
    /// </summary>
    /// <param name="_start"></param>
    /// <param name="_direct"></param>
    /// <returns></returns>
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


    /// <summary>
    /// 返回可用出生点
    /// </summary>
    /// <param name="_pos"></param>
    /// <param name="_Spawns"></param>
    public void GetSpawnPos(Vector3 _pos, out List<Vector3> _Spawns)
    {
        _Spawns = new List<Vector3>();
        Vector3 _ChunkLocation = GetRelaChunkLocation(_pos);

        //提前返回-没有区块
        if (!Allchunks.TryGetValue(_ChunkLocation, out Chunk _chunktemp))
            return;

        _chunktemp.GetSpawnPos(GetRelaPos(_pos), out List<Vector3> __Spawns);
        _Spawns = new List<Vector3>(__Spawns);
    }

    #endregion

}
