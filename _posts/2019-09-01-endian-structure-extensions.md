---
layout: post
title: C# 跨机器大端小端字节数组转为结构体对象
enable: true
---

通常，在我们接收到网络字节数组后，会将字节数组转换为对应的结构体对象。

### 忽略大小端问题的实现方式

在 C# 语言中，下面代码是最优雅的，性能也能得到保证的实现方式。

```c#
using System.Runtime.InteropServices;

namespace EndianExtensionsTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Target
    {
        public Int32 Id;
        public Int32 x;
        public Int32 y;
        public Int32 z;
    }


    class Program
    {
        static void Main(String[] args)
        {
            Byte[] buffer = new Byte[16]
            { 
                0x00,0x01,0x02,0x03,
                0x04,0x05,0x06,0x07,
                0x08,0x09,0x0A,0x0B,
                0x0C,0x0D,0x0E,0x0F
            };

            var obj = ToStructureHostEndian<Target>(buffer);
            Console.WriteLine($"ID: {obj.Id}, X: {obj.x}, Y: {obj.y}, Z: {obj.z}");
        }

        public static T ToStructureHostEndian&lt;T&gt;(Byte[] buffer) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size != buffer.Length)
            {
                throw new ArgumentException("length of buffer not equal length of struct T");
            }

            var intPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, intPtr, size);
            var obj = Marshal.PtrToStructure&lt;T&gt;(intPtr);
            Marshal.FreeHGlobal(intPtr);

            return obj;
        }
    }
}
```

上面代码隐藏着一个问题，<strong>想象一下，假设发送端用大端方式将结构体对象转换成字节数组，接收端以小端方式将数组转换为结构体对象时，会导致发送端和接收端数据不一致；反之，同理。</strong>接下来，就是我解决问题的整个过程。

### 考虑大小端问题的实现方式（一）

为了解决发送端与接收端大小端编码解码不一致的情况，我们可以将转换后的结构体对象里的字段进行二次转换。

```c#
using System.Runtime.InteropServices;

namespace EndianExtensionsTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Target
    {
        public Int32 Id;
        public Int32 x;
        public Int32 y;
        public Int32 z;
    }

    class Program
    {
        static void Main(String[] args)
        {
            // 假设，bufer 是发送端以大端结构转为的字节数组
            Byte[] buffer = new Byte[16]
            { 
                0x00,0x01,0x02,0x03,
                0x04,0x05,0x06,0x07,
                0x08,0x09,0x0A,0x0B,
                0x0C,0x0D,0x0E,0x0F
            };

            var obj = ToStructureHostEndian<Target>(buffer);
            if (BitConverter.IsLittleEndian)
            {
                var ids = BitConverter.GetBytes(obj.Id);
                Array.Reverse(ids);
                obj.Id = BitConverter.ToInt32(ids, 0);

                var xs = BitConverter.GetBytes(obj.x);
                Array.Reverse(xs);
                obj.x = BitConverter.ToInt32(xs, 0);

                var ys = BitConverter.GetBytes(obj.y);
                Array.Reverse(ys);
                obj.y = BitConverter.ToInt32(ys, 0);

                var zs = BitConverter.GetBytes(obj.z);
                Array.Reverse(zs);
                obj.z = BitConverter.ToInt32(zs, 0);
            }

            Console.WriteLine($"ID: {obj.Id}, X: {obj.x}, Y: {obj.y}, Z: {obj.z}");
        }

        public static T ToStructureHostEndian&lt;T&gt;(Byte[] buffer) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size != buffer.Length)
            {
                throw new ArgumentException("buffer 的长度与结构体的字节长度不匹配");
            }

            var intPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, intPtr, size);
            var obj = Marshal.PtrToStructure&lt;T&gt;(intPtr);
            Marshal.FreeHGlobal(intPtr);

            return obj;
        }
    }
}
```

上面的实现方式存在的问题是，<strong>不优雅且扩展性差</strong>。想象一下，如果我们向 Target 结构体添加成百上千的字段，那么我们就会写成百上千相似的转换代码。是否有更好的方式解决这个问题呢？试一下反射。

