/* MainWindow.xaml.cs
 * 主窗口聚合了日记、任务、时间记录、打卡、个人信息和数据管理等主要模块。
 * 这里同时包含部分 XAML 绑定转换器、数据加载保存逻辑，以及各模块的事件处理。
 */
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
            // 根据层级调整缩进，每级增加 20 像素
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
            // 根据层级调整字重
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
            // 根据层级调整字号
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

// 甯冨皵鍊煎埌涓嬪垝绾胯浆鎹㈠櫒
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

// 鐗堟湰淇℃伅 - 鑷姩鏇存柊涓哄綋鍓嶆椂闂?
public static class AppVersion
{
    public const string VERSION = "0.1.1.244";
    public static readonly string BUILD_DATE = DateTime.Now.ToString("yyyy-MM-dd");
    public static readonly string BUILD_TIME = DateTime.Now.ToString("HH:mm");
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // 缁熶竴搴旂敤鏁版嵁
    private AppData _appData = new AppData();
    private const string DATA_FILE = "app_data.json";
    private const string LOG_FILE = "app_crash_log.txt";

    // 浠诲姟璁℃暟灞炴€?
    private string _tempTaskCount = "0/0";
    public string TempTaskCount
    {
        get { return _tempTaskCount; }
        set
        {
            if (_tempTaskCount != value)
            {
                _tempTaskCount = value;
                OnPropertyChanged(nameof(TempTaskCount));
            }
        }
    }

    private string _projectTaskCount = "0/0";
    public string ProjectTaskCount
    {
        get { return _projectTaskCount; }
        set
        {
            if (_projectTaskCount != value)
            {
                _projectTaskCount = value;
                OnPropertyChanged(nameof(ProjectTaskCount));
            }
        }
    }

    // INotifyPropertyChanged鎺ュ彛瀹炵幇
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // 鎻愰啋鍔熻兘鐩稿叧鍙橀噺
    private DispatcherTimer _reminderTimer;
    private DateTime _lastReminderDate = DateTime.MinValue;
    private bool _isPersonalInfoEditing;
    private bool _isLoadingPersonalInfo;
    private PersonalInfo? _personalInfoEditBackup;
    
