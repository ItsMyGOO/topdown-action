# 八向动画素材槽位

将跑动表按以下文件名放入本目录即可**自动生效**（无需改代码，放好后重启游戏）：

| 文件名 | 方向 |
|---|---|
| Run_Right.png | 右 |
| Run_DownRight.png | 右下 |
| Run_Down.png | 下 |
| Run_DownLeft.png | 左下 |
| Run_Left.png | 左 |
| Run_UpLeft.png | 左上 |
| Run_Up.png | 上 |
| Run_UpRight.png | 右上 |

**规格**：与 `Game/Art/2D HD Character Knight/Spritesheets/With shadows/Run.png` 一致——
15 列 × 8 行，单帧 128×128（整表 1920×1024），行 0 为默认变体，朝右基准。

**回退**：缺失的方向自动回退到现有方案（Run/RunBackwards + 水平翻转）。
放部分方向也可以：比如只放 Run_Left.png，则只有向左用新素材。
