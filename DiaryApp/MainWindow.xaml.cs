/*===========================================
 * 【AI友好型代码框架注释】
 * 文件名: MainWindow.xaml.cs
 * 框架类型: WPF桌面应用程序主窗口
 * 应用类型: 日记/任务/时间/打卡综合管理助手
 * 主要功能模块:
 * 1. 转换器类 (Lines: 20-104) - XAML绑定用的各种转换器
 *    - LevelToMarginConverter: 级别到边距转换器
 *    - LevelToFontWeightConverter: 级别到字体粗细转换器
 *    - LevelToFontSizeConverter: 级别到字体大小转换器
 *    - BooleanToUnderlineConverter: 布尔值到下划线转换器
 * 
 * 2. 应用资源 (Lines: 106-116) - 全局颜色定义
 *    - 预定义各种UI颜色常量
 * 
 * 3. 版本信息 (Lines: 119-124) - 应用版本和构建信息
 *    - 版本号、构建日期和时间
 * 
 * 4. 主窗口类 (Lines: 126+) - 核心应用逻辑
 *    - 成员变量 (Lines: 128-132): 应用数据、文件路径等
 *    - 路径方法 (Lines: 134-145): 获取数据文件和日志文件路径
 *    - 日志方法 (Lines: 148-198): 普通日志和崩溃日志记录
 *    - 异常处理 (Lines: 200-217): 全局异常捕获和处理
 *    - 当前选择项 (Lines: 219-226): 日记、任务、打卡条目
 *    - 构造函数 (Lines: 231-307): 初始化、异常处理设置
 *    - UI初始化 (Lines: 309-325): 窗口大小、时间线、周视图
 *    - 数据管理 (Lines: 445-494): 数据加载和保存
 *    - 窗口控制 (Lines: 496-527): 关闭、最小化、最大化等
 *    - 日记模块 (Lines: 529-1034): 日记的增删改查、搜索、标签筛选
 *    - 时间记录模块 (Lines: 1036-1267): 周视图、时间记录的添加和编辑
 *    - 打卡模块 (Lines: 1269-1444): 打卡记录、统计、连续打卡计算
 *    - 数据备份 (Lines: 1446+): 自动备份、旧备份清理
 * 
 * 【关键点标记】
 * - 异常处理区域: 用于添加新的异常类型和错误处理逻辑
 * - 数据管理区域: 注意文件I/O异常和JSON序列化错误，需添加适当的错误提示
 * - UI初始化区域: 避免在构造函数中添加耗时操作，可移至异步方法
 * - 模块事件区域: 各功能模块的事件处理集中在此，便于定位和维护
 * - 数据持久化: 所有数据变更后需调用SaveAppData()保存
 * 
 * 【报错提示】
 * - 文件操作可能抛出IOException: 需捕获并提示用户检查文件权限
 * - JSON序列化可能抛出JsonException: 需记录详细错误信息
 * - UI线程操作需注意Dispatcher异常: 确保UI元素操作在UI线程执行
 * - 窗口调整大小可能触发Win32 API异常: 已通过try-catch处理
 * - 数据访问需注意空引用异常: 对关键属性添加空值检查
 * ===========================================*/
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DiaryApp;

// 多级标题转换器
public class LevelToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            // 根据级别调整缩进，每级缩进20像素
            return new Thickness(level * 20, 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LevelToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            // 根据级别调整字体粗细
            return level switch
            {
                0 => FontWeights.Regular,
                1 => FontWeights.Bold,
                2 => FontWeights.ExtraBold,
                3 => FontWeights.UltraBold,
                _ => FontWeights.Regular
            };
        }
        return FontWeights.Regular;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LevelToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int level)
        {
            // 根据级别调整字体大小
            return level switch
            {
                0 => 14.0,
                1 => 16.0,
                2 => 18.0,
                3 => 20.0,
                _ => 14.0
            };
        }
        return 14.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// 布尔值到下划线转换器
