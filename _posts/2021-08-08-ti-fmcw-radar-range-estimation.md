---
layout: post
title: TI FMCW 雷达 - 测距
enable: true
---

Hello, everybody, and welcome to the series of technical videos on Millimeter Wave Sensing. Specifically, we'll be talking about a sensing technology called FMCW RADARS, which is very popular in both automotive and industrial segments. The goal of the series is to give you a short but hopefully fairly in-depth understanding of this class of radars.

大家好，欢迎观看有关毫米波传感的技术视频系列。 具体来说，我们将讨论一项称为 FMCW 雷达的传感技术，该技术在汽车和工业领域非常流行。该系列的目标是让您简单了解这类雷达，同时又为以后深入了解奠定基础。

FMCW stands for Frequency Modulated Continuous Waves. And I'll explain the reason for this naming a little bit later. This radar basically measures the range, velocity, and angle of arrival of objects in front of it. So this series of videos discusses each of these dimensions of sensing in some detail, starting with the range in module one, then moving on to velocity over the next couple of modules, and finally angle estimation in module five. For those of you who are new to millimeter wave sensing, I would recommend that you view all of these videos in sequence.

FMCW 表示调频连续波。我稍后将解释这样命名的原因。该雷达主要测量其前方物体的距离、速度和到达角。因此，该视频系列将会比较详细地讨论其中的每个传感维度，从第一个模块中的距离开始，然后在接下来的几个模块中继续讨论速度，最后在第五个模块中讨论角度估算。如果您不熟悉 毫米波传感，那么我建议您按顺序观看所有这些视频。

The first module is going to start with explaining the basics of FMCW radar operation. And then focus primarily on range estimation using the radar. So the kind of questions that we will focus on in this module are: you have a radar, you have an object in front of it, how does the radar estimate the range to this object. What if there are multiple objects at different ranges from the radar? How close can two objects get and still hope to be resolved as always two objects? What determines the furthest distance that a radar can see?

第一个模块将首先介绍 FMCW 雷达运行的基础知识。然后主要讨论如何使用雷达进行距离估算。在本模块中，我们将重点解答这样的问题：您有一个雷达，并且雷达前方有一个物体，雷达如何估算它与该物体之间的距离？如果有多个物体，并且它们与雷达之间的距离是不同的，将会怎样？两个物体能够相距多近而仍然能够有望总是被解析为两个物体？什么决定雷达可以看到的最远距离？

At the heart of an FMCW radar is a signal called a chirp. What is a chirp? A chirp is a sinusoid or a sine wave whose frequency increases linearly with time. So in this amplitude versus time, or A-t plot, the chirp could start as a sine wave with a frequency of, say, fc. And gradually increase its frequency, ending up with a frequency of say, fc plus B, where B is the bandwidth of the chirp. Thus The chirp is basically a continuous wave whose frequency is linearly modulated. Hence the term frequency modulated continuous wave or FMCW for short.

FMCW 雷达的核心是一种称为线性调频脉冲的信号。什么是线性调频脉冲？线性调频脉冲是频率随时间以线性方式增长的正弦波。那么，在该振幅-时间图中，或者说 A-t 图中，线性调频脉冲可能以频率为 fc 的正弦波开始。然后，其频率逐渐增大，最后，假设以 fc 加 B 的频率结束，其中 B 是线性调频脉冲的带宽。因此，线性调频脉冲本质上是一种频率以线性方式进行调制的连续波。因此，我们使用术语调频连续波，或简称 FMCW。

Now if the same chirp were represented in a frequency versus time plot, or f-t plot, how would it look? Remember that the frequency of the chirp increases linearly with time, linear being the operative word. So in the f-t plot, the chip would be a straight line with a certain slope S.

现在，如果在频率-时间图或者说 f-t 图中显示同一线性调频脉冲，它看起来会是什么样子的？请记住，线性调频脉冲的频率随时间以线性方式增大，其中线性是关键词。因此，在 f-t 图中，线性调频脉冲会是一条具有特定斜率 S 的直线。

And just to put in some typical numbers, this figure for example could represent a chirp which starts at a frequency fc of 77 gigahertz, spans a bandwidth B of 4 gigahertz, thus ending up at a frequency of 81 gigahertz. The slope S of the chirp defines the rate at which the chirp ramps up. In this example, the chirp is sweeping a bandwidth of 4 gigahertz, with a time period Tc of 40 microseconds, which corresponds to a slope of 100 megahertz per microsecond. As we shall see later, the bandwidth B and the slope S are important parameters which define system performance.

