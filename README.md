# 摸鱼阅读

无边框、置顶的小说阅读条。目标框架是 .NET Framework 4.8。

## 目录

```
Cyberloafing.sln
build.bat                 本机编译脚本
src/Cyberloafing/         工程和源码
content/                  默认设置，以及 content/novel 里的示例小说
bin/                      编译结果，不进入版本库
```

## 编译

本机没有 .NET SDK，用仓库里的脚本编译：

```bat
build.bat
bin\Cyberloafing.exe
```

装了 .NET SDK 或 Visual Studio 之后，也可以打开 `Cyberloafing.sln`，或在仓库根目录执行 `dotnet build`。

## 阅读

- 小说放在程序旁边的 `novel` 文件夹，里面的 txt 都会被读到。右键“选择小说”切换，每本的进度分开记在 `progress.ini`。
- 文本可以是 UTF-8、UTF-16 或 GBK。
- 左键单击下一页，拖动挪位置。滚轮翻页，按住 Ctrl 滚轮改字号。
- 方向键、空格也可以翻页。Home 回到开头，End 到末尾。
- 右键菜单：查找、查找下一处（F3）、章节、百分比、复制这一页、自动翻页。
- 自动翻页时，鼠标停在阅读条上会暂停。

## 快捷键

| 按键 | 作用 |
|---|---|
| Ctrl+Alt+右 / 左 | 下一页 / 上一页 |
| Ctrl+Alt+上 / 下 | 加大 / 减小字号 |
| Ctrl+Alt+H | 隐藏或显示 |
| Ctrl+Alt+Q | 退出 |
| Esc | 隐藏 |
| Ctrl+O | 打开文件 |
| Ctrl+F | 查找 |

窗口不出现在任务栏里。隐藏之后，用托盘图标双击，或再按 Ctrl+Alt+H。

## 设置

`settings.ini` 和右键里的“阅读设置”是同一份配置。位置和大小按 100% 缩放来记，打开时会按当前屏幕缩放。

- `translucent=1`：半透明底，浓度由 `opacity` 决定。
- `translucent=0`：背景色被抠掉，只留文字。字色不要和底色相同，否则文字也会被抠掉。
- `size_y` 加大后，一页会排进多行。
- `click_through=1`：鼠标点穿阅读条。这时要用快捷键或托盘菜单翻页。

章节按“第一章”“第2回”这种标题识别。

## 许可

Copyright (C) 2026 noir017

本程序为自由软件，可在 GNU General Public License 第 3 版或（依你的选择）任何更新版本的条款下再发布和修改。详见 [LICENSE](LICENSE)。
