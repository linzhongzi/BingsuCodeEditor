## Fork 版本说明
Fork 自:
https://github.com/Buizz/BingsuCodeEditor

Fork 并创建分支:
https://github.com/linzhongzi/BingsuCodeEditor/tree/0.19.6

BingsuCodeEditor-0.19.6.0 项目由 Buizz/BingsuCodeEditor-master-2025.01.16(6a1cc9a) 修改而来。

BingsuCodeEditor-0.19.6.0 项目是 EUD-Editor-3-0.19.6.0 的依赖项目。

--------------------------------------------------------------------------------

## Tag 版本
* 0.19.6.0:	首次创建，未修改任何代码与配置。基于 BingsuCodeEditor Master 2025.01.16(6a1cc9a) 的提交版本创建。
* 0.19.6.1:	统一Nuget包位置。尚未翻译中文为韩文。修正编译错误。
* 0.19.6.2	韩英注释翻译为中文注释。
* 0.19.6.3	翻译所有韩文代码为中文。

--------------------------------------------------------------------------------

## 工程目录结构:

```
EUD-Editor-3 Source\
├── BingsuCodeEditor\
│   ├── BingsuBlocklyEpsEditor\
│   ├── BingsuCodeEditor\ 
│   ├── BingsuCodeEditorTest\
│   └── NuGet.Config                   ← 指定还原 NuGet 包时的位置为: packages
├── EUD-Editor-3\
│   ├── EUD Editor 3\
│   ├── EUD Editor 3.sln
│   └── NuGet.Config                   ← 指定还原 NuGet 包时的位置为: packages
└── packages\                          ← 所有 NuGet 包统一放此
```

