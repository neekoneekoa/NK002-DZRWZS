using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiaryApp;

public partial class SettingsWindow : Window
{
    private readonly AppData _appData;

    public SettingsWindow()
    {
        InitializeComponent();
        _appData = new AppData();
        InitializeWindow();
        DataContext = new SettingsViewModel(_appData);
    }

    public SettingsWindow(AppData appData)
    {
        InitializeComponent();
        _appData = appData;
        InitializeWindow();
        DataContext = new SettingsViewModel(appData);
        InitializeTimePickers();

        HourComboBox.SelectionChanged += TimeComboBox_SelectionChanged;
        MinuteComboBox.SelectionChanged += TimeComboBox_SelectionChanged;
    }

    private void InitializeWindow()
    {
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeTimePickers()
    {
        var hour = _appData.ReminderSetting.ReminderTime?.Hours ?? 20;
        HourComboBox.SelectedIndex = hour;

        var minute = _appData.ReminderSetting.ReminderTime?.Minutes ?? 0;
        var minuteIndex = Array.IndexOf(new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55 }, minute);
        MinuteComboBox.SelectedIndex = minuteIndex >= 0 ? minuteIndex : 0;
    }

    private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HourComboBox.SelectedItem is not ComboBoxItem hourItem ||
            MinuteComboBox.SelectedItem is not ComboBoxItem minuteItem ||
            !int.TryParse(hourItem.Content?.ToString(), out var hour) ||
            !int.TryParse(minuteItem.Content?.ToString(), out var minute))
        {
            return;
        }

        _appData.ReminderSetting.ReminderTime = new TimeSpan(hour, minute, 0);
    }
}

public class SettingsViewModel
{
    public string Version { get; }
    public string BuildDate { get; }
    public string BuildTime { get; }
    public string CurrentTime { get; }
    public ReminderSetting ReminderSetting { get; set; } = new();
    public string ChangeLog { get; }
    public string LimitationNote { get; }

    public SettingsViewModel()
    {
        Version = AppVersion.VERSION;
        BuildDate = AppVersion.BUILD_DATE;
        BuildTime = AppVersion.BUILD_TIME;
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ChangeLog = string.Join("\n",
            "1. 修复六大板块标签在鼠标悬停时的颜色串位问题。",
            "2. 删除仓库中的个人记录，替换为覆盖各模块的中性测试数据。",
            "3. 调整日记编辑窗口宽度，并把时间记录饼状图移回可视区域。",
            "4. 为任务补充一次性提醒，可设置具体日期和时间。",
            "5. 修复循环提醒逻辑，支持每 3 天等间隔提醒。",
            "6. 加宽时间记录编辑窗口，确保内容完整显示。",
            "7. 标签颜色改为稳定映射，同一标签每次保持一致。",
            "8. 将“今日打卡”按钮上移到连续打卡信息旁边。",
            "9. 重构导入导出格式，兼容旧版备份并为后续版本预留扩展。");
        LimitationNote =
            "任务提醒目前仅支持桌面端弹窗提醒，开源本地版本暂未接入手机推送。";
    }

    public SettingsViewModel(AppData appData) : this()
    {
        ReminderSetting = appData.ReminderSetting;
    }
}
