# Dash 扩展与体力系统规划

> 适用项目：UOP1 / Chop Chop  
> Unity 版本：`2022.3.62f3c1`  
> 规划范围：Dash 冷却、速度曲线、动画/音效/VFX/镜头反馈，以及完整的角色体力系统  
> 暂不实现：空中 Dash、Dash 无敌帧  
> 文档性质：这是基于当前项目结构制定的实施计划，不代表这些功能已经写入项目

---

## 1. 这次改动想解决什么

基础 Dash 已经解决了“按键后进入 Dash State，并通过 `CharacterController` 产生位移”的问题。下一阶段不应该只是继续往 `DashMovementActionSO` 里堆字段，而是要把它扩展成一个规则清楚、表现完整、以后能继续调整的角色能力。

本轮规划包含四组目标：

1. Dash 有冷却，不能无限连续触发。
2. Dash 速度随时间变化，不再是全程恒速。
3. Dash 有动画、声音、粒子、镜头和可选残影，并且保持项目现有风格。
4. 角色拥有正式的体力系统，Dash、跳跃和劈砍都需要消耗体力，体力能够延迟恢复、持续回复、限制动作，并通过 HUD 给玩家反馈。

这几组功能彼此有关，但不应写成一个巨大的 `DashAction`：

```text
输入请求
  ↓
能否执行：状态规则 + 冷却 + 体力
  ↓
成功提交动作：消耗输入、体力，启动冷却
  ↓
玩法执行：方向、速度曲线、碰撞、持续时间
  ↓
表现反馈：动画、音效、粒子、镜头、残影
  ↓
结束：回到 Idle / Walking，清理临时表现
```

玩法是否成功不能由动画或粒子决定；表现层也不应该修改体力和冷却。

---

## 2. 当前项目已经具备的扩展点

这份规划不是从空项目出发。当前工程已经提供了很多可以直接复用的结构。

### 2.1 状态机

当前角色使用 ScriptableObject 状态机：

- `StateSO` 保存一个状态包含的 Action 列表。
- `TransitionTableSO` 保存状态之间的 Condition。
- `StateAction` 有 `OnStateEnter`、`OnUpdate`、`OnStateExit`。
- 同一个 Action SO 被多个 State 引用时，一台状态机会复用同一个运行时 Action 实例。

最后一点对体力系统很重要：一个无状态的通用 Action 可以安全复用；如果 Action 内记录“上一次攻击请求编号”，也必须明确知道它会在多个状态之间共享。

### 2.2 Dash 当前结构

当前 `Dash` State 的 Action 顺序是：

```text
DashMovement
GroundGravity
ApplyMovementVector
```

这是正确的基础顺序：先计算水平速度，再加入贴地重力，最后由 `ApplyMovementVector` 统一乘 `Time.deltaTime` 并调用 `CharacterController.Move()`。

在继续扩展前，需要先保证基础 Dash 已满足：

- `Dash -> Idle` 使用 `Timer_Dash Is True AND IsMoving Is False`。
- `Dash -> Walking` 使用 `Timer_Dash Is True AND IsMoving Is True`。
- 两条退出转换中不再使用 `HasDashInput Is False` 作为持续时间条件。
- Console 中没有由 Dash 新增的异常。
- 临时的 `Debug.Log("Dash cached...")` 在完成输入验证后删除。

### 2.3 已有表现系统

项目中已经存在：

- `AnimatorParameterActionSO`：在进入、退出或更新状态时设置 Animator 参数。
- `PlayAudioCueActionSO`：通过音频事件通道播放 `AudioCueSO`。
- `ShakeCamActionSO`：广播镜头震动事件。
- `PlayerEffectController`：控制走路、起跳、落地和攻击粒子。
- `AudioCueSO`：支持多组声音、随机播放和避免立即重复。
- Cinemachine Impulse：`CameraManager` 已监听 Camera Shake 事件。
- URP、普通 Particle System、现成的尘土粒子和 Toon 风格斩击贴图。

因此动画参数、音效接线、粒子和镜头反馈都适合继续作为独立 StateAction 放进 Dash State，而不是写进移动 Action。

### 2.4 当前缺少的资源

检查现有 PigChef 动画后，没有发现专门的 Dash / Dodge / Roll 动画片段，Animator 参数列表中也没有 `IsDashing`。

这意味着：

- Codex 可以创建参数、Animator State、转换条件和对应 StateAction 资产。
- 如果使用现有 `Run_withStaff` 作为临时占位，Codex 可以完成接线和播放速度调整。
- 真正符合角色骨架、姿势和性格的 Dash 骨骼动画，最好由动画师制作或由你提供 `.fbx/.anim` 后再接入。

---

## 3. Codex 能生成什么，以及哪些地方需要人工把关

