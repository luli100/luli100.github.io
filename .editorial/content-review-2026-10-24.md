# 博客内容清理记录 · 2026-10-24

本轮按独立阅读价值、内容完整性和关键结论的可验证性整理已有文章。首轮首页公开文章从 45 篇减至 33 篇，撤回 12 篇。补充撤回 MathJax 建站配置说明后，当前公开 32 篇，累计撤回 13 篇。原来 enable: false 的文章和两篇手记不在本次撤回范围。

所有撤回文章从 `_posts/` 移到 `_drafts/`，原文逐字节保留，未删除配图。普通 GitHub Pages 构建不会发布草稿，因此这些文章会同时退出首页、RSS/Atom 和站点地图，原文章网址也将不再提供正文。Git 历史和草稿均可恢复。

## 逐篇决定

| 文章 | 撤回原因 | 后续处理 |
| --- | --- | --- |
| [本人遵循的编码规范](../_drafts/2019-07-07-my-specification-of-programming.md) | 正文只有“避免使用 continue”标题和一个外链，没有自己的论证、示例或适用条件。 | 外链占位 |
| [实现有限状态机 FSM (1)](../_drafts/2020-04-04-finate-state-machine-1.md) | 只有一个标题和代码，没有问题背景、状态转移表、调用示例和运行结果；Turnstile 构造函数还是 private，未说明如何创建对象。 | 代码素材待成文 |
| [实现有限状态机 FSM (2)](../_drafts/2020-04-05-finate-state-machine-2.md) | 遍历迁移表命中后没有结束处理；原文代码在锁定状态收到一次 COIN，会依次执行 Unlock 和 ThankYou。构造函数同样是 private。 | 代码存在已复现的逻辑问题 |
| [实现有限状态机 FSM (3)](../_drafts/2020-04-06-finate-state-machine-3.md) | 只有状态模式代码，没有对前两种实现的比较和使用示例；初始状态为 unlocked，与前两篇的 LOCKED 不一致且没有说明。整组三篇应合并重写后再发布。 | 代码素材待成文 |
| [Data Fusion](../_drafts/2021-06-06-data-fusion.md) | 只有开场类比，末尾“数据融合”章节为空，尚未说明融合模型、权重如何确定或完整示例。 | 未完成 |
| [DBSCAN 算法](../_drafts/2022-04-06-dbscan.md) | 正文为空，只有文章元数据。 | 空白占位 |
| [选择 α-β-(γ) 滤波参数](../_drafts/2023-09-06-alpha-beta-gama-filter.md) | 只有公式，没有解释符号、噪声模型、适用假设、参数计算示例与来源；α-β 段定义 γ 后却未使用，需核对公式转录。 | 公式摘录待补全 |
| [工具箱](../_drafts/2023-11-10-toolbox.md) | 只有六个工具外链，缺少用途、使用场景和实际经验；可以日后并入独立资源页。 | 书签清单 |
| [逆矩阵](../_drafts/2021-04-04-inverse-of-a-matrix.md) | 例题矩阵与增广矩阵第二行不一致；按原题 A 乘文中 A^{-1}，得到 [[1,0,0],[6/5,19/10,1/10],[0,0,8/5]]，不是单位矩阵。需重新推导并验算。 | 验算未通过 |
| [卡尔曼滤波](../_drafts/2023-09-10-kalman-filter.md) | 开头讨论过程噪声，但预测公式未说明 Q=0 的限制；位置、速度分开使用标量增益，示例又用位置残差更新速度，未给出匹配的观测模型和交叉协方差。不能作为完整多维卡尔曼推导发布，需统一模型后重写。 | 模型与推导待重写 |
| [线程安全的 SafeObservableCollection&lt;T&gt;](../_drafts/2019-04-28-wpf-thread-safe-observable-collection.md) | 修改通过 BeginInvoke 异步入队，而继承的 Remove 等方法同步查找索引并返回。原文类在后台连续 Remove("A") 两次，两个调用均返回 true，队列执行后 A 和 B 都被删除；不满足标题对线程安全集合的承诺。 | 代码存在已复现的逻辑问题 |
| [C# 结构体和字节数组之间的转换](../_drafts/2019-08-03-convert-struct-byte-arrary-CSharp.md) | 新分配的内存首次 StructureToPtr 就传入 fDeleteOld=true；缺少 try/finally、输入长度校验和可封送类型限制，结尾还推荐 BinaryFormatter。应明确原生布局与序列化协议的区别后重写。 | 资源处理与建议需重写 |
| [如何在 Jekyll 生成的 Github 页面中使用 MathJax](../_drafts/2023-08-01-how-to-use-mathjax-in-jekyll-generated-github-pages.md) | 主要是建站配置备忘，缺少进一步的问题分析；示例仍引用已退役的 cdn.mathjax.org，与本站当前使用的 cdnjs 配置不一致。按精简博客的方向撤回，保留现有 MathJax 语法速查页。 | 配置备忘与过时示例 |

## 已做的核实

- 状态机（2）：直接提取原文 C# 代码，使用反射调用原文私有构造函数，输入一次 COIN，实际动作序列为 Unlock、ThankYou。一次事件被重复处理。
- SafeObservableCollection：直接提取原文类，在 .NET 8 WPF Dispatcher 上运行；初始集合为 [A,B]，后台连续两次 Remove(A) 都返回 true；派发队列处理后集合为空，错误删除了 B。
- 逆矩阵：用有理数精确运算计算原题矩阵与文中逆矩阵的乘积，结果不是单位矩阵。
- Marshal 的首次封送与旧结构销毁语义，对照 [Microsoft StructureToPtr 文档](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.structuretoptr)核实。原始输入长度不足时会抛异常，文章又没有 finally 释放已分配的内存。
- BinaryFormatter 建议，对照 [Microsoft 安全指南](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide)核实。
- WPF 集合同步要求，对照 [EnableCollectionSynchronization 文档](https://learn.microsoft.com/en-us/dotnet/api/system.windows.data.bindingoperations.enablecollectionsynchronization)核实。
- 卡尔曼协方差预测与过程噪声，对照 [Covariance Extrapolation Equation](https://kalmanfilter.net/covextrap.html)核实。该资料明确区分一般形式和 Q=0 的特例。

复现项目放在系统临时目录，未加入博客发布内容。没有运行原文中潜在不安全的非托管内存示例。

- 补充核实：文章示例中的 cdn.mathjax.org 服务已退役，见 [MathJax 官方公告](https://www.mathjax.org/cdn-shutting-down/)。本站实际配置已使用 cdnjs，本次只撤回文章，不修改数学公式渲染配置。

## 本轮保留的取舍

短文并不自动等于低质量。UDP 报文长度、WPF 控制台显示/隐藏、MATLAB 程序集调用等虽然篇幅不长，但包含明确问题与可用步骤，予以保留。MathJax 语法页有成对的语法和效果，作为速查页保留。

保留 WPF/异步、编译链接、P/Invoke、设计模式和“谁依赖谁”等主要技术文章，保留两篇个人手记。保留不等于已经证明每一段旧代码都没有问题。

保留文章有两项值得后续单独整理：大小端文章中关于反射不能修改结构体字段的绝对说法需要修订；FMCW 测距文章目前以视频双语文字稿为主，适合补图并重组为独立教程。本轮不把翻译资料仅因缺少原创性而下架。

同时移除了 Cache Aside 文章中一处未闭合的代码围栏，避免后续说明被错误显示为代码。

## 恢复方式

修改和验证草稿后，将相应文件从 `_drafts/` 移回 `_posts/`，沿用原文件名即可恢复原链接。发布前检查问题背景、示例输入输出、必要假设，以及原有读者可能复制的代码。

## 原稿校验值

移动前后 SHA-256 一致：

- `2019-07-07-my-specification-of-programming.md`：`5a03b3e07723e4a42545ed5ad9a0c6949bcbd44a0b1ed2a0a04b8a74e41cdb5a`
- `2020-04-04-finate-state-machine-1.md`：`38679e6206868db39c47ddee4af328988461dcc031d9030b71f0961eebcd4a60`
- `2020-04-05-finate-state-machine-2.md`：`2661b36c49dd0e0a0a80d1229b4edc6d928f470ca5045a68d2e1b9401648236c`
- `2020-04-06-finate-state-machine-3.md`：`2afe700774c7a822e4873df7c9ad0e6a732daffae07542a75c836780af81980d`
- `2021-06-06-data-fusion.md`：`7bc0430072581f193d868e0a89c64b0e04410fc0926d3524e4c14e4a008ab844`
- `2022-04-06-dbscan.md`：`cb77fe6f233e10f2cff9bb94d69703b5740a5de492c4aa638a5c61a5642a3518`
- `2023-09-06-alpha-beta-gama-filter.md`：`27173e64e4d3c4be019d80fda49d79383ead34b81f474a2d7053e91d47a0775d`
- `2023-11-10-toolbox.md`：`b4ac6cd3f8a53037391f5e33a46fe31922919b541f3a3cab160c6b9e4642572c`
- `2021-04-04-inverse-of-a-matrix.md`：`81fad36c4fbc795d98d3e6af82bbcaec81d6669f6acda9b7e5a4874c02765ed7`
- `2023-09-10-kalman-filter.md`：`f3863ddb1f6f0f0b35d937bae6292007466a477fe39bd021d00fc93d168b90c0`
- `2019-04-28-wpf-thread-safe-observable-collection.md`：`a0e127fcf9e20da41a3ba4511b881762a25c8a201e4ffe5ebf0b419896127c50`
- `2019-08-03-convert-struct-byte-arrary-CSharp.md`：`1329d584a3e28317b463ce7dbe4243dd2f333589998960726e27263bddcfcd6f`
- `2023-08-01-how-to-use-mathjax-in-jekyll-generated-github-pages.md`：`0c5cca5364e11fa31965905e0a9a4103837ab1b0f3c8152d5a56b686e0b409d9`
