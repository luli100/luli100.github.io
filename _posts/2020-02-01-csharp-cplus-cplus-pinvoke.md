---
layout: post
title: P/Invoke，C# 与 C++ 交互技术
enable: false
---

            <p>P/Invoke『Platform Invoke』是可用于从托管代码访问非托管库中的结构、回调和函数的一种技术。</p>
            <p>本博文教程源码 <a href="https://gitee.com/luli100/pinvoke-tutorial">https://gitee.com/luli100/pinvoke-tutorial</a>。</p>
            <h3>结构体的封装和调用方式</h3>
            <p>默认情况下，非托管结构与托管结构在内存中的布局不同，因此，成功跨托管/非托管边界传递结构需要额外的步骤来保留数据完整性。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>#include &lt;iostream&gt;
#include &lt;comdef.h&gt;
using namespace std;

struct Book
{
    int32_t id;
    BSTR name;
    float price;
};

extern "C"
{
    __declspec(dllexport) void Print(Book book)
    {
        cout &lt;&lt; "Book Id: " &lt;&lt; book.id &lt;&lt; endl;
        wcout &lt;&lt; "Book Name: " &lt;&lt; book.name &lt;&lt; endl;
        cout &lt;&lt; "Book Price: " &lt;&lt; book.price &lt;&lt; endl;
    }

    __declspec(dllexport) Book Generate()
    {
        Book book = Book();
        book.id = 100;
        book.name = SysAllocString(L"C++");
        book.price = 100.0;
        return book;
    }
}</code></pre></div></div>
            <p>非托管模块是一个 DLL，它定义了一个 Book 结构和两个函数（Generate，Print）。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>using System.Runtime.InteropServices;

namespace TestShoppingService
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Book
    {
        public Int32 Id;
        [MarshalAs(UnmanagedType.BStr)]
        public String Name;
        public Single Price;
    }

    class Program
    {
        [DllImport("ShoppingService.dll", EntryPoint = "Generate")]
        private static extern Book Generate();
        [DllImport("ShoppingService.dll", EntryPoint = "Print")]
        private static extern void Print(Book book);

        static void Main(String[] args)
        {
            var book = Generate();
            Print(book);
            book.Name = "C#";
            book.Price = 200;
            Print(book);

            Console.ReadLine();
        }
    }
}</code></pre></div></div>
            <p>托管模块，也即 C# 代码，它导入非托管 DLL 中的 Generate 函数和 Print 函数，根据托管模块重新定义了一个 Book 结构来等效于非托管模块的 Book 结构。事实上，这两个结构的名字可以不同，内容大小一致也可以完成同样的功能。</p>
            <p>请注意，DLL 的任何部分都未使用传统的 #include 指令向托管代码公开。DLL 仅在运行时访问，因此在编译时不会检测到使用 DllImport 导入函数的问题。</p>
            <h3>为什么要使用 extern "C"</h3>
            <p>C++ 编译器会给程序中的每个函数换一个独一无二的名字。在 C 语言中，这个过程是不需要的，因为没有函数重载。重载不兼容于绝大部分链接程序，因为链接程序通常无法分辨同名的函数。名变换是对链接程序的妥协；链接程序通常坚持函数名必须独一无二。不要将 extern "C" 看作是声明这个函数是用 C 语言写的，应该看作是声明这个函数被当作好象 C 语言写的一样而进行调用。不管如何，它总意味着一件事：名变换被禁止了。</p>
            <h3>类的封装和调用方式</h3>
            <p>由于 C# 中结构是值类型，类是引用类型，因此，在封送 C++ 类时，需要做特殊处理。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>#include &lt;iostream&gt;
using namespace std;

class Book
{
public:
    Book(int32_t id, string name, float price);
    void Print();
    void ChangeName(string name);
    void UpdatePrice(float price);
private:
    int32_t id;
    string name;
    float price;
};

Book::Book(int32_t id, string name, float price) :id(id), name(name), price(price)
{

}

void Book::Print()
{
    cout &lt;&lt; "Book Id: " &lt;&lt; id &lt;&lt; endl;
    cout &lt;&lt; "Book Name: " &lt;&lt; name &lt;&lt; endl;
    cout &lt;&lt; "Book Price: " &lt;&lt; price &lt;&lt; endl;
}

void Book::ChangeName(string name)
{
    this->name = name;
}

void Book::UpdatePrice(float price)
{
    this->price = price;
}