| 内容 | 可自动完成度 | Codex 可以做什么 | 仍需要什么 |
|---|---|---|---|
| 冷却与体力代码 | 高 | 编写配置、运行时模型、Condition、Action、事件、测试 | 你确认最终规则和数值手感 |
| 状态机接线 | 高 | 创建 SO 资产、调整 Dash State 与 Transition Table、验证引用 | Play Mode 手感确认 |
| 速度曲线 | 高 | 添加 `AnimationCurve`、归一化采样、结束判定和调试信息 | 你在 Inspector 中微调曲线 |
| Animator 接线 | 高 | 创建 `IsDashing`、State、Transition、进入/退出 Action | 需要合适的动画 Clip 才能达到最终质量 |
| 粒子、拖尾 | 较高 | 基于现有尘土材质创建 Particle System/TrailRenderer 和播放 Action | 需要 Game View 中反复调大小、颜色和密度 |
| 残影 | 中 | 编写 SkinnedMesh 烘焙、对象池、淡出材质和 StateAction | 需要观察蒙皮、透明排序和性能 |
| Camera Impulse | 高 | 创建可配置的方向、强度和持续时间请求 | 需要避免晕动和震动过强 |
| UI 逻辑与 Prefab 接线 | 高 | 创建体力条脚本、事件监听、淡入淡出和低体力反馈 | 最终布局要结合实际分辨率检查 |
| UI/VFX 位图素材 | 中 | 可根据现有贴图和截图生成图标、遮罩、噪声等位图 | 需要你确认风格，且仍要设置 Unity 导入参数 |
| 原创音效 | 低 | 可以建立 AudioCue、随机组、混音配置并接入已有/外部音频 | 当前工具不能直接产出可靠的原创 Dash 音效文件 |
| 原创骨骼动画 | 低 | 可以接线、重定向已有 Clip、制作临时方案 | 最终 Dash 动画仍建议使用 DCC/动画师制作 |

结论：Codex 可以独立完成绝大多数“玩法系统和 Unity 集成”，也能生成可用的粒子、拖尾、残影和 UI 实现。最难自动保证风格一致的是原创骨骼动画和原创音效本体。

---

## 4. 推荐的总体架构

不要把冷却、持续时间、速度曲线和体力全部塞进 `Protagonist.cs`。推荐拆成下面几层。

```text
DashConfigSO
├─ Duration
├─ PeakSpeed
├─ SpeedCurve
├─ Cooldown
└─ StaminaCost（体力阶段再接入）

DashAbility（挂在 PigChef 上的运行时组件）
├─ 当前 Dash 方向
├─ 已经过时间 / NormalizedTime
├─ 下一次可用时间
├─ IsDashing / IsReady / IsFinished
├─ TryStartDash()
└─ CancelDash()

StateMachine
├─ HasDashInputCondition
├─ IsDashReadyCondition
├─ HasEnoughStaminaCondition
├─ CommitDashAction
├─ DashMovementAction
├─ DashFinishedCondition
└─ 表现类 Actions
```

体力部分则是：

```text
StaminaConfigSO             StaminaCostSO
├─ MaxStamina               ├─ ActionName
├─ RegenPerSecond           ├─ Amount
├─ RegenDelay               └─ 可选的恢复延迟覆盖
└─ 可选的 Exhausted 规则
          ↓
StaminaModel（纯 C# 规则，可做 EditMode 测试）
          ↓
StaminaController（MonoBehaviour 生命周期和事件桥接）
          ↓
Condition / StateAction / HUD
```

### 为什么增加 `DashAbility`

当前 `DashMovementAction` 自己知道方向，但外部 Condition 无法安全读取它的计时状态。如果继续使用独立的 `Timer_Dash.asset`，就会出现两份持续时间：

```text
DashMovementActionSO.Duration
Timer_Dash.timerLength
```

两者一旦被调成不同值，速度曲线已经结束但状态还没退出，或者状态提前退出而曲线没有走完。

`DashAbility` 作为该角色这一轮 Dash 的运行时事实来源，可以让移动 Action 和结束 Condition 读取同一个进度，避免重复配置。

### 为什么不直接照搬 HealthSO

项目的生命值系统使用共享 `HealthSO` 保存当前生命值。这对现有项目有效，但体力是每帧恢复、频繁变化且明显属于角色实例的运行时状态。直接创建一个全局 `StaminaSO.CurrentStamina` 容易产生：

- 退出 Play Mode 后资产保留脏值。
- 两个角色或测试实例共享同一份当前体力。
- 场景切换和重生时重置职责不清楚。

推荐让 `StaminaConfigSO` 只保存配置，让 `StaminaController/StaminaModel` 拥有当前体力。UI 通过事件读取归一化值，不拥有体力副本。

---

## 5. 13.1 Dash 冷却时间

### 5.1 规则建议

第一版建议：

```text
Cooldown：0.45 ~ 0.80 秒
推荐起始值：0.60 秒
计时来源：Time.time / Time.deltaTime（受暂停和 Time.timeScale 影响）
冷却开始：成功进入 Dash 时
冷却结束：到达 nextReadyTime
冷却期间按 Dash：不进入 Dash，不消耗体力
```

冷却必须在“动作成功提交”时开始，而不是收到输入时开始。否则在对话、受击、空中或体力不足时按键，也会无缘无故进入冷却。

### 5.2 新增配置

推荐在 `DashConfigSO` 中增加：

```csharp
[SerializeField, Min(0f)] private float _cooldown = 0.6f;
public float Cooldown => _cooldown;
```

不要用一个 `cooldownRemaining` 序列化字段保存运行时状态。

### 5.3 运行时状态

`DashAbility` 可以记录：

```csharp
private float _nextReadyTime;

public bool IsReady => Time.time >= _nextReadyTime;
public float CooldownRemaining => Mathf.Max(0f, _nextReadyTime - Time.time);
public float CooldownNormalized => ...;
```

成功开始时：

```csharp
_nextReadyTime = Time.time + _config.Cooldown;
```

### 5.4 新增 Condition

```text
IsDashReadyConditionSO
```

`Statement()` 只返回 `DashAbility.IsReady`，不能在 Condition 中启动冷却。

进入 Dash 的转换变为：

```text
Idle -> Dash
HasDashInput Is True
AND IsDashReady Is True
AND HasEnoughStamina(DashCost) Is True
```

```text
Walking -> Dash
HasDashInput Is True
AND IsDashReady Is True
AND HasEnoughStamina(DashCost) Is True
```

