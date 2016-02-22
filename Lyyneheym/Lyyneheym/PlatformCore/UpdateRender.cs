﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

using Yuri;
using Yuri.Utils;
using Yuri.ILPackage;

namespace Yuri.PlatformCore
{
    /// <summary>
    /// 渲染类：负责将场景动作转化为前端事物的类
    /// </summary>
    public class UpdateRender
    {
        #region 辅助函数
        /// <summary>
        /// <para>将逆波兰式计算为等价的Double类型实例</para>
        /// <para>如果逆波兰式为空，则返回参数nullValue的值</para>
        /// </summary>
        /// <param name="polish">逆波兰式</param>
        /// <param name="nullValue">默认值</param>
        /// <returns>Double实例</returns>
        private double ParseDouble(string polish, double nullValue)
        {
            if (polish == "")
            {
                return nullValue;
            }
            return Convert.ToDouble(this.RunMana.CalculatePolish(polish));
        }

        /// <summary>
        /// <para>将逆波兰式计算为等价的Int32类型实例</para>
        /// <para>如果逆波兰式为空，则返回参数nullValue的值</para>
        /// </summary>
        /// <param name="polish">逆波兰式</param>
        /// <param name="nullValue">默认值</param>
        /// <returns>Int32实例</returns>
        private int ParseInt(string polish, int nullValue)
        {
            if (polish == "")
            {
                return nullValue;
            }
            return (int)(Convert.ToDouble(this.RunMana.CalculatePolish(polish)));
        }

        /// <summary>
        /// <para>把逆波兰式直接看做字符串处理</para>
        /// <para>如果逆波兰式为空，则返回参数nullValue的值</para>
        /// </summary>
        /// <param name="polish">逆波兰式</param>
        /// <param name="nullValue">默认值</param>
        /// <returns>String实例</returns>
        private string ParseDirectString(string polish, string nullValue)
        {
            if (polish == "")
            {
                return nullValue;
            }
            return polish;
        }
        #endregion

        #region 键位按钮状态
        /// <summary>
        /// 获取键盘上某个按键当前状态
        /// </summary>
        public KeyStates GetKeyboardState(Key key)
        {
            if (UpdateRender.KS_KEY_Dict.ContainsKey(key) == false)
            {
                UpdateRender.KS_KEY_Dict.Add(key, KeyStates.None);
                return KeyStates.None;
            }
            return UpdateRender.KS_KEY_Dict[key];
        }

        /// <summary>
        /// 设置键盘上某个按键当前状态
        /// </summary>
        public void SetKeyboardState(Key key, KeyStates state)
        {
            UpdateRender.KS_KEY_Dict[key] = state;
        }

        /// <summary>
        /// 获取鼠标某个按钮的当前状态
        /// </summary>
        public MouseButtonState GetMouseButtonState(MouseButton key)
        {
            return UpdateRender.KS_MOUSE_Dict[key];
        }

        /// <summary>
        /// 设置鼠标某个按钮的当前状态
        /// </summary>
        public void SetMouseButtonState(MouseButton key, MouseButtonState state)
        {
            UpdateRender.KS_MOUSE_Dict[key] = state;
        }

        /// <summary>
        /// 获取鼠标滚轮滚过的距离
        /// </summary>
        public int GetMouseWheelDelta()
        {
            return UpdateRender.KS_MOUSE_WHEEL_DELTA;
        }

        /// <summary>
        /// 设置鼠标滚轮滚过的距离
        /// </summary>
        public void SetMouseWheelDelta(int delta)
        {
            UpdateRender.KS_MOUSE_WHEEL_DELTA = delta;
        }

        /// <summary>
        /// 鼠标滚轮差值
        /// </summary>
        public static int KS_MOUSE_WHEEL_DELTA = 0;

        /// <summary>
        /// 鼠标按钮状态字典
        /// </summary>
        private static Dictionary<MouseButton, MouseButtonState> KS_MOUSE_Dict = new Dictionary<MouseButton, MouseButtonState>();

        /// <summary>
        /// 键盘按钮状态字典
        /// </summary>
        private static Dictionary<Key, KeyStates> KS_KEY_Dict = new Dictionary<Key, KeyStates>();
        #endregion

        #region 周期性调用
        /// <summary>
        /// 更新函数：根据鼠标状态更新游戏，它的优先级低于精灵按钮
        /// </summary>
        public void UpdateForMouseState()
        {
            // 按下了鼠标左键
            if (UpdateRender.KS_MOUSE_Dict[MouseButton.Left] == MouseButtonState.Pressed)
            {
                // 要松开才生效的情况下
                if (this.MouseLeftUpFlag == true)
                {
                    // 正在显示对话
                    if (this.isShowingDialog)
                    {
                        // 如果还在播放打字动画就跳跃
                        if (this.MsgStoryboardDict.ContainsKey(0) && this.MsgStoryboardDict[0].GetCurrentProgress() != 1.0)
                        {
                            this.MsgStoryboardDict[0].SkipToFill();
                            this.MouseLeftUpFlag = false;
                            return;
                        }
                        // 判断是否已经完成全部趟的显示
                        else if (this.pendingDialogQueue.Count == 0)
                        {
                            // 弹掉用户等待状态
                            this.RunMana.ExitCall();
                            this.isShowingDialog = false;
                            this.dialogPreStr = string.Empty;
                            // 非连续对话时消除对话框
                            if (this.IsContinousDialog == false)
                            {
                                this.viewMana.GetMessageLayer(0).Visibility = Visibility.Hidden;
                                this.HideMessageTria();
                            }
                            // 截断语音
                            this.Stopvocal();
                        }
                        // 正在显示对话则向前推进一个趟
                        else
                        {
                            this.viewMana.GetMessageLayer(0).displayBinding.Visibility = Visibility.Visible;
                            this.DrawDialogRunQueue();
                        }
                    }
                }
                // 连续按压生效的情况下
                else
                {

                }
                // 保持按下的状态
                this.MouseLeftUpFlag = false;
            }
            // 松开了鼠标左键
            else
            {
                this.MouseLeftUpFlag = true;
            }
            // 按下了鼠标右键
            if (UpdateRender.KS_MOUSE_Dict[MouseButton.Right] == MouseButtonState.Pressed)
            {
                // 要松开才生效的情况下
                if (this.MouseRightUpFlag == true)
                {
                    // 正在显示对话则隐藏对话
                    if (this.isShowingDialog)
                    {
                        var mainMsgLayer = this.viewMana.GetMessageLayer(0).displayBinding;
                        if (mainMsgLayer.Visibility == Visibility.Hidden)
                        {
                            mainMsgLayer.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            mainMsgLayer.Visibility = Visibility.Hidden;
                        }
                    }
                }
                // 连续按压生效的情况下
                else
                {

                }
                // 保持按下的状态
                this.MouseRightUpFlag = false;
            }
            else
            {
                this.MouseRightUpFlag = true;
            }
        }

