$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$appPath = Join-Path $scriptPath "DiaryApp\bin\Debug\net8.0-windows\DiaryApp.exe"
Start-Process -FilePath $appPath -WindowStyle Normal
