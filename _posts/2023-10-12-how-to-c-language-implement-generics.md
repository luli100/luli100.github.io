---
layout: post
title: C 语言如何实现泛型
enable: true
---

C 语言是过程式编程语言，C 语言设计的目标是提供一种能以简易的方式编译、处理底层内存产生少量的机器码以及不需要任何运行环境支持便能运行的编程语言。而对于更高阶、更抽象的编程范式来说，C 语言这种基于过程和底层的设计初衷就会成为它的短板。因为，在编程世界中，更多的编程工作是解决业务上的问题，而不是计算机的问题，所以，我们需要更为贴近业务、更为抽象的语言，比如 C#。

当然，C 语言也提供了一点抽象能力和泛型编程能力。

### 从一个简单的例子说起

从一个交换两个变量值的 swap 函数说起，代码如下：

```c
int swap(int* x, int* y)
{
    int temp = *x;
    *x = *y;
    *y = temp;
}
```

这个函数最大的问题，就是它只能给 int 类型的值使用，如果我们想交换两个 double 类型的值，还得写另外一个 swap 函数：

```c
double swap(double* x, double* y)
{
    double temp = *x;
    *x = *y;
    *y = temp;
}
```

float、struct 等类型，该如何是好？利用泛型对数据类型进行抽象，C 语言的类型泛型基本上来说就是使用 void * 关键字或宏定义。

### swap 函数的泛型版本 1（void *）

使用 void * 泛型版本的 swap 函数，代码如下：

```c
void swap(void* x, void* y, const int size)
{
    char* temp = malloc(size);
    memcpy(temp, y, size);
    memcpy(y, x, size);
    memcpy(x, temp, size);
}
```

这个实现方式有三个重点：

**函数接口中增加了一个 size 参数。** 这是因为，用了 void* 之后，类型被“抽象”掉了，编译器不能通过类型得到内存占用大小，因此，需要添加一个 size 来标识类型占用字节的长度。

**swap 函数实现中使用了 memcpy() 函数。** 还是因为类型被抽象掉了，不能用赋值表达式，因此只能用内存复制的方法了。

**swap 函数实现中使用了 malloc() 函数。** 这就是交换数据时，需要用的临时存储空间。

现在的 swap 函数新增的 size 参数，使用 memcpy 内存拷贝函数以及 malloc 动态内存分配函数，增加了编程的复杂度。这就是 C 语言的类型抽象所带来的复杂度提升。

### swap 函数的泛型版本 2（宏定义）

除了使用 void* 来做泛型之外，在 C 语言中，还可以使用**宏定义**来做泛型，代码如下：

```c
#define swap(x,y,size) \
{\
    char p = malloc(size); \
    memcpy(temp, &x, size); \
    memcpy(&x, &y, size); \
    memcpy(&y, temp, size); \
}
```

因为宏的本质是做字符替换，使用宏会导致代码膨胀的问题，导致编译出的执行文件比较大。

不过，对于 swap 这个简单的函数来说，用 void* 和 宏替换都可以达到泛型。如果是 min() 或 max() 函数，那么宏替换会暴露更多的问题。比如，对于下面的这个宏：

```c
#define min(x, y) ((x) > (y) ? (y) > (x))
```

其中，最大的问题，就是有可能会有**重复执行**的问题。如：

**min(i++, j++)** 对这个示例来说，我们本意是比较完后，对变量 i 和 j 做累加，实际情况却会导致变量 i 或 j 被累加两次。

**min(foo(), bar())** 对这示例来说，我们本意是比较 foo() 和 bar() 函数的返回值，然而，经过宏替换后，foo() 或 bar() 函数会被调用两次。

### swap 函数泛型的其它问题

对于 void* 和 宏定义版本，都加了一个 size。这是因为不加入 size 的话，那么我们函数内部就需要自己检查 size，然而，void* 这种地址方式是没法得到 size 的。而宏定义方式，虽然不会把类型给隐藏掉，可以使用向 sizeof(x) 这样的方式得到size。但如果类型是 char*，使用 sizeof 方式只能得到指针类型的size，而不是指针指向数据的 size。另外，对于不同类型，比如说 double 和 int，那么应该使用谁的 size 呢？这些都是问题。

于是，这种泛型，我们根本没有办法检查传入参数的 size，我们只能加入一个 size 参数，从而增加函数接口的复杂度，然后把问题抛给调用者。

### 一个更为复杂的泛型示例 - search 函数

如果我们把这个事情变得更复杂一点，写一个 search 函数，再传一个数组，然后想搜索 target，搜到返回数组下标，搜不到返回 -1。search 的泛型版本代码如下：

```c
int search(void* x, int len, void* target, 
	int elem_size, int (*cmpFn)(void*, void*))
{
    for (int i = 0; i < len; i++)
    {
        if (cmpFn((unsigned char*)x + elem_size * i, target) == 0)
        {
            return i;
        }
    }

    return -1;
}
```

**elem_size 的作用** 函数接口上增加了一个 elem_size，也就是数组里面每个元素的 size。这样，当我们遍历数组的时候，可以通过这个 elem_size 正确地移动指针到下一个数组元素。

**cmpFn 的作用** 由于我们要去比较数据里的每个元素和 target 是否相等，因为不同数据类型的比较实现不一样。比如，int 类型比较用 == 就行了，但是如果字符串数组，那么比较就需要用 strcmp 这类的函数。而如果你传以恶搞结构体数组，那么比较两个数据对象是否一样就比较复杂了，所以，必须要自定义一个比较函数。

在上面的代码中，我们没有使用 memcmp 函数，是因为，如果这个数组是一个指针数组，或是这个数组是一个结构体数组，而结构体数组中有指针成员。我们想比较的是指针指向的内容，而不是指针本身这个变量。所以，用 memcmp 会导致我们在比较指针（内存地址），而不是指针所指向的值。

所以，调用者需要提供类似下面的比较函数：

```c
int int_cmp(int* x, int* y)
{
    return *x - *y;
}

int string_cmp(char* x, char* y)
{
    return strcmp(x, y);
}
```

如果面对有业务类型的结构体，可能会是这样的比较函数：

```c
typedef struct _account 
{
    char name[10];
    char id[20];
} Account;

int account_cmp(Account* x, Account* y)
{
    int n = strcmp(x->name, y->name);
    if (n != 0)
    {
        return n;
    }
    else
    {
        return strcmp(x->id, y->id);
    }
}
```

你能使用 C 语言把泛型干成上面这个样子，看上去还行。但是，上面的 search 函数只能用于数组这样的顺序型的数据结构。如果 search 函数能支持一些非顺序型的数据结构，比如：堆、栈、哈希表、树、图，那么，用 C 语言来干泛型，基本上是完成不了。对于像 search 这样的算法来说，数据类型的泛型问题就已经把事情搞得很复杂了，然而，数据结构的泛型问题会提升几个数量级的复杂度。

### 总结

如果说，程序 = 算法 + 数据结构，那么 C 语言会存在几个问题：

**适配数据类型。** 一个通用的算法，需要对所处理数据的数据类型进行适配。但在适配数据类型的过程中，C 语言只能使用 void* 或宏替换的方式，这两种方式导致了类型过于宽松，并带来了很多其它问题。

**适配数据结构。** 算法其实是在操作数据结构，而数据则是放在数据结构中的，所以，真正的泛型除了适配数据类型外，还要适配数据结构，最后这个事情导致泛型算法的复杂度急剧上升。比如，容器内存的分配和释放，不同的数据结构体可能有非常不一样的内存分配和释放模型；再比如对象之间的复制，是深拷贝，还是浅拷贝。