只需放入一些典型的数字，该图就可以表示以 77GHz 的频率 fc 开始的线性调频脉冲，跨越 4GHz 的带宽 B，最终以 81GHz 的频率结束。线性调频脉冲的斜率 S 定义线性调频脉冲上升的速率。在本示例中，线性调频脉冲跨越 4GHz 的带宽，具有 40 微秒的 Tc 时长，这对应于每微秒 100MHz 的斜率。正如我们稍后将看到的，带宽 B 和斜率 S 是用于定义系统性能的重要参数。 

Now that we know what a chirp is, we are ready to understand how an FMCW radar works. This here is a simplified block diagram of an FMCW radar with a single TX and a single RX antenna. The radar operates as follows. The synthesizer generates a chirp. This chirp is transmitted by the TX antenna. The chirp is reflected off an object. And the reflected chirp is received at the RX antenna. The RX signal and the TX signal are mixed. And the resulting signal is called an IF signal-- IF standing for Intermediate Frequency. We'll analyze the IF signal in more detail in the next slide.

现在我们已经知道什么是线性调频脉冲，那么我们可以了解 FMCW 雷达的工作原理了。这是一个简化的 FMCW 雷达框图，该雷达具有单个 TX 天线和单个 RX 天线。该雷达的工作原理如下。合成器生成一个线性调频脉冲。TX 天线将该线性调频脉冲发射出去。当遇到物体时，该线性调频脉冲会反射回来。RX 天线接收反射的线性调频脉冲。RX 信号和 TX 信号混合在一起。最终生成的信号 称为 IF 信号 -- IF 表示中频。我们将在下一张幻灯片中对 IF 信号进行更详细的分析。

But first, let's spend a little time understanding this component called a mixer. What is a mixer? A mixer has two inputs and one output. A simple way to understand the mixer is as follows. If two sinusoids are input to the two input ports of the mixer, the output of the mixer is a sinusoid with the following two properties.

但是，首先让我们花点儿时间来了解这个称为混频器的组件。什么是混频器？混频器具有两个输入和一个输出。下面是了解混频器的一种简单方法。如果向混频器的两个输入端口输入两个正弦波，那么混频器的输出是具有以下两条性质的正弦波。

Property number 1: the instantaneous frequency of the output equals the difference of the instantaneous frequencies of the two input sinusoids. So even if these sinusoids, if their frequencies were varying with time, the frequency of the output at any point in time would be equal to the difference of the input frequencies at that point in time.

性质 1：输出的瞬时频率等于两个输入正弦波的瞬时频率的差值。因此，即使这些正弦波的频率随时间发生 变化，任一时刻输出频率也将等于该时刻的输入频率差值。

Property number 2: the starting phase of the output sinusoid is equal to the difference of the starting phases of the two input sinusoids. These two properties are illustrated in these equations here, where x1 and x2 are the two inputs, and x_out is the output of the mixer. So note here that the two inputs have frequencies of omega 1 and omega 2, and starting phases of phi 1 and phi 2, respectively. And the output has a frequency of omega 1 minus omega 2, and a starting phase of phi 1 minus phi 2.

性质 2：输出 正弦波的起始相位等于两个输入正弦波的起始相位差值。这里的方程阐释了这两条性质，其中 x1 和 x2 是两个输入，x_out 是混频器的输出。那么，在这里请注意，两个输入分别具有频率 ω1 和 ω2 以及起始 相位 φ1 和 φ2。输出具有频率 ω1 减 ω2，并具有起始相位 φ1 减 φ2。 

Let's look more closely at the operation of the mixer in the radar. And I think it's best illustrated using the f-t plot that we talked about earlier. So the plot here refers to the RF signal. So you have the transmitter chirp here and the received chirp here. Note that the received chirp is a time delay replica of the TX chirp. And for now, I'm assuming there is only one object in front of the radar. Hence, only one RX chirp.

让我们更加详细地看看雷达中混频器的工作原理。我想使用我们先前讨论过的 f-t 图可以对其进行最佳阐释。那么，这里的图表示射频信号。那么，这里是发射器线性调频脉冲，这里是接收到的线性调频脉冲。请注意，接收到的线性调频脉冲是 TX 线性调频脉冲的延时副本。 现在，我假设雷达前方只有一个物体。因此，只有一个 RX 线性调频脉冲。 

