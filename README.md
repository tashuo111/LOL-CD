# 闪现计时器 1.9.1

这是基于 [lkfun/Timer](https://github.com/lkfun/Timer) 修改的独立个人备份，仓库保存当前 1.9.1 源码，便携运行包保存在 GitHub Releases。它不是原作者的官方新版，也不是 League Akari 或 Riot Games 的官方产品。

## 下载与运行 有封号风险  游戏内发送记录如果无法使用 可以用豆包 codex之类AI应用尝试修复

[下载 Windows 64 位便携包](https://github.com/tashuo111/LOL-CD/releases/latest/download/Timer-1.9.1-win-x64.zip) · [SHA-256 校验值](https://github.com/tashuo111/LOL-CD/releases/latest/download/SHA256SUMS.txt)

适用 Windows 10/11 64 位。完整解压到可写文件夹后，直接双击 **Timer.exe**，无需脚本或另装 .NET。请保留整个目录；不要只复制 EXE。窗口标题显示“闪现计时器 1.9.1”。如果提示已运行，请先正常退出旧版。

包内的 `portable.mode` 使程序直接读取并保存同目录的 `settings.json`，不会读取旧电脑个人目录中的配置。请保留这两个文件。

## 当前功能

| 快捷键 | 便携包中的作用 |
| --- | --- |
| Ctrl + 主键盘 1～5 | 记录对应位置或楼层的闪现 |
| X | 松开时尝试发送当前冷却文本 |
| F7 | 生成文本并立即复制 |
| 小键盘 . | 重置比赛时间与记录 |
| 小键盘 + / - | 比赛时间增加 / 减少 5 秒 |
| 小键盘 0 | 显示或隐藏浮窗 |

快捷键可在设置中修改。便携包配置开启游戏发送和语音；可按需要关闭。

- 常规模式对应上路、打野、中路、下路、辅助；文本示例为 `top1145 jug2000`。全小写，位置与恢复时间直接相连，不带 `f`。
- 文本只包含仍在冷却的记录；数字表示预计恢复的**比赛时间**，不是剩余秒数。生成按钮、F7 和“试运行”会生成并复制；预览刷新不会反复改写剪贴板。所有位置就绪时不复制或发送。
- 海克斯大乱斗模式对应 1 楼至 5 楼，文本示例为 `1楼1145 2楼2000`；每楼实际 CD 可手动设置为 0.1～600 秒。切换模式清空旧 CD，保留比赛时间。
- 开启语音后，记录时不播报，到期提示“AD 闪现已就绪”或“1楼闪现已就绪”等；提供测试语音按钮。开启语音开关时会播报开启确认。
- 浮窗显示比赛时间、剩余 CD 与预计恢复时间，可调整位置。

## 已知限制

**X 游戏内发送存在电脑之间的兼容性差异。** 用户已确认在当前电脑可以成功发送，但换另一台电脑失败，具体原因仍待诊断。X 松开后通过普通 Windows 输入 API 尝试 Enter → 文本 → Enter；即使 Windows 返回成功，也不能证明游戏已接收到消息。必须游戏在前台、聊天框原本关闭，发送间隔至少 1 秒。程序不抢焦点、不提权、不绕过输入限制。如果游戏没有打开聊天框，请使用 **F7 → 手动 Enter → Ctrl+V → Enter**。界面的“发送”按钮提供操作提示，实际模拟输入由快捷键触发。

换电脑时请完整复制便携目录，确认发送设置中快捷键为 X、游戏内发送已开启，并且至少有一个正在冷却的记录。按 X 后可查看工具的快捷键状态及同目录 `last-send.json`，区分快捷键未触发、游戏未在前台和输入提交失败。工具与游戏的权限等级不同也可能阻止模拟输入，详见 [Microsoft SendInput 文档](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)。

大乱斗的 **176.47 秒是本版本保留的默认预设**（300÷1.7），不是对当前官方版本、所有装备或海克斯强化组合的最新结论。请以游戏内实际 CD 为准并修改对应楼层的数值。程序不自动识别强化、装备、多充能或刷新效果。恢复时间向上取整到秒，避免提前提示。

常规模式仍沿用原有规则：基础 300 秒、勾选明朗鞋减 30 秒、星界减 15 秒；未更新为当前赛季的急速计算模型。

语音依赖目标电脑的 Windows 语音引擎和音频设备。程序是手动计时器，不读取游戏技能状态。发送诊断保存在配置目录的 `last-send.json`，不记录消息正文。

## 从源码构建

需要 Windows 和 .NET SDK **8.0.425**。`global.json` 固定使用该 SDK 功能带，并允许兼容补丁版本（`rollForward: latestPatch`），不会自动选用 .NET 9/10 SDK。在仓库目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

也可用 `-Dotnet "C:\路径\dotnet.exe"` 指定 SDK。脚本先运行测试，再发布自带运行时的 win-x64 程序，复制 `packaging/` 中的便携配置，并生成 ZIP 和 SHA-256 校验文件。结果位于 `artifacts/日期时间/`，可据此重建运行包。

GitHub Actions 在推送 `main` 或手动运行时，使用 Windows 2022 和 .NET SDK 8.0.425 调用同一脚本。该 Windows 镜像没有中文 SAPI 语音引擎，因此 CI 显式传入 `-SkipChineseVoiceTest`，仅执行其余 **31 项**测试；脚本会输出警告，且任何已执行测试失败都会停止发布。两个资产均上传成功后才发布 `v1.9.1`，已有正式发布会跳过，已有未完成草稿会报错并保留。源码提交不包含本地旧 `downloads/` 目录中的压缩包，下载请使用上面的 Releases 链接。

源码基础默认值仍为 F1～F5 记录、F6 发送，且默认关闭语音和发送；直接执行 `dotnet run` 不等于便携包配置。请使用上述脚本生成带 **Ctrl+1～5 / X / F7** 配置的完整包。没有 `portable.mode` 时，程序使用 `%LOCALAPPDATA%\LkfunTimer\settings.json`，首次运行可导入 `conf.ini`。

本机带中文语音引擎的验证结果为 **32/32 通过**，其中包含中文语音音频生成测试。脚本默认仍执行全部 32 项；只有显式传入 `-SkipChineseVoiceTest` 才排除该项，中文语音未因此获得 CI 验证。其余测试覆盖计时与文本、快捷键、设置和发送流程；测试通过不代表真实 LOL 游戏内发送成功。

## 来源与参考

- 原始项目：[lkfun/Timer](https://github.com/lkfun/Timer)。保留原项目相关源码和资源署名。
- 发送实现参考：[LeagueAkari/LeagueAkari 的游戏内发送模块](https://github.com/LeagueAkari/LeagueAkari/tree/dev/src/main/shards/in-game-send)及 [Windows 输入实现](https://github.com/LeagueAkari/LeagueAkari/blob/dev/native/win32-x64/src/input/input.cc)。

此备份没有新增许可证，也不对上游代码或资源授予额外权利；相关权利以原始来源的实际声明为准。
