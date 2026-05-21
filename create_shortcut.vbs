Set WshShell = CreateObject("WScript.Shell")
Set Shortcut = WshShell.CreateShortcut(WshShell.SpecialFolders("Desktop") & "\AAFSS.lnk")
Shortcut.TargetPath = "D:\2026\试点10\AAFSS\launch.bat"
Shortcut.WorkingDirectory = "D:\2026\试点10\AAFSS"
Shortcut.Description = "AAFSS - 声疲劳载荷谱编制系统"
Shortcut.IconLocation = "C:\Windows\System32\imageres.dll,108"
Shortcut.Save()
MsgBox "快捷方式已创建到桌面：AAFSS.lnk", vbInformation, "AAFSS"
