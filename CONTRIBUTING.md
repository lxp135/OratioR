# 如何贡献

感谢您考虑为项目贡献代码或修复缺陷。如果您计划进行的更改可能超过几行代码，请提前告知您的计划。这将有助于避免重复工作。

## 本地搭建 / Git 钩子

本仓库使用 [mise](https://mise.jdx.dev/) 管理开发工具链，并使用 [lefthook](https://github.com/evilmartians/lefthook) 管理 git 钩子。

首先，如果尚未安装，请安装 mise 本身——请参阅 [mise 安装指南](https://mise.jdx.dev/getting-started.html)（在 Windows 上，例如 `winget install jdx.mise`）。

然后，克隆仓库后，运行 `mise install` 和 `mise run setup`（这将运行 `lefthook install` 来配置钩子）。

pre-commit 钩子对暂存的 .cs 文件运行 `dotnet format`，以保持格式与 `.editorconfig` 一致。

## 提交更改

请发送一个 Pull Request，清楚列出您所做的更改（了解更多关于 [Pull Request](http://help.github.com/pull-requests/) 的信息）。
发送 Pull Request 时，请遵循我们的编码规范，并确保所有提交都是原子性的（每个功能一个提交）。

请始终为您的提交编写清晰的日志消息。对于小更改，单行消息即可；但对于较大更改，则应包含更全面的描述。

## 本地化

如果您对字符串资源（resources.resx 等）进行了更改，请不要在 PR 中包含非母语的资源文件。这些文件由 Crowdin 本地化工具生成。

## 编码规范

编码规范在 `.editorconfig` 中定义，并由内置的 .NET 分析器（`AnalysisLevel=latest-recommended`）以及 `Roslynator.Analyzers` 强制执行。
警告视为错误（`TreatWarningsAsErrors=true`），且 pre-commit 钩子运行 `dotnet format`，因此遵守编码风格无需花费太多精力。

非常感谢

Antony Corbett
