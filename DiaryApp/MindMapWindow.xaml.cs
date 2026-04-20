using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiaryApp
{
    public partial class MindMapWindow : Window
    {
        private MindMapNode _rootNode;
        private Dictionary<string, FrameworkElement> _nodeElements = new Dictionary<string, FrameworkElement>();
        private Dictionary<string, FrameworkElement> _connectionLines = new Dictionary<string, FrameworkElement>();
        private MindMapNode? _selectedNode = null;

        public MindMapWindow(MindMapNode rootNode)
        {
            InitializeComponent();
            _rootNode = rootNode;
            RenderMindMap();
        }

        private void RenderMindMap()
        {
            MindMapCanvas.Children.Clear();
            _nodeElements.Clear();
            _connectionLines.Clear();

            double canvasWidth = MindMapCanvas.Width;
            double canvasHeight = MindMapCanvas.Height;

            double rootX = 80;
            double rootY = canvasHeight / 2;

            CalculateNodePositions(_rootNode, rootX, rootY, 0);
            RenderNodeVisuals(_rootNode);
        }

        private double CalculateNodePositions(MindMapNode node, double x, double y, int level)
        {
            node.X = x;
            node.Y = y;

            if (!node.IsExpanded || node.Children.Count == 0)
            {
                return y;
            }

            double horizontalSpacing = 200;
            double verticalSpacing = 80;

            double startY = y - (node.Children.Count - 1) * verticalSpacing / 2;
            
            for (int i = 0; i < node.Children.Count; i++)
            {
                double childY = startY + i * verticalSpacing;
                CalculateNodePositions(node.Children[i], x + horizontalSpacing, childY, level + 1);
            }

            return y;
        }

        private void RenderNodeVisuals(MindMapNode node)
        {
            Border nodeBorder = CreateNodeBorder(node);
            
            // 强制计算节点大小
            nodeBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            nodeBorder.Arrange(new Rect(0, 0, nodeBorder.DesiredSize.Width, nodeBorder.DesiredSize.Height));
            
            double nodeHeight = nodeBorder.ActualHeight;
            Canvas.SetLeft(nodeBorder, node.X);
            Canvas.SetTop(nodeBorder, node.Y - nodeHeight / 2);
            MindMapCanvas.Children.Add(nodeBorder);
            _nodeElements[node.Id] = nodeBorder;

            if (node.IsExpanded && node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    Line connectionLine = CreateConnectionLine(node.X + nodeBorder.ActualWidth, node.Y, child.X, child.Y);
                    MindMapCanvas.Children.Insert(0, connectionLine);
                    _connectionLines[$"{node.Id}_{child.Id}"] = connectionLine;

                    RenderNodeVisuals(child);
                }
            }
        }

        private Border CreateNodeBorder(MindMapNode node)
        {
            Style nodeStyle = node.IsRoot ? (Style)FindResource("RootNodeStyle") : (Style)FindResource("ChildNodeStyle");
            
            Border border = new Border
            {
                Style = nodeStyle,
                Tag = node
            };

            Grid grid = new Grid();
            
            StackPanel mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBlock textBlock = new TextBlock
            {
                Text = node.Content,
                Foreground = Brushes.White,
                FontSize = node.IsRoot ? 16 : 14,
                FontWeight = node.IsRoot ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 120,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Button expandButton = new Button
            {
                Content = node.IsExpanded ? "▼" : "▶",
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

            Button addButton = new Button
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

            Button editButton = new Button
            {
                Content = "✏",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 3, 0),
                Tag = node
            };
            editButton.Click += EditNodeButton_Click;

            Button deleteButton = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 12,
                Cursor = Cursors.Hand,
                Tag = node,
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

            border.MouseLeftButtonDown += (s, e) =>
            {
                _selectedNode = node;
                e.Handled = true;
            };

            return border;
        }

        private Line CreateConnectionLine(double x1, double y1, double x2, double y2)
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
                -1, -1);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var newNode = new MindMapNode
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = content,
                    IsExpanded = true
                };
                parent.Children.Add(newNode);
                parent.UpdatedAt = DateTime.Now;
                RenderMindMap();
            }
        }

        private void EditNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MindMapNode node)
            {
                string newContent = Microsoft.VisualBasic.Interaction.InputBox(
                    "请输入新的内容：",
                    "修改分支",
                    node.Content,
                    -1, -1);

                if (!string.IsNullOrWhiteSpace(newContent))
                {
                    node.Content = newContent;
                    node.UpdatedAt = DateTime.Now;
                    RenderMindMap();
                }
            }
        }

        private void DeleteNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MindMapNode node)
            {
                string message = $"确定要删除分支 \"{node.Content}\" 及其所有子分支吗？";
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
        }

        private int CountAllChildren(MindMapNode node)
        {
            int count = node.Children.Count;
            foreach (var child in node.Children)
            {
                count += CountAllChildren(child);
            }
            return count;
        }

        private bool DeleteNode(MindMapNode parent, string nodeId)
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
            MessageBox.Show("个人数据已保存！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
