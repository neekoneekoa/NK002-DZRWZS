using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DiaryApp;

// 版本信息 - 自动更新为当前时间
public static class AppVersion
{
    public const string VERSION = "0.0.1.13";
    public static readonly string BUILD_DATE = DateTime.Now.ToString("yyyy-MM-dd");
    public static readonly string BUILD_TIME = DateTime.Now.ToString("HH:mm");
}

public partial class MainWindow : Window
{
    private ObservableCollection<DiaryEntry> _diaries = new ObservableCollection<DiaryEntry>();
    private const string DATA_FILE = "diaries.json";
    
    // 获取应用数据文件的完整路径
    private string GetDataFilePath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, DATA_FILE);
    }
    private DiaryEntry? _currentEntry;

    public MainWindow()
    {
        InitializeComponent();
        
        // 支持窗口拖动 (因为设置了 WindowStyle="None")
        this.MouseLeftButtonDown += (s, e) => DragMove();
        
        DiaryListBox.ItemsSource = _diaries;
        LoadDiaries();
        
        // 默认显示今天的日期
        DateLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    private void LoadDiaries()
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
                var list = JsonSerializer.Deserialize<List<DiaryEntry>>(json, options);
                if (list != null)
                {
                    _diaries.Clear();
                    foreach (var item in list.OrderByDescending(d => d.CreatedAt))
                    {
                        _diaries.Add(item);
                    }
                }
            }
            catch { /* 忽略加载错误 */ }
        }
    }

    private void SaveDiaries()
    {
        try
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_diaries, options);
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

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void NewButton_Click(object sender, RoutedEventArgs e)
    {
        _currentEntry = null;
        DiaryListBox.SelectedItem = null;
        TitleTextBox.Text = "";
        ContentTextBox.Text = "";
        DateLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        TitleTextBox.Focus();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = TitleTextBox.Text.Trim();
            var content = ContentTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
            {
                MessageBox.Show("写点什么再保存吧~", "提示");
                return;
            }

            if (_currentEntry == null)
            {
                // 新增
                var newEntry = new DiaryEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = string.IsNullOrEmpty(title) ? "无标题" : title,
                    Content = content,
                    CreatedAt = DateTime.Now
                };
                _diaries.Insert(0, newEntry);
                _currentEntry = newEntry;
            }
            else
            {
                // 更新
                _currentEntry.Title = title;
                _currentEntry.Content = content;
                // 为了触发列表更新，这里简单粗暴地移除再添加，或者实现 INotifyPropertyChanged
                var index = _diaries.IndexOf(_currentEntry);
                if (index >= 0)
                {
                    _diaries[index] = new DiaryEntry 
                    { 
                        Id = _currentEntry.Id, 
                        Title = title, 
                        Content = content, 
                        CreatedAt = _currentEntry.CreatedAt 
                    };
                }
            }

            SaveDiaries();
            
            // 创建自动备份
            BackupManager.CreateAutoBackup(_diaries.ToList(), $"自动备份 - {DateTime.Now:yyyy-MM-dd HH:mm}");
            
            // 清理旧备份，保留最近10个
            BackupManager.CleanOldBackups(10);

            MessageBox.Show("日记已保存！已自动创建备份。", "成功");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}\n\n详细错误：{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DiaryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiaryListBox.SelectedItem is DiaryEntry entry)
        {
            _currentEntry = entry;
            TitleTextBox.Text = entry.Title;
            ContentTextBox.Text = entry.Content;
            DateLabel.Text = entry.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        }
    }

    private void ExportBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Diary Backup Files (*.diary)|*.diary|All Files (*.*)|*.*",
            DefaultExt = "diary",
            FileName = $"my_diary_backup_{DateTime.Now:yyyyMMdd_HHmmss}.diary"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                BackupManager.ExportBackupToLocation(_diaries.ToList(), dialog.FileName);
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
            Filter = "Diary Backup Files (*.diary)|*.diary|All Files (*.*)|*.*",
            DefaultExt = "diary"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var restoredDiaries = BackupManager.RestoreBackup(dialog.FileName);
                if (restoredDiaries != null)
                {
                    var result = MessageBox.Show(
                        $"找到 {restoredDiaries.Count} 条日记记录。\n\n是否要导入这些数据？\n\n注意：这将替换当前所有日记内容！", 
                        "确认导入", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _diaries.Clear();
                            foreach (var entry in restoredDiaries.OrderByDescending(d => d.CreatedAt))
                            {
                                _diaries.Add(entry);
                            }
                            
                            SaveDiaries();
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
            $"{index + 1}. {b.info.CreatedAt:yyyy-MM-dd HH:mm} - {b.info.Description} ({b.info.EntryCount}条记录)"));

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
}

public class DiaryEntry
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    // 用于界面显示格式化日期
    public string DateStr => CreatedAt.ToString("MM-dd HH:mm");
}
