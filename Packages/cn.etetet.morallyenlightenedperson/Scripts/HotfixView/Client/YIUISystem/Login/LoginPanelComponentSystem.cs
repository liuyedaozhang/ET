using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using System.Threading.Tasks;
#if !UNITY_EDITOR
using WeChatWASM;
#endif

namespace ET.Client
{
    /// <summary>
    /// Author  RK
    /// Date    2025.6.20
    /// Desc
    /// </summary>
    [FriendOf(typeof(LoginPanelComponent))]
    public static partial class LoginPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this LoginPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this LoginPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this LoginPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始
        #region 获取用户信息
#if !UNITY_EDITOR
        
        /// <summary>
        /// 获取用户信息[头像， 名字等]。
        /// 如果是微信，可以在data中传入 target=GameObject来指定在特定区域创建可点击区域，用户点击后拉起授权信息。 如果不传target则是全屏创建可点击区域。
        /// 如果是抖音，直接调用该方法会直接拉起授权， 抖音只能授权一次，下次在调用会得到授权失败
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        public static void GetUser(Dictionary<string, object> data, Action<bool, Dictionary<string, object>> callback)
        {
            // 参考： https://developers.weixin.qq.com/minigame/dev/api/open-api/user-info/wx.getUserInfo.html
            
            WX.GetSetting(new GetSettingOption()
            {
                success = (res) =>
                {
                    try
                    {
                        bool hasUserInfoAuth = false;
                        
                        // 安全检查 authSetting 并访问 scope.userInfo
                        if (res?.authSetting != null)
                        {
                            try
                            {
                                var authSetting = res.authSetting;
                                
                                // 检查是否存在 scope.userInfo 并且值为 true
                                if (authSetting.ContainsKey("scope.userInfo"))
                                {
                                    var value = authSetting["scope.userInfo"];
                                    hasUserInfoAuth = value != null && (bool)value;
                                    Debug.Log($"scope.userInfo 授权状态: {hasUserInfoAuth}");
                                }
                                else
                                {
                                    Debug.Log("scope.userInfo 不存在于 authSetting 中");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"检查授权状态失败: {ex.Message}");
                                hasUserInfoAuth = false;
                            }
                        }
                        else
                        {
                            Debug.Log("authSetting 为空");
                        }

                        if (hasUserInfoAuth)
                        {
                            // 已经授权，可以直接调用 GetUserInfo 获取头像昵称
                            Debug.Log("用户已授权，直接获取用户信息");
                            WX.GetUserInfo(new GetUserInfoOption()
                            {
                                success = (res2) =>
                                {
                                    try
                                    {
                                        Dictionary<string, object> _infos = new()
                                        {
                                            { "KU_Name", res2.userInfo.nickName },
                                            { "KU_Avatar", res2.userInfo.avatarUrl }
                                        };
                                        Debug.Log($"获取用户信息成功: {res2.userInfo.nickName}");
                                        callback?.Invoke(true, _infos);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.LogError($"处理用户信息失败: {ex.Message}");
                                        callback?.Invoke(false, new Dictionary<string, object>());
                                    }
                                },

                                fail = (err) =>
                                {
                                    Debug.LogError($"GetUserInfo 调用失败: {err.errMsg}");
                                    callback?.Invoke(false, new Dictionary<string, object>());
                                }
                            });
                        }
                        else
                        {
                            // 未授权，创建授权按钮
                            Debug.Log("用户未授权，创建授权按钮");
                            CreateUserInfoButton(data, callback);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"GetSetting success 回调异常: {ex.Message}");
                        CreateUserInfoButton(data, callback);
                    }
                },
                fail = (err) =>
                {
                    Debug.LogError($"GetSetting 调用失败: {err.errMsg}");
                    callback?.Invoke(false, new Dictionary<string, object>());
                }
            });
        }

