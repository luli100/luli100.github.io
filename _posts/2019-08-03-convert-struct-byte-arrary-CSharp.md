---
layout: post
title: C# 结构体和字节数组之间的转换
enable: true
---

默认情况下，在 C# 中创建任何对象都存在于托管内存中，这有很多有点，比如自动垃圾回收。另一方面，在 C# 中使用非托管内存更加困难，需要我们手动分配和释放内存空间。如果忘记释放内存空间，会导致内存泄漏，最终导致应用程序崩溃。

在 C# 中，结构体和字节数组之间的转换所需的全部功能，都可以在 Marshal 静态类中找到。

### 结构体转字节数组

结构体转字节数组，可以认为是序列化。

```
public static Byte[] Serialize<T>(T t) where T : struct
{
    var size = Marshal.SizeOf(typeof(T));
    var array = new Byte[size];
    var ptr = Marshal.AllocHGlobal(size);
    Marshal.StructureToPtr(t, ptr, true);
    Marshal.Copy(ptr, array, 0, size);
    Marshal.FreeHGlobal(ptr);
    return array;
}
```

### 字节数组转结构体

字节数组转结构体，可以认为是反序列化。

```
public static T Deserialize<T>(byte[] array) where T : struct
{
    var size = Marshal.SizeOf(typeof(T));
    var ptr = Marshal.AllocHGlobal(size);
    Marshal.Copy(array, 0, ptr, size);
    var t = (T)Marshal.PtrToStructure(ptr, typeof(T));
    Marshal.FreeHGlobal(ptr);
    return t;
}
```

### 结论

当然，你也可以使用 BinaryFormatter 或者 BinaryWriter/BinaryReader 完成结构体和字节数组之间的转换。但绝大多数情况下，本文的方式可能是最佳实践。