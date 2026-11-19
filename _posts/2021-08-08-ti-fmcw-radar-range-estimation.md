---
layout: post
title: TI FMCW 雷达 - 测距
enable: true
use_math: true
description: 从发射与回波的频率差推导 FMCW 测距，结合距离 FFT 和数值算例，区分有效扫频带宽、距离格点间隔与 IF 限制的最远距离。
toc:
  - title: 距离为什么会变成频率差
    anchor: beat-frequency
  - title: 怎样从 FFT 找到多个目标
    anchor: range-fft
  - title: 两个目标离多近还能分开
    anchor: range-resolution
  - title: 最远距离由什么限制
    anchor: maximum-range
  - title: 同样的带宽，为什么还要选斜率
    anchor: chirp-tradeoff
  - title: 参考资料
    anchor: 参考资料
---

雷达前方 15 m 和 30 m 处各有一个反射目标。接收天线看到的是叠在一起的回波，雷达怎样从中读出两个距离？如果两个目标再靠近一些，什么时候就分不开了？

FMCW（调频连续波）雷达的做法是：**让发射频率随时间变化，把回波的延迟变成容易测量的差频。** 本文先用静止目标和线性上扫频说明测距原理，再看距离分辨率和最远距离分别受什么限制。文中的图和数值都是原理示例，不是实测结果或某款芯片的配置建议。

## 距离为什么会变成频率差 {#beat-frequency}

雷达在一段时间内以固定斜率提高发射频率，这段扫频信号称为 **chirp**。用 $$S$$ 表示频率变化的斜率，例如 20 MHz/µs 表示每过 1 µs，频率升高 20 MHz。

设静止目标距雷达 $$d$$，光速为 $$c$$，回波是发射信号的延迟副本。因为信号要走一个来回，延迟是 $$\tau=2d/c$$。当发射和回波都处于同一条 chirp 的线性扫频段时，它们在频率–时间图上是两条平行线：在**同一频率**上看，两条线相差 $$\tau$$；在**同一时刻**看，两条线相差 $$S\tau$$。

<figure class="architecture-figure">
  <a href="/images/posts/ti-fmcw-radar-range-estimation/chirp-delay.svg" target="_blank" rel="noopener"><img src="/images/posts/ti-fmcw-radar-range-estimation/chirp-delay.svg" alt="频率–时间图中，发射 chirp 是上升实线，静止目标的回波是向右平移的上升虚线。两线沿时间相差往返延迟 τ，在同一时刻沿频率相差 Sτ。" width="720" height="366" loading="lazy"></a>
  <figcaption>图 1：横着量是往返延迟，竖着量是同一时刻的频率差（示意，未按实际参数比例绘制）。</figcaption>
</figure>

接收机拿发射信号作参考，与回波混频，再滤掉高频成分，留下低频差频（也叫拍频，本文记作 $$f_b$$）。对静止目标，$$\tau$$ 不变；扫频斜率又固定，所以差频也是恒定的。于是，**测距离就变成了测频率**：

$$
f_b=S\tau=\frac{2Sd}{c},\qquad d=\frac{cf_b}{2S}
$$

取 $$c\approx3\times10^8\ \mathrm{m/s}$$、斜率 20 MHz/µs，15 m 目标的往返延迟约为 0.1 µs，对应差频 2 MHz；30 m 目标的延迟加倍，差频就是 4 MHz。也就是说，发射端在毫米波频段扫频，ADC 要采的却是混频后的低频信号。

## 怎样从 FFT 找到多个目标 {#range-fft}

对一个 chirp，接收机采下的差频信号可以包含来自多个目标的频率分量。在同一接收通道内，对这个 chirp 的 ADC 样本沿采样时间做 FFT（快速傅里叶变换），就能看出哪些频率分量较强。这一步叫**距离 FFT**（Range FFT）：原来横轴是差频，按 $$d=cf_b/(2S)$$ 换算后，横轴就是距离。

<figure class="architecture-figure">
  <a href="/images/posts/ti-fmcw-radar-range-estimation/range-fft.svg" target="_blank" rel="noopener"><img src="/images/posts/ti-fmcw-radar-range-estimation/range-fft.svg" alt="距离 FFT 示意图。斜率为 20 MHz/µs 时，2 MHz 和 4 MHz 两个差频峰分别对应 15 m 和 30 m；下方两行刻度对齐显示差频和距离。" width="720" height="350" loading="lazy"></a>
  <figcaption>图 2：两行刻度描述的是同一个频谱。2 MHz 和 4 MHz 的峰分别落在 15 m 和 30 m；峰形和幅度仅作示意。</figcaption>
</figure>

真实频谱不一定是「一个目标一个峰」：相近的回波可能叠在一起，单个目标也可能有多个反射点。两个静止目标的距离完全相同时，单靠距离 FFT 不能把它们分开。运动目标还会给差频带来多普勒分量；本文的距离换算忽略这一项，速度的估计通常还要用到多个 chirp。

## 两个目标离多近还能分开 {#range-resolution}

两个目标相差 $$\Delta d$$，它们的差频就相差 $$\Delta f_b=2S\Delta d/c$$。FFT 只看到了有限长的一段信号，所以频谱里的峰有宽度。对普通距离 FFT，时长为 $$T_{\mathrm{obs}}$$ 的有效观测窗口，能够分辨的频率间隔量级是 $$1/T_{\mathrm{obs}}$$。把这个间隔换算成距离，就得到常用的理论距离分辨率：