        /// <summary>
        /// 创建用户信息授权按钮
        /// </summary>
        private static void CreateUserInfoButton(Dictionary<string, object> data, Action<bool, Dictionary<string, object>> callback)
        {
            try
            {
                // 计算按钮位置和大小
                int x = 0;
                int y = 0;
                int width = Screen.width;
                int height = Screen.height;
                
                if (data != null && data.ContainsKey("target") && data["target"] is GameObject targetObj)
                {
                    // 设置为特定元素所在区域
                    float[] _pos = GetScreenPosition_WX(targetObj);
                    x = (int)_pos[0];
                    y = (int)_pos[1];
                    width = (int)_pos[2];
                    height = (int)_pos[3];
                    Debug.Log($"创建授权按钮位置: x={x}, y={y}, width={width}, height={height}");
                }
                else
                {
                    Debug.Log("创建全屏授权按钮");
                }

                var _button = WX.CreateUserInfoButton(x, y, width, height, "zh_CN", false);
                _button.Show();
                _button.OnTap((res) =>
                {
                    try
                    {
                        _button.Hide();
                        
                        Debug.Log($"用户点击授权按钮，错误码: {res.errCode}");

                        if (res.errCode == 0)
                        {
                            // 用户同意授权后回调，通过回调可获取用户头像昵称信息
                            Dictionary<string, object> _infos = new()
                            {
                                { "KU_Name", res.userInfo.nickName },
                                { "KU_Avatar", res.userInfo.avatarUrl }
                            };
                            Debug.Log($"用户授权成功: {res.userInfo.nickName}");
                            callback?.Invoke(true, _infos);
                        }
                        else
                        {
                            Debug.LogWarning($"用户授权失败，错误码: {res.errCode}");
                            callback?.Invoke(false, new Dictionary<string, object>());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"处理授权回调异常: {ex.Message}");
                        callback?.Invoke(false, new Dictionary<string, object>());
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建授权按钮异常: {ex.Message}");
                callback?.Invoke(false, new Dictionary<string, object>());
            }
        }

        /// <summary>
        /// 获取元素在屏幕上的坐标信息，并且转换成微信中的坐标（左上角的x，左上角的y ， 高， 宽）, 暂时只测试了UI元素
        /// </summary>
        /// <param name="_obj">目标元素</param>
        private static float[] GetScreenPosition_WX(GameObject _obj)
        {
            try
            {
                Canvas _canvas = _obj.GetComponentInParent<Canvas>();
                if (_canvas == null) 
                {
                    Debug.LogWarning("未找到Canvas组件，使用全屏尺寸");
                    return new float[] { 0, 0, Screen.width, Screen.height };
                }

                // 获取Canvas的渲染模式
                RenderMode _renderMode = _canvas.renderMode;

                // WorldSpace模式 直接返回
                if (_renderMode == RenderMode.WorldSpace) 
                {
                    Debug.LogWarning("WorldSpace模式，使用全屏尺寸");
                    return new float[] { 0, 0, Screen.width, Screen.height };
                }

                RectTransform rectTransform = _obj.GetComponentInParent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogWarning("未找到RectTransform组件，使用全屏尺寸");
                    return new float[] { 0, 0, Screen.width, Screen.height };
                }

                Vector3[] _corners = new Vector3[4];
                // 获取元素四个角的世界坐标。 左下角是第一个点，然后顺时针旋转（左下， 左上， 右上， 右下）
                rectTransform.GetWorldCorners(_corners);

                // 世界坐标转换成屏幕坐标
                Vector3 _leftTop; // 左上角
                Vector3 _rightBottom; // 右下角
                if (_renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Screen Space - Overlay 模式
                    _leftTop = _corners[1];
                    _rightBottom = _corners[3];
                }
                else
                {
                    // Screen Space - Camera 模式
                    if (_canvas.worldCamera == null)
                    {
                        Debug.LogWarning("Screen Space - Camera 模式但worldCamera为空，使用全屏尺寸");
                        return new float[] { 0, 0, Screen.width, Screen.height };
                    }
                    _leftTop = _canvas.worldCamera.WorldToScreenPoint(_corners[1]);
                    _rightBottom = _canvas.worldCamera.WorldToScreenPoint(_corners[3]);
                }

                // 计算元素的高和宽
                float _width = Mathf.Abs(_rightBottom.x - _leftTop.x);
                float _height = Mathf.Abs(_leftTop.y - _rightBottom.y);

                // 把元素左上角的左边转换成微信的坐标，微信左上角是 (0,0)
                // 参考微信方法中的说明： WX.CreateUserInfoButton()
                float _x = _leftTop.x;
                float _y = Screen.height - _leftTop.y;

                // 确保坐标和尺寸都是正数
                _x = Mathf.Max(0, _x);
                _y = Mathf.Max(0, _y);
                _width = Mathf.Max(1, _width);
                _height = Mathf.Max(1, _height);

                return new float[] { _x, _y, _width, _height };
            }
            catch (Exception ex)
            {
                Debug.LogError($"计算屏幕位置失败: {ex.Message}");
                return new float[] { 0, 0, Screen.width, Screen.height };
            }
        }
#endif
        #endregion
        
        private static void GetUserInfo(bool success, Dictionary<string, object> data)
        {
            if (success)
            {
                string userName = data.ContainsKey("KU_Name") ? data["KU_Name"].ToString() : "未知";
                string userAvatar = data.ContainsKey("KU_Avatar") ? data["KU_Avatar"].ToString() : "未知";
                Log.Info($"用户名: {userName}, 用户头像: {userAvatar}");
            }
            else
            {
                Debug.Log("获取用户信息失败");
            }
        }
        
        [YIUIInvoke(LoginPanelComponent.OnEventWeChatInvoke)]
        private static async ETTask OnEventWeChatInvoke(this LoginPanelComponent self)
        {
#if !UNITY_EDITOR
            Log.Info("开始微信登录流程");
            
            // 确保传递正确的GameObject
            if (self.u_ComWxloginRectTransform != null)
            {
                Dictionary<string, object> userInforData = new Dictionary<string, object>
                {
                    { "target", self.u_ComWxloginRectTransform.gameObject }
                };
                GetUser(userInforData, GetUserInfo);
            }
            else
            {
                Debug.LogWarning("u_ComWxloginRectTransform 为空，使用全屏授权");
                GetUser(null, GetUserInfo);
            }
#else
            Debug.Log("编辑器模式，跳过微信登录");
            // 在编辑器中可以模拟登录成功
            GetUserInfo(true, new Dictionary<string, object>
            {
                { "KU_Name", "成为你的云" },
                { "KU_Avatar", "https://thirdwx.qlogo.cn/mmopen/vi_32/nRyOKRlTU204d2sOLkibhQashW42WibTAjPzrZKic9yrwE6zN443dsZ0GVlANTFRk6XJMcOtacWTeXWsgff5dYnrEdh5712fHQkON5QpWzgV1M/132" }
            });
#endif
            
            await ETTask.CompletedTask;
            
            // GlobalComponent globalComponent = self.Root().GetComponent<GlobalComponent>();
            // await LoginHelper.Login(
            //     self.Root(),
            //     globalComponent.GlobalConfig.Address,
            //     "1",
            //     "2"
            // );
        }
        #endregion YIUIEvent结束
    }
}