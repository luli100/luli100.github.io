---
layout: post
title: C++ 编译器 - 链接
enable: true
---

.cpp 文件编译成 obj 文件后，还需要经过一个称为“链接”的过程。链接的主要内容就是找到每个符号和函数的位置以及将每个单独的 obj 文件链接在一起，生成可执行文件。单个 obj 文件没有任何关系，这些文件不能交互，所以，如果我们决定把程序分割成多个文件，我们需要一种方法把这些文件链接起来形成一个程序，这就是连接器的目的。

### main 函数

在我们的项目中，现在只有一个 math.cpp 源文件。然而，这不是一个实际的应用程序，因为它没有包含 main 函数，如果你 build 项目，你会看到一个链接错误：

<img src="/images/project1-link-error.png" width="80%">

这是因为 build 项目时，会先后经历编译和链接两个阶段，不同阶段的错误类型是不一样的。修改程序，故意删除分号，按 Ctrl + F7，看一下编译错误是什么样子的：

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
    log("add");

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
	log("add");

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
	log("add");

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
	log("add");

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