体力系统尚未完成时，先只使用前两个条件。

### 5.5 冷却验收

- 持续连按 Dash，不会在冷却内再次进入 Dash。
- 冷却内按键不会扣体力。
- 暂停游戏时冷却不会偷偷结束。
- Dash 被碰撞阻挡或提前中断时，第一版仍保留完整冷却。
- 重生后冷却重置为可用。

思考题：如果将来 Dash 被受击打断，你希望退还部分冷却吗？第一版建议不退，规则更清楚。

---

## 6. 13.2 Dash 速度曲线

### 6.1 曲线定义

推荐让曲线横轴固定表示 Dash 归一化时间：

```text
x = 0：刚进入 Dash
x = 1：Dash 结束
y：PeakSpeed 的倍率
```

每帧计算：

```csharp
float normalizedTime = Mathf.Clamp01(elapsed / duration);
float speedMultiplier = Mathf.Max(0f, speedCurve.Evaluate(normalizedTime));
float currentSpeed = peakSpeed * speedMultiplier;
```

然后只把速度写入 `movementVector.x/z`。这里仍然不能乘 `Time.deltaTime`，因为 `ApplyMovementVectorAction` 会统一处理。

### 6.2 推荐曲线手感

Dash 的第一帧应该立刻有明显速度，避免使用从 `0` 缓慢升起的普通 Ease In。

推荐起始关键点：

| 时间 x | 倍率 y | 意义 |
|---:|---:|---|
| 0.00 | 1.00 | 按下后立即冲出 |
| 0.15 | 1.10 | 极短的爆发峰值 |
| 0.55 | 0.75 | 中段开始衰减 |
| 1.00 | 0.00 | 结束时自然收速 |

曲线切线可以先使用平滑模式，再根据手感把开头改得更陡。

### 6.3 速度、时间和距离的关系

恒速时：

```text
距离 = 速度 × 时间
```

使用曲线后：

```text
距离 ≈ PeakSpeed × Duration × 曲线平均值
```

假设曲线平均值约为 `0.65`：

| PeakSpeed | Duration | 估计距离 |
|---:|---:|---:|
| 18 | 0.20 | 2.34 |
| 24 | 0.20 | 3.12 |
| 28 | 0.22 | 4.00 |

因此给固定速度 Dash 加入衰减曲线后，如果 PeakSpeed 不变，总距离通常会缩短。这不是 Bug，而是曲线面积变小了。

### 6.4 结束判定

新增：

```text
DashFinishedConditionSO
```

它读取 `DashAbility.IsFinished`，替换通用的 `Timer_Dash`。这样 `Duration` 只存在于 `DashConfigSO` 中。

退出转换保持：

```text
Dash -> Idle
DashFinished Is True
AND IsMoving Is False
```

```text
Dash -> Walking
DashFinished Is True
AND IsMoving Is True
```

### 6.5 速度曲线验收

- 30、60、120 FPS 下总距离基本一致。
- Inspector 修改曲线后，速度变化符合预期。
- 曲线末端为 0 时，不会残留上一帧高速度。
- 撞墙时不会穿透或持续抖动。
- Dash 结束后的 Walking 不会继承 Dash 峰值速度。

---

## 7. 13.3 空中 Dash

本阶段明确不添加空中 Dash。

需要保留以下规则：

- 只有 `Idle` 和 `Walking` 能进入 Dash。
- Jump 状态不增加到 Dash 的转换。
- 空中按下 Dash 不应在很久以后落地自动触发。
- 如果以后增加输入缓冲，只保留很短的有效窗口，例如 `0.08 ~ 0.12` 秒。

暂不添加并不意味着代码里写死 `isGrounded`。状态转换表已经表达了“允许从哪些状态进入”，优先让状态机承担这条规则。

---

## 8. 13.4 动画、音效、特效和镜头反馈

### 8.1 Dash State 推荐 Action 顺序

完整版本可以按下面的顺序组织：

```text
1. CommitDashAction
   - 锁定方向
   - 消耗 Dash 输入
   - 检查并消耗体力
   - 启动冷却和持续时间

2. IsDashing_True_OnEnter
3. PlaySound_Dash
4. PlayDashParticles
5. DashCameraImpulse
6. ControlDashAfterimage
7. DashMovement
8. GroundGravity
9. ApplyMovementVector
```

其中 `ControlDashAfterimage` 在 `OnStateEnter` 启动，在 `OnStateExit` 停止。退出时设置 Animator False 也应由一个明确的 OnExit Action 完成。

注意：如果 `CommitDashAction.TryStartDash()` 失败，理论上状态就不应该被进入。因此进入条件必须先保证冷却和体力可用；提交方法仍应再次验证并返回结果，防止以后其他系统在同一帧改变资源。

### 8.2 动画方案

推荐新增 Animator Bool：

```text
IsDashing
```

对应两个 Action 资产：

```text
IsDashing_True_OnEnter.asset
IsDashing_False_OnExit.asset
```

使用 Bool 而不是 Trigger 的理由：

- Dash 有明确持续时间。
- 被打断时能在 OnExit 清理。
- Animator 可以根据 bool 平稳离开 Dash State。

Animator 建议：

```text
Any State / Locomotion -> DashAnimation：IsDashing == true
DashAnimation -> Locomotion：IsDashing == false
Has Exit Time：关闭或只作为保护
Transition Duration：0.03 ~ 0.08
Root Motion：关闭
```

位移权威仍然是 `CharacterController`，不要让 Dash 动画 Root Motion 再移动一次。

当前没有专用 Dash Clip。实施优先级：