Remember from the last slide that the output frequency of the mixer is the difference of the instantaneous frequencies of its two inputs, namely the TX chirp and the RX chirp. So to generate the f-t plot for the IF signal, I just need to subtract this line from this. And as you can see, these two lines are at a fixed distance from each other. And that fixed distance is given by the slope of the chirp times the round trip delay. In other words, S-tau. So a single object in front of the radar produces an IF signal consisting of a single frequency given by S-tau.

回忆一下上一张幻灯片的内容，混频器的输出频率是其两个输入，即 TX 线性调频 脉冲和 RX 线性调频脉冲的瞬时频率的差值。那么，为了生成 IF 信号的 f-t 图，我需要将这条线从这条线上减去。正如您看到的，这两条线相互之间存在固定的距离。该固定的距离由线性调频脉冲的斜率乘以往返延迟给出。换句话说，S-τ。因此，雷达前方的单个物体可生成一个包含单个频率的 IF 信号，该频率由 S-τ 给出。 

Now tau, the round trip delay from the radar to the object and back, can also be expressed as twice the distance to the object divided by the speed of light. So this is the fundamental concept to remember. A single object in front of the radar produces an IF signal with a constant frequency given by S2d/c.

现在，τ，即从雷达 到物体然后又返回的往返延迟，也可以表示为与物体的距离除以光速，然后乘以 2。那么，这是需要记住的基本概念。雷达前方的单个物体可生成具有恒定频率的 IF 信号，该频率由 S2d/c 给出。 

Now, it is important to note that the IF signal is only valid from the time the reflected signal is received at the RX antenna. So if you were to digitize this IF signal using an ADC, you need to make sure that you only pick up samples after this time tau has elapsed, and only up to the time where the TX signal is present.

现在，应注意，IF 信号仅从在 RX 天线上接收到反射信号开始有效，这一点很重要。因此，如果您要使用 ADC 对该 IF 信号进行数字化，那么您需要确保仅在经过该时间 τ 之后再接收样本，并且只能持续到 TX 信号消失之前。 

Another point worth noting is that the round trip delay tau is usually a very small fraction of the total chirp time. So for example, for a radar with a maximum distance of 300 meters and a chirp time of 40 microseconds, this ratio of tau by Tc is only 5%. Fourier transforms are at the heart of FMCW radar signal processing. And we will see throughout the series of videos, they are used in range, velocity, and angle estimation. So we will from time to time take short detours to remind ourselves of relevant properties of Fourier transforms.

另外一点值得注意的是，往返延迟 τ 通常是总线性调频脉冲时间的很小一部分。例如，对于最大距离为 300 米并且线性调频脉冲时间为 40 微秒的雷达，该 τ 与 Tc 的比率仅为 5%。傅里叶变换是 FMCW 雷达信号处理的核心。我们将在整个视频系列中看到，它们用于距离、速度和角度估算。因此，我们将不时地稍微转移一下话题，回忆一下傅里叶变换的相关性质。 

A Fourier transform converts a time domain signal into a frequency domain. So a single tone in the time domain produces a single tone in the frequency domain. Likewise, the two tones in the time domain should result in two peaks in the frequency domain. But is that always the case? So in this example here, within the observation window of T, the red tone completes two cycles while the blue tone completes 2.5 cycles. And this difference of 0.5 cycles between the red and the blue tone, it seems is not sufficient to resolve the two tones in the frequency spectrum. So here you have only a single tone corresponding to the contributions from both these signals.

傅里叶变换将时域信号转换到频域中。因此时域中的单个音调会在频域中产生单个音调。类似地，时域中的两个音调应在频域中产生两个峰值。但情况是否总是如此呢？那么，在这个示例中，在观测窗口 T 内，红色音调完成了两个周期，而蓝色音调完成了 2.5 个周期。红色音调和蓝色音调之间的该 0.5 个周期差值似乎不足以解析频谱中的两个音调。那么，在这里，您只有单个与这两个信号的贡献对应的音调。

Let's now double the observation window from T to 2T. Doubling the observation window, now results in a difference of one cycle between the red and the blue tones. And as you can see, the two tones are now resolved in the frequency spectrum. So the take away is that longer the observation period, better the resolution. And in general, an observation window of T can separate frequency components that are separated by more than 1 by T hertz. This completes our short digression on Fourier transforms.

