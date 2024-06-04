---
layout: post
title: Position, LookDirection and UpDirection of 3D Camera
enable: true
---

想象一下，你在 3D 空间中移动你的头而不是 Camera。然后，Camera.Position是指定你的头部位置，Camera.LookDirection 决定你看的方向，而 Camera.UpDirection 表示你头部的方向。下面的图片为你澄清一些事情。

在第一张图片中，相机以 + Z 轴为中心，向下看到 -Z 轴和你的形状:

<img src="https://i.sstatic.net/GwKHh.png" width="80%">

当你设置照相机的位置为例子(x = 1，y = 0，z = 10)时，照相机的位置变化如下:

<img src="https://i.sstatic.net/qCXsU.png" width="80%">

因此，相机向右移动一点，因此你会在视图的左侧看到你的形状。

现在如果Camera.LookDirection 改为(x = 0，y = 1，z =-1) ，然后相机只看这个点(0, 1, -1) ，像这样:

<img src="https://i.sstatic.net/6Amx7.png" width="80%">

LookDirection 实际上等于“要看的点”减去“摄像头位置”。

这意味着在上图中，如果我们想让位于(1,0,10)的相机看到点(0,1，-1) ，那么 LookDirection 应该是:

(0,1,-1) - (1,0,10) = (-1,1,-11).

最后，Camera.UpDirection 决定你的摄像头顶端指向哪里。在下面的图片中，设置 Camera.UpDirection 为 (-0.5,1,0) 导致相机逆时针旋转:

<img src="https://i.sstatic.net/mR780.png" width="80%">

### 参考资料
[Make Camera LookDirection look front face](https://stackoverflow.com/questions/30690348/make-camera-lookdirection-look-front-face)