extern "C"
{
    __declspec(dllexport) Book* CreateBook(int32_t id, const char* name, float price)
    {
        return new Book(id,name,price);
    }

    __declspec(dllexport) void DeleteBook(Book* book)
    {
        delete book;
    }

    __declspec(dllexport) void Print(Book* book)
    {
        book->Print();
    }

    __declspec(dllexport) void ChangeName(Book* book, const char* name)
    {
        book->ChangeName(name);
    }

    __declspec(dllexport) void UpdatePrice(Book* book, float price)
    {
        book->UpdatePrice(price);
    }
}</code></pre></div></div>
            <p>通常来说，C++ 代码会提供一个 C++ 原始类的包装类供 C# 调用。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>using System.Runtime.InteropServices;

namespace TestShoppingService
{
    internal class Book
    {
        [DllImport("ShoppingService.dll", EntryPoint = "CreateBook")]
        private static extern IntPtr CreateBook(Int32 id, String name, Single price);
        [DllImport("ShoppingService.dll", EntryPoint = "DeleteBook")]
        private static extern void DeleteBook(IntPtr bookPtr);
        [DllImport("ShoppingService.dll", EntryPoint = "Print")]
        private static extern void Print(IntPtr bookPtr);
        [DllImport("ShoppingService.dll", EntryPoint = "ChangeName")]
        private static extern void ChangeName(IntPtr bookPtr, String name);
        [DllImport("ShoppingService.dll", EntryPoint = "UpdatePrice")]
        private static extern void UpdatePrice(IntPtr bookPtr, Single price);
        private IntPtr bookPtr;
        public Book(Int32 id, String name, Single price)
        {
            this.bookPtr = CreateBook(id,name,price);
        }

        ~Book()
        {
            DeleteBook(this.bookPtr);
            this.bookPtr = IntPtr.Zero;
        }

        public void Print()
        {
            Print(this.bookPtr);
        }

        public void ChangeName(String name)
        {
            ChangeName(this.bookPtr, name);
        }

        public void UpdatePrice(Single price)
        {
            UpdatePrice(this.bookPtr, price);
        }
    }

    class Program
    {
        static void Main(String[] args)
        {
            Book book = new Book(100,"C#",200);
            book.Print();
            book.ChangeName("Linux");
            book.UpdatePrice(300);
            book.Print();
            Console.ReadLine();
        }
    }
}</code></pre></div></div>
            <p>C# 通常也提供一个类来封装 C++ 的包装类，供其它 C# 代码调用。</p>
            <h3>C++ 如何调用 C# 函数</h3>
            <p>有时候，我们需要 C++ 可以主动调用 C# 函数。我们知道，C# 实现回调功能可以用委托进行包装，最为关键的是 C++ 可以通过函数指针直接接收 C# 委托来实现回调。</p>
            <div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>#include &lt;iostream&gt;

extern "C"
{
    typedef void(__stdcall* PCALLBACK) (int32_t count);
    PCALLBACK pcallback = nullptr;
    _declspec(dllexport) void SetCallback(PCALLBACK callback)
    {
        pcallback = callback;
    }

    _declspec(dllexport) void Notify()
    {
        if (pcallback != nullptr)
        {
            pcallback(100);
        }
    }
}</code></pre></div></div>
            <p>从 C++ 代码可以看到，PCALLBACK 就是定义的函数指针，接收一个 int32_t 参数。SetCallback 函数用于将 C# 传递进来的委托转换为函数指针。Notify 函数的作用是向 C# 提供一个函数调用，从而触发 C++ 代码调用 C# 委托。</p>
<div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>class Program
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void DataChangedCallBack(Int32 value);
    [DllImport("ShoppingService.dll", EntryPoint = "SetCallback")]
    private static extern void SetCallback(DataChangedCallBack callback);
    [DllImport("ShoppingService.dll", EntryPoint = "Notify")]
    private static extern void Notify();
    static void Main(String[] args)
    {
        SetCallback((t) => { Console.WriteLine($"C++ 调用 C# 函数，结果为：{t}"); });
        Notify();
        Console.ReadLine();
    }
}</code></pre></div></div>
            <p>需要注意的是，DataChangedCallBack 委托特性标注 [UnmanagedFunctionPointer(CallingConvention.StdCall)] 中的  CallingConvention.StdCall 应与 C++ 代码的函数指针声明 __stdcall 保持一致。</p>
            <h3>结论</h3>
            <p>当你遇到 C++ 代码比 C# 代码更能解决你的需求场景时，千万别忘了 P/Invoke 的存在。</p>
        </div>
        
        <script>
            if (/mobile/i.test(navigator.userAgent) || /android/i.test(navigator.userAgent))
            {
                document.body.classList.add('mobile');
            }
        </script>
    </body>
</html>