1. 临时复用 `Run_withStaff`，提高播放速度并快速验证 Animator 接线。
2. 由动画师制作身体前倾、脚步快速蹬地、手杖稳定的短 Dash Clip。
3. 替换 Clip，不改变玩法代码。

### 8.3 音效方案

项目当前的角色音效以短促、自然、偏 Foley 的声音为主，`AudioCueSO` 已支持多变体随机播放。Dash 音效应避免科幻激光感，推荐组成：

```text
主体：0.12 ~ 0.25 秒的空气 whoosh
质感：轻微布料/木质摆动
落地感：很轻的尘土扑声，可选
变体：2 ~ 3 个，RandomNoImmediateRepeat
```

新增资产建议：

```text
Assets/Audio/SFX/Characters/Actions/PigChef/Dash_01.wav
Assets/Audio/SFX/Characters/Actions/PigChef/Dash_02.wav
Assets/ScriptableObjects/Audio/AudioCues/SFX/Protagonist/SFX_Dash.asset
Assets/ScriptableObjects/StateMachine/Protagonist/Actions/SFX/PlaySound_Dash.asset
```

配置优先复用项目已有 `2.5DSFX_Config`，保持与 Jump、Swing 相同的 SFX Mixer 路由和空间感。

没有新音频时，可以临时引用 `SFX_SwingCane` 验证链路，但不要把它当最终音效，因为劈砍瞬态通常比 Dash 更尖锐。

Codex 可以完成 Cue、随机组、事件通道、Mixer 配置和 StateAction 接线；最终 `.wav` 建议由你提供、从合规音效库选择，或交给声音设计工具制作。

### 8.4 起步尘土粒子

项目已有：

```text
PuffOfDust.fbx
PuffOfDust.mat
JumpParticle.prefab
LandingParticle.prefab
WalkingParticle.prefab
```

最一致的做法是基于这些资源制作 `DashStartParticle.prefab`，而不是生成一套发光魔法粒子。

建议视觉：

- 颜色：暖白、浅米色，受环境光影响。
- 形状：向 Dash 反方向喷出。
- 数量：6 ~ 12 个短命粒子。
- Lifetime：约 `0.18 ~ 0.35` 秒。
- 起始速度：中等，略带横向散开。
- 大小：比 Walking Dust 明显，比 Landing Full Intensity 稍小。
- 不使用持续发射，进入 Dash 时 Burst 一次。

新增：

```text
PlayerEffectController.PlayDashParticles(Vector3 dashDirection)
PlayDashParticlesActionSO
DashStartParticle.prefab
```

粒子只表现 Dash，不负责决定 Dash 是否开始。

### 8.5 拖尾方案

第一版更推荐 `TrailRenderer`，原因是实现稳定、成本低、很容易与现有 Toon 风格匹配。

建议：

```text
Time：0.08 ~ 0.15 秒
Width：角色宽度的 0.25 ~ 0.45
颜色：暖白 -> 透明，或浅黄绿 -> 透明
材质：URP Unlit Transparent
拐角/端点顶点：少量即可
```

可以新增 `ControlDashTrailActionSO`：

- `OnStateEnter`：Clear 后开始 emitting。
- `OnStateExit`：停止 emitting，让已有尾迹自然消失。
- 重生或 Disable 时：Clear，避免旧轨迹留在新位置。

### 8.6 残影方案

残影不是简单复制整个 PigChef GameObject。角色包含 Animator、碰撞、状态机和多个组件，直接 Instantiate 会制造严重副作用。

推荐实现：

1. 找到主角色 `SkinnedMeshRenderer`。
2. 按固定间隔调用 `BakeMesh`。
3. 从小型对象池取一个只有 `MeshFilter + MeshRenderer` 的 Ghost。
4. 使用 URP Unlit 透明材质绘制烘焙网格。
5. 在 `0.12 ~ 0.20` 秒内淡出并归还池。

建议限制：

```text
生成间隔：0.04 ~ 0.07 秒
同时存在：最多 3 ~ 5 个
阴影：关闭
接收阴影：关闭
碰撞：无
材质实例：使用 MaterialPropertyBlock，不要每个残影 new Material
```

风格上建议使用暖白或低饱和黄绿色，不使用高亮霓虹蓝。也可以只做脚下和手杖拖影，减少整个角色“分身”的感觉。

残影属于第二阶段表现。先让音效、尘土和 TrailRenderer 工作，再判断是否真的需要它。

### 8.7 Camera Shake / Impulse

当前 `ShakeCamActionSO` 只能广播一个无参数事件，`CameraManager` 会调用同一个 `CinemachineImpulseSource.GenerateImpulse()`。这适合受击，但 Dash 往往需要更轻、更短、带方向的反馈。

推荐新增可配置请求，而不是直接复用受击强度：

```text
CameraImpulseRequest
├─ Direction
├─ Amplitude
├─ Duration（或 ImpulseDefinition）
└─ SourcePosition
```

第一版 Dash 参数建议非常克制：

```text
Amplitude：受击震动的 20% ~ 40%
方向：Dash 反方向的轻推
持续：0.08 ~ 0.15 秒
```

验收时要用键鼠和手柄分别体验，避免连续 Dash 导致镜头晕动。

### 8.8 美术风格一致性原则

从当前项目资源可以提炼出以下方向：

- 角色和场景偏温暖、卡通、低饱和，不适合赛博霓虹特效。
- 现有移动反馈主要是实体尘土，而不是魔法能量。
- `SlashToon.png` 使用简洁、尖锐、白色的图形语言。
- UI 使用棕色、橄榄黄绿、米白等自然色。
- 音频是自然 Foley 和角色动作声音，音效随机组用于减少重复感。

