"""生成“系统架构对话”中的可缩放 SVG 示意图，仅使用 Python 标准库。"""
from pathlib import Path
from html import escape

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "images/posts/system-architecture"
OUT.mkdir(parents=True, exist_ok=True)

def text(x, y, value, size=22, anchor="middle", cls=""):
    lines = value.split("\n")
    return f'<text x="{x}" y="{y}" font-size="{size}" text-anchor="{anchor}" class="{cls}">' + "".join(
        f'<tspan x="{x}" dy="{0 if i == 0 else size * 1.4}">{escape(line)}</tspan>' for i, line in enumerate(lines)
    ) + '</text>'

def box(x, y, w, h, value, cls="box", size=22):
    lines = len(value.split("\n"))
    baseline = y + h / 2 - (lines - 1) * size * .7 + size * .34
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="7" class="{cls}"/>' + text(x+w/2, baseline, value, size)

def arrow(points, dashed=False):
    return f'<polyline points="{points}" class="arrow" marker-end="url(#tip)"' + (' stroke-dasharray="6 5"' if dashed else '') + '/>'

def line(x1, y1, x2, y2, cls="line"):
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" class="{cls}"/>'

def save(name, height, title, desc, elements):
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 {height}" role="img" aria-labelledby="title desc">
<title id="title">{escape(title)}</title><desc id="desc">{escape(desc)}</desc>
<defs><marker id="tip" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10z" fill="#806a4d"/></marker></defs>
<style>
text {{font-family:'Microsoft YaHei','PingFang SC','Noto Sans CJK SC',sans-serif;fill:#27342f}}
.title {{font-weight:700}} .muted {{fill:#61695f}} .emphasis {{fill:#7b522c;font-weight:700}}
.box {{fill:#fff;stroke:#aeb9ad;stroke-width:1.8}}
.soft {{fill:#eef2e9;stroke:#aeb9ad;stroke-width:1.8}}
.warning {{fill:#fbefdf;stroke:#c3a078;stroke-width:1.8}}
.boundary {{fill:#f1f2ed;stroke:#bbc4b6;stroke-width:1.6;stroke-dasharray:6 5}}
.arrow,.line {{fill:none;stroke:#806a4d;stroke-width:2.2;stroke-linejoin:round}}
.faint {{stroke:#d4dbce;stroke-width:1.4}}
</style>
<rect width="600" height="{height}" rx="10" fill="#f8f8f3"/>
{text(28, 38, title, 23, 'start', 'title')}
{''.join(elements)}
</svg>'''
    (OUT / (name + '.svg')).write_text(svg, encoding='utf-8', newline='\n')

save('01-cache-tradeoff',410,'缓存改变查询路径，也增加更新责任','直接查询与缓存查询的关系比较。',[
    text(28,80,'原来',20,'start','muted'),box(28,100,170,62,'查询请求'),box(380,100,190,62,'数据源'),arrow('202,131 372,131'),text(287,115,'每次回源',20),
    text(28,214,'加入缓存以后',20,'start','muted'),box(28,242,124,62,'查询请求'),box(236,242,128,62,'缓存','soft'),box(448,242,124,62,'数据源'),
    arrow('158,273 228,273'),text(193,257,'先查',19),arrow('370,273 440,273'),text(405,257,'未命中',19),
    arrow('510,309 510,340 300,340 300,310',True),text(300,382,'写入后，缓存需要失效或更新',21,cls='emphasis')])

save('01-scenario',470,'把“高可用”写成具体场景','六要素：刺激源、刺激、环境、制品、响应与响应度量。',[
    box(18,66,564,264,'','boundary'),text(36,96,'环境：系统正常运行时',21,'start','muted'),
    box(34,121,224,68,'刺激源\n运维人员'),box(342,121,224,68,'刺激\n误删数据库'),arrow('264,155 334,155'),
    box(34,235,224,68,'制品\n业务数据库'),box(342,235,224,68,'响应\n发现、告警并恢复'),arrow('454,194 454,211 146,211 146,229'),arrow('264,269 334,269'),
    box(30,363,540,75,'响应度量（示例）\n2小时内恢复；丢失窗口不超过5分钟','soft',21)])

e=[text(30,90,'服务甲：每天10次，每次中断1秒',21,'start'),line(35,127,565,127)]
for x in range(90,550,46): e.append(f'<rect x="{x}" y="116" width="6" height="22" fill="#bc8b64"/>')
e += [text(30,173,'全年中断 3,650 秒，约 1 小时',22,'start','emphasis'),text(30,233,'服务乙：每年1次，中断48小时',21,'start'),line(35,268,565,268),f'<rect x="288" y="257" width="85" height="22" fill="#bc8b64"/>',text(30,311,'故障更少，不等于不可用时间更短',22,'start','emphasis'),text(30,344,'标记用于说明差别，不按时间比例绘制',18,'start','muted')]
save('01-reliability',360,'故障次数和不可用时间，是两笔账','甲频繁短中断，乙少量长中断；应分别评价连续工作能力和可用时间。',e)

save('02-buffer-budget',470,'64 MB 缓冲，究竟能撑多久？','短暂停顿与持续过载的容量预算。',[
    box(24,78,140,78,'输入\n26 MB/s'),box(230,78,140,78,'缓冲\n64 MB','soft'),box(436,78,140,78,'消费\nμ MB/s'),arrow('170,117 222,117'),arrow('376,117 428,117'),
    line(28,192,572,192,'faint'),text(30,233,'暂停消费：μ = 0',22,'start'),text(300,281,'64 ÷ 26 ≈ 2.46 秒',30,cls='emphasis'),
    line(28,315,572,315,'faint'),text(30,355,'持续过载：μ = 25 MB/s',22,'start'),text(300,404,'每秒积压 1 MB，约 64 秒填满',25,cls='emphasis'),text(300,448,'理想预算：初始为空，未计额外开销',18,cls='muted')])

save('02-process-boundaries',480,'进程隔离和资源独立，是不同的事','拆分进程可以隔离部分运行时和故障影响，但仍共享主机资源。',[
    box(24,77,552,139,'','boundary'),text(44,108,'单进程',21,'start','muted'),box(44,130,145,55,'界面'),box(226,130,145,55,'解析'),box(408,130,145,55,'五路接收'),
    text(300,246,'共享地址空间与进程内运行时',21,cls='muted'),
    box(24,290,165,69,'界面进程'),box(218,290,165,69,'采集进程1'),box(412,290,164,69,'采集进程5'),text(300,388,'……每路有自己的进程边界',20,cls='muted'),
    box(24,417,552,43,'仍共享 CPU、网卡、磁盘和主机电源','warning',20)])

save('02-ring-buffer',410,'复用槽位，需要明确所有权','槽位从空闲到写入、可读、读取后归还，写者不能覆盖仍在使用的数据。',[
    text(300,100,'固定存储循环复用，减少反复分配',22,cls='muted'),
    box(24,181,114,74,'空闲','soft'),box(170,181,114,74,'写入'),box(316,181,114,74,'可读','soft'),box(462,181,114,74,'读取'),
    arrow('143,218 162,218'),arrow('289,218 308,218'),arrow('435,218 454,218'),
    arrow('519,261 519,310 81,310 81,261'),text(300,300,'读完归还',22),
    text(300,369,'缓冲满了：等待、拒绝或丢弃，必须事先约定',20,cls='emphasis')])

e=[]
for i,(name,budget) in enumerate([('发现异常','检测'),('隔离旧实例','隔离'),('启动新实例','启动'),('恢复配置与状态','恢复'),('验证业务重新就绪','验证')]):
    y=76+i*65
    e += [text(80,y+30,'T'+budget,21),box(174,y,398,48,name,'soft' if i==4 else 'box',22)]
    if i<4:e.append(arrow(f'373,{y+51} 373,{y+59}'))
e.append(text(300,424,'服务恢复时间 = 上述各段之和',22,cls='emphasis'))
save('03-recovery-timeline',440,'创建进程，只完成了恢复的一段','检测、隔离、启动、状态恢复和业务验证共同构成恢复过程。',e)

save('03-file-recovery',400,'写到一半的文件，怎样重新读？','完整记录后出现不完整尾部；恢复依据是格式约定和有效边界。',[
    box(24,100,122,82,'完整\n记录1','soft'),box(168,100,122,82,'完整\n记录2','soft'),box(312,100,122,82,'完整\n记录3','soft'),box(456,100,120,82,'不完整\n尾部','warning'),
    text(300,226,'依据：边界、长度、序号、校验',22),arrow('516,190 516,256'),
    box(24,263,552,66,'识别缺失；截断、跳过或标记损坏部分','box',21),text(300,372,'处理规则和持久性保证，需要故障测试证明',20,cls='emphasis')])

save('03-failure-domains',460,'有两份实例，还要问共用什么','同机进程与跨机冗余的故障域比较。',[
    text(150,83,'同一台主机',22),text(450,83,'不同主机',22),box(24,108,252,220,'','boundary'),
    box(45,134,210,64,'采集进程 A'),box(45,237,210,64,'采集进程 B'),
    box(324,108,252,95,'主机 A\n独立采集与存储'),box(324,233,252,95,'主机 B\n独立采集与存储'),
    text(150,365,'电源、OS、磁盘共享',19,cls='muted'),text(450,365,'可覆盖更多主机故障',19,cls='muted'),
    text(300,420,'输入复制、故障判定、结果选择仍要设计',21,cls='emphasis')])

e=[text(30,86,'方案一：在实时路径转换',21,'start','muted')]
for x,label in zip([24,168,312,456],['厂家输入','实时转换','统一格式','后处理']):e.append(box(x,111,120,66,label,'soft' if x==168 else 'box',20))
for x in [149,293,437]:e.append(arrow(f'{x},144 {x+12},144'))
e += [text(300,219,'协议适配和转换成本，进入接收链路',21,cls='emphasis'),line(28,251,572,251,'faint'),text(30,293,'方案二：先记录，再离线适配',21,'start','muted')]
for x,label in zip([24,168,312,456],['厂家输入','原样记录','离线适配','统一结果']):e.append(box(x,319,120,66,label,'soft' if x==312 else 'box',20))
for x in [149,293,437]:e.append(arrow(f'{x},352 {x+12},352'))
e.append(text(300,437,'协议版本与采集配置，必须和原始数据匹配',20,cls='emphasis'))
save('04-change-boundary',470,'把协议差异留在哪一侧？','实时转换和离线转换把修改与计算成本放在不同位置。',e)

save('04-replay-testing',460,'回归一致，不等于已经证明正确','固定条件后比较新旧结果；再以独立确认的期望结果验证正确性。',[
    box(100,80,400,67,'固定输入、配置和协议版本','soft',22),arrow('300,152 300,180 150,180 150,211'),arrow('300,180 450,180 450,211'),
    box(30,220,240,60,'旧解析器'),box(330,220,240,60,'新解析器'),arrow('150,285 150,317'),arrow('450,285 450,317'),
    box(30,325,240,54,'旧结果'),box(330,325,240,54,'新结果'),arrow('275,352 322,352'),text(300,315,'比差异',19),
    text(300,420,'正确性另需独立确认的样本与判据',22,cls='emphasis')])

save('04-data-protection',430,'数据交接，需要几种不同的保障','采集、交接、归档、备份分别需要权限、可信校验、审计和恢复验证。',[
    text(99,94,'身份与权限',20,cls='muted'),text(299,94,'可信校验',20,cls='muted'),text(499,94,'访问与审计',20,cls='muted'),
    box(24,124,150,76,'采集机'),box(224,124,150,76,'交接验证','soft'),box(424,124,150,76,'归档系统'),arrow('180,162 216,162'),arrow('380,162 416,162'),
    text(30,277,'哈希基准也要保护',21,'start'),text(30,315,'拷贝成功 ≠ 能够恢复',21,'start','emphasis'),
    arrow('499,206 499,274'),box(424,283,150,68,'独立备份'),text(499,390,'恢复演练',20,cls='muted')])

print(f"Generated {len(list(OUT.glob('*.svg')))} SVG diagrams in {OUT}")
