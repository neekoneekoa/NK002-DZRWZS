using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiaryApp
{
    public partial class CheckInLogWindow : Window
    {
        public CheckInLogWindow(CheckInProject project, List<CheckInEntry> checkIns)
        {
            InitializeComponent();
            
            if (project != null)
            {
                ProjectTitleText.Text = project.Name;
            }
            
            if (checkIns != null)
            {
                StatsText.Text = $"累计打卡 {checkIns.Count} 次";

                var viewModels = checkIns
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CheckInLogViewModel(c))
                    .ToList();
                LogItemsControl.ItemsSource = viewModels;
            }
        }
    }

    public class CheckInLogViewModel
    {
        private CheckInEntry _entry;

        public CheckInLogViewModel(CheckInEntry entry)
        {
            _entry = entry;
        }

        public string DateStr => _entry.CreatedAt.ToString("yyyy年MM月dd日 HH:mm");
        public string Notes => _entry.Notes;
        public List<string> Photos => _entry.Photos;
        public bool HasNotes => !string.IsNullOrEmpty(_entry.Notes);
        public bool HasPhotos => _entry.Photos != null && _entry.Photos.Any();
    }
}