        /// <summary>
        /// 更新函数：根据键盘状态更新游戏，它的优先级低于精灵按钮
        /// </summary>
        public void UpdateForKeyboardState()
        {
            
        }

        /// <summary>
        /// 更新函数：并行处理器，每帧被调用一次
        /// </summary>
        public void ParallelProcessor()
        {

        }
        
        /// <summary>
        /// 鼠标左键是否松开标志位
        /// </summary>
        private bool MouseLeftUpFlag = true;

        /// <summary>
        /// 鼠标右键是否松开标志位
        /// </summary>
        private bool MouseRightUpFlag = true;
        #endregion

        #region 文字层相关
        /// <summary>
        /// 把文字描绘到指定的文字层上
        /// </summary>
        /// <param name="msglayId">文字层ID</param>
        /// <param name="str">要描绘的字符串</param>
        public void DrawStringToMsgLayer(int msglayId, string str)
        {
            this.isShowingDialog = true;
            string[] strRuns = this.DialogToRuns(str);
            foreach (string run in strRuns)
            {
                this.pendingDialogQueue.Enqueue(run);
            }
            // 主动调用一次显示
            this.DrawDialogRunQueue();
            this.RunMana.UserWait("UpdateRender", "DialogWaitForClick");
        }

        /// <summary>
        /// 将对话队列里的一趟显示出来，如果显示完毕后队列已空则结束对话状态
        /// </summary>
        private void DrawDialogRunQueue()
        {
            if (this.pendingDialogQueue.Count != 0)
            {
                string currentRun = this.pendingDialogQueue.Dequeue();
                this.TypeWriter(0, this.dialogPreStr, currentRun, this.viewMana.GetMessageLayer(0).displayBinding, GlobalDataContainer.GAME_MSG_TYPING_DELAY);
                this.dialogPreStr += currentRun;
            }
        }

        /// <summary>
        /// 将文字直接描绘到文字层上而不等待
        /// </summary>
        /// <param name="id">文字层id</param>
        /// <param name="text">要描绘的字符串</param>
        private void DrawTextDirectly(int id, string text)
        {
            TextBlock t = this.viewMana.GetMessageLayer(id).displayBinding;
            t.Text = text;
        }

        /// <summary>
        /// 将文本处理转义并分割为趟
        /// </summary>
        /// <param name="dialogStr">要显示的文本</param>
        /// <returns>趟数组</returns>
        private string[] DialogToRuns(string dialogStr)
        {
            return dialogStr.Split(new string[] { "\\|" }, StringSplitOptions.None);
        }

        /// <summary>
        /// 在指定的文字层绑定控件上进行打字动画
        /// </summary>
        /// <param name="id">层id</param>
        /// <param name="orgString">原字符串</param>
        /// <param name="appendString">要追加的字符串</param>
        /// <param name="msglayBinding">文字层的控件</param>
        /// <param name="wordTimeSpan">字符之间的打字时间间隔</param>
        private void TypeWriter(int id, string orgString, string appendString, TextBlock msglayBinding, int wordTimeSpan)
        {
            this.HideMessageTria();
            Storyboard MsgLayerTypingStory = new Storyboard();
            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            Duration aniDuration = new Duration(TimeSpan.FromMilliseconds(wordTimeSpan * appendString.Length));
            stringAnimationUsingKeyFrames.Duration = aniDuration;
            MsgLayerTypingStory.Duration = aniDuration;
            string tmp = orgString;
            foreach (char c in appendString)
            {
                discreteStringKeyFrame = new DiscreteStringKeyFrame();
                discreteStringKeyFrame.KeyTime = KeyTime.Paced;
                tmp += c;
                discreteStringKeyFrame.Value = tmp;
                stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
            }
            Storyboard.SetTarget(stringAnimationUsingKeyFrames, msglayBinding);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            MsgLayerTypingStory.Children.Add(stringAnimationUsingKeyFrames);
            MsgLayerTypingStory.Completed += new EventHandler(this.TypeWriterAnimationCompletedCallback);
            MsgLayerTypingStory.Begin();
            this.MsgStoryboardDict[id] = MsgLayerTypingStory;
        }

        /// <summary>
        /// 打字动画完成回调
        /// </summary>
        private void TypeWriterAnimationCompletedCallback(object sender, EventArgs e)
        {
            if (this.MsgStoryboardDict.ContainsKey(0) && this.MsgStoryboardDict[0].GetCurrentProgress() == 1.0)
            {
                this.ShowMessageTria();
                this.BeginMessageTriaUpDownAnimation();
            }
        }

