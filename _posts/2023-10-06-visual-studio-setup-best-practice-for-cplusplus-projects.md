---
layout: post
title: 在 Visual Studio 中配置 C/C++ 项目的最佳实践
enable: true
---

今天，我将谈论在 Visual Studio 中，配置项目的最佳实践。

这是我使用的最佳实践，可能不是你的最佳实践。

### 创建一个 Calculator 项目

尝试这点一下 "Solution Explorer" 下的 "Show All Files"：

<img src="/images/project-setup1.png" width="80%">

### 添加项目文件

在 Calculator 项目下，增加一个 "src" 文件夹，然后添加一个 main.cpp 文件：

<img src="/images/project-setup2.png" width="80%">

### 设置项目属性页

将项目属性页中的 "Output Directory" 和 "Intermediate Directory" 设置如下：

<img src="/images/project-setup3.png" width="80%">

### 构建项目

Build 项目，会得到项目目录结构如下：

<img src="/images/project-setup6.png" width="80%">

bin 目录结构如下：

<img src="/images/project-setup4.png" width="80%">

Calculator 目录结构如下：

<img src="/images/project-setup5.png" width="80%">

### 总结

你还可以对编译，链接，优化等选项进行设置。好的项目目录结构，会让你的生活变得更容易。