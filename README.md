# HotUpdate

基于 **Unity（团结引擎 2022.3）+ HybridCLR + YooAsset** 的 Space Shooter 热更新示例工程：AOT 层负责启动与资源补丁，热更层（`HotUpdate` 程序集）负责游戏逻辑。

## 技术栈

| 模块 | 说明 |
|------|------|
| Unity | 2022.3.62t3（团结引擎 1.8.1） |
| [HybridCLR](https://github.com/focus-creative-games/hybridclr) | 热更 DLL 加载、AOT 补充元数据 |
| [YooAsset](https://github.com/tuyoogame/yooasset) 3.0.1-beta | 资源包、补丁下载、Host/Offline 模式 |
| 示例玩法 | YooAsset Space Shooter Sample（飞行射击） |

## 仓库说明（最小可上传内容）

本仓库**只包含可复现的工程源码**，不包含：

- `Library/`、`Temp/`、`Logs/` 等 Unity 缓存
- `HybridCLRData/`（Generate / IL2CPP 裁剪产物）
- `Build/` 打好的玩家包
- `Assets/StreamingAssets/yoo/` 下已构建的 `.bundle`（体积大，需本地或 CDN 构建）

克隆后需在本地执行 **HybridCLR Generate** 与 **YooAsset 资源构建** 才能完整运行（见下文）。

## 目录结构

```
Assets/
  AOT/                    # AOT 程序集：Boot、补丁 FSM、GameManager
  HybridCLRGenerate/      # link.xml、AOTGenericReferences（Generate 生成）
  DLLS/                   # 裁剪后的 AOT 元数据 dll、HotUpdate.dll（.bytes）
  Samples/YooAsset/...    # Space Shooter 资源与热更脚本（HotUpdate.asmdef）
  StreamingAssets/yoo/    # YooAsset 内置/缓存资源（构建后生成，默认不提交 bundle）
Packages/
  manifest.json           # UPM 依赖（含 HybridCLR Git、YooAsset OpenUPM）
ProjectSettings/
```

## 环境要求

- **团结引擎 / Unity 2022.3.62**（或兼容的 2022.3 LTS）
- Git（拉取 `com.code-philosophy.hybridclr`）
- 可选：本地 HTTP 服务，用于 `HostPlayMode` 托管 CDN（默认 `http://127.0.0.1/CDN/PC/v1.0/`）

## 快速开始

### 1. 克隆

```bash
git clone <your-repo-url> HotUpdate
cd HotUpdate
```

### 2. 用 Unity / 团结引擎打开工程

首次打开会解析 `Packages/manifest.json`，自动拉取 HybridCLR、YooAsset 等包。

### 3. HybridCLR（必做）

菜单依次执行（或 **HybridCLR → Generate → All**）：

1. **HybridCLR → Installer → Install**（仅首次）
2. **HybridCLR → Generate → AOTGenericReference**  
   更新 `Assets/HybridCLRGenerate/AOTGenericReferences.cs` 中的 `PatchedAOTAssemblyList`
3. 打一次 **IL2CPP 玩家包** 后，从 `HybridCLRData/AssembliesPostIl2CppStrip` 将裁剪 dll 复制到 `Assets/DLLS/`（扩展名 `.bytes`）  
   需与 `PatchedAOTAssemblyList` 一致，并包含 `HotUpdate.dll.bytes`
4. 真机/PC 包运行前在 `FsmStartGame` 中会 `LoadMetadataForAOTAssembly` + `Assembly.Load(HotUpdate.dll)`

### 4. YooAsset 资源（必做）

1. 打开 **YooAsset → AssetBundle Collector**，确认 `DefaultPackage` 收集器包含 `GameRes`、`DLLS` 等组
2. **YooAsset → AssetBundle Builder** 构建 `DefaultPackage`
3. 按需二选一：
   - **编辑器**：`Boot` 上 `PlayMode = EditorSimulateMode`
   - **真机/PC 包**：`HostPlayMode`，将构建产物部署到 CDN，并修改 `FsmInitializePackage.GetHostServerURL()` 中的地址

### 5. 运行

- 启动场景见 `ProjectSettings/EditorBuildSettings`（示例为 Space Shooter `Boot` 场景）
- 流程：补丁 UI → 资源初始化/更新 → 加载 DLL → 进入 `scene_home` / `scene_battle`

## 运行模式说明

| `Boot.PlayMode` | 用途 |
|-----------------|------|
| `EditorSimulateMode` | 仅编辑器，本地模拟 AB，不依赖 CDN |
| `HostPlayMode` | 发布包：内置资源 + 远程 CDN 热更 |
| `OfflinePlayMode` | 全内置 StreamingAssets，无网络 |

**注意**：`EditorSimulateMode` 在**已打包玩家**中会报错，发布请使用 `HostPlayMode` 或 `OfflinePlayMode`。

## 热更与 AOT 分工

- **AOT**：`Boot`、`PatchManager`、补丁状态机、`FsmStartGame`（加载元数据与热更 DLL）
- **HotUpdate**：`SceneBattle`、`BattleRoom`、`EntityPlayer` 等 gameplay 脚本
- **补充元数据列表**：以 `AOTGenericReferences.PatchedAOTAssemblyList` 为准，**不要**向列表随意添加 `System.dll`；**不要**对 `AOT.dll` 调用 `LoadMetadataForAOTAssembly`

## 常见问题

- **白块/粉块模型**：战斗资源依赖未打进包或未从 CDN 下载；检查 `Entity` / `EntityArt` / `Shader` 组并重新 Build
- **`System.dll` 加载失败**：该程序集通常不在 `PatchedAOTAssemblyList` 中，勿加入 `AOTMetaAssemblyFiles`
- **`patchAOTAssemblies`**：仅编辑器配置占位，**不会**自动加载；运行时仍需手动 `LoadMetadataForAOTAssembly`

## 参考链接

- [HybridCLR 文档](https://hybridclr.cn/)
- [YooAsset 文档](https://www.yooasset.com/)

## License

示例资源与第三方包遵循各自仓库协议；本仓库业务代码请以你方项目许可为准。
