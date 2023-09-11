---
layout: post
title: 如何在 Jekyll 生成的 Github 页面中使用 MathJax
---

我在用 Jekyll + Github Pages 搭建自己的技术博客时，遇到了一个问题：如何在 Jekyll 中支持 MathJax？通过在 google 上搜索解决方案，我最终解决了这个问题。

下面的做法将帮助你在 Jekyll 中使用 MathJax：

1. 使用正确的 Markdown 解释器
   
   在<code>_config.yml</code>使用 kramdown

2. 修改页面模板

   现在我们必须修改页面模板以添加 MathJax 的支持。

   在你的 _includes 目录中创建一个名为 mathjax_support.html 文件，如下所示：
   ```
   <script type="text/x-mathjax-config">
      MathJax.Hub.Config({
         TeX: {
            equationNumbers: {
               autoNumber: "AMS"
            }
         },
         tex2jax: {
            inlineMath: [ ['$','$'], ["\\(","\\)"] ],
            displayMath: [ ['$$','$$'], ["\\[","\\]"] ],
            processEscapes: true,
            processEnvironments: true
         },
         "HTML-CSS": { availableFonts: ["TeX"] }
      });
   </script>
   <script type="text/javascript" src="https://cdn.mathjax.org/mathjax/latest/MathJax.js?config=TeX-AMS_HTML-full"></script>
   ```

3. 修改页面布局模板
   
   以布局模板 post.html 为例，在 <head> 标签里添加以下内容：
   
   ```
   {% raw %}
   <head>
      ...
      {% if page.use_math %}
         {% include mathjax_support.html %}
      {% endif %}
      ...
   </head>
   {% endraw %}
   ```

4. 页面配置设置
   
   在有数学公式的页面配置中，添加 use_math: true。
   
   ```
   ---
   layout: post
   title: 如何在 Jekyll 生成的 Github 页面使用 MathJax
   use_math: true
   ---
   ```

参考资料：
1. [How to use MathJax in Jekyll generated Github pages](https://haixing-hu.github.io/programming/2013/09/20/how-to-use-mathjax-in-jekyll-generated-github-pages/)
2. [Jekyll and Mathjax](https://talk.jekyllrb.com/t/jekyll-and-mathjax/5514)
