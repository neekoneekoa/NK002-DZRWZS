using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DiaryApp;

public partial class MainWindow : Window
{
    private ObservableCollection<DiaryEntry> _diaries = new ObservableCollection<DiaryEntry>();
    private const string DATA_FILE = "diaries.json";
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
        if (File.Exists(DATA_FILE))
        {
            try 
            {
                var json = File.ReadAllText(DATA_FILE);
                var list = JsonSerializer.Deserialize<List<DiaryEntry>>(json);
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
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_diaries, options);
        File.WriteAllText(DATA_FILE, json);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
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
        MessageBox.Show("日记已保存！", "成功");
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
