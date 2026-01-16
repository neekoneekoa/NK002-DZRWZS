using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiaryApp
{
    public partial class TaskEditWindow : Window
    {
        public TaskEntry? TaskEntry { get; private set; }
        public bool IsDeleteRequested { get; private set; } = false;
        private List<string> _currentProjectTags = new List<string>();
        private ReminderSetting? _tempReminderSettings = null;

        public TaskEditWindow(TaskEntry? taskEntry = null)
        {
            InitializeComponent();
            TaskEntry = taskEntry;
            LoadTaskData();
            
            // 添加标题文本框事件处理
            TitleTextBox.GotFocus += TitleTextBox_GotFocus;
            TitleTextBox.LostFocus += TitleTextBox_LostFocus;
            
            // 初始化提示文字
            InitializeTitlePlaceholder();
            
            // 初始化提醒信息显示
            UpdateReminderInfoDisplay();
        }

        private void LoadTaskData()
        {
            // 防止初始化时控件尚未完全初始化导致的空引用异常
            if (TitleTextBox == null || PriorityComboBox == null || 
                StatusComboBox == null || CompletedDatePicker == null || ChaptersPanel == null ||
                TaskTypeComboBox == null)
            {
                return;
            }
        
            if (TaskEntry != null)
            {
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
                
                // 显示/隐藏项目标签相关控件
                UpdateProjectTagsVisibility();
                
                // 加载项目标签
                RefreshProjectTagsDisplay();
                
                PriorityComboBox.SelectedIndex = TaskEntry.Priority - 1;
                StatusComboBox.SelectedIndex = (int)TaskEntry.Status;
                CompletedDatePicker.SelectedDate = TaskEntry.CompletedAt;

                // 加载章节
                ChaptersPanel.Children.Clear();
                foreach (var chapter in TaskEntry.Chapters)
                {
                    AddChapterToPanel(chapter);
                }
            }
            else
            {
                // 默认值
                TitleTextBox.Text = "";
                TaskTypeComboBox.SelectedIndex = 0; // 默认临时任务
                PriorityComboBox.SelectedIndex = 1;
                StatusComboBox.SelectedIndex = 0;
                CompletedDatePicker.SelectedDate = null;

                // 显示/隐藏项目标签相关控件
                UpdateProjectTagsVisibility();
                
                // 默认添加第一章
                ChaptersPanel.Children.Clear();
                AddDefaultChapter();
            }

            TitleTextBox.Focus();
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
            // 背景颜色按钮点击事件 - 暂时留空
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

            // 章节标题栏
            var chapterHeaderPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // 折叠/展开按钮
            var expandButton = new Button
            {
                Content = "▼",
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
                Width = 300, // 更长的宽度
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8),
                Tag = "章节标题"
            };
            
            // 添加占位符功能
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

            // 创建章节内容面板（可折叠）
            var chapterContentPanel = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0)
            };

            // 章节内容
            var chapterContentTextBox = new TextBox
            {
                Text = chapter.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 80,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightGray,
                Background = Brushes.White,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = "章节内容"
            };
            
            // 添加占位符功能
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

            // 子任务列表
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

            // 加载现有子任务
            foreach (var subTask in chapter.SubTasks)
            {
                AddSubTaskToChapterPanel(subTask, subTasksPanel);
            }

            // 添加到章节内容面板
            chapterContentPanel.Children.Add(chapterContentTextBox);
            chapterContentPanel.Children.Add(subTasksPanel);

            // 删除章节按钮事件
            deleteChapterButton.Click += (sender, e) =>
            {
                // 显示确认删除对话框
                MessageBoxResult result = MessageBox.Show(
                    "确定要删除这个章节吗？删除后将无法恢复。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 从UI中移除章节面板
                    if (chapterPanel.Parent is Panel parentPanel)
                    {
                        parentPanel.Children.Remove(chapterPanel);
                    }
                    
                    // 从任务的章节列表中移除对应的章节
                    if (TaskEntry != null)
                    {
                        TaskEntry.Chapters.Remove(chapter);
                    }
                }
            };

            // 折叠/展开逻辑
            var isExpanded = true;
            expandButton.Click += (sender, e) =>
            {
                isExpanded = !isExpanded;
                chapterContentPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                expandButton.Content = isExpanded ? "▼" : "▶";
            };

            chapterHeaderPanel.Children.Add(expandButton);
            chapterHeaderPanel.Children.Add(chapterTitleTextBox);
            chapterHeaderPanel.Children.Add(addSubTaskButton);
            chapterHeaderPanel.Children.Add(deleteChapterButton);

            // 将章节标题栏和内容面板添加到章节面板
            chapterPanel.Children.Add(chapterHeaderPanel);
            chapterPanel.Children.Add(chapterContentPanel);

            // 添加到章节列表
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

            // 第一行：折叠/展开按钮、时间范围和名称
            var firstRowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 折叠/展开按钮
            var expandButton = new Button
            {
                Content = "▼",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 8
            };

            // 开始时间和结束时间选择器
            var startTimePicker = new DatePicker
            {
                SelectedDate = subTask.StartDate, // 使用子任务的开始日期
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
                SelectedDate = subTask.EndDate, // 使用子任务的结束日期
                Width = 120,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var nameTextBox = new TextBox
            {
                Text = subTask.Title,
                Width = 300, // 更长的宽度
                FontSize = 14,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Tag = "子任务名称"
            };
            
            // 添加占位符功能
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

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "×",
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

            // 第二行：子任务内容
            var contentTextBox = new TextBox
            {
                Text = subTask.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 60,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Background = Brushes.White,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8),
                Tag = "子任务内容"
            };
            
            // 添加占位符功能
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

            // 第四行：注意事项备注
            var notesTextBox = new TextBox
            {
                Text = subTask.Notes,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 40,
                FontSize = 12,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Orange,
                Background = Brushes.LightYellow,
                Padding = new Thickness(8),
                Tag = "注意事项备注"
            };
            
            // 添加占位符功能
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

            // 创建可折叠的子任务内容面板
            var subTaskContentPanel = new StackPanel
            {
                Margin = new Thickness(20, 0, 0, 0)
            };

            subTaskContentPanel.Children.Add(contentTextBox);
            subTaskContentPanel.Children.Add(notesTextBox);

            // 折叠/展开逻辑
            var isExpanded = true;
            expandButton.Click += (sender, e) =>
            {
                isExpanded = !isExpanded;
                subTaskContentPanel.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                expandButton.Content = isExpanded ? "▼" : "▶";
            };

            subTaskPanel.Children.Add(firstRowPanel);
            subTaskPanel.Children.Add(subTaskContentPanel);

            parentPanel.Children.Add(subTaskBorder);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("请输入任务标题", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 收集章节数据
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

                    // 获取章节标题
                    if (chapterPanel.Children.Count > 0 && chapterPanel.Children[0] is StackPanel chapterHeaderPanel)
                    {
                        // 章节标题面板包含：折叠按钮(0)、标题文本框(1)、添加子任务按钮(2)
                        if (chapterHeaderPanel.Children.Count > 1 && chapterHeaderPanel.Children[1] is TextBox chapterTitleTextBox)
                        {
                            chapter.Title = chapterTitleTextBox.Text;
                        }
                    }

                    // 获取章节内容和子任务
                    if (chapterPanel.Children.Count > 1 && chapterPanel.Children[1] is StackPanel chapterContentPanel)
                    {
                        // 章节内容面板包含：章节内容文本框(0)、子任务列表面板(1)
                        if (chapterContentPanel.Children.Count > 0 && chapterContentPanel.Children[0] is TextBox chapterContentTextBox)
                        {
                            chapter.Content = chapterContentTextBox.Text;
                        }

                        if (chapterContentPanel.Children.Count > 1 && chapterContentPanel.Children[1] is StackPanel subTasksPanel)
                        {
                            // 获取子任务
                            foreach (var subTaskChild in subTasksPanel.Children)
                            {
                                if (subTaskChild is Border subTaskBorder && subTaskBorder.Child is StackPanel subTaskPanel)
                                {
                                    var subTask = new SubTask
                                    {
                                        Id = Guid.NewGuid().ToString()
                                    };

                                    // 解析子任务面板
                                    if (subTaskPanel.Children.Count > 0 && subTaskPanel.Children[0] is StackPanel firstRowPanel)
                                    {
                                        // 子任务第一行包含：折叠按钮(0)、开始时间(1)、分隔符(2)、结束时间(3)、标题(4)、完成复选框(5)、删除按钮(6)
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
                                        // 子任务内容面板包含：内容文本框(0)、备注文本框(1)
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
                // 创建新任务
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
                // 更新现有任务
                TaskEntry.Title = TitleTextBox.Text;
                TaskEntry.Priority = PriorityComboBox.SelectedIndex + 1;
                TaskEntry.Status = (TaskStatus)StatusComboBox.SelectedIndex;
                TaskEntry.TaskType = (TaskType)TaskTypeComboBox.SelectedIndex;
                TaskEntry.ProjectTags = new List<string>(_currentProjectTags);
                TaskEntry.CompletedAt = CompletedDatePicker.SelectedDate;
                TaskEntry.Chapters = chapters;
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

        // 任务类型选择改变事件
        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 检查控件是否已经初始化，避免在XAML初始化过程中触发事件导致的空引用异常
            if (ProjectTagsPanel == null || ProjectTagsDisplayPanel == null)
            {
                return;
            }
            UpdateProjectTagsVisibility();
        }

        // 更新项目标签控件的可见性
        private void UpdateProjectTagsVisibility()
        {
            // 检查控件是否已经初始化，避免空引用异常
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

        // 添加项目标签
        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            var tagText = ProjectTagsTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(tagText) && tagText != ProjectTagsTextBox.Tag?.ToString())
            {
                // 分割多个标签（用逗号分隔）
                var tags = tagText.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var tag in tags)
                {
                    var trimmedTag = tag.Trim();
                    if (!string.IsNullOrEmpty(trimmedTag) && !_currentProjectTags.Contains(trimmedTag))
                    {
                        _currentProjectTags.Add(trimmedTag);
                    }
                }
                
                RefreshProjectTagsDisplay();
                ProjectTagsTextBox.Text = "";
            }
        }

        // 移除项目标签
        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                _currentProjectTags.Remove(tag);
                RefreshProjectTagsDisplay();
            }
        }

        // 刷新项目标签显示
        private void RefreshProjectTagsDisplay()
        {
            ProjectTagsItemsControl.ItemsSource = null;
            ProjectTagsItemsControl.ItemsSource = _currentProjectTags;
            
            // 如果是编辑现有任务，初始化标签列表
            if (TaskEntry != null && TaskEntry.ProjectTags != null)
            {
                _currentProjectTags = new List<string>(TaskEntry.ProjectTags);
                ProjectTagsItemsControl.ItemsSource = _currentProjectTags;
            }
        }

        // 提醒按钮点击事件
        private void ReminderButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个临时任务对象用于显示
            var displayTask = TaskEntry ?? new TaskEntry { Title = TitleTextBox.Text.Trim() }; 
            
            // 确定要使用的提醒设置（优先使用临时的，如果没有则使用任务的）
            var reminderSettings = _tempReminderSettings ?? TaskEntry?.ReminderSettings;

            // 打开提醒设置窗口
            var reminderWindow = new ReminderSettingsWindow(displayTask, reminderSettings);
            reminderWindow.Owner = this;
            bool? result = reminderWindow.ShowDialog();

            if (result == true)
            {
                // 如果用户点击了保存
                if (reminderWindow.IsSaveRequested && reminderWindow.ReminderSettings != null)
                {
                    // 根据情况保存到临时变量或任务对象
                    if (TaskEntry != null)
                    {
                        TaskEntry.ReminderSettings = reminderWindow.ReminderSettings;
                    }
                    else
                    {
                        _tempReminderSettings = reminderWindow.ReminderSettings;
                    }
                    MessageBox.Show("提醒设置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdateReminderInfoDisplay();
                }
            }
        }
        
        /// <summary>
        /// 更新提醒信息显示
        /// </summary>
        private void UpdateReminderInfoDisplay()
        {
            // 获取当前的提醒设置
            var reminderSettings = _tempReminderSettings ?? TaskEntry?.ReminderSettings;
            
            if (reminderSettings != null)
            {
                // 更新显示内容
                string startDateText = "";
                if (reminderSettings.StartDate.HasValue)
                {
                    startDateText = reminderSettings.StartDate.Value.ToString("yyyy-MM-dd");
                    if (reminderSettings.ReminderTime.HasValue)
                    {
                        startDateText += " " + reminderSettings.ReminderTime.Value.ToString("HH:mm");
                    }
                }
                ReminderStartDateText.Text = startDateText;
                
                // 处理不同的提醒方式
                string reminderTypeText = "";
                switch (reminderSettings.ReminderType)
                {
                    case ReminderType.Daily:
                        reminderTypeText = "每日提醒";
                        if (reminderSettings.IntervalDays.HasValue && reminderSettings.IntervalDays.Value > 1)
                        {
                            reminderTypeText += $"（每{reminderSettings.IntervalDays.Value}天）";
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
                            reminderTypeText += $"（每月第{reminderSettings.MonthlyDayNumber.Value}个{dayNames[(int)reminderSettings.MonthlyDayOfWeek.Value]}）";
                        }
                        break;
                    case ReminderType.Yearly:
                        reminderTypeText = "每年提醒";
                        break;
                    case ReminderType.Interval:
                        reminderTypeText = "间隔提醒";
                        if (reminderSettings.IntervalDays.HasValue)
                        {
                            reminderTypeText += $"（每{reminderSettings.IntervalDays.Value}天）";
                        }
                        break;
                }
                
                ReminderTypeText.Text = reminderTypeText;
                
                // 显示下次提醒时间
                string nextReminderText = "";
                if (reminderSettings.NextReminderDate.HasValue)
                {
                    nextReminderText = reminderSettings.NextReminderDate.Value.ToString("yyyy-MM-dd HH:mm");
                }
                NextReminderDateText.Text = nextReminderText;
                
                ReminderStatusText.Text = reminderSettings.IsActive ? "已启用" : "已禁用";
                
                // 显示提醒信息面板
                ReminderInfoPanel.Visibility = Visibility.Visible;
            }
        }
    }
}