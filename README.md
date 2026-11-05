# check in, please — Unity Demo

本目录中的 `UnityDemo` 是 Unity 6.3 LTS（6000.3.28f1）项目。v0.4.1 采用侧上方斜俯视的三层实时三维剖顶建筑，任意时刻只显示一层，并支持 WASD 与鼠标移动：一楼前台和办公室电脑，二楼 201–203、三楼 301–303 六间客房，由两段折返楼梯连通。保留摄像头局部视野、开门观察与柔和遮黑边界。

## 启动

双击根目录的 `Play-Demo.cmd`，或运行 `Builds/Windows/CheckInPlease.exe`。新版分发包为 `Builds/CheckInPlease-Demo-v0.4.1-Windows.zip`，解压后运行其中的 exe。请保留同目录中的 `CheckInPlease_Data`、`UnityPlayer.dll` 和其他运行库，不能只复制 exe。

编辑工程：在 Unity Hub 中添加本目录的 `UnityDemo` 文件夹，使用 6000.3.28f1 打开。打开 `Assets/CheckInPlease/Scenes/Hotel.unity` 并点击 Play。如果尚未生成场景，可在菜单 **Check In Please → Create Demo Scene** 创建。

软件位置：
- 编辑器：`F:\CheckInPleaseTools\Unity\6000.3.28f1\Editor\Unity.exe`
- Hub：`F:\CheckInPleaseTools\UnityHub\Unity Hub.exe`
- 安装包与日志：`F:\CheckInPleaseTools\Downloads`、`F:\CheckInPleaseTools\Logs`

Unity 许可证需要通过 Hub 登录并激活。此项目不包含或绕过许可证。

## 可玩流程

1. 晨间阅读新规，支付 20 现金与 2 配给。
2. 走到一楼前台，每日审查三名来客。比较证件与条例，可询问、拒绝，或批准入住并分配到二、三楼的六间客房。
3. 处理完来客后开始夜班。沿西侧楼梯进入二楼或三楼；住客外出统一点名，此时可开门进入客房。
4. 搜查书桌和行李。每夜 4 次行动；同一来源只消耗一次，自动记录线索。查看时切换到平面视角，可翻面。
5. 沿楼梯返回一楼，走到办公室的电脑前，选择同一住客的两个独立来源，点击「电脑提交举报」。仍可封存、销毁或外泄材料。
6. 结束审计进入次日。违规放行的罚款、举报回执和外泄后果会延迟到次日。第三日结束后根据经营状况给出结局。

WASD 按画面方向移动（W 上、S 下、A 左、D 右），也可鼠标点击地面寻路，点击标签自动走近互动。WASD 会取消待执行的鼠标路线和交互；墙壁、家具、关闭的房门与楼层边界会阻挡移动，斜向移动不会加速。楼层和地图按钮自动寻路到目的地，爬楼过程自动完成；J 打开日志；Esc 返回或暂停。打开界面时不能移动。重要操作及手动移动停止时自动存档。标题页的“开始新游戏”会要求再次点击确认覆盖现有存档。

## 三层建筑与通行

- 一楼：前台、等候区、办公室、电脑与档案柜。审查必须走近前台；举报必须走近办公室电脑。大厅与办公室处于同一张可行走的三维楼层。
- 二楼：201、202、203；三楼：301、302、303。两层都能实际分配住客、安装监控、开门搜查和取得证据。每日来客仍为原有三名，不增加经济负担。
- 每层西侧为楼梯间。两段带踏步、半层平台与扶手的折返楼梯连接 1↔2↔3。人物沿三维路径升降，一楼前往三楼必须经过二楼。
- 左上方「1 楼 / 2 楼 / 3 楼」及区域地图均触发实际行走；不会瞬移。任意时刻只显示当前一层，包括爬楼期间；镜头保持当前楼层构图，到达平台后切换到新楼层。
- 人物所在楼层与平面位置随存档保存。旧存档自动补齐三楼房间，保留原房间观察配置。

## 客房视角与观察

- 沿楼梯到达二楼或三楼后，顶部房号选择当前楼层正在操作的房间，不会把人物传送进房。
- 侧上方固定正交镜头、去屋顶和局部降低的前墙，能同时看到走廊及三个房间。房门可以实际开合。
- 201 房初始配有一台摄像头。其余房间未安装，所有房门初始关闭。没有有效观察来源时，内部完全遮黑，只保留房间边界和门牌。
- 「安装摄像头」「切换安装位置」「左转 / 右转 15°」「视角」控制当前房间的摄像头。三个安装点、朝向和 40° / 60° / 90° 视角共同决定覆盖范围，摄像头不会自动揭示整间房。
- 摄像头与门口视线均使用渐变边缘。家具不参与视野遮挡；床、书桌等实体仍会挡住人物行走。
- 宵禁后点击「打开房门」，人物先走到门口再开门。可见范围随人物在走廊中的位置变化。进入房间后可直接观察，离开或关门会重新遮黑。
- 摄像头只能观察，搜查仍须开门、走近物品并消耗原有搜查行动。不可见物品不显示交互标签。白天允许查看楼层和调整已有摄像头朝向，开门、安装及移动摄像头需等到夜班。
- 摄像头配置和房门状态随存档保留；旧 v1 存档自动补齐这些设置。当前原型安装摄像头不另收费用，不引入新的经济规则。

