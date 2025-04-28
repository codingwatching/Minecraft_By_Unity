using UnityEngine;

/// <summary>
/// 掌管update的状态父类
/// 注意继承后就不要动update函数了，重写就行
/// </summary>
public class MC_Tick_Base : MonoBehaviour
{
    protected ManagerHub managerhub => _managerhub ??= SceneData.GetManagerhub();
    private ManagerHub _managerhub;
    private Game_State lastState = Game_State.Ending;

    private void Update()
    {
        Game_State currentState = MC_Runtime_DynamicData.instance.GetGameState();
        OnceCheck(currentState);
        TickCheck(currentState);
        Handle_GameState_Tick();
    }

    private void OnceCheck(Game_State currentState)
    {
        if (currentState != lastState)
        {
            switch (currentState)
            {
                case Game_State.Start:
                    Handle_GameState_Start_Once();
                    break;
                case Game_State.Loading:
                    Handle_GameState_Loading_Once();
                    break;
                case Game_State.Playing:
                    Handle_GameState_Playing_Once();
                    break;
                case Game_State.Pause:
                    Handle_GameState_Pause_Once();
                    break;
            }

            lastState = currentState;
        }

    }
    private void TickCheck(Game_State currentState)
    {
        switch (currentState)
        {
            case Game_State.Start:
                Handle_GameState_Start();
                break;
            case Game_State.Loading:
                Handle_GameState_Loading();
                break;
            case Game_State.Playing:
                Handle_GameState_Playing();
                break;
            case Game_State.Pause:
                Handle_GameState_Pause();
                break;
        }
    }
    public virtual void Handle_GameState_Tick() { }
    public virtual void Handle_GameState_Start() { }
    public virtual void Handle_GameState_Start_Once() { }
    public virtual void Handle_GameState_Loading() { }
    public virtual void Handle_GameState_Loading_Once() { }
    public virtual void Handle_GameState_Playing() { }
    public virtual void Handle_GameState_Playing_Once() { }
    public virtual void Handle_GameState_Pause() { }
    public virtual void Handle_GameState_Pause_Once() { }

}

/// <summary>
/// 模板Tick类
/// </summary>
public class MonoTemplate : MC_Tick_Base
{
    public override void Handle_GameState_Tick()
    {
        // 每帧调用，不依赖状态切换
    }

    public override void Handle_GameState_Start()
    {
        // Start状态下每帧调用
    }

    public override void Handle_GameState_Start_Once()
    {
        // Start状态切换进来时调用一次
    }

    public override void Handle_GameState_Loading()
    {
        // Loading状态下每帧调用
    }

    public override void Handle_GameState_Loading_Once()
    {
        // Loading状态切换进来时调用一次
    }

    public override void Handle_GameState_Playing()
    {
        // Playing状态下每帧调用
    }

    public override void Handle_GameState_Playing_Once()
    {
        // Playing状态切换进来时调用一次
    }

    public override void Handle_GameState_Pause()
    {
        // Pause状态下每帧调用
    }

    public override void Handle_GameState_Pause_Once()
    {
        // Pause状态切换进来时调用一次
    }
}