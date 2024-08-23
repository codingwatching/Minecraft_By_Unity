using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class CanvasManager : MonoBehaviour
{
    [Header("UIMAnager")]
    public List<CanvasId> UIManager = new List<CanvasId>();
    












    [Header("Transforms")]
    //��������
    public ParticleSystem partSystem;
    public World world;
    public Transform Camera;
    public MusicManager musicmanager;
    //public GameObject MainCamera;
    //public GameObject PlayerObject;
    public Player player;
    public TextMeshProUGUI selectblockname;
    public BackPackManager BackPackManager;
    public LifeManager LifeManager; 
    public TextMeshProUGUI gamemodeTEXT;
    public GameObject CreativeButtom;
    public GameObject SurvivalButtom;
    //public GameObject Survival_Screen;

    //��Ҫ��Ļ
    //public GameObject Start_Screen;
    //public GameObject Init_Screen;
    //public GameObject Loading_Screen;

    //���ɶ���
    //public Slider slider;
    //public TextMeshProUGUI tmp;
    //public GameObject handle;

    //InitScreen
    //public TextMeshProUGUI ErrorMessage;
    //public Toggle SuperPlainToggle;

    //Playing��Ļ����
    public GameObject OpenYourEyes;
    //public GameObject Debug_Screen;
    //public GameObject ToolBar;
    //public GameObject CursorCross_Screen;
    public GameObject Swimming_Screen;
    //public GameObject Pause_Screen;
    //public GameObject HowToPlay_Screen;
    public GameObject Prompt_Screen;
    public GameObject prompt;
    public TextMeshProUGUI prompt_Text;
    //public GameObject DeadScreen;

    //�޸�ֵ����
    public Slider slider_bgm;
    public Slider slider_sound;
    public Slider slider_MouseSensitivity;
    public Toggle toggle_SpaceMode;  
    public Toggle toggle_SuperMing;


    //��Ϸ״̬�ж�
    public bool isPausing = false;


    //�޸�ֵ
    [Header("Options")]
    //bgm
    public float volume_bgm = 0.5f;
    private float previous_bgm = 0.5f;

    //sound
    public float volume_sound = 0.5f;
    private float previous_sound = 0.5f;
    
    //render speed
    public float Mouse_Sensitivity = 1f;
    private float previous_Mouse_Sensitivity = 1f;

    //isSpaceMode
    public bool SpaceMode_isOn = false;
    private bool previous_spaceMode_isOn = false; 

    //isSpaceMode
    public bool SuperMining_isOn = false;
    private bool previous_SuperMining_isOn = false;

    //pormpt
    public float promptShowspeed = 400f;

    //eyestime
    public float eyesOpenTime = 5f;

    //Jump_MuaCherish
    public GameObject muacherish;
    public float speed = 1.0f; // ���Ƹ����ٶȵĲ���
    public float magnitude = 0.04f; // ���Ƹ������ȵĲ���
    public float colorSpeed = 1f; //������ɫ�ٶ�
    Coroutine muacherishCoroutine;

    //ShowBlockName
    Coroutine showblocknameCoroutine;

    //Score
    //public TextMeshProUGUI scoreText;
    

    //һ���Դ���
    bool hasExec_Playing = true;
    public bool hasExec_PromptScreen_isShow = false;
    bool hasExec_PromptScreen_isHide = true;
    bool hasExec_InWater = false;

    //debug


    //----------------------------------- �������� ---------------------------------------

    private void Start()
    {
        InitCanvasManager();

    }

    public void InitCanvasManager()
    {
        if (muacherishCoroutine == null)
        {
            muacherishCoroutine = StartCoroutine(jumpMuaCherish());
        }


        // ��ʼ���޸�ֵ����
        isPausing = false;
        volume_bgm = 0.5f;
        previous_bgm = 0.5f;
        volume_sound = 0.5f;
        previous_sound = 0.5f;
        //Mouse_Sensitivity = 1f;
        //previous_Mouse_Sensitivity = 1f;
        SpaceMode_isOn = false;
        previous_spaceMode_isOn = false;
        SuperMining_isOn = false;
        previous_SuperMining_isOn = false;

        // ��ʼ����ʾ��Ϣ��ʾ�ٶ�
        promptShowspeed = 400f;

        // ��ʼ������ʱ��
        eyesOpenTime = 5f;

        // ��ʼ����������
        speed = 1.0f;
        magnitude = 0.04f;
        colorSpeed = 1f;
        muacherishCoroutine = null;

        // ��ʼ����ʾ��������Э��
        showblocknameCoroutine = null;

        // ��ʼ��һ���Դ����ִ��״̬
        hasExec_Playing = true;
        hasExec_PromptScreen_isShow = false;
        hasExec_PromptScreen_isHide = true;
        hasExec_InWater = false;

        // ��ʼ��UI����ջ
        UIBuffer = new FixedStack<int>(5, 0);

        // ��ʼ��״̬��ر���
        isInitError = false;
        Initprogress = 0;
        NotNeedBackGround = false;

        // ��ʼ������ʱ��
        startTime = 0f;
        endTime = 0f;

        // ��ʼ����ǰ��������
        currentWorldType = VoxelData.Biome_Default;
        numberofWorldType = 7; // ����

        // ��ʼ����Ҫ�ڲ������ı���
        RenderSize_Value = 0.3f;

        // ��ʼ����������
        previous_mappedValue = 0;  // ��Ⱦ��Χ
        previous_starttoreder_mappedValue = 0;  // ��ʼ��Ⱦ��Χ


    }


    
    private void Update() 
    {


        //������
        if (world.game_state == Game_State.Loading)
        {
            LoadingWorld();

        }

        //�������
        else if (world.game_state == Game_State.Playing)
        {
            //Survival
            if (world.game_mode == GameMode.Survival)
            {
                GameMode_Survival();
            }
            //Creative
            else if (world.game_mode == GameMode.Creative)
            {
                GameMode_Creative();
            }

            
            
        }

        //Pause
        else if (world.game_state == Game_State.Pause)
        {
            Show_Esc_Screen();
        }

        Prompt_FlashLight();
    }

    //���ؽ�����
    public void LoadingWorld()
    {
        TextMeshProUGUI progressNumber = UIManager[VoxelData.ui��������].childs[0]._object.GetComponent<TextMeshProUGUI>();
        GameObject progressHandle = UIManager[VoxelData.ui��������].childs[1]._object;

        //UpdateText
        progressNumber.text = $"{(Initprogress * 100):F2} %";

        //UpdateScrollBar
        float x = Mathf.Lerp(1f, 6f, Initprogress);
        float y = Mathf.Lerp(1f, 6f, Initprogress);
        progressHandle.transform.localScale = new Vector3(x, y, 1f);

        //�������������
        if (Initprogress == 1)
        {
            world.game_state = Game_State.Playing;

            SwitchToUI(VoxelData.ui���);

        }
    }



    //----------------------------------------------------------------------------------------






    //------------------------------------- ���ð� -----------------------------------------------------------------------------------------------------------------------------------------------

    //����ui����ջ
    [Header("Transforms")]
    public NighManager nightmanager;
    public FixedStack<int> UIBuffer = new FixedStack<int>(5, 0);

    [Header("״̬")]
    public bool isInitError = false;
    [HideInInspector]public float Initprogress = 0;
    public bool NotNeedBackGround = false; //��Ϸ����ͣ���ر�����

    //Score
    float startTime; float endTime;

    // ��ǰ��������
    public int currentWorldType = VoxelData.Biome_Default;
    private int numberofWorldType = 7; // ����-��������Ҽ���

    //��Ҫ�ڲ������ı���
    public float RenderSize_Value = 0.3f;

    //��������
    private int previous_mappedValue;  //��Ⱦ��Χ 
    private int previous_starttoreder_mappedValue;  //��ʼ��Ⱦ��Χ

    //------------------------------------- ��Ҫ���� ------------------------------------------

    // ��תUI
    public void SwitchToUI(int _TargetID)
    {
        //�쳣���
        if (_TargetID < 0 || _TargetID >= UIManager.Count)
        {
            Debug.LogError("�Ƿ�ID");
            return;
        }
        
        

        //����Ŀ�������������ж�
        if (UpdateCanvasState(_TargetID))
        {
            //�����ϼ�����ʾ�¼�
            if (UIBuffer.Count() > 0)
            {

                UIManager[UIBuffer.Peek()].canvas.SetActive(false);
            }
            UIManager[_TargetID].canvas.SetActive(true);

            //Music���ڸ���Ŀ�������������ж�ǰ��
            musicmanager.PlaySound_Click();
        }


        

        UIBuffer.Push(_TargetID);  // ��Ŀ��UI��ID����̶�ջ
        //print($"���� {_TargetID}, count {UIBuffer.Count()}");
    }

    //�����ϼ�UI
    public void BackToPrevious()
    {
        if (UIBuffer.Count() > 0)
        {

            int currentUIID = UIBuffer.Pop(); // ������ǰUI��ID
            int previousUIID = UIBuffer.Peek(); // ��ȡ��һ��UI��ID������������

            // ��ʾ�ϼ�UI
            UIManager[currentUIID].canvas.SetActive(false);
            UIManager[previousUIID].canvas.SetActive(true);


            //�����ϸ����壬�����Ŀ¼����
            if (currentUIID == VoxelData.uiѡ��ϸ��)
            {
                UIManager[Detail_Number].canvas.SetActive(false);
            }


            //Music
            musicmanager.PlaySound_Click();

            //print($"��һ��UI: {previousUIID}, ��������С: {UIBuffer.Count()}");
        }
        else
        {
            Debug.Log("û����һ��UI�ɷ���");
        }
    }


    //����Ŀ�������������ж�
    //����ֵ���Ƿ���Ի���
    public bool UpdateCanvasState(int _TargetID)
    {

        //�½�����
        if (_TargetID == VoxelData.ui��ʼ��_�½�����)
        {
            //ͬ��һЩ����

            //rendersize
            UIManager[_TargetID].childs[5]._object.GetComponent<Slider>().value = RenderSize_Value;
            UIManager[_TargetID].childs[6]._object.GetComponent<TextMeshProUGUI>().text = $"��Ⱦ���룺{Mathf.RoundToInt(Mathf.Lerp(2, 23, RenderSize_Value))} ����";
        }


        //�ж��ǲ���ѡ��
        else if (_TargetID == VoxelData.uiѡ��)
        {
            if (NotNeedBackGround)
            {
                UIManager[_TargetID].canvas.GetComponent<Image>().color = new Color(0, 0, 0, 120f / 255);
            }
            else
            {
                UIManager[_TargetID].canvas.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            }
        }
        //�ж��ǲ���ѡ��ϸ��
        else if (_TargetID == VoxelData.uiѡ��ϸ��)
        {

            //ͬ��һЩ����

            //rendersize
            UIManager[VoxelData.uiѡ��ϸ��].childs[5]._object.GetComponent<Slider>().value = RenderSize_Value;
            UIManager[VoxelData.uiѡ��ϸ��].childs[22]._object.GetComponent<TextMeshProUGUI>().text = $"��Ⱦ���룺{Mathf.RoundToInt(Mathf.Lerp(2, 23, RenderSize_Value))} ����";

            if (NotNeedBackGround)
            {
                UIManager[_TargetID].canvas.GetComponent<Image>().color = new Color(0, 0, 0, 120f / 255);
            }
            else
            {
                UIManager[_TargetID].canvas.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            }

            UIManager[_TargetID].childs[Detail_Number]._object.SetActive(true);
        }

        //��Ϸģʽ���ж�
        else if (_TargetID == VoxelData.ui��������)
        {


            if (isInitError)
            {
                return false;
            }
            else
            { 

                //��Ϸ״̬�л�
                world.game_state = Game_State.Loading;

                //��������л�
                //PlayerObject.SetActive(true);
                //MainCamera.SetActive(false);

                //����
                HideCursor();
            }
            

        }

        //���ģʽ
        else if (_TargetID == VoxelData.ui���)
        {
            // �������������Ļ����
            Cursor.lockState = CursorLockMode.Locked;
            //��겻����
            Cursor.visible = false;

            if (world.game_mode == GameMode.Survival)
            {
                UIManager[_TargetID].childs[0]._object.SetActive(true);
            }
            else
            {
                UIManager[_TargetID].childs[0]._object.SetActive(false);
            }

        }

        //����Ϸ����ͣ
        else if (_TargetID == VoxelData.ui��Ϸ����ͣ)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        //����
        else if (_TargetID == VoxelData.ui����)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UIManager[_TargetID].childs[0]._object.GetComponent<TextMeshProUGUI>().text = $"������{(int)(endTime - startTime)}";
        }

        //һ�㶼��true
        return true;
    }


    //--------------------------------- ����һ�������� ---------------------------------------

    //�½��������
    public void Compoments_SaveWorldSettings(int _id)
    {
        //print("OnDeselect_SaveWorldSettings");
        switch (_id)
        {
            //��������
            case 0:
                world.worldSetting.name = UIManager[VoxelData.ui��ʼ��_�½�����].childs[0]._object.GetComponent<TMP_InputField>().text;
                break;

            //��������
            case 1:
                
                String _text = UIManager[VoxelData.ui��ʼ��_�½�����].childs[1]._object.GetComponent<TMP_InputField>().text;
                int _number;


                if (_text != null && string.IsNullOrEmpty(_text))
                {
                    isInitError = true;
                    //print("����Ϊ��!");
                    UIManager[VoxelData.ui��ʼ��_�½�����].childs[4]._object.GetComponent<TextMeshProUGUI>().text = "����Ϊ�գ�";
                }
                else
                {
                    if (int.TryParse(_text, out _number))
                    {
                        
                        if (_number > 0)
                        {
                            if (isInitError)
                            {
                                isInitError = false;
                                UIManager[VoxelData.ui��ʼ��_�½�����].childs[4]._object.GetComponent<TextMeshProUGUI>().text = " ";
                            }

                            
                            world.worldSetting.seed = _number;
                            
                        } 
                        else
                        {
                            isInitError = true;
                            //Debug.Log("���ӱ������0��");
                            UIManager[VoxelData.ui��ʼ��_�½�����].childs[4]._object.GetComponent<TextMeshProUGUI>().text = "���ӱ������0��";

                        }

                    }
                    else if (_text == "�����������������")
                    {
                        isInitError = false;
                    }

                    else
                    {
                        isInitError = true;
                        //Debug.Log("����ת��ʧ�ܣ�");
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[4]._object.GetComponent<TextMeshProUGUI>().text = "����ת��ʧ�ܣ�";
                    }
                }
                    
                
                break;

            //��Ⱦ����
            case 2:

                float Rendervalue = UIManager[VoxelData.ui��ʼ��_�½�����].childs[5]._object.GetComponent<Slider>().value;

                //����ȫ�ֱ���
                RenderSize_Value = Rendervalue;

                //��ֵ��һ�������int
                //2 7 12 17 23 
                int mappedValue = Mathf.RoundToInt(Mathf.Lerp(2, 23, Rendervalue));

                if (previous_mappedValue != mappedValue)
                {
                    //�ı��ı�
                    UIManager[VoxelData.ui��ʼ��_�½�����].childs[6]._object.GetComponent<TextMeshProUGUI>().text = $"��Ⱦ���룺{mappedValue} ����";

                    //�ı�world
                    world.renderSize = mappedValue;

                    previous_mappedValue = mappedValue;
                }

                break;

            //������Ϸģʽ
            case 3:
                if (world.game_mode == GameMode.Survival)
                {
                    //��Ϊ����ģʽ
                    world.game_mode = GameMode.Creative;
                    UIManager[VoxelData.ui��ʼ��_�½�����].childs[2]._object.GetComponent<TextMeshProUGUI>().text = "��Ϸģʽ������";
                }
                else
                {
                    //��Ϊ����ģʽ
                    world.game_mode = GameMode.Survival;
                    UIManager[VoxelData.ui��ʼ��_�½�����].childs[2]._object.GetComponent<TextMeshProUGUI>().text = "��Ϸģʽ������";
                }

                musicmanager.PlaySound_Click();
                break;

            //������������
            case 4:
                // �л�����һ����������
                currentWorldType = (currentWorldType + 1) % numberofWorldType;

                switch (currentWorldType)
                {
                    case 0:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ���ԭȺϵ";
                        break;
                    case 1:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ���ԭȺϵ";
                        break;
                    case 2:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ�ɳĮȺϵ";
                        break;
                    case 3:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ�����Ⱥϵ";
                        break;
                    case 4:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ�����Ⱥϵ";
                        break;
                    case 5:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ�Ĭ��";
                        break;
                    case 6:
                        UIManager[VoxelData.ui��ʼ��_�½�����].childs[3]._object.GetComponent<TextMeshProUGUI>().text = "�������ͣ���ƽ̹";
                        break;
                    default:
                        print("ClickToSwitchWorldType����");
                        break;
                }



                musicmanager.PlaySound_Click();
                break;

            default:
                break;
        }
    }


    //ѡ�����


    //ѡ��ϸ�� - ��Ƶ����
    public void Compoments_VideoSettings(int _id)
    {
        switch (_id)
        {
            //��Ⱦ��Χ
            case 0:
                float Rendervalue = UIManager[VoxelData.uiѡ��ϸ��].childs[5]._object.GetComponent<Slider>().value;

                //����ȫ�ֱ���
                RenderSize_Value = Rendervalue;

                //��ֵ��һ�������int
                //2 7 12 17 23 
                int mappedValue = Mathf.RoundToInt(Mathf.Lerp(2, 23, Rendervalue));

                if (previous_mappedValue != mappedValue)
                {
                    //�ı��ı�
                    UIManager[VoxelData.uiѡ��ϸ��].childs[22]._object.GetComponent<TextMeshProUGUI>().text = $"��Ⱦ���룺{mappedValue} ����";

                    //�ı�world
                    world.renderSize = mappedValue;

                    previous_mappedValue = mappedValue;
                }

                break;

            //��ʼ��Ⱦ��Χ
            case 1:
                float StartToRendervalue = UIManager[VoxelData.uiѡ��ϸ��].childs[6]._object.GetComponent<Slider>().value;

                //��ֵ��һ�������int
                //2 7 12 17 23 
                int starttoreder_mappedValue = Mathf.RoundToInt(Mathf.Lerp(1, 4, StartToRendervalue));

                if (previous_starttoreder_mappedValue != starttoreder_mappedValue)
                {
                    //�ı��ı�
                    UIManager[VoxelData.uiѡ��ϸ��].childs[23]._object.GetComponent<TextMeshProUGUI>().text = $"��ʼ��Ⱦ�ľ��룺{starttoreder_mappedValue} ����";

                    //�ı�world
                    world.StartToRender = starttoreder_mappedValue;

                    previous_starttoreder_mappedValue = starttoreder_mappedValue;
                }

                break;
            default :  
                break;
        }
    }

    //ѡ��ϸ�� - ��������Ч
    public void Compoments_MusicSettings(int _id)
    {
        float value;

        switch (_id)
        {
            //��������
            case 0:

                //GetValue
                value = UIManager[VoxelData.uiѡ��ϸ��].childs[7]._object.GetComponent<Slider>().value;

                //Update
                musicmanager.Audio_envitonment.volume = Mathf.Lerp(0f, 1f, value);

                break;

            //��Ч
            case 1:
                
                //GetValue
                value = UIManager[VoxelData.uiѡ��ϸ��].childs[8]._object.GetComponent<Slider>().value;

                //Update
                musicmanager.Audio_player_place.volume = Mathf.Lerp(0f, 0.8f, value);
                musicmanager.Audio_player_broke.volume = Mathf.Lerp(0f, 0.4f, value);
                musicmanager.Audio_player_moving.volume = Mathf.Lerp(0f, 0.6f, value);
                musicmanager.Audio_player_falling.volume = Mathf.Lerp(0f, 0.4f, value);
                musicmanager.Audio_player_diving.volume = Mathf.Lerp(0f, 0.8f, value);
                musicmanager.Audio_Click.volume = Mathf.Lerp(0f, 0.2f, value);
                musicmanager.Audio_Player_moving_swiming.volume = Mathf.Lerp(0f, 0.8f, value);

                break;
            default: 
                break;
        }
    }

    //ѡ��ϸ�� - ��ҹ����
    public void Compoments_NightSettings(int _id)
    {
        switch (_id)
        {
            //����ʱ��
            case 0:
                
                break;

            //ҹ��ʱ��
            case 1:

                break;

            //����ʱ��
            case 2:

                break;
            default : 
                break;
        }
    }

    //ѡ��ϸ�� - �������
    public void Compoments_PlayerSettings(int _id)
    {

    }


    //ѡ��ϸ�� - ��������

    //------------------------------------- ���� ------------------------------------------

    //���沢�ص�����ҳ��
    public void SaveAndBackToMenu()
    {
        StopAllCoroutines();


        SwitchToUI(VoxelData.ui�˵�);

        InitCanvasManager();
        musicmanager.InitMusicManager();
        world.InitWorld();
        player.InitPlayerManager();

    }


    //�ޱ���������´�ѡ��ui
    public void SwitchNoBackGround(bool _t) 
    {
        NotNeedBackGround = _t;
    }

    //�ر�ϸ��������Ŀ¼
    public void CloseDetail_Child(int _Id)
    {
        UIManager[VoxelData.uiѡ��ϸ��].childs[_Id]._object.SetActive(false);
    }

    //������MuaCherish
    IEnumerator jumpMuaCherish()
    {
        float offset = 0.0f;

        while (true)
        {
            // ��������
            float scaleX = 1.0f + Mathf.PingPong(offset * speed, magnitude) * 0.5f; // ����x�������
            float scaleY = 1.0f + Mathf.PingPong(offset * speed, magnitude) * 0.5f; // ����y�������
            muacherish.transform.localScale = new Vector3(scaleX, scaleY, muacherish.transform.localScale.z);

            // ������ɫ����
            Color color = new Color(
                Mathf.Sin(offset * colorSpeed) * 0.5f + 0.5f, // ��
                Mathf.Sin(offset * colorSpeed + Mathf.PI * 2 / 3) * 0.5f + 0.5f, // ��
                Mathf.Sin(offset * colorSpeed + Mathf.PI * 4 / 3) * 0.5f + 0.5f, // ��
                1f // ��͸����
            );
            muacherish.GetComponent<TextMeshProUGUI>().color = color;

            offset += Time.deltaTime;

            yield return null;
        }
    }

    //ѡ�����ѡ��ϸ������ʱ�򴫵ݵĲ���
    private int Detail_Number = 0;
    public void UpdateDetail_Number(int _t)
    {
        Detail_Number = _t;
    }

    //����վ
    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }

    //Fov
    public void ChangeFOV()
    {
        // ��ȡ��������ֵ
        float sliderValue = UIManager[VoxelData.uiѡ��].childs[0]._object.GetComponent<Slider>().value;

        // ����������ֵӳ�䵽FOV�ķ�Χ��
        float newFOV = Mathf.Lerp(50f, 90f, sliderValue);

        // ������ҵ�FOV
        player.CurrentFOV = newFOV;
        player.eyes.fieldOfView = newFOV;
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------------







    //------------------------------------- GameMode -----------------------------------------
    //Survival
    void GameMode_Survival()
    {
        if (hasExec_Playing)
        {
            //ToolBar.SetActive(true);
            //CursorCross_Screen.SetActive(true);
            Prompt_Screen.SetActive(true);

            openyoureyes();

            //��¼��ʼʱ��
            startTime = Time.time;

            hasExec_Playing = false;
        }


        //EscScreen
        Show_Esc_Screen();



        //Debug���
        if (Input.GetKeyDown(KeyCode.F3))
        {
            //Debug_Screen.SetActive(!Debug_Screen.activeSelf);
        }


        //SwimmingScreen
        if (world.GetBlockType(Camera.transform.position + new Vector3(0f, 0.2f, 0f)) == VoxelData.Water)
        {
            //��ˮ
            if (hasExec_InWater == false)
            {
                //data
                //Debug.Log("IntoWater");
                
                Swimming_Screen.SetActive(true);

                partSystem.Play();

                LifeManager.Oxy_IntoWater();

                //update
                hasExec_InWater = true;
            }

        }
        else
        {
            Swimming_Screen.SetActive(false);

            partSystem.Stop();


            //��ˮ
            if (hasExec_InWater == true)
            {

                //data
                //Debug.Log("OutWater");
                LifeManager.Oxy_OutWater();


                //update
                hasExec_InWater = false;
            }

        }
    }

    //Creative
    void GameMode_Creative()
    {
        if (hasExec_Playing)
        {
            //Loading_Screen.SetActive(false);
            //ToolBar.SetActive(true);
            //Survival_Screen.SetActive(false);
            //CursorCross_Screen.SetActive(true);
            Prompt_Screen.SetActive(true);

            //toggle_SuperMing.isOn = true;
            player.isSuperMining = true;
            openyoureyes();

            BackPackManager.CreativeMode();

            StopCoroutine(muacherishCoroutine);

            hasExec_Playing = false;
        }


        //EscScreen
        Show_Esc_Screen();



        //Debug���
        if (Input.GetKeyDown(KeyCode.F3))
        {
            //Debug_Screen.SetActive(!Debug_Screen.activeSelf);
        }


        //SwimmingScreen
        if (world.GetBlockType(Camera.transform.position) == VoxelData.Water)
        {
            //��ˮ
            Swimming_Screen.SetActive(true);
            partSystem.Play();
        }
        else
        {
            Swimming_Screen.SetActive(false);
            partSystem.Stop();

        }
    }

    //----------------------------------------------------------------------------------------






    //----------------------------------- Init_Screen ---------------------------------------
  

    //ѡ������ģʽ
    public void GamemodeToSurvival()
    {
        world.game_mode = GameMode.Survival;
        gamemodeTEXT.text = "��ǰ��Ϸģʽ������ģʽ";

        //�ı䰴ť��ɫ
        SurvivalButtom.GetComponent<Image>().color = new Color(106 / 255f, 115 / 255f, 200 / 255f, 1f);
        CreativeButtom.GetComponent<Image>().color = new Color(149 / 255f, 134 / 255f, 119 / 255f, 1f);
    }

    //ѡ����ģʽ
    public void GamemodeToCreative()
    {
        world.game_mode = GameMode.Creative;
        gamemodeTEXT.text = "��ǰ��Ϸģʽ������ģʽ";

        //�ı䰴ť��ɫ
        SurvivalButtom.GetComponent<Image>().color = new Color(149 / 255f, 134 / 255f, 119 / 255f, 1f);
        CreativeButtom.GetComponent<Image>().color = new Color(106 / 255f, 115 / 255f, 200 / 255f, 1f);
    }


    //---------------------------------------------------------------------------------------






    //--------------------------------- Loading_Screen -------------------------------------

    //Loading������ʱ���һ�����۵Ķ���
    void openyoureyes()
    {
        OpenYourEyes.SetActive(true);
        StartCoroutine(Animation_Openyoureyes());
    }

    IEnumerator Animation_Openyoureyes()
    {
        Image image = OpenYourEyes.GetComponent<Image>();

        // ��¼���俪ʼ��ʱ��
        float startTime = Time.time;

        while (Time.time - startTime < eyesOpenTime)
        {
            // ���㵱ǰ��͸����
            float alpha = Mathf.Lerp(1f, 0f, (Time.time - startTime) / eyesOpenTime);

            // ����Image�����͸����
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);

            // �ȴ�һ֡
            yield return null;
        }

        // ��������󣬽�͸��������Ϊ��ȫ͸��
        image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);

        //ֱ�Ӹ�����
        OpenYourEyes.SetActive(false);
        image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
    }

    //--------------------------------------------------------------------------------------






    //--------------------------------- Playing_Screen -------------------------------------
    //Ese_Screen
    void Show_Esc_Screen()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (!isPausing)
            {
                //δ��ͣ
                isPausing = !isPausing;
                
                SwitchToUI(VoxelData.ui��Ϸ����ͣ);
                world.game_state = Game_State.Pause;
            }
            else
            {
                //������ͣ
                isPausing = !isPausing;

                SwitchToUI(VoxelData.ui���);
                world.game_state = Game_State.Playing;
            }

        }
    }

    //�ص���Ϸ
    public void BackToGame()
    {

        isPausing = !isPausing;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SwitchToUI(VoxelData.ui���);
        world.game_state = Game_State.Playing;
    }





    ////Help����
    //IEnumerator Help_Animation_Open()
    //{
    //    HowToPlay_Screen.SetActive(true);
    //    yield return null;

    //    for (float y = 0; y <= 1; y += 0.1f)
    //    {
    //        HowToPlay_Screen.transform.localScale = new Vector3(1f, y, 1f);
    //        yield return null;
    //    }

    //    HowToPlay_Screen.transform.localScale = new Vector3(1f, 1f, 1f);
    //}

    //IEnumerator Help_Animation_Close()
    //{
    //    for (float y = 1; y >= 0; y -= 0.1f)
    //    {
    //        HowToPlay_Screen.transform.localScale = new Vector3(1f, y, 1f);
    //        yield return null;
    //    }

    //    HowToPlay_Screen.SetActive(false);

    //}



    //Help_Close
    //public void Back_Pause_Screen()
    //{
    //    StartCoroutine(Help_Animation_Close());

    //    //music
    //    musicmanager.PlaySound_Click();
    //}


    //�ֵ�Ͳ����ʾ
    void Prompt_FlashLight()
    { 
        if (player.transform.position.y <= 50f + 5f)
        {
            if (hasExec_PromptScreen_isShow == false)
            {
                First_Prompt_PlayerThe_Flashlight();
            }
            
        }

        if (Input.GetKeyDown(KeyCode.F) && hasExec_PromptScreen_isShow)
        {
            if (hasExec_PromptScreen_isHide)
            {
                StartCoroutine(Hide_Animation_PromptScreen());

                hasExec_PromptScreen_isHide = false;
            }
        }

    }

    public void First_Prompt_PlayerThe_Flashlight()
    {
        prompt_Text.text = "You can press <F> \r\nto open \"FlashLight\"";
        StartCoroutine(Show_Animation_PromptScreen());
        hasExec_PromptScreen_isShow = true;
    }

    IEnumerator Show_Animation_PromptScreen()
    {
        //����ԭ��ֵ
        Vector2 oldPosition = prompt.GetComponent<RectTransform>().anchoredPosition;
        float sx = oldPosition.x;

        //-344~344����������
        while (sx <= 344f)
        {
            sx += Time.deltaTime * promptShowspeed;
            prompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(sx, oldPosition.y);
            yield return null;
        }

        //����λ��
        prompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(344f, oldPosition.y);
        yield return null;
    }

    IEnumerator Hide_Animation_PromptScreen()
    {
        //����ԭ��ֵ
        Vector2 oldPosition = prompt.GetComponent<RectTransform>().anchoredPosition;
        float sx = oldPosition.x;

        //-344~344����������
        while (sx >= -344f)
        {
            sx -= Time.deltaTime * promptShowspeed;
            prompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(sx, oldPosition.y);
            yield return null;
        }

        //����λ��
        prompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-344f, oldPosition.y);
        prompt_Text.text = "";
        yield return null;
    }


    //--------------------------------------------------------------------------------------






    //---------------------------------- ʵʱ�޸�ֵ ------------------------------------------

    //����ֵ
    void UpdatePauseScreenValue()
    {
        //bgm volume
        volume_bgm = slider_bgm.value;
        if (volume_bgm != previous_bgm)
        {
            //bgm
            musicmanager.Audio_envitonment.volume = Mathf.Lerp(0f, 1f, volume_bgm);

            //����previous
            previous_bgm = volume_bgm;
        }


        //sound volume
        volume_sound = slider_sound.value;
        if (volume_sound != previous_sound)
        {
            //sound
            musicmanager.Audio_player_place.volume = Mathf.Lerp(0f, 1f, volume_sound);
            musicmanager.Audio_player_broke.volume = Mathf.Lerp(0f, 1f, volume_sound);
            musicmanager.Audio_player_moving.volume = Mathf.Lerp(0f, 1f, volume_sound);
            musicmanager.Audio_player_falling.volume = Mathf.Lerp(0f, 1f, volume_sound);
            musicmanager.Audio_player_diving.volume = Mathf.Lerp(0f, 1f, volume_sound);
            musicmanager.Audio_Click.volume = Mathf.Lerp(0f, 1f, volume_sound);

            //����previous
            previous_sound = volume_sound;
        }

        //MouseSensitivity
        Mouse_Sensitivity = Mathf.Lerp(1f, 4f, slider_MouseSensitivity.value);

        if (Mouse_Sensitivity != previous_Mouse_Sensitivity)
        {
            //�ı����������

            //����previous
            previous_Mouse_Sensitivity = Mouse_Sensitivity;
        }


        //space mode
        SpaceMode_isOn = toggle_SpaceMode.isOn;
        if (SpaceMode_isOn != previous_spaceMode_isOn)
        {
            if (SpaceMode_isOn)
            {
                player.gravity = -3f;
                player.isSpaceMode = true;
            }
            else
            {
                player.gravity = -20f;
                player.isSpaceMode = false;
            }

            //����previous
            previous_spaceMode_isOn = SpaceMode_isOn;
        }


        //SuperMining
        SuperMining_isOn = toggle_SuperMing.isOn;
        if (SuperMining_isOn != previous_SuperMining_isOn)
        {
            if (SuperMining_isOn)
            {
                player.isSuperMining = true;
            }
            else
            {
                player.isSuperMining = false;
            }

            //����previous
            previous_SuperMining_isOn = SuperMining_isOn;
        }

    }


    //---------------------------------------------------------------------------------------





    //---------------------------------- ���������� -----------------------------------------
    public void PlayerDead()
    {
        //DeadScreen.SetActive(true);
        SwitchToUI(VoxelData.ui����);

       

        world.game_state = Game_State.Dead;
    }

    public void PlayerClickToRestart()
    {
        //DeadScreen.SetActive(false);
        SwitchToUI(VoxelData.ui���);

        

        world.game_state = Game_State.Playing;

        LifeManager.blood = 20; 
        LifeManager.oxygen = 10;
        LifeManager.UpdatePlayerBlood(0, false, false);
        startTime = Time.time;

        player.InitPlayerLocation();
        player.transform.rotation = Quaternion.identity;

        world.Update_CenterChunks();

        world.HideFarChunks();

        openyoureyes();
    }
    //--------------------------------------------------------------------------------------






    //------------------------------------ ������ -------------------------------------------
    void HideCursor()
    {
        // �������������Ļ����
        Cursor.lockState = CursorLockMode.Locked;
        //��겻����
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        //music
        musicmanager.PlaySound_Click();

        Application.Quit();
    }

    //��ʾBlock����
    public void Change_text_selectBlockname(byte prokeblocktype)
    {
        //255�������л�����
        //��255�������ƻ����飬ֱ�Ӳ����ƻ���������־���
        if (prokeblocktype == 255)
        {
            //����з���Ļ�
            if (BackPackManager.istheindexHaveBlock(player.selectindex))
            {
                //�����Э����ִ�У���������ֹ
                if (showblocknameCoroutine != null)
                {
                    StopCoroutine(showblocknameCoroutine);
                }

                showblocknameCoroutine = StartCoroutine(showblockname(prokeblocktype));
            }
        }
        else
        {
            //�����Э����ִ�У���������ֹ
            if (showblocknameCoroutine != null)
            {
                StopCoroutine(showblocknameCoroutine);
            }

            showblocknameCoroutine = StartCoroutine(showblockname(prokeblocktype));
        }

        
    }

    IEnumerator showblockname(byte _blocktype)
    {

        if (_blocktype == 255)
        {
            //��ʾ������������2s
            selectblockname.text = world.blocktypes[BackPackManager.slots[player.selectindex].blockId].blockName;

            yield return new WaitForSeconds(2f);

            selectblockname.text = "";

            showblocknameCoroutine = null;
        }
        else
        {
            //��ʾ������������2s
            selectblockname.text = world.blocktypes[_blocktype].blockName;

            yield return new WaitForSeconds(2f);

            selectblockname.text = "";

            showblocknameCoroutine = null;
        }
        
    }

    //--------------------------------------------------------------------------------------




}