因此 Dash 推荐关键词：

```text
短促、弹性、尘土、布料/空气、暖白、低饱和、卡通，不发光或仅极弱发光
```

如果后续让我直接生成 VFX 或 UI 位图，最好先给我一张你认可的 Game View 截图，并指出“更偏尘土”还是“更偏速度线”。这样可以用现有 `PuffOfDust`、`SlashToon` 和 UI 调色板作为风格参考。

---

## 9. 13.6 完整体力系统

### 9.1 第一版正式规则

推荐起始数值：

| 参数 | 起始值 | 说明 |
|---|---:|---|
| Max Stamina | 100 | 便于理解百分比和调参 |
| Regen Per Second | 20 | 空体力约 5 秒回满，不含延迟 |
| Regen Delay | 0.75 秒 | 最后一次成功消耗后等待 |
| Dash Cost | 25 | 满体力可连续使用 4 次，但受冷却限制 |
| Jump Cost | 15 | 不让普通移动过度受限 |
| Attack Cost | 12 | 允许较长连击，但不能无限挥砍 |

规则：

- 只有动作真正开始时才消耗体力。
- 体力不足时，动作不开始，也不启动相应冷却。
- 最后一次成功消耗后的 `RegenDelay` 内不恢复。
- 之后按 `RegenPerSecond × Time.deltaTime` 连续恢复。
- 当前体力限制在 `[0, MaxStamina]`。
- 暂停时不恢复。
- 角色死亡或失效时暂停恢复；重生时第一版恢复满。
- 对话期间可选择继续恢复，推荐继续恢复，减少玩家等待。

### 9.2 为什么这不只是一个 float

一个完整体力系统至少要区分：

```text
配置：最大值、恢复速度、恢复延迟、动作消耗
运行时：当前值、上次消耗时间、是否恢复、是否耗尽
命令：CanSpend、TrySpend、Restore、Reset
通知：数值改变、拒绝动作、耗尽/恢复
表现：HUD、闪烁、声音
集成：Dash、Jump、Attack
测试：边界、帧率、暂停、重生和重复请求
```

如果只有 `public float stamina`，其他脚本会随意做 `stamina -= 20`，以后很难知道谁扣成了负数，也无法统一触发 UI 和恢复延迟。

### 9.3 数据层

#### `StaminaConfigSO`

只保存全局规则：

```csharp
MaxStamina
RegenerationPerSecond
RegenerationDelay
ResetToFullOnRespawn
UseScaledTime
```

第一版推荐固定使用缩放时间，可以暂不暴露 `UseScaledTime`，避免无意义配置。

#### `StaminaCostSO`

每类动作建立独立资产：

```text
DashStaminaCost.asset
JumpStaminaCost.asset
AttackStaminaCost.asset
```

字段：

```csharp
ActionId / DisplayName
Amount
可选：RegenDelayOverride
```

独立资产的好处是 Condition、Action、UI 提示和调试器引用的是同一个成本定义，不会在多个地方分别填 `25`。

### 9.4 纯规则层 `StaminaModel`

推荐用普通 C# 类保存核心规则：

```text
Current
Max
Normalized
SecondsUntilRegeneration
CanSpend(cost)
TrySpend(cost)
Restore(amount)
Tick(deltaTime)
ResetToFull()
```

它不访问 `MonoBehaviour`、Transform、Animator、UI 或 StateMachine，因此可以在 EditMode 下快速测试。

`TrySpend` 是唯一合法的扣除入口：

```text
cost < 0          -> 配置错误
cost == 0         -> 成功但不改变数值
Current < cost    -> 返回 false，不扣除
Current >= cost   -> 扣除、重置恢复延迟、返回 true
```

使用浮点数时，在 UI 显示和边界测试中要使用容差，不直接比较复杂计算后的 `float == 0`。

### 9.5 Unity 桥接层 `StaminaController`

挂在 `PigChef.prefab` 根对象，与 `Protagonist`、StateMachine 同级。

职责：

- 从 `StaminaConfigSO` 初始化 `StaminaModel`。
- 在 `Update` 中调用 `Tick(Time.deltaTime)`。
- 对外提供只读属性和 `CanSpend/TrySpend`。
- 数值变化时广播 UI 事件。
- Disable、死亡、重生和场景切换时执行明确的生命周期规则。

推荐 API：

```csharp
public float Current { get; }
public float Max { get; }
public float Normalized { get; }
public bool CanSpend(StaminaCostSO cost);
public bool TrySpend(StaminaCostSO cost);
public void Restore(float amount);
public void ResetToFull();
```

不要公开：

```csharp
public float currentStamina;
```

### 9.6 事件与 HUD

项目已有 `FloatEventChannelSO` 和 `BoolEventChannelSO`，第一版可以复用：

```text
StaminaNormalizedChanged：float，范围 0~1
StaminaSpendRejected：Void 或包含 ActionId 的专用事件
StaminaExhaustedChanged：bool，可选
```

HUD 推荐放在现有 `Canvas-Gameplay.prefab`，靠近生命 UI：

```text
StaminaBar
├─ Background：深棕
├─ Fill：橄榄黄绿
├─ LowStaminaOverlay：低体力时偏橙/闪烁
└─ CanvasGroup：满体力后延迟淡出
```

`UIStaminaBar` 只负责显示：

- 收到 normalized 后修改 `Image.fillAmount` 或 Slider value。
- 消耗时立即显示。
- 满体力保持约 0.5 秒后淡出。
- 不足时短促闪烁，不自己修改体力。
- OnEnable 订阅，OnDisable 取消订阅。

