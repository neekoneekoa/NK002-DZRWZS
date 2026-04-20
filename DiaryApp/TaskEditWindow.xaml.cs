using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DiaryApp
{
    public partial class TaskEditWindow : Window
    {
        public TaskEntry? TaskEntry { get; private set; }
        public bool IsDeleteRequested { get; private set; } = false;
        private List<string> _currentProjectTags = new List<string>();
        private ReminderSetting? _tempReminderSettings = null;
        private AppData _appData;
        private const string DATA_FILE = "app_data.json";

        // 自动扩展输入框的默认高度和最少行数
        private const double DEFAULT_TEXTBOX_HEIGHT = 30;
        private const double LINE_HEIGHT = 20;
        private const int MIN_LINES_FOR_AUTO_EXPAND = 1;

        // 存储所有提醒日期
        private List<DateTime> _reminderDates = new List<DateTime>();

        // 为输入框启用自动扩展
        private void SetupAutoExpandTextBox(TextBox textBox, double initialHeight = DEFAULT_TEXTBOX_HEIGHT)
        {
            var defaultHeight = ComputeDefaultTextBoxHeight(textBox);
            textBox.Height = defaultHeight;
            textBox.MinHeight = defaultHeight;
            textBox.MaxHeight = double.PositiveInfinity;
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            
            textBox.TextChanged += AutoExpandTextBox_TextChanged;
            textBox.SizeChanged += AutoExpandTextBox_SizeChanged;
        }

        private double ComputeDefaultTextBoxHeight(TextBox textBox)
        {
            var singleLineText = new System.Windows.Media.FormattedText(
                "A",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(textBox).PixelsPerDip);

            double paddingHeight = textBox.Padding.Top + textBox.Padding.Bottom;
            double borderHeight = textBox.BorderThickness.Top + textBox.BorderThickness.Bottom;
            return singleLineText.Height + paddingHeight + borderHeight;
        }

        private void AutoExpandTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // 延迟更新高度，避免频繁重算
            textBox.Dispatcher.BeginInvoke(() => UpdateTextBoxHeight(textBox), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void AutoExpandTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // 宽度变化时重新计算高度
            if (e.WidthChanged)
            {
                textBox.Dispatcher.BeginInvoke(() => UpdateTextBoxHeight(textBox), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void UpdateTextBoxHeight(TextBox textBox)
        {
            if (textBox == null) return;

            // 保存当前光标位置
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // 临时改为自动高度，便于测量
            textBox.Height = double.NaN;

            // 娴嬮噺鏂囨湰楂樺害
            var formattedText = new System.Windows.Media.FormattedText(
                textBox.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(textBox).PixelsPerDip);

            // 鑰冭檻padding
            double paddingHeight = textBox.Padding.Top + textBox.Padding.Bottom;
            double borderHeight = textBox.BorderThickness.Top + textBox.BorderThickness.Bottom;
            double totalPadding = paddingHeight + borderHeight;

            // 计算需要的行数
            double textWidth = textBox.ActualWidth - textBox.Padding.Left - textBox.Padding.Right - textBox.BorderThickness.Left - textBox.BorderThickness.Right;
            if (textWidth <= 0) textWidth = 200; // 默认值

            formattedText.MaxTextWidth = textWidth;
            double textHeight = formattedText.Height + totalPadding;

            var singleLineText = new System.Windows.Media.FormattedText(
                "A",
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                System.Windows.Media.Brushes.Black,
                VisualTreeHelper.GetDpi(textBox).PixelsPerDip);
            double defaultHeight = singleLineText.Height + totalPadding;

            // 计算行数
            int lineCount = (int)Math.Ceiling(formattedText.Height / singleLineText.Height);

            // 只有一行时保持默认高度
            if (lineCount <= 1)
            {
                textBox.Height = defaultHeight;
            }
            else if (lineCount <= MIN_LINES_FOR_AUTO_EXPAND)
            {
                textBox.Height = defaultHeight;
            }
            else
            {
                textBox.Height = Math.Max(defaultHeight, textHeight);
            }

            // 恢复光标位置
            textBox.SelectionStart = selectionStart;
            textBox.SelectionLength = selectionLength;
        }

        public TaskEditWindow(AppData appData, TaskEntry? taskEntry = null)
        {
            try
            {
                InitializeComponent();
                _appData = appData;
                TaskEntry = taskEntry;
                LoadTaskData();
                
                // 注册标题输入框事件
                TitleTextBox.GotFocus += TitleTextBox_GotFocus;
                TitleTextBox.LostFocus += TitleTextBox_LostFocus;

                // 注册项目标签输入框事件
                if (ProjectTagsTextBox != null)
                {
                    ProjectTagsTextBox.PreviewMouseLeftButtonUp += ProjectTagsTextBox_PreviewMouseLeftButtonUp;
                    ProjectTagsTextBox.GotFocus += ProjectTagsTextBox_GotFocus;
                }
                
                // 初始化占位提示
                InitializeTitlePlaceholder();
                
                // 初始化提醒信息显示
                RefreshReminderInfoDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TaskEditWindow 构造函数异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"初始化任务编辑窗口时发生错误: {ex.Message}\n\n堆栈跟踪: {ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // 继续抛出异常，便于上层感知
            }
        }

        private void LoadTaskData()
        {
            try
            {
                // 防止初始化未完成时访问控件
                if (TitleTextBox == null || PriorityComboBox == null || 
                    StatusComboBox == null || CompletedDatePicker == null || ChaptersPanel == null ||
                    TaskTypeComboBox == null || ReminderCalendar == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadTaskData: 控件尚未初始化完成，跳过数据加载");
                    return;
                }
            
                if (TaskEntry != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadTaskData: 加载任务数据 - {TaskEntry.Title}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadTaskData: 创建新任务");
                }

                if (TaskEntry != null)
                {
                    _tempReminderSettings = TaskEntry.ReminderSettings?.Clone() as ReminderSetting;
                    if (!string.IsNullOrEmpty(TaskEntry.Title))
                    {
                        TitleTextBox.Text = TaskEntry.Title;
                        TitleTextBox.Foreground = Brushes.Black;
                    }
                    else
                    {
                        InitializeTitlePlaceholder();
                    }
                    
                    // 加载任务类型
                    TaskTypeComboBox.SelectedIndex = (int)TaskEntry.TaskType;
                    
                    // 显示或隐藏项目标签相关控件
                    UpdateProjectTagsVisibility();
                    
                    // 鍔犺浇椤圭洰鏍囩
                    if (TaskEntry.ProjectTags != null)
                    {
                        _currentProjectTags = new List<string>(TaskEntry.ProjectTags);
                    }
                    RefreshProjectTagsDisplay();
                    
                    PriorityComboBox.SelectedIndex = TaskEntry.Priority - 1;
                    StatusComboBox.SelectedIndex = (int)TaskEntry.Status;
                    CompletedDatePicker.SelectedDate = TaskEntry.CompletedAt;

                    // 鍔犺浇绔犺妭
                    ChaptersPanel.Children.Clear();
                    if (TaskEntry.Chapters != null)
                    {
                        foreach (var chapter in TaskEntry.Chapters)
                        {
                            AddChapterToPanel(chapter);
                        }
                    }
                }
                else
                {
                    // 默认值
                    TitleTextBox.Text = "";
                    TaskTypeComboBox.SelectedIndex = 0; // 榛樿涓存椂浠诲姟
                    PriorityComboBox.SelectedIndex = 1;
                    StatusComboBox.SelectedIndex = 0;
                    CompletedDatePicker.SelectedDate = null;

                    // 显示或隐藏项目标签相关控件
                    UpdateProjectTagsVisibility();
                    
                    // 榛樿娣诲姞绗竴绔?
                    ChaptersPanel.Children.Clear();
                    AddDefaultChapter();
                }

                TitleTextBox.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadTaskData 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                MessageBox.Show($"加载任务数据时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTitlePlaceholder()
        {
            if (string.IsNullOrEmpty(TitleTextBox.Text))
            {
                TitleTextBox.Text = TitleTextBox.Tag?.ToString() ?? "";
                TitleTextBox.Foreground = Brushes.Gray;
            }
        }

        private void TitleTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TitleTextBox.Text == TitleTextBox.Tag?.ToString())
            {
                TitleTextBox.Text = "";
                TitleTextBox.Foreground = Brushes.Black;
            }
        }

        private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 鑳屾櫙棰滆壊鎸夐挳鐐瑰嚮浜嬩欢 - 鏆傛椂鐣欑┖
        }

        private void TitleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                TitleTextBox.Text = TitleTextBox.Tag?.ToString() ?? "";
                TitleTextBox.Foreground = Brushes.Gray;
            }
        }

        private void AddChapterButton_Click(object sender, RoutedEventArgs e)
        {
            var chapter = new TaskChapter
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"第{ChaptersPanel.Children.Count + 1}章",
                Content = "",
                CreatedAt = DateTime.Now,
                OrderIndex = ChaptersPanel.Children.Count
            };
            AddChapterToPanel(chapter);
        }

        private void AddDefaultChapter()
        {
            var chapter = new TaskChapter
            {
                Id = Guid.NewGuid().ToString(),
                Title = "第一章",
                Content = "",
                CreatedAt = DateTime.Now,
                OrderIndex = 0
            };
            AddChapterToPanel(chapter);
        }







        private void AddChapterToPanel(TaskChapter chapter)
        {
            var chapterPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // 绔犺妭鏍囬鏍?
            var chapterHeaderPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 鎶樺彔/灞曞紑鎸夐挳
            var expandButton = new Button
            {
                Content = ">",
                Width = 25,
                Height = 25,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 10
            };

            var chapterTitleTextBox = new TextBox
            {
                Text = chapter.Title,
                Width = 300, // 鏇撮暱鐨勫搴?
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8),
                Tag = "绔犺妭鏍囬"
            };
            
            // 娣诲姞鍗犱綅绗﹀姛鑳?
            if (string.IsNullOrEmpty(chapter.Title))
            {
                chapterTitleTextBox.Text = chapterTitleTextBox.Tag?.ToString() ?? "";
                chapterTitleTextBox.Foreground = Brushes.Gray;
            }
            chapterTitleTextBox.GotFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Text == tb.Tag?.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.Black;
                }
            };
            chapterTitleTextBox.LostFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.Foreground = Brushes.Gray;
                }
            };

            var addSubTaskButton = new Button
            {
                Content = "+ 添加子任务",
                Width = 100,
                Height = 30,
                Background = Brushes.LightBlue,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10, 0, 10, 0),
                FontSize = 12
            };

            var deleteChapterButton = new Button
            {
                Content = "删除章节",
                Width = 100,
                Height = 30,
                Background = Brushes.LightCoral,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 12
            };

            // 鍒涘缓绔犺妭鍐呭闈㈡澘锛堝彲鎶樺彔锛?
            var chapterContentPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            // 绔犺妭鍐呭
            var chapterContentTextBox = new TextBox
            {
                Text = chapter.Content,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.White,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = "章节内容"
            };
            
            // 璁剧疆鑷姩鎵╁睍鍔熻兘
            SetupAutoExpandTextBox(chapterContentTextBox);
            
            // 娣诲姞鍗犱綅绗﹀姛鑳?
            if (string.IsNullOrEmpty(chapter.Content))
            {
                chapterContentTextBox.Text = chapterContentTextBox.Tag?.ToString() ?? "";
                chapterContentTextBox.Foreground = Brushes.Gray;
            }
            chapterContentTextBox.GotFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Text == tb.Tag?.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.Black;
                }
            };
            chapterContentTextBox.LostFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.Foreground = Brushes.Gray;
                }
            };

            // 瀛愪换鍔″垪琛?
            var subTasksPanel = new StackPanel
            {
                Margin = new Thickness(20, 0, 0, 0)
            };

            addSubTaskButton.Click += (sender, e) =>
            {
                var subTask = new SubTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "新子任务",
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    ScheduledTime = DateTime.Now,
                    Content = "",
                    Notes = ""
                };
                AddSubTaskToChapterPanel(subTask, subTasksPanel);
            };

            // 鍔犺浇鐜版湁瀛愪换鍔?
            if (chapter.SubTasks != null)
            {
                foreach (var subTask in chapter.SubTasks)
                {
                    AddSubTaskToChapterPanel(subTask, subTasksPanel);
                }
            }

            // 娣诲姞鍒扮珷鑺傚唴瀹归潰鏉?
            chapterContentPanel.Children.Add(chapterContentTextBox);
            chapterContentPanel.Children.Add(subTasksPanel);

            // 鍒犻櫎绔犺妭鎸夐挳浜嬩欢
            deleteChapterButton.Click += (sender, e) =>
            {
                // 鏄剧ず纭鍒犻櫎瀵硅瘽妗?
                MessageBoxResult result = MessageBox.Show(
                    "确定要删除这个章节吗？删除后将无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 浠嶶I涓Щ闄ょ珷鑺傞潰鏉?
                    if (chapterPanel.Parent is Panel parentPanel)
                    {
                        parentPanel.Children.Remove(chapterPanel);
                    }
                    
                    // 浠庝换鍔＄殑绔犺妭鍒楄〃涓Щ闄ゅ搴旂殑绔犺妭
                    if (TaskEntry != null)
                    {
                        TaskEntry.Chapters.Remove(chapter);
                    }
                }
            };

            // 鎶樺彔/灞曞紑閫昏緫
            var isExpanded = true;
            expandButton.Click += (sender, e) =>
            {
                isExpanded = !isExpanded;
                chapterContentPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                expandButton.Content = isExpanded ? ">" : "v";
            };

            chapterHeaderPanel.Children.Add(expandButton);
            chapterHeaderPanel.Children.Add(chapterTitleTextBox);
            chapterHeaderPanel.Children.Add(addSubTaskButton);
            chapterHeaderPanel.Children.Add(deleteChapterButton);

            // 灏嗙珷鑺傛爣棰樻爮鍜屽唴瀹归潰鏉挎坊鍔犲埌绔犺妭闈㈡澘
            chapterPanel.Children.Add(chapterHeaderPanel);
            chapterPanel.Children.Add(chapterContentPanel);

            // 娣诲姞鍒扮珷鑺傚垪琛?
            ChaptersPanel.Children.Add(chapterPanel);
        }

        private void AddSubTaskToChapterPanel(SubTask subTask, Panel parentPanel)
        {
            var subTaskPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var subTaskBorder = new Border
            {
                Background = Brushes.LightGray,
                Padding = new Thickness(10),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.DarkGray,
                Child = subTaskPanel
            };

            // 绗竴琛岋細鎶樺彔/灞曞紑鎸夐挳銆佹椂闂磋寖鍥村拰鍚嶇О
            var firstRowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 鎶樺彔/灞曞紑鎸夐挳
            var expandButton = new Button
            {
                Content = ">",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 8
            };

            // 寮€濮嬫椂闂村拰缁撴潫鏃堕棿閫夋嫨鍣?
            var startTimePicker = new DatePicker
            {
                SelectedDate = subTask.StartDate, // 浣跨敤瀛愪换鍔＄殑寮€濮嬫棩鏈燂紙鏈夐粯璁ゅ€硷級
                Width = 120,
                FontSize = 12,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var timeSeparator = new TextBlock
            {
                Text = "-",
                Margin = new Thickness(5, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var endTimePicker = new DatePicker
            {
                SelectedDate = subTask.EndDate, // 浣跨敤瀛愪换鍔＄殑缁撴潫鏃ユ湡锛堟湁榛樿鍊硷級
                Width = 120,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var nameTextBox = new TextBox
            {
                Text = subTask.Title,
                Width = 300, // 鏇撮暱鐨勫搴?
                FontSize = 14,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Tag = "子任务名称"
            };
            
            // 娣诲姞鍗犱綅绗﹀姛鑳?
            if (string.IsNullOrEmpty(subTask.Title))
            {
                nameTextBox.Text = nameTextBox.Tag?.ToString() ?? "";
                nameTextBox.Foreground = Brushes.Gray;
            }
            nameTextBox.GotFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Text == tb.Tag?.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.Black;
                }
            };
            nameTextBox.LostFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.Foreground = Brushes.Gray;
                }
            };

            var completedCheckBox = new CheckBox
            {
                IsChecked = subTask.IsCompleted,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 鍒犻櫎鎸夐挳
            var deleteButton = new Button
            {
                Content = "x",
                Width = 25,
                Height = 25,
                Background = Brushes.Red,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };

            deleteButton.Click += (sender, e) =>
            {
                parentPanel.Children.Remove(subTaskBorder);
            };

            firstRowPanel.Children.Add(expandButton);
            firstRowPanel.Children.Add(startTimePicker);
            firstRowPanel.Children.Add(timeSeparator);
            firstRowPanel.Children.Add(endTimePicker);
            firstRowPanel.Children.Add(nameTextBox);
            firstRowPanel.Children.Add(completedCheckBox);
            firstRowPanel.Children.Add(deleteButton);

            // 绗簩琛岋細瀛愪换鍔″唴瀹?
            var contentTextBox = new TextBox
            {
                Text = subTask.Content,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.White,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = "子任务内容"
            };
            
            // 璁剧疆鑷姩鎵╁睍鍔熻兘
            SetupAutoExpandTextBox(contentTextBox);
            
            // 娣诲姞鍗犱綅绗﹀姛鑳?
            if (string.IsNullOrEmpty(subTask.Content))
            {
                contentTextBox.Text = contentTextBox.Tag?.ToString() ?? "";
                contentTextBox.Foreground = Brushes.Gray;
            }
            contentTextBox.GotFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Text == tb.Tag?.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.Black;
                }
            };
            contentTextBox.LostFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.Foreground = Brushes.Gray;
                }
            };

            // 绗洓琛岋細娉ㄦ剰浜嬮」澶囨敞
            var notesTextBox = new TextBox
            {
                Text = subTask.Notes,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Orange,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8),
                Tag = "注意事项备注"
            };
            
            // 璁剧疆鑷姩鎵╁睍鍔熻兘
            SetupAutoExpandTextBox(notesTextBox);
            
            // 娣诲姞鍗犱綅绗﹀姛鑳?
            if (string.IsNullOrEmpty(subTask.Notes))
            {
                notesTextBox.Text = notesTextBox.Tag?.ToString() ?? "";
                notesTextBox.Foreground = Brushes.Gray;
            }
            notesTextBox.GotFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && tb.Text == tb.Tag?.ToString())
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.Black;
                }
            };
            notesTextBox.LostFocus += (sender, e) =>
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text))
                {
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.Foreground = Brushes.Gray;
                }
            };

            // 鍒涘缓鍙姌鍙犵殑瀛愪换鍔″唴瀹归潰鏉?
            var subTaskContentPanel = new StackPanel
            {
                Margin = new Thickness(20, 0, 0, 0)
            };

            subTaskContentPanel.Children.Add(contentTextBox);
            subTaskContentPanel.Children.Add(notesTextBox);

            // 鎶樺彔/灞曞紑閫昏緫
            var isExpanded = true;
            expandButton.Click += (sender, e) =>
            {
                isExpanded = !isExpanded;
                subTaskContentPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                expandButton.Content = isExpanded ? ">" : "v";
            };

            subTaskPanel.Children.Add(firstRowPanel);
            subTaskPanel.Children.Add(subTaskContentPanel);

            parentPanel.Children.Add(subTaskBorder);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("请输入任务标题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 鏀堕泦绔犺妭鏁版嵁
            var chapters = new List<TaskChapter>();
            foreach (var chapterChild in ChaptersPanel.Children)
            {
                if (chapterChild is StackPanel chapterPanel)
                {
                    var chapter = new TaskChapter
                    {
                        Id = Guid.NewGuid().ToString(),
                        SubTasks = new List<SubTask>()
                    };

                    // 鑾峰彇绔犺妭鏍囬
                    if (chapterPanel.Children.Count > 0 && chapterPanel.Children[0] is StackPanel chapterHeaderPanel)
                    {
                        // 绔犺妭鏍囬闈㈡澘鍖呭惈锛氭姌鍙犳寜閽?0)銆佹爣棰樻枃鏈(1)銆佹坊鍔犲瓙浠诲姟鎸夐挳(2)
                        if (chapterHeaderPanel.Children.Count > 1 && chapterHeaderPanel.Children[1] is TextBox chapterTitleTextBox)
                        {
                            chapter.Title = chapterTitleTextBox.Text;
                        }
                    }

                    // 鑾峰彇绔犺妭鍐呭鍜屽瓙浠诲姟
                    if (chapterPanel.Children.Count > 1 && chapterPanel.Children[1] is StackPanel chapterContentPanel)
                    {
                        // 绔犺妭鍐呭闈㈡澘鍖呭惈锛氱珷鑺傚唴瀹规枃鏈(0)銆佸瓙浠诲姟鍒楄〃闈㈡澘(1)
                        if (chapterContentPanel.Children.Count > 0 && chapterContentPanel.Children[0] is TextBox chapterContentTextBox)
                        {
                            chapter.Content = chapterContentTextBox.Text;
                        }

                        if (chapterContentPanel.Children.Count > 1 && chapterContentPanel.Children[1] is StackPanel subTasksPanel)
                        {
                            // 鑾峰彇瀛愪换鍔?
                            foreach (var subTaskChild in subTasksPanel.Children)
                            {
                                if (subTaskChild is Border subTaskBorder && subTaskBorder.Child is StackPanel subTaskPanel)
                                {
                                    var subTask = new SubTask
                                    {
                                        Id = Guid.NewGuid().ToString()
                                    };

                                    // 瑙ｆ瀽瀛愪换鍔￠潰鏉?
                                    if (subTaskPanel.Children.Count > 0 && subTaskPanel.Children[0] is StackPanel firstRowPanel)
                                    {
                                        // 瀛愪换鍔＄涓€琛屽寘鍚細鎶樺彔鎸夐挳(0)銆佸紑濮嬫椂闂?1)銆佸垎闅旂(2)銆佺粨鏉熸椂闂?3)銆佹爣棰?4)銆佸畬鎴愬閫夋(5)銆佸垹闄ゆ寜閽?6)
                                        if (firstRowPanel.Children.Count > 1 && firstRowPanel.Children[1] is DatePicker startTimePicker)
                                        {
                                            subTask.ScheduledTime = startTimePicker.SelectedDate;
                                            subTask.StartDate = startTimePicker.SelectedDate ?? DateTime.Now;
                                        }
                                        if (firstRowPanel.Children.Count > 3 && firstRowPanel.Children[3] is DatePicker endTimePicker)
                                        {
                                            subTask.EndDate = endTimePicker.SelectedDate ?? DateTime.Now.AddHours(1);
                                        }
                                        if (firstRowPanel.Children.Count > 4 && firstRowPanel.Children[4] is TextBox nameTextBox)
                                        {
                                            subTask.Title = nameTextBox.Text;
                                        }
                                        if (firstRowPanel.Children.Count > 5 && firstRowPanel.Children[5] is CheckBox completedCheckBox)
                                        {
                                            subTask.IsCompleted = completedCheckBox.IsChecked ?? false;
                                        }
                                    }

                                    if (subTaskPanel.Children.Count > 1 && subTaskPanel.Children[1] is StackPanel subTaskContentPanel)
                                    {
                                        // 瀛愪换鍔″唴瀹归潰鏉垮寘鍚細鍐呭鏂囨湰妗?0)銆佸娉ㄦ枃鏈(1)
                                        if (subTaskContentPanel.Children.Count > 0 && subTaskContentPanel.Children[0] is TextBox contentTextBox)
                                        {
                                            subTask.Content = contentTextBox.Text;
                                        }
                                        if (subTaskContentPanel.Children.Count > 1 && subTaskContentPanel.Children[1] is TextBox notesTextBox)
                                        {
                                            subTask.Notes = notesTextBox.Text;
                                        }
                                    }

                                    subTask.CreatedAt = DateTime.Now;
                                    chapter.SubTasks.Add(subTask);
                                }
                            }
                        }
                    }

                    chapter.CreatedAt = DateTime.Now;
                    chapter.OrderIndex = chapters.Count;
                    chapters.Add(chapter);
                }
            }

            if (TaskEntry == null)
            {
                // 鍒涘缓鏂颁换鍔?
                TaskEntry = new TaskEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = TitleTextBox.Text,
                    Priority = PriorityComboBox.SelectedIndex + 1,
                    Status = (TaskStatus)StatusComboBox.SelectedIndex,
                    TaskType = (TaskType)TaskTypeComboBox.SelectedIndex,
                    ProjectTags = new List<string>(_currentProjectTags),
                    Chapters = chapters,
                    ReminderSettings = _tempReminderSettings,
                    CreatedAt = DateTime.Now
                };
            }
            else
            {
                // 鏇存柊鐜版湁浠诲姟
                TaskEntry.Title = TitleTextBox.Text;
                TaskEntry.Priority = PriorityComboBox.SelectedIndex + 1;
                TaskEntry.Status = (TaskStatus)StatusComboBox.SelectedIndex;
                TaskEntry.TaskType = (TaskType)TaskTypeComboBox.SelectedIndex;
                TaskEntry.ProjectTags = new List<string>(_currentProjectTags);
                TaskEntry.CompletedAt = CompletedDatePicker.SelectedDate;
                TaskEntry.Chapters = chapters;
                TaskEntry.ReminderSettings = _tempReminderSettings;
            }

            DialogResult = true;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaskEntry != null)
            {
                var result = MessageBox.Show("确定要删除此任务吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    IsDeleteRequested = true;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // 浠诲姟绫诲瀷閫夋嫨鏀瑰彉浜嬩欢
        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 妫€鏌ユ帶浠舵槸鍚﹀凡缁忓垵濮嬪寲锛岄伩鍏嶅湪XAML鍒濆鍖栬繃绋嬩腑瑙﹀彂浜嬩欢瀵艰嚧鐨勭┖寮曠敤寮傚父
                if (ProjectTagsPanel == null || ProjectTagsDisplayPanel == null || TaskTypeComboBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("TaskTypeComboBox_SelectionChanged: 控件尚未初始化完成");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"TaskTypeComboBox_SelectionChanged: 选中索引 {TaskTypeComboBox.SelectedIndex}");
                UpdateProjectTagsVisibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TaskTypeComboBox_SelectionChanged 异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }

        // 鏇存柊椤圭洰鏍囩鎺т欢鐨勫彲瑙佹€?
        private void UpdateProjectTagsVisibility()
        {
            // 妫€鏌ユ帶浠舵槸鍚﹀凡缁忓垵濮嬪寲锛岄伩鍏嶇┖寮曠敤寮傚父
            if (ProjectTagsPanel == null || ProjectTagsDisplayPanel == null || TaskTypeComboBox == null)
            {
                return;
            }

            if (TaskTypeComboBox.SelectedIndex == (int)TaskType.Project)
            {
                ProjectTagsPanel.Visibility = Visibility.Visible;
                ProjectTagsDisplayPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ProjectTagsPanel.Visibility = Visibility.Collapsed;
                ProjectTagsDisplayPanel.Visibility = Visibility.Collapsed;
            }
        }

        // 娣诲姞椤圭洰鏍囩
        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            var tagText = ProjectTagsTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(tagText) && tagText != ProjectTagsTextBox.Tag?.ToString())
            {
                bool globalTagAdded = false;
                // 鍒嗗壊澶氫釜鏍囩锛堢敤閫楀彿鍒嗛殧锛?
                var tags = tagText.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                {
                    var trimmedTag = tag.Trim();
                    if (!string.IsNullOrEmpty(trimmedTag))
                    {
                        if (!_currentProjectTags.Contains(trimmedTag))
                        {
                            _currentProjectTags.Add(trimmedTag);
                        }

                        // 淇濆瓨鍒板叏灞€鏍囩
                        if (_appData != null)
                        {
                            if (_appData.GlobalTags == null) _appData.GlobalTags = new List<string>();
                            if (!_appData.GlobalTags.Contains(trimmedTag))
                            {
                                _appData.GlobalTags.Add(trimmedTag);
                                globalTagAdded = true;
                            }
                        }
                    }
                }
                
                if (globalTagAdded)
                {
                    SaveAppData();
                }
                
                RefreshProjectTagsDisplay();
                ProjectTagsTextBox.Text = "";
                ProjectTagsTextBox.Focus();
            }
        }

        // 绉婚櫎椤圭洰鏍囩
        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _currentProjectTags.Remove(tag);
                RefreshProjectTagsDisplay();
            }
        }

        // 鍒锋柊椤圭洰鏍囩鏄剧ず
        private void RefreshProjectTagsDisplay()
        {
            ProjectTagsItemsControl.ItemsSource = null;
            ProjectTagsItemsControl.ItemsSource = _currentProjectTags;
        }

        private void ProjectTagsTextBox_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (QuickTagPopup != null && !QuickTagPopup.IsOpen)
            {
                ShowQuickTagPopup();
            }
        }

        private void ProjectTagsTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowQuickTagPopup();
        }

        private void ShowQuickTagPopup()
        {
            if (QuickTagPopup == null || QuickTagsItemsControl == null || NoQuickTagsText == null || _appData == null) return;

            var globalTags = _appData.GlobalTags ?? new List<string>();
            
            if (globalTags.Count > 0)
            {
                QuickTagsItemsControl.ItemsSource = null;
                QuickTagsItemsControl.ItemsSource = globalTags;
                QuickTagsItemsControl.Visibility = Visibility.Visible;
                NoQuickTagsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                QuickTagsItemsControl.Visibility = Visibility.Collapsed;
                NoQuickTagsText.Visibility = Visibility.Visible;
            }

            QuickTagPopup.IsOpen = true;
        }

        private void QuickTag_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string? tag = null;
            if (sender is Border border && border.DataContext is string t1)
            {
                tag = t1;
            }
            else if (sender is TextBlock textBlock && textBlock.Text is string t2)
            {
                tag = t2;
            }

            if (tag != null)
            {
                if (!_currentProjectTags.Contains(tag))
                {
                    _currentProjectTags.Add(tag);
                    RefreshProjectTagsDisplay();
                }
                QuickTagPopup.IsOpen = false;
                ProjectTagsTextBox.Focus();
            }
        }

        private void DeleteQuickTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag && _appData != null)
            {
                if (_appData.GlobalTags != null && _appData.GlobalTags.Contains(tag))
                {
                    _appData.GlobalTags.Remove(tag);
                    // 閲嶆柊鏄剧ず寮圭獥浠ュ埛鏂板垪琛?
                    ShowQuickTagPopup();
                    
                    // 淇濆瓨鏁版嵁
                    SaveAppData();
                }
                // 闃叉浜嬩欢鍐掓场瀵艰嚧寮圭獥鍏抽棴
                e.Handled = true;
            }
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            if (QuickTagPopup != null)
            {
                QuickTagPopup.IsOpen = false;
            }
        }

        // 鎻愰啋鎸夐挳鐐瑰嚮浜嬩欢
        private void ReminderButton_Click(object sender, RoutedEventArgs e)
        {
            // 鍒涘缓涓€涓复鏃朵换鍔″璞＄敤浜庢樉绀?
            var displayTask = TaskEntry ?? new TaskEntry { Title = TitleTextBox.Text.Trim() }; 
            
            // 纭畾瑕佷娇鐢ㄧ殑鎻愰啋璁剧疆锛堜紭鍏堜娇鐢ㄤ复鏃剁殑锛屽鏋滄病鏈夊垯浣跨敤浠诲姟鐨勶級
            var reminderSettings = _tempReminderSettings ?? TaskEntry?.ReminderSettings?.Clone() as ReminderSetting;

            // 鎵撳紑鎻愰啋璁剧疆绐楀彛
            var reminderWindow = new ReminderSettingsWindow(displayTask, reminderSettings);
            reminderWindow.Owner = this;
            bool? result = reminderWindow.ShowDialog();

            if (result == true)
            {
                // 濡傛灉鐢ㄦ埛鐐瑰嚮浜嗕繚瀛?
                if (reminderWindow.IsSaveRequested && reminderWindow.ReminderSettings != null)
                {
                    // 鏍规嵁鎯呭喌淇濆瓨鍒颁复鏃跺彉閲忔垨浠诲姟瀵硅薄
                    _tempReminderSettings = reminderWindow.ReminderSettings;
                    MessageBox.Show("提醒设置已保存。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshReminderInfoDisplay();
                }
            }
        }
        
        /// <summary>
        /// 鏇存柊鎻愰啋淇℃伅鏄剧ず
        /// </summary>
        private void RefreshReminderInfoDisplay()
        {
            var reminderSettings = _tempReminderSettings ?? TaskEntry?.ReminderSettings;
            if (reminderSettings == null)
            {
                if (ReminderInfoPanel != null)
                {
                    ReminderInfoPanel.Visibility = Visibility.Collapsed;
                }
                return;
            }

            ReminderStartDateText.Text = reminderSettings.StartDate.HasValue
                ? $"{reminderSettings.StartDate.Value:yyyy-MM-dd} {(reminderSettings.ReminderTime ?? TimeSpan.Zero):hh\\:mm}"
                : "";

            ReminderTypeText.Text = reminderSettings.ReminderType switch
            {
                ReminderType.Once => "Once",
                ReminderType.Daily => (reminderSettings.IntervalDays ?? 1) > 1
                    ? $"Daily (every {reminderSettings.IntervalDays} days)"
                    : "Daily",
                ReminderType.Weekly => reminderSettings.WeekDays != null && reminderSettings.WeekDays.Count > 0
                    ? $"Weekly ({string.Join(", ", reminderSettings.WeekDays.Select(GetWeekDayLabel))})"
                    : "Weekly",
                ReminderType.Monthly => reminderSettings.MonthlyDayNumber.HasValue && reminderSettings.MonthlyDayOfWeek.HasValue
                    ? $"Monthly (week {reminderSettings.MonthlyDayNumber}, {GetWeekDayLabel(reminderSettings.MonthlyDayOfWeek.Value)})"
                    : "Monthly",
                ReminderType.Yearly => "Yearly",
                ReminderType.Interval => $"Interval ({Math.Max(1, reminderSettings.IntervalDays ?? 1)} days)",
                _ => "Not set"
            };

            if (reminderSettings.NextReminderDate == null && reminderSettings.IsEnabled && reminderSettings.IsActive)
            {
                reminderSettings.NextReminderDate = ReminderScheduler.CalculateNextReminderDate(reminderSettings);
            }

            NextReminderDateText.Text = reminderSettings.NextReminderDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
            ReminderStatusText.Text = reminderSettings.IsEnabled && reminderSettings.IsActive ? "Enabled" : "Disabled";
            _reminderDates = CalculatePreviewReminderDates(reminderSettings);

            if (ReminderCalendar != null)
            {
                if (reminderSettings.StartDate.HasValue)
                {
                    ReminderCalendar.DisplayDateStart = reminderSettings.StartDate.Value;
                    ReminderCalendar.DisplayDateEnd = reminderSettings.StartDate.Value.AddYears(1);
                    ReminderCalendar.SelectedDate = reminderSettings.NextReminderDate ?? reminderSettings.StartDate.Value;
                }

                ReminderCalendar.DisplayMode = CalendarMode.Month;
                ReminderCalendar.DisplayMode = CalendarMode.Month;
            }

            ReminderInfoPanel.Visibility = Visibility.Visible;
        }

        private string GetWeekDayLabel(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Mon",
                DayOfWeek.Tuesday => "Tue",
                DayOfWeek.Wednesday => "Wed",
                DayOfWeek.Thursday => "Thu",
                DayOfWeek.Friday => "Fri",
                DayOfWeek.Saturday => "Sat",
                DayOfWeek.Sunday => "Sun",
                _ => "?"
            };
        }

        private List<DateTime> CalculatePreviewReminderDates(ReminderSetting reminderSettings)
        {
            if (!reminderSettings.StartDate.HasValue)
            {
                return new List<DateTime>();
            }

            var startDate = reminderSettings.StartDate.Value.Date;
            return ReminderScheduler.CalculateReminderDates(reminderSettings, startDate, startDate.AddYears(1));
        }

        private void UpdateReminderInfoDisplay()
        {
            // 鑾峰彇褰撳墠鐨勬彁閱掕缃?
            var reminderSettings = _tempReminderSettings ?? TaskEntry?.ReminderSettings;
            
            if (reminderSettings != null)
            {
                // 鏇存柊鏄剧ず鍐呭
                string startDateText = "";
                if (reminderSettings.StartDate.HasValue)
                {
                    startDateText = reminderSettings.StartDate.Value.ToString("yyyy-MM-dd");
                    if (reminderSettings.ReminderTime.HasValue)
                        {
                            var time = reminderSettings.ReminderTime.Value;
                            startDateText += $" {time.Hours:D2}:{time.Minutes:D2}";
                        }
                }
                ReminderStartDateText.Text = startDateText;
                
                // 澶勭悊涓嶅悓鐨勬彁閱掓柟寮?
                string reminderTypeText = "";
                switch (reminderSettings.ReminderType)
                {
                    case ReminderType.Daily:
                        reminderTypeText = "每日提醒";
                        if (reminderSettings.IntervalDays.HasValue && reminderSettings.IntervalDays.Value > 1)
                        {
                            reminderTypeText += $"（每 {reminderSettings.IntervalDays.Value} 天）";
                        }
                        break;
                    case ReminderType.Weekly:
                        reminderTypeText = "每周提醒";
                        if (reminderSettings.WeekDays != null && reminderSettings.WeekDays.Count > 0)
                        {
                            reminderTypeText += "（";
                            var dayNames = new[] { "日", "一", "二", "三", "四", "五", "六" };
                            foreach (var day in reminderSettings.WeekDays)
                            {
                                reminderTypeText += $"{dayNames[(int)day]}、";
                            }
                            reminderTypeText = reminderTypeText.TrimEnd('、') + "）";
                        }
                        break;
                    case ReminderType.Monthly:
                        reminderTypeText = "每月提醒";
                        if (reminderSettings.MonthlyDayNumber.HasValue && reminderSettings.MonthlyDayOfWeek.HasValue)
                        {
                            var dayNames = new[] { "日", "一", "二", "三", "四", "五", "六" };
                            reminderTypeText += $"（每月第{reminderSettings.MonthlyDayNumber.Value}个星期{dayNames[(int)reminderSettings.MonthlyDayOfWeek.Value]}）";
                        }
                        break;
                    case ReminderType.Yearly:
                        reminderTypeText = "每年提醒";
                        break;
                    case ReminderType.Interval:
                        reminderTypeText = "间隔提醒";
                        if (reminderSettings.IntervalDays.HasValue)
                        {
                            reminderTypeText += $"（每 {reminderSettings.IntervalDays.Value} 天）";
                        }
                        break;
                }
                
                ReminderTypeText.Text = reminderTypeText;
                
                // 鏄剧ず涓嬫鎻愰啋鏃堕棿
                string nextReminderText = "";
                if (reminderSettings.NextReminderDate.HasValue)
                {
                    nextReminderText = reminderSettings.NextReminderDate.Value.ToString("yyyy-MM-dd HH:mm");
                }
                NextReminderDateText.Text = nextReminderText;
                
                ReminderStatusText.Text = reminderSettings.IsActive ? "已启用" : "已禁用";
                
                // 璁＄畻鎵€鏈夋彁閱掓棩鏈?
                _reminderDates = CalculateAllReminderDates(reminderSettings);
                
                // 鏇存柊鏃ュ巻鎺т欢
                if (ReminderCalendar != null)
                {
                    // 璁剧疆鏃ュ巻鐨勬樉绀鸿寖鍥?
                    if (reminderSettings.StartDate.HasValue)
                    {
                        ReminderCalendar.DisplayDateStart = reminderSettings.StartDate.Value;
                        ReminderCalendar.DisplayDateEnd = reminderSettings.StartDate.Value.AddYears(1);
                    }
                    
                    // 濡傛灉鏈変笅娆℃彁閱掓棩鏈燂紝閫変腑璇ユ棩鏈?
                    if (reminderSettings.NextReminderDate.HasValue)
                    {
                        ReminderCalendar.SelectedDate = reminderSettings.NextReminderDate.Value;
                    }
                    // 鍚﹀垯濡傛灉鏈夊紑濮嬫棩鏈燂紝閫変腑璇ユ棩鏈?
                    else if (reminderSettings.StartDate.HasValue)
                    {
                        ReminderCalendar.SelectedDate = reminderSettings.StartDate.Value;
                    }
                    
                    // 閲嶆柊鍔犺浇鏃ュ巻浠ユ樉绀烘墍鏈夋彁閱掓棩鏈?
                    ReminderCalendar.DisplayMode = CalendarMode.Month;
                    ReminderCalendar.DisplayMode = CalendarMode.Month;
                }
                
                // 鏄剧ず鎻愰啋淇℃伅闈㈡澘
                ReminderInfoPanel.Visibility = Visibility.Visible;
            }
        }

        // 璁＄畻鎵€鏈夋彁閱掓棩鏈?
        private List<DateTime> CalculateAllReminderDates(ReminderSetting reminderSettings)
        {
            var reminderDates = new List<DateTime>();
            
            if (!reminderSettings.StartDate.HasValue || !reminderSettings.IsEnabled || !reminderSettings.IsActive)
                return reminderDates;

            var startDate = reminderSettings.StartDate.Value.Date;
            var endDate = startDate.AddYears(1); // 计算未来一年的提醒日期

            switch (reminderSettings.ReminderType)
            {
                case ReminderType.Daily:
                    // 姣忔棩鎻愰啋
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        reminderDates.Add(date);
                    }
                    break;

                case ReminderType.Weekly:
                    // 姣忓懆鎻愰啋
                    if (reminderSettings.WeekDays != null && reminderSettings.WeekDays.Count > 0)
                    {
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            if (reminderSettings.WeekDays.Contains(date.DayOfWeek))
                            {
                                reminderDates.Add(date);
                            }
                        }
                    }
                    break;

                case ReminderType.Monthly:
                    // 姣忔湀鎻愰啋
                    for (var date = startDate; date <= endDate; date = date.AddMonths(1))
                    {
                        if (reminderSettings.MonthlyDayNumber.HasValue && reminderSettings.MonthlyDayOfWeek.HasValue)
                        {
                            // 璁＄畻姣忔湀鐨勭鍑犱釜鏄熸湡鍑?
                            var targetDate = GetMonthlyWeekDayDate(date.Year, date.Month, 
                                reminderSettings.MonthlyDayNumber.Value, reminderSettings.MonthlyDayOfWeek.Value);
                            if (targetDate.HasValue && targetDate.Value >= startDate)
                            {
                                reminderDates.Add(targetDate.Value);
                            }
                        }
                        else
                        {
                            // 榛樿姣忔湀鍚屼竴澶?
                            reminderDates.Add(date);
                        }
                    }
                    break;

                case ReminderType.Yearly:
                    // 姣忓勾鎻愰啋
                    for (var date = startDate; date <= endDate; date = date.AddYears(1))
                    {
                        reminderDates.Add(date);
                    }
                    break;

                case ReminderType.Interval:
                    // 闂撮殧鎻愰啋锛堝闅斾竴澶╋級
                    if (reminderSettings.IntervalDays.HasValue && reminderSettings.IntervalDays.Value > 0)
                    {
                        var interval = reminderSettings.IntervalDays.Value;
                        for (var date = startDate; date <= endDate; date = date.AddDays(interval))
                        {
                            reminderDates.Add(date);
                        }
                    }
                    break;
            }

            return reminderDates;
        }

        // 鑾峰彇姣忔湀绗嚑涓槦鏈熷嚑鐨勬棩鏈?
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

        // 检查日期是否为提醒日期
        private bool IsReminderDate(DateTime date)
        {
            return _reminderDates.Any(d => d.Date == date.Date);
        }

        // 鏃ュ巻鏃ユ湡鍔犺浇浜嬩欢澶勭悊
        private void ReminderCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateCalendarReminderDates();
        }

        // 鏃ュ巻鏄剧ず鏈堜唤鏀瑰彉浜嬩欢澶勭悊
        private void ReminderCalendar_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            if (e.NewMode == CalendarMode.Month)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    UpdateCalendarReminderDates();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        // 鏃ュ巻閫夋嫨鏃ユ湡鏀瑰彉浜嬩欢澶勭悊
        private void ReminderCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateCalendarReminderDates();
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        // 娴嬭瘯鏃ュ巻鏇存柊鎸夐挳鐐瑰嚮浜嬩欢
        private void TestCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("测试日历刷新按钮被点击");
            System.Diagnostics.Debug.WriteLine($"当前提醒日期数量: {_reminderDates.Count}");
            
            // 强制重新计算提醒日期
            if (TaskEntry?.ReminderSettings != null)
            {
                _reminderDates = CalculateAllReminderDates(TaskEntry.ReminderSettings);
                System.Diagnostics.Debug.WriteLine($"重新计算后提醒日期数量: {_reminderDates.Count}");
            }
            
            UpdateCalendarReminderDates();
        }

        // 更新日历中的提醒日期显示
        private void UpdateCalendarReminderDates()
        {
            if (ReminderCalendar == null || _reminderDates.Count == 0)
                return;

            System.Diagnostics.Debug.WriteLine($"UpdateCalendarReminderDates: 找到 {_reminderDates.Count} 个提醒日期");
            foreach (var date in _reminderDates.Take(5))
            {
                System.Diagnostics.Debug.WriteLine($"提醒日期: {date:yyyy-MM-dd}");
            }

            // 浣跨敤Dispatcher寮傛鎵ц锛岀‘淇濇棩鍘嗘帶浠跺畬鍏ㄥ姞杞?
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("开始查找日历中的日期按钮...");
                    
                    // 鐩存帴鏌ユ壘鏃ュ巻鎺т欢涓殑鎵€鏈夋棩鏈熸寜閽?
                    var dayButtons = FindVisualChildren<System.Windows.Controls.Primitives.CalendarDayButton>(ReminderCalendar);
                    System.Diagnostics.Debug.WriteLine($"找到 {dayButtons.Count} 个日期按钮");
                    
                    int highlightedCount = 0;
                    foreach (var dayButton in dayButtons)
                    {
                        if (dayButton.DataContext is DateTime date)
                        {
                            if (IsReminderDate(date))
                            {
                                // 使用 Tag 标记提醒日期，以触发样式
                                dayButton.Tag = "ReminderDate";
                                highlightedCount++;
                                System.Diagnostics.Debug.WriteLine($"高亮日期: {date:yyyy-MM-dd}");
                            }
                            else
                            {
                                // 清除提醒日期标记
                                dayButton.ClearValue(FrameworkElement.TagProperty);
                            }
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"高亮了 {highlightedCount} 个日期按钮");
                }
                catch (Exception ex)
                {
                    // 闈欓粯澶勭悊寮傚父锛岄伩鍏嶅奖鍝嶇敤鎴蜂綋楠?
                    System.Diagnostics.Debug.WriteLine($"更新日历提醒日期时出错: {ex.Message}");
                }
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        // 鏌ユ壘鍙瀛愬厓绱?
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        // 鏌ユ壘鎵€鏈夊彲瑙嗗瓙鍏冪礌
        private List<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    children.Add(typedChild);
                }
                
                children.AddRange(FindVisualChildren<T>(child));
            }
            
            return children;
        }

        private string GetDataFilePath()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.Combine(appDir, DATA_FILE);
        }

        private void SaveAppData()
        {
            try
            {
                _appData.LastSaved = DateTime.Now;
                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var json = System.Text.Json.JsonSerializer.Serialize(_appData, options);
                var dataFile = GetDataFilePath();
                System.IO.File.WriteAllText(dataFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
            }
        }
    }
}



