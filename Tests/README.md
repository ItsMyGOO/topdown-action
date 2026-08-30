# Tests

当前自动化测试优先覆盖不依赖运行中场景的纯逻辑模块，例如：

- 角色移动速度演进规则
- 轻量状态机行为

建议本地使用以下命令运行：

```bash
dotnet test Tests/GodotGameTemplate.Tests.csproj
```

如果本地尚未安装 .NET SDK 或 Godot C# 依赖，请先完成工具链安装后再运行。