现在，让我们将观测窗口加倍，从 T 增加到 2T。现在，将观测窗口加倍，会导致红色 音调和蓝色音调之间的差值为一个周期。正如您看到的，现在在频谱中解析了这两个音调。所以，重点是，观测期越长，解析就越好。一般而言，观测窗口 T 可以分隔以高于 T 分之一赫兹进行分隔的频率分量。有关傅里叶变换的简短题外话就谈到这里。

So far, we've talked about a single object in front of the radar. It's easy to extend this to the case where there are multiple objects in front of the radar. So here, you have a radar transmitting a single chirp, and you get multiple reflected chirps from different objects. Each delayed by a different amount depending on the distance to that object. So the IF signal will have tones corresponding to each of these reflections. And the frequency of these tones, as we learnt, is directly proportional to the range. So this has the smallest frequency and corresponds to the closest object. While this corresponds to the farthest.

到目前为止，我们已经讨论了雷达前方的单个物体。很容易将它扩展到雷达前方有多个物体的情况。那么，这里有一个雷达，它正在发射单个线性调频脉冲，您获取了多个从不同物体反射的线性调频脉冲。 每个脉冲具有不同量的延迟，具体取决于与物体之间的距离。因此，IF 信号将具有与其中每个反射相对应的音调。正如我们 所了解到的，这些音调的频率与距离成正比。因此，这条具有最小的频率并对应于最近的物体。而这条对应于最远的物体。 

A Fourier transform on this IF signal well then show up multiple peaks. And the frequency of these peaks will be directly proportional to the range of the corresponding object. So again, this corresponds to the closest object, and this one to the farthest. The moment we talk about multiple objects, the next natural question is range resolution. That is, how close can two of these objects be and still be resolved as two peaks in the IF spectrum.

有关该 IF 信号的傅里叶变换会显示多个峰值。这些峰值的频率将与对应物体的距离成正比。那么，这还是对应于最近的物体，这对应于最远的物体。由于我们现在是在讨论多个物体，因此下一个问题自然是距离分辨率。也就是说，其中的两个物体能够相距多近而仍然能够在 IF 频谱中解析为两个峰值？

So in this example, we have two reflected chirps from two objects. And the corresponding A-t plot of the IF signal shows two sine waves. But the frequencies of these sound waves are so close that they show up as a single peak in the frequency spectrum. How do we improve the range resolution of this radar?

那么，在该示例中，我们有两个从两个物体反射的线性调频脉冲。IF 信号的对应 A-t 图显示了两个正弦波。但这些声波的频率是如此接近，以至于 它们在频谱中 显示为单个峰值。我们如何提高 该雷达的距离分辨率？ 

Taking a cue from our recap of Fourier transforms, one option is to extend the observation window of these two sine waves by increasing the length of the IF signal. So that's what I've done here. So the chirp is extended which then extends the duration of the IF signal. And this resolves the two peaks in the frequency domain. Note that increasing the duration of the IF signal proportionally increases the bandwidth of the chirp. So that gives us a clue that possibly a larger bandwidth corresponds to a better range resolution.

可以从我们对傅里叶变换的扼要重述中获得提示，一种选项是 通过增大 IF 信号的长度来扩展这两个正弦波的观测窗口。那么，我在这里就是这么做的。那么，线性调频脉冲得到扩展，从而扩展了 IF 信号的持续时间。这在频域中解析了两个峰值。请注意，增加 IF 信号的持续时间能够成正比增加线性调频脉冲的带宽。这就提示我们，更大的带宽可能对应更好的距离分辨率。 

Now that we have some intuition on how to improve the range resolution of radar, it would be nice to go a step further and actually derive an expression for this range resolution. And as it turns out, it's not that difficult either. All you need to know are these two pieces of information, something that we've already learned before. So at this point, I would really like to encourage you to pause here and try and derive this expression for the range resolution.

现在，我们对如何提高雷达的距离分辨率有一些直观的认识，我们最好更进一步，实际推导该距离分辨率表达式。事实证明，这其实并没有那么困难。您需要知道的 就是这两条信息，我们以前已了解过。那么，此时，我非常想鼓励您在这里暂停，尝试推导该距离分辨率表达式。 

So two objects, which are delta d apart in distance, will have the IF frequencies separated by delta f, given by this expression. For these two frequencies to show up as distinct peaks in the IF frequency spectrum, this frequency separation delta f must be greater than 1 by the duration of the IF signal, which is virtually equal to the duration of the chirp TcC if you discount the small fraction in the beginning, the portion tau arising from the round trip delay.

