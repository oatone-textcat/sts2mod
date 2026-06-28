# Changelog

## 1.0.3

- 修复光标完全不显示（先是冻结的原版指针且无点击动作，后是连指针都没有）的根本问题：本模组以 `Microsoft.NET.Sdk` 单 DLL 方式编译，不会运行 Godot 的 C# 源生成器，导致自定义 `Node` 子类的 `_Process` 等回调**根本不被调用**——节点进了场景树却从不逐帧更新。改用内置的 `CanvasLayer`+`Sprite2D` 并通过 `SceneTree.ProcessFrame` 信号（普通 `Callable`，不依赖源生成器）每帧驱动。
- overlay 挂到 SceneTree 根 `Window`（始终在树内、渲染于最顶层），不再挂到初始化时尚未入树的 `NGame`。
- 用 `Sprite2D.GetGlobalMousePosition()` 自洽定位，修正 Retina / 内容缩放下的坐标偏移。
- 常态与点击动作均使用 P.R.T.S 风格动态光标；隐藏全部系统光标形状，屏幕上只保留 P.R.T.S 光标。

## 1.0.2

- （未生效的中间版本）尝试用游戏内 overlay 替代逐帧硬件光标，但自定义节点的 `_Process` 不被回调，光标不显示；已在 1.0.3 修复。

## 1.0.1

- 修复部分机器上每帧刷新系统光标导致的闪烁。
- 改为透明系统 Arrow 加游戏内 overlay 播放动画。
- 忽略窗口初始化时的无效 `(0, 0)` 鼠标位置，减少左上角短暂弹出。

## 1.0.0

- 初始正式版。
- 将默认 Arrow 光标替换为 P.R.T.S 风格 60 FPS 动态光标。
- 禁用点击、拖拽光标动画，始终使用 Idle 动画。
- 修复烘焙时由 Spine 自动时间推进导致的动画跳变。
