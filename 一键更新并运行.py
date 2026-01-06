#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
一键更新并运行脚本
功能：自动版本更新 → 编译 → 运行（简化版）
"""

import os
import re
import subprocess
from datetime import datetime

def update_version():
    """自动更新版本号"""
    version_file = r"D:\cunchu\项目\电子任务助手\DiaryApp\MainWindow.xaml.cs"
    
    try:
        with open(version_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # 查找版本号（支持带V前缀的版本号，如 V0.1.1.1）
        pattern = r'public const string VERSION = "(V?[\d.]+)";'
        match = re.search(pattern, content)
        
        if match:
            version_str = match.group(1)
            version_parts = version_str.split('.')
            
            # 递增版本号
            if len(version_parts) == 4:
                major, minor, patch1, patch2 = map(int, version_parts)
                new_patch2 = patch2 + 1
                new_version = f"{major}.{minor}.{patch1}.{new_patch2}"
            else:
                major, minor, patch = map(int, version_parts)
                new_patch = patch + 1
                new_version = f"{major}.{minor}.{new_patch}"
            
            # 保存更新
            new_content = re.sub(
                pattern, 
                f'public const string VERSION = "{new_version}";',
                content
            )
            
            with open(version_file, 'w', encoding='utf-8') as f:
                f.write(new_content)
            
            print(f"✅ 版本更新：{version_str} → {new_version}")
            return True
        else:
            print("❌ 未找到版本号定义")
            return False
            
    except Exception as e:
        print(f"❌ 版本更新失败：{e}")
        return False

def build_and_run():
    """编译并运行"""
    try:
        # 清理并编译
        print("🔨 编译中...")
        result = subprocess.run(
            ["dotnet", "build", "--configuration", "Debug"], 
            cwd=r"D:\cunchu\项目\电子任务助手\DiaryApp",
            capture_output=True, 
            text=True,
            encoding='utf-8',
            errors='ignore'
        )
        
        if result.returncode == 0:
            print("✅ 编译成功")
            
            # 运行应用
            print("🚀 启动应用...")
            exe_path = r"D:\cunchu\项目\电子任务助手\DiaryApp\bin\Debug\net8.0-windows\DiaryApp.exe"
            
            subprocess.Popen([exe_path])
            print(f"✅ 应用已启动！")
            
            return True
        else:
            print(f"❌ 编译失败：{result.stderr}")
            return False
            
    except Exception as e:
        print(f"❌ 执行失败：{e}")
        return False

def main():
    """主函数"""
    print("="*50)
    print("🚀 一键更新并运行")
    print("="*50)
    
    # 1. 更新版本
    if update_version():
        # 2. 编译并运行
        build_and_run()
    else:
        print("❌ 版本更新失败，跳过编译")
    
    print("="*50)
    input("按Enter键退出...")

if __name__ == "__main__":
    main()