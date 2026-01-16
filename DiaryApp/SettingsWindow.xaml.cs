using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Globalization;

namespace DiaryApp;

public partial class SettingsWindow : Window
{
    private readonly AppData _appData;

    public SettingsWindow()
    {
        InitializeComponent();
        
        // 支持窗口拖动
        this.MouseLeftButtonDown += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        
        _appData = new AppData();
        // 设置数据上下文
        DataContext = new SettingsViewModel(_appData);
    }

    public SettingsWindow(AppData appData)
    {
        InitializeComponent();
        
        // 支持窗口拖动
        this.MouseLeftButtonDown += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        
        _appData = appData;
        // 设置数据上下文
        DataContext = new SettingsViewModel(appData);
        
        // 初始化时间选择器
        InitializeTimePickers();
        
        // 添加选择变更事件处理
        HourComboBox.SelectionChanged += TimeComboBox_SelectionChanged;
        MinuteComboBox.SelectionChanged += TimeComboBox_SelectionChanged;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    // 初始化时间选择器
    private void InitializeTimePickers()
    {
        // 设置小时选择器
        int hour = _appData.ReminderSetting.ReminderTime.HasValue ? _appData.ReminderSetting.ReminderTime.Value.Hours : 20;
        HourComboBox.SelectedIndex = hour;
        
        // 设置分钟选择器
        int minute = _appData.ReminderSetting.ReminderTime.HasValue ? _appData.ReminderSetting.ReminderTime.Value.Minutes : 0;
        int minuteIndex = Array.IndexOf(new int[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 }, minute);
        MinuteComboBox.SelectedIndex = minuteIndex >= 0 ? minuteIndex : 0;
    }
    
    // 时间选择变更事件处理
    private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HourComboBox.SelectedItem != null && MinuteComboBox.SelectedItem != null)
        {
            // 获取选择的小时和分钟
            int hour = int.Parse((HourComboBox.SelectedItem as ComboBoxItem).Content.ToString());
            int minute = int.Parse((MinuteComboBox.SelectedItem as ComboBoxItem).Content.ToString());
            
            // 更新提醒设置
            _appData.ReminderSetting.ReminderTime = new TimeSpan(hour, minute, 0);
        }
    }
}

// 设置窗口的数据模型
public class SettingsViewModel
{
    public string Version { get; }
    public string BuildDate { get; }
    public string BuildTime { get; }
    public string CurrentTime { get; }
    public ReminderSetting ReminderSetting { get; set; } = new ReminderSetting();

    public SettingsViewModel()
    {
        Version = AppVersion.VERSION;
        BuildDate = AppVersion.BUILD_DATE;
        BuildTime = AppVersion.BUILD_TIME;
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public SettingsViewModel(AppData appData) : this()
    {
        ReminderSetting = appData.ReminderSetting;
    }
}