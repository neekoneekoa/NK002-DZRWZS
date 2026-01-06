using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO;
using System;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace DiaryApp;

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
    public const string VERSION = "V0.1.1.1";
    public static readonly string BUILD_DATE = DateTime.Now.ToString("yyyy-MM-dd");
    public static readonly string BUILD_TIME = DateTime.Now.ToString("HH:mm");
}

public partial class MainWindow : Window
{
    // 统一应用数据
    private AppData _appData = new AppData();
    private const string DATA_FILE = "app_data.json";
    private const string LOG_FILE = "app_crash_log.txt";
    
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
            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine($"崩溃时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"程序版本: {AppVersion.VERSION} ({AppVersion.BUILD_DATE} {AppVersion.BUILD_TIME})");
            sb.AppendLine($"操作系统: {Environment.OSVersion}");
            sb.AppendLine($".NET版本: {Environment.Version}");
            sb.AppendLine($"机器名: {Environment.MachineName}");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            sb.AppendLine($"错误信息: {message}");
            if (ex != null)
            {
                sb.AppendLine("────────────────────────────────────────────────────────────");
                sb.AppendLine("异常详情:");
                sb.AppendLine($"类型: {ex.GetType().FullName}");
                sb.AppendLine($"消息: {ex.Message}");
                sb.AppendLine($"源: {ex.Source}");
                sb.AppendLine($"堆栈跟踪:");
                sb.AppendLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    sb.AppendLine("────────────────────────────────────────────────────────────");
                    sb.AppendLine("内部异常:");
                    sb.AppendLine($"类型: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"消息: {ex.InnerException.Message}");
                    sb.AppendLine($"堆栈跟踪:");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }
            }
            sb.AppendLine("════════════════════════════════════════════════════════════");
            sb.AppendLine();
            File.AppendAllText(logPath, sb.ToString());
        }
        catch { /* 忽略日志错误 */ }
    }
    
    // 全局未处理异常处理 (非UI线程)
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
    
    // 当前选中的日记条目（用于编辑）
    private DiaryEntry? _currentDiaryEntry;
    
    // 当前选中的任务条目（用于编辑）
    private TaskEntry? _currentTaskEntry;
    
    // 当前选中的打卡记录（用于编辑）
    private CheckInEntry? _currentCheckInEntry;

    // 当前查看的周
    private DateTime _currentWeekStart = GetWeekStart(DateTime.Today);

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
            // 支持窗口拖动 (因为设置了 WindowStyle="None")
            this.MouseLeftButtonDown += (s, e) =>
            {
                // 检查鼠标按钮是否确实按下
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
            
            Log("开始调用InitializeUI()");
            InitializeUI();
            Log("InitializeUI()完成");
            
            Log("开始调用LoadAppData()");
            LoadAppData();
            Log("LoadAppData()完成");
            
            Log("开始设置默认选项卡");
            // 默认显示日记板块
            MainTabControl.SelectedIndex = 0;
            Log("选项卡设置完成");
            
            Log("MainWindow构造函数成功完成 - 窗口应该已显示");
        }
        catch (Exception ex)
        {
            Log($"MainWindow构造函数异常: {ex.Message}");
            Log($"异常堆栈: {ex.StackTrace}");
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                Log($"内部异常: {innerEx.Message}");
                Log($"内部异常堆栈: {innerEx.StackTrace}");
                innerEx = innerEx.InnerException;
            }
            MessageBox.Show($"初始化错误: {ex.Message}\n\n详细信息: {ex.StackTrace}\n\n请查看 startup_log.txt 获取更多调试信息", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
    
    private void InitializeUI()
    {
        // 设置初始窗口大小为显示器的70%
        SetWindowSizeTo70Percent();
        
        // 初始化日记时间轴
        RefreshDiaryTimeline();
        
        // 设置默认日期时间显示
        DateLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        
        // 初始化周视图
        UpdateWeekDisplay();
        
        // 初始化打卡统计
        UpdateCheckInStats();
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
                    
                    // 重新排序数据
                    _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                    _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                    _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                    _appData.CheckIns = _appData.CheckIns.OrderByDescending(c => c.Date).ToList();
                }
            }
            catch { /* 忽略加载错误 */ }
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
        Application.Current.Shutdown();
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
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    #endregion

    #region 日记模块事件

    private void NewDiaryButton_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new DiaryEditWindow();
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            _appData.Diaries.Add(editWindow.ResultEntry);
            SaveAppData();
            RefreshDiaryTimeline();
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

    private void RefreshDiaryMonthDates()
    {
        DiaryMonthDatesPanel.Children.Clear();
        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var datesWithDiaries = _appData.Diaries
            .Where(d => d.CreatedAt.Date >= firstDayOfMonth && d.CreatedAt.Date <= lastDayOfMonth)
            .Select(d => d.CreatedAt.Date)
            .Distinct()
            .ToHashSet();

        for (var date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
        {
            var hasDiary = datesWithDiaries.Contains(date);
            var button = new Button
            {
                Content = $"{date.Day}",
                Height = 30,
                Margin = new Thickness(2),
                Padding = new Thickness(8, 0, 8, 0),
                Background = hasDiary ? (System.Windows.Media.Brush?)AppBrushes.A29BFE : System.Windows.Media.Brushes.Transparent,
                Foreground = hasDiary ? System.Windows.Media.Brushes.White : AppBrushes._2D3436,
                BorderThickness = hasDiary ? new Thickness(0) : new Thickness(1),
                BorderBrush = AppBrushes.DFE6E9,
                FontSize = 12
            };
            button.Click += (s, e) => JumpToDate(date);
            DiaryMonthDatesPanel.Children.Add(button);
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
            var panel = FindDiaryEntryPanel(targetEntry.Id);
            if (panel != null)
            {
                ToggleDiaryEntry(targetEntry.Id, true);
                panel.BringIntoView();
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
        
        var filteredDiaries = _appData.Diaries.AsEnumerable();
        
        if (!string.IsNullOrEmpty(searchText))
        {
            filteredDiaries = filteredDiaries.Where(d => d.SearchableText.Contains(searchText));
        }
        
        if (!string.IsNullOrEmpty(tagFilter))
        {
            filteredDiaries = filteredDiaries.Where(d => d.Tags.Any(t => t.ToLower().Contains(tagFilter)));
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
        
        var currentMonth = "";
        foreach (var entry in sortedDiaries)
        {
            var entryMonth = entry.CreatedAt.ToString("yyyy年M月");
            if (entryMonth != currentMonth)
            {
                currentMonth = entryMonth;
                var monthHeader = new TextBlock
                {
                    Text = entryMonth,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = AppBrushes.A29BFE,
                    Margin = new Thickness(0, 15, 0, 10)
                };
                DiaryTimelinePanel.Children.Add(monthHeader);
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
        
        var infoStackPanel = new StackPanel();
        
        var titleText = new TextBlock
        {
            Text = entry.Title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = AppBrushes._2D3436,
            TextWrapping = TextWrapping.Wrap
        };
        infoStackPanel.Children.Add(titleText);
        
        var timeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 5, 0, 0)
        };
        var timeText = new TextBlock
        {
            Text = entry.TimeOnly,
            FontSize = 12,
            Foreground = AppBrushes.A29BFE,
            Margin = new Thickness(0, 0, 10, 0)
        };
        timeRow.Children.Add(timeText);
        
        if (entry.Tags.Count > 0)
        {
            var tagsText = new TextBlock
            {
                Text = string.Join(" ", entry.Tags.Select(t => $"#{t}")),
                FontSize = 11,
                Foreground = AppBrushes._636E72,
                TextWrapping = TextWrapping.Wrap
            };
            timeRow.Children.Add(tagsText);
        }
        infoStackPanel.Children.Add(timeRow);
        
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
        collapsedPanel.Children.Add(headerBorder);
        mainStackPanel.Children.Add(collapsedPanel);
        
        var expandedPanel = new StackPanel
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
        var contentText = new TextBlock
        {
            Text = entry.ContentPreview,
            FontSize = 13,
            Foreground = AppBrushes._636E72,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 24
        };
        contentBorder.Child = contentText;
        expandedPanel.Children.Add(contentBorder);
        
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
        expandedPanel.Children.Add(buttonPanel);
        
        mainStackPanel.Children.Add(expandedPanel);
        
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
            ToggleDiaryEntry(entryId, true);
        }
    }

    private void ToggleDiaryEntry(string entryId, bool expand)
    {
        foreach (var child in DiaryTimelinePanel.Children)
        {
            if (child is Border border && border.Tag?.ToString() == entryId)
            {
                if (border.Child is StackPanel mainPanel && mainPanel.Children.Count >= 2)
                {
                    if (mainPanel.Children[0] is StackPanel collapsedPanel)
                    {
                        collapsedPanel.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
                    }
                    if (mainPanel.Children[1] is StackPanel expandedPanel)
                    {
                        expandedPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                break;
            }
        }
    }

    private void EditDiaryEntry(DiaryEntry entry)
    {
        var editWindow = new DiaryEditWindow(entry);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.ResultEntry != null)
        {
            var index = _appData.Diaries.FindIndex(d => d.Id == entry.Id);
            if (index >= 0)
            {
                _appData.Diaries[index] = editWindow.ResultEntry;
                SaveAppData();
                RefreshDiaryTimeline();
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
        }
    }

    #endregion

    #region 任务模块事件

    private void NewTaskButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTaskEntry = null;
        TaskListBox.SelectedItem = null;
        TaskTitleTextBox.Text = "";
        TaskContentTextBox.Text = "";
        TaskPriorityCombo.SelectedIndex = 1; // 默认中优先级
        TaskLevelCombo.SelectedIndex = 0;    // 默认1级
        TaskStatusCombo.SelectedIndex = 0;   // 默认待完成
        TaskCompletedDatePicker.SelectedDate = null;
        SubTasksPanel.Children.Clear();
        AddDefaultSubTask();
        TaskTitleTextBox.Focus();
    }

    private void TaskListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskListBox.SelectedItem is TaskEntry entry)
        {
            _currentTaskEntry = entry;
            TaskTitleTextBox.Text = entry.Title;
            TaskContentTextBox.Text = entry.Content;
            TaskPriorityCombo.SelectedIndex = entry.Priority - 1;
            TaskLevelCombo.SelectedIndex = entry.Level - 1;
            TaskStatusCombo.SelectedIndex = (int)entry.Status;
            TaskCompletedDatePicker.SelectedDate = entry.CompletedAt;
            
            // 加载子任务
            SubTasksPanel.Children.Clear();
            foreach (var subTask in entry.SubTasks)
            {
                AddSubTaskToPanel(subTask);
            }
        }
    }

    private void AddSubTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var subTask = new SubTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = "新子任务",
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };
        AddSubTaskToPanel(subTask);
    }

    private void AddDefaultSubTask()
    {
        var subTask = new SubTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = "子任务1",
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };
        AddSubTaskToPanel(subTask);
    }

    private void AddSubTaskToPanel(SubTask subTask)
    {
        var subTaskPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var checkBox = new CheckBox
        {
            IsChecked = subTask.IsCompleted,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var textBox = new TextBox
        {
            Text = subTask.Title,
            Width = 200,
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.White,
            Padding = new Thickness(5)
        };

        subTaskPanel.Children.Add(checkBox);
        subTaskPanel.Children.Add(textBox);
        SubTasksPanel.Children.Add(subTaskPanel);
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
        var weekEnd = _currentWeekStart.AddDays(6);
        CurrentWeekText.Text = $"{_currentWeekStart.Year}年第{GetWeekNumber(_currentWeekStart)}周 ({_currentWeekStart:MM-dd} ~ {weekEnd:MM-dd})";
    }

    private void AddTimeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        // 创建新的时间记录
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
        // 获取当前周的时间记录
        var weekRecords = _appData.TimeRecords
            .Where(t => t.Date.Date >= _currentWeekStart.Date && t.Date.Date <= _currentWeekStart.AddDays(6).Date)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.StartTime)
            .ToList();

        // 清除现有的时间块
        var timeGrid = FindName("TimeGrid") as Grid;
        if (timeGrid != null)
        {
            // 保存时间标签和日期标题
            var timeLabels = new List<UIElement>();
            var dateHeaders = new List<UIElement>();
            
            for (int i = 0; i < timeGrid.Children.Count; i++)
            {
                var child = timeGrid.Children[i];
                var row = Grid.GetRow(child);
                var col = Grid.GetColumn(child);
                
                // 保存时间标签（第0列）
                if (col == 0 && child is TextBlock)
                {
                    timeLabels.Add(child);
                }
                // 保存日期标题（第0行）
                else if (row == 0 && child is TextBlock)
                {
                    dateHeaders.Add(child);
                }
            }
            
            // 清除所有子元素
            timeGrid.Children.Clear();
            
            // 重新添加时间标签和日期标题
            foreach (var label in timeLabels)
            {
                timeGrid.Children.Add(label);
            }
            foreach (var header in dateHeaders)
            {
                timeGrid.Children.Add(header);
            }
        }

        // 重新绘制网格线
        if (timeGrid != null)
        {
            DrawGridLines(timeGrid);
        
            // 绘制时间记录
            DrawTimeRecords(timeGrid, weekRecords);
        }
    }
    
    private void DrawGridLines(Grid timeGrid)
    {
        if (timeGrid == null) return;
        
        // 绘制时间块网格线
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
                Grid.SetRow(border, row + 1); // +1 是因为第0行是日期标题
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
            // 计算星期几（0=周一, 6=周日）
            int dayOfWeek = (int)record.Date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // 将周日从0转换为7
            dayOfWeek -= 1; // 转换为0-6的索引
            
            // 计算开始时间行号（08:00-19:00，共12小时）
            int startHour = record.StartTime.Hours;
            if (startHour < 8 || startHour >= 19) continue; // 只显示08:00-19:00的记录
            
            int startRow = startHour - 8;
            
            // 计算结束时间行号
            int endHour = record.EndTime.Hours;
            if (endHour <= 8) continue;
            if (endHour > 19) endHour = 19;
            
            int endRow = endHour - 8;
            int rowSpan = endRow - startRow;
            
            if (rowSpan < 1) rowSpan = 1;
            
            // 创建时间块
            var timeBlock = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(150, 108, 92, 231)), // 半透明紫色
                BorderBrush = Brushes.DarkSlateBlue,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand
            };
            
            // 添加点击事件
            timeBlock.MouseLeftButtonDown += (s, e) => EditTimeRecord(record);
            
            // 创建内容面板
            var contentPanel = new StackPanel
            {
                Margin = new Thickness(5),
                Background = Brushes.Transparent
            };
            
            // 添加活动名称
            var activityText = new TextBlock
            {
                Text = record.Activity,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            
            // 添加时间范围
            var timeText = new TextBlock
            {
                Text = $"{record.StartTime:HH:mm} - {record.EndTime:HH:mm}",
                FontSize = 10,
                Foreground = Brushes.LightGray
            };
            
            contentPanel.Children.Add(activityText);
            contentPanel.Children.Add(timeText);
            
            timeBlock.Child = contentPanel;
            
            // 设置位置和大小
            Grid.SetRow(timeBlock, startRow + 1); // +1 是因为第0行是日期标题
            Grid.SetColumn(timeBlock, dayOfWeek + 1); // +1 是因为第0列是时间标签
            Grid.SetRowSpan(timeBlock, rowSpan);
            Grid.SetColumnSpan(timeBlock, 1);
            
            // 添加到网格
            timeGrid.Children.Add(timeBlock);
        }
    }
    
    private void EditTimeRecord(TimeRecordEntry record)
    {
        var editWindow = new TimeRecordEditWindow(record);
        var result = editWindow.ShowDialog();
        
        if (result == true)
        {
            // 保存数据
            SaveAppData();
            // 更新显示
            UpdateTimeRecordDisplay();
        }
        else if (result == null)
        {
            // 删除记录
            _appData.TimeRecords.Remove(record);
            SaveAppData();
            UpdateTimeRecordDisplay();
        }
    }

    #endregion

    #region 打卡模块事件

    private void CheckInListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CheckInListBox.SelectedItem is CheckInEntry entry)
        {
            _currentCheckInEntry = entry;
            CheckInValueTextBox.Text = entry.Value;
            // 可以添加更多编辑字段
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
            var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "运动";
            var value = CheckInValueTextBox.Text.Trim();
            
            // 创建新的打卡记录
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
                // 更新当前选中的打卡记录
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
        
        foreach (var checkIn in checkIns)
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
        
        var totalDays = (checkIns.Max(c => c.Date) - checkIns.Min(c => c.Date)).Days + 1;
        var successDays = checkIns.Count;
        
        return (double)successDays / totalDays * 100;
    }

    private void UpdateCheckInList()
    {
        // 添加空值检查，防止初始化期间出现空引用异常
        if (CheckInTypeCombo == null || CheckInListBox == null) return;
        
        var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "运动";
        var filteredCheckIns = _appData.CheckIns
            .Where(c => c.Type == selectedType)
            .OrderByDescending(c => c.Date)
            .ToList();
        CheckInListBox.ItemsSource = filteredCheckIns;
    }

    private void UpdateCheckInStats()
    {
        // 添加空值检查，防止初始化期间出现空引用异常
        if (CheckInTypeCombo == null) return;
        
        var selectedType = (CheckInTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "运动";
        var typeCheckIns = _appData.CheckIns.Where(c => c.Type == selectedType).ToList();
        
        if (typeCheckIns.Any())
        {
            var currentStreak = CalculateStreak(selectedType, DateTime.Today);
            var longestStreak = CalculateLongestStreak(typeCheckIns);
            var successRate = CalculateSuccessRate(typeCheckIns);
            
            // 更新UI显示 - 这里需要添加对应的TextBlock到XAML
            // CurrentStreakText.Text = currentStreak.ToString();
            // LongestStreakText.Text = longestStreak.ToString();
            // SuccessRateText.Text = $"{successRate:F0}%";
        }
        else
        {
            // CurrentStreakText.Text = "0";
            // LongestStreakText.Text = "0";
            // SuccessRateText.Text = "0%";
        }
    }

    #endregion

    #region 统一保存和备份事件

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 根据当前选中的标签页保存对应模块的数据
            var selectedTab = MainTabControl.SelectedIndex;
            
            switch (selectedTab)
            {
                case 0: // 日记
                    break;
                case 1: // 任务
                    SaveTaskEntry();
                    break;
                case 2: // 时间记录
                    break;
                case 3: // 打卡
                    break;
            }

            SaveAppData();
            
            // 创建自动备份
            BackupManager.CreateAutoBackup(_appData, $"自动备份 - {DateTime.Now:yyyy-MM-dd HH:mm}");
            
            // 清理旧备份，保留最近10个
            BackupManager.CleanOldBackups(10);

            MessageBox.Show("数据已保存！已自动创建备份。", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}\n\n详细错误：{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveTaskEntry()
    {
        var title = TaskTitleTextBox.Text.Trim();
        var content = TaskContentTextBox.Text.Trim();

        if (string.IsNullOrEmpty(title))
        {
            return; // 空标题不保存
        }

        // 收集子任务
        var subTasks = new List<SubTask>();
        int index = 1;
        foreach (var child in SubTasksPanel.Children)
        {
            if (child is StackPanel subTaskPanel)
            {
                var checkBox = subTaskPanel.Children.OfType<CheckBox>().FirstOrDefault();
                var textBox = subTaskPanel.Children.OfType<TextBox>().FirstOrDefault();
                
                if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    subTasks.Add(new SubTask
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = textBox.Text.Trim(),
                        IsCompleted = checkBox?.IsChecked ?? false,
                        CreatedAt = DateTime.Now
                    });
                }
                index++;
            }
        }

        if (_currentTaskEntry == null)
        {
            // 新增
            var newEntry = new TaskEntry
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Content = content,
                Priority = TaskPriorityCombo.SelectedIndex + 1,
                Level = TaskLevelCombo.SelectedIndex + 1,
                Status = (TaskStatus)TaskStatusCombo.SelectedIndex,
                CompletedAt = TaskCompletedDatePicker.SelectedDate,
                SubTasks = subTasks,
                CreatedAt = DateTime.Now
            };
            _appData.Tasks.Insert(0, newEntry);
            _currentTaskEntry = newEntry;
        }
        else
        {
            // 更新
            _currentTaskEntry.Title = title;
            _currentTaskEntry.Content = content;
            _currentTaskEntry.Priority = TaskPriorityCombo.SelectedIndex + 1;
            _currentTaskEntry.Level = TaskLevelCombo.SelectedIndex + 1;
            _currentTaskEntry.Status = (TaskStatus)TaskStatusCombo.SelectedIndex;
            _currentTaskEntry.CompletedAt = TaskCompletedDatePicker.SelectedDate;
            _currentTaskEntry.SubTasks = subTasks;
        }

        TaskListBox.ItemsSource = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
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
            MessageBox.Show($"保存失败：{ex.Message}\n\n请检查程序目录的写权限。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        $"找到应用数据备份。\n\n是否要导入这些数据？\n\n注意：这将替换当前所有数据！", 
                        "确认导入", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _appData = restoredData;
                            
                            // 重新排序数据
                            _appData.Diaries = _appData.Diaries.OrderByDescending(d => d.CreatedAt).ToList();
                            _appData.Tasks = _appData.Tasks.OrderByDescending(t => t.CreatedAt).ToList();
                            _appData.TimeRecords = _appData.TimeRecords.OrderByDescending(t => t.Date).ThenByDescending(t => t.StartTime).ToList();
                            _appData.CheckIns = _appData.CheckIns.OrderByDescending(c => c.Date).ToList();
                            
                            SaveAppData();
                            
                            // 刷新界面
                            InitializeUI();
                            
                            MessageBox.Show("数据导入成功！", "成功");
                        }
                        catch (Exception saveEx)
                        {
                            MessageBox.Show($"数据导入成功，但保存到本地文件失败：{saveEx.Message}\n\n数据将在程序重启时丢失。\n请检查程序目录的写权限。", "部分成功", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            $"找到 {backups.Count} 个备份文件：\n\n{backupList}\n\n是否要打开备份文件夹管理？", 
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
