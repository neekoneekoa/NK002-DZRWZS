#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
自动版本更新并打包脚本
功能：1. 自动递增版本号 2. 重新编译程序 3. 生成Release版本
"""

import os
import re
import subprocess
import shutil
from datetime import datetime

def read_version_file():
    """读取当前版本信息"""
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    version_file = os.path.join(base_dir, "DiaryApp", "MainWindow.xaml.cs")
    
    with open(version_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    return content

def increment_version(content):
    """自动递增版本号"""
    # 查找版本号模式 - 支持带V前缀的版本号（如 V0.1.1.1）
    pattern = r'public const string VERSION = "(V?[\d.]+)";'
    match = re.search(pattern, content)
    
    if match:
        version_str = match.group(1)
        version_parts = version_str.split('.')
        
        # 支持3位和4位版本号格式
        if len(version_parts) == 3:
            major, minor, patch = map(int, version_parts)
            # 递增补丁版本号 (patch)
            new_patch = patch + 1
            new_version = f"{major}.{minor}.{new_patch}"
        elif len(version_parts) == 4:
            major, minor, patch1, patch2 = map(int, version_parts)
            # 递增最后一个补丁版本号
            new_patch2 = patch2 + 1
            new_version = f"{major}.{minor}.{patch1}.{new_patch2}"
        else:
            print(f"❌ 不支持的版本号格式：{version_str}")
            return content, version_str
        
        # 替换版本号
        new_content = re.sub(
            pattern, 
            f'public const string VERSION = "{new_version}";',
            content
        )
        
        print(f"📈 版本号更新：{version_str} → {new_version}")
        return new_content, new_version
    else:
        print("❌ 未找到版本号定义")
        return content, "unknown"

def save_version_file(content):
    """保存更新后的版本文件"""
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    version_file = os.path.join(base_dir, "DiaryApp", "MainWindow.xaml.cs")
    
    with open(version_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"✅ 版本信息已保存到：{version_file}")

def clean_project():
    """清理项目"""
    print("🧹 清理项目文件...")
    
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.join(base_dir, "DiaryApp")
    
    result = subprocess.run(
        ["dotnet", "clean", "DiaryApp.csproj"], 
        cwd=project_dir,
        capture_output=True, 
        text=True,
        encoding='utf-8',
        errors='ignore'
    )
    
    if result.returncode == 0:
        print("✅ 项目清理成功")
    else:
        print(f"⚠️ 清理警告：{result.stderr}")
    
    return result.returncode == 0

def build_release():
    """编译Release版本"""
    print("🔨 编译Release版本...")
    
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.join(base_dir, "DiaryApp")
    
    result = subprocess.run(
        ["dotnet", "build", "DiaryApp.csproj", "--configuration", "Release"], 
        cwd=project_dir,
        capture_output=True, 
        text=True,
        encoding='utf-8',
        errors='ignore'
    )
    
    if result.returncode == 0:
        print("✅ Release版本编译成功")
        return True
    else:
        print(f"❌ 编译失败：{result.stderr}")
        return False

def test_run():
    """测试运行"""
    print("🚀 测试运行新版本...")
    
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    exe_path = os.path.join(base_dir, "DiaryApp", "bin", "Release", "net8.0-windows", "DiaryApp.exe")
    
    if not os.path.exists(exe_path):
        print(f"❌ 未找到EXE文件：{exe_path}")
        return False
    
    try:
        # 启动应用程序
        process = subprocess.Popen(
            [exe_path], 
            stdout=subprocess.PIPE, 
            stderr=subprocess.PIPE,
            encoding='utf-8',
            errors='ignore'
        )
        
        print(f"✅ 应用已启动，进程ID: {process.pid}")
        
        # 等待3秒后关闭（测试用途）
        import time
        time.sleep(3)
        process.terminate()
        
        print("✅ 测试运行完成")
        return True
        
    except Exception as e:
        print(f"❌ 测试运行失败：{e}")
        return False

def show_version_info(new_version):
    """显示版本信息"""
    current_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    # 获取当前脚本所在目录
    base_dir = os.path.dirname(os.path.abspath(__file__))
    debug_path = os.path.join(base_dir, "DiaryApp", "bin", "Debug", "net8.0-windows", "DiaryApp.exe")
    release_path = os.path.join(base_dir, "DiaryApp", "bin", "Release", "net8.0-windows", "DiaryApp.exe")
    
    print("\n" + "="*50)
    print("🎉 版本更新完成！")
    print("="*50)
    print(f"📦 新版本号：{new_version}")
    print(f"🕐 更新时间：{current_time}")
    print(f"📁 EXE路径：")
    print(f"   Debug: {debug_path}")
    print(f"   Release: {release_path}")
    print("="*50)

def main():
    """主函数"""
    print("🔄 开始自动版本更新流程...")
    print("-" * 50)
    
    try:
        # 1. 读取版本文件
        content = read_version_file()
        
        # 2. 递增版本号
        new_content, new_version = increment_version(content)
        
        # 3. 保存更新
        save_version_file(new_content)
        
        # 4. 清理项目
        clean_project()
        
        # 5. 编译Release版本
        if build_release():
            # 6. 测试运行
            test_run()
            
            # 7. 显示结果
            show_version_info(new_version)
            
            print("\n🎯 建议操作：")
            print("1. 测试新版本功能是否正常")
            print("2. 使用Release版本进行发布")
            print("3. 备份源代码以防意外")
        else:
            print("❌ 编译失败，请检查代码错误")
            
    except Exception as e:
        print(f"❌ 脚本执行出错：{e}")
        return False
    
    return True

if __name__ == "__main__":
    main()
    input("\n按Enter键退出...")