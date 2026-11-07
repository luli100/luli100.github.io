"""一维卡尔曼滤波：验证文章中的结论。

实验一：融合后的方差是否等于 (1-K)^2 P + K^2 R，最优 K 是否为 P/(P+R)。
实验二：雷达跟踪前车距离，对比 Q=0 与 Q=0.01。
    帧周期 50 ms，雷达测距标准差 0.3 m。
    速度 v 是距离的变化率，负值表示靠近。
    滤波器的运动模型假设前车以 v = -10 m/s 匀速运动。
    前 100 帧确实匀速；第 100 帧起前车刹车，
    v 每秒减小 2 m/s（靠近得越来越快），模型对此一无所知。

只用标准库，运行：python kalman_1d.py
"""
import random
import sys

sys.stdout.reconfigure(encoding="utf-8")

DT = 0.05          # 帧周期 (s)
V_MODEL = -10.0    # 模型假设的速度 (m/s)，负值表示靠近
R = 0.3 ** 2       # 雷达测距方差：标准差 0.3 m
N = 200
BRAKE_FRAME = 100
BRAKE_ACC = -2.0   # 刹车后的相对加速度 (m/s^2)


def rms(e):
    return (sum(v * v for v in e) / len(e)) ** 0.5


def fusion_variance():
    """用大量随机误差直接统计融合后的方差，与公式对比。"""
    P, R_ = 1.0, 4.0
    n = 200_000
    e_pred = [random.gauss(0, P ** 0.5) for _ in range(n)]
    e_meas = [random.gauss(0, R_ ** 0.5) for _ in range(n)]
    print(f"== 实验一：融合后的方差（P={P}, R={R_}）==")
    print(f"{'K':>5} | {'统计值':>7} | {'(1-K)^2 P + K^2 R':>17}")
    for K in (0.0, 0.1, 0.2, 0.3, 0.5, 1.0):
        fused = [(1 - K) * a + K * b for a, b in zip(e_pred, e_meas)]
        formula = (1 - K) ** 2 * P + K ** 2 * R_
        print(f"{K:>5.1f} | {rms(fused) ** 2:>7.3f} | {formula:>17.3f}")
    print(f"卡尔曼增益 P/(P+R) = {P / (P + R_):.1f}，"
          f"对应方差 PR/(P+R) = {P * R_ / (P + R_):.3f}")


def truth():
    x, v = 150.0, V_MODEL
    xs = []
    for k in range(N):
        if k >= BRAKE_FRAME:
            v += BRAKE_ACC * DT
        x += v * DT
        xs.append(x)
    return xs


def run(Q, xs, zs):
    x_hat, P = zs[0], R      # 用第一帧测量初始化
    Ks, errs = [None], [x_hat - xs[0]]
    for k in range(1, N):
        # ① 预测
        x_pred = x_hat + V_MODEL * DT
        P_pred = P + Q
        # ② 更新
        K = P_pred / (P_pred + R)
        x_hat = x_pred + K * (zs[k] - x_pred)
        P = (1 - K) * P_pred
        Ks.append(K)
        errs.append(x_hat - xs[k])
    return Ks, errs


def tracking():
    xs = truth()
    zs = [x + random.gauss(0, R ** 0.5) for x in xs]
    results = {Q: run(Q, xs, zs) for Q in (0.0, 0.01)}

    print("== 实验二（1）：K 随帧数的变化 ==")
    print(f"{'帧':>5} | {'K (Q=0)':>9} | {'K (Q=0.01)':>10}")
    for k in (1, 2, 5, 10, 20, 50, 99, 150, 199):
        print(f"{k:>5} | {results[0.0][0][k]:>9.4f} | {results[0.01][0][k]:>10.4f}")

    print()
    print("== 实验二（2）：估计误差（估计 - 真值, m），第 100 帧开始刹车 ==")
    print(f"{'帧':>5} | {'雷达误差':>8} | {'Q=0':>8} | {'Q=0.01':>8}")
    for k in (50, 90, 100, 110, 120, 140, 160, 180, 199):
        print(f"{k:>5} | {zs[k]-xs[k]:>8.3f} | {results[0.0][1][k]:>8.3f} | {results[0.01][1][k]:>8.3f}")

    print()
    print("== 实验二（3）：均方根误差 (m) ==")
    print(f"{'区间':<14} | {'雷达':>6} | {'Q=0':>6} | {'Q=0.01':>6}")
    for name, a, b in (("匀速段 20-99", 20, 100), ("刹车段 100-199", 100, 200)):
        radar = rms([zs[k] - xs[k] for k in range(a, b)])
        print(f"{name:<14} | {radar:>6.3f} | {rms(results[0.0][1][a:b]):>6.3f} | {rms(results[0.01][1][a:b]):>6.3f}")


if __name__ == "__main__":
    random.seed(1)
    fusion_variance()
    print()
    random.seed(1)
    tracking()