那么，两个间距为 Δd 的物体，其 IF 频率间隔为 Δf，由该表达式 给出。为了使这两个频率在 IF 频谱中显示为不同的峰值，该频率间隔 Δf 必须大于 1 除以 IF 信号的持续时间，如果您忽略开始的一小部分，即往返延迟产生的 τ 部分，那么 这基本上等于线性调频脉冲的持续时间 Tc。 

So substituting for using this expression, you know we get this inequality here, which after some rearrangement becomes this. And note that the slope times the duration of the chirp is actually the bandwidth of the chirp. So this expression can be further simplified, and you finally get this expression here, which says that two objects can be separated in the IF frequency spectrum as long as the distance (separation) between them is greater than the ratio of the speed of light to twice the bandwidth of the chirp. So the takeaway here is that the range resolution depends only on the bandwidth swept by the chirp, and is given by this expression over here-- speed of light divided by twice the bandwidth.

那么，替换该表达式，您知道我们会 得到这里的不等式，在进行一些重新整理之后变成这样。请注意，斜率乘以线性调频脉冲的持续时间实际上是线性调频脉冲的带宽。因此，可以进一步简化该表达式，您最终得到这里的表达式，它表示，只要 两个物体之间的距离（间隔）大于光速与线性调频脉冲带宽的两倍之比，就可以在 IF 频谱中分离它们。那么，这里的重点是，距离分辨率仅取决于线性调频脉冲覆盖的带宽，由这里的表达式给出 -- 光速 除以带宽的两倍。 

Time for a question now. So you have two chirps here-- Chirp A and Chirp B. Chirp A has twice the duration of Chirp B. But both of them have the same bandwidth. Which of these two chirps gives you a better range resolution? So if you think about it, both of these chirps have the same bandwidth, B. So from the formula that we just derived, c by 2B, both of them should have the same range resolution.

现在有个问题。那么，这里有两个线性调频脉冲 -- 线性调频脉冲 A 和 B。A 的持续时间是 B 的两倍。但它们具有相同的带宽。这两个线性调频脉冲中的哪一个 可以提供更好的距离分辨率？那么，如果您对其进行考虑，这两个线性调频脉冲具有相同的带宽 B。因此，根据我们刚刚推导的公式 c 除以 2B，它们应具有相同的距离分辨率。 

But then, Chirp A has a longer duration and hence a longer observation window of the IF signal. So intuitively, if you take into consideration the properties of Fourier transforms, this chirp, Chirp A, should have a better resolution than just Chirp B. How do we resolve this contradiction? Something for you to think about.

但是，线性调频脉冲 A 具有更长的持续时间，因此具有更长的 IF 信号观测窗口。因此，凭直觉，如果您考虑傅里叶变换的性质，线性调频脉冲 A 的分辨率应好于线性调频脉冲 B。我们如何解决该矛盾？请考虑一下 这个问题。

So we've talked about the IF signal, and that the frequency of tones in the IF signal is directly proportional to the range of objects. In most radars, what happens is that the IF signal is digitized for subsequent processing. So it's first low pass filtered, and then digitized by an ADC, and sent to a suitable processor such as a DSP.

那么，我们已经讨论了 IF 信号，IF 信号中音调的频率与物体的距离成正比。在大多数雷达中，发生的情况是，会对 IF 信号进行数字化，以供后续处理。那么，它首先会进行低通滤波，然后由 ADC 进行数字化，接着被发送到合适的处理器，如 DSP。 

The DSP could begin by doing a Fourier transform to estimate the range of objects, and subsequently do other kinds of processing to estimate the velocity and angle of arrival of these objects. And this is something we'll get to in subsequent modules. When ever we are digitizing a signal. we need to know what is the bandwidth of interest so that the low pass filter and the ADC sampling rate can be appropriately set.

DSP 可能首先执行傅里叶变换，以估算物体的距离，随后执行其他种类的处理，以估算这些物体的速度和到达角。这是我们将在后续模块中讨论的内容。每当我们要对信号进行数字化时，我们就需要知道目标带宽，以便可以适当地设置低通滤波器和 ADC 采样率。 

