---
layout: post
title: 如何在 Jekyll 生成的 Github 页面使用 MathJax
use_math: true
---

在我用 Jekyll + Github pages 搭建自己的技术博客时，遇到了一个问题：如何在 Jekyll 中支持 MathJax？通过在 google 上搜索解决方案，我最终解决了这个问题。

下面的做法将帮助你在 Jekyll 中使用 MathJax：

1. 使用正确的 Markdown 解释器
   在<code>-config.yml</code>使用 kramdown
$x+y+z=1$

$$x+y+z=1$$