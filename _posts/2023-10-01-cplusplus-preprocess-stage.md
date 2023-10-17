---
layout: post
title: C/C++ 编译器 - 预处理
enable: true
---

预处理器是在程序源文件被编译之前根据预处理指令对程序源文件进行处理的程序。预处理器指令以 # 号开头标识，末尾不包含分号。预处理命令不是C/C++语言本身的组成部分，不能直接对它们进行编译和链接。C/C++提供的预处理功能主要有文件包含（#include）、宏替换(#define)、条件编译(if, #ifdef)等。

### 头文件包含

**#include** 实际上很简单，预处理会将你指定的那个文件复制粘贴到你写的 .cpp 文件中。我们一起来验证一下。

用 visual studio 新建一个空的 C++ 项目，然后新建一个 math.cpp， 输入下面代码：

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

实际上，我们有一种方法可以告诉编译器输出一个文件，其中包含预处理器输出的结果。

打开项目属性页，设置 Preprocess to a File 为 Yes，如下图所示：

<img src="/images/project1-property-pages.png" width="80%">

再按 Ctrl + F7 编译文件，打开 Debug 输出目录，可以看到一个 math.i 文件，使用文本编译器打开 math.i，可以看到如下内容：

```
#line 1 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"
int add(int x, int y)
{
	int result = x + y;
	return result;
#line 1 "D:\\gitee\\practice\\Project1\\Project1\\endbrace.h"
}
#line 6 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"
```

### 宏替换

回到 math.cpp 文件中，定义一个宏，进行如下修改：

```
#define INTEGER int

INTEGER add(INTEGER x, INTEGER y)
{
	INTEGER result = x + y;
	return result;
}
```

然后按 Ctrl + F7 编译文件，再次查看 math.i 输出文件，可以看到如下内容：

```
#line 1 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"


int add(int x, int y)
{
	int result = x + y;
	return result;
}
```

**#define 预处理语句基本上的作用，就是搜索 INTEGER，并将 INTEGRER 替换为 int。**

### 条件编译

**if 预处理语句可以让我们基于给定条件的代码包含或排除。**

```
#if 1

int add(int x, int y)
{
	int result = x + y;
	return result;
}

#endif
```

以上代码编译会得到如下内容：

```
#line 1 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"


int add(int x, int y)
{
	int result = x + y;
	return result;
}

#line 10 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"
```

基本上和源代码函数没有区别。

同理，将条件修改为 0 ：

```
#if 0

int add(int x, int y)
{
	int result = x + y;
	return result;
}

#endif
```

再次编译可以得到如下内容：

```
#line 1 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"








#line 10 "D:\\gitee\\practice\\Project1\\Project1\\math.cpp"
```

没有看到函数代码，这验证了我们的想法。

我们再看一下添加 #include iostream，会是一番什么景象：

```
#include <iostream>

int add(int x, int y)
{
	int result = x + y;
	return result;
}
```

按 Ctrl + F7 编译，可以看到 math.i 文件变得很大了：

<img src="/images/include-iostream.png" width="80%">

这是因为 iostream 也包含了其它文件，而它本身也包含了大量的代码。

### 总结

以上内容就是预处理器所做的工作，一旦这个阶段结束，我们就可以将 C++ 代码编译成机器代码。