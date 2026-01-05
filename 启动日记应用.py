#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
日记应用启动器 - Python版本
使用Python脚本启动.NET应用，绕过批处理文件问题
"""

import os
import subprocess
import sys
from pathlib import Path

def find_app_path():
    """查找应用路径 - 优先使用Debug版本（最新修复）"""
    possible_paths = [
        # 优先使用Debug版本（包含最新修复）
        "D:\\cunchu\\项目\\电子任务助手\\DiaryApp\\bin\\Debug\\net8.0-windows\\DiaryApp.exe",
        "D:\\cunchu\\项目\\电子任务助手\\DiaryApp\\bin\\Release\\net8.0-windows\\DiaryApp.exe"
    ]
    
    for path in possible_paths:
        if os.path.exists(path):
            return path
    return None

def check_dotnet():
    """检查.NET环境"""
    try:
        result = subprocess.run(['dotnet', '--version'], capture_output=True, text=True)
        if result.returncode == 0:
            print(f"✓ 找到.NET版本: {result.stdout.strip()}")
            return True
    except FileNotFoundError:
        print("✗ 未找到.NET")
        return False
    return False

def main():
    print("=" * 50)
    print("    日记应用启动器 - Python版本")
    print("=" * 50)
    print()
    
    # 检查.NET环境
    print("1. 检查.NET环境...")
    if not check_dotnet():
        print("✗ 错误：未找到.NET运行时")
        print("请安装.NET 8.0 Desktop Runtime")
        print("下载地址: https://dotnet.microsoft.com/download/dotnet/8.0")
        input("按Enter键退出...")
        return
    
    # 查找应用路径
    print("\n2. 查找应用文件...")
    app_path = find_app_path()
    
    if not app_path:
        print("✗ 错误：找不到DiaryApp.exe")
        print("请确保应用已编译")
        input("按Enter键退出...")
        return
    
    print(f"✓ 找到应用: {app_path}")
    
    # 启动应用
    print("\n3. 启动应用...")
    try:
        print("正在启动应用窗口...")
        process = subprocess.Popen([app_path], 
                                 stdout=subprocess.PIPE, 
                                 stderr=subprocess.PIPE)
        print("✓ 应用已启动！")
        print(f"  进程ID: {process.pid}")
        print("\n请查看桌面上的应用窗口")
        print("如果应用没有出现，请检查杀毒软件")
        
    except Exception as e:
        print(f"✗ 启动失败: {e}")
        input("按Enter键退出...")
        return
    
    print("\n" + "=" * 50)
    print("应用正在运行中...")
    print("关闭此窗口不会影响应用运行")
    input("按Enter键退出...")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n用户取消操作")
    except Exception as e:
        print(f"\n\n发生错误: {e}")
        input("按Enter键退出...")