为了和现有 UI 保持一致，优先复用 `BGSlider.png`、`FGSlider.png`、棕色边框和橄榄黄绿色。只有现有素材无法满足时，再生成新的体力图标或遮罩。

### 9.7 通用 StateMachine Condition

新增：

```text
HasEnoughStaminaConditionSO
```

它持有一个 `StaminaCostSO` 引用，在 `Awake` 缓存 `StaminaController`，`Statement()` 只调用：

```csharp
return stamina.CanSpend(cost);
```

Condition 绝对不能调用 `TrySpend`，因为状态机可能检查多个转换，失败的组合条件不应消耗资源。

### 9.8 通用 StateAction

新增：

```text
SpendStaminaActionSO
```

它在 `OnStateEnter` 调用 `TrySpend(cost)`。正常情况下进入条件已经检查过，因此应成功；如果失败，输出明确的开发期错误或触发拒绝事件，不能静默让体力变负数。

对于 Dash 和 Jump，通用 Action 足够。Attack 需要额外处理“一次输入经过多个攻击状态”的问题，见下文。

---

## 10. Dash 接入体力

### 10.1 转换条件

```text
HasDashInput True
AND IsDashReady True
AND HasEnoughStamina(DashCost) True
```

### 10.2 成功提交

推荐由 `CommitDashAction` 在 `OnStateEnter` 完成一个完整事务：

1. 再次确认冷却可用。
2. 再次确认体力足够。
3. `TrySpend(DashCost)`。
4. 消耗 Dash 输入。
5. 锁定方向。
6. 设置 `_nextReadyTime`。
7. 将 Dash 计时清零。

因为 State 已经进入，步骤 1~3 正常不应失败。保留二次验证是为了以后加入 Buff、多人或同帧其他消耗时更安全。

### 10.3 输入不足时

体力不足时不要保留一个永不过期的 `dashInput = true`，否则玩家会在体力恢复后自动 Dash。

建议把 Dash 输入缓存从永久 bool 升级成短时请求：

```text
lastDashPressedTime
dashInputBuffer = 0.10 秒
HasDashInput = Time.time - lastDashPressedTime <= buffer
```

冷却或体力不足时，按键可以触发一次 HUD 闪烁，但请求在约 0.1 秒后自动过期。

---

## 11. Jump 接入体力

跳跃消耗不能加到所有空中 State，否则从上升切换到下降时会重复扣除。

只在真正“起跳”的状态消耗：

```text
JumpAscending
JumpAscendingAttacking
```

不要在这些状态消耗：

```text
JumpDescending
JumpDescendingAttacking
```

因为角色从台阶边缘掉下去也会进入下降状态，这不是主动跳跃，不应扣体力。

进入起跳状态的转换增加：

```text
IsHoldingJump True
AND HasEnoughStamina(JumpCost) True
```

对应 State 的 Action 列表在 `AscendAction` 之前加入 `SpendStamina(JumpCost)`。

需要验证：

- 按住跳跃只扣一次。
- 从上升进入下降不再扣。
- 从高处走落不扣。
- 体力不足时不进入起跳状态。
- 体力不足时松开再按，恢复足够后可以正常跳。

---

## 12. Attack / 劈砍接入体力

这是三种动作里最容易重复扣除的一种。

当前项目有多个攻击状态：

```text
IdleAttacking
WalkAttacking
JumpAscendingAttacking
JumpDescendingAttacking
```

同一次攻击可能因为角色移动或到达跳跃顶点，在这些状态之间转换。如果简单地把 `SpendStamina(AttackCost)` 加到所有攻击 State，状态切换就可能再次扣除。

### 12.1 推荐：请求序号去重

每次收到新的 Attack 输入时：

```csharp
attackRequestSequence++;
attackInput = true;
```

`SpendAttackStaminaAction` 记录：

```text
lastChargedAttackRequestSequence
```

进入任意攻击状态时：

```text
如果当前 requestSequence 已收费：不再扣
如果是新 requestSequence：TrySpend，并记录已收费编号
```

所有攻击 State 必须引用同一个 `SpendAttackStaminaActionSO` 资产。当前状态机对同一 SO 会复用运行时 Action 实例，因此去重编号能够跨攻击状态保持。

### 12.2 条件与输入过期

所有“非攻击状态 -> 攻击状态”的转换加入：

```text
HasAttackInput True
AND HasEnoughStamina(AttackCost) True
```

攻击状态之间为了保持同一次动作而发生的转换，不需要重新检查和消费体力。

体力不足时也不应让 `attackInput` 永久残留。建议给攻击请求增加和 Dash 类似的短缓冲，或者在拒绝后明确消费本次请求并提示 HUD。

### 12.3 不要依赖动画事件扣体力

当前 `ConsumeAttackInput()` 由 Animation Event 调用。动画事件可以继续负责清理攻击输入或开启命中判定，但不建议让它成为体力扣除权威：

- 动画 Clip 缺少事件时不会扣。
- 替换动画可能改变规则。
- 帧率、过渡和动画中断会让时序更难理解。

体力应该在玩法状态成功进入时提交，动画事件只服务表现或攻击帧窗口。

---

## 13. 可选的进阶体力规则

以下内容能让系统更完整，但建议基础版稳定后逐项加入。

### 13.1 Exhausted 状态与滞回

如果体力降到 0，可以进入 `Exhausted`：

- 体力条明显闪烁。
- 在恢复到最大值的 20% 前，不允许高消耗动作。
- 达到恢复阈值后才解除。

使用两个阈值可以避免体力在 0 附近反复切换状态。是否采用需要你先确认，因为它会明显改变游戏难度。

### 13.2 动作恢复延迟覆盖