        /// <summary>
        /// 初始化文字小三角
        /// </summary>
        private void InitMsgLayerTria()
        {
            this.MainMsgTriangleSprite = ResourceManager.GetInstance().GetPicture(GlobalDataContainer.GAME_MESSAGELAYER_TRIA_FILENAME, new Int32Rect(-1, 0, 0, 0));
            Image TriaView = new Image();
            BitmapImage bmp = MainMsgTriangleSprite.myImage;
            this.MainMsgTriangleSprite.displayBinding = TriaView;
            TriaView.Width = bmp.PixelWidth;
            TriaView.Height = bmp.PixelHeight;
            TriaView.Source = bmp;
            TriaView.Visibility = Visibility.Hidden;
            TriaView.RenderTransform = new TranslateTransform();
            Canvas.SetLeft(TriaView, GlobalDataContainer.GAME_MESSAGELAYER_TRIA_X);
            Canvas.SetTop(TriaView, GlobalDataContainer.GAME_MESSAGELAYER_TRIA_Y);
            Canvas.SetZIndex(TriaView, GlobalDataContainer.GAME_Z_PICTURES - 1);
            this.view.BO_MainGrid.Children.Add(this.MainMsgTriangleSprite.displayBinding);
        }

        /// <summary>
        /// 隐藏对话小三角
        /// </summary>
        private void HideMessageTria()
        {
            // 只有主文字层需要作用小三角
            if (this.currentMsgLayer == 0)
            {
                this.MainMsgTriangleSprite.displayBinding.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// 显示对话小三角
        /// </summary>
        /// <param name="opacity">透明度</param>
        private void ShowMessageTria(double opacity = 1.0f)
        {
            this.MainMsgTriangleSprite.displayOpacity = opacity;
            this.MainMsgTriangleSprite.displayBinding.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 为对话小三角施加跳动动画
        /// </summary>
        private void BeginMessageTriaUpDownAnimation()
        {
            SpriteAnimation.UpDownRepeatAnimation(this.MainMsgTriangleSprite, TimeSpan.FromMilliseconds(500), 10, 0.8);
        }

        /// <summary>
        /// 设置小三角位置
        /// </summary>
        /// <param name="X">目标X坐标</param>
        /// <param name="Y">目标Y坐标</param>
        private void SetMessageTriaPosition(double X, double Y)
        {
            Canvas.SetLeft(this.view.BO_MsgTria, X);
            Canvas.SetTop(this.view.BO_MsgTria, Y);
        }

        /// <summary>
        /// 当前正在操作的文字层
        /// </summary>
        private int currentMsgLayer = 0;

        /// <summary>
        /// 是否正在显示对话
        /// </summary>
        private bool isShowingDialog = false;

        /// <summary>
        /// 获取当前是否正在显示对话
        /// </summary>
        public bool IsShowingDialog
        {
            get
            {
                return this.isShowingDialog;
            }
            private set
            {
                this.isShowingDialog = value;
            }
        }

        /// <summary>
        /// 是否下一动作仍为对话
        /// </summary>
        private bool IsContinousDialog = false;

        /// <summary>
        /// 主文字层背景精灵
        /// </summary>
        private YuriSprite MainMsgTriangleSprite;

        /// <summary>
        /// 当前已经显示的文本内容
        /// </summary>
        private string dialogPreStr = String.Empty;

        /// <summary>
        /// 待显示的文本趟队列
        /// </summary>
        private Queue<string> pendingDialogQueue = new Queue<string>();

        /// <summary>
        /// 对话故事板容器
        /// </summary>
        private Dictionary<int, Storyboard> MsgStoryboardDict = new Dictionary<int, Storyboard>();
        #endregion

        #region 渲染器类自身相关方法和引用
        /// <summary>
        /// 为更新器设置作用窗体
        /// </summary>
        /// <param name="mw">窗体引用</param>
        public void SetMainWindow(MainWindow mw)
        {
            if (mw != null)
            {
                this.view = mw;
                this.ViewLoadedInit();
            }
        }

        /// <summary>
        /// 在主视窗加载后的初始化动作
        /// </summary>
        private void ViewLoadedInit()
        {
            // 为视窗管理设置引用
            this.viewMana.SetMainWndReference(this.view);
            // 初始化小三角
            this.InitMsgLayerTria();
            // 初始化文本层
            this.viewMana.InitMessageLayer();
        }

        /// <summary>
        /// 设置运行时环境引用
        /// </summary>
        /// <param name="rm">运行时环境</param>
        public void SetRuntimeManagerReference(RuntimeManager rm)
        {
            this.RunMana = rm;
        }

        /// <summary>
        /// 渲染类构造器
        /// </summary>
        public UpdateRender()
        {
            // 初始化鼠标键位
            UpdateRender.KS_MOUSE_Dict.Add(MouseButton.Left, MouseButtonState.Released);
            UpdateRender.KS_MOUSE_Dict.Add(MouseButton.Middle, MouseButtonState.Released);
            UpdateRender.KS_MOUSE_Dict.Add(MouseButton.Right, MouseButtonState.Released);
        }

        /// <summary>
        /// 主窗体引用
        /// </summary>
        private MainWindow view = null;

        /// <summary>
        /// 运行时环境引用
        /// </summary>
        private RuntimeManager RunMana = null;

        /// <summary>
        /// 音乐引擎
        /// </summary>
        private Musician musician = Musician.GetInstance();

        /// <summary>
        /// 资源管理器
        /// </summary>
        private ResourceManager resMana = ResourceManager.GetInstance();

        /// <summary>
        /// 屏幕管理器
        /// </summary>
        private ScreenManager scrMana = ScreenManager.GetInstance();

        /// <summary>
        /// 视窗管理器
        /// </summary>
        private ViewManager viewMana = ViewManager.GetInstance();
        #endregion

        #region 演绎函数
        /// <summary>
        /// 接受一个场景动作并演绎她
        /// </summary>
        /// <param name="action">场景动作实例</param>
        public void Accept(SceneAction action)
        {
            switch (action.aType)
            {
                case SActionType.act_a:
                    this.A(
                        this.ParseDirectString(action.argsDict["name"], ""),
                        this.ParseInt(action.argsDict["vid"], -1),
                        this.ParseDirectString(action.argsDict["face"], ""),
                        this.ParseDirectString(action.argsDict["loc"], "")
                        );
                    break;
                case SActionType.act_bg:
                    this.Background(
                        this.ParseInt(action.argsDict["id"], 0),
                        this.ParseDirectString(action.argsDict["filename"], ""),
                        this.ParseDouble(action.argsDict["x"], 0),
                        this.ParseDouble(action.argsDict["y"], 0),
                        this.ParseDouble(action.argsDict["opacity"], 1),
                        this.ParseDouble(action.argsDict["xscale"], 1),
                        this.ParseDouble(action.argsDict["yscale"], 1),
                        this.ParseDouble(action.argsDict["ro"], 0),
                        SpriteAnchorType.Center,
                        new Int32Rect(-1, 0, 0, 0)
                        );
                    break;
                case SActionType.act_picture:
                    this.Picture(
                        this.ParseInt(action.argsDict["id"], 0),
                        this.ParseDirectString(action.argsDict["filename"], ""),
                        this.ParseDouble(action.argsDict["x"], 0),
                        this.ParseDouble(action.argsDict["y"], 0),
                        this.ParseDouble(action.argsDict["opacity"], 1),
                        this.ParseDouble(action.argsDict["xscale"], 1),
                        this.ParseDouble(action.argsDict["yscale"], 1),
                        this.ParseDouble(action.argsDict["ro"], 0),
                        SpriteAnchorType.Center,
                        new Int32Rect(-1, 0, 0, 0)
                        );
                    break;
                case SActionType.act_move:
                    string moveResType = action.argsDict["name"];
                    this.Move(
                        this.ParseInt(action.argsDict["id"], 0),
                        moveResType == "picture" ? ResourceType.Pictures : (moveResType == "stand" ? ResourceType.Stand : ResourceType.Background),
                        this.ParseDirectString(action.argsDict["target"], ""),
                        this.ParseDouble(action.argsDict["dash"], 1),
                        this.ParseDouble(action.argsDict["acc"], 0),
                        TimeSpan.FromMilliseconds(this.ParseDouble(action.argsDict["time"], 0))
                        );
                    break;
                case SActionType.act_deletepicture:
                    this.Deletepicture(
                        this.ParseInt(action.argsDict["id"], -1),
                        ResourceType.Pictures
                        );
                    break;
                case SActionType.act_cstand:
                    this.Cstand(
                        this.ParseInt(action.argsDict["id"], 0),
                        String.Format("{0}_{1}.png", action.argsDict["name"], action.argsDict["face"]),
                        this.ParseDouble(action.argsDict["x"], 0),
                        this.ParseDouble(action.argsDict["y"], 0),
                        this.ParseDouble(action.argsDict["opacity"], 1),
                        this.ParseDouble(action.argsDict["xscale"], 1),
                        this.ParseDouble(action.argsDict["yscale"], 1),
                        this.ParseDouble(action.argsDict["ro"], 0),
                        action.argsDict["anchor"] == "" ? (action.argsDict["anchor"] == "center" ? SpriteAnchorType.Center : SpriteAnchorType.LeftTop) : SpriteAnchorType.Center,
                        new Int32Rect(0, 0, 0, 0)
                        );
                    break;
                case SActionType.act_deletecstand:
                    this.Deletecstand(
                        (CharacterStandType)this.ParseInt(action.argsDict["id"], 5)
                        );
                    break;
                case SActionType.act_se:
                    this.Se(
                        this.ParseDirectString(action.argsDict["filename"], ""),
                        this.ParseDouble(action.argsDict["vol"], 1000)
                        );
                    break;
                case SActionType.act_bgm:
                    this.Bgm(
                        this.ParseDirectString(action.argsDict["filename"], ""),
                        this.ParseDouble(action.argsDict["vol"], 1000)
                        );
                    break;
                case SActionType.act_stopbgm:
                    this.Stopbgm();
                    break;
                case SActionType.act_vocal:
                    this.Vocal(
                        this.ParseDirectString(action.argsDict["name"], ""),
                        this.ParseInt(action.argsDict["vid"], -1),
                        this.musician.VocalDefaultVolume
                        );
                    break;
                case SActionType.act_stopvocal:
                    this.Stopvocal();
                    break;
                case SActionType.act_title:
                    break;
                case SActionType.act_menu:
                    break;
                case SActionType.act_save:
                    break;
                case SActionType.act_load:
                    break;
                case SActionType.act_label:
                    break;
                case SActionType.act_switch:
                    this.Switch(
                        this.ParseInt(action.argsDict["id"], 0),
                        this.ParseDirectString(action.argsDict["state"], "on") == "on"
                        );
                    break;
                case SActionType.act_var:
                    this.Var(
                        this.ParseDirectString(action.argsDict["name"], "$__LyynehermTempVar"),
                        this.ParseDirectString(action.argsDict["dash"], "1")
                        );
                    break;
                case SActionType.act_break:
                    this.Break(
                        action
                        );
                    break;
                case SActionType.act_shutdown:
                    this.Shutdown();
                    break;
                case SActionType.act_branch:
                    this.Branch(
                        this.ParseDirectString(action.argsDict["link"], "")
                        );
                    break;
                case SActionType.act_titlepoint:
                    break;
                case SActionType.act_freeze:
                    break;
                case SActionType.act_trans:
                    break;
                case SActionType.act_button:
                    this.Button(
                        this.ParseInt(action.argsDict["id"], 0),
                        true,
                        this.ParseDouble(action.argsDict["x"], 0),
                        this.ParseDouble(action.argsDict["y"], 0),
                        this.ParseDirectString(action.argsDict["target"], ""),
                        this.ParseDirectString(action.argsDict["normal"], ""),
                        this.ParseDirectString(action.argsDict["over"], ""),
                        this.ParseDirectString(action.argsDict["on"], ""),
                        this.ParseDirectString(action.argsDict["type"], "once")
                        );
                    break;
                case SActionType.act_deletebutton:
                    this.Deletebutton(
                        this.ParseInt(action.argsDict["id"], -1)
                        );
                    break;
                case SActionType.act_style:
                    break;
                case SActionType.act_msglayer:
                    this.MsgLayer(
                        this.ParseInt(action.argsDict["id"], 0)
                        );
                    break;
                case SActionType.act_msglayeropt:
                    this.MsgLayerOpt(
                        this.ParseInt(action.argsDict["id"], 0),
                        this.ParseDirectString(action.argsDict["target"], ""),
                        this.RunMana.CalculatePolish(action.argsDict["dash"]).ToString()
                        );
                    break;
                case SActionType.act_draw:
                    this.DrawCommand(
                        this.ParseInt(action.argsDict["id"], 0),
                        this.ParseDirectString(action.argsDict["dash"], "")
                        );
                    break;
                case SActionType.act_dialog:
                    this.Dialog(
                        action.aTag
                        );
                    break;
                case SActionType.act_dialogTerminator:
                    this.DialogTerminator(
                        action.aTag.Split('#')[1] == "1"
                        );
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 结束程序
        /// </summary>
        public void Shutdown()
        {
            DebugUtils.ConsoleLine("Shutdown is called", "UpdateRender", OutputStyle.Important);
            this.view.Close();
        }

        /// <summary>
        /// 跳过所有动画
        /// </summary>
        public void SkipAllAnimation()
        {
            SpriteAnimation.SkipAllAnimation();
        }

        /// <summary>
        /// 演绎函数：显示文本
        /// </summary>
        /// <param name="dialogStr">要显示的文本</param>
        private void Dialog(string dialogStr)
        {
            this.pendingDialog += dialogStr;
        }

        /// <summary>
        /// 演绎函数：结束本节对话并清空文字层
        /// </summary>
        /// <param name="continous">下一动作是否为对话</param>
        private void DialogTerminator(bool continous)
        {
            this.viewMana.GetMessageLayer(0).Visibility = Visibility.Visible;
            this.DrawStringToMsgLayer(0, this.pendingDialog);
            this.pendingDialog = string.Empty;
            this.IsContinousDialog = continous;
        }

        /// <summary>
        /// 演绎函数：直接描绘文字
        /// </summary>
        private void Drawtext(int id, string text)
        {
            if (id != 0)
            {
                this.DrawTextDirectly(id, text);
            }
            else
            {
                DebugUtils.ConsoleLine(String.Format("Drawtext cannot apply on MessageLayer0 (Main MsgLayer): {0}", text), 
                    "UpdateRender", OutputStyle.Error);
            }
        }

        /// <summary>
        /// 待显示的字符串
        /// </summary>
        private string pendingDialog = String.Empty;

        /// <summary>
        /// 演绎函数：角色状态设置
        /// </summary>
        /// <param name="name">角色名字</param>
        /// <param name="vid">语音序号</param>
        /// <param name="face">立绘表情</param>
        /// <param name="locStr">立绘位置</param>
        private void A(string name, int vid, string face, string locStr)
        {
            if (face != "")
            {
                this.Cstand(-1, String.Format("{0}_{1}.png", name, face), locStr, 1, 1, 1, 0, SpriteAnchorType.Center, new Int32Rect(0, 0, 0, 0));
            }
            if (vid != -1)
            {
                this.Vocal(name, vid, this.musician.VocalDefaultVolume);
            }
        }

        /// <summary>
        /// 演绎函数：中断循环
        /// </summary>
        /// <param name="breakSa">中断循环动作实例</param>
        private void Break(SceneAction breakSa)
        {
            this.RunMana.CallStack.ESP.IP = breakSa.next;
        }

        /// <summary>
        /// 演绎函数：放置按钮
        /// </summary>
        /// <param name="id"></param>
        /// <param name="enable"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <param name="over"></param>
        /// <param name="on"></param>
        /// <param name="type"></param>
        private void Button(int id, bool enable, double x, double y, string target, string normal, string over, string on, string type)
        {
            SpriteDescriptor normalDesc = new SpriteDescriptor()
            {
                resourceName = normal
            }, overDesc = null, onDesc = null;
            if (over != "")
            {
                overDesc = new SpriteDescriptor()
                {
                    resourceName = over
                };
            }
            if (on != "")
            {
                onDesc = new SpriteDescriptor()
                {
                    resourceName = on
                };
            }
            this.scrMana.AddButton(id, enable, x, y, target, type, normalDesc, overDesc, onDesc);
            this.viewMana.Draw(id, ResourceType.Button);
        }

        /// <summary>
        /// 从画面移除一个按钮
        /// </summary>
        /// <param name="id">按钮id</param>
        public void Deletebutton(int id)
        {
            if (id == -1)
            {
                this.viewMana.RemoveView(ResourceType.Button);
            }
            else
            {
                this.viewMana.RemoveButton(id);
            }
        }

        /// <summary>
        /// 演绎函数：显示背景
        /// </summary>
        /// <param name="id">图片ID</param>
        /// <param name="filename">资源名称</param>
        /// <param name="x">图片X坐标</param>
        /// <param name="y">图片Y坐标</param>
        /// <param name="opacity">不透明度</param>
        /// <param name="xscale">X缩放比</param>
        /// <param name="yscale">Y缩放比</param>
        /// <param name="ro">图片角度</param>
        /// <param name="anchor">锚点</param>
        /// <param name="cut">纹理切割矩</param>
        private void Background(int id, string filename, double x, double y, double opacity, double xscale, double yscale, double ro, SpriteAnchorType anchor, Int32Rect cut)
        {
            this.scrMana.AddBackground(id, filename, x, y, id, ro, opacity, xscale, yscale, anchor, cut);
            this.viewMana.Draw(id, ResourceType.Background);
        }

        /// <summary>
        /// 演绎函数：显示图片
        /// </summary>
        /// <param name="id">图片ID</param>
        /// <param name="filename">资源名称</param>
        /// <param name="x">图片X坐标</param>
        /// <param name="y">图片Y坐标</param>
        /// <param name="opacity">不透明度</param>
        /// <param name="xscale">X缩放比</param>
        /// <param name="yscale">Y缩放比</param>
        /// <param name="ro">角度</param>
        /// <param name="anchor">锚点</param>
        /// <param name="cut">纹理切割矩</param>
        private void Picture(int id, string filename, double x, double y, double opacity, double xscale, double yscale, double ro, SpriteAnchorType anchor, Int32Rect cut)
        {
            this.scrMana.AddPicture(id, filename, x, y, id, xscale, yscale, ro, opacity, anchor, cut);
            this.viewMana.Draw(id, ResourceType.Pictures);
        }

        /// <summary>
        /// 演绎函数：移动图片
        /// </summary>
        /// <param name="id">图片ID</param>
        /// <param name="rType">资源类型</param>
        /// <param name="property">改变的属性</param>
        /// <param name="toValue">目标值</param>
        /// <param name="acc">加速度</param>
        /// <param name="duration">完成所需时间</param>
        private void Move(int id, ResourceType rType, string property, double toValue, double acc, Duration duration)
        {
            YuriSprite actionSprite = this.viewMana.GetSprite(id, rType);
            SpriteDescriptor descriptor = this.scrMana.GetSpriteDescriptor(id, rType);
            if (actionSprite == null)
            {
                DebugUtils.ConsoleLine(String.Format("Ignored move (sprite is null): {0}, {1}", rType.ToString(), id),
                    "UpdateRender", OutputStyle.Warning);
                return;
            }
            switch (property)
            {
                case "x":
                    SpriteAnimation.XYMoveToAnimation(actionSprite, duration, toValue, actionSprite.displayY, acc, 0);
                    descriptor.X = toValue;
                    break;
                case "y":
                    SpriteAnimation.XYMoveToAnimation(actionSprite, duration, actionSprite.displayX, toValue, 0, acc);
                    descriptor.Y = toValue;
                    break;
                case "o":
                case "opacity":
                    SpriteAnimation.OpacityToAnimation(actionSprite, duration, toValue, acc);
                    descriptor.Opacity = toValue;
                    break;
                case "a":
                case "angle":
                    SpriteAnimation.RotateToAnimation(actionSprite, duration, toValue, acc);
                    descriptor.Angle = toValue;
                    break;
                case "s":
                case "scale":
                    SpriteAnimation.ScaleToAnimation(actionSprite, duration, toValue, toValue, acc, acc);
                    descriptor.ScaleX = descriptor.ScaleY = toValue;
                    break;
                case "sx":
                case "scalex":
                    SpriteAnimation.ScaleToAnimation(actionSprite, duration, toValue, descriptor.ScaleY, acc, 0);
                    descriptor.ScaleX = toValue;
                    break;
                case "sy":
                case "scaley":
                    SpriteAnimation.ScaleToAnimation(actionSprite, duration, descriptor.ScaleX, toValue, 0, acc);
                    descriptor.ScaleY = toValue;
                    break;
                default:
                    DebugUtils.ConsoleLine(String.Format("Move instruction without valid parameters: {0}", property),
                        "UpdateRender", OutputStyle.Warning);
                    break;
            }
        }

        /// <summary>
        /// 演绎函数：移除图片
        /// </summary>
        private void Deletepicture(int id, ResourceType rType)
        {
            if (id == -1)
            {
                this.viewMana.RemoveView(rType);
            }
            else
            {
                this.viewMana.RemoveSprite(id, rType);
            }
        }

        /// <summary>
        /// 演绎函数：显示立绘
        /// </summary>
        /// <param name="id">图片ID</param>
        /// <param name="filename">资源名称</param>
        /// <param name="locationStr">立绘位置字符串</param>
        /// <param name="opacity">不透明度</param>
        /// <param name="xscale">X缩放比</param>
        /// <param name="yscale">Y缩放比</param>
        /// <param name="ro">角度</param>
        /// <param name="anchor">锚点</param>
        /// <param name="cut">纹理切割矩</param>
        private void Cstand(int id, string filename, string locationStr, double opacity, double xscale, double yscale, double ro, SpriteAnchorType anchor, Int32Rect cut)
        {
            CharacterStandType cst;
            switch (locationStr)
            {
                case "l":
                case "left":
                    cst = CharacterStandType.Left;
                    if (id == -1) { id = 0; }
                    break;
                case "ml":
                case "midleft":
                    cst = CharacterStandType.MidLeft;
                    if (id == -1) { id = 1; }
                    break;
                case "mr":
                case "midright":
                    cst = CharacterStandType.MidRight;
                    if (id == -1) { id = 3; }
                    break;
                case "r":
                case "right":
                    cst = CharacterStandType.Right;
                    if (id == -1) { id = 4; }
                    break;
                default:
                    cst = CharacterStandType.Mid;
                    if (id == -1) { id = 2; }
                    break;
            }
            this.scrMana.AddCharacterStand(id, filename, cst, id, ro, opacity, anchor, cut);
            this.viewMana.Draw(id, ResourceType.Stand);
        }

        /// <summary>
        /// 演绎函数：显示立绘
        /// </summary>
        /// <param name="id">图片ID</param>
        /// <param name="filename">资源名称</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="opacity">不透明度</param>
        /// <param name="xscale">X缩放比</param>
        /// <param name="yscale">Y缩放比</param>
        /// <param name="ro">角度</param>
        /// <param name="anchor">锚点</param>
        /// <param name="cut">纹理切割矩</param>
        private void Cstand(int id, string filename, double x, double y, double opacity, double xscale, double yscale, double ro, SpriteAnchorType anchor, Int32Rect cut)
        {
            this.scrMana.AddCharacterStand(id, filename, x, y, id, ro, opacity, anchor, cut);
            this.viewMana.Draw(id, ResourceType.Stand);
        }

        /// <summary>
        /// 演绎函数：移除立绘
        /// </summary>
        private void Deletecstand(CharacterStandType cst)
        {
            if (cst == CharacterStandType.All)
            {
                this.viewMana.RemoveView(ResourceType.Stand);
            }
            else
            {
                this.viewMana.RemoveSprite(Convert.ToInt32(cst), ResourceType.Stand);
            }
        }

        /// <summary>
        /// 演绎函数：播放音效
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <param name="volume">音量</param>
        private void Se(string resourceName, double volume)
        {
            var seKVP = this.resMana.GetSE(resourceName);
            this.musician.PlaySE(seKVP.Key, seKVP.Value, (float)volume);
        }

        /// <summary>
        /// 演绎函数：播放BGM，如果是同一个文件将不会重新播放
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <param name="volume">音量</param>
        private void Bgm(string resourceName, double volume)
        {
            // 如果当前BGM就是此BGM就只调整音量
            if (this.musician.currentBGM != resourceName)
            {
                var bgmKVP = this.resMana.GetBGM(resourceName);
                this.musician.PlayBGM(resourceName, bgmKVP.Key, bgmKVP.Value, (float)volume);
            }
            else
            {
                this.musician.SetBGMVolume((float)volume);
            }
        }

        /// <summary>
        /// 演绎函数：停止BGM
        /// </summary>
        private void Stopbgm()
        {
            this.musician.StopAndReleaseBGM();
        }

        /// <summary>
        /// 演绎函数：播放Vocal，这个动作会截断正在播放的Vocal
        /// </summary>
        /// <param name="name">角色名称</param>
        /// <param name="vid">语音编号</param>
        /// <param name="volume">音量</param>
        private void Vocal(string name, int vid, double volume)
        {
            if (vid == -1) { return; }
            this.Vocal(String.Format("{0}_{1}{2}", name, vid, GlobalDataContainer.GAME_VOCAL_POSTFIX), (float)volume);
        }

        /// <summary>
        /// 演绎函数：播放Vocal，这个动作会截断正在播放的Vocal
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <param name="volume">音量</param>
        private void Vocal(string resourceName, double volume)
        {
            if (resourceName != "")
            {
                var vocalKVP = this.resMana.GetVocal(resourceName);
                this.musician.PlayVocal(vocalKVP.Key, vocalKVP.Value, (float)volume);
            }
        }

        /// <summary>
        /// 演绎函数：停止语音
        /// </summary>
        private void Stopvocal()
        {
            this.musician.StopAndReleaseVocal();
        }

        /// <summary>
        /// 回到标题，这个动作会在弹空整个调用堆栈后再施加
        /// </summary>
        private void Title()
        {
            this.RunMana.ExitAll();
            // 没有标志回归点就从程序入口重新开始
            if (this.titlePointContainer.Key == null || this.titlePointContainer.Value == null)
            {
                var mainScene = this.resMana.GetScene(GlobalDataContainer.Script_Main);
                if (mainScene == null)
                {
                    DebugUtils.ConsoleLine(String.Format("No Entry Point Scene: {0}, Program will exit.", GlobalDataContainer.Script_Main),
                        "Director", OutputStyle.Error);
                    Environment.Exit(0);
                }
                this.RunMana.CallScene(mainScene);
            }
            // 有回归点就调用回归点场景并把IP指针偏移到回归点动作
            else
            {
                this.RunMana.CallScene(this.titlePointContainer.Key);
                this.RunMana.CallStack.ESP.IP = this.titlePointContainer.Value;
            }
        }

        private void Menu()
        {

        }

        private void Save()
        {

        }

        private void Load()
        {

        }

        /// <summary>
        /// 变量操作
        /// </summary>
        /// <param name="varname">变量名</param>
        /// <param name="dashPolish">表达式的等价逆波兰式</param>
        private void Var(string varname, string dashPolish)
        {
            this.RunMana.Assignment(varname, dashPolish);
        }

        /// <summary>
        /// 开关操作
        /// </summary>
        /// <param name="switchId">开关id</param>
        /// <param name="toState">目标状态</param>
        private void Switch(int switchId, bool toState)
        {
            this.RunMana.Symbols.SwitchAssign(switchId, toState);
        }

        /// <summary>
        /// 选择项
        /// </summary>
        /// <param name="linkStr">选择项跳转链</param>
        private void Branch(string linkStr)
        {
            // 处理跳转链
            List<KeyValuePair<string, string>> tagList = new List<KeyValuePair<string, string>>();
            string[] linkItems = linkStr.Split(';');
            foreach (var linkItem in linkItems)
            {
                string[] linkPair = linkItem.Split(',');
                if (linkPair.Length == 2)
                {
                    tagList.Add(new KeyValuePair<string,string>(linkPair[0].Trim(), linkPair[1].Trim()));
                }
                else
                {
                    DebugUtils.ConsoleLine(String.Format("Ignore Branch Item: {0}", linkItem),
                        "UpdateRender", OutputStyle.Error);
                }
            }
            if (tagList.Count == 0)
            {
                return;
            }
            // 处理按钮显示参数
            double GroupX = GlobalDataContainer.GAME_WINDOW_WIDTH / 2.0 - GlobalDataContainer.GAME_BRANCH_WIDTH / 2.0;
            double BeginY = GlobalDataContainer.GAME_WINDOW_ACTUALHEIGHT / 2.0 - (GlobalDataContainer.GAME_BRANCH_HEIGHT * 2.0) * (tagList.Count / 2.0);
            double DeltaY = GlobalDataContainer.GAME_BRANCH_HEIGHT;
            // 描绘按钮
            for (int i = 0; i < tagList.Count; i++)
            {
                SpriteDescriptor normalDesc = new SpriteDescriptor()
                {
                    resourceName = GlobalDataContainer.GAME_BRANCH_BACKGROUNDNORMAL
                },
                overDesc = new SpriteDescriptor()
                {
                    resourceName = GlobalDataContainer.GAME_BRANCH_BACKGROUNDSELECT
                },
                onDesc = new SpriteDescriptor()
                {
                    resourceName = GlobalDataContainer.GAME_BRANCH_BACKGROUNDSELECT
                };
                this.scrMana.AddBranchButton(i, GroupX, BeginY + DeltaY * 2 * i, tagList[i].Value, tagList[i].Key, normalDesc, overDesc, onDesc);
                this.viewMana.Draw(i, ResourceType.BranchButton);
            }
            // 追加等待
            this.RunMana.UserWait("UpdateRender", String.Format("BranchWaitFor:{0}", linkStr));
        }

        /// <summary>
        /// 移除屏幕上所有的选择项按钮
        /// </summary>
        public void RemoveAllBranchButton()
        {
            this.viewMana.RemoveView(ResourceType.BranchButton);
        }

        /// <summary>
        /// 标记标题回归点
        /// </summary>
        private void Titlepoint()
        {
            this.titlePointContainer = new KeyValuePair<Scene, SceneAction>(
                this.resMana.GetScene(this.RunMana.CallStack.ESP.bindingSceneName),
                this.RunMana.CallStack.ESP.IP);
        }

        /// <summary>
        /// 标题动作节点
        /// </summary>
        private KeyValuePair<Scene, SceneAction> titlePointContainer = new KeyValuePair<Scene, SceneAction>(null, null);

        /// <summary>
        /// 演绎函数：选定要操作的文字层
        /// </summary>
        /// <param name="id">文字层ID</param>
        private void MsgLayer(int id)
        {
            this.currentMsgLayer = id;
        }

        /// <summary>
        /// 演绎函数：修改文字层的属性
        /// </summary>
        /// <param name="msglayId">层id</param>
        /// <param name="property">要修改的属性名</param>
        /// <param name="valueStr">值字符串</param>
        private void MsgLayerOpt(int msglayId, string property, string valueStr)
        {
            if (msglayId >= 0 && msglayId < GlobalDataContainer.GAME_MESSAGELAYER_COUNT)
            {
                MessageLayer ml = this.viewMana.GetMessageLayer(msglayId);
                MessageLayerDescriptor mld = this.scrMana.GetMsgLayerDescriptor(msglayId);
                switch (property)
                {
                    case "fs":
                    case "fontsize":
                        ml.FontSize = mld.FontSize = Convert.ToDouble(valueStr);
                        break;
                    case "fn":
                    case "fontname":
                        ml.FontName = mld.FontName = valueStr;
                        break;
                    case "fc":
                    case "fontcolor":
                        string[] rgbItem = valueStr.Split(',');
                        if (rgbItem.Length != 3)
                        {
                            DebugUtils.ConsoleLine("Font Color should be RGB format", "UpdateRender", OutputStyle.Error);
                            return;
                        }
                        ml.FontColor = mld.FontColor = Color.FromRgb(Convert.ToByte(rgbItem[0]), Convert.ToByte(rgbItem[1]), Convert.ToByte(rgbItem[2]));
                        break;
                    case "v":
                    case "visible":
                        mld.Visible = valueStr == "true";
                        ml.Visibility = mld.Visible ? Visibility.Visible : Visibility.Hidden;
                        break;
                    case "l":
                    case "lineheight":
                        ml.LineHeight = mld.LineHeight = Convert.ToDouble(valueStr);
                        break;
                    case "o":
                    case "opacity":
                        ml.Opacity = mld.Opacity = Convert.ToDouble(valueStr);
                        break;
                    case "x":
                        ml.X = mld.X = Convert.ToDouble(valueStr);
                        break;
                    case "y":
                        ml.Y = mld.Y = Convert.ToDouble(valueStr);
                        break;
                    case "z":
                        ml.Z = mld.Z = (int)Convert.ToDouble(valueStr);
                        break;
                    case "h":
                    case "height":
                        ml.Height = mld.Height = Convert.ToDouble(valueStr);
                        break;
                    case "w":
                    case "width":
                        ml.Width = mld.Width = Convert.ToDouble(valueStr);
                        break;
                    case "p":
                    case "padding":
                        string[] padItem = valueStr.Split(',');
                        ml.Padding = mld.Padding = new Thickness(Convert.ToDouble(padItem[0]), Convert.ToDouble(padItem[1]), Convert.ToDouble(padItem[2]), Convert.ToDouble(padItem[3]));
                        break;
                    case "ha":
                    case "horizontal":
                        ml.HorizontalAlignment = mld.HorizonAlign = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), valueStr, true);
                        break;
                    case "va":
                    case "vertical":
                        ml.VerticalAlignment = mld.VertiAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), valueStr);
                        break;
                    case "bg":
                    case "backgroundname":
                        mld.BackgroundResourceName = valueStr;
                        this.viewMana.Draw(msglayId, ResourceType.MessageLayerBackground);
                        break;
                    case "r":
                    case "reset":
                        this.viewMana.GetMessageLayer(msglayId).Reset();
                        break;
                    case "sr":
                    case "stylereset":
                        this.viewMana.GetMessageLayer(msglayId).StyleReset();
                        break;
                }
            }
            else
            {
                DebugUtils.ConsoleLine(String.Format("msglayeropt id out of range: MsgLayer {0}", msglayId),
                    "UpdateRender", OutputStyle.Error);
            }
        }

        /// <summary>
        /// 把字符串描绘到指定的文字层上
        /// </summary>
        /// <param name="id">文字层id</param>
        /// <param name="str"></param>
        private void DrawCommand(int id, string str)
        {
            this.Drawtext(id, str);
        }
        #endregion

    }
}