---
layout: post
title: Console.WriteLine vs Debug.WriteLine
enable: true
---

Console.WriteLine 在 debug / release 时写入输出流。

Debug.WriteLine 写入 Listener 集合中的跟踪侦听器，但仅在 debug 时写入。以 release 版本编译应用程序时，编译器不会将 Debug 元素编译到代码中。

正如 Debug.WriteLine 写入 Listener 集合中的所有跟踪侦听器，这可能会在多个地方输出，比如VS 输出窗口、控制台、日志文件、注册侦听器的第三方应用程序（[DebugView](https://learn.microsoft.com/zh-cn/sysinternals/downloads/debugview)）等等。