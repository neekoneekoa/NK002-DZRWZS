using System.Windows;

namespace DiaryApp;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        
        // 支持窗口拖动
        this.MouseLeftButtonDown += (s, e) => DragMove();
        
        // 设置数据上下文
        DataContext = new SettingsViewModel();
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
}

// 设置窗口的数据模型
public class SettingsViewModel
{
    public string Version { get; }
    public string BuildDate { get; }
    public string BuildTime { get; }
    public string CurrentTime { get; }

    public SettingsViewModel()
    {
        Version = AppVersion.VERSION;
        BuildDate = AppVersion.BUILD_DATE;
        BuildTime = AppVersion.BUILD_TIME;
        CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}