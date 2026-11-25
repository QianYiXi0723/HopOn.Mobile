# HopOn.Mobile

HopOn 的安卓端。目前通过局域网连接桌面版 HopOn 查看体力、每日进度并打卡；目标是逐步做到不依赖电脑、独立运行。

## 当前状态

- 配对：电脑端「定时与状态 → 设置 → 手机连接」打开开关，手机在此页面搜索电脑；校园网等隔离网络搜不到时，手动填电脑显示的 IP。输入电脑屏幕上的 6 位验证码完成配对。
- 数据：全部来自电脑端局域网网关，令牌存在安卓 SecureStorage。
- 平台：仅安卓（net8.0-android）。iOS 暂缓。

## 构建

需要 .NET 8 + MAUI android workload + Android SDK。

```bash
dotnet publish HopOn.Mobile/HopOn.Mobile.csproj -f net8.0-android -c Release -p:AndroidSdkDirectory=<你的SDK路径>
```

产物：`HopOn.Mobile/bin/Release/net8.0-android/publish/*-Signed.apk`

## 结构

- `HopOn.Mobile.Core` — 纯 C# 类库，无 MAUI 依赖。数据源接口 `IHopOnDataSource`、局域网数据源 `PcRemoteDataSource`、设备发现、配对地址选择。
- `HopOn.Mobile` — MAUI 安卓界面。
- `HopOn.Mobile.Tests` — xunit 单元测试。

## 关系

桌面版：<https://github.com/QianYiXi0723/HopOn>
