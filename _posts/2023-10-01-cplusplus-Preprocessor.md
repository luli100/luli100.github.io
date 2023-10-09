---
layout: post
title: C++ 编译器 - 预处理
enable: true
---

预处理器是在程序源文件被编译之前根据预处理指令对程序源文件进行处理的程序。预处理器指令以#号开头标识，末尾不包含分号。预处理命令不是C/C++语言本身的组成部分，不能直接对它们进行编译和链接。C/C++语言的一个重要功能是可以使用预处理指令和具有预处理的功能。C/C++提供的预处理功能主要有文件包含（#include）、宏替换(#define)、条件编译(if, #ifdef)等。

### 头文件包含

**#include** 实际上很简单，预处理会将你指定的那个文件复制粘贴到你写的 .cpp 文件中。我们一起来验证一下。

新建一个 math.cpp， 输入下面代码：

```
int add(int x, int y)
{
	int result = x + y;
	return result;
}
```

按 Ctrl + F7 编译 math.cpp 文件，可以编译成功。

回到 math.cpp，删除右花括号，再按 Ctrl + F7 编译 math.cpp 文件，你可以看到编译器报如下错误：

math.cpp(2,1): fatal  error C1075: '{': no matching token found

再新建一个 endbrace.h 文件，内容就一个右花括号。

```
}
```

然后，我们将 math.cpp 的代码修改如下：

```
int add(int x, int y)
{
	int result = x + y;
	return result;
#include "endbrace.h"
```

按 Ctrl + F7 进行编译，也会编译成功。因为所有编译器此时做的工作就是：复制所有 endbrace.h 内容，替换掉 #include "endbrace.h"。


### 宏替换

### 条件编译


