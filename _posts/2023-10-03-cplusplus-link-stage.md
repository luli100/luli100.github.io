---
layout: post
title: C++ 编译器 - 链接
enable: true
---

.cpp 文件编译成 obj 文件后，还需要经过一个称为“链接”的过程。链接的主要内容就是找到每个符号和函数的位置以及将每个单独的 obj 文件链接在一起，生成可执行文件。单个 obj 文件没有任何关系，这些文件不能交互，所以，如果我们决定把程序分割成多个文件，我们需要一种方法把这些文件链接起来形成一个程序，这就是链接器的作用。

### main 函数

在我们的项目中，现在只有一个 math.cpp 源文件。然而，这不是一个实际的应用程序，因为它没有包含 main 函数，如果你 build 项目，你会看到一个链接错误：

<img src="/images/project1-link-error.png" width="80%">

这是因为在 build 项目时，会先后经历编译和链接两个阶段，不同阶段的错误类型是不一样的。修改程序，故意删除分号，按 Ctrl + F7，看一下编译错误是什么样子的：

<img src="/images/project1-compile-error.png" width="80%">

回过头看，链接阶段的错误代码以 LINK 开头，编译阶段的错误代码以字母 C 开头。知道这一点很重要，不同阶段得到的错误类型是不一样的，这样你就可以根据错误信息修复 BUG。

其实，应用程序入口点不一定非要是 main 函数，只要有一个入口点就行了，打开项目属性页，设置 Entry Point 为 add 函数：

<img src="/images/project1-entry-point.png" width="80%">

将刚才删掉的分号修复后，再 build 项目，链接错误消失。通常来说，我们都会把 main 函数作为入口，但你要知道的是，入口点不一定非得是 main 函数，可以是任何函数。恢复项目属性页的 Entry Point 设置为空。在 math.cpp 中再添加一个 main 函数。

```
const char* log(const char* message)
{
    return message;
}

int add(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    return 0;
}
```

build 项目，也会成功地生成了 EXE 文件。

### 多源文件

现在，我们补充一下 main 函数，并修改一下 log 函数，代码如下：

```
#include <iostream>

void log(const char* message)
{
    std::cout << message << std::endl;
}

int multiply(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    std::cout << multiply(10, 10) << std::endl;
    std::cin.get();

    return 0;
}
```

点击 Local Windows Debugger，正如你所见，应用程序可以正常运行。现在，我们新建一个 log.cpp 文件，并将 math.cpp 中的 log 函数移动到 log.cpp 文件中：

```
#include <iostream>

void log(const char* message)
{
    std::cout << message << std::endl;
}
```

现在 math.cpp 中的代码如下：

```
#include <iostream>

int multiply(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    std::cout << multiply(10, 10) << std::endl;
    std::cin.get();

    return 0;
}
```

按 Ctrl + F7，会得到一个编译错误：

<img src="/images/project1-compile-error2.png" width="80%">

在 math.cpp 中添加一个 log 函数的声明，按 Ctrl + F7，你可以看到能够编译成功。

```
#include <iostream>

void log(const char* message);

int multiply(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    std::cout << multiply(10, 10) << std::endl;
    std::cin.get();

    return 0;
}
```

继续 build 项目，也成功了。

### 其它错误

回到 log.cpp 文件里，做如下代码修改：

```
#include <iostream>

void logr(const char* message)
{
    std::cout << message << std::endl;
}
```

如果我们现在 build 整个项目，你会得到一个链接错误 - 无法解析的外部符号：

<img src="/images/project1-link-error2.png" width="80%">

这是因为调用 multiply 函数中调用了 log 函数，编译器在链接的时候，却找不到 log 函数的定义。

另一个有趣的情况，注释掉 multiply 函数中的 log 函数调用，再 Build 项目，你会发现错误消失了。

<img src="/images/project1-build-right.png" width="80%">

发生这种情况的原因是，我们从来没有调用过 log 函数，所以链接器不需要去链接这个 log 函数。