不同动作可以有不同恢复延迟：

```text
Attack：0.45 秒
Jump：0.60 秒
Dash：0.85 秒
```

这比单纯提高消耗更容易调出节奏，但第一版可以全部使用全局 `0.75` 秒。

### 13.3 体力修改器

未来食物、装备或 Buff 可能提供：

```text
MaxStaminaMultiplier
RegenMultiplier
CostMultiplier by ActionId
FlatCostReduction
```

先把修改器作为 `StaminaController` 的明确接口，不要现在就创建复杂 Buff 框架。等真正出现第二个修改来源时再抽象。

### 13.4 存档

建议：

- 当前体力不存档，加载/重生后恢复满。
- 如果以后有永久升级，只保存最大体力或恢复属性的升级数据。
- 不把运行时 `Current` 写回配置 SO。

---

## 14. 建议新增和修改的文件

以下是规划清单，实际实施时可以分阶段创建。

### 14.1 Dash 核心

```text
新增：Assets/Scripts/Characters/Abilities/DashAbility.cs
新增：Assets/Scripts/Characters/Config/DashConfigSO.cs
新增：Assets/Scripts/Characters/StateMachine/Conditions/IsDashReadyConditionSO.cs
新增：Assets/Scripts/Characters/StateMachine/Conditions/DashFinishedConditionSO.cs
新增：Assets/Scripts/Characters/StateMachine/Actions/CommitDashActionSO.cs
修改：Assets/Scripts/Characters/StateMachine/Actions/DashMovementActionSO.cs
修改：Assets/Prefabs/Characters/PigChef.prefab
修改：Assets/ScriptableObjects/StateMachine/Protagonist/States/Dash.asset
修改：Assets/ScriptableObjects/StateMachine/Protagonist/PigChef_TransitionTable.asset
```

### 14.2 体力核心

```text
新增：Assets/Scripts/Characters/Stamina/StaminaModel.cs
新增：Assets/Scripts/Characters/Stamina/StaminaController.cs
新增：Assets/Scripts/Characters/Config/StaminaConfigSO.cs
新增：Assets/Scripts/Characters/Config/StaminaCostSO.cs
新增：Assets/Scripts/Characters/StateMachine/Conditions/HasEnoughStaminaConditionSO.cs
新增：Assets/Scripts/Characters/StateMachine/Actions/SpendStaminaActionSO.cs
新增：Assets/Scripts/Characters/StateMachine/Actions/SpendAttackStaminaActionSO.cs
新增：Assets/Scripts/UI/UIStaminaBar.cs
修改：Assets/Prefabs/Characters/PigChef.prefab
修改：Assets/Prefabs/UI/GameplayScene/Canvas-Gameplay.prefab
修改：相关 Jump / Attacking State 资产
修改：PigChef_TransitionTable.asset
```

### 14.3 Dash 表现

```text
可新增：PlayDashParticlesActionSO.cs
可新增：ControlDashTrailActionSO.cs
可新增：ControlDashAfterimageActionSO.cs
可新增：DashAfterimageEmitter.cs
可新增：DashCameraImpulseActionSO.cs
可新增：DashStartParticle.prefab
可新增：DashTrailMaterial.mat
可新增：DashAfterimageMaterial.mat
可新增：SFX_Dash.asset
可新增：PlaySound_Dash.asset
修改：PlayerEffectController.cs
修改：PigChef.prefab
修改：PigChef.controller
```

所有 `.asset`、`.prefab` 和 `.controller` 修改优先通过 Unity Editor/MCP 完成，不手写 GUID。修改后必须检查 Missing Reference 和 YAML 差异。

---

## 15. 推荐实施顺序

### 阶段 0：固定基础版本

1. 修正 Dash 两条退出转换。
2. 用 Debugger 确认 Dash 能持续完整时间。
3. 记录当前 Speed、Duration、距离和 Console 基线。
4. 提交或备份一个可回退版本。

完成标准：恒速 Dash 在不同帧率下稳定。

### 阶段 1：统一 Dash 配置和运行时状态

1. 创建 `DashConfigSO`。
2. 创建 `DashAbility`。
3. 将方向、进度、结束判定和冷却移到明确的运行时拥有者。
4. 用 `DashFinishedCondition` 替换独立 Timer。

完成标准：持续时间只有一个配置来源。

### 阶段 2：加入冷却

1. 创建 `IsDashReadyCondition`。
2. 在进入 Dash 的转换中加入该条件。
3. 成功开始时记录 `_nextReadyTime`。
4. 测试连按、暂停、碰墙和重生。

完成标准：冷却内不会进入 Dash，也不会误启动冷却。

### 阶段 3：加入速度曲线

1. 在 `DashConfigSO` 增加曲线和 PeakSpeed。
2. 使用统一进度采样。
3. 计算估计距离并调整 PeakSpeed。
4. 测试 30/60/120 FPS。

完成标准：曲线手感清楚且总距离稳定。

### 阶段 4：体力核心与 HUD

1. 先写 `StaminaModel` 和 EditMode 测试。
2. 写 `StaminaController`。
3. 建立 Config 和三种 Cost 资产。
4. 建立 UI 事件和 `UIStaminaBar`。
5. 只做测试按钮或 Inspector 调用验证，不立刻接所有动作。

完成标准：消耗、延迟、恢复、上限、暂停和 UI 都独立正确。

### 阶段 5：接入 Dash

1. Dash 转换检查体力。
2. Commit Dash 时消费体力。
3. 不足时 HUD 反馈，输入请求过期。
4. 验证冷却与体力不会发生“只启动一个”的半成功状态。

完成标准：一次成功 Dash 恰好扣一次。