So let's say we are interested in objects from zero to a maximum distance of, say, dmax. The maximum IF signal. The maximum frequency of the IF signal is then going to be S2dmax/c. And correspondingly, the bandwidth of interest is going to be from zero to this maximum IF frequency which means that the low pass filter should have a cut-off frequency, which is beyond this IF max. And also the ADC should have a sampling rate which is greater than the same value.

那么，假设我们对零到最大距离 dmax 之间的物体感兴趣。最大 IF 信号。IF 信号的最大频率将为 S2dmax/c。相应地，目标带宽将从零到该最大 IF 频率，这意味着低通滤波器的截止频率应高于该 IF_max。此外，ADC 应具有高于该同一值的采样率。 

So you can see here that the maximum sampling rate of the ADC can limit the maximum distance that the radar can see. Note that the maximum IF bandwidth depends on the product of the slope and the maximum distance. So if the ADC sampling rate and hence the IF bandwidth is a bottleneck in the sensor, you can always trade off the slope and the maximum distance. And typically, radar's tend to use smaller slopes for larger dmax.

因此，您可以在这里看到，ADC 的最大采样率可能会限制雷达可以看到的最大距离。请注意，最大 IF 带宽取决于斜率与最大距离的乘积。因此，如果 ADC 采样率和 IF 带宽是传感器的瓶颈，那么您始终可以对斜率和最大距离进行折衷。通常，雷达倾向于针对较大的 dmax 使用较小的斜率。 

Time for another question. Re-visiting our earlier example, what more can we say about these two chirps? Chirp A and B have the same bandwidth. But Chirp A takes twice as long as Chirp B. A good time pause the video, and try to answer this question.

现在有另一个问题。重新查看我们先前的示例，对于这两个线性调频脉冲，我们还可以讨论什么？线性调频脉冲 A 和 B 具有相同的带宽。但线性调频脉冲 A 的长度是 B 的两倍。现在您应该暂停一下视频，尝试回答该问题。 

Since both A and B have the same bandwidth, they of course, have the same range resolution. But note that Chirp A has half the slope of Chirp B. So for the same maximum range requirement, or for the same dmax, Chirp A would require only half the IF bandwidth, which translates to an ADC with a smaller sampling rate. So while Chirp A has the advantage of a more relaxed ADC requirement, Chirp B of course has the advantage of requiring only half the measurement time. So that is the trade going on here.

由于 A 和 B 具有相同的带宽，因此它们当然具有相同的距离分辨率。但是，请注意，线性调频脉冲 A 的斜率是 B 的一半。因此，对于相同的最大距离要求， 或对于相同的 dmax，线性调频脉冲 A 仅需要一半的 IF 带宽，这意味着 ADC 具有较小的采样率。因此，线性调频脉冲 A 具有 ADC 要求更宽松的优势，而线性调频脉冲 B 当然也具有仅需要一半测量时间的优势。那么，这就是需要进行折衷的地方。

So this slide summarizes all that we have discussed so far. This is a block diagram of an FMCW radar with a single transmit and a single receive antenna. Let's go with the sequence of events involved in estimating the range of an object. So first, the synthesizer or synth generates a chirp. This chirp is transmitted over the TX antenna. It is reflected off multiple objects in front of the radar. And the receiver sees delayed versions of this chirp. The received signal and the transmitted signal are mixed to create an IF signal.

 那么，该幻灯片总结了到目前为止我们已经讨论的所有内容。这是具有单个发射天线和单个接收天线的 FMCW 雷达的框图。让我们来查看估算物体的距离所涉及的 一系列事件。那么，首先，合成器 生成一个线性调频脉冲。该线性调频脉冲通过 TX 天线进行发射。到达雷达前方的多个物体后反射回来。接收器看到该线性调频脉冲的延迟版本。接收到的信号和发射的信号进行混合，从而产生 IF 信号。 

This IF signal consists of multiple tones and the frequency of each of these tones is proportional to the range of the corresponding object. The IF signal is then low pass filtered and digitized. And note that the sampling rate of the ADC must be commensurate with the maximum distance that we wish to see.

该 IF 信号包含多个音调，其中每个音调的频率与对应物体的距离成正比。然后，IF 信号进行低通滤波并数字化。请注意，ADC 的采样率必须与我们希望看到的最大 距离相称。 

The digitized data is then processed. An FFT is performed on this data. And location of the peaks in the frequency spectrum directly correspond to the range of objects. Note that here I've plotted the FFT with range on the x-axis rather than the IF frequency, which is OK. Because as we've learned, the IF frequency is directly proportional to the range. This FFT is called range FFT because it resolve objects in range. And this term range FFT is something that you will see a lot in FMCW literature.