### 考虑大小端问题的实现方式（二）

针对上面的问题，首先想到的就是利用 C# 语言的反射特性。利用反射特性重新给字段赋值，必须要将 Target 结构换为 Target 类，因为 <strong>C# 反射特性无法为结构体的公共字段赋值</strong>。

```c#
using System.Runtime.InteropServices;

namespace EndianExtensionsTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Target
    {
        public Int32 Id;
        public Int32 x;
        public Int32 y;
        public Int32 z;
    }

    class Program
    {
        static void Main(String[] args)
        {
            // 假设，bufer 是发送端以大端结构转为的字节数组
            Byte[] buffer = new Byte[16]
            { 
                0x00,0x01,0x02,0x03,
                0x04,0x05,0x06,0x07,
                0x08,0x09,0x0A,0x0B,
                0x0C,0x0D,0x0E,0x0F
            };

            var obj = ToStructureHostEndian<Target>(buffer);
            if (BitConverter.IsLittleEndian)
            {
                var fields = obj.GetType().GetFields();
                foreach (var field in fields)
                {
                    var typeCode = Type.GetTypeCode(field.FieldType);
                    switch (typeCode)
                    {
                        case TypeCode.Int32:
                            {
                                var value = (Int32)field.GetValue(obj);
                                var bytes = BitConverter.GetBytes(value);
                                Array.Reverse(bytes);
                                field.SetValue(obj, BitConverter.ToInt32(bytes));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            Console.WriteLine($"ID: {obj.Id}, X: {obj.x}, Y: {obj.y}, Z: {obj.z}");
        }

        public static T ToStructureHostEndian&lt;T&gt;(Byte[] buffer) where T : class
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size != buffer.Length)
            {
                throw new ArgumentException("buffer 的长度与结构体的字节长度不匹配");
            }

            var intPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, intPtr, size);
            var obj = Marshal.PtrToStructure&lt;T&gt;(intPtr);
            Marshal.FreeHGlobal(intPtr);

            return obj;
        }
    }
}
```
问题暂时得到了解决。需要注意的是，上面的代码必须完成 TypeCode 对应得值转换（先不考虑嵌套类型的情况）。大家都知道，利用反射特性必然会降低性能。有没有除了反射之外的方法呢？必须有。

### 考虑大小端问题的实现方式（三）

终极的解决方案是，反转 Target 结构体字段的内存布局为 TargetReversed 结构体，反转整个 buffer 内容。

```c#
using System.Runtime.InteropServices;

namespace EndianExtensionsTests
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Target
    {
        public Int32 Id;
        public Int32 x;
        public Int32 y;
        public Int32 z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TargetReversed
    {
        public Int32 z;
        public Int32 y;
        public Int32 x;
        public Int32 Id;
    }

    class Program
    {
        static void Main(String[] args)
        {
            // 假设，bufer 是发送端以大端结构转为的字节数组
            Byte[] buffer = new Byte[16]
            { 
                0x00,0x01,0x02,0x03,
                0x04,0x05,0x06,0x07,
                0x08,0x09,0x0A,0x0B,
                0x0C,0x0D,0x0E,0x0F
            };

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
                var obj = ToStructureHostEndian<TargetReversed>(buffer);
                Console.WriteLine($"ID: {obj.Id}, X: {obj.x}, Y: {obj.y}, Z: {obj.z}");
            }
            else
            {
                var obj = ToStructureHostEndian<Target>(buffer);
                Console.WriteLine($"ID: {obj.Id}, X: {obj.x}, Y: {obj.y}, Z: {obj.z}");
            }
        }

        public static T ToStructureHostEndian&lt;T&gt;(Byte[] buffer) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size != buffer.Length)
            {
                throw new ArgumentException("buffer 的长度与结构体的字节长度不匹配");
            }

            var intPtr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, intPtr, size);
            var obj = Marshal.PtrToStructure&lt;T&gt;(intPtr);
            Marshal.FreeHGlobal(intPtr);

            return obj;
        }
    }
}
```
### 结论

当遇到一个问题时，反复的思考，实践，琢磨，一定能够找到你心目中的最佳实践。