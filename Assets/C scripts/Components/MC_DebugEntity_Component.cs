using Homebrew;
using MCEntity;
using UnityEngine;
using static MC_UtilityFunctions;

[RequireComponent(typeof(MC_Velocity_Component))]
public class MC_DebugEntity_Component : MonoBehaviour
{

    #region 状态

    [Foldout("实时碰撞", true)]
    [ReadOnly] public bool Front;
    [ReadOnly] public bool Back;
    [ReadOnly] public bool Left;
    [ReadOnly] public bool Right;
    [ReadOnly] public bool Up;
    [ReadOnly] public bool Down;

    #endregion


    #region 周期函数

    MC_Velocity_Component Velocity_Component;
    MC_Collider_Component Collider_Component;

    private void Awake()
    {
        Velocity_Component = GetComponent<MC_Velocity_Component>();
        Collider_Component = GetComponent<MC_Collider_Component>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {

        if (Collider_Component.managerhub.world.game_state == Game_State.Playing)
        {
            _ReferUpdate_EntityJumpAndMoving();

            _ReferUpdate_RealCollision();

            _ReferUpdate_EntityControler();

            _ReferUpdate_LifeComponentTest();
        }

       
    }


    #endregion


    #region 实时碰撞

    void _ReferUpdate_RealCollision()
    {
        Front = Collider_Component.collider_Front;
        Back = Collider_Component.collider_Back;
        Left = Collider_Component.collider_Left;
        Right = Collider_Component.collider_Right;
        Up = Collider_Component.collider_Up;
        Down = Collider_Component.collider_Down;
    }

    #endregion


    #region 实体跳跃和随机移动

    [Foldout("实体跳跃", true)]
    [Header("跳跃方向")] public Vector3 Jump_Direct;
    [Header("跳跃力度")] public float Jump_Value;
    [Header("跳跃一次")] public bool Toggle_EntityOnceJump;

    [Foldout("实体移动", true)]
    [Header("随机选点半径")] public float Debug_radious = 5f;
    [Header("移动一次")] public bool Toggle_EntityOnceMove;

    private void _ReferUpdate_EntityJumpAndMoving()
    {
        if (Toggle_EntityOnceJump)
        {
            Velocity_Component.AddForce(Jump_Direct, Jump_Value);
            Toggle_EntityOnceJump = false;
        }

        if (Toggle_EntityOnceMove)
        {
            Velocity_Component.MoveToObject(MC_UtilityFunctions.GetRandomPointInCircle(transform.position, Debug_radious));
            Toggle_EntityOnceMove = false;
        }
    }

    #endregion


    #region 暂时接管小键盘

    [Foldout("接管控制", true)]
    [Header("暂时接管小键盘--[IJKL]移动，[?]跳跃")] public bool EntityControlerEnable;
    [Header("旋转角速度")] public float RotationSpeed = 0.3f;

    void _ReferUpdate_EntityControler()
    {
        if (EntityControlerEnable)
        {
            EntityControler();
        }
    }

    void EntityControler()
    {
        // 使用小键盘控制实体方向
        
        // 检查输入并设置x和z方向的速度
        if (Input.GetKey(KeyCode.I)) // 向前
        {
            Velocity_Component.SetVelocity(BlockDirection.前, Velocity_Component.speed_move);
        }
        else if (Input.GetKey(KeyCode.K)) // 向后
        {
            Velocity_Component.SetVelocity(BlockDirection.后, Velocity_Component.speed_move);
        }

        if (Input.GetKey(KeyCode.J)) // 向左
        {
            Velocity_Component.SetVelocity(BlockDirection.左, Velocity_Component.speed_move);
        }
        else if (Input.GetKey(KeyCode.L)) // 向右
        {
            Velocity_Component.SetVelocity(BlockDirection.右, Velocity_Component.speed_move);
        }

        // 使用RightShift进行跳跃
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            Velocity_Component.EntityJump();
        }

        // 按下 < 向左旋转，> 向右旋转
        // 如果两个同时按住，则不触发旋转
        if (Input.GetKey(KeyCode.N) && !Input.GetKey(KeyCode.M))
        {
            // 向左旋转
            Velocity_Component.EntityRotation(-RotationSpeed, 0);
        }
        else if (Input.GetKey(KeyCode.M) && !Input.GetKey(KeyCode.N))
        {
            // 向右旋转
            Velocity_Component.EntityRotation(RotationSpeed, 0);
        }


    }


    #endregion


    #region Life组件测试

    [Foldout("Life组件测试", true)]
    [Header("更新的数值")] public int UpdateValue = -1;
    [Header("更新变化血条")] public bool Toggle_UpdateBlood;

    [Header("设定的血条")] public int SetValue = 20;
    [Header("更新设定血条")] public bool Toggle_UpdateSetBlood;

    MC_Life_Component life_Component;


    void _ReferUpdate_LifeComponentTest()
    {
        if (Toggle_UpdateBlood)
        {

            if (life_Component == null)
            {
                life_Component = GetComponent<MC_Life_Component>();
            }

            life_Component.UpdateEntityLife(UpdateValue, Vector3.back);

            print($"更新后血量: {life_Component.EntityBlood}");

            Toggle_UpdateBlood = false;
        }


        if (Toggle_UpdateSetBlood)
        {
            if (life_Component == null)
            {
                life_Component = GetComponent<MC_Life_Component>();
            }

            life_Component.SetEntityBlood(SetValue);

            print($"设定血量: {life_Component.EntityBlood}");

            Toggle_UpdateSetBlood = false;
        }


    }

    #endregion

}