然后对数字化数据进行处理。对该数据 执行 FFT。频谱中峰值的位置直接对应于物体的距离。请注意，在这里，我绘制 FFT 时 x 轴上显示的是距离，而不是 IF 频率，这是可以的。因为正如我们所了解到的，IF 频率与距离 成正比。该 FFT 称为距离 FFT，因为它在距离方面对物体进行解析。对于距离 FFT 这一术语，您将在 FMCW 文献中经常看到它。

This slide over here summarizes some of the key concepts and formulas that we see in this module. First, an object at a distance of D produces an IF frequency of S2d/c. Range resolution depends only on the bandwidth spanned by the chirp and is given by the speed of light divided by twice the bandwidth. The ADC sampling rate Fs, limits the maximum range dmax that the radar can see.

这里的幻灯片总结了我们在该模块中看到的部分关键概念和公式。首先，距离为 d 的物体会生成 IF 频率 S2d/c。距离分辨率仅取决于线性调频脉冲跨越的带宽， 由光速除以带宽的两倍给出。ADC 采样率 Fs 限制雷达可以看见的最大距离 dmax。

The other thing: when we talk about bandwidth and FMCW radars, there are usually two bandwidths that are important. The RF bandwidth and the IF bandwidth. And it's important to clearly distinguish between both of these. So the RF bandwidth is the bandwidth spanned by the chirp. A larger RF bandwidth directly translates to a better range resolution. RF bandwidths are typically in the range of a few hundred of megahertz to several gigahertz. An RF bandwidth of 4 gigahertz, for example, translates to a range resolution of 4 centimeters. An RF bandwidth of 400 megahertz translates to a range of resolution of about 30 centimeters.

其他要点：当我们讨论带宽和 FMCW 雷达时，通常有两种重要的带宽。射频带宽和 IF 带宽。应清晰地区分这两者，这一点很重要。那么，射频带宽是线性调频脉冲跨越的带宽。较大的射频带宽可直接转换为较好的距离分辨率。射频带宽的范围通常为几百 MHz 至几 GHz。例如，4GHz 的射频带宽可转换为 4 厘米的距离分辨率。400MHz 的射频带宽可转换为大约 30 厘米的距离分辨率。 

The other bandwidth is the IF bandwidth. A larger IF bandwidth primarily enables the radar to see a larger maximum distance. Also enables faster chirps. By faster chirps, I mean chirps with higher slopes. The IF bandwidth of typical radars is in the low megahertz region. So that's one of the things about FMCW radars, that you can have an RF signal spanning a large bandwidth of, say, 4 gigahertz, but yet your ADC would only need to sample a signal of a few megahertz.

另一种带宽 是 IF 带宽。较大的 IF 带宽主要可以使雷达看到较大的最大距离。还可以实现较快的线性调频脉冲。我说较快的线性调频脉冲，是指具有较高斜率的线性调频脉冲。典型雷达的 IF 带宽处于低 MHz 范围内。那么，这是有关 FMCW 雷达的要点之一，您可以具有跨越较大带宽的射频信号，比如 4GHz，但您的 ADC 仅需要对几 MHz 的信号进行采样。

This concludes the first module in the series. We focused primarily on range estimation using the FMCW radar. Here's a question to motivate the subsequent modules. So there are two objects equidistant from the radar. How will the range FFT look like? Now since these objects are equidistant, the range FFT will have a single peak corresponding to this range d and incorporating the effects of both these objects. So how then do we separate out these two objects. It turns out that if these two objects have different velocities relating to the radar, then they can be separated out by further signal processing. And to understand that, we need to really look at the phase of the IF signal which is something we will be doing in the next module.

该系列的第一个 模块到此结束。我们主要讨论了如何使用 FMCW 雷达进行距离估算。这里有一个问题，用以引出后续模块。有两个与雷达的距离相同的物体。距离 FFT 看起来会是什么样的？现在，由于这些物体的距离是相同的，因此距离 FFT 将具有与该距离 d 相对应并受到这两个物体影响的单个峰值。那么，我们如何分离这两个物体呢？事实证明，如果这两个物体具有不同的相对于雷达的速度，那么可以通过进一步的信号处理分离它们。要理解这一点，我们需要实际看看 IF 信号的相位，这是我们将在下一个模块中做的事情。