public class BooleanToUnderlineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUnderline && isUnderline)
        {
            return TextDecorations.Underline;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public static class AppBrushes
{
    public static readonly SolidColorBrush A29BFE = new SolidColorBrush(Color.FromArgb(255, 0xA2, 0x9B, 0xFE));
    public static readonly SolidColorBrush B2BEC3 = new SolidColorBrush(Color.FromArgb(255, 0xB2, 0xBE, 0xC3));
    public static readonly SolidColorBrush DFE6E9 = new SolidColorBrush(Color.FromArgb(255, 0xDF, 0xE6, 0xE9));
    public static readonly SolidColorBrush F8F9FA = new SolidColorBrush(Color.FromArgb(255, 0xF8, 0xF9, 0xFA));
    public static readonly SolidColorBrush _00B894 = new SolidColorBrush(Color.FromArgb(255, 0x00, 0xB8, 0x94));
    public static readonly SolidColorBrush _FF7675 = new SolidColorBrush(Color.FromArgb(255, 0xFF, 0x76, 0x75));
    public static readonly SolidColorBrush _636E72 = new SolidColorBrush(Color.FromArgb(255, 0x63, 0x6E, 0x72));
    public static readonly SolidColorBrush _2D3436 = new SolidColorBrush(Color.FromArgb(255, 0x2D, 0x34, 0x36));
}

// 版本信息 - 自动更新为当前时间
public static class AppVersion
{
    public const string VERSION = "0.1.1.58";
    public static readonly string BUILD_DATE = DateTime.Now.ToString("yyyy-MM-dd");
    public static readonly string BUILD_TIME = DateTime.Now.ToString("HH:mm");
}

public partial class MainWindow : Window
{
    // 统一应用数据
    private AppData _appData = new AppData();
    private const string DATA_FILE = "app_data.json";
    private const string LOG_FILE = "app_crash_log.txt";
    
    // 提醒功能相关变量
    private DispatcherTimer _reminderTimer;
    private DateTime _lastReminderDate = DateTime.MinValue;
    
    // 获取应用数据文件的完整路径
    private string GetDataFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, DATA_FILE);
    }
    
    // 获取日志文件的完整路径
    private string GetLogFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, LOG_FILE);
    }
    
    // 记录日志
    private void Log(string message)
    {
        try
        {
            var logPath = GetLogFilePath();
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch { /* 忽略日志错误 */ }
    }
    
    // 记录崩溃日志
    private void LogCrash(string message, Exception? ex = null)
    {
        try
        {
            var logPath = GetLogFilePath();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"崩溃时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"程序版本: {AppVersion.VERSION} ({AppVersion.BUILD_DATE} {AppVersion.BUILD_TIME})");
            sb.AppendLine($"操作系统: {Environment.OSVersion}");
            sb.AppendLine($".NET版本: {Environment.Version}");
            sb.AppendLine($"机器名称: {Environment.MachineName}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"错误信息: {message}");
            if (ex != null)
            {
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("异常详情:");
                sb.AppendLine($"类型: {ex.GetType().FullName}");
                sb.AppendLine($"信息: {ex.Message}");
                sb.AppendLine($"源: {ex.Source}");
                sb.AppendLine("堆栈跟踪:");
                sb.AppendLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    sb.AppendLine("--------------------------------------------------");
                    sb.AppendLine("内部异常:");
                    sb.AppendLine($"类型: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"信息: {ex.InnerException.Message}");
                    sb.AppendLine("堆栈跟踪:");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
            }
            sb.AppendLine("==================================================");
            sb.AppendLine();
            File.AppendAllText(logPath, sb.ToString());
        }
        catch { /* 忽略日志错误 */ }
    }
    
    // 全局未处理异常处理(非UI线程)
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = ex != null ? ex.Message : "未知错误";
        LogCrash("UnhandledException (AppDomain)", ex);
        MessageBox.Show($"程序发生未处理的异常错误。\n错误信息: {msg}\n\n错误详情已保存到 crash_log.txt", 
            "程序崩溃", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    // UI线程未处理异常处理
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("DispatcherUnhandledException (UI Thread)", e.Exception);
        e.Handled = true; // 标记为已处理，防止程序退出
        MessageBox.Show($"程序发生未处理的异常错误。\n错误信息: {e.Exception.Message}\n\n错误详情已保存到 crash_log.txt", 
            "程序崩溃", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    // 当前选中的日记条目(用于编辑)
    private DiaryEntry? _currentDiaryEntry;
    
    // 当前选中的任务条目(用于编辑)
    private TaskEntry? _currentTaskEntry;
    
    // 当前选中的打卡条目(用于编辑)
    private CheckInEntry? _currentCheckInEntry;

    // 当前查看的周
    private DateTime _currentWeekStart = GetWeekStart(DateTime.Today);
    
    // 当前查看的月份
    private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    
    // 鼠标拖动相关变量
    private bool _isDragging = false;
    private int _startRow = -1;
    private int _startCol = -1;
    private int _currentRow = -1;
    private int _currentCol = -1;
    private Border? _dragPreviewBorder = null;

    public MainWindow()
    {
        // 设置全局异常处理
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // 清空旧的崩溃日志
        try { File.Delete(GetLogFilePath()); } catch { }
        
        try
        {
            Log("MainWindow构造函数开始");
            
            Log("开始调用InitializeComponent()");
            InitializeComponent();
            Log("InitializeComponent()完成");
            
            Log("开始设置窗口拖动");
            // 支持窗口拖动 (因为设置了WindowStyle="None")
            this.MouseLeftButtonDown += (s, e) =>
            {
                // 检查鼠标左键是否真实按下
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
            
            // 双击标题栏区域切换最大化/正常状态
            this.MouseDoubleClick += (s, e) =>
            {
                // 只在非最大化状态下双击才切换
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                }
            };
            Log("窗口拖动设置完成");
            
            // 设置窗口边缘调整大小
            SetupWindowResize();
            Log("窗口调整大小设置完成");
            
            Log("开始调用LoadAppData()");
            LoadAppData();
            Log("LoadAppData()完成");
            
            Log("开始调用InitializeUI()");
            InitializeUI();
            Log("InitializeUI()完成");
            
            Log("开始设置默认选项卡");
            // 默认显示日记模块
            MainTabControl.SelectedIndex = 0;
            Log("选项卡设置完成");
            
            // 添加TabControl的SelectionChanged事件处理程序
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
            Log("TabControl SelectionChanged事件处理程序添加完成");
            
            // 初始化提醒功能
            InitializeReminder();
            Log("提醒功能初始化完成");
            
            Log("MainWindow构造函数成功完成 - 窗口应该已显示");
        }
        catch (Exception ex)
        {
            Log($"MainWindow构造函数失败: {ex.Message}");
            Log($"异常堆栈: {ex.StackTrace}");
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                Log($"内部异常: {innerEx.Message}");
                Log($"内部异常堆栈: {innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
            }
            MessageBox.Show($"初始化错误: {ex.Message}\n\n详细信息: {ex.StackTrace}\n\n请查看startup_log.txt 获取更多调试信息", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    private void InitializeUI()
    {
        // 设置初始窗口大小为显示器的70%
        SetWindowSizeTo70Percent();
        
        // 设置日记周期筛选默认为"全部"
        DiaryPeriodFilterBox.SelectedIndex = 0;
        
        // 初始化日历视图
        RefreshDiaryMonthDates();
        
        // 初始化日记时间线
        RefreshDiaryTimeline();
        
        // 设置默认日期时间显示
        DateLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        
        // 初始化周视图
        UpdateWeekDisplay();
        
        // 初始化时间记录显示（确保时间记录模块在启动时就正确初始化）
        UpdateTimeRecordDisplay();
        
        // 初始化打卡统计
        UpdateCheckInStats();
        
        // 初始化个人数据
        LoadPersonalInfo();
    }
    
    private void SetWindowSizeTo70Percent()
    {
        // 获取主显示器的工作区域
        var workArea = SystemParameters.WorkArea;
        
        // 计算70%的宽度和高度
        double targetWidth = workArea.Width * 0.7;
        double targetHeight = workArea.Height * 0.7;
        
        // 设置窗口大小
        this.Width = targetWidth;
        this.Height = targetHeight;
        
        // 将窗口居中显示在工作区域
        this.Left = workArea.Left + (workArea.Width - targetWidth) / 2;
        this.Top = workArea.Top + (workArea.Height - targetHeight) / 2;
    }
    
    private void SetupWindowResize()
    {
        // 顶部调整
        TopResizeBorder.MouseLeftButtonDown += (s, e) =>
        {
            if (WindowState == WindowState.Normal)
            {
                DragResize(ResizeDirection.Top);
            }
        };
        TopResizeBorder.MouseEnter += (s, e) => 
        {
            if (WindowState == WindowState.Normal)
                Cursor = Cursors.SizeNS;
        };
        TopResizeBorder.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
        
        // 左侧调整
        LeftResizeBorder.MouseLeftButtonDown += (s, e) =>
        {
            if (WindowState == WindowState.Normal)
            {
                DragResize(ResizeDirection.Left);
            }
        };
        LeftResizeBorder.MouseEnter += (s, e) => 
        {
            if (WindowState == WindowState.Normal)
                Cursor = Cursors.SizeWE;
        };
        LeftResizeBorder.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
        
        // 右侧调整
        RightResizeBorder.MouseLeftButtonDown += (s, e) =>
        {
            if (WindowState == WindowState.Normal)
            {
                DragResize(ResizeDirection.Right);
            }
        };
        RightResizeBorder.MouseEnter += (s, e) => 
        {
            if (WindowState == WindowState.Normal)
                Cursor = Cursors.SizeWE;
        };
        RightResizeBorder.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
        
        // 底部调整
        BottomResizeBorder.MouseLeftButtonDown += (s, e) =>
        {
            if (WindowState == WindowState.Normal)
            {
                DragResize(ResizeDirection.Bottom);
            }
        };
        BottomResizeBorder.MouseEnter += (s, e) => 
        {
            if (WindowState == WindowState.Normal)
                Cursor = Cursors.SizeNS;
        };
        BottomResizeBorder.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
        
        // 右下角调整手柄
        ResizeGrip.MouseLeftButtonDown += (s, e) =>
        {
            if (WindowState == WindowState.Normal)
            {
                DragResize(ResizeDirection.BottomRight);
            }
        };
        ResizeGrip.MouseEnter += (s, e) => 
        {
            if (WindowState == WindowState.Normal)
                Cursor = Cursors.SizeNWSE;
        };
        ResizeGrip.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
    }
    
    private enum ResizeDirection { Top, Bottom, Left, Right, BottomRight }
    
    private void DragResize(ResizeDirection direction)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        if (source != null)
        {
            SendMessage(source.Handle, 0x112, (IntPtr)(61440 + direction switch
            {
                ResizeDirection.Top => 3,
                ResizeDirection.Bottom => 6,
                ResizeDirection.Left => 1,
                ResizeDirection.Right => 2,
                ResizeDirection.BottomRight => 8,
                _ => 8
            }), IntPtr.Zero);
        }
    }
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

    private void LoadAppData()
    {
        var dataFile = GetDataFilePath();
        if (File.Exists(dataFile))
        {
            try 
            {
                var json = File.ReadAllText(dataFile);
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var data = JsonSerializer.Deserialize<AppData>(json, options);
                if (data != null)
                {
                    _appData = data;
                    
                    // 过滤掉旧的打卡记录（只保留最近30天的）
                    var thirtyDaysAgo = DateTime.Today.AddDays(-29);
                    _appData.CheckIns = _appData.CheckIns
                        .Where(c => c.Date >= thirtyDaysAgo)
                        .OrderByDescending(c => c.Date)
                        .ToList();
                    
                    // 重新排序其他数据
                    _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                    _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                    _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                }
            }
            catch { /* 忽略加载错误 */ }
        }
        
        // 重新计算个人信息数值
        RecalculatePersonalInfo();
    }

    // 重新计算个人信息数值
    private void RecalculatePersonalInfo()
    {
        try
        {
            // 从所有日记中收集参数并重新计算
            decimal totalSavings = 0;
            
            // 遍历所有日记
            foreach (var diary in _appData.Diaries)
            {
                // 遍历日记中的所有参数
                foreach (var param in diary.Parameters)
                {
                    string trimmedName = param.Name.Trim();
                    if (trimmedName.Equals("金钱", StringComparison.OrdinalIgnoreCase) || 
                        trimmedName.Equals("savings", StringComparison.OrdinalIgnoreCase) || 
                        trimmedName.Equals("Savings", StringComparison.OrdinalIgnoreCase))
                    {
                        if (decimal.TryParse(param.Value, out decimal paramValue))
                        {
                            totalSavings += paramValue;
                        }
                    }
                }
            }
            
            // 更新个人信息
            _appData.PersonalInfo.Savings = totalSavings;
            _appData.PersonalInfo.LastUpdated = DateTime.Now;
            
            // 更新UI显示
            PersonalSavingsTextBox.Text = totalSavings.ToString();
            PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
            
            System.Diagnostics.Debug.WriteLine($"个人信息重新计算完成：存款 = {totalSavings}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"重新计算个人信息失败：{ex.Message}");
        }
    }

    private void SaveAppData()
    {
        try
        {
            _appData.LastSaved = DateTime.Now;
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_appData, options);
            var dataFile = GetDataFilePath();
            File.WriteAllText(dataFile, json);
        }
        catch (Exception ex)
        {
            // 保存失败时，记录错误但不中断用户操作
            System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
            throw; // 重新抛出异常，让调用者决定如何处理
        }
    }

    #region 窗口控制事件

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 如果启用了后台运行，则隐藏窗口而不是关闭
        if (_appData.ReminderSetting.IsEnabled && _appData.ReminderSetting.IsMinimizedToTray)
        {
            HideWindow();
        }
        else
        {
            ExitApplication();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_appData);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
        
        // 保存设置并更新提醒定时器
        SaveAppData();
        UpdateReminderTimer();
    }
    
    // 后台运行按钮点击事件
    private void BackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }
    
    // 显示主窗口
    private void ShowWindow()
    {
        this.Visibility = Visibility.Visible;
        this.WindowState = WindowState.Normal;
        this.Activate();
    }
    
    // 隐藏主窗口（后台运行）
    private void HideWindow()
    {
        this.Visibility = Visibility.Hidden;
    }
    
    // 退出应用程序
    private void ExitApplication()
    {
        // 停止定时器
        if (_reminderTimer != null)
        {
            _reminderTimer.Stop();
        }
        
        Application.Current.Shutdown();
    }
    
    // 初始化提醒功能
    private void InitializeReminder()
    {
        // 创建提醒定时器，每分钟检查一次
        _reminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _reminderTimer.Tick += ReminderTimer_Tick;
        
        // 根据当前配置启动或停止定时器
        UpdateReminderTimer();
    }
    
    // 根据配置更新提醒定时器
    private void UpdateReminderTimer()
    {
        if (_appData.ReminderSetting.IsEnabled)
        {
            if (!_reminderTimer.IsEnabled)
            {
                _reminderTimer.Start();
            }
        }
        else
        {
            if (_reminderTimer.IsEnabled)
            {
                _reminderTimer.Stop();
            }
        }
    }
    
    // 提醒定时器的Tick事件处理程序
    private void ReminderTimer_Tick(object sender, EventArgs e)
    {
        // 检查是否应该显示提醒
        if (ShouldShowReminder())
        {
            ShowReminder();
            
            // 更新最后提醒日期
            _lastReminderDate = DateTime.Now.Date;
        }
    }
    
    // 检查是否应该显示提醒
    private bool ShouldShowReminder()
    {
        if (!_appData.ReminderSetting.IsEnabled)
        {
            return false;
        }
        
        // 检查是否是当天第一次提醒
        if (_lastReminderDate == DateTime.Now.Date)
        {
            return false;
        }
        
        // 检查当前时间是否达到提醒时间
        var now = DateTime.Now;
        var reminderTime = new DateTime(now.Year, now.Month, now.Day, 
                                      _appData.ReminderSetting.ReminderTime.Hours, 
                                      _appData.ReminderSetting.ReminderTime.Minutes, 
                                      0);
        
        return now >= reminderTime;
    }
    
    // 显示提醒消息
    private void ShowReminder()
    {
        // 使用WPF消息框显示提醒
        MessageBox.Show(_appData.ReminderSetting.ReminderMessage, "我的日记助手", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region 日记模块事件

    private void NewDiaryButton_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new DiaryEditWindow(_appData.PersonalInfo);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            _appData.Diaries.Add(editWindow.ResultEntry);
            SaveAppData();
            RefreshDiaryTimeline();
            // 重新计算个人信息数值
            RecalculatePersonalInfo();
        }
    }

    private void DiarySearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        DiarySearchPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void DiarySearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DiarySearchBox.Text))
        {
            DiarySearchPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void DiarySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshDiaryTimeline();
    }

    private void DiaryTagFilterBox_GotFocus(object sender, RoutedEventArgs e)
    {
        DiaryTagPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void DiaryTagFilterBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DiaryTagFilterBox.Text))
        {
            DiaryTagPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void DiaryTagFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshDiaryTimeline();
    }

    private void DiaryPeriodFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 隐藏提示文字
        if (DiaryPeriodPlaceholder != null)
        {
            DiaryPeriodPlaceholder.Visibility = DiaryPeriodFilterBox.SelectedIndex > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        RefreshDiaryTimeline();
    }

    private void RefreshDiaryMonthDates()
    {
        // 更新月份标题
        if (CurrentMonthText != null)
        {
            CurrentMonthText.Text = $"{_currentMonth.Year}年{_currentMonth.Month}月";
        }

        DiaryMonthCalendarPanel.Children.Clear();
        
        var firstDayOfMonth = _currentMonth;
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        
        // 获取该月第一天是星期几
        var firstDayWeekDay = (int)firstDayOfMonth.DayOfWeek;
        
        // 获取当前的搜索和筛选条件
        var searchText = DiarySearchBox.Text.ToLower().Trim();
        var tagFilter = DiaryTagFilterBox.Text.ToLower().Trim();
        var periodFilter = "";
        if (DiaryPeriodFilterBox.SelectedIndex > 0 && DiaryPeriodFilterBox.SelectedItem is ComboBoxItem selectedItem)
        {
            periodFilter = selectedItem.Content.ToString();
        }
        
        // 获取筛选后的日记（用于日历高亮显示）
        var filteredDiaries = _appData.Diaries.AsEnumerable();
        
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredDiaries = filteredDiaries.Where(d => d.SearchableText.Contains(searchText));
        }
        
        if (!string.IsNullOrEmpty(tagFilter))
        {
            filteredDiaries = filteredDiaries.Where(d => d.Tags.Any(t => t.ToLower().Contains(tagFilter)));
        }
        
        if (!string.IsNullOrEmpty(periodFilter) && periodFilter != "全部")
        {
            filteredDiaries = filteredDiaries.Where(d => d.PeriodTypeDescription == periodFilter);
        }
        
        // 获取筛选后的日期（绿色高亮）
        var filteredDates = filteredDiaries
            .Where(d => d.CreatedAt.Date >= firstDayOfMonth && d.CreatedAt.Date <= lastDayOfMonth)
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .ToHashSet();
        
        // 获取该月所有有日记的日期（蓝色普通显示）
        var datesWithDiaries = _appData.Diaries
            .Where(d => d.CreatedAt.Date >= firstDayOfMonth && d.CreatedAt.Date <= lastDayOfMonth)
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .ToHashSet();

        // 添加空白日期（月初前的空白）
        for (int i = 0; i < firstDayWeekDay; i++)
        {
            var emptyText = new TextBlock
            {
                Text = "",
                Height = 30,
                Margin = new Thickness(1)
            };
            DiaryMonthCalendarPanel.Children.Add(emptyText);
        }

        // 添加该月的所有日期
        for (var date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
        {
            var hasDiary = datesWithDiaries.Contains(date);
            var isFiltered = filteredDates.Contains(date);
            var isToday = date.Date == DateTime.Today.Date;
            
            var button = new Button
            {
                Content = $"{date.Day}",
                Height = 30,
                Margin = new Thickness(1),
                Padding = new Thickness(2),
                Background = isToday ? AppBrushes._FF7675 : 
                           (isFiltered ? (System.Windows.Media.Brush?)AppBrushes._00B894 : 
                           (hasDiary ? (System.Windows.Media.Brush?)AppBrushes.A29BFE : System.Windows.Media.Brushes.Transparent)),
                Foreground = isToday || isFiltered || hasDiary ? System.Windows.Media.Brushes.White : AppBrushes._2D3436,
                BorderThickness = isToday ? new Thickness(0) : new Thickness(1),
                BorderBrush = isToday ? AppBrushes._FF7675 : AppBrushes.DFE6E9,
                FontSize = 11,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Tag = date
            };
            button.Click += (s, e) => JumpToDate(date);
            DiaryMonthCalendarPanel.Children.Add(button);
        }
    }

    private void JumpToDate(DateTime date)
    {
        var targetEntry = _appData.Diaries
            .Where(d => d.CreatedAt.Date == date.Date)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefault();
        
        if (targetEntry != null)
        {
            // 获取当前的搜索和筛选条件
            var currentSearchText = DiarySearchBox.Text.ToLower().Trim();
            var currentTagFilter = DiaryTagFilterBox.Text.ToLower().Trim();
            var currentPeriodFilter = "";
            if (DiaryPeriodFilterBox.SelectedIndex > 0 && DiaryPeriodFilterBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentPeriodFilter = selectedItem.Content.ToString();
            }
            
            // 获取所有符合条件的日记
            var filteredDiaries = _appData.Diaries.AsEnumerable();
            
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                filteredDiaries = filteredDiaries.Where(d => d.SearchableText.Contains(currentSearchText));
            }
            
            if (!string.IsNullOrEmpty(currentTagFilter))
            {
                filteredDiaries = filteredDiaries.Where(d => d.Tags.Any(t => t.ToLower().Contains(currentTagFilter)));
            }
            
            if (!string.IsNullOrEmpty(currentPeriodFilter) && currentPeriodFilter != "全部")
            {
                filteredDiaries = filteredDiaries.Where(d => d.PeriodTypeDescription == currentPeriodFilter);
            }
            
            var sortedDiaries = filteredDiaries.OrderByDescending(d => d.CreatedAt).ToList();
            
            // 将目标日记移到最前面
            var targetIndex = sortedDiaries.FindIndex(d => d.Id == targetEntry.Id);
            if (targetIndex > 0)
            {
                sortedDiaries.RemoveAt(targetIndex);
                sortedDiaries.Insert(0, targetEntry);
            }
            
            // 重新创建时间线，将目标日记放在最上面
            DiaryTimelinePanel.Children.Clear();
            foreach (var entry in sortedDiaries)
            {
                CreateDiaryEntryPanel(entry);
            }
            
            // 展开目标日记
            ToggleDiaryEntry(targetEntry.Id, true);
            
            // 滚动到顶部（因为目标日记已经在最上面）
            if (DiaryTimelinePanel.Children.Count > 0)
            {
                var firstChild = DiaryTimelinePanel.Children[0];
                if (firstChild is Border firstBorder)
                {
                    firstBorder.BringIntoView();
                }
            }
        }
    }

    private StackPanel? FindDiaryEntryPanel(string entryId)
    {
        foreach (var child in DiaryTimelinePanel.Children)
        {
            if (child is Border border && border.Tag?.ToString() == entryId)
            {
                return border.Child as StackPanel;
            }
        }
        return null;
    }

    private void RefreshDiaryTimeline()
    {
        DiaryTimelinePanel.Children.Clear();
        
        var searchText = DiarySearchBox.Text.ToLower().Trim();
        var tagFilter = DiaryTagFilterBox.Text.ToLower().Trim();
        
        // 获取周期类型筛选
        var periodFilter = "";
        if (DiaryPeriodFilterBox.SelectedIndex > 0 && DiaryPeriodFilterBox.SelectedItem is ComboBoxItem selectedItem)
        {
            periodFilter = selectedItem.Content.ToString();
        }
        
        var filteredDiaries = _appData.Diaries.AsEnumerable();
        
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredDiaries = filteredDiaries.Where(d => d.SearchableText.Contains(searchText));
        }
        
        if (!string.IsNullOrEmpty(tagFilter))
        {
            filteredDiaries = filteredDiaries.Where(d => d.Tags.Any(t => t.ToLower().Contains(tagFilter)));
        }
        
        // 周期类型筛选
        if (!string.IsNullOrEmpty(periodFilter) && periodFilter != "全部")
        {
            filteredDiaries = filteredDiaries.Where(d => d.PeriodTypeDescription == periodFilter);
        }
        
        var sortedDiaries = filteredDiaries.OrderByDescending(d => d.CreatedAt).ToList();
        
        if (sortedDiaries.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "暂无日记",
                Foreground = AppBrushes.B2BEC3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 50, 0, 0)
            };
            DiaryTimelinePanel.Children.Add(emptyText);
            return;
        }
        
        var currentDate = "";
        foreach (var entry in sortedDiaries)
        {
            var entryDate = entry.CreatedAt.ToString("MM月dd日");
            if (entryDate != currentDate)
            {
                currentDate = entryDate;
                var dateHeader = new TextBlock
                {
                    Text = entryDate,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = AppBrushes.A29BFE,
                    Margin = new Thickness(0, 20, 0, 15)
                };
                DiaryTimelinePanel.Children.Add(dateHeader);
            }
            
            CreateDiaryEntryPanel(entry);
        }
        
        RefreshDiaryMonthDates();
    }

    private void CreateDiaryEntryPanel(DiaryEntry entry)
    {
        var mainStackPanel = new StackPanel
        {
            Tag = entry.Id,
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        // 添加鼠标点击事件到整个条目
        mainStackPanel.MouseLeftButtonDown += (s, e) =>
        {
            ToggleDiaryEntry(entry.Id, true);
            e.Handled = true; // 防止事件冒泡
        };
        
        var collapsedPanel = new StackPanel();
        
        var headerBorder = new Border
        {
            Background = System.Windows.Media.Brushes.White,
            CornerRadius = new System.Windows.CornerRadius(8),
            Padding = new Thickness(12)
        };
        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        // 创建标题行，包含标题和时间
        var titleRow = new Grid();
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var titleText = new TextBlock
        {
            Text = entry.Title,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = AppBrushes._2D3436,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(titleText, 0);
        titleRow.Children.Add(titleText);
        
        // 时间（放在右边）
        var timeText = new TextBlock
        {
            Text = entry.CreatedAt.ToString("MM月dd日 HH:mm"),
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = AppBrushes.A29BFE,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(timeText, 1);
        titleRow.Children.Add(timeText);
        
        var infoStackPanel = new StackPanel();
        infoStackPanel.Children.Add(titleRow);
        
        // 周期类型和标签的行
        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        // 周期类型
        var periodTypeText = new TextBlock
        {
            Text = entry.PeriodTypeDescription,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = AppBrushes.A29BFE,
            Margin = new Thickness(0, 0, 15, 0)
        };
        metaRow.Children.Add(periodTypeText);
        
        // 标签
        if (entry.Tags.Count > 0)
        {
            var tagsText = new TextBlock
            {
                Text = string.Join(" ", entry.Tags.Select(t => $"#{t}")),
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = AppBrushes._636E72,
                TextWrapping = TextWrapping.Wrap
            };
            metaRow.Children.Add(tagsText);
        }
        infoStackPanel.Children.Add(metaRow);
        
        Grid.SetColumn(infoStackPanel, 0);
        headerGrid.Children.Add(infoStackPanel);
        
        var expandButton = new Button
        {
            Content = "▼",
            Width = 24,
            Height = 24,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = AppBrushes.B2BEC3,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Tag = entry.Id
        };
        expandButton.Click += DiaryEntry_Expand_Click;
        Grid.SetColumn(expandButton, 1);
        headerGrid.Children.Add(expandButton);
        
        headerBorder.Child = headerGrid;
        mainStackPanel.Children.Add(headerBorder);
        
        // 创建内容面板，用于显示展开的内容
        var contentPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 5, 0, 0)
        };
        
        var contentBorder = new Border
        {
            Background = AppBrushes.F8F9FA,
            CornerRadius = new System.Windows.CornerRadius(8),
            Padding = new Thickness(12)
        };
        // 只显示内容的前三行
        var lines = entry.Content.Split('\n');
        var previewLines = lines.Take(3).ToArray();
        var previewContent = string.Join("\n", previewLines);
        if (lines.Length > 3)
        {
            previewContent += "...";
        }
        
        var contentText = new TextBlock
        {
            Text = previewContent,
            FontSize = 13,
            Foreground = AppBrushes._636E72,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 24
        };
        contentBorder.Child = contentText;
        contentPanel.Children.Add(contentBorder);
        
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        var collapseButton = new Button
        {
            Content = "收起",
            Width = 60,
            Height = 28,
            Background = AppBrushes.DFE6E9,
            Foreground = AppBrushes._2D3436,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0)
        };
        collapseButton.Click += (s, e) => ToggleDiaryEntry(entry.Id, false);
        
        var modifyButton = new Button
        {
            Content = "修改",
            Width = 60,
            Height = 28,
            Background = AppBrushes._00B894,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0)
        };
        modifyButton.Click += (s, e) => EditDiaryEntry(entry);
        
        var deleteButton = new Button
        {
            Content = "删除",
            Width = 60,
            Height = 28,
            Background = AppBrushes._FF7675,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12
        };
        deleteButton.Click += (s, e) => DeleteDiaryEntry(entry);
        
        buttonPanel.Children.Add(collapseButton);
        buttonPanel.Children.Add(modifyButton);
        buttonPanel.Children.Add(deleteButton);
        
        contentPanel.Children.Add(buttonPanel);
        mainStackPanel.Children.Add(contentPanel);
        
        var containerBorder = new Border
        {
            Tag = entry.Id,
            Child = mainStackPanel
        };
        
        DiaryTimelinePanel.Children.Add(containerBorder);
    }

    private void DiaryEntry_Expand_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string entryId)
        {
            ToggleDiaryEntry(entryId); // 使用切换逻辑，而不是只能展开
        }
    }

    private void ToggleDiaryEntry(string entryId, bool? expand = null)
    {
        foreach (var child in DiaryTimelinePanel.Children)
        {
            if (child is Border border && border.Tag?.ToString() == entryId)
            {
                if (border.Child is StackPanel mainPanel && mainPanel.Children.Count >= 2)
                {
                    // 现在 mainPanel.Children[0] 是标题，mainPanel.Children[1] 是内容面板
                    if (mainPanel.Children[1] is StackPanel contentPanel)
                    {
                        bool shouldExpand;
                        if (expand.HasValue)
                        {
                            shouldExpand = expand.Value;
                        }
                        else
                        {
                            // 切换状态：如果当前是收起状态，则展开；反之亦然
                            shouldExpand = contentPanel.Visibility == Visibility.Collapsed;
                        }
                        
                        contentPanel.Visibility = shouldExpand ? Visibility.Visible : Visibility.Collapsed;
                        
                        // 更新展开按钮图标
                        if (mainPanel.Children[0] is Border headerBorder && 
                            headerBorder.Child is Grid headerGrid &&
                            headerGrid.Children.Count > 1 &&
                            headerGrid.Children[1] is Button expandButton)
                        {
                            expandButton.Content = shouldExpand ? "▲" : "▼";
                        }
                    }
                }
                break;
            }
        }
    }

    private void EditDiaryEntry(DiaryEntry entry)
    {
        var editWindow = new DiaryEditWindow(_appData.PersonalInfo, entry);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            var index = _appData.Diaries.FindIndex(d => d.Id == entry.Id);
            if (index >= 0)
            {
                _appData.Diaries[index] = editWindow.ResultEntry;
                SaveAppData();
                RefreshDiaryTimeline();
                // 重新计算个人信息数值
                RecalculatePersonalInfo();
            }
        }
    }

    private void DeleteDiaryEntry(DiaryEntry entry)
    {
        var result = MessageBox.Show($"确定要删除日记「{entry.Title}」吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _appData.Diaries.RemoveAll(d => d.Id == entry.Id);
            SaveAppData();
            RefreshDiaryTimeline();
            // 重新计算个人信息数值
            RecalculatePersonalInfo();
        }
    }

    private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        RefreshDiaryMonthDates();
    }

    private void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        RefreshDiaryMonthDates();
    }

    #endregion

    #region 任务模块事件

    private void NewTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var taskEditWindow = new TaskEditWindow();
        if (taskEditWindow.ShowDialog() == true)
        {
            // 如果用户保存了任务，将任务添加到数据源
            if (taskEditWindow.TaskEntry != null)
            {
                _appData.Tasks.Add(taskEditWindow.TaskEntry);
                SaveAppData();
            }
            // 刷新任务列表
            RefreshTaskLists();
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        // 检查是否有选中的临时任务
        if (TempTaskListBox.SelectedItem is TaskEntry tempTask)
        {
            DeleteTask(tempTask);
        }
        // 妫€鏌ユ槸鍚︽湁閫変腑鐨勯」鐩换鍔?
        else if (ProjectTaskListBox.SelectedItem is TaskEntry projectTask)
        {
            DeleteTask(projectTask);
        }
        else
        {
            MessageBox.Show("请先选中要删除的任务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void DeleteTask(TaskEntry task)
    {
        var result = MessageBox.Show($"确定要删除任务「{task.Title}」吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _appData.Tasks.RemoveAll(t => t.Id == task.Id);
            SaveAppData();
            RefreshTaskLists();
        }
    }

    private void RefreshTaskLists()
    {
        // 娓呯┖鐜版湁鍒楄〃
        TempTaskListBox.Items.Clear();
        ProjectTaskListBox.Items.Clear();
        
        // 灏嗕换鍔″垎绫绘坊鍔犲埌涓嶅悓鍒楄〃
        foreach (var task in _appData.Tasks)
        {
            // 绠€鍗曞垽鏂細娌℃湁瀛愪换鍔＄殑涓轰复鏃朵换鍔★紝鏈夊瓙浠诲姟鐨勪负椤圭洰浠诲姟
            if (task.SubTasks.Count == 0)
            {
                TempTaskListBox.Items.Add(task);
            }
            else
            {
                ProjectTaskListBox.Items.Add(task);
            }
        }
    }

    private void TempTaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 鍙栨秷椤圭洰浠诲姟鍒楄〃鐨勯€夋嫨
        ProjectTaskListBox.SelectedItem = null;
    }

    private void ProjectTaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 鍙栨秷涓存椂浠诲姟鍒楄〃鐨勯€夋嫨
        TempTaskListBox.SelectedItem = null;
    }

    #endregion

    #region TabControl事件处理

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Log("MainTabControl_SelectionChanged事件触发，当前索引: " + MainTabControl.SelectedIndex);
            
            // 如果切换到时间记录模块（索引为2），初始化时间记录显示
            if (MainTabControl.SelectedIndex == 2)
            {
                Log("切换到时间记录模块，开始初始化时间记录显示");
                
                // 确保周视图正确更新
                UpdateWeekDisplay();
                
                // 重新初始化时间记录显示
                UpdateTimeRecordDisplay();
                
                Log("时间记录显示初始化完成");
            }
        }
        catch (Exception ex)
        {
            LogCrash("TabControl切换时发生错误", ex);
            MessageBox.Show($"TabControl切换时发生错误: {ex.Message}", "错误");
        }
    }

    #endregion

    #region 时间记录模块事件

    private void PreviousWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _currentWeekStart = _currentWeekStart.AddDays(-7);
        UpdateWeekDisplay();
        UpdateTimeRecordDisplay();
    }

    private void NextWeekButton_Click(object sender, RoutedEventArgs e)
    {
        _currentWeekStart = _currentWeekStart.AddDays(7);
        UpdateWeekDisplay();
        UpdateTimeRecordDisplay();
    }

    private void UpdateWeekDisplay()
    {
        if (CurrentWeekText != null)
        {
            var weekEnd = _currentWeekStart.AddDays(6);
            CurrentWeekText.Text = $"{_currentWeekStart.Year}年第{GetWeekNumber(_currentWeekStart)}周({_currentWeekStart:MM-dd} ~ {weekEnd:MM-dd})";
        }
    }

    private void AddTimeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        // 鍒涘缓鏂扮殑鏃堕棿璁板綍
        var newRecord = new TimeRecordEntry
        {
            Id = Guid.NewGuid().ToString(),
            Date = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Activity = "新活动",
            Category = "工作",
            Notes = "",
            CreatedAt = DateTime.Now
        };

        _appData.TimeRecords.Add(newRecord);
        _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
        
        SaveAppData();
        UpdateTimeRecordDisplay();
        
        MessageBox.Show("时间记录已添加！", "成功");
    }

    private void UpdateTimeRecordDisplay()
    {
        // 鑾峰彇褰撳墠鍛ㄧ殑鏃堕棿璁板綍
        var weekRecords = _appData.TimeRecords
            .Where(t => t.Date.Date >= _currentWeekStart.Date && t.Date.Date <= _currentWeekStart.AddDays(6).Date)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.StartTime)
            .ToList();

        // 娓呴櫎鐜版湁鐨勬椂闂村潡
        var timeGrid = FindName("TimeGrid") as Grid;
        if (timeGrid != null)
        {
            // 首先移除所有鼠标事件处理程序，避免重复订阅
            timeGrid.MouseLeftButtonDown -= TimeGrid_MouseLeftButtonDown;
            timeGrid.MouseMove -= TimeGrid_MouseMove;
            timeGrid.MouseLeftButtonUp -= TimeGrid_MouseLeftButtonUp;
            timeGrid.MouseLeave -= TimeGrid_MouseLeave;
            
            // 淇濆瓨鏃堕棿鏍囩鍜屾棩鏈熸爣棰?
            var timeLabels = new List<UIElement>();
            var dateHeaders = new List<UIElement>();
            
            for (int i = 0; i < timeGrid.Children.Count; i++)
            {
                var child = timeGrid.Children[i];
                var row = Grid.GetRow(child);
                var col = Grid.GetColumn(child);
                
                // 淇濆瓨鏃堕棿鏍囩锛堢0鍒楋級
                if (col == 0 && child is TextBlock)
                {
                    timeLabels.Add(child);
                }
                // 淇濆瓨鏃ユ湡鏍囬锛堢0琛岋級
                else if (row == 0 && child is TextBlock)
                {
                    dateHeaders.Add(child);
                }
            }
            
            // 娓呴櫎鎵€鏈夊瓙鍏冪礌
            timeGrid.Children.Clear();
            
            // 閲嶆柊娣诲姞鏃堕棿鏍囩鍜屾棩鏈熸爣棰?
            foreach (var label in timeLabels)
            {
                timeGrid.Children.Add(label);
            }
            foreach (var header in dateHeaders)
            {
                timeGrid.Children.Add(header);
            }

            // 閲嶆柊缁樺埗缃戞牸绾?
            DrawGridLines(timeGrid);
        
            // 缁樺埗鏃堕棿璁板綍
            DrawTimeRecords(timeGrid, weekRecords);
            
            // 重新添加鼠标事件处理程序
            timeGrid.MouseLeftButtonDown += TimeGrid_MouseLeftButtonDown;
            timeGrid.MouseMove += TimeGrid_MouseMove;
            timeGrid.MouseLeftButtonUp += TimeGrid_MouseLeftButtonUp;
            timeGrid.MouseLeave += TimeGrid_MouseLeave;
        }
    }
    
    private void DrawGridLines(Grid timeGrid)
    {
        if (timeGrid == null) return;
        
        // 缁樺埗鏃堕棿鍧楃綉鏍肩嚎
        for (int row = 0; row < 12; row++)
        {
            for (int col = 1; col <= 7; col++)
            {
                var border = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                Grid.SetRow(border, row + 1); // +1 鏄洜涓虹0琛屾槸鏃ユ湡鏍囬
                Grid.SetColumn(border, col);
                Grid.SetRowSpan(border, 1);
                Grid.SetColumnSpan(border, 1);
                timeGrid.Children.Add(border);
            }
        }
    }
    
    private void DrawTimeRecords(Grid timeGrid, List<TimeRecordEntry> records)
    {
        if (timeGrid == null) return;
        
        foreach (var record in records)
        {
            // 璁＄畻鏄熸湡鍑狅紙0=鍛ㄤ竴, 6=鍛ㄦ棩锛?
            int dayOfWeek = (int)record.Date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // 灏嗗懆鏃ヤ粠0杞崲涓?
            dayOfWeek -= 1; // 杞崲涓?-6鐨勭储寮?
            
            // 璁＄畻寮€濮嬫椂闂磋鍙凤紙08:00-19:00锛屽叡12灏忔椂锛?
            int startHour = record.StartTime.Hours;
            if (startHour < 8 || startHour >= 19) continue; // 鍙樉绀?8:00-19:00鐨勮褰?
            
            int startRow = startHour - 8;
            
            // 璁＄畻缁撴潫鏃堕棿琛屽彿
            int endHour = record.EndTime.Hours;
            if (endHour <= 8) continue;
            if (endHour > 19) endHour = 19;
            
            int endRow = endHour - 8;
            int rowSpan = endRow - startRow;
            
            if (rowSpan < 1) rowSpan = 1;
            
            // 鍒涘缓鏃堕棿鍧?
            var timeBlock = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(150, 108, 92, 231)), // 鍗婇€忔槑绱壊
                BorderBrush = Brushes.DarkSlateBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand
            };
            
            // 娣诲姞鐐瑰嚮浜嬩欢
            timeBlock.MouseLeftButtonDown += (s, e) => EditTimeRecord(record);
            
            // 鍒涘缓鍐呭闈㈡澘
            var contentPanel = new StackPanel
            {
                Margin = new Thickness(5),
                Background = Brushes.Transparent
            };
            
            // 娣诲姞娲诲姩鍚嶇О
            var activityText = new TextBlock
            {
                Text = record.Activity,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            
            // 娣诲姞鏃堕棿鑼冨洿
            var timeText = new TextBlock
            {
                Text = $"{record.StartTime:hh\\:mm} - {record.EndTime:hh\\:mm}",
                FontSize = 10,
                Foreground = Brushes.LightGray
            };
            
            contentPanel.Children.Add(activityText);
            contentPanel.Children.Add(timeText);
            
            timeBlock.Child = contentPanel;
            
            // 璁剧疆浣嶇疆鍜屽ぇ灏?
            Grid.SetRow(timeBlock, startRow + 1); // +1 鏄洜涓虹0琛屾槸鏃ユ湡鏍囬
            Grid.SetColumn(timeBlock, dayOfWeek + 1); // +1 鏄洜涓虹0鍒楁槸鏃堕棿鏍囩
            Grid.SetRowSpan(timeBlock, rowSpan);
            Grid.SetColumnSpan(timeBlock, 1);
            
            // 娣诲姞鍒扮綉鏍?
            timeGrid.Children.Add(timeBlock);
        }
    }
    
    private void EditTimeRecord(TimeRecordEntry record)
    {
        var editWindow = new TimeRecordEditWindow(record);
        var result = editWindow.ShowDialog();
        
        if (result == true)
        {
            // 淇濆瓨鏁版嵁
            SaveAppData();
            // 鏇存柊鏄剧ず
            UpdateTimeRecordDisplay();
        }
        else if (result == null)
        {
            // 鍒犻櫎璁板綍
            _appData.TimeRecords.Remove(record);
            SaveAppData();
            UpdateTimeRecordDisplay();
        }
    }
    
    #region 时间网格鼠标事件处理
    
    private void TimeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null) return;
        
        // 获取鼠标点击位置
        Point mousePoint = e.GetPosition(timeGrid);
        
        // 确定点击的行和列
        int row = GetRowFromPoint(mousePoint, timeGrid);
        int col = GetColumnFromPoint(mousePoint, timeGrid);
        
        // 只处理有效的行和列（行：1-12，列：1-7）
        if (row >= 1 && row <= 12 && col >= 1 && col <= 7)
        {
            _isDragging = true;
            _startRow = row;
            _startCol = col;
            _currentRow = row;
            _currentCol = col;
            
            // 创建拖动预览边框
            _dragPreviewBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 108, 92, 231)),
                BorderBrush = Brushes.DarkSlateBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Opacity = 0.7
            };
            
            // 设置初始位置
            Grid.SetRow(_dragPreviewBorder, row);
            Grid.SetColumn(_dragPreviewBorder, col);
            Grid.SetRowSpan(_dragPreviewBorder, 1);
            Grid.SetColumnSpan(_dragPreviewBorder, 1);
            
            // 添加到网格
            timeGrid.Children.Add(_dragPreviewBorder);
            
            // 捕获鼠标，确保能跟踪到鼠标离开窗口的情况
            timeGrid.CaptureMouse();
        }
    }
    
    private void TimeGrid_MouseMove(object sender, MouseEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null || !_isDragging) return;
        
        // 获取鼠标位置
        Point mousePoint = e.GetPosition(timeGrid);
        
        // 确定当前的行和列
        int row = GetRowFromPoint(mousePoint, timeGrid);
        int col = GetColumnFromPoint(mousePoint, timeGrid);
        
        // 只处理有效的行和列（行：1-12，列：1-7）
        if (row >= 1 && row <= 12 && col >= 1 && col <= 7)
        {
            _currentRow = row;
            _currentCol = col;
            
            // 更新拖动预览边框的位置和大小
            if (_dragPreviewBorder != null)
            {
                // 计算起始和结束位置（确保起始位置小于结束位置）
                int startRow = Math.Min(_startRow, _currentRow);
                int endRow = Math.Max(_startRow, _currentRow);
                int startCol = Math.Min(_startCol, _currentCol);
                int endCol = Math.Max(_startCol, _currentCol);
                
                // 设置位置和跨度
                Grid.SetRow(_dragPreviewBorder, startRow);
                Grid.SetColumn(_dragPreviewBorder, startCol);
                Grid.SetRowSpan(_dragPreviewBorder, endRow - startRow + 1);
                Grid.SetColumnSpan(_dragPreviewBorder, endCol - startCol + 1);
            }
        }
    }
    
    private void TimeGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null || !_isDragging)
        {
            return;
        }
        
        // 释放鼠标捕获
        timeGrid.ReleaseMouseCapture();
        
        // 移除拖动预览边框
        if (_dragPreviewBorder != null)
        {
            timeGrid.Children.Remove(_dragPreviewBorder);
            _dragPreviewBorder = null;
        }
        
        // 计算起始和结束位置（确保起始位置小于结束位置）
        int startRow = Math.Min(_startRow, _currentRow);
        int endRow = Math.Max(_startRow, _currentRow);
        int startCol = Math.Min(_startCol, _currentCol);
        int endCol = Math.Max(_startCol, _currentCol);
        
        // 只处理有效的选择（至少1行1列）
        if (startRow >= 1 && endRow <= 12 && startCol >= 1 && endCol <= 7)
        {
            // 创建新的时间记录
            CreateNewTimeRecord(startCol, endCol, startRow, endRow);
        }
        
        // 重置拖动状态
        _isDragging = false;
        _startRow = -1;
        _startCol = -1;
        _currentRow = -1;
        _currentCol = -1;
    }
    
    private void TimeGrid_MouseLeave(object sender, MouseEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null || !_isDragging)
        {
            return;
        }
        
        // 释放鼠标捕获
        timeGrid.ReleaseMouseCapture();
        
        // 移除拖动预览边框
        if (_dragPreviewBorder != null)
        {
            timeGrid.Children.Remove(_dragPreviewBorder);
            _dragPreviewBorder = null;
        }
        
        // 重置拖动状态
        _isDragging = false;
        _startRow = -1;
        _startCol = -1;
        _currentRow = -1;
        _currentCol = -1;
    }
    
    private int GetRowFromPoint(Point point, Grid grid)
    {
        double rowHeight = 40; // 行高固定为40
        int row = (int)Math.Floor(point.Y / rowHeight);
        
        // 确保行号在有效范围内（1-12，对应08:00-19:00）
        return Math.Max(1, Math.Min(12, row));
    }
    
    private int GetColumnFromPoint(Point point, Grid grid)
    {
        double timeLabelWidth = 60; // 时间标签列宽固定为60
        if (point.X < timeLabelWidth)
        {
            return 0; // 时间标签列
        }
        
        double totalColumnsWidth = grid.ActualWidth - timeLabelWidth;
        if (totalColumnsWidth <= 0)
        {
            return 1; // 默认返回第一列
        }
        
        double columnWidth = totalColumnsWidth / 7;
        int col = (int)Math.Floor((point.X - timeLabelWidth) / columnWidth) + 1;
        
        // 确保列数在有效范围内
        return Math.Max(1, Math.Min(7, col));
    }
    
    private void CreateNewTimeRecord(int startCol, int endCol, int startRow, int endRow)
    {
        try
        {
            // 参数验证
            if (startCol < 1 || endCol > 7 || startRow < 1 || endRow > 12)
            {
                MessageBox.Show("选择的时间段无效，请重新选择！", "错误");
                return;
            }
            
            // 确保起始位置小于结束位置
            int actualStartCol = Math.Min(startCol, endCol);
            int actualEndCol = Math.Max(startCol, endCol);
            int actualStartRow = Math.Min(startRow, endRow);
            int actualEndRow = Math.Max(startRow, endRow);
            
            // 计算开始时间和结束时间（每一行代表一个小时，从08:00开始）
            TimeSpan startTime = TimeSpan.FromHours(8 + (actualStartRow - 1));
            TimeSpan endTime = TimeSpan.FromHours(8 + actualEndRow);
            
            // 计算开始日期和结束日期
            DateTime startDate = _currentWeekStart.AddDays(actualStartCol - 1);
            DateTime endDate = _currentWeekStart.AddDays(actualEndCol - 1);
            
            // 如果选择了多个小时但只有一天，创建一个时间记录
            if (startDate == endDate)
            {
                // 创建新的时间记录
                var newRecord = new TimeRecordEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = startDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Activity = "新活动",
                    Category = "工作",
                    Notes = "",
                    CreatedAt = DateTime.Now
                };
                
                // 添加到数据
                _appData.TimeRecords.Add(newRecord);
            }
            // 如果选择了多个天，每天创建一个时间记录
            else
            {
                // 第一天的记录
                var firstDayRecord = new TimeRecordEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = startDate,
                    StartTime = startTime,
                    EndTime = TimeSpan.FromHours(19),
                    Activity = "新活动",
                    Category = "工作",
                    Notes = "",
                    CreatedAt = DateTime.Now
                };
                _appData.TimeRecords.Add(firstDayRecord);
                
                // 中间天的记录（全天）
                for (int day = actualStartCol + 1; day < actualEndCol; day++)
                {
                    var midDayRecord = new TimeRecordEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        Date = _currentWeekStart.AddDays(day - 1),
                        StartTime = TimeSpan.FromHours(8),
                        EndTime = TimeSpan.FromHours(19),
                        Activity = "新活动",
                        Category = "工作",
                        Notes = "",
                        CreatedAt = DateTime.Now
                    };
                    _appData.TimeRecords.Add(midDayRecord);
                }
                
                // 最后一天的记录
                var lastDayRecord = new TimeRecordEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = endDate,
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = endTime,
                    Activity = "新活动",
                    Category = "工作",
                    Notes = "",
                    CreatedAt = DateTime.Now
                };
                _appData.TimeRecords.Add(lastDayRecord);
            }
            
            // 保存数据并更新显示
            SaveAppData();
            UpdateTimeRecordDisplay();
            
            // 显示成功消息
            MessageBox.Show("时间段记录已添加！", "成功");
        }
        catch (Exception ex)
        {
            LogCrash("创建时间记录失败", ex);
            MessageBox.Show($"添加时间段记录失败：{ex.Message}", "错误");
        }
    }
    
    #endregion

    #endregion

    #region 打卡模块事件

    private void CheckInListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CheckInListBox.SelectedItem is CheckInEntry entry)
        {
            _currentCheckInEntry = entry;
            CheckInValueTextBox.Text = entry.Value;
            // 鍙互娣诲姞鏇村缂栬緫瀛楁
        }
    }

    private void CheckInTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCheckInList();
        UpdateCheckInStats();
    }

    private void CheckInButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "杩愬姩";
            var value = CheckInValueTextBox.Text.Trim();
            
            // 鍒涘缓鏂扮殑鎵撳崱璁板綍
            var newCheckIn = new CheckInEntry
            {
                Id = Guid.NewGuid().ToString(),
                Type = selectedType,
                Value = string.IsNullOrEmpty(value) ? "完成" : value,
                Date = DateTime.Today,
                CreatedAt = DateTime.Now
            };
            
            _appData.CheckIns.Add(newCheckIn);
            _appData.CheckIns = _appData.CheckIns.OrderByDescending(c => c.Date).ToList();
            
            SaveAppData();
            UpdateCheckInList();
            UpdateCheckInStats();
            
            MessageBox.Show("打卡成功！", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打卡失败：{ex.Message}", "错误");
        }
    }

    private void SaveCheckInButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentCheckInEntry != null)
            {
                // 鏇存柊褰撳墠閫変腑鐨勬墦鍗¤褰?
                _currentCheckInEntry.Value = CheckInValueTextBox.Text.Trim();
                _currentCheckInEntry.UpdatedAt = DateTime.Now;
                
                SaveAppData();
                UpdateCheckInList();
                UpdateCheckInStats();
                
                MessageBox.Show("打卡记录已更新！", "成功");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"更新打卡记录失败：{ex.Message}", "错误");
        }
    }

    private int CalculateStreak(string type, DateTime date)
    {
        var checkIns = _appData.CheckIns
            .Where(c => c.Type == type)
            .OrderByDescending(c => c.Date)
            .ToList();

        var streak = 0;
        var currentDate = date;
        
        // 检查今天是否有打卡记录
        var todayCheckIn = checkIns.FirstOrDefault(c => c.Date.Date == date.Date);
        if (todayCheckIn == null)
        {
            return 0; // 如果今天没打卡，连续天数为0
        }
        
        streak = 1; // 今天打卡了，连续天数至少为1
        currentDate = currentDate.AddDays(-1);
        
        foreach (var checkIn in checkIns.Skip(1)) // 跳过今天的记录
        {
            if (checkIn.Date.Date == currentDate.Date)
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }
            else if (checkIn.Date.Date < currentDate.Date)
            {
                break;
            }
        }
        
        return streak;
    }

    private int CalculateLongestStreak(List<CheckInEntry> checkIns)
    {
        if (!checkIns.Any()) return 0;
        
        var sortedCheckIns = checkIns.OrderBy(c => c.Date).ToList();
        var maxStreak = 0;
        var currentStreak = 0;
        var previousDate = DateTime.MinValue;
        
        foreach (var checkIn in sortedCheckIns)
        {
            if (previousDate == DateTime.MinValue || (checkIn.Date - previousDate).Days == 1)
            {
                currentStreak++;
            }
            else
            {
                maxStreak = Math.Max(maxStreak, currentStreak);
                currentStreak = 1;
            }
            previousDate = checkIn.Date;
        }
        
        return Math.Max(maxStreak, currentStreak);
    }

    private double CalculateSuccessRate(List<CheckInEntry> checkIns)
    {
        if (!checkIns.Any()) return 0;
        
        // 只计算最近30天的成功率，避免历史数据影响
        var thirtyDaysAgo = DateTime.Today.AddDays(-29); // 包括今天共30天
        var recentCheckIns = checkIns.Where(c => c.Date >= thirtyDaysAgo).ToList();
        
        if (!recentCheckIns.Any()) return 0;
        
        var totalDaysInPeriod = (DateTime.Today - thirtyDaysAgo).Days + 1;
        var successDays = recentCheckIns.Count;
        
        return (double)successDays / totalDaysInPeriod * 100;
    }

    private void UpdateCheckInList()
    {
        // 娣诲姞绌哄€兼鏌ワ紝闃叉鍒濆鍖栨湡闂村嚭鐜扮┖寮曠敤寮傚父
        if (CheckInTypeCombo == null || CheckInListBox == null) return;
        
        var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "杩愬姩";
        var filteredCheckIns = _appData.CheckIns
            .Where(c => c.Type == selectedType)
            .OrderByDescending(c => c.Date)
            .ToList();
        CheckInListBox.ItemsSource = filteredCheckIns;
    }

    private void UpdateCheckInStats()
    {
        // 添加空值检查，防止初始化期间出现空引用异常
        if (CheckInTypeCombo == null || CurrentStreakText == null || LongestStreakText == null || SuccessRateText == null) return;
        
        var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "运动";
        var typeCheckIns = _appData.CheckIns.Where(c => c.Type == selectedType).ToList();
        
        if (typeCheckIns.Any())
        {
            var currentStreak = CalculateStreak(selectedType, DateTime.Today);
            var longestStreak = CalculateLongestStreak(typeCheckIns);
            var successRate = CalculateSuccessRate(typeCheckIns);
            
            // 更新UI显示
            CurrentStreakText.Text = currentStreak.ToString();
            LongestStreakText.Text = longestStreak.ToString();
            SuccessRateText.Text = $"{successRate:F0}%";
        }
        else
        {
            // 如果没有该类型的打卡记录，显示0
            CurrentStreakText.Text = "0";
            LongestStreakText.Text = "0";
            SuccessRateText.Text = "0%";
        }
    }

    #endregion

    #region 统一保存和备份事件

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 鏍规嵁褰撳墠閫変腑鐨勬爣绛鹃〉淇濆瓨瀵瑰簲妯″潡鐨勬暟鎹?
            var selectedTab = MainTabControl.SelectedIndex;
            
            switch (selectedTab)
            {
                case 0: // 鏃ヨ
                    break;
                case 1: // 浠诲姟

                    break;
                case 2: // 鏃堕棿璁板綍
                    break;
                case 3: // 鎵撳崱
                    break;
            }

            SaveAppData();
            
            // 鍒涘缓鑷姩澶囦唤
            BackupManager.CreateAutoBackup(_appData, $"自动备份 - {DateTime.Now:yyyy-MM-dd HH:mm}");
            
            // 娓呯悊鏃у浠斤紝淇濈暀鏈€杩?0涓?
            BackupManager.CleanOldBackups(10);

            MessageBox.Show("数据已保存！已自动创建备份。", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}\n\n详细错误：{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveDataButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveAppData();
            MessageBox.Show("数据已成功保存！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}\n\n请检查程序目录的写入权限。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "App Backup Files (*.backup)|*.backup|All Files (*.*)|*.*",
            DefaultExt = "backup",
            FileName = $"app_backup_{DateTime.Now:yyyyMMdd_HHmmss}.backup"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                BackupManager.ExportBackupToLocation(_appData, dialog.FileName);
                MessageBox.Show($"备份已导出到：{dialog.FileName}", "导出成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误");
            }
        }
    }

    // ===== 个人数据管理功能 =====
    private void LoadPersonalInfo()
    {
        try
        {
            var personalInfo = _appData.PersonalInfo;
            PersonalNameTextBox.Text = personalInfo.Name;
            PersonalPhoneTextBox.Text = personalInfo.Phone;
            PersonalBirthdayPicker.SelectedDate = personalInfo.Birthday;
            PersonalSavingsTextBox.Text = personalInfo.Savings.ToString();
            PersonalLastUpdatedText.Text = $"最后更新：{personalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
        }
        catch (Exception ex)
        {
            Log($"加载个人数据失败：{ex.Message}");
        }
    }

    private void PersonalNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _appData.PersonalInfo.Name = PersonalNameTextBox.Text;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalPhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _appData.PersonalInfo.Phone = PersonalPhoneTextBox.Text;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalBirthdayPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        _appData.PersonalInfo.Birthday = PersonalBirthdayPicker.SelectedDate;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalSavingsTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (decimal.TryParse(PersonalSavingsTextBox.Text, out decimal savings))
            {
                _appData.PersonalInfo.Savings = savings;
                _appData.PersonalInfo.LastUpdated = DateTime.Now;
                PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
            }
        }
        catch (Exception ex)
        {
            Log($"存款输入错误：{ex.Message}");
        }
    }

    private void EditPersonalInfoButton_Click(object sender, RoutedEventArgs e)
    {
        // 启用所有输入控件
        PersonalNameTextBox.IsEnabled = true;
        PersonalPhoneTextBox.IsEnabled = true;
        PersonalBirthdayPicker.IsEnabled = true;
        PersonalSavingsTextBox.IsEnabled = true;
        
        // 禁用修改按钮，启用保存按钮
        EditPersonalInfoButton.IsEnabled = false;
        SavePersonalInfoButton.IsEnabled = true;
    }

    private void SavePersonalInfoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveAppData();
            MessageBox.Show("个人信息已保存！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 禁用所有输入控件
            PersonalNameTextBox.IsEnabled = false;
            PersonalPhoneTextBox.IsEnabled = false;
            PersonalBirthdayPicker.IsEnabled = false;
            PersonalSavingsTextBox.IsEnabled = false;
            
            // 启用修改按钮，禁用保存按钮
            EditPersonalInfoButton.IsEnabled = true;
            SavePersonalInfoButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "App Backup Files (*.backup)|*.backup|All Files (*.*)|*.*",
            DefaultExt = "backup"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var restoredData = BackupManager.RestoreBackup(dialog.FileName);
                if (restoredData != null)
                {
                    var result = MessageBox.Show(
                        $"发现应用数据备份。\n\n是否要导入这些数据？\n\n注意：这将覆盖当前所有数据！", 
                        "确认导入", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _appData = restoredData;
                            
                            // 閲嶆柊鎺掑簭鏁版嵁
                            _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                            _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                            _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                            _appData.CheckIns = _appData.CheckIns.OrderByDescending(c => c.Date).ToList();
                            
                            SaveAppData();
                            
                            // 鍒锋柊鐣岄潰
                            InitializeUI();
                            
                            MessageBox.Show("数据导入成功！", "成功");
                        }
                        catch (Exception saveEx)
                        {
                            MessageBox.Show($"数据导入成功，但保存到本地文件失败：{saveEx.Message}\n\n数据将在程序重启时丢失。\n请检查程序目录的写入权限。", "部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("备份文件损坏或格式不正确，无法导入。", "导入失败");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}", "错误");
            }
        }
    }

    private void ManageBackupsButton_Click(object sender, RoutedEventArgs e)
    {
        var backups = BackupManager.GetAllBackups();
        if (backups.Count == 0)
        {
            MessageBox.Show("暂无备份文件。", "备份管理");
            return;
        }

        var backupList = string.Join("\n", backups.Select((b, index) => 
            $"{index + 1}. {b.info.CreatedAt:yyyy-MM-dd HH:mm} - {b.info.Description}"));

        var result = MessageBox.Show(
            $"找到 {backups.Count} 个备份文件：\n\n{backupList}\n\n是否要打开备份文件管理器？", 
            "备份管理", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            System.Diagnostics.Process.Start("explorer.exe", Path.Combine(Directory.GetCurrentDirectory(), "Backups"));
        }
    }

    #endregion

    #region 辅助方法

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private static int GetWeekNumber(DateTime date)
    {
        var jan1 = new DateTime(date.Year, 1, 1);
        var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekNum = cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekNum;
    }

    #endregion
}