    // 鑾峰彇搴旂敤鏁版嵁鏂囦欢鐨勫畬鏁磋矾寰?
    private string GetDataFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, DATA_FILE);
    }
    
    // 鑾峰彇鏃ュ織鏂囦欢鐨勫畬鏁磋矾寰?
    private string GetLogFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, LOG_FILE);
    }
    
    // 璁板綍鏃ュ織
    private void Log(string message)
    {
        try
        {
            var logPath = GetLogFilePath();
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
            File.AppendAllText(logPath, logMessage);
        }
        catch { /* 蹇界暐鏃ュ織閿欒 */ }
    }
    
    // 璁板綍宕╂簝鏃ュ織
    private void LogCrash(string message, Exception? ex = null)
    {
        try
        {
            var logPath = GetLogFilePath();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"崩溃时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"绋嬪簭鐗堟湰: {AppVersion.VERSION} ({AppVersion.BUILD_DATE} {AppVersion.BUILD_TIME})");
            sb.AppendLine($"鎿嶄綔绯荤粺: {Environment.OSVersion}");
            sb.AppendLine($".NET鐗堟湰: {Environment.Version}");
            sb.AppendLine($"机器名称: {Environment.MachineName}");
            sb.AppendLine("--------------------------------------------------");
            sb.AppendLine($"错误信息: {message}");
            if (ex != null)
            {
                sb.AppendLine("--------------------------------------------------");
                sb.AppendLine("异常详情:");
                sb.AppendLine($"绫诲瀷: {ex.GetType().FullName}");
                sb.AppendLine($"信息: {ex.Message}");
                sb.AppendLine($"婧? {ex.Source}");
                sb.AppendLine("鍫嗘爤璺熻釜:");
                sb.AppendLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    sb.AppendLine("--------------------------------------------------");
                    sb.AppendLine("内部异常:");
                    sb.AppendLine($"绫诲瀷: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"信息: {ex.InnerException.Message}");
                    sb.AppendLine("鍫嗘爤璺熻釜:");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
            }
            sb.AppendLine("==================================================");
            sb.AppendLine();
            File.AppendAllText(logPath, sb.ToString());
        }
        catch { /* 蹇界暐鏃ュ織閿欒 */ }
    }
    
    // 鍏ㄥ眬鏈鐞嗗紓甯稿鐞?闈濽I绾跨▼)
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var msg = ex != null ? ex.Message : "未知错误";
        LogCrash("UnhandledException (AppDomain)", ex);
        MessageBox.Show($"程序发生未处理的异常错误。\n错误信息: {msg}\n\n错误详情已保存到 app_crash_log.txt",
            "程序崩溃", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    // UI绾跨▼鏈鐞嗗紓甯稿鐞?
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("DispatcherUnhandledException (UI Thread)", e.Exception);
        e.Handled = true; // 鏍囪涓哄凡澶勭悊锛岄槻姝㈢▼搴忛€€鍑?
        MessageBox.Show($"程序发生未处理的异常错误。\n错误信息: {e.Exception.Message}\n\n错误详情已保存到 app_crash_log.txt",
            "程序崩溃", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    // 褰撳墠閫変腑鐨勬棩璁版潯鐩?鐢ㄤ簬缂栬緫)
    private DiaryEntry? _currentDiaryEntry;
    
    // 褰撳墠閫変腑鐨勪换鍔℃潯鐩?鐢ㄤ簬缂栬緫)
    private TaskEntry? _currentTaskEntry;
    
    // 褰撳墠閫変腑鐨勬墦鍗℃潯鐩?鐢ㄤ簬缂栬緫)
    private CheckInEntry? _currentCheckInEntry;

    // 褰撳墠鏌ョ湅鐨勫懆
    private DateTime _currentWeekStart = GetWeekStart(DateTime.Today);
    
    // 褰撳墠鏌ョ湅鐨勬湀浠?
    private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    
    // 榧犳爣鎷栧姩鐩稿叧鍙橀噺
    private bool _isDragging = false;
    private int _startRow = -1;
    private int _startCol = -1;
    private int _currentRow = -1;
    private int _currentCol = -1;
    private Border? _dragPreviewBorder = null;
    
    // 鏃堕棿璁板綍缃戞牸寮曠敤
    private Grid _timeGrid;

    public MainWindow()
    {
        // 璁剧疆鍏ㄥ眬寮傚父澶勭悊
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        // 娓呯┖鏃х殑宕╂簝鏃ュ織
        try { File.Delete(GetLogFilePath()); } catch { }
        
        try
        {
            Log("MainWindow构造函数开始");
            
            Log("开始调用 InitializeComponent()");
            InitializeComponent();
            Log("InitializeComponent()瀹屾垚");
            
            // 鑾峰彇鏃堕棿璁板綍缃戞牸寮曠敤
            _timeGrid = FindName("TimeGrid") as Grid ?? throw new Exception("未找到 TimeGrid 元素");
            
            Log("开始设置窗口拖动");
            // 支持窗口拖动，因为窗口使用了 WindowStyle="None"
            this.MouseLeftButtonDown += (s, e) =>
            {
                // 检查鼠标左键是否处于按下状态
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
            
            // 鍙屽嚮鏍囬鏍忓尯鍩熷垏鎹㈡渶澶у寲/姝ｅ父鐘舵€?
            this.MouseDoubleClick += (s, e) =>
            {
                // 鍙湪闈炴渶澶у寲鐘舵€佷笅鍙屽嚮鎵嶅垏鎹?
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
            
            // 璁剧疆绐楀彛杈圭紭璋冩暣澶у皬
            SetupWindowResize();
            Log("窗口缩放设置完成");
            
            Log("开始调用 LoadAppData()");
            LoadAppData();
            Log("LoadAppData()瀹屾垚");
            
            Log("开始调用 InitializeUI()");
            InitializeUI();
            Log("InitializeUI()瀹屾垚");
            
            Log("开始设置默认选项卡");
            // 榛樿鏄剧ず鏃ヨ妯″潡
            MainTabControl.SelectedIndex = 0;
            Log("选项卡设置完成");
            
            // 娣诲姞TabControl鐨凷electionChanged浜嬩欢澶勭悊绋嬪簭
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
            Log("TabControl SelectionChanged浜嬩欢澶勭悊绋嬪簭娣诲姞瀹屾垚");
            
            // 鍒濆鍖栨彁閱掑姛鑳?
            InitializeReminder();
            Log("提醒功能初始化完成");
            
            Log("MainWindow构造函数成功完成，窗口应已显示");
        }
        catch (Exception ex)
        {
            Log($"MainWindow鏋勯€犲嚱鏁板け璐? {ex.Message}");
            Log($"异常堆栈: {ex.StackTrace}");
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                Log($"内部异常: {innerEx.Message}");
                Log($"鍐呴儴异常堆栈: {innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
            }
            MessageBox.Show($"初始化错误: {ex.Message}\n\n详细信息: {ex.StackTrace}\n\n请查看 startup_log.txt 获取更多调试信息", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    private void InitializeUI()
    {
        // 将初始窗口大小设置为屏幕的 70%
        SetWindowSizeTo70Percent();
        
        // 将日记周期筛选默认设为“全部”
        DiaryPeriodFilterBox.SelectedIndex = 0;
        
        // 初始化日历视图
        RefreshDiaryMonthDates();
        
        // 鍒濆鍖栨棩璁版椂闂寸嚎
        RefreshDiaryTimeline();
        
        // 璁剧疆榛樿鏃ユ湡鏃堕棿鏄剧ず
        DateLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        
        // 鍒濆鍖栧懆瑙嗗浘
        UpdateWeekDisplay();
        
        // 鍒濆鍖栨椂闂磋褰曟樉绀猴紙纭繚鏃堕棿璁板綍妯″潡鍦ㄥ惎鍔ㄦ椂灏辨纭垵濮嬪寲锛?
        UpdateTimeRecordDisplay();
        
        // 鍒濆鍖栦换鍔″垪琛紙纭繚浠诲姟鏁版嵁鍦ㄥ惎鍔ㄦ椂灏辨纭姞杞斤級
        RefreshTaskLists();
        
        // 鍒濆鍖栨墦鍗￠」鐩垪琛?
        RefreshCheckInProjectList();
        
        // 鍒濆鍖栦釜浜烘暟鎹?
        LoadPersonalInfo();
        
        // 鏇存柊鍊掓暟鏃ユ樉绀?
        UpdateCountdownDisplay();
        
        // 鏇存柊鎻愰啋瀹氭椂鍣ㄧ姸鎬?
        UpdateReminderTimer();
    }
    
    private void SetWindowSizeTo70Percent()
    {
        // 鑾峰彇涓绘樉绀哄櫒鐨勫伐浣滃尯鍩?
        var workArea = SystemParameters.WorkArea;
        
        // 璁＄畻70%鐨勫搴﹀拰楂樺害
        double targetWidth = workArea.Width * 0.7;
        double targetHeight = workArea.Height * 0.7;
        
        // 璁剧疆绐楀彛澶у皬
        this.Width = targetWidth;
        this.Height = targetHeight;
        
        // 灏嗙獥鍙ｅ眳涓樉绀哄湪宸ヤ綔鍖哄煙
        this.Left = workArea.Left + (workArea.Width - targetWidth) / 2;
        this.Top = workArea.Top + (workArea.Height - targetHeight) / 2;
    }
    
    private void SetupWindowResize()
    {
        // 椤堕儴璋冩暣
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
        
        // 宸︿晶璋冩暣
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
        
        // 鍙充晶璋冩暣
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
        
        // 搴曢儴璋冩暣
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
        
        // 鍙充笅瑙掕皟鏁存墜鏌?
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
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
                var data = JsonSerializer.Deserialize<AppData>(json, options);
                if (data != null)
                {
                    _appData = data;
                    
                    // 杩囨护鎺夋棫鐨勬墦鍗¤褰曪紙鍙繚鐣欐渶杩?0澶╃殑锛?
                    var thirtyDaysAgo = DateTime.Today.AddDays(-29);
                    _appData.CheckIns = _appData.CheckIns
                        .Where(c => c.Date >= thirtyDaysAgo)
                        .OrderByDescending(c => c.Date)
                        .ToList();
                    
                    // 纭繚鎵€鏈夋暟鎹泦鍚堥兘琚纭垵濮嬪寲
                    _appData.Diaries = _appData.Diaries ?? new List<DiaryEntry>();
                    _appData.Tasks = _appData.Tasks ?? new List<TaskEntry>();
                    _appData.TimeRecords = _appData.TimeRecords ?? new List<TimeRecordEntry>();
                    _appData.CheckIns = _appData.CheckIns ?? new List<CheckInEntry>();
                    _appData.CheckInProjects = _appData.CheckInProjects ?? new List<CheckInProject>();
                    _appData.PersonalInfo = _appData.PersonalInfo ?? new PersonalInfo();

                    // 鍒濆鍖栦换鍔＄殑Chapters灞炴€у拰绔犺妭鐨凷ubTasks灞炴€?
                    foreach (var task in _appData.Tasks)
                    {
                        task.Chapters = task.Chapters ?? new List<TaskChapter>();
                        task.ProjectTags = task.ProjectTags ?? new List<string>();

                        foreach (var chapter in task.Chapters)
                        {
                            chapter.SubTasks = chapter.SubTasks ?? new List<SubTask>();
                        }
                    }

                    // 閲嶆柊鎺掑簭鍏朵粬鏁版嵁
                    _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                    _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                    _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                }
            }
            catch { /* 蹇界暐鍔犺浇閿欒 */ }
        }
        
        // 閲嶆柊璁＄畻涓汉淇℃伅鏁板€?
        // RecalculatePersonalInfo(); // 绂佺敤鑷姩閲嶆柊璁＄畻锛岄槻姝㈣鐩栨墜鍔ㄨ緭鍏ョ殑鍊?
        
        // 鏇存柊UI鏄剧ず
        LoadPersonalInfo();
        UpdateCountdownDisplay();
    }

    // 閲嶆柊璁＄畻涓汉淇℃伅鏁板€?
    private void RecalculatePersonalInfo()
    {
        try
        {
            // 浠庢墍鏈夋棩璁颁腑鏀堕泦鍙傛暟骞堕噸鏂拌绠?
            decimal totalSavings = 0;
            
            // 閬嶅巻鎵€鏈夋棩璁?
            foreach (var diary in _appData.Diaries)
            {
                // 閬嶅巻鏃ヨ涓殑鎵€鏈夊弬鏁?
                foreach (var param in diary.Parameters)
                {
                    string trimmedName = param.Name.Trim();
                    if (trimmedName.Equals("金钱", StringComparison.OrdinalIgnoreCase) ||
                        trimmedName.Equals("金额", StringComparison.OrdinalIgnoreCase) || 
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
            
            // 鏇存柊涓汉淇℃伅
            _appData.PersonalInfo.Savings = totalSavings;
            _appData.PersonalInfo.LastUpdated = DateTime.Now;
            
            // 鏇存柊UI鏄剧ず
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
            // 保存失败鏃讹紝璁板綍閿欒浣嗕笉涓柇鐢ㄦ埛鎿嶄綔
            System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
            throw; // 閲嶆柊鎶涘嚭寮傚父锛岃璋冪敤鑰呭喅瀹氬浣曞鐞?
        }
    }

    #region 绐楀彛鎺у埗浜嬩欢

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 濡傛灉鍚敤浜嗗悗鍙拌繍琛岋紝鍒欓殣钘忕獥鍙ｈ€屼笉鏄叧闂?
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
        
        // 淇濆瓨璁剧疆骞舵洿鏂版彁閱掑畾鏃跺櫒
        SaveAppData();
        UpdateReminderTimer();
    }
    
    // 鍚庡彴杩愯鎸夐挳鐐瑰嚮浜嬩欢
    private void BackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        HideWindow();
    }
    
    // 鏄剧ず涓荤獥鍙?
    private void ShowWindow()
    {
        this.Visibility = Visibility.Visible;
        this.WindowState = WindowState.Normal;
        this.Activate();
    }
    
    // 闅愯棌涓荤獥鍙ｏ紙鍚庡彴杩愯锛?
    private void HideWindow()
    {
        this.Visibility = Visibility.Hidden;
    }
    
    // 閫€鍑哄簲鐢ㄧ▼搴?
    private void ExitApplication()
    {
        // 鍋滄瀹氭椂鍣?
        if (_reminderTimer != null)
        {
            _reminderTimer.Stop();
        }
        
        Application.Current.Shutdown();
    }
    
    // 鍒濆鍖栨彁閱掑姛鑳?
    private void InitializeReminder()
    {
        // 鍒涘缓鎻愰啋瀹氭椂鍣紝姣忓垎閽熸鏌ヤ竴娆?
        _reminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _reminderTimer.Tick += ReminderTimer_Tick;
        
        // 鏍规嵁褰撳墠閰嶇疆鍚姩鎴栧仠姝㈠畾鏃跺櫒
        UpdateReminderTimer();
    }
    
    // 鏍规嵁閰嶇疆鏇存柊鎻愰啋瀹氭椂鍣?
    private void UpdateReminderTimer()
    {
        if (_reminderTimer == null) return;

        var hasTaskReminder = _appData.Tasks.Any(t =>
            t.ReminderSettings != null &&
            t.ReminderSettings.IsEnabled &&
            t.ReminderSettings.IsActive);

        if (_appData.ReminderSetting.IsEnabled || hasTaskReminder)
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
    
    // 鎻愰啋瀹氭椂鍣ㄧ殑Tick浜嬩欢澶勭悊绋嬪簭
    private void ReminderTimer_Tick(object sender, EventArgs e)
    {
        // 妫€鏌ユ槸鍚﹀簲璇ユ樉绀烘彁閱?
        if (ShouldShowReminder())
        {
            ShowReminder();
            
            // 鏇存柊鏈€鍚庢彁閱掓棩鏈?
            _lastReminderDate = DateTime.Now.Date;
        }
    }
    
    // 妫€鏌ユ槸鍚﹀簲璇ユ樉绀烘彁閱?
    private bool ShouldShowReminder()
    {
        CheckTaskReminders();

        if (!_appData.ReminderSetting.IsEnabled)
        {
            return false;
        }
        
        // 妫€鏌ユ槸鍚︽槸褰撳ぉ绗竴娆℃彁閱?
        if (_lastReminderDate == DateTime.Now.Date)
        {
            return false;
        }
        
        // 妫€鏌ュ綋鍓嶆椂闂存槸鍚﹁揪鍒版彁閱掓椂闂?
        var now = DateTime.Now;
        int hour = _appData.ReminderSetting.ReminderTime.HasValue ? _appData.ReminderSetting.ReminderTime.Value.Hours : 20;
        int minute = _appData.ReminderSetting.ReminderTime.HasValue ? _appData.ReminderSetting.ReminderTime.Value.Minutes : 0;
        var reminderTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        
        return now >= reminderTime;
    }
    
    // 鏄剧ず鎻愰啋娑堟伅
    private void ShowReminder()
    {
        MessageBox.Show(_appData.ReminderSetting.ReminderMessage, "我的日记助手", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CheckTaskReminders()
    {
        try
        {
            var now = DateTime.Now;
            var dueTasks = new List<(TaskEntry Task, DateTime DueAt, string Message)>();
            var hasChanges = false;

            foreach (var task in _appData.Tasks)
            {
                var reminder = task.ReminderSettings;
                if (reminder == null || !reminder.IsEnabled || !reminder.IsActive || task.Status == TaskStatus.Completed)
                {
                    continue;
                }

                reminder.NextReminderDate ??= ReminderScheduler.CalculateNextReminderDate(reminder, now.AddSeconds(-1));
                if (!reminder.NextReminderDate.HasValue || reminder.NextReminderDate.Value > now)
                {
                    continue;
                }

                if (reminder.LastTriggeredAt.HasValue && reminder.LastTriggeredAt.Value >= reminder.NextReminderDate.Value)
                {
                    continue;
                }

                var message = string.IsNullOrWhiteSpace(reminder.ReminderMessage)
                    ? $"任务提醒：{task.Title}"
                    : reminder.ReminderMessage;

                dueTasks.Add((task, reminder.NextReminderDate.Value, message));
                reminder.LastTriggeredAt = now;

                if (reminder.ReminderType == ReminderType.Once)
                {
                    reminder.IsActive = false;
                    reminder.IsEnabled = false;
                    reminder.NextReminderDate = null;
                }
                else
                {
                    reminder.NextReminderDate = ReminderScheduler.CalculateNextReminderDate(reminder, now);
                }

                hasChanges = true;
            }

            if (dueTasks.Count == 0)
            {
                if (hasChanges)
                {
                    SaveAppData();
                }
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var dueTask in dueTasks)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }

                sb.AppendLine($"任务：{dueTask.Task.Title}");
                sb.AppendLine($"提醒时间：{dueTask.DueAt:yyyy-MM-dd HH:mm}");
                sb.Append(dueTask.Message);
            }

            MessageBox.Show(sb.ToString(), "任务提醒", MessageBoxButton.OK, MessageBoxImage.Information);

            if (hasChanges)
            {
                SaveAppData();
            }
        }
        catch (Exception ex)
        {
            Log($"任务提醒检查失败: {ex.Message}");
        }
    }
    // 鏍规嵁鎻愰啋璁剧疆鑷姩鐢熸垚浠诲姟
    private void AutoGenerateTasksFromReminders()
    {
        try
        {
            var today = DateTime.Now.Date;
            var tasksToGenerate = new List<TaskEntry>();

            // 妫€鏌ユ墍鏈夌幇鏈変换鍔★紝鎵惧嚭闇€瑕佺敓鎴愭柊浠诲姟鐨勯偅浜?
            foreach (var existingTask in _appData.Tasks)
            {
                if (existingTask.ReminderSettings != null && 
                    existingTask.ReminderSettings.IsEnabled && 
                    existingTask.ReminderSettings.IsActive)
                {
                    var reminderSettings = existingTask.ReminderSettings;
                    
                    // 妫€鏌ヤ粖澶╂槸鍚﹂渶瑕佺敓鎴愪换鍔?
                    if (ShouldGenerateTaskToday(reminderSettings, today))
                    {
                        // 鍒涘缓鏂颁换鍔?
                        var newTask = CreateTaskFromReminder(existingTask, today);
                        if (newTask != null)
                        {
                            tasksToGenerate.Add(newTask);
                        }
                    }
                }
            }

            // 娣诲姞鐢熸垚鐨勪换鍔?
            if (tasksToGenerate.Count > 0)
            {
                foreach (var task in tasksToGenerate)
                {
                    _appData.Tasks.Add(task);
                }
                
                SaveAppData();
                RefreshTaskLists();
                
                // 鏄剧ず鐢熸垚浠诲姟鐨勯€氱煡
                if (tasksToGenerate.Count == 1)
                {
                    MessageBox.Show($"已自动生成任务：{tasksToGenerate[0].Title}", "任务生成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"已自动生成 {tasksToGenerate.Count} 个任务", "任务生成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"自动生成任务时出错: {ex.Message}");
        }
    }

    // 妫€鏌ヤ粖澶╂槸鍚﹀簲璇ョ敓鎴愪换鍔?
    private bool ShouldGenerateTaskToday(ReminderSetting reminderSettings, DateTime today)
    {
        if (!reminderSettings.StartDate.HasValue)
            return false;

        var startDate = reminderSettings.StartDate.Value.Date;
        
        // 濡傛灉浠婂ぉ鏃╀簬寮€濮嬫棩鏈燂紝涓嶇敓鎴愪换鍔?
        if (today < startDate)
            return false;

        switch (reminderSettings.ReminderType)
        {
            case ReminderType.Daily:
                // 姣忔棩鎻愰啋 - 姣忓ぉ閮界敓鎴?
                return true;

            case ReminderType.Weekly:
                // 姣忓懆鎻愰啋 - 妫€鏌ヤ粖澶╂槸鍚﹀湪鎸囧畾鐨勬槦鏈熷嚑涓?
                if (reminderSettings.WeekDays != null && reminderSettings.WeekDays.Count > 0)
                {
                    return reminderSettings.WeekDays.Contains(today.DayOfWeek);
                }
                return false;

            case ReminderType.Monthly:
                // 姣忔湀鎻愰啋 - 妫€鏌ヤ粖澶╂槸鍚︽槸姣忔湀鐨勫悓涓€澶╂垨鎸囧畾鐨勬槦鏈熷嚑
                if (reminderSettings.MonthlyDayNumber.HasValue && reminderSettings.MonthlyDayOfWeek.HasValue)
                {
                    // 妫€鏌ユ槸鍚︽槸姣忔湀鐨勭鍑犱釜鏄熸湡鍑?
                    var monthlyDate = GetMonthlyWeekDayDate(today.Year, today.Month, 
                        reminderSettings.MonthlyDayNumber.Value, reminderSettings.MonthlyDayOfWeek.Value);
                    return monthlyDate.HasValue && monthlyDate.Value.Date == today.Date;
                }
                else
                {
                    // 榛樿妫€鏌ユ槸鍚︽槸姣忔湀鐨勫悓涓€澶?
                    return today.Day == startDate.Day;
                }

            case ReminderType.Yearly:
                // 姣忓勾鎻愰啋 - 妫€鏌ヤ粖澶╂槸鍚︽槸姣忓勾鐨勫悓涓€澶?
                return today.Month == startDate.Month && today.Day == startDate.Day;

            case ReminderType.Interval:
                // 闂撮殧鎻愰啋 - 妫€鏌ヤ粖澶╂槸鍚﹀湪闂撮殧鍛ㄦ湡鍐?
                if (reminderSettings.IntervalDays.HasValue && reminderSettings.IntervalDays.Value > 0)
                {
                    var daysSinceStart = (today - startDate).Days;
                    return daysSinceStart >= 0 && daysSinceStart % reminderSettings.IntervalDays.Value == 0;
                }
                return false;

            default:
                return false;
        }
    }

    // 鏍规嵁鎻愰啋璁剧疆鍒涘缓鏂颁换鍔?
    private TaskEntry? CreateTaskFromReminder(TaskEntry originalTask, DateTime taskDate)
    {
        try
        {
            var newTask = new TaskEntry
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"{originalTask.Title} - {taskDate:MM月dd日}",
                Content = originalTask.Content,
                TaskType = originalTask.TaskType,
                ProjectTags = new List<string>(originalTask.ProjectTags ?? new List<string>()),
                Status = TaskStatus.Pending,
                Priority = originalTask.Priority,
                Chapters = new List<TaskChapter>(),
                CreatedAt = DateTime.Now,
                StartDate = taskDate,
                EndDate = taskDate.AddDays(1),
                ReminderSettings = null // 鏂扮敓鎴愮殑浠诲姟涓嶅鍒舵彁閱掕缃紝閬垮厤寰幆鐢熸垚
            };

            // 澶嶅埗绔犺妭缁撴瀯锛堜絾涓嶅鍒跺叿浣撳唴瀹癸級
            if (originalTask.Chapters != null && originalTask.Chapters.Count > 0)
            {
                foreach (var originalChapter in originalTask.Chapters)
                {
                    var newChapter = new TaskChapter
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = originalChapter.Title,
                        Content = "", // 娓呯┖鍐呭锛岀瓑寰呯敤鎴峰～鍐?
                        SubTasks = new List<SubTask>(),
                        Notes = "",
                        CreatedAt = DateTime.Now
                    };

                    // 澶嶅埗瀛愪换鍔＄粨鏋?
                    if (originalChapter.SubTasks != null && originalChapter.SubTasks.Count > 0)
                    {
                        foreach (var originalSubTask in originalChapter.SubTasks)
                        {
                            var newSubTask = new SubTask
                            {
                                Id = Guid.NewGuid().ToString(),
                                Title = originalSubTask.Title,
                                Content = "", // 娓呯┖鍐呭
                                IsCompleted = false,
                                StartDate = taskDate,
                                EndDate = taskDate.AddDays(1)
                            };
                            newChapter.SubTasks.Add(newSubTask);
                        }
                    }

                    newTask.Chapters.Add(newChapter);
                }
            }

            return newTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建任务时出错: {ex.Message}");
            return null;
        }
    }

    // 鑾峰彇姣忔湀绗嚑涓槦鏈熷嚑鐨勬棩鏈燂紙澶嶅埗鑷猅askEditWindow锛?
    private DateTime? GetMonthlyWeekDayDate(int year, int month, int weekNumber, DayOfWeek dayOfWeek)
    {
        try
        {
            var firstDayOfMonth = new DateTime(year, month, 1);
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            var targetDayOfWeek = (int)dayOfWeek;

            // 璁＄畻绗竴涓洰鏍囨槦鏈熷嚑鐨勬棩鏈?
            var daysUntilTarget = (targetDayOfWeek - firstDayOfWeek + 7) % 7;
            var firstTargetDate = firstDayOfMonth.AddDays(daysUntilTarget);

            // 璁＄畻绗嚑涓槦鏈熷嚑鐨勬棩鏈?
            var targetDate = firstTargetDate.AddDays((weekNumber - 1) * 7);

            // 纭繚鏃ユ湡鍦ㄥ綋鏈堣寖鍥村唴
            if (targetDate.Month == month)
            {
                return targetDate;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 鏃ヨ妯″潡浜嬩欢

    private void NewDiaryButton_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new DiaryEditWindow(_appData.PersonalInfo, _appData);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            _appData.Diaries.Add(editWindow.ResultEntry);
            SaveAppData();
            RefreshDiaryTimeline();
            // 鏇存柊涓汉淇℃伅UI鏄剧ず
            LoadPersonalInfo();
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
        ShowDiaryQuickTagPopup();
    }

    private void DiaryTagFilterBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DiaryTagPopup != null && !DiaryTagPopup.IsOpen)
        {
            ShowDiaryQuickTagPopup();
        }
    }

    private void ShowDiaryQuickTagPopup()
    {
        if (DiaryTagPopup == null || DiaryQuickTagsItemsControl == null || NoDiaryQuickTagsText == null || _appData == null) return;

        var globalTags = _appData.GlobalTags ?? new List<string>();
        
        if (globalTags.Count > 0)
        {
            DiaryQuickTagsItemsControl.ItemsSource = null;
            DiaryQuickTagsItemsControl.ItemsSource = globalTags;
            DiaryQuickTagsItemsControl.Visibility = Visibility.Visible;
            NoDiaryQuickTagsText.Visibility = Visibility.Collapsed;
        }
        else
        {
            DiaryQuickTagsItemsControl.Visibility = Visibility.Collapsed;
            NoDiaryQuickTagsText.Visibility = Visibility.Visible;
        }

        DiaryTagPopup.IsOpen = true;
    }

    private void DiaryQuickTag_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is string tag)
        {
            DiaryTagFilterBox.Text = tag;
            DiaryTagPopup.IsOpen = false;
            DiaryTagFilterBox.Focus();
            DiaryTagFilterBox.CaretIndex = DiaryTagFilterBox.Text.Length;
        }
    }

    private void DeleteDiaryQuickTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            if (_appData.GlobalTags != null && _appData.GlobalTags.Contains(tag))
            {
                _appData.GlobalTags.Remove(tag);
                ShowDiaryQuickTagPopup();
                SaveAppData();
            }
            e.Handled = true;
        }
    }

    private void ClosePopup_Click(object sender, RoutedEventArgs e)
    {
        if (DiaryTagPopup != null)
        {
            DiaryTagPopup.IsOpen = false;
        }
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
        // 闅愯棌鎻愮ず鏂囧瓧
        if (DiaryPeriodPlaceholder != null)
        {
            DiaryPeriodPlaceholder.Visibility = DiaryPeriodFilterBox.SelectedIndex > 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        RefreshDiaryTimeline();
    }

    private void RefreshDiaryMonthDates()
    {
        // 鏇存柊鏈堜唤鏍囬
        if (CurrentMonthText != null)
        {
            CurrentMonthText.Text = $"{_currentMonth.Year}年{_currentMonth.Month}月";
        }

        DiaryMonthCalendarPanel.Children.Clear();
        
        var firstDayOfMonth = _currentMonth;
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        
        // 鑾峰彇璇ユ湀绗竴澶╂槸鏄熸湡鍑?
        var firstDayWeekDay = (int)firstDayOfMonth.DayOfWeek;
        
        // 鑾峰彇褰撳墠鐨勬悳绱㈠拰绛涢€夋潯浠?
        var searchText = DiarySearchBox.Text.ToLower().Trim();
        var tagFilter = DiaryTagFilterBox.Text.ToLower().Trim();
        var periodFilter = "";
        if (DiaryPeriodFilterBox.SelectedIndex > 0 && DiaryPeriodFilterBox.SelectedItem is ComboBoxItem selectedItem)
        {
            periodFilter = selectedItem.Content.ToString();
        }
        
        // 鑾峰彇绛涢€夊悗鐨勬棩璁帮紙鐢ㄤ簬鏃ュ巻楂樹寒鏄剧ず锛?
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
        
        // 鑾峰彇绛涢€夊悗鐨勬棩鏈燂紙缁胯壊楂樹寒锛?
        var filteredDates = filteredDiaries
            .Where(d => d.CreatedAt.Date >= firstDayOfMonth && d.CreatedAt.Date <= lastDayOfMonth)
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .ToHashSet();
        
        // 鑾峰彇璇ユ湀鎵€鏈夋湁鏃ヨ鐨勬棩鏈燂紙钃濊壊鏅€氭樉绀猴級
        var datesWithDiaries = _appData.Diaries
            .Where(d => d.CreatedAt.Date >= firstDayOfMonth && d.CreatedAt.Date <= lastDayOfMonth)
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .ToHashSet();

        // 娣诲姞绌虹櫧鏃ユ湡锛堟湀鍒濆墠鐨勭┖鐧斤級
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

        // 娣诲姞璇ユ湀鐨勬墍鏈夋棩鏈?
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
            // 鑾峰彇褰撳墠鐨勬悳绱㈠拰绛涢€夋潯浠?
            var currentSearchText = DiarySearchBox.Text.ToLower().Trim();
            var currentTagFilter = DiaryTagFilterBox.Text.ToLower().Trim();
            var currentPeriodFilter = "";
            if (DiaryPeriodFilterBox.SelectedIndex > 0 && DiaryPeriodFilterBox.SelectedItem is ComboBoxItem selectedItem)
            {
                currentPeriodFilter = selectedItem.Content.ToString();
            }
            
            // 鑾峰彇鎵€鏈夌鍚堟潯浠剁殑鏃ヨ
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
            
            // 灏嗙洰鏍囨棩璁扮Щ鍒版渶鍓嶉潰
            var targetIndex = sortedDiaries.FindIndex(d => d.Id == targetEntry.Id);
            if (targetIndex > 0)
            {
                sortedDiaries.RemoveAt(targetIndex);
                sortedDiaries.Insert(0, targetEntry);
            }
            
            // 閲嶆柊鍒涘缓鏃堕棿绾匡紝灏嗙洰鏍囨棩璁版斁鍦ㄦ渶涓婇潰
            DiaryTimelinePanel.Children.Clear();
            foreach (var entry in sortedDiaries)
            {
                CreateDiaryEntryPanel(entry);
            }
            
            // 灞曞紑鐩爣鏃ヨ
            ToggleDiaryEntry(targetEntry.Id, true);
            
            // 婊氬姩鍒伴《閮紙鍥犱负鐩爣鏃ヨ宸茬粡鍦ㄦ渶涓婇潰锛?
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
        
        // 鑾峰彇鍛ㄦ湡绫诲瀷绛涢€?
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
        
        // 鍛ㄦ湡绫诲瀷绛涢€?
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
        
        // 娣诲姞榧犳爣鐐瑰嚮浜嬩欢鍒版暣涓潯鐩?
        mainStackPanel.MouseLeftButtonDown += (s, e) =>
        {
            ToggleDiaryEntry(entry.Id, true);
            e.Handled = true; // 闃叉浜嬩欢鍐掓场
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
        
        // 鍒涘缓鏍囬琛岋紝鍖呭惈鏍囬鍜屾椂闂?
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
        
        // 鏃堕棿锛堟斁鍦ㄥ彸杈癸級
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
        
        // 鍛ㄦ湡绫诲瀷鍜屾爣绛剧殑琛?
        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0)
        };
        
        // 鍛ㄦ湡绫诲瀷
        var periodTypeText = new TextBlock
        {
            Text = entry.PeriodTypeDescription,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = AppBrushes.A29BFE,
            Margin = new Thickness(0, 0, 15, 0)
        };
        metaRow.Children.Add(periodTypeText);
        
        // 鏍囩
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
            Content = ">",
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
        
        // 鍒涘缓鍐呭闈㈡澘锛岀敤浜庢樉绀哄睍寮€鐨勫唴瀹?
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
        // 鍙樉绀哄唴瀹圭殑鍓嶄笁琛?
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
            ToggleDiaryEntry(entryId); // 浣跨敤鍒囨崲閫昏緫锛岃€屼笉鏄彧鑳藉睍寮€
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
                    // 鐜板湪 mainPanel.Children[0] 鏄爣棰橈紝mainPanel.Children[1] 鏄唴瀹归潰鏉?
                    if (mainPanel.Children[1] is StackPanel contentPanel)
                    {
                        bool shouldExpand;
                        if (expand.HasValue)
                        {
                            shouldExpand = expand.Value;
                        }
                        else
                        {
                            // 鍒囨崲鐘舵€侊細濡傛灉褰撳墠鏄敹璧风姸鎬侊紝鍒欏睍寮€锛涘弽涔嬩害鐒?
                            shouldExpand = contentPanel.Visibility == Visibility.Collapsed;
                        }
                        
                        contentPanel.Visibility = shouldExpand ? Visibility.Visible : Visibility.Collapsed;
                        
                        // 鏇存柊灞曞紑鎸夐挳鍥炬爣
                        if (mainPanel.Children[0] is Border headerBorder && 
                            headerBorder.Child is Grid headerGrid &&
                            headerGrid.Children.Count > 1 &&
                            headerGrid.Children[1] is Button expandButton)
                        {
                            expandButton.Content = shouldExpand ? ">" : "v";
                        }
                    }
                }
                break;
            }
        }
    }

    private void EditDiaryEntry(DiaryEntry entry)
    {
        var editWindow = new DiaryEditWindow(_appData.PersonalInfo, _appData, entry);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            var index = _appData.Diaries.FindIndex(d => d.Id == entry.Id);
            if (index >= 0)
            {
                _appData.Diaries[index] = editWindow.ResultEntry;
                SaveAppData();
                RefreshDiaryTimeline();
                // 鏇存柊涓汉淇℃伅UI鏄剧ず
                LoadPersonalInfo();
            }
        }
    }

    private void DeleteDiaryEntry(DiaryEntry entry)
    {
        var result = MessageBox.Show($"确定要删除日记《{entry.Title}》吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            // Calculate savings to remove
            decimal savingsToRemove = 0;
            if (entry.Parameters != null)
            {
                foreach (var param in entry.Parameters)
                {
                    string trimmedName = param.Name.Trim();
                    if (trimmedName.Equals("金钱", StringComparison.OrdinalIgnoreCase) ||
                        trimmedName.Equals("金额", StringComparison.OrdinalIgnoreCase) || 
                        trimmedName.Equals("savings", StringComparison.OrdinalIgnoreCase) || 
                        trimmedName.Equals("Savings", StringComparison.OrdinalIgnoreCase))
                    {
                        if (decimal.TryParse(param.Value, out decimal paramValue))
                        {
                            savingsToRemove += paramValue;
                        }
                    }
                }
            }
            
            if (savingsToRemove != 0)
            {
                _appData.PersonalInfo.Savings -= savingsToRemove;
                _appData.PersonalInfo.LastUpdated = DateTime.Now;
            }

            _appData.Diaries.RemoveAll(d => d.Id == entry.Id);
            SaveAppData();
            RefreshDiaryTimeline();
            // 鏇存柊涓汉淇℃伅UI鏄剧ず
            LoadPersonalInfo();
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

    #region 浠诲姟妯″潡浜嬩欢

    private void NewTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var taskEditWindow = new TaskEditWindow(_appData);
        if (taskEditWindow.ShowDialog() == true)
        {
            // 濡傛灉鐢ㄦ埛淇濆瓨浜嗕换鍔★紝灏嗕换鍔℃坊鍔犲埌鏁版嵁婧?
            if (taskEditWindow.TaskEntry != null)
            {
                _appData.Tasks.Add(taskEditWindow.TaskEntry);
                SaveAppData();
            }
            // 鍒锋柊浠诲姟鍒楄〃
            RefreshTaskLists();
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        // 妫€鏌ユ槸鍚︽湁閫変腑鐨勪复鏃朵换鍔?
        if (TempTaskListBox.SelectedItem is TaskEntry tempTask)
        {
            DeleteTask(tempTask);
        }
        // 濡偓閺屻儲妲搁崥锔芥箒闁鑵戦惃鍕€嶉惄顔绘崲閸?
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
        var result = MessageBox.Show($"确定要删除任务《{task.Title}》吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _appData.Tasks.RemoveAll(t => t.Id == task.Id);
            SaveAppData();
            RefreshTaskLists();
        }
    }

    private void EditTaskButton_Click(object sender, RoutedEventArgs e)
    {
        TaskEntry? selectedTask = null;
        
        // 娣诲姞鏃ュ織璁板綍褰撳墠閫変腑鐘舵€?
        Log($"编辑任务按钮点击 - 临时任务选中: {TempTaskListBox.SelectedItem != null}, 项目任务选中: {ProjectTaskListBox.SelectedItem != null}");
        
        // 妫€鏌ラ€変腑鐘舵€侊紝濡傛灉涓や釜閮介€変腑浜嗭紙鐞嗚涓婁笉搴旇鍙戠敓锛夛紝浼樺厛澶勭悊椤圭洰浠诲姟鎴栬€呮彁绀虹敤鎴?
        bool isTempSelected = TempTaskListBox.SelectedItem is TaskEntry;
        bool isProjectSelected = ProjectTaskListBox.SelectedItem is TaskEntry;

        if (isTempSelected && isProjectSelected)
        {
            // 寮傚父鎯呭喌锛氫袱涓垪琛ㄩ兘鏈夐€変腑椤?
            // 灏濊瘯娓呴櫎涓存椂浠诲姟鐨勯€変腑鐘舵€侊紝鍋囪鐢ㄦ埛鎯崇紪杈戦」鐩换鍔★紙鍥犱负鐢ㄦ埛鐗瑰埆鎻愬埌浜嗛」鐩换鍔＄殑闂锛?
            TempTaskListBox.SelectedItem = null;
            selectedTask = ProjectTaskListBox.SelectedItem as TaskEntry;
            Log($"检测到双重选中，强制选择项目任务: {selectedTask?.Title}");
        }
        else if (isTempSelected)
        {
            selectedTask = TempTaskListBox.SelectedItem as TaskEntry;
            Log($"选中了临时任务: {selectedTask?.Title}");
        }
        else if (isProjectSelected)
        {
            selectedTask = ProjectTaskListBox.SelectedItem as TaskEntry;
            Log($"选中了项目任务: {selectedTask?.Title}");
        }

        if (selectedTask == null)
        {
            MessageBox.Show("请先选中要编辑的任务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Log("没有选中任何任务，显示提示信息");
            return;
        }

        try
        {
            var editWindow = new TaskEditWindow(_appData, selectedTask);
            if (editWindow.ShowDialog() == true)
            {
                if (editWindow.IsDeleteRequested)
                {
                    _appData.Tasks.RemoveAll(t => t.Id == selectedTask.Id);
                }
                SaveAppData();
                RefreshTaskLists();
            }
        }
        catch (Exception ex)
        {
            Log($"打开任务编辑窗口时发生异常: {ex.Message}");
            Log($"异常堆栈: {ex.StackTrace}");
            MessageBox.Show($"打开任务编辑窗口时发生错误: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void RefreshTaskLists()
    {
        // 淇濆瓨褰撳墠閫変腑鐘舵€?
        var selectedTempTaskId = (TempTaskListBox.SelectedItem as TaskEntry)?.Id;
        var selectedProjectTaskId = (ProjectTaskListBox.SelectedItem as TaskEntry)?.Id;
        
        // 娓呯┖鐜版湁鍒楄〃
        TempTaskListBox.Items.Clear();
        ProjectTaskListBox.Items.Clear();
        
        // 灏嗕换鍔″垎绫诲苟鎺掑簭鍚庢坊鍔犲埌涓嶅悓鍒楄〃
        // 鎺掑簭瑙勫垯锛氬凡瀹屾垚鐨勪换鍔℃帓鍦ㄦ湭瀹屾垚鐨勪换鍔′笅闈?-> 鎸夋爣绛惧垎缁?-> 鎸夊垱寤烘椂闂存帓搴?
        var sortedTasks = _appData.Tasks.OrderBy(t => t.Status == TaskStatus.Completed ? 1 : 0) // 鍏堟寜瀹屾垚鐘舵€佹帓搴?
                                      .ThenBy(t => t.ProjectTags != null && t.ProjectTags.Count > 0 ? t.ProjectTags[0] : "zzzzzz") // 再按标签排序，无标签排在最后
                                      .ThenBy(t => t.CreatedAt); // 最后按创建时间排序
        
        foreach (var task in sortedTasks)
        {
            // 鏍规嵁浠诲姟绫诲瀷鍒嗙被
            if (task.TaskType == TaskType.Temporary)
            {
                TempTaskListBox.Items.Add(task);
            }
            else if (task.TaskType == TaskType.Project)
            {
                ProjectTaskListBox.Items.Add(task);
            }
        }
        
        // 鎭㈠閫変腑鐘舵€?- 鍏堟仮澶嶄复鏃朵换鍔★紝鍐嶆仮澶嶉」鐩换鍔?
        if (selectedTempTaskId != null)
        {
            var taskToSelect = _appData.Tasks.FirstOrDefault(t => t.Id == selectedTempTaskId && t.TaskType == TaskType.Temporary);
            if (taskToSelect != null)
            {
                TempTaskListBox.SelectedItem = taskToSelect;
            }
        }
        
        if (selectedProjectTaskId != null)
        {
            var taskToSelect = _appData.Tasks.FirstOrDefault(t => t.Id == selectedProjectTaskId && t.TaskType == TaskType.Project);
            if (taskToSelect != null)
            {
                ProjectTaskListBox.SelectedItem = taskToSelect;
            }
        }

        // 鏇存柊浠诲姟璁℃暟
        UpdateTaskCounts();
        UpdateReminderTimer();
    }

    // 鏇存柊浠诲姟璁℃暟
    private void UpdateTaskCounts()
    {
        // 璁＄畻涓存椂浠诲姟鐨勬湭瀹屾垚鏁伴噺鍜屾€绘暟
        var tempTasks = _appData.Tasks.Where(t => t.TaskType == TaskType.Temporary).ToList();
        var tempUncompletedCount = tempTasks.Count(t => t.Status != TaskStatus.Completed);
        TempTaskCount = $"{tempUncompletedCount}/{tempTasks.Count}";

        // 璁＄畻椤圭洰浠诲姟鐨勬湭瀹屾垚鏁伴噺鍜屾€绘暟
        var projectTasks = _appData.Tasks.Where(t => t.TaskType == TaskType.Project).ToList();
        var projectUncompletedCount = projectTasks.Count(t => t.Status != TaskStatus.Completed);
        ProjectTaskCount = $"{projectUncompletedCount}/{projectTasks.Count}";
    }

    private void TempTaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try 
        {
            if (TempTaskListBox.SelectedItem != null)
            {
                ProjectTaskListBox.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
            Log($"TempTaskListBox_SelectionChanged error: {ex.Message}");
        }
    }

    private void ProjectTaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ProjectTaskListBox.SelectedItem != null)
            {
                TempTaskListBox.SelectedItem = null;
            }
        }
        catch (Exception ex)
        {
             Log($"ProjectTaskListBox_SelectionChanged error: {ex.Message}");
        }
    }

    private void TempTaskListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 绌烘柟娉曪紝閬垮厤闂€€
    }

    private void ProjectTaskListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 绌烘柟娉曪紝閬垮厤闂€€
    }

    private void EditSelectedTask()
    {
        try
        {
            TaskEntry? selectedTask = null;
            
            if (TempTaskListBox.SelectedItem is TaskEntry tempTask)
            {
                selectedTask = tempTask;
            }
            else if (ProjectTaskListBox.SelectedItem is TaskEntry projectTask)
            {
                selectedTask = projectTask;
            }

            if (selectedTask == null)
            {
                MessageBox.Show("请先选中要编辑的任务", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Log($"开始编辑任务: {selectedTask.Title}");
            var editWindow = new TaskEditWindow(_appData, selectedTask);
            if (editWindow.ShowDialog() == true)
            {
                if (editWindow.IsDeleteRequested)
                {
                    _appData.Tasks.RemoveAll(t => t.Id == selectedTask.Id);
                    Log($"浠诲姟宸插垹闄? {selectedTask.Title}");
                }
                else
                {
                    Log($"浠诲姟宸叉洿鏂? {selectedTask.Title}");
                }
                SaveAppData();
                RefreshTaskLists();
            }
            Log("浠诲姟缂栬緫瀹屾垚");
        }
        catch (Exception ex)
        {
            Log($"编辑任务时发生异常: {ex.Message}");
            Log($"异常堆栈: {ex.StackTrace}");
            MessageBox.Show($"编辑任务时发生错误: {ex.Message}\n\n详细信息: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region TabControl浜嬩欢澶勭悊

    // 娣诲姞涓€涓瓧娈垫潵璺熻釜涓婁竴娆＄殑閫変腑绱㈠紩
    private int _previousTabIndex = -1;

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            int currentIndex = MainTabControl.SelectedIndex;
            Log("MainTabControl_SelectionChanged 事件触发，当前索引: " + currentIndex);
            
            // 鍙湁褰撶储寮曠湡姝ｅ彂鐢熷彉鍖栨椂鎵嶅鐞嗕簨浠?
            if (currentIndex == _previousTabIndex)
            {
                return;
            }
            
            // 鏇存柊涓婁竴娆＄殑绱㈠紩
            _previousTabIndex = currentIndex;
            
            // 濡傛灉鍒囨崲鍒版椂闂磋褰曟ā鍧楋紙绱㈠紩涓?锛夛紝鍒濆鍖栨椂闂磋褰曟樉绀?
            if (currentIndex == 2)
            {
                Log("切换到时间记录模块，开始初始化时间记录显示");
                
                // 纭繚鍛ㄨ鍥炬纭洿鏂?
                UpdateWeekDisplay();
                
                // 閲嶆柊鍒濆鍖栨椂闂磋褰曟樉绀?
                UpdateTimeRecordDisplay();
                
                Log("时间记录显示初始化完成");
            }

            // 濡傛灉鍒囨崲鍒版暟鎹鐞嗘ā鍧楋紙绱㈠紩涓?锛夛紝鍒濆鍖栨暟鎹粺璁?
            if (currentIndex == 4)
            {
                Log("切换到数据管理模块，开始计算统计数据");
                UpdateDataManagementStats();
            }
        }
        catch (Exception ex)
        {
            LogCrash("TabControl切换时发生错误", ex);
            MessageBox.Show($"TabControl 切换时发生错误: {ex.Message}", "错误");
        }
    }

    private void UpdateDataManagementStats()
    {
        try
        {
            // 1. 鍩虹缁熻
            if (StatsTotalCheckInsText != null)
                StatsTotalCheckInsText.Text = _appData.CheckIns.Count.ToString();
            
            if (StatsRecordedDaysText != null)
                StatsRecordedDaysText.Text = _appData.TimeRecords.Select(t => t.Date.Date).Distinct().Count().ToString();
            
            if (StatsTotalTasksText != null)
                StatsTotalTasksText.Text = _appData.Tasks.Count.ToString();
            
            if (StatsTotalDiariesText != null)
                StatsTotalDiariesText.Text = _appData.Diaries.Count.ToString();

            // 2. 璇︾粏缁熻
            
            // 鏈€闀胯繛缁墦鍗?
            if (StatsLongestStreakText != null)
            {
                if (_appData.CheckInProjects.Count > 0)
                {
                    // 璁＄畻姣忎釜椤圭洰鐨勫綋鍓嶈繛缁墦鍗?
                    var projectStreaks = new List<(string Name, int Streak)>();
                    foreach (var project in _appData.CheckInProjects)
                    {
                        var projectCheckIns = _appData.CheckIns
                            .Where(c => c.ProjectId == project.Id)
                            .ToList();
                        var streak = CalculateCheckInCurrentStreak(projectCheckIns);
                        projectStreaks.Add((project.Name, streak));
                    }
                    
                    var bestProject = projectStreaks.OrderByDescending(p => p.Streak).FirstOrDefault();
                    if (bestProject.Streak > 0)
                    {
                        StatsLongestStreakText.Text = $"{bestProject.Name}: {bestProject.Streak} 天";
                    }
                    else
                    {
                        StatsLongestStreakText.Text = "暂无连续打卡";
                    }
                }
                else
                {
                    StatsLongestStreakText.Text = "暂无打卡项目";
                }
            }

            // 杩炵画璁版棩璁?
            if (StatsDiaryStreakText != null)
            {
                var streak = CalculateDiaryStreak();
                StatsDiaryStreakText.Text = $"{streak} 天";
            }

            // 鏈€杩戝畬鎴愮殑涓変釜椤圭洰
            if (StatsRecentProjectsList != null)
            {
                var recentProjects = _appData.Tasks
                    .Where(t => t.TaskType == TaskType.Project && t.Status == TaskStatus.Completed && t.CompletedAt.HasValue)
                    .OrderByDescending(t => t.CompletedAt)
                    .Take(3)
                    .Select(t => t.Title)
                    .ToList();
                
                if (recentProjects.Count == 0) recentProjects.Add("暂无已完成项目");
                StatsRecentProjectsList.ItemsSource = recentProjects;
            }

            // 娲诲姩鏃堕棿鏈€闀跨殑涓変釜娲诲姩
            if (StatsTopActivitiesList != null)
            {
                var topActivities = _appData.TimeRecords
                    .GroupBy(t => t.Activity)
                    .Select(g => new { Name = g.Key, TotalHours = g.Sum(t => t.DurationHours) })
                    .OrderByDescending(x => x.TotalHours)
                    .Take(3)
                    .Select(x => new { Name = x.Name, Time = $"{x.TotalHours:F1} 小时" })
                    .ToList();
                
                if (topActivities.Count == 0) 
                {
                    StatsTopActivitiesList.ItemsSource = new[] { new { Name = "暂无活动记录", Time = "" } };
                }
                else
                {
                    StatsTopActivitiesList.ItemsSource = topActivities;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"鏇存柊鏁版嵁绠＄悊缁熻澶辫触: {ex.Message}");
        }
    }

    private int CalculateDiaryStreak()
    {
        if (!_appData.Diaries.Any()) return 0;
        
        var dates = _appData.Diaries
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();
            
        if (!dates.Any()) return 0;

        // 濡傛灉浠婂ぉ娌℃湁鍐欐棩璁帮紝浠庢槰澶╁紑濮嬬畻
        // 濡傛灉浠婂ぉ鍐欎簡锛屼粠浠婂ぉ寮€濮嬬畻
        var checkDate = DateTime.Today;
        if (!dates.Contains(checkDate))
        {
            checkDate = DateTime.Today.AddDays(-1);
            if (!dates.Contains(checkDate)) return 0; // 鏄ㄥぉ涔熸病鍐欙紝鏂簡
        }
        
        int streak = 0;
        foreach (var date in dates)
        {
            if (date == checkDate)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (date < checkDate)
            {
                break;
            }
        }
        
        return streak;
    }

    #endregion

    #region 鏃堕棿璁板綍妯″潡浜嬩欢

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
            CurrentWeekText.Text = $"{_currentWeekStart.Year}年第{GetWeekNumber(_currentWeekStart)}周 ({_currentWeekStart:MM-dd} ~ {weekEnd:MM-dd})";
        }

        // 鏇存柊琛ㄥご鏃ユ湡
        try
        {
            if (WeekDay1Text != null) WeekDay1Text.Text = $"周一 ({_currentWeekStart:MM.dd})";
            if (WeekDay2Text != null) WeekDay2Text.Text = $"周二 ({_currentWeekStart.AddDays(1):MM.dd})";
            if (WeekDay3Text != null) WeekDay3Text.Text = $"周三 ({_currentWeekStart.AddDays(2):MM.dd})";
            if (WeekDay4Text != null) WeekDay4Text.Text = $"周四 ({_currentWeekStart.AddDays(3):MM.dd})";
            if (WeekDay5Text != null) WeekDay5Text.Text = $"周五 ({_currentWeekStart.AddDays(4):MM.dd})";
            if (WeekDay6Text != null) WeekDay6Text.Text = $"周六 ({_currentWeekStart.AddDays(5):MM.dd})";
            if (WeekDay7Text != null) WeekDay7Text.Text = $"周日 ({_currentWeekStart.AddDays(6):MM.dd})";
        }
        catch (Exception ex)
        {
            Log($"更新周视图日期失败: {ex.Message}");
        }
    }

    private void AddTimeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        // 閸掓稑缂撻弬鎵畱閺冨爼妫跨拋鏉跨秿
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

        // 鎵撳紑缂栬緫绐楀彛
        EditTimeRecord(newRecord);
    }

    private void UpdateTimeRecordDisplay()
    {
        // 閼惧嘲褰囪ぐ鎾冲閸涖劎娈戦弮鍫曟？鐠佹澘缍?
        var weekRecords = _appData.TimeRecords
            .Where(t => t.Date.Date >= _currentWeekStart.Date && t.Date.Date <= _currentWeekStart.AddDays(6).Date)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.StartTime)
            .ToList();

        // 濞撳懘娅庨悳鐗堟箒閻ㄥ嫭妞傞梻鏉戞健
        var timeGrid = _timeGrid;
        if (timeGrid != null)
        {
            // 棣栧厛绉婚櫎鎵€鏈夐紶鏍囦簨浠跺鐞嗙▼搴忥紝閬垮厤閲嶅璁㈤槄
            timeGrid.MouseLeftButtonDown -= TimeGrid_MouseLeftButtonDown;
            timeGrid.MouseMove -= TimeGrid_MouseMove;
            timeGrid.MouseLeftButtonUp -= TimeGrid_MouseLeftButtonUp;
            timeGrid.MouseLeave -= TimeGrid_MouseLeave;
            
            // 娣囨繂鐡ㄩ弮鍫曟？閺嶅洨顒烽崪灞炬）閺堢喐鐖ｆ０?
            var timeLabels = new List<UIElement>();
            var dateHeaders = new List<UIElement>();
            
            for (int i = 0; i < timeGrid.Children.Count; i++)
            {
                var child = timeGrid.Children[i];
                var row = Grid.GetRow(child);
                var col = Grid.GetColumn(child);
                
                // 娣囨繂鐡ㄩ弮鍫曟？閺嶅洨顒烽敍鍫㈩儑0閸掓绱?
                if (col == 0 && child is TextBlock)
                {
                    timeLabels.Add(child);
                }
                // 娣囨繂鐡ㄩ弮銉︽埂閺嶅洭顣介敍鍫㈩儑0鐞涘矉绱?
                else if (row == 0 && child is TextBlock)
                {
                    dateHeaders.Add(child);
                }
            }
            
            // 濞撳懘娅庨幍鈧張澶婄摍閸忓啰绀?
            timeGrid.Children.Clear();
            
            // 闁插秵鏌婂ǎ璇插閺冨爼妫块弽鍥╊劮閸滃本妫╅張鐔哥垼妫?
            foreach (var label in timeLabels)
            {
                timeGrid.Children.Add(label);
            }
            foreach (var header in dateHeaders)
            {
                timeGrid.Children.Add(header);
            }

            // 闁插秵鏌婄紒妯哄煑缂冩垶鐗哥痪?
            DrawGridLines(timeGrid);
        
            // 缂佹ê鍩楅弮鍫曟？鐠佹澘缍?
            DrawTimeRecords(timeGrid, weekRecords);
            
            // 閲嶆柊娣诲姞榧犳爣浜嬩欢澶勭悊绋嬪簭
            timeGrid.MouseLeftButtonDown += TimeGrid_MouseLeftButtonDown;
            timeGrid.MouseMove += TimeGrid_MouseMove;
            timeGrid.MouseLeftButtonUp += TimeGrid_MouseLeftButtonUp;
            timeGrid.MouseLeave += TimeGrid_MouseLeave;
            
            // 閲嶇疆鎷栧姩鐘舵€佸彉閲忥紝闃叉缂栬緫淇濆瓨鍚庢嫋鍔ㄥ紓甯?
            _isDragging = false;
            _startRow = -1;
            _startCol = -1;
            _currentRow = -1;
            _currentCol = -1;
            _dragPreviewBorder = null;
        }
    }
    
    private void DrawGridLines(Grid timeGrid)
    {
        if (timeGrid == null) return;
        
        // 缂佹ê鍩楅弮鍫曟？閸ф缍夐弽鑲╁殠
        for (int row = 0; row < 24; row++)
        {
            for (int col = 1; col <= 7; col++)
            {
                var border = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                Grid.SetRow(border, row); // 绗竴琛屽搴?0:00
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
            // 鐠侊紕鐣婚弰鐔告埂閸戠媴绱?=閸涖劋绔? 6=閸涖劍妫╅敍?
            int dayOfWeek = (int)record.Date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // 鐏忓棗鎳嗛弮銉ょ矤0鏉烆剚宕叉稉?
            dayOfWeek -= 1; // 鏉烆剚宕叉稉?-6閻ㄥ嫮鍌ㄥ?
            
            // 鐠侊紕鐣诲鈧慨瀣闂傜顢戦崣鍑ょ礄00:00-23:00閿涘苯鍙?4鐏忓繑妞傞敍?
            int startHour = record.StartTime.Hours;
            if (startHour < 0 || startHour >= 24) continue; // 閸欘亝妯夌粈?0:00-23:00閻ㄥ嫯顔囪ぐ?
            
            int startRow = startHour;
            
            // 璁＄畻缁撴潫鏃堕棿琛屽彿
            int endHour = (int)record.EndTime.TotalHours;
            if (endHour <= 0 && record.EndTime.TotalMinutes > 0) endHour = 24; // Handle cases where total hours might be 0 but there is duration, or just use TotalHours logic
            
            // If strictly 0 duration, skip
            if (endHour <= 0 && record.EndTime.TotalMinutes <= 0) continue;
            
            if (endHour > 24) endHour = 24;
            
            int endRow = endHour - 1;
            int rowSpan = endHour - startHour; // 鐩存帴璁＄畻灏忔椂宸紝纭繚璺ㄥ害姝ｇ‘
            
            if (rowSpan < 1) rowSpan = 1;
            
            // 鍒涘缓鏃堕棿鍧?
            // 浣跨敤 Category 浣滀负鏍囩棰滆壊锛屽鏋滄病鏈?Category 鍒欎娇鐢?Activity
            string colorKey = !string.IsNullOrEmpty(record.Category) ? record.Category : record.Activity;
            var brush = TagToColorConverter.GetColorBrush(colorKey);
            // 绋嶅井澧炲姞閫忔槑搴?
            var color = brush.Color;
            var transparentBrush = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B));
            transparentBrush.Freeze();

            var timeBlock = new Border
            {
                Background = transparentBrush,
                BorderBrush = Brushes.Transparent, // 绉婚櫎杈规锛岀湅璧锋潵鏇存墎骞冲寲
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand
            };
            
            // 濞ｈ濮為悙鐟板毊娴滃娆?
            timeBlock.MouseLeftButtonDown += (s, e) => 
            {
                e.Handled = true; // 闃绘浜嬩欢鍐掓场鍒扮綉鏍肩殑鐐瑰嚮浜嬩欢
                EditTimeRecord(record);
            };
            
            // 娣诲姞鍙抽敭鑿滃崟
            var contextMenu = new ContextMenu();
            var deleteMenuItem = new MenuItem { Header = "删除" };
            deleteMenuItem.Click += (s, e) =>
            {
                // 鏄剧ず纭鍒犻櫎瀵硅瘽妗?
                var result = MessageBox.Show("确定要删除这条时间记录吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 浣跨敤RemoveAll鏂规硶鐩存帴鍒犻櫎璁板綍
                        _appData.TimeRecords.RemoveAll(r => r.Id == record.Id);
                        SaveAppData();
                        UpdateTimeRecordDisplay();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除失败：{ex.Message}，请检查程序目录的写入权限。", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            contextMenu.Items.Add(deleteMenuItem);
            timeBlock.ContextMenu = contextMenu;
            
            // 閸掓稑缂撻崘鍛啇闂堛垺婢?
            var contentPanel = new StackPanel
            {
                Margin = new Thickness(5),
                Background = Brushes.Transparent
            };
            
            // 娣诲姞鍒嗙被鏍囩锛堝鏋滄湁锛?
            if (!string.IsNullOrEmpty(record.Category))
            {
                var tagBorder = new Border
                {
                    Background = TagToColorConverter.GetColorBrush(record.Category),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 1, 4, 1),
                    Margin = new Thickness(0, 0, 0, 4),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                var tagText = new TextBlock
                {
                    Text = record.Category,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };

                tagBorder.Child = tagText;
                contentPanel.Children.Add(tagBorder);
            }
            
            // 濞ｈ濮炲ú璇插З閸氬秶袨
            var activityText = new TextBlock
            {
                Text = record.Activity,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            
            // 濞ｈ濮為弮鍫曟？閼煎啫娲?
            var timeText = new TextBlock
            {
                Text = $"{record.StartTime:hh\\:mm} - {record.EndTime:hh\\:mm}",
                FontSize = 10,
                Foreground = Brushes.LightGray
            };
            
            contentPanel.Children.Add(activityText);
            contentPanel.Children.Add(timeText);
            
            timeBlock.Child = contentPanel;
            
            // 鐠佸墽鐤嗘担宥囩枂閸滃苯銇囩亸?
            Grid.SetRow(timeBlock, startRow); // 鐩存帴浣跨敤寮€濮嬪皬鏃朵綔涓鸿绱㈠紩锛屽搴?0:00-23:00
            Grid.SetColumn(timeBlock, dayOfWeek + 1); // +1 閺勵垰娲滄稉铏诡儑0閸掓妲搁弮鍫曟？閺嶅洨顒?
            Grid.SetRowSpan(timeBlock, rowSpan);
            Grid.SetColumnSpan(timeBlock, 1);
            
            // 濞ｈ濮為崚鎵秹閺?
            timeGrid.Children.Add(timeBlock);
        }
    }
    
    private void EditTimeRecord(TimeRecordEntry record)
    {
        // 妫€鏌ヨ褰曟槸鍚﹀凡缁忓瓨鍦ㄤ簬闆嗗悎涓?
        bool isExistingRecord = _appData.TimeRecords.Any(r => r.Id == record.Id);
        
        // 璋冭瘯淇℃伅

        
        var editWindow = new TimeRecordEditWindow(_appData, record);
        var result = editWindow.ShowDialog();
        
        
        
        if (result == true)
        {
            // 濡傛灉鏄柊璁板綍锛屾坊鍔犲埌闆嗗悎涓?
            if (!isExistingRecord)
            {
                _appData.TimeRecords.Add(editWindow.EditedRecord);
            }
            else
            {
                // 鏇存柊鐜版湁璁板綍
                var existingRecord = _appData.TimeRecords.FirstOrDefault(r => r.Id == editWindow.EditedRecord.Id);
                if (existingRecord != null)
                {
                    // 鏇存柊鐜版湁璁板綍鐨勬墍鏈夊睘鎬?
                    existingRecord.Date = editWindow.EditedRecord.Date;
                    existingRecord.StartTime = editWindow.EditedRecord.StartTime;
                    existingRecord.EndTime = editWindow.EditedRecord.EndTime;
                    existingRecord.Activity = editWindow.EditedRecord.Activity;
                    existingRecord.Category = editWindow.EditedRecord.Category;
                    existingRecord.Notes = editWindow.EditedRecord.Notes;
                }
            }
            
            // 閲嶆柊鎺掑簭璁板綍
            _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
            
            try
            {
                // 淇濆瓨鏁版嵁
                SaveAppData();
                // 鏇存柊鏄剧ず
                UpdateTimeRecordDisplay();
                
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}，请检查程序目录的写入权限。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else if (result == null)
        {
            // 鍒犻櫎璁板綍锛堝彧澶勭悊宸叉湁璁板綍锛?
            if (isExistingRecord)
            {
                
                
                try
                {
                    // 浣跨敤RemoveAll鏂规硶鐩存帴鍒犻櫎璁板綍
                    _appData.TimeRecords.RemoveAll(r => r.Id == record.Id);
                    
                    SaveAppData();
                    UpdateTimeRecordDisplay();
                    
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败：{ex.Message}，请检查程序目录的写入权限。", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                
            }
        }
        else
        {
            
        }
        // 濡傛灉鐐瑰嚮鍙栨秷锛屽浜庢柊璁板綍涓嶅仛浠讳綍澶勭悊锛屽浜庡凡鏈夎褰曚繚鎸佷笉鍙?
    }
    
    private void TimeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null) return;
        
        // 鑾峰彇榧犳爣鐐瑰嚮浣嶇疆
        Point mousePoint = e.GetPosition(timeGrid);
        
        // 纭畾鐐瑰嚮鐨勮鍜屽垪
        int row = GetRowFromPoint(mousePoint, timeGrid);
        int col = GetColumnFromPoint(mousePoint, timeGrid);
        
        // 鍙鐞嗘湁鏁堢殑琛屽拰鍒楋紙琛岋細0-23锛屽垪锛?-7锛?
        if (row >= 0 && row <= 23 && col >= 1 && col <= 7)
        {
            _isDragging = true;
            _startRow = row;
            _startCol = col;
            _currentRow = row;
            _currentCol = col;
            
            // 鍒涘缓鎷栧姩棰勮杈规
            _dragPreviewBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 108, 92, 231)),
                BorderBrush = Brushes.DarkSlateBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Opacity = 0.7
            };
            
            // 璁剧疆鍒濆浣嶇疆
            Grid.SetRow(_dragPreviewBorder, row);
            Grid.SetColumn(_dragPreviewBorder, col);
            Grid.SetRowSpan(_dragPreviewBorder, 1);
            Grid.SetColumnSpan(_dragPreviewBorder, 1);
            
            // 娣诲姞鍒扮綉鏍?
            timeGrid.Children.Add(_dragPreviewBorder);
            
            // 鎹曡幏榧犳爣锛岀‘淇濊兘璺熻釜鍒伴紶鏍囩寮€绐楀彛鐨勬儏鍐?
            timeGrid.CaptureMouse();
        }
    }
    
    private void TimeGrid_MouseMove(object sender, MouseEventArgs e)
    {
        var timeGrid = sender as Grid;
        if (timeGrid == null || !_isDragging) return;
        
        // 鑾峰彇榧犳爣浣嶇疆
        Point mousePoint = e.GetPosition(timeGrid);
        
        // 纭畾褰撳墠鐨勮鍜屽垪
        int row = GetRowFromPoint(mousePoint, timeGrid);
        int col = GetColumnFromPoint(mousePoint, timeGrid);
        
        // 鍙鐞嗘湁鏁堢殑琛屽拰鍒楋紙琛岋細0-23锛屽垪锛?-7锛?
            if (row >= 0 && row <= 23 && col >= 1 && col <= 7)
        {
            _currentRow = row;
            _currentCol = col;
            
            // 鏇存柊鎷栧姩棰勮杈规鐨勪綅缃拰澶у皬
            if (_dragPreviewBorder != null)
            {
                // 璁＄畻璧峰鍜岀粨鏉熶綅缃紙纭繚璧峰浣嶇疆灏忎簬缁撴潫浣嶇疆锛?
                int startRow = Math.Min(_startRow, _currentRow);
                int endRow = Math.Max(_startRow, _currentRow);
                int startCol = Math.Min(_startCol, _currentCol);
                int endCol = Math.Max(_startCol, _currentCol);
                
                // 璁剧疆浣嶇疆鍜岃法搴?
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
        
        // 閲婃斁榧犳爣鎹曡幏
        timeGrid.ReleaseMouseCapture();
        
        // 绉婚櫎鎷栧姩棰勮杈规
        if (_dragPreviewBorder != null)
        {
            timeGrid.Children.Remove(_dragPreviewBorder);
            _dragPreviewBorder = null;
        }
        
        // 璁＄畻璧峰鍜岀粨鏉熶綅缃紙纭繚璧峰浣嶇疆灏忎簬缁撴潫浣嶇疆锛?
        int startRow = Math.Min(_startRow, _currentRow);
        int endRow = Math.Max(_startRow, _currentRow);
        int startCol = Math.Min(_startCol, _currentCol);
        int endCol = Math.Max(_startCol, _currentCol);
        
        // 鍙鐞嗘湁鏁堢殑閫夋嫨锛堣嚦灏?琛?鍒楋級
        if (startRow >= 0 && endRow <= 23 && startCol >= 1 && endCol <= 7)
        {
            // 鍒涘缓鏂扮殑鏃堕棿璁板綍
            CreateNewTimeRecord(startCol, endCol, startRow, endRow);
        }
        
        // 閲嶇疆鎷栧姩鐘舵€?
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
        
        // 閲婃斁榧犳爣鎹曡幏
        timeGrid.ReleaseMouseCapture();
        
        // 绉婚櫎鎷栧姩棰勮杈规
        if (_dragPreviewBorder != null)
        {
            timeGrid.Children.Remove(_dragPreviewBorder);
            _dragPreviewBorder = null;
        }
        
        // 閲嶇疆鎷栧姩鐘舵€?
        _isDragging = false;
        _startRow = -1;
        _startCol = -1;
        _currentRow = -1;
        _currentCol = -1;
    }
    
    private int GetRowFromPoint(Point point, Grid grid)
    {
        // 浣跨敤鍥哄畾琛岄珮40锛屼笌XAML涓畾涔夌殑涓€鑷?
        double rowHeight = 40;
        
        int row = (int)Math.Floor(point.Y / rowHeight);
        
        // 纭繚琛屽彿鍦ㄦ湁鏁堣寖鍥村唴锛?-23锛屽搴?0:00-23:00锛?
        return Math.Max(0, Math.Min(23, row));
    }
    
    private int GetColumnFromPoint(Point point, Grid grid)
    {
        double timeLabelWidth = 60; // 鏃堕棿鏍囩鍒楀鍥哄畾涓?0
        if (point.X < timeLabelWidth)
        {
            return 0; // 鏃堕棿鏍囩鍒?
        }
        
        double totalColumnsWidth = grid.ActualWidth - timeLabelWidth;
        if (totalColumnsWidth <= 0)
        {
            return 1; // 榛樿杩斿洖绗竴鍒?
        }
        
        double columnWidth = totalColumnsWidth / 7;
        int col = (int)Math.Floor((point.X - timeLabelWidth) / columnWidth) + 1;
        
        // 纭繚鍒楁暟鍦ㄦ湁鏁堣寖鍥村唴
        return Math.Max(1, Math.Min(7, col));
    }
    
    private void CreateNewTimeRecord(int startCol, int endCol, int startRow, int endRow)
    {
        try
        {
            // 鍙傛暟楠岃瘉
            if (startCol < 1 || endCol > 7 || startRow < 0 || endRow > 23)
            {
                MessageBox.Show("选择的时间段无效，请重新选择。", "错误");
                return;
            }
            
            // 纭繚璧峰浣嶇疆灏忎簬缁撴潫浣嶇疆
            int actualStartCol = Math.Min(startCol, endCol);
            int actualEndCol = Math.Max(startCol, endCol);
            int actualStartRow = Math.Min(startRow, endRow);
            int actualEndRow = Math.Max(startRow, endRow);
            
            // 璁＄畻寮€濮嬫椂闂村拰缁撴潫鏃堕棿锛堟瘡涓€琛屼唬琛ㄤ竴涓皬鏃讹紝浠?0:00寮€濮嬶級
            TimeSpan startTime = TimeSpan.FromHours(actualStartRow);
            TimeSpan endTime = TimeSpan.FromHours(actualEndRow + 1);
            
            // 璁＄畻寮€濮嬫棩鏈熷拰缁撴潫鏃ユ湡
            DateTime startDate = _currentWeekStart.AddDays(actualStartCol - 1);
            DateTime endDate = _currentWeekStart.AddDays(actualEndCol - 1);
            
            // 濡傛灉閫夋嫨浜嗗涓皬鏃朵絾鍙湁涓€澶╋紝鍒涘缓涓€涓椂闂磋褰?
            // 鍒涘缓瑕佺紪杈戠殑璁板綍
            TimeRecordEntry recordToEdit = null;
            
            if (startDate == endDate)
            {
                // 鍒涘缓鏂扮殑鏃堕棿璁板綍锛堜絾涓嶇珛鍗虫坊鍔犲埌闆嗗悎锛?
                recordToEdit = new TimeRecordEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = startDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Activity = "新活动",
                    Category = "宸ヤ綔",
                    Notes = "",
                    CreatedAt = DateTime.Now
                };
            }
            // 濡傛灉閫夋嫨浜嗗涓ぉ锛岄渶瑕佺壒娈婂鐞?
            else
            {
                // 鍏堝垱寤鸿缂栬緫鐨勭涓€澶╄褰曪紙涓嶇珛鍗虫坊鍔犲埌闆嗗悎锛?
                recordToEdit = new TimeRecordEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = startDate,
                    StartTime = startTime,
                    EndTime = TimeSpan.FromHours(24),
                    Activity = "新活动",
                    Category = "宸ヤ綔",
                    Notes = "",
                    CreatedAt = DateTime.Now
                };
            }
            
            // 鎵撳紑缂栬緫绐楀彛
            if (recordToEdit != null)
            {
                // 鐗规畩澶勭悊璺ㄥぉ鎯呭喌
                if (startDate != endDate)
                {
                    // 鏄剧ず鎻愮ず锛屽憡鐭ョ敤鎴疯法澶╅€夋嫨浼氬垱寤哄涓褰?
                    MessageBox.Show("你选择了跨天的时间段，将会创建多条记录。请先编辑并保存第一天的记录，其余日期的记录会自动创建。", "提示");
                    
                    // 鍏堣鐢ㄦ埛缂栬緫绗竴澶╃殑璁板綍
                    EditTimeRecord(recordToEdit);
                    
                    // 濡傛灉鐢ㄦ埛鐐瑰嚮浜嗕繚瀛橈紝鍐嶅垱寤哄叾浠栧ぉ鐨勮褰?
                    if (_appData.TimeRecords.Any(r => r.Id == recordToEdit.Id))
                    {
                        // 鍒涘缓涓棿澶╃殑璁板綍
                        for (int day = actualStartCol + 1; day < actualEndCol; day++)
                        {
                            var midDayRecord = new TimeRecordEntry
                            {
                                Id = Guid.NewGuid().ToString(),
                                Date = _currentWeekStart.AddDays(day - 1),
                                StartTime = TimeSpan.FromHours(0),
                                EndTime = TimeSpan.FromHours(24),
                                Activity = recordToEdit.Activity,
                                Category = recordToEdit.Category,
                                Notes = recordToEdit.Notes,
                                CreatedAt = DateTime.Now
                            };
                            _appData.TimeRecords.Add(midDayRecord);
                        }
                        
                        // 鍒涘缓鏈€鍚庝竴澶╃殑璁板綍
                        var lastDayRecord = new TimeRecordEntry
                        {
                            Id = Guid.NewGuid().ToString(),
                            Date = endDate,
                            StartTime = TimeSpan.FromHours(0),
                            EndTime = endTime,
                            Activity = recordToEdit.Activity,
                            Category = recordToEdit.Category,
                            Notes = recordToEdit.Notes,
                            CreatedAt = DateTime.Now
                        };
                        _appData.TimeRecords.Add(lastDayRecord);
                        
                        // 淇濆瓨鏁版嵁骞舵洿鏂版樉绀?
                        SaveAppData();
                        UpdateTimeRecordDisplay();
                    }
                }
                else
                {
                    // 鍗曞ぉ鎯呭喌锛岀洿鎺ョ紪杈?
                    EditTimeRecord(recordToEdit);
                }
            }
        }
        catch (Exception ex)
        {
            LogCrash("创建时间记录失败", ex);
            MessageBox.Show($"添加时间段记录失败：{ex.Message}", "错误");
        }
    }
    
    #endregion

    #region 缁熶竴淇濆瓨鍜屽浠戒簨浠?

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 閺嶈宓佽ぐ鎾冲闁鑵戦惃鍕垼缁涢箖銆夋穱婵嗙摠鐎电懓绨插Ο鈥虫健閻ㄥ嫭鏆熼幑?
            var selectedTab = MainTabControl.SelectedIndex;
            
            switch (selectedTab)
            {
                case 0: // 閺冦儴顔?
                    break;
                case 1: // 娴犺濮?

                    break;
                case 2: // 閺冨爼妫跨拋鏉跨秿
                    break;
                case 3: // 閹垫挸宕?
                    break;
            }

            SaveAppData();
            
            // 閸掓稑缂撻懛顏勫З婢跺洣鍞?
            BackupManager.CreateAutoBackup(_appData, $"鑷姩澶囦唤 - {DateTime.Now:yyyy-MM-dd HH:mm}");
            
            // 濞撳懐鎮婇弮褍顦禒鏂ょ礉娣囨繄鏆€閺堚偓鏉?0娑?
            BackupManager.CleanOldBackups(10);

            MessageBox.Show("数据已保存，并已自动创建备份。", "成功");
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
            MessageBox.Show("数据已成功保存。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
            Filter = "兼容备份 (*.backup)|*.backup|旧版备份 (*.diary)|*.diary|所有文件 (*.*)|*.*",
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

    // ===== 涓汉鏁版嵁绠＄悊鍔熻兘 =====
    private void LoadPersonalInfo()
    {
        try
        {
            _isLoadingPersonalInfo = true;
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
        finally
        {
            _isLoadingPersonalInfo = false;
        }
    }

    private void PersonalNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingPersonalInfo || !_isPersonalInfoEditing)
        {
            return;
        }

        _appData.PersonalInfo.Name = PersonalNameTextBox.Text;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalPhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingPersonalInfo || !_isPersonalInfoEditing)
        {
            return;
        }

        _appData.PersonalInfo.Phone = PersonalPhoneTextBox.Text;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalBirthdayPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingPersonalInfo || !_isPersonalInfoEditing)
        {
            return;
        }

        _appData.PersonalInfo.Birthday = PersonalBirthdayPicker.SelectedDate;
        _appData.PersonalInfo.LastUpdated = DateTime.Now;
        PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
    }

    private void PersonalSavingsTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isLoadingPersonalInfo || !_isPersonalInfoEditing)
            {
                return;
            }

            if (decimal.TryParse(PersonalSavingsTextBox.Text, out decimal savings))
            {
                // Only update if value changed to avoid circular updates and timestamp resets
                if (_appData.PersonalInfo.Savings != savings)
                {
                    _appData.PersonalInfo.Savings = savings;
                    _appData.PersonalInfo.LastUpdated = DateTime.Now;
                    PersonalLastUpdatedText.Text = $"最后更新：{_appData.PersonalInfo.LastUpdated:yyyy-MM-dd HH:mm}";
                }
            }
        }
        catch (Exception ex)
        {
            Log($"存款输入错误：{ex.Message}");
        }
    }

    private PersonalInfo ClonePersonalInfo(PersonalInfo source)
    {
        return new PersonalInfo
        {
            Id = source.Id,
            Name = source.Name,
            Phone = source.Phone,
            Birthday = source.Birthday,
            Savings = source.Savings,
            LastUpdated = source.LastUpdated
        };
    }

    private void RestorePersonalInfo(PersonalInfo source)
    {
        _appData.PersonalInfo.Id = source.Id;
        _appData.PersonalInfo.Name = source.Name;
        _appData.PersonalInfo.Phone = source.Phone;
        _appData.PersonalInfo.Birthday = source.Birthday;
        _appData.PersonalInfo.Savings = source.Savings;
        _appData.PersonalInfo.LastUpdated = source.LastUpdated;
    }

    private void SetPersonalInfoEditMode(bool isEditing)
    {
        _isPersonalInfoEditing = isEditing;

        PersonalNameTextBox.IsEnabled = isEditing;
        PersonalPhoneTextBox.IsEnabled = isEditing;
        PersonalBirthdayPicker.IsEnabled = isEditing;
        PersonalSavingsTextBox.IsEnabled = isEditing;

        EditPersonalInfoButton.Content = isEditing ? "取消编辑" : "编辑个人信息";
        EditPersonalInfoButton.IsEnabled = true;
        SavePersonalInfoButton.IsEnabled = true;
        SavePersonalInfoButton.Opacity = isEditing ? 1.0 : 0.92;
    }

    private void EditPersonalInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPersonalInfoEditing)
        {
            _personalInfoEditBackup = ClonePersonalInfo(_appData.PersonalInfo);
            SetPersonalInfoEditMode(true);
            PersonalNameTextBox.Focus();
            PersonalNameTextBox.SelectAll();
            return;
        }

        if (_personalInfoEditBackup != null)
        {
            RestorePersonalInfo(_personalInfoEditBackup);
            LoadPersonalInfo();
        }

        _personalInfoEditBackup = null;
        SetPersonalInfoEditMode(false);
    }

    private void SavePersonalInfoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_isPersonalInfoEditing)
            {
                MessageBox.Show("当前还没有进入编辑状态。请先点击“编辑个人信息”后再保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveAppData();
            MessageBox.Show("个人信息已保存。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            _personalInfoEditBackup = null;
            SetPersonalInfoEditMode(false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MindMapButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mindMapWindow = new MindMapWindow(_appData.MindMapRoot);
            var result = mindMapWindow.ShowDialog();
            
            if (result == true)
            {
                SaveAppData();
                MessageBox.Show("思维导图已保存。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开思维导图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "所有兼容备份 (*.backup;*.diary)|*.backup;*.diary|新版备份 (*.backup)|*.backup|旧版备份 (*.diary)|*.diary|所有文件 (*.*)|*.*",
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
                        $"检测到可导入的备份文件。\n\n是否导入这些数据？\n\n注意：这会覆盖当前所有数据。", 
                        "确认导入", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _appData = restoredData;
                            
                            // 闁插秵鏌婇幒鎺戠碍閺佺増宓?
                            _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                            _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                            _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                            _appData.CheckIns = _appData.CheckIns.OrderByDescending(c => c.Date).ToList();
                            
                            SaveAppData();
                            
                            // 閸掗攱鏌婇悾宀勬桨
                            InitializeUI();
                            
                            MessageBox.Show("数据导入成功。", "成功");
                        }
                        catch (Exception saveEx)
                        {
                            MessageBox.Show($"数据导入成功，但保存到本地文件失败：{saveEx.Message}\n\n数据会在程序重启后丢失，请检查程序目录的写入权限。", "部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    #region 杈呭姪鏂规硶

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

    #region 鎵撳崱椤圭洰绠＄悊

    private void CheckInProjectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (CheckInProjectListBox != null && CheckInProjectListBox.SelectedItem is CheckInProject project)
            {
                UpdateSelectedProjectData(project);
            }
        }
        catch (Exception ex)
        {
            Log($"打卡项目选择变更异常: {ex.Message}");
        }
    }

    private void AddCheckInProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var newProject = new CheckInProject
            {
                Id = Guid.NewGuid().ToString(),
                Name = "新项目",
                Type = "习惯",
                CreatedAt = DateTime.Now
            };

            _appData.CheckInProjects.Add(newProject);
            SaveAppData();
            RefreshCheckInProjectList();
            if (CheckInProjectListBox != null)
                CheckInProjectListBox.SelectedItem = newProject;
            MessageBox.Show("已添加新的打卡项目。", "成功");
        }
        catch (Exception ex)
        {
            Log($"添加打卡项目失败: {ex.Message}");
            MessageBox.Show($"添加失败：{ex.Message}", "错误");
        }
    }

    private void EditCheckInProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CheckInProjectListBox != null && CheckInProjectListBox.SelectedItem is CheckInProject project)
            {
                var editWindow = new CheckInProjectEditWindow(project);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    SaveAppData();
                    RefreshCheckInProjectList();
                    MessageBox.Show("项目编辑成功。", "成功");
                }
            }
            else
            {
                MessageBox.Show("请先选择一个项目。", "提示");
            }
        }
        catch (Exception ex)
        {
            Log($"编辑打卡项目失败: {ex.Message}");
            MessageBox.Show($"编辑失败：{ex.Message}", "错误");
        }
    }

    private void DeleteCheckInProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CheckInProjectListBox == null || CheckInProjectListBox.SelectedItem is not CheckInProject project)
            {
                MessageBox.Show("请先选择一个项目。", "提示");
                return;
            }

            var relatedCheckInCount = _appData.CheckIns.Count(c => c.ProjectId == project.Id);
            var confirmMessage = relatedCheckInCount > 0
                ? $"确定要删除打卡项目“{project.Name}”吗？\n\n这会同时删除关联的 {relatedCheckInCount} 条打卡记录，且无法恢复。"
                : $"确定要删除打卡项目“{project.Name}”吗？\n\n删除后无法恢复。";

            var result = MessageBox.Show(confirmMessage, "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _appData.CheckInProjects.RemoveAll(p => p.Id == project.Id);
            _appData.CheckIns.RemoveAll(c => c.ProjectId == project.Id);

            SaveAppData();
            RefreshCheckInProjectList();

            if (_appData.CheckInProjects.Count > 0)
            {
                CheckInProjectListBox.SelectedItem = _appData.CheckInProjects[0];
                UpdateSelectedProjectData(_appData.CheckInProjects[0]);
            }
            else
            {
                ClearSelectedProjectData();
            }

            MessageBox.Show("打卡项目已删除。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log($"删除打卡项目失败: {ex.Message}");
            MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CheckInTodayButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CheckInProjectListBox != null && CheckInProjectListBox.SelectedItem is CheckInProject project)
            {
                var todayCheckIn = _appData.CheckIns.FirstOrDefault(c => 
                    c.ProjectId == project.Id && c.Date.Date == DateTime.Today);

                if (todayCheckIn != null)
                {
                    MessageBox.Show("今天已经打过卡了。", "提示");
                    return;
                }

                // 寮瑰嚭鎵撳崱鏃ュ織绐楀彛
                var dialog = new CheckInDialog(_appData);
                dialog.Owner = this;
                if (dialog.ShowDialog() == true)
                {
                    var newCheckIn = new CheckInEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        ProjectId = project.Id,
                        Type = project.Type,
                        Value = "瀹屾垚",
                        Date = DateTime.Today,
                        Notes = dialog.Notes,
                        Tags = dialog.Tags,
                        Photos = dialog.PhotoPaths,
                        CreatedAt = DateTime.Now
                    };

                    _appData.CheckIns.Add(newCheckIn);
                    SaveAppData();
                    UpdateSelectedProjectData(project);

                    MessageBox.Show("打卡成功。", "成功");
                }
            }
            else
            {
                MessageBox.Show("请先选择一个项目。", "提示");
            }
        }
        catch (Exception ex)
        {
            Log($"今日打卡失败: {ex.Message}");
            MessageBox.Show($"打卡失败：{ex.Message}", "错误");
        }
    }

    private void ViewCheckInLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CheckInProjectListBox != null && CheckInProjectListBox.SelectedItem is CheckInProject project)
            {
                var projectCheckIns = _appData.CheckIns
                    .Where(c => c.ProjectId == project.Id)
                    .ToList();

                var logWindow = new CheckInLogWindow(project, projectCheckIns);
                logWindow.Owner = this;
                logWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("请先选择一个项目。", "提示");
            }
        }
        catch (Exception ex)
        {
            Log($"查看打卡日志失败: {ex.Message}");
            MessageBox.Show($"查看日志失败：{ex.Message}", "错误");
        }
    }

    private void RefreshCheckInProjectList()
    {
        try
        {
            if (CheckInProjectListBox == null) return;

            CheckInProjectListBox.ItemsSource = null;
            CheckInProjectListBox.ItemsSource = _appData.CheckInProjects;
        }
        catch (Exception ex)
        {
            Log($"刷新打卡项目列表失败: {ex.Message}");
        }
    }

    private void ClearSelectedProjectData()
    {
        try
        {
            if (SelectedProjectTitleText != null)
                SelectedProjectTitleText.Text = "项目数据";

            if (ProjectTotalCountText != null)
                ProjectTotalCountText.Text = "0";

            if (ProjectCurrentStreakText != null)
                ProjectCurrentStreakText.Text = "0";

            if (MonthLongestStreakText != null)
                MonthLongestStreakText.Text = "0天";

            if (MonthCheckInCountText != null)
                MonthCheckInCountText.Text = "0次";

            if (MonthSuccessRateText != null)
                MonthSuccessRateText.Text = "0%";

            UpdateProjectCheckInCalendar(new List<CheckInEntry>());
        }
        catch (Exception ex)
        {
            Log($"清空项目数据显示失败: {ex.Message}");
        }
    }

    private void UpdateSelectedProjectData(CheckInProject project)
    {
        try
        {
            if (SelectedProjectTitleText != null)
                SelectedProjectTitleText.Text = $"项目数据：{project.Name}";

            var projectCheckIns = _appData.CheckIns
                .Where(c => c.ProjectId == project.Id)
                .OrderByDescending(c => c.Date)
                .ToList();

            if (ProjectTotalCountText != null)
                ProjectTotalCountText.Text = projectCheckIns.Count.ToString();

            var currentStreak = CalculateCheckInCurrentStreak(projectCheckIns);
            if (ProjectCurrentStreakText != null)
                ProjectCurrentStreakText.Text = currentStreak.ToString();

            UpdateProjectCheckInCalendar(projectCheckIns);
            // UpdateCountdownDisplay(); // 鍊掓暟鏃ョ幇鍦ㄦ槸鐙珛鐨勶紝涓嶉渶瑕侀殢椤圭洰鏇存柊
            UpdateMonthlyStats(project);
        }
        catch (Exception ex)
        {
            Log($"更新项目数据显示失败: {ex.Message}");
        }
    }

    private int CalculateCheckInCurrentStreak(List<CheckInEntry> checkIns)
    {
        if (!checkIns.Any()) return 0;

        var orderedCheckIns = checkIns.OrderByDescending(c => c.Date).ToList();
        var streak = 0;
        var currentDate = DateTime.Today;

        foreach (var checkIn in orderedCheckIns)
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

    private void UpdateCountdownDisplay()
    {
        try
        {
            // 鏁版嵁杩佺Щ锛氬皢鏃х殑鍗曟暟 Countdown 杩佺Щ鍒?Countdowns 鍒楄〃
            if (_appData.Countdowns == null) _appData.Countdowns = new List<CountdownItem>();
            
            // 妫€鏌ユ棫鏁版嵁鏄惁瀛樺湪
            #pragma warning disable CS0612 // 绫诲瀷鎴栨垚鍛樺凡杩囨椂
            if (_appData.Countdown != null)
            {
                _appData.Countdowns.Add(_appData.Countdown);
                _appData.Countdown = null;
                SaveAppData();
            }
            #pragma warning restore CS0612

            if (CountdownContainer == null) return;
            CountdownContainer.Children.Clear();

            // 鍥哄畾鏄剧ず4涓Ы浣?
            for (int i = 0; i < 4; i++)
            {
                var border = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 0, i == 3 ? 0 : 20, 0), // 鏈€鍚庝竴涓笉闇€瑕佸彸杈硅窛
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA")),
                    MinHeight = 160 // 纭繚楂樺害涓€鑷?
                };

                if (i < _appData.Countdowns.Count)
                {
                    var item = _appData.Countdowns[i];
                    
                    // 鐜版湁鍊掓暟鏃ュ唴瀹?
                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // 1. 椤堕儴锛氬浘鏍?+ 鎿嶄綔鎸夐挳
                    var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                    headerGrid.Children.Add(new TextBlock 
                    { 
                        Text = "倒数日", 
                        FontSize = 14, 
                        FontWeight = FontWeights.Bold, 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")), 
                        VerticalAlignment = VerticalAlignment.Center 
                    });
                    
                    var headerButtons = new StackPanel 
                    { 
                        Orientation = Orientation.Horizontal, 
                        HorizontalAlignment = HorizontalAlignment.Right, 
                        VerticalAlignment = VerticalAlignment.Center 
                    };
                    
                    // 鍒犻櫎鎸夐挳
                    var deleteBtn = new Button { 
                        Content = "删除", 
                        Background = Brushes.Transparent, 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7675")),
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 8, 0),
                        ToolTip = "删除倒数日",
                        Tag = item
                    };
                    deleteBtn.Click += DeleteCountdown_Click;
                    headerButtons.Children.Add(deleteBtn);

                    // 缂栬緫鎸夐挳
                    var editBtn = new Button { 
                        Content = "设置", 
                        Background = Brushes.Transparent, 
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C5CE7")),
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        ToolTip = "修改倒数日",
                        Tag = item
                    };
                    editBtn.Click += EditCountdown_Click;
                    headerButtons.Children.Add(editBtn);
                    
                    headerGrid.Children.Add(headerButtons);
                    Grid.SetRow(headerGrid, 0);
                    grid.Children.Add(headerGrid);

                    // 2. 鏍囬
                    var titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
                    titlePanel.Children.Add(new TextBlock { Text = "标题：", FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")), Margin = new Thickness(0, 0, 0, 3) });
                    titlePanel.Children.Add(new TextBlock { Text = item.Title, FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436")), FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
                    Grid.SetRow(titlePanel, 1);
                    grid.Children.Add(titlePanel);

                    // 3. 鍒版湡鏃堕棿
                    var datePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
                    datePanel.Children.Add(new TextBlock { Text = "到期时间：", FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")), Margin = new Thickness(0, 0, 0, 3) });
                    datePanel.Children.Add(new TextBlock { Text = item.TargetDate.ToString("yyyy年MM月dd日"), FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436")), FontWeight = FontWeights.SemiBold });
                    Grid.SetRow(datePanel, 2);
                    grid.Children.Add(datePanel);

                    // 4. 鍓╀綑鏃堕棿
                    var remainDays = (item.TargetDate.Date - DateTime.Today).Days;
                    var remainText = "";
                    if (remainDays > 0) remainText = $"还有 {remainDays} 天";
                    else if (remainDays == 0) remainText = "就在今天!";
                    else remainText = $"已过期 {Math.Abs(remainDays)} 天";

                    var remainPanel = new StackPanel();
                    remainPanel.Children.Add(new TextBlock { Text = "剩余时间：", FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")), Margin = new Thickness(0, 0, 0, 3) });
                    remainPanel.Children.Add(new TextBlock { Text = remainText, FontSize = 14, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C5CE7")), FontWeight = FontWeights.Bold });
                    Grid.SetRow(remainPanel, 3);
                    grid.Children.Add(remainPanel);

                    border.Child = grid;
                }
                else
                {
                    // 绌烘Ы浣?- 鏄剧ず娣诲姞鎸夐挳
                    border.Background = Brushes.White; // 鐧借壊鑳屾櫙
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE6E9"));
                    border.BorderThickness = new Thickness(2);
                    
                    var emptyGrid = new Grid();
                    var addBtn = new Button {
                        Content = "+",
                        FontSize = 40,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE6E9")),
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        ToolTip = "添加倒数日",
                        Width = 100,
                        Height = 100
                    };
                    addBtn.Click += AddCountdown_Click;
                    emptyGrid.Children.Add(addBtn);
                    border.Child = emptyGrid;
                }
                
                CountdownContainer.Children.Add(border);
            }
        }
        catch (Exception ex)
        {
            Log($"更新倒数日显示失败: {ex.Message}");
        }
    }

    private void AddCountdown_Click(object sender, RoutedEventArgs e)
    {
        if (_appData.Countdowns.Count >= 4)
        {
            MessageBox.Show("最多只能添加 4 个倒数日。", "提示");
            return;
        }
        ShowCountdownDialog(null);
    }

    private void EditCountdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CountdownItem item)
        {
            ShowCountdownDialog(item);
        }
    }

    private void DeleteCountdown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CountdownItem item)
        {
            if (MessageBox.Show("确定要删除这个倒数日吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _appData.Countdowns.Remove(item);
                SaveAppData();
                UpdateCountdownDisplay();
                Log("删除了倒数日");
            }
        }
    }

    private void ShowCountdownDialog(CountdownItem? itemToEdit)
    {
        try
        {
            bool isNew = itemToEdit == null;
            
            // 鍒涘缓鍊掓暟鏃ヨ缃獥鍙?
            var dialog = new Window
            {
                Title = isNew ? "新建倒数日" : "编辑倒数日",
                Width = 350,
                Height = 280, 
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Colors.White)
            };

            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            // 鏍囬杈撳叆
            mainPanel.Children.Add(new TextBlock 
            { 
                Text = "标题：", 
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436"))
            });

            var titleBox = new TextBox
            {
                Text = itemToEdit?.Title ?? "重要日子",
                Margin = new Thickness(0, 0, 0, 15),
                Height = 30,
                FontSize = 14,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            mainPanel.Children.Add(titleBox);

            // 鏃ユ湡閫夋嫨
            mainPanel.Children.Add(new TextBlock 
            { 
                Text = "目标日期：", 
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3436"))
            });

            var datePicker = new DatePicker
            {
                SelectedDate = itemToEdit?.TargetDate ?? DateTime.Today.AddDays(1),
                Margin = new Thickness(0, 0, 0, 20),
                Height = 35, // Match previous height
                FontSize = 14
            };
            mainPanel.Children.Add(datePicker);

            // 鎸夐挳鍖哄煙
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            
            var okBtn = new Button 
            { 
                Content = "确定", 
                Width = 90, 
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0), 
                IsDefault = true,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C5CE7")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            
            // 娣诲姞鍦嗚鏍峰紡
            var okStyle = new Style(typeof(Border));
            okStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(6)));
            okBtn.Resources.Add(typeof(Border), okStyle);

            var cancelBtn = new Button 
            { 
                Content = "取消", 
                Width = 90, 
                Height = 35,
                IsCancel = true,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DFE6E9")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#636E72")),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = Cursors.Hand
            };
            
            // 娣诲姞鍦嗚鏍峰紡
            var cancelStyle = new Style(typeof(Border));
            cancelStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(6)));
            cancelBtn.Resources.Add(typeof(Border), cancelStyle);
            
            okBtn.Click += (s, args) => { 
                if (string.IsNullOrWhiteSpace(titleBox.Text))
                {
                    MessageBox.Show("请输入标题。", "提示");
                    return;
                }
                if (datePicker.SelectedDate == null)
                {
                    MessageBox.Show("请选择日期", "提示");
                    return;
                }
                dialog.DialogResult = true; 
                dialog.Close(); 
            };
            
            cancelBtn.Click += (s, args) => dialog.Close();

            btnPanel.Children.Add(okBtn);
            btnPanel.Children.Add(cancelBtn);
            mainPanel.Children.Add(btnPanel);

            dialog.Content = mainPanel;

            if (dialog.ShowDialog() == true)
            {
                if (isNew)
                {
                    var newItem = new CountdownItem
                    {
                        Title = titleBox.Text.Trim(),
                        TargetDate = datePicker.SelectedDate.Value,
                        CreatedAt = DateTime.Now
                    };
                    _appData.Countdowns.Add(newItem);
                    Log($"创建了新倒数日: {newItem.Title}");
                }
                else
                {
                    if (itemToEdit != null)
                    {
                        itemToEdit.Title = titleBox.Text.Trim();
                        itemToEdit.TargetDate = datePicker.SelectedDate.Value;
                        Log($"更新了倒数日: {itemToEdit.Title}");
                    }
                }
                SaveAppData();
                UpdateCountdownDisplay();
            }
        }
        catch (Exception ex)
        {
            Log($"设置倒数日失败: {ex.Message}");
            MessageBox.Show($"设置倒数日失败: {ex.Message}", "错误");
        }
    }

    private void UpdateMonthlyStats(CheckInProject project)
    {
        try
        {
            var projectCheckIns = _appData.CheckIns
                .Where(c => c.ProjectId == project.Id)
                .ToList();

            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var monthCheckIns = projectCheckIns
                .Where(c => c.Date >= monthStart && c.Date <= monthEnd)
                .ToList();

            var longestMonthStreak = CalculateCheckInLongestStreak(monthCheckIns);
            if (MonthLongestStreakText != null)
                MonthLongestStreakText.Text = $"{longestMonthStreak}天";

            if (MonthCheckInCountText != null)
                MonthCheckInCountText.Text = $"{monthCheckIns.Count}次";

            var daysInMonth = (int)(monthEnd - monthStart).TotalDays + 1;
            var completionRate = daysInMonth > 0 ? (monthCheckIns.Count * 100.0 / daysInMonth) : 0;
            if (MonthSuccessRateText != null)
                MonthSuccessRateText.Text = $"{completionRate:F0}%";
        }
        catch (Exception ex)
        {
            Log($"更新月度统计失败: {ex.Message}");
        }
    }

    private void UpdateProjectCheckInCalendar(List<CheckInEntry> projectCheckIns)
    {
        try
        {
            if (ProjectCheckInCalendarGrid == null) return;

            ProjectCheckInCalendarGrid.Children.Clear();

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var dayNames = new[] { "日", "一", "二", "三", "四", "五", "六" };
            foreach (var dayName in dayNames)
            {
                var dayLabel = new TextBlock
                {
                    Text = dayName,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(2)
                };
                ProjectCheckInCalendarGrid.Children.Add(dayLabel);
            }

            var firstDayOfWeek = (int)monthStart.DayOfWeek;
            for (int i = 0; i < firstDayOfWeek; i++)
            {
                var emptyBlock = new TextBlock { Margin = new Thickness(2) };
                ProjectCheckInCalendarGrid.Children.Add(emptyBlock);
            }

            for (int day = 1; day <= monthEnd.Day; day++)
            {
                var currentDate = new DateTime(now.Year, now.Month, day);
                var hasCheckIn = projectCheckIns.Any(c => c.Date.Date == currentDate.Date);

                var dayBlock = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(4),
                    Background = hasCheckIn ? new SolidColorBrush(Color.FromArgb(255, 108, 92, 231)) : new SolidColorBrush(Color.FromArgb(255, 240, 243, 248)),
                    Margin = new Thickness(2),
                    Child = new TextBlock
                    {
                        Text = day.ToString(),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 10,
                        Foreground = hasCheckIn ? Brushes.White : Brushes.Black
                    }
                };
                ProjectCheckInCalendarGrid.Children.Add(dayBlock);
            }
        }
        catch (Exception ex)
        {
            Log($"更新项目打卡日历失败: {ex.Message}");
        }
    }

    private int CalculateCheckInLongestStreak(List<CheckInEntry> checkIns)
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

    #endregion
}






