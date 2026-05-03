## Fork 版本说明
Fork 自:
https://github.com/Buizz/BingsuCodeEditor

Fork 并创建分支:
https://github.com/linzhongzi/BingsuCodeEditor/tree/0.19.6

BingsuCodeEditor-0.19.6.1 项目由 Buizz/BingsuCodeEditor-master-2025.01.16(6a1cc9a) 修改而来。

BingsuCodeEditor-0.19.6.1 项目是 EUD-Editor-3-0.19.6.1 的依赖项目。

--------------------------------------------------------------------------------

## Tag 版本
0.19.6.0	首次创建，未修改任何代码。基于 BingsuCodeEditor Master 2025.01.16(6a1cc9a) 的提交版本创建。
0.19.6.1	统一Nuget包位置。尚未翻译中文为韩文。修正编译错误。

--------------------------------------------------------------------------------

## 工程目录结构:

```
EUD-Editor-3-0.19.6.1 Source\
├── packages\                          ← 所有 NuGet 包统一放此
├── BingsuCodeEditor\
│   ├── NuGet.Config                   ← 指定还原 NuGet 包时的位置为: packages
│   ├── BingsuBlocklyEpsEditor\
│   ├── BingsuCodeEditor\ 
│   └── BingsuCodeEditorTest\
└── EUD-Editor-3\
```

