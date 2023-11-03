---
layout: post
title: UDP 报文数据包长度
enable: true
---

不知道你在使用 C# 进行 UDP 网络编程时，是否遇到过下面的错误：

<img src="/images/udp_send_to_error.png" width="80%">

运行下面的代码会复现上面的错误信息：

```C#
try
{
    Byte[] datas = new Byte[UInt16.MaxValue];
    var client = new Socket(SocketType.Dgram, ProtocolType.Udp);
    client.SendTo(datas, datas.Length, SocketFlags.None, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4099));
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
```

其实，上面的问题在于，UDP 一次性发送了 65535 字节的数据，超过了 UDP 最大数据包长度 65507 字节。

### UDP 最大数据包长度

IP 数据报格式：

<img src="/images/internet_protocol_datagram_format.png" width="80%">

![图片来源](http://www.tcpipguide.com/free/t_IPDatagramGeneralFormat.htm)

UDP 消息格式：

<img src="/images/udp_message_format.png" width="80%">

![图片来源](http://www.tcpipguide.com/free/t_UDPMessageFormat.htm)

UDP 数据包最大长度不是由 UDP 头部的 Length 字段所决定，而是由 IP 头部的 Total Length 字段限制。

Total Length 字段为 16 位长度，因此其最大值为 65535，除去 20 字节的 IP 首部和 8 字节的 UDP 首部，UDP 数据报中用户数据的最大长度为 **65535 - 8 - 20 = 65507** 字节。

### 单包 UDP 数据包长度

通常来说，每个以太网帧都有最小 64 字节，最大不能超过 1518 字节。除去链路层的首部和尾部的 18 个字节，链路层的数据区长度范围是 46 
~ 1500 字节。因此，链路层数据区的最大长度为 1500 字节，即 **MTU**（Maximum Transmission Unit 最大传输单元，是指数据链路层上面所能通过的最大数据包的字节大小）为 1500 字节。

因为 IP 首部为 20 字节，UDP 头部为 8 字节，因此单包 UDP 报文的数据区最大长度为 **1500 - 8 - 20 = 1472** 字节。