$$
\Delta d_{\mathrm{res}}\approx\frac{c}{2ST_{\mathrm{obs}}}=\frac{c}{2B_{\mathrm{eff}}}
$$

**这里的 $$B_{\mathrm{eff}}$$ 是 ADC 有效采样窗口覆盖的射频（RF）扫频带宽**，即 $$ST_{\mathrm{obs}}$$，不一定是整个 chirp 从头到尾扫过的带宽。用于测距的采样窗口要落在发射和目标回波的线性扫频重叠区间内。若采样率为 $$F_s$$，每条 chirp 有 $$N$$ 个有效样本，通常用 $$T_{\mathrm{obs}}=N/F_s$$ 计算。TI 的 [Radar Academy](https://dev.ti.com/tirex/explore/content/radar_academy_2_10_00_2/_build_radar_academy_2_10_00_2/source/introduction/terminology.html) 也区分了总扫频带宽和有效扫频带宽。

例如，20 MHz/µs 的斜率有效采样 50 µs，等于扫过 1 GHz，理论分辨率约为 **15 cm**。同样计算，4 GHz 约为 **3.75 cm**，400 MHz 则约为 **37.5 cm**。这些是理论量级，不是只要目标间隔达到这个数字就一定能分开；窗函数、两路回波的强弱差和噪声都会影响实际结果。

**分辨率、FFT 格点间隔和测距精度是三件事。** 分辨率问的是「两个目标能否分开」，格点间隔问的是「距离谱隔多少米取一个点」，测距精度问的是「单个目标的估计值离真值多远」。例如以 10 MS/s 采 500 点，观测时间正好是 50 µs。用 500 点 FFT 时，频率格点间隔是 20 kHz，换算成距离就是 15 cm。补零到 1000 点后，距离格点间隔变成 7.5 cm，但原始观测还是那 50 µs，并没有因此获得 7.5 cm 的距离分辨率。

## 最远距离由什么限制 {#maximum-range}

由 $$f_b=2Sd/c$$ 可见，目标越远，差频越高。如果差频超出了接收链路可以保留并正确采样的最高 IF（中频）频率 $$f_{\mathrm{IF,max}}$$，就不能再按这条线性关系无歧义地读出目标距离。**由 IF 带宽给出的距离上限**是：

$$
d_{\mathrm{IF,max}}=\frac{c\,f_{\mathrm{IF,max}}}{2S}
$$

这里不能直接拿采样率 $$F_s$$ 代替 $$f_{\mathrm{IF,max}}$$。后者要看设备的 IF 通带、采样模式和滤波。比如对从零频开始的实采样低通链路，理论上限是 $$F_s/2$$，实际还需要滤波余量。TI 的 [SWRA553A](https://www.ti.com/lit/an/swra553a/swra553a.pdf#page=3) 给出的设备示例中，Complex 1x 的采样相关上限为 $$0.9F_s$$，而 Real 和 Complex 2x 为 $$0.9F_s/2$$；实际还不能超过器件支持的 IF 带宽。

举例来说，若某接收链路最高可用差频是 5 MHz，斜率为 20 MHz/µs，则 IF 限制的最远距离约为 **37.5 m**。这不保证 37.5 m 处的目标一定能被检测到：目标反射越弱，或接收端噪声越大，实际能可靠探测的距离就可能越短。

## 同样的带宽，为什么还要选斜率 {#chirp-tradeoff}

比较两个假设都能采满有效窗口的 chirp：

| 对比项 | Chirp A | Chirp B |
| --- | ---: | ---: |
| 扫频斜率 | 20 MHz/µs | 10 MHz/µs |
| 有效采样时间 | 50 µs | 100 µs |
| 有效扫频带宽 | 1 GHz | 1 GHz |
| 理论距离分辨率 | 15 cm | 15 cm |
| 30 m 目标的差频 | 4 MHz | 2 MHz |

为什么 B 观察得更久，距离分辨率却没有提高？因为它虽然能分开间隔只有一半的**频率**，斜率减半后，同样的**距离差**产生的差频间隔也只有一半。两者正好抵消。不过，同一距离在 B 中产生的差频更低，因此 B 用更长的采样时间换来了更宽松的 IF 带宽要求。

选参数时可以把问题倒过来问：**要分开多近的两个目标？** 这决定所需的有效扫频带宽。**在可用的 IF 带宽内，要覆盖多远？** 这限制能用多大的扫频斜率。两者一起决定你至少要留多长的有效采样窗口。

## 参考资料 {#参考资料}

- TI，[*Intro to mmWave Sensing: FMCW Radars — Module 1: Range Estimation*](https://www.ti.com/video/5415203482001)：原英文课程及[课件](https://www.ti.com.cn/content/dam/videos/external-videos/zh-cn/2/3816841626001/5415203482001.mp4/subassets/mmwaveSensing-FMCW-offlineviewing_0.pdf)。
- TI，[*Programming Chirp Parameters in TI Radar Devices*（SWRA553A）](https://www.ti.com/lit/an/swra553a/swra553a.pdf)：第 2.1 节介绍距离上限与分辨率，第 5 节介绍采样和扫频时序。
