import sys
import json
import os
from datetime import datetime
from PyQt6.QtWidgets import (QApplication, QMainWindow, QWidget, QVBoxLayout, 
                             QHBoxLayout, QListWidget, QTextEdit, QLineEdit, 
                             QPushButton, QLabel, QMessageBox, QSplitter, 
                             QListWidgetItem, QFrame)
from PyQt6.QtCore import Qt, QSize
from PyQt6.QtGui import QFont, QIcon, QColor

# --- 数据管理 ---
DATA_FILE = "diaries.json"

def load_diaries():
    if not os.path.exists(DATA_FILE):
        return []
    try:
        with open(DATA_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    except:
        return []

def save_diaries(diaries):
    with open(DATA_FILE, "w", encoding="utf-8") as f:
        json.dump(diaries, f, ensure_ascii=False, indent=2)

# --- 样式表 (QSS) ---
STYLESHEET = """
QMainWindow {
    background-color: #f3f4f6;
}

/* 左侧列表区域 */
QListWidget {
    background-color: #ffffff;
    border: none;
    border-right: 1px solid #e5e7eb;
    outline: none;
    padding: 10px;
}

QListWidget::item {
    background-color: #ffffff;
    border-bottom: 1px solid #f3f4f6;
    padding: 15px;
    margin-bottom: 5px;
    border-radius: 8px;
    color: #374151;
}

QListWidget::item:selected {
    background-color: #e0e7ff; /* Indigo 100 */
    color: #4338ca; /* Indigo 700 */
    border: 1px solid #c7d2fe;
}

QListWidget::item:hover {
    background-color: #f9fafb;
}

/* 右侧编辑区域 */
QWidget#RightPanel {
    background-color: #ffffff;
}

QLineEdit {
    border: none;
    border-bottom: 2px solid #e5e7eb;
    padding: 10px;
    font-size: 24px;
    font-weight: bold;
    color: #1f2937;
    background-color: transparent;
}

QLineEdit:focus {
    border-bottom: 2px solid #6366f1; /* Indigo 500 */
}

QTextEdit {
    border: none;
    padding: 15px;
    font-size: 16px;
    line-height: 1.6;
    color: #4b5563;
    background-color: transparent;
}

/* 按钮样式 */
QPushButton {
    background-color: #6366f1; /* Indigo 500 */
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 6px;
    font-weight: bold;
    font-size: 14px;
}

QPushButton:hover {
    background-color: #4f46e5; /* Indigo 600 */
}

QPushButton:pressed {
    background-color: #4338ca; /* Indigo 700 */
}

QPushButton#DeleteButton {
    background-color: #ef4444; /* Red 500 */
}

QPushButton#DeleteButton:hover {
    background-color: #dc2626; /* Red 600 */
}

/* 标签样式 */
QLabel#DateLabel {
    color: #9ca3af;
    font-size: 12px;
    margin-bottom: 10px;
}

QLabel#WelcomeLabel {
    color: #9ca3af;
    font-size: 18px;
}
"""

# --- 主窗口 ---
class DiaryApp(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("我的电子日记")
        self.resize(1000, 700)
        
        # 初始化数据
        self.diaries = load_diaries()
        self.current_diary_index = -1
        
        # 设置 UI
        self.setup_ui()
        self.apply_styles()
        self.refresh_list()

    def setup_ui(self):
        # 主布局
        main_widget = QWidget()
        self.setCentralWidget(main_widget)
        main_layout = QHBoxLayout(main_widget)
        main_layout.setContentsMargins(0, 0, 0, 0)
        main_layout.setSpacing(0)

        # 分割器 (左侧列表，右侧内容)
        splitter = QSplitter(Qt.Orientation.Horizontal)
        splitter.setHandleWidth(1)
        
        # --- 左侧面板 ---
        left_panel = QWidget()
        left_layout = QVBoxLayout(left_panel)
        left_layout.setContentsMargins(0, 0, 0, 0)
        left_layout.setSpacing(0)
        
        # 顶部标题栏 (左侧)
        left_header = QFrame()
        left_header.setStyleSheet("background-color: #f9fafb; border-right: 1px solid #e5e7eb; padding: 15px;")
        left_header_layout = QHBoxLayout(left_header)
        app_title = QLabel("📚 日记本")
        app_title.setFont(QFont("Microsoft YaHei", 12, QFont.Weight.Bold))
        app_title.setStyleSheet("color: #4b5563;")
        
        new_btn = QPushButton("+ 新建")
        new_btn.setCursor(Qt.CursorShape.PointingHandCursor)
        new_btn.clicked.connect(self.create_new_diary)
        
        left_header_layout.addWidget(app_title)
        left_header_layout.addStretch()
        left_header_layout.addWidget(new_btn)
        
        # 日记列表
        self.diary_list = QListWidget()
        self.diary_list.currentRowChanged.connect(self.load_diary_content)
        
        left_layout.addWidget(left_header)
        left_layout.addWidget(self.diary_list)
        
        # --- 右侧面板 ---
        self.right_panel = QWidget()
        self.right_panel.setObjectName("RightPanel")
        right_layout = QVBoxLayout(self.right_panel)
        right_layout.setContentsMargins(30, 30, 30, 30)
        right_layout.setSpacing(15)
        
        # 编辑器区域
        self.title_edit = QLineEdit()
        self.title_edit.setPlaceholderText("在这里输入标题...")
        
        self.date_label = QLabel()
        self.date_label.setObjectName("DateLabel")
        
        self.content_edit = QTextEdit()
        self.content_edit.setPlaceholderText("今天发生了什么？写下来吧...")
        
        # 底部按钮区
        btn_layout = QHBoxLayout()
        
        self.save_btn = QPushButton("保存日记")
        self.save_btn.setCursor(Qt.CursorShape.PointingHandCursor)
        self.save_btn.clicked.connect(self.save_current_diary)
        self.save_btn.setFixedSize(120, 40)
        
        self.delete_btn = QPushButton("删除")
        self.delete_btn.setObjectName("DeleteButton")
        self.delete_btn.setCursor(Qt.CursorShape.PointingHandCursor)
        self.delete_btn.clicked.connect(self.delete_current_diary)
        self.delete_btn.setFixedSize(80, 40)
        
        btn_layout.addStretch()
        btn_layout.addWidget(self.delete_btn)
        btn_layout.addWidget(self.save_btn)
        
        right_layout.addWidget(self.title_edit)
        right_layout.addWidget(self.date_label)
        right_layout.addWidget(self.content_edit)
        right_layout.addLayout(btn_layout)
        
        # 初始状态隐藏编辑器，显示欢迎语
        self.editor_widgets = [self.title_edit, self.date_label, self.content_edit, self.save_btn, self.delete_btn]
        self.welcome_label = QLabel("选择一篇日记查看，或者点击“新建”开始记录")
        self.welcome_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.welcome_label.setObjectName("WelcomeLabel")
        right_layout.addWidget(self.welcome_label)
        
        self.toggle_editor(False)

        # 添加到分割器
        splitter.addWidget(left_panel)
        splitter.addWidget(self.right_panel)
        splitter.setStretchFactor(0, 1)
        splitter.setStretchFactor(1, 3)
        
        main_layout.addWidget(splitter)

    def apply_styles(self):
        self.setStyleSheet(STYLESHEET)
        # 设置字体
        font = QFont("Microsoft YaHei", 10)
        self.setFont(font)

    def toggle_editor(self, show):
        for w in self.editor_widgets:
            w.setVisible(show)
        self.welcome_label.setVisible(not show)

    def refresh_list(self):
        self.diary_list.clear()
        # 按时间倒序
        sorted_diaries = sorted(self.diaries, key=lambda x: x['created_at'], reverse=True)
        
        for diary in sorted_diaries:
            # 创建列表项
            title = diary.get('title', '无标题')
            date_str = diary.get('created_at', '')[:10]
            content_preview = diary.get('content', '').replace('\n', ' ')[:30]
            
            item_text = f"{title}\n{date_str} - {content_preview}..."
            item = QListWidgetItem(item_text)
            # 存储真实数据的 ID 或索引，这里简单起见存储 ID
            item.setData(Qt.ItemDataRole.UserRole, diary['id'])
            self.diary_list.addItem(item)

    def create_new_diary(self):
        self.diary_list.clearSelection()
        self.current_diary_index = -1
        self.title_edit.clear()
        self.content_edit.clear()
        self.date_label.setText(datetime.now().strftime("%Y-%m-%d %H:%M"))
        self.title_edit.setFocus()
        self.toggle_editor(True)

    def load_diary_content(self, row):
        if row < 0:
            return
            
        item = self.diary_list.item(row)
        diary_id = item.data(Qt.ItemDataRole.UserRole)
        
        # 查找对应日记
        diary = next((d for d in self.diaries if d['id'] == diary_id), None)
        if diary:
            self.current_diary_index = self.diaries.index(diary)
            self.title_edit.setText(diary.get('title', ''))
            self.content_edit.setText(diary.get('content', ''))
            self.date_label.setText(diary.get('created_at', ''))
            self.toggle_editor(True)

    def save_current_diary(self):
        title = self.title_edit.text().strip()
        content = self.content_edit.toPlainText().strip()
        
        if not title and not content:
            QMessageBox.warning(self, "提示", "写点什么再保存吧！")
            return

        now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        
        if self.current_diary_index == -1:
            # 新增
            new_diary = {
                "id": str(datetime.now().timestamp()),
                "title": title if title else "无标题",
                "content": content,
                "created_at": now,
                "updated_at": now
            }
            self.diaries.append(new_diary)
        else:
            # 更新
            self.diaries[self.current_diary_index]['title'] = title
            self.diaries[self.current_diary_index]['content'] = content
            self.diaries[self.current_diary_index]['updated_at'] = now
            
        save_diaries(self.diaries)
        self.refresh_list()
        
        # 恢复选中状态（如果是新增，选中第一个；如果是编辑，保持选中）
        if self.current_diary_index == -1:
             self.diary_list.setCurrentRow(0)
        else:
             # 因为列表刷新了，需要重新找位置，这里简化处理，不做复杂定位
             pass
             
        QMessageBox.information(self, "成功", "日记已保存")

    def delete_current_diary(self):
        if self.current_diary_index == -1:
            return
            
        reply = QMessageBox.question(self, '确认删除', 
                                   "确定要删除这篇日记吗？此操作不可恢复。",
                                   QMessageBox.StandardButton.Yes | QMessageBox.StandardButton.No, 
                                   QMessageBox.StandardButton.No)

        if reply == QMessageBox.StandardButton.Yes:
            del self.diaries[self.current_diary_index]
            save_diaries(self.diaries)
            self.refresh_list()
            self.current_diary_index = -1
            self.toggle_editor(False)

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = DiaryApp()
    window.show()
    sys.exit(app.exec())