存档：`%USERPROFILE%\AppData\LocalLow\Northern District\Check In Please\hotel-save-v1.json`，上一次文件保留为 `.bak`。自动验证模式不会写入玩家存档。

## 工程结构

- `Scripts/Core/GameModel.cs`：独立于 Unity 的状态机、九名住客、证据来源、规则与延迟后果。
- `Scripts/WorldView.cs`：侧上方镜头、实时人物、步行动画及证件肖像。
- `Scripts/WorldView.Hotel.cs`：三层建筑组织、一楼前台及办公室电脑、折返楼梯、楼层通行与住客呈现。
- `Scripts/Core/HotelLayout.cs`：楼层高度、六间有效客房和逐层通行规则。
- `Scripts/WorldView.Floor.cs`：客房楼层的三维建筑、家具、门、监控模型及世界坐标寻路。
- `Scripts/Core/RoomObservation.cs`：可序列化的观察配置、摄像头视角和门洞视线规则，独立于 Unity 可测试。
- `Scripts/DemoGame.cs`：交互、存读档、场景切换、环境音及自动运行验证。
- `Scripts/DemoUI.cs`：中文 HUD、审查、物品、档案、日志、结局与暂停界面。
- `Resources/HotelMatte.shader`：内置渲染管线的哑光阴影材质。
- `Resources/HotelPlate.shader`、`HotelOverlay.shader`：场景背景与人物接触阴影。
- `Resources/HotelRoom.shader`：实时视野遮黑、软边过渡及旧墙面、木材、织物材质。
- `Resources/HotelText.shader`：带深度遮挡的世界文字，避免上下楼时门牌和招牌穿透楼板。
- `Resources/Art/`：可直接运行的环境和纸张素材，已放入工程，不依赖在线服务。
- `Editor/DemoBuilder.cs`：场景生成与 Windows Mono 构建。

整座旅馆采用实时三维场景，建筑与家具按选定 B 方案重建，固定侧上方视角，不提供自由旋转或搬动家具。当前楼层剖顶显示以避免上层遮住下层，三层建筑和楼梯在同一世界坐标中连接。前台审查保留证件桌面界面，办公室使用电脑举报终端。人物仍为程序建模与简单步行动画。证件上的姓名、编号、规则、线索和处置按钮均为运行时内容。

原概念图保留在 `output/gameplay-concepts-v2-dark`。背景素材来源与生成提示词记录在 `output/runtime-art-v2/manifest.json`；新增墙面、木材、地毯和织物图集使用内置 image_gen 生成，原始提示词与来源记录在 `output/runtime-art-v3/manifest.json`，运行素材为 `Resources/Art/RoomSurfaces.png`。新版实机截图由测试生成到 `Builds/Windows/Validation`。

当前范围不含完整长篇剧情、五阵营系统、自由对话、战斗或正式配音。六个客房复用布局；玩法无联网依赖，代码不接入在线服务。中文默认使用 Windows 系统字体 Microsoft YaHei，未复制分发商业字体。

## 构建与检查

PowerShell 从本目录运行：

```powershell
dotnet run --project Tests/CheckInPlease.ModelTests.csproj
.\Build-Demo.ps1
.\Builds\Windows\CheckInPlease.exe -demoSmokeTest -logFile smoke-player.log
.\Builds\Windows\CheckInPlease.exe -hotelSmokeTest -logFile hotel-player.log
```

第一项检查关键经济与流程约束。第二项使用 F 盘编辑器批量创建场景并构建。为避开 Unity Linker 对路径中英文逗号的解析问题，脚本将源工程同步到 `F:\CheckInPleaseTools\BuildWorkspace\UnityDemo`，构建成功后再将成品复制回本目录；不移动源工程。第三项在真实 Unity Player 中走完三天、截图并退出，输出位于 `Builds/Windows/Validation`，退出码 0 表示流程检查通过。它是独立验证模式，不修改正常存档。

第四项专项检查 WASD 方向与速度、碰撞、寻路接管、界面暂停、单层显示、一楼前台、六房分配、楼梯连续升降、三楼观察与搜查、跨层视野隔离、楼层位置存档，以及返回一楼电脑提交举报。原 -floorSmokeTest 参数仍兼容，运行同一套专项检查。截图测试必须使用正常显示的游戏窗口，隐藏窗口会生成全黑截图并判定失败。构建脚本保留已有 Validation 目录和运行日志。