那我们恢复 multiply 函数中的 log 函数调用，然后注释掉 main 函数中的 multiply 函数调用，会发生什么情况呢？

<img src="/images/project1-build-error.png" width="80%">

Build 项目，仍然得到一个链接错误，为什么会这样呢？你可能会问，我没有在任何地方调用 multiply 函数，为什么它会产生链接错误呢？我们可不可以说这些没有被调用的代码没有意义呢？

大错特错，虽然在 math.cpp 文件中没有调用 multiply 函数，但技术上讲，我们可能在另一个文件中使用它，所以连接器确实需要链接它。

如果我们能告诉编译器 multiply 函数只在 math.cpp 文件中使用，就可以去掉这种链接的必要性了。下面是一种可以做到的方法：

<img src="/images/project1-build-right2.png" width="80%">

在本例中，我们实际上修改了函数定义的名字，我们把函数的名字改从 logr 改回 log，然后将 void 返回参数改为 int，再 Build 项目，你仍会看到一个链接错误：

<img src="/images/project1-build-error2.png" width="80%">

这是因为在 math.cpp 中，我们声明的 log 函数是一个返回值是 void，所以要寻找名为 log 函数，它返回 void 以及同样的参数。

还有一种常见错误 - 重复的符号，换句话说，我们有两个名字相同的函数且有相同的返回值和相同的参数，如果发生这种情况，链接器就会不知道链接到哪一个。现在，我们在 math.cpp 里也加一个 log 函数的定义，Build 项目，看会出现什么情况：

<img src="/images/project1-build-error3.png" width="80%">

你可能认为这种类型的错误不会经常发生，然而，这可能会悄悄发生在你身上。

### 三个解决方案

为了 Build 项目成功，我们先创建一个 log.h 头文件。

现在 log.h 头文件的代码如下：

```
void log(const char* message)
{
    std::cout << message << std::endl;
}
```

log.cpp 文件的代码如下：

```
#include <iostream>
#include "log.h"

void InitLog()
{
    log("Init Log");
}
```

math.cpp 文件的代码如下：

```
#include <iostream>
#include "log.h"

void log(const char* message);

static int multiply(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    std::cout << multiply(10, 10) << std::endl;
    std::cin.get();

    return 0;
}
```

还记得 #include 语句的工作原理吗？当我们包含头文件时，编译器会取头文件的内容，把它放在 #include 语句的地方。现在 Build 项目，会出现什么情况呢？

<img src="/images/project1-build-error4.png" width="80%">

同样会出现链接错误，跟上面的情况是一样的。有三种方案可以解决这个问题：

1、 static

在 log.h 中做如下修改：

```
static void log(const char* message)
{
    std::cout << message << std::endl;
}
```

这时 Build 项目，不会得到任何链接错误。

2、 inline

inline 的意思是，获取实际的函数体并将函数调用替换为函数本身：

```
inline void log(const char* message)
{
    std::cout << message << std::endl;
}
```

同样的，Build 项目，也不会得到任何链接错误。

3、分离函数声明和定义

将 log.h 头文件的代码修改如下：

```
void log(const char* message);
```

将 log.cpp 文件的代码修改如下：

```
#include <iostream>
#include "log.h"

void InitLog()
{
    log("Init Log");
}

void log(const char* message)
{
    std::cout << message << std::endl;
}
```

将 math.cpp 文件的代码修改如下：

```
#include <iostream>
#include "log.h"

static int multiply(int x, int y)
{
    log("multiply");
    return x * y;
}

int main()
{
    std::cout << multiply(10, 10) << std::endl;
    std::cin.get();

    return 0;
}
```

此时，Build 项目，也不会出现链接错误。

### 总结

链接器最终会将 obj 文件链接在一起，它也会将程序使用的其他库链接起来，例如 C 运行时库。如果有必要，C++ 标准库，平台 API，还有很多其它的东西，从不同地方链接是很常见的操作，甚至还有不同类型的链接，比如静态链接和动态链接。