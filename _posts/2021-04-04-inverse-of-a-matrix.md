---
layout: post
title: 逆矩阵
enable: true
use_math: true
---

### 定义

### 单位矩阵

### Example A (An invertible matrix).

求矩阵

$$
A =
\begin{bmatrix}
  1 & 0 & 2 \\\\
  -2 & 1 & 3 \\\\
  2 & -3 & 2
\end{bmatrix}
$$

的逆矩阵。

解
将矩阵与单位矩阵排在一起，然后做初等行变换

$$
\left[
    \begin{array}{ccc:ccc}
        1 & 0 & 2 & 1 & 0 & 0 \\\\
        -2 & 1 & 0 & 0 & 1 & 0 \\\\
        2 & -3 & 2 & 0 & 0 & 1
    \end{array}
\right]
$$

通过 $R_{2}=R_{2} + 2R_{1}$ 和 $R_{3}=R_{3}-2R_{1}$ 行变换，可以得到

$$
\left[
    \begin{array}{ccc:ccc}
        1 & 0 & 2 & 1 & 0 & 0 \\\\
        0 & 1 & 4 & 2 & 1 & 0 \\\\
        0 & -3 & -2 & -2 & 0 & 1
    \end{array}
\right]
$$

通过 $R_{3}=R_{3} + 3R_{2}$ 行变换，可以得到

$$
\left[
    \begin{array}{ccc:ccc}
        1 & 0 & 2 & 1 & 0 & 0 \\\\
        0 & 1 & 4 & 2 & 1 & 0 \\\\
        0 & 0 & 10 & 4 & 3 & 1
    \end{array}
\right]
$$

通过 $R_{3}=R_{3}÷10$行变换，可以得到

$$
\left[
    \begin{array}{ccc:ccc}
        1 & 0 & 2 & 1 & 0 & 0 \\\\
        0 & 1 & 4 & 2 & 1 & 0 \\\\
        0 & 0 & 1 & 2/5 & 3/10 & 1/10
    \end{array}
\right]
$$

通过 $R_{1} = R_{1} - 2R_{3}$ 和 $R_{2} = R_{2} - 4R_{3}$ 行变换，可以得到

$$
\left[
    \begin{array}{ccc:ccc}
        1 & 0 & 0 & 1/5 & -3/5 & -1/5 \\\\
        0 & 1 & 0 & 2/5 & -1/5 & -3/5 \\\\
        0 & 0 & 1 & 2/5 & 3/10 & 1/10
    \end{array}
\right]
$$

因此，可以得到矩阵 A 的逆矩阵

$$
A^{-1}=
    \begin{bmatrix}
        1/5 & -3/5 & -1/5 \\\\
        2/5 & -1/5 & -3/5 \\\\
        2/5 & 3/10 & 1/10
    \end{bmatrix}
$$

### 参考资料

1. [Inverse of a Matrix.](https://www.mathsisfun.com/algebra/matrix-inverse.html)
2. [how to find inverse matrix.](https://www.q-math.com/?p=2056)

