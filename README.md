# check in, please — Unity Demo

本目录中的 `UnityDemo` 是 Unity 6.3 LTS（6000.3.28f1）项目。v0.2 将已确定的灰暗美术设计接入三日可玩原型：固定斜俯视的绘制场景、实时三维符号人物、冷灰绿室内、暗红政治宣传与纸质文书界面。

## 启动

双击根目录的 `Play-Demo.cmd`，或运行 `Builds/Windows/CheckInPlease.exe`。新版分发包为 `Builds/CheckInPlease-Demo-v0.2-Windows.zip`，解压后运行其中的 exe。请保留同目录中的 `CheckInPlease_Data`、`UnityPlayer.dll` 和其他运行库，不能只复制 exe。

编辑工程：在 Unity Hub 中添加本目录的 `UnityDemo` 文件夹，使用 6000.3.28f1 打开。打开 `Assets/CheckInPlease/Scenes/Hotel.unity` 并点击 Play。如果尚未生成场景，可在菜单 **Check In Please → Create Demo Scene** 创建。

软件位置：
- 编辑器：`F:\CheckInPleaseTools\Unity\6000.3.28f1\Editor\Unity.exe`
- Hub：`F:\CheckInPleaseTools\UnityHub\Unity Hub.exe`
- 安装包与日志：`F:\CheckInPleaseTools\Downloads`、`F:\CheckInPleaseTools\Logs`

Unity 许可证需要通过 Hub 登录并激活。此项目不包含或绕过许可证。

## 可玩流程

1. 晨间阅读新规，支付 20 现金与 2 配给。
2. 每日审查三名住客。比较身份号、有效期、劳动许可及访客计划。可询问、批准入住并选房、拒绝。
3. 处理完来客后开始夜班。住客外出统一点名，此时可进入已入住客房。
4. 搜查书桌和行李。每夜 4 次行动；同一来源只消耗一次，自动记录线索。查看时切换到平面视角，可翻面。
5. 进入办公室，选择同一住客的线索。两个独立来源可举报换取配给与信任；也可外泄换钱并增加暴露、封存到以后或销毁。
6. 结束审计进入次日。违规放行的罚款、举报回执和外泄后果会延迟到次日。第三日结束后根据经营状况给出结局。

鼠标点击地面移动，点击标签自动走近互动；地图按钮切换区域；J 打开日志；Esc 返回或暂停。每次重要操作自动存档。标题页的“开始新游戏”会要求再次点击确认覆盖现有存档。

存档：`%USERPROFILE%\AppData\LocalLow\Northern District\Check In Please\hotel-save-v1.json`，上一次文件保留为 `.bak`。自动验证模式不会写入玩家存档。

## 工程结构

- `Scripts/Core/GameModel.cs`：独立于 Unity 的状态机、九名住客、证据来源、规则与延迟后果。
- `Scripts/WorldView.cs`：大厅、客房、办公室的绘制背景，实时三维人物及证件肖像；按画面校准的行走区域与网格寻路。
- `Scripts/DemoGame.cs`：交互、存读档、场景切换、环境音及自动运行验证。
- `Scripts/DemoUI.cs`：中文 HUD、审查、物品、档案、日志、结局与暂停界面。
- `Resources/HotelMatte.shader`：内置渲染管线的哑光阴影材质。
- `Resources/HotelPlate.shader`、`HotelOverlay.shader`：场景背景与人物接触阴影。
- `Resources/Art/`：可直接运行的环境和纸张素材，已放入工程，不依赖在线服务。
- `Editor/DemoBuilder.cs`：场景生成与 Windows Mono 构建。

v0.2 采用固定镜头的 2.5D 表现：用原概念图制作无界面、无人物的背景层，叠加可移动的实时三维人物、地面接触阴影与真实交互。大厅和客房保留概念图构图；办公室是同风格新素材。证件上的姓名、编号、规则、线索和处置按钮均为运行时内容。场景家具目前是绘制背景的一部分，不能旋转镜头或自由搬动；人物仍为程序建模与简单步行动画。

原概念图保留在 `output/gameplay-concepts-v2-dark`。素材来源与生成提示词记录在 `output/runtime-art-v2/manifest.json`。旧版实机截图保留在 `Builds/Validation-v0.1`，新版实机截图由测试生成到 `Builds/Windows/Validation`。

当前范围不含完整长篇剧情、五阵营系统、自由对话、监控安装、战斗或正式配音。三个客房复用布局；玩法无联网依赖，代码不接入在线服务。中文默认使用 Windows 系统字体 Microsoft YaHei，未复制分发商业字体。

## 构建与检查

PowerShell 从本目录运行：

```powershell
dotnet run --project Tests/CheckInPlease.ModelTests.csproj
.\Build-Demo.ps1
.\Builds\Windows\CheckInPlease.exe -demoSmokeTest -logFile smoke-player.log
```

第一项检查关键经济与流程约束。第二项使用 F 盘编辑器批量创建场景并构建。为避开 Unity Linker 对路径中英文逗号的解析问题，脚本将源工程同步到 `F:\CheckInPleaseTools\BuildWorkspace\UnityDemo`，构建成功后再将成品复制回本目录；不移动源工程。第三项在真实 Unity Player 中走完三天、截图并退出，输出位于 `Builds/Windows/Validation`，退出码 0 表示流程检查通过。它是独立验证模式，不修改正常存档。