//UI������
[System.Serializable]
public class CanvasId
{
    public string name;
    public GameObject canvas;
    public List<UIChild> childs;
}

//UI����
[System.Serializable]
public class UIChild
{
    public string name;
    public GameObject _object;
}


//�̶���С��ջ
public class FixedStack<T>
{ 
    private List<T> stack;
    private int capacity;

    public FixedStack(int capacity, T initialElement)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than 0");
        }

        this.capacity = capacity;
        stack = new List<T>(capacity);

        // ��ʼ��ʱ����һ����ʼԪ��
        stack.Add(initialElement);
    }

    public void Push(T item)
    {
        if (stack.Count >= capacity)
        {
            // �����������Ƴ�����µ�Ԫ��
            stack.RemoveAt(0);
        }

        stack.Add(item);
    }

    public T Pop()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        // ��ȡջ��Ԫ��
        T item = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return item;
    }

    public T Peek()
    {
        if (IsEmpty())
        {
            throw new InvalidOperationException("Stack is empty.");
        }

        return stack[stack.Count - 1];
    }

    public bool IsEmpty()
    {
        return stack.Count == 0;
    }

    public int Count()
    {
        return stack.Count;
    }

    public int Capacity()
    {
        return capacity;
    }

    public T[] ToArray()
    {
        // �� List<T> ת��Ϊ����
        return stack.ToArray();
    }
}