# UDP 接收与发送结果：回环实验

配套文章：[UDP 接收慢，会让发送端停发吗？](https://luli100.github.io/udp-receiver-backpressure-and-ethernet-flow-control/)

需要 .NET 10 SDK，无第三方包依赖。从仓库根目录运行：

```powershell
dotnet run --project examples/udp-flow-control/UdpFlowControl.csproj --configuration Release
```

程序只向 `127.0.0.1` 的临时 UDP 端口发送人工构造的数据，不访问实际设备，不修改网卡配置。输出为 JSON Lines，包括运行环境和三个场景：

1. 接收 Socket 已绑定，先验证回环通信正常，然后在发送 20,000 个 1466 字节报文期间不执行接收。全部发送结束后，再读取剩余报文。
2. 释放一个临时端口后向该端口发送。只记录发送结果，不把“全部成功”或“收到 ICMP 对应异常”规定为跨平台必然结果；短暂的端口释放也不保证它绝不会被其他进程占用。
3. 向本机发送一个 65,508 字节的 IPv4 UDP 载荷，验证 `MessageSize` 错误。

第一个场景要求发送全部成功、接收至少一个报文且接收数量少于发送数量；第三个场景要求得到 `MessageSize`。两者通过时返回退出码 0，否则返回 1。闭端口场景只作观察。

接收队列的实际容量、保留报文数、耗时以及 ICMP 错误反馈都会受系统实现影响。这个实验用于验证发送 API 的成功不能证明接收应用获得了报文，**不能证明物理网卡是否触发 PAUSE，也不能验证 DCA1000 是否响应 PAUSE**。回环流量没有经过那条物理以太网链路。
