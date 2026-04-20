using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiaryApp
{
    public partial class MindMapWindow : Window
    {
        private readonly MindMapNode _rootNode;
        private readonly Dictionary<string, FrameworkElement> _nodeElements = new();
        private readonly Dictionary<string, FrameworkElement> _connectionLines = new();
        private MindMapNode? _selectedNode;

        public MindMapWindow(MindMapNode rootNode)
        {
            InitializeComponent();
            _rootNode = rootNode ?? new MindMapNode
            {
                Content = "个人资料",
                IsRoot = true,
                IsExpanded = true
            };

            _rootNode.IsRoot = true;
            _rootNode.IsExpanded = true;
            RenderMindMap();
        }

        private void RenderMindMap()
        {
            MindMapCanvas.Children.Clear();
            _nodeElements.Clear();
            _connectionLines.Clear();

            double canvasHeight = MindMapCanvas.Height;
            double rootX = 80;
            double rootY = canvasHeight / 2;

            CalculateNodePositions(_rootNode, rootX, rootY);
            RenderNodeVisuals(_rootNode);
        }

        private void CalculateNodePositions(MindMapNode node, double x, double y)
        {
            node.X = x;
            node.Y = y;

            if (!node.IsExpanded || node.Children.Count == 0)
            {
                return;
            }

            const double horizontalSpacing = 220;
            const double verticalSpacing = 90;
            double startY = y - (node.Children.Count - 1) * verticalSpacing / 2;

            for (int i = 0; i < node.Children.Count; i++)
            {
                double childY = startY + i * verticalSpacing;
                CalculateNodePositions(node.Children[i], x + horizontalSpacing, childY);
            }
        }

        private void RenderNodeVisuals(MindMapNode node)
        {
            Border nodeBorder = CreateNodeBorder(node);
            nodeBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            nodeBorder.Arrange(new Rect(0, 0, nodeBorder.DesiredSize.Width, nodeBorder.DesiredSize.Height));

            double nodeWidth = nodeBorder.DesiredSize.Width;
            double nodeHeight = nodeBorder.DesiredSize.Height;

            Canvas.SetLeft(nodeBorder, node.X);
            Canvas.SetTop(nodeBorder, node.Y - nodeHeight / 2);
            MindMapCanvas.Children.Add(nodeBorder);
            _nodeElements[node.Id] = nodeBorder;

            if (!node.IsExpanded || node.Children.Count == 0)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                var line = CreateConnectionLine(node.X + nodeWidth, node.Y, child.X, child.Y);
                MindMapCanvas.Children.Insert(0, line);
                _connectionLines[$"{node.Id}_{child.Id}"] = line;
                RenderNodeVisuals(child);
            }
        }

        private Border CreateNodeBorder(MindMapNode node)
        {
            Style nodeStyle = node.IsRoot
                ? (Style)FindResource("RootNodeStyle")
                : (Style)FindResource("ChildNodeStyle");

            var border = new Border
            {
                Style = nodeStyle,
                Tag = node
            };

            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var textBlock = new TextBlock
            {
                Text = node.Content,
                Foreground = Brushes.White,
                FontSize = node.IsRoot ? 16 : 14,
                FontWeight = node.IsRoot ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 150,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var expandButton = new Button
            {
                Content = node.IsExpanded ? ">" : "v",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 10,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 3, 0),
                Tag = node,
                Visibility = node.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed
            };
            expandButton.Click += ExpandCollapseButton_Click;

            var addButton = new Button
            {
                Content = "+",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 14,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 3, 0),
                Tag = node
            };
            addButton.Click += AddChildButton_Click;

            var editButton = new Button
            {
                Content = "编",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 3, 0),
                Tag = node,
                ToolTip = "编辑节点"
            };
            editButton.Click += EditNodeButton_Click;

            var deleteButton = new Button
            {
                Content = "删",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Tag = node,
                ToolTip = "删除节点",
                Visibility = node.IsRoot ? Visibility.Collapsed : Visibility.Visible
            };
            deleteButton.Click += DeleteNodeButton_Click;

            buttonPanel.Children.Add(expandButton);
            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(deleteButton);

            mainPanel.Children.Add(textBlock);
            mainPanel.Children.Add(buttonPanel);

            border.Child = mainPanel;
            border.MouseLeftButtonDown += (_, e) =>
            {
                _selectedNode = node;
                e.Handled = true;
            };

            return border;
        }

        private static Line CreateConnectionLine(double x1, double y1, double x2, double y2)
        {
            return new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(Color.FromRgb(108, 92, 231)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 }
            };
        }

        private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MindMapNode node)
            {
                node.IsExpanded = !node.IsExpanded;
                node.UpdatedAt = DateTime.Now;
                RenderMindMap();
            }
        }

        private void AddRootChildButton_Click(object sender, RoutedEventArgs e)
        {
            AddChildNode(_rootNode);
        }

        private void AddChildButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MindMapNode parent)
            {
                AddChildNode(parent);
            }
        }

        private void AddChildNode(MindMapNode parent)
        {
            string content = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入分支内容：",
                "添加分支",
                "",
                -1,
                -1);

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var newNode = new MindMapNode
            {
                Id = Guid.NewGuid().ToString(),
                Content = content.Trim(),
                IsExpanded = true,
                UpdatedAt = DateTime.Now
            };

            parent.Children.Add(newNode);
            parent.IsExpanded = true;
            parent.UpdatedAt = DateTime.Now;
            RenderMindMap();
        }

        private void EditNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not MindMapNode node)
            {
                return;
            }

            string newContent = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入新的内容：",
                "修改分支",
                node.Content,
                -1,
                -1);

            if (string.IsNullOrWhiteSpace(newContent))
            {
                return;
            }

            node.Content = newContent.Trim();
            node.UpdatedAt = DateTime.Now;
            RenderMindMap();
        }

        private void DeleteNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not MindMapNode node)
            {
                return;
            }

            string message = $"确定要删除分支“{node.Content}”及其所有子分支吗？";
            if (node.Children.Count > 0)
            {
                message += $"\n\n将同时删除 {CountAllChildren(node)} 个子分支。";
            }

            var result = MessageBox.Show(
                message,
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteNode(_rootNode, node.Id);
                RenderMindMap();
            }
        }

        private static int CountAllChildren(MindMapNode node)
        {
            int count = node.Children.Count;
            foreach (var child in node.Children)
            {
                count += CountAllChildren(child);
            }

            return count;
        }

        private static bool DeleteNode(MindMapNode parent, string nodeId)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                if (parent.Children[i].Id == nodeId)
                {
                    parent.Children.RemoveAt(i);
                    parent.UpdatedAt = DateTime.Now;
                    return true;
                }

                if (DeleteNode(parent.Children[i], nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _rootNode.UpdatedAt = DateTime.Now;
            MessageBox.Show("思维导图已保存。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "是否保存更改？",
                "确认关闭",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _rootNode.UpdatedAt = DateTime.Now;
                DialogResult = true;
                Close();
            }
            else if (result == MessageBoxResult.No)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
