# Unity ML-Agents 躲避球 AI 项目



---

## 项目简介

基于 Unity ML-Agents 的强化学习躲避球游戏。Agent 通过强化学习（PPO）自主学习躲避来袭的球体，从 v1 迭代到 v10，最终能够实现高效的侧向闪避和中心位置控制。

## 演示视频

https://www.bilibili.com/video/BV1xcRUBEEHZ/

## 技术栈

| 技术 | 版本 |
|------|------|
| Unity | 2021+ |
| ML-Agents | 2.0.2 |
| Python | 3.10 |
| PyTorch | 2.1.1 |
| NumPy | 1.23.5 |

## 快速开始

### 环境配置

```bash
# 创建 conda 环境
conda create -n mlagents python=3.10
conda activate mlagents

# 安装依赖
pip install torch==2.1.1 numpy==1.23.5 onnx==1.15.0 protobuf==3.20.3
pip install mlagents==1.1.0
```

### 训练模型

``

mlagents-learn config/dodgeball_config.yaml --run-id=dodgeball_v10 --initialize-from=dodgeball_v9 --force
```

然后在 Unity 中点击 Play 按钮连接训练。

### 导出模型

```bash
# 训练完成后
mlagents-learn config/dodgeball_config.yaml --run-id=dodgeball_v10 --resume
# 在 Unity 中点击 Play，然后 Ctrl+C 停止

# 复制模型到 Unity
copy "results\dodgeball_v10\DodgeBallAgent.onnx" "Assets\Models\"
```

## 项目结构

```
ObstacleAvoidance/
├── Assets/
│   ├── Models/                  # 训练好的 ONNX 模型
│   │   └── dodgeball_v10.onnx
│   ├── Scripts/
│   │   ├── DodgeBallAgent.cs    # Agent 主脚本（奖励函数核心）
│   │   ├── BallController.cs   # 球控制脚本
│   │   └── Spawner.cs        # 球生成器
│   └── Scenes/
│       └── SampleScene.unity
├── config/
│   └── dodgeball_config.yaml    # 训练配置文件
├── results/                     # 训练结果
│   └── dodgeball_v10/
└── README.md
```

## 模型演进

| 版本 | 改动 | Mean Reward |
|------|------|-----------|
| v1 | 基础版，存活奖励 |  
| v3 | 危险感知+闪避奖励 |
| v4 | 调优超参数 | 
| v5 | 添加球速度观察，Space Size6 → 14 | 
| v6-v8 |调整奖励函数|
| v9 | 垂直方向奖励+存活强化 | 
| **v10** | 垂直闪避+中心位置奖励| 

## 核心算法

### 奖励函数（v10）

```csharp
public override void OnActionReceived(ActionBuffers actions)
{
    // 1. 存活奖励
    AddReward(0.05f);

    // 2. 速度奖励（保持移动）
    float speed = agentRb.velocity.magnitude;
    if (speed > 0.5f) AddReward(0.01f);

    // 3. 中心位置奖励
    float distToCenter = Vector3.Distance(transform.position, Vector3.zero);
    if (distToCenter < 5f)
        AddReward(0.02f * (5f - distToCenter));

    // 4. 遍历所有球，计算躲避奖励
    foreach (var ball in balls)
    {
        // 垂直方向奖励：向球的两侧闪避
        Vector3 dirToBall = (ball.position - transform.position).normalized;
        float sideMovement = Mathf.Abs(Vector3.Dot(
            Vector3.Cross(dirToBall, Vector3.up).normalized, 
            agentRb.velocity.normalized));
        
        if (sideMovement > 0.3f)
            AddReward(sideMovement * 0.12f);

        // 靠近球惩罚（越近越强）
        if (dist < 3f)
            AddReward(-0.08f * (3f - dist));
    }
}
```

### 观察空间

- 自身位置：2 (x, z)
- 3个球 × 4观察 = 12 (位置x, 位置z, 速度x, 速度z)
- **总计 Space Size = 14**

### 动作空间

- 连续动作：2 (x, z 方向)
- Move Speed: 30

## 展示技能

- ✅ 强化学习 (PPO 算法)
- ✅ Unity 游戏引擎开发
- ✅ Python 机器学习



## 常见问题

**Q: 模型无法导入 Unity？**
A: 确保使用 protobuf==3.20.3 导出 ONNX 模型。

**Q: Agent 不动？**
A: 检查 Behavior Parameters 中 Space Size 是否为 14，Continuous Actions 是否为 2。

**Q: 训练不稳定？**
A: 尝试调整 config/dodgeball_config.yaml 中的 batch_size 和 learning_rate。


