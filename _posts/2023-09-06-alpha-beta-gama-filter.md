---
layout: post
title: α-β-(γ) 滤波
use_math: true
---

### α 滤波参数选择

$\lambda = {\delta_w \over \delta_v} \cdot T^2$

$\alpha = {-\lambda^2 + \sqrt{\lambda^4 + 16\lambda^2} \over 8}$

### α-β 滤波参数选择

$\lambda = {\delta_w \over \delta_v} \cdot T^2$

$\gamma = {4 + \lambda - \sqrt{8\lambda + \lambda^2} \over 4}$

$\alpha = 1 - \lambda^2$

$\beta = 2 \cdot (2 - \alpha) - 4 \cdot \sqrt{1 - \alpha}$

## α-β-γ 滤波参数选择

$\lambda = {\delta_w \over \delta_v} \cdot T^2$

$b = {\lambda \over 2} - 3$

$c = {\lambda \over 2} + 3$

$d = -1$

$p = c - {b^2 \over 3}$

$q = {2b^3 \over 27} - {bc \over 3} + d$

$v = \sqrt{q^2 + {4p^3 \over 27}}$

$z = -\sqrt[3]{q + {v \over 2}}$

$s = z - {p \over 3z} - {b \over 3}$

$\alpha = 1 - s^2$

$\beta = 2 \cdot (1 - s)^2$

$\gamma = {\beta^2 \over 2\alpha}$