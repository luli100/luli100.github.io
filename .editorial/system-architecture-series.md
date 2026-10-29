# “系统架构对话”系列维护约定

## 来源与范围

第一批文章依据本机 `E:\工作\14_架构设计与演进\软考高级-系统架构设计师\课程\讲义` 中第 8 章前四讲加工，未修改原讲义。学习记录中的学习者回答可能有误，不能作为正确答案直接转发。

本次输出为四篇 HTML 博客，按用户最新要求归档为 2020-10-08 到 2020-10-11，保持每日一篇。原讲义记录的是 2026 年的学习日期；博客归档年份已由用户明确调整为 2020 年，不应再从原讲义或实际整理日期覆盖回来。

## 连载规则

- 系列标识固定为 `system-architecture`，名称为“系统架构对话”。定义放在 `_data/series.yml`。
- 新篇使用 `layout: post`、`enable: true`、`series: system-architecture`，并给出不重复的整数 `series_order`。下一篇为 5。
- 既有链接保持稳定。系列入口 `/series/system-architecture/` 按篇次自动列出已发布文章，前后篇由 `_includes/series-nav.html` 自动生成。
- 日期遵循用户指定的 2020 年归档规则，月日参考学习文档并保持每日一篇。若新资料日期重叠或与已发布篇次冲突，应先说明冲突，不自动改成整理当天。
- 不自动把目录中的新文件发布成文章；仍需阅读、加工、核实与验证。

## 写作标准

以真实问题推进对话，保留直觉为什么不够、追问如何补上条件、修正后的判断怎样使用。对话可以重组和压缩，但不虚构作者亲自完成的测试、测量或项目结果。

围绕一个贯穿案例连接不同主题。不是每个问题都配一个标准答案或考试术语；避免把真题、教材清单和讲义修订记录直接当作博客主体。未经核实的教材页码、真题年份与答案不带入文章。

图应解释结构、时间、边界或因果关系。已有 12 张原生 SVG，源文件生成脚本为 `.editorial/build-architecture-diagrams.py`，运行只依赖 Python 标准库。新增图用有意义的文件名，保留 title、desc、正文替代文本和图注；避免把完整段落塞进图片。图片可点击查看 SVG。

示例数字要写清条件、单位和是否为实测。不能把缓冲容量预算当作性能保证；不能用主备、热备名称直接承诺切换时延；不能把存活等同于业务健康；不能把回归结果一致等同于正确；不能把无保护的哈希文件当作真实性证明。

## 发布验证

检查 HTML 结构、目录锚点、图片路径与图中文字边界；核对每天一篇和 series_order。需要浏览器检查时遵守电脑操作工具的当前规则，不绕过工具的停止或网址校验限制。

发布后确认四类页面：系列入口、每篇文章、前后篇和首页系列入口；原有文章、手记和导航不得因系列模板失效。未来文章应沿用这些检查。

## 核实资料

- SEI：[Eliciting and Specifying Quality Attribute Requirements](https://sei.cmu.edu/documents/5465/2013_018_101_60984.pdf)
- SEI：[Deriving Architectural Tactics](https://sei.cmu.edu/documents/704/2003_005_001_14213.pdf)
- SEI：[Realizing and Refining Architectural Tactics](https://www.sei.cmu.edu/library/file_redirect/2009_005_001_15101.pdf/)
- Microsoft：[Workstation and server garbage collection](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/workstation-server-gc)
- Google SRE：[Addressing Cascading Failures](https://sre.google/sre-book/addressing-cascading-failures/)
- PostgreSQL：[High Availability, Load Balancing, and Replication](https://www.postgresql.org/docs/current/high-availability.html)
- Microsoft：[Cryptographic services](https://learn.microsoft.com/en-us/dotnet/standard/security/cryptographic-services)
