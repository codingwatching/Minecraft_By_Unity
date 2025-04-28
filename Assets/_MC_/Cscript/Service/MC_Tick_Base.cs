using UnityEngine;

/// <summary>
/// �ƹ�update��״̬����
/// ע��̳к�Ͳ�Ҫ��update�����ˣ���д����
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
/// ģ��Tick��
/// </summary>
public class MonoTemplate : MC_Tick_Base
{
    public override void Handle_GameState_Tick()
    {
        // ÿ֡���ã�������״̬�л�
    }

    public override void Handle_GameState_Start()
    {
        // Start״̬��ÿ֡����
    }

    public override void Handle_GameState_Start_Once()
    {
        // Start״̬�л�����ʱ����һ��
    }

    public override void Handle_GameState_Loading()
    {
        // Loading״̬��ÿ֡����
    }

    public override void Handle_GameState_Loading_Once()
    {
        // Loading״̬�л�����ʱ����һ��
    }

    public override void Handle_GameState_Playing()
    {
        // Playing״̬��ÿ֡����
    }

    public override void Handle_GameState_Playing_Once()
    {
        // Playing״̬�л�����ʱ����һ��
    }

    public override void Handle_GameState_Pause()
    {
        // Pause״̬��ÿ֡����
    }

    public override void Handle_GameState_Pause_Once()
    {
        // Pause״̬�л�����ʱ����һ��
    }
}