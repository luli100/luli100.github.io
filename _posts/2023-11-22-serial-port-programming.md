---
layout: post
title: UART（Universal Asynchronous Receiver Transmitter）通信
enable: true
---

在使用串口调试助手或者进行串口编程时，往往要选择串口号，以及设置波特率，校验位，数据位，停止位才能正常通信。

### 数据帧格式

数据帧格式是通信协议所必须的，它帮助设备之间正确理解彼此之间的谈话内容。下图便是 UART 通信的数据帧格式：

<img src="/images/uart_data_frame.png" width="80%">

UART 数据帧格式包含了起始位，数据，奇偶校验位，还有停止位。

传输数据之前，UART 的发送线和接收线是高电平，表明线路是空闲的，没有数据传输。

当发送设备想要开始通信时，它会把发送线拉成低电平，这样一来，接收设备会知道发送端想要发送数据。当传输线变低时，它会保持低电平一个始终脉冲。在此之后，有一个 8 位数据帧需要在线路上进行传输。

然后就是奇偶校验位，最后一个停止位通知接收方通信结束。停止位实际上是逻辑高电平，它在一个时钟脉冲保持高电平，并且通知接收者。

所以基本上 UART 数据帧是一个 10 位数据帧，包括开始位和停止位，如果我们使用奇偶校验位，那么它就变成 11 位数据帧。

### 波特率

波特率是指在单位时间内传输的数据比特数，发送方会将数据以比特的形式一位一位地传输给接收方，接收方则按照同样的速率接收这些比特，以此完成数据传输。波特率越高，数据传输速度就越快。

在传输数据时，发送方和接收方需要以相同的波特率进行通信。如果两端的波特率不同，那么就会出现数据丢失、传输错误等问题，导致数据传输失败。

### 数据位

其实数据帧的数据位可以不为 8 位，你可以看到串口调试助手数据位可以选择 5、6、7、8，这个可以根据你的需要进行设定。比如有这么一个场景：你传输的所有数据的数值都不会超过 31，那么数据位选择 5，那完全没得问题，并且还可以增加通信效率。

### 奇偶校验位

奇偶校验位是可选的，有些设备需要这个，有些设备不需要。只有在设备需要检查数据流中出现的错误时才使用。

### 停止位

为什么要配置停止位的值，并且可以设置成 1、1.5、2？在早些时候，这些设备的速度非常慢，所以在接收到一个数据帧后，它们不能立即重新组织自己以获得新的数据，所以在旧版本的 UART 中曾经有 2 个停止位。

<!--
### 参考资料

https://www.youtube.com/@FoolishEngineer

http://m.blog.chinaunix.net/uid-9688646-id-3275789.html


-->