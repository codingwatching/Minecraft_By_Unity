using Homebrew;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

namespace MCEntity
{
    [RequireComponent(typeof(MC_Component_Velocity))]
    [RequireComponent(typeof(MC_Component_Physics))]
    [RequireComponent(typeof(MC_Component_Registration))]
    public class MC_Component_Life : MonoBehaviour
    {

        #region 状态

        [Foldout("状态", true)]

        [ReadOnly] public bool isEntity_Hurt = false;
        [ReadOnly] public bool isEntity_Dead = false;
        [ReadOnly] public bool isEntity_Dive = false;

        #endregion


        #region 周期函数

        MC_Component_Velocity Component_Velocity;
        MC_Component_Physics Component_Physics;
        MC_Component_AI Component_AI;
        MC_Component_Music Component_Music;
        MC_Service_World Service_World;
        MC_Component_Registration Component_Registration;
        ManagerHub managerhub;
        private void Awake()
        {
            Component_Velocity = GetComponent<MC_Component_Velocity>();
            Component_Physics = GetComponent<MC_Component_Physics>();
            Component_AI = GetComponent<MC_Component_AI>();
            Component_Music = GetComponent<MC_Component_Music>();
            Service_World = Component_Physics.managerhub.Service_World;
            Component_Registration = GetComponent<MC_Component_Registration>();
            managerhub = Component_Physics.managerhub;

            _ReferAwake_GetAllRenderersAndCreateMaterialInstance();
        }


        private void Start()
        {
            if (EntityMat != null)
            {
                originalColor = EntityMat.color; // 获取材质的原始颜色
            }

        }


        private void Update()
        {
            if (MC_Runtime_DynamicData.instance.GetGameState() == Game_State.Playing)
            {
                _ReferUpdate_CheckOxy();
                _ReferUpdate_FallingCheck();
                _ReferUpdate_DynamicEntityMatColor();
            }
        }





        #endregion


        #region 材质实例

        //创建材质实例
        [Foldout("材质实例", true)]
        [Header("(注意渲染器会选择第一个捕获的渲染器的材质作为材质实例\n如果有透明皮肤的实体需要注意)")]
        [Header("受伤渲染器")] public Renderer[] hurtRenderers;
        [Header("受伤颜色")] public Color Color_BeHurt = new Color(220 / 255f, 81 / 255f, 136 / 255f, 1f);
        [Header("水中颜色")] public Color Color_UnderWater = new Color(0x5B / 255f, 0x5B / 255f, 0x5B / 255f, 1f); private Color save_Color;
        [Header("被挤压颜色")] public Color Color_UnderBlock = new Color(0x00 / 255f, 0x00 / 255f, 0x00 / 255f, 1f);
        [Header("闪烁颜色(测试用)")] public Color Color_Blink;
        private Material EntityMat;

        //获取旗下子类的所有Renderer
        //并为他们创建材质实例
        void _ReferAwake_GetAllRenderersAndCreateMaterialInstance()
        {
            //找到所有渲染器
            if(hurtRenderers.Length == 0)
            {
                // 用一个 List 来存储所有的 Renderer，方便处理
                List<Renderer> renderersList = new List<Renderer>();

                // 递归遍历当前对象和所有子对象
                Stack<Transform> stack = new Stack<Transform>();
                stack.Push(transform);

                while (stack.Count > 0)
                {
                    Transform current = stack.Pop();

                    // 获取当前对象的 Renderer 组件
                    Renderer[] renderers = current.GetComponents<Renderer>();
                    renderersList.AddRange(renderers);

                    // 将所有子对象压入栈中，递归遍历
                    foreach (Transform child in current)
                    {
                        stack.Push(child);
                    }
                }

                // 将获取的 Renderer 转换为数组并存储到 Renderers
                hurtRenderers = renderersList.ToArray();
            }

            

            //创建材质实例
            EntityMat = new Material(hurtRenderers[0].sharedMaterial);

            save_Color = EntityMat.color;

            foreach (var item in hurtRenderers)
            {
                item.material = EntityMat;
            }
        }


        void _ReferUpdate_DynamicEntityMatColor()
        {
            //提前返回-如果有Lock
            if (_DynamicEntityColorLock)
                return;

            // 提前返回 - 如果hurt则退出
            if (isEntity_Hurt)
                return;

            //提前返回-返回255
            if (managerhub.Service_World.GetBlockType(Component_Physics.EyesPoint) == 255)
                return;

            // 默认颜色
            Color targetColor = save_Color;  

            // 如果被挤压
            if (MC_Runtime_StaticData.Instance.ItemData.items[managerhub.Service_World.GetBlockType(Component_Physics.EyesPoint)].isSolid)
                targetColor = Color_UnderBlock;
            // 如果在水里
            else if (Component_Physics.IsInTheWater(Component_Physics.HeadPoint))
                targetColor = Color_UnderWater;

            // 如果当前目标颜色与之前不同，则更新材质颜色
            if (EntityMat.GetColor("_Color") != targetColor)
            {
                EntityMat.SetColor("_Color", targetColor);
            }

        }

        /// <summary>
        /// 获取实体材质实例
        /// </summary>
        /// <returns></returns>
        public Material GetEntityMat()
        {
            return EntityMat;
        }

        /// <summary>
        /// 实例颜色锁定
        /// </summary>
        /// <param name="_lock"></param>
        private bool _DynamicEntityColorLock;
        public void DynamicEntityColorLock(bool _lock)
        {
            _DynamicEntityColorLock = _lock;
        }

        /// <summary>
        /// 改变材质实例颜色
        /// </summary>
        public void UpdateEntityColor(Color _TargetColor)
        {
            EntityMat.SetColor("_Color", _TargetColor);
        }