### 阶段 6：接入 Jump

只给主动起跳状态增加检查和消费，验证走下台阶不扣体力。

### 阶段 7：接入 Attack

实现攻击请求序号去重，覆盖地面、走路、上升和下降攻击状态。

完成标准：一次攻击请求无论经过几个攻击状态，都只扣一次。

### 阶段 8：表现反馈

推荐添加顺序：

```text
音效接线
-> 起步尘土
-> TrailRenderer
-> 轻微 Camera Impulse
-> Animator 接线和占位 Clip
-> 专用动画 Clip
-> 最后判断是否需要残影
```

每次只加一种反馈并录制前后对比，避免出现“效果很多但不知道哪一项破坏了手感”。

---

## 16. 测试计划

### 16.1 StaminaModel EditMode 测试

- 初始化为最大值。
- 消耗恰好等于当前值后为 0。
- 消耗大于当前值时失败且数值不变。
- 负消耗被拒绝或报告配置错误。
- 恢复不超过最大值。
- 恢复延迟结束前不恢复。
- 延迟结束后按秒率恢复。
- 不同 `deltaTime` 切分得到近似相同结果。
- ResetToFull 正确。

### 16.2 Dash PlayMode / 手工验证

- Idle 和 Walking 都能 Dash。
- 空中不能 Dash。
- 冷却内不能 Dash。
- 体力不足不能 Dash。
- 被拒绝时不扣体力、不启动冷却。
- 成功时只扣一次。
- 曲线结束时退出到正确状态。
- 撞墙不穿透、不持续抖动。
- 暂停时计时和恢复按设计停止。
- 重生后临时状态清理。

### 16.3 Jump 验证

- 按住跳跃只扣一次。
- 上升转下降不扣第二次。
- 走下高处不扣。
- 体力不足时动作不发生。

### 16.4 Attack 验证

- 原地攻击、走路攻击、上升攻击、下降攻击各扣一次。
- 攻击状态互相转换不重复扣。
- 连续两次输入分别扣两次。
- 体力不足时不会在恢复后自动挥砍。

### 16.5 表现验证

- Animator 无不存在参数的报错。
- Dash 动画不产生 Root Motion 双重位移。
- 音效进入正确 SFX Mixer，暂停和音量设置有效。
- 连续触发不会创建无限 AudioSource、粒子或残影。
- 残影池能回收，材质不会逐次实例化。
- 粒子和拖尾在重生/传送后不留在旧位置。
- Camera Impulse 不明显晕动。

---

## 17. 风险与回退策略

| 风险 | 表现 | 预防/回退 |
|---|---|---|
| Duration 重复配置 | 曲线和状态退出不同步 | 使用 `DashConfigSO + DashFinishedCondition` 单一来源 |
| Condition 扣体力 | 转换失败也消耗 | Condition 只查，Action/Commit 才扣 |
| Attack 重复扣除 | 状态切换多扣一次 | 请求序号去重，同一 Action SO 跨状态复用 |
| 输入永久缓存 | 恢复体力后自动动作 | 使用 0.1 秒左右输入缓冲或拒绝后消费 |
| Root Motion 双位移 | Dash 距离异常 | Animator 关闭 Root Motion，移动权威仍是 CharacterController |
| 残影频繁 Instantiate | GC 和卡顿 | 小型池、数量上限、MaterialPropertyBlock |
| 共用受击镜头震动 | Dash 震动过强 | 独立的可配置 Impulse 请求 |
| Runtime 值写入 SO | Play 后资产脏值 | 当前体力由角色实例拥有，SO 只存配置 |
| Prefab/Asset 引用丢失 | Missing Script/Reference | Unity Editor 创建资产、逐项检查 Diff 和 Console |

每个阶段应独立提交。出现问题时只回退当前阶段，不回退已验证的基础 Dash。

---

## 18. 实施前需要你最终决定的设计问题

这些问题不会阻止先写核心结构，但会影响最后的手感：

1. Dash 冷却是从开始计算，还是 Dash 结束后才开始？本规划推荐从开始计算。
2. Dash 碰墙是否立即结束？基础版可以等 Duration 结束，后续再加入中断。
3. 体力为 0 时是否进入明显的 Exhausted 状态？本规划暂不强制。
4. 体力条始终显示，还是满体力时隐藏？推荐满后淡出。
5. 攻击体力不足时，是直接无动作，还是仍允许播放一个很短的失败反馈？推荐不攻击，但闪烁体力条。
6. Dash 视觉更偏“尘土爆发”还是“角色残影”？当前项目风格更适合先做尘土和短拖尾。
7. 是否愿意提供一个专用 Dash 动画和 2~3 个 Dash Whoosh 音效？如果没有，可以先使用占位资源完成全部逻辑链路。

---

## 19. 最终完成标准

- Dash 的持续时间、曲线和冷却只有明确的配置来源。
- Dash、Jump、Attack 都在动作真正开始时恰好消耗一次体力。
- 体力不足不会产生延迟自动动作。
- 体力有上限、恢复延迟、每秒恢复、重生规则和 HUD。
- 玩法逻辑不依赖动画事件、音效或粒子。
- 表现反馈分别由独立 StateAction 组合。
- 空中 Dash 保持未启用。
- Animator 无缺失参数，Console 无新增错误。
- 不同帧率下 Dash 距离和体力恢复近似一致。
- 连续触发不会产生无界对象、材质或事件订阅。
- 文档中的手工 Inspector/Prefab 接线均已完成并验证。

当这些条件全部满足后，这套功能才算从“能冲一下”升级成了可继续扩展的 Dash 与体力系统。