        /// <summary>
        /// 恢复材质实例颜色
        /// </summary>
        public void ResetEntityColor()
        {
            EntityMat.SetColor("_Color", save_Color);
        }

        #endregion


        #region 生命值部分

        [Foldout("生命值设置", true)]
        [Header("实体生命值")] public int EntityBlood = 20;
        [Header("受伤持续时间")] public float Hurt_ElapseTime = 0.3f;
        private Color originalColor; // 保存材质的原始颜色

        /// <summary>
        /// 更新实体生命值，扣血就是-1,Vector3.zero的话则不会强制移动
        /// </summary>
        public void UpdateEntityLife(int _updateLifeValue, Vector3 _hutyDirect)
        {
            //提前返回-受伤或者死亡不触发
            if (isEntity_Hurt || isEntity_Dead)
                return;

            //提前返回-0不触发
            if (_updateLifeValue == 0)
                return;

            //触发受伤程序
            if (_updateLifeValue < 0)
                Handle_Hurt(_hutyDirect);

            //逃跑
            if(Component_AI != null)
            {
                if (!Component_AI.isAggressive && Component_AI.EntityCanFlee)
                    Component_AI.EntityFlee();
            }
           

            //修改血量并检查死亡程序
            if (CheckDead_EditBlood(_updateLifeValue))
                Handle_Dead();

            //print($"受击向量: {_hutyDirect}");
        }


        void Handle_Hurt(Vector3 _hurtDirect)
        {
            isEntity_Hurt = true;

            //受伤音效
            if(Component_Music != null)
                Component_Music.PlaySound(Component_Music.BehurtClip);
            //managerhub.Service_Music.Create3DSound(transform.position, Component_Music.BehurtClip);

            //材质变红
            if (EntityMat != null)
                StartCoroutine(ChangeColorCoroutine());
            else
                Debug.LogWarning("EntityMat is not assigned!");

            //强制位移
            if (_hurtDirect != Vector3.zero)
                Component_Velocity.AddForce(_hurtDirect, Component_Velocity.force_hurt);

        }

        void Handle_Dead()
        {
            isEntity_Dead = true;
            Component_Registration.LogOffEntity();
        }


        //材质变红
        IEnumerator ChangeColorCoroutine()
        {
            // 将材质颜色变为红色
            EntityMat.color = Color_BeHurt;

            // 等待1秒
            yield return new WaitForSeconds(Hurt_ElapseTime);

            // 将材质颜色还原
            EntityMat.color = originalColor;

            isEntity_Hurt = false;
        }


        bool CheckDead_EditBlood(int _updateLifeValue)
        {
            //提前返回-如果死亡
            if (EntityBlood + _updateLifeValue <= 0)
                return true;


            EntityBlood += _updateLifeValue;


            return false;
        }


        /// <summary>
        /// 设定实体血量
        /// </summary>
        public void SetEntityBlood(int _SetValue)
        {

            if (_SetValue == 0)
            {
                Handle_Dead();
            } else if (_SetValue > 0 && _SetValue <= 20)
            {
                EntityBlood = _SetValue;
            }
            else if (_SetValue > 20)
            {
                EntityBlood = 20;
            }
            else
            {
                print("life组件出现错误");
            }

        }


        #endregion


        #region 氧气部分

        [Header("实体氧气值")] public int EntityOxygen = 10;
        Coroutine Coroutine_CheckOxy;


        void _ReferUpdate_CheckOxy()
        {
            if (managerhub.Service_World.GetBlockType(Component_Physics.EyesPoint) == VoxelData.Water && Coroutine_CheckOxy == null)
            {
                isEntity_Dive = true;
                Coroutine_CheckOxy = StartCoroutine(_CheckOxy());
            }
        }

        IEnumerator _CheckOxy()
        {
            

            while (true)
            {
                //提前返回-浮出水面
                if (managerhub.Service_World.GetBlockType(Component_Physics.EyesPoint) != VoxelData.Water)
                {
                    EntityOxygen = 10;
                    Coroutine_CheckOxy = null;
                    isEntity_Dive = false;
                    yield break;
                }

                if (EntityOxygen < 0)
                {
                    if (EntityBlood > 0)
                    {
                        UpdateEntityLife(-1, Vector3.zero);
                    }
                    else
                    {
                        Coroutine_CheckOxy = null;
                        yield break;
                    }
                    
                }
                else
                {
                    EntityOxygen--;
                }

                yield return new WaitForSeconds(1f);


            }

            
        }

        #endregion


        #region 摔落检测

        [Foldout("摔落参数", true)]
        [Header("最大摔落高度")] public float maxFallDis = 4f;
        [Header("摔落音效ID")] public int Default_DropGround = 37;
        private float realMaxY = -Mathf.Infinity;  //当前触及的最大高度

        void _ReferUpdate_FallingCheck()
        {

            if (!Component_Physics.isGround || Component_Physics.IsInTheWater(Component_Physics.EyesPoint))
            {
                //实时更新
                if (Component_Physics.FootPoint.y > realMaxY)
                    realMaxY = Component_Physics.FootPoint.y;
            }
            else
            {
                //落地检测
                float _Drop = realMaxY - Component_Physics.FootPoint.y;
                if (_Drop > maxFallDis)
                {
                    //print($"扣除血量:{_Drop - maxFallDis}");
                    UpdateEntityLife(-(int)(_Drop - maxFallDis), Vector3.zero);
                    realMaxY = Component_Physics.FootPoint.y;

                    //播放落地音效
                    managerhub.Service_Music.Create3DSound(transform.position, Default_DropGround);
                }
            }


        } 

        #endregion

    }

}

