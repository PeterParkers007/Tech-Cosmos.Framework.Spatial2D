# Tech-Cosmos Spatial2D

2D 空间判定包。它是一份**空间查询数据库**，不是物理引擎。

一句话：你把圆和矩形登记进去，自己改位置，需要的时候问「这块区域叠了谁」。系统自己不会每帧跑、不会推开物体、不会发回调。

---

## 目录

1. [它是什么 / 不是什么](#它是什么--不是什么)
2. [两层怎么分](#两层怎么分)
3. [形状与坐标系](#形状与坐标系)
4. [分层掩码](#分层掩码)
5. [底层 API（纯 C#）](#底层-api纯-c)
6. [查询结果 SpatialHit](#查询结果-spatialhit)
7. [Unity 组件层](#unity-组件层)
8. [位置同步（最容易用错）](#位置同步最容易用错)
9. [典型用法](#典型用法)
10. [性能与网格](#性能与网格)
11. [第一版明确不做](#第一版明确不做)
12. [程序集与安装](#程序集与安装)

---

## 它是什么 / 不是什么

### 是

- 管理圆形、矩形两种判定区域
- 按需回答：点、圆、矩形有没有叠上已登记的区域
- 按需扫掠：一块区域沿位移走过去，会先碰到谁
- 用整数层掩码过滤「查哪些层」

### 不是

| 不做 | 含义 |
|------|------|
| 不模拟物理 | 没有重力、质量、力、速度 |
| 不处理碰撞响应 | 不反弹、不挡住、不改 Transform |
| 不主动更新 | 没有每帧必跑的核心循环 |
| 不负责移动 | 位置由外部写入 `SetPosition` |
| 不自动跟 Transform | 组件层也不每帧同步 |
| 不做回调 | 不会 `OnCollisionEnter` |
| 不做复杂形状 | 没有多边形、网格、凹形 |
| 不做 3D | 只用 X/Y |

Unity 自带的 `Rigidbody2D` / `Collider2D` 是另一套。本包装的是「谁和谁叠了」，叠完你自己决定扣血、停步还是无视。

---

## 两层怎么分

```
┌─────────────────────────────────────────┐
│  Unity 组件（TechCosmos.Spatial2D.Unity）│
│  CircleArea2D / RectArea2D              │
│  配置、Enable 注册、Disable 注销、Gizmos │
└──────────────────┬──────────────────────┘
                   │ 只在注册/注销/改参数时碰底层
                   ▼
┌─────────────────────────────────────────┐
│  纯 C# 库（TechCosmos.Spatial2D）        │
│  SpatialWorld / SpatialBody             │
│  不引用 UnityEngine                     │
└─────────────────────────────────────────┘
```

- **底层**：数据结构 + 查询。可以在无 Unity 的测试里单独用。
- **上层**：给策划/关卡挂预制体用。对标 `CircleCollider2D` / `BoxCollider2D` 的编辑体验，但**不管物理、不自动跟位移**。

两条用底层的路：

| 用法 | 谁写 Body |
|------|----------|
| 挂了 `CircleArea2D` / `RectArea2D` | 走路 `SetPosition` 或 `SyncPosition`；缩放 `SetScale` 或 `SyncScale`；两个都变了用 `SyncPose` |
| 不挂组件，纯代码 `world.AddCircle` | 你自己拿着返回的 `SpatialBody` 调 |

底层只有一个入口：`body.SetPosition(x, y)`。组件**不会**在 `Update` / `LateUpdate` 里自动调它。

不要对同一块 Body 又让组件误以为会自动同步、又自己乱改，规则就一条：**谁改了世界坐标，谁负责 SetPosition。**

---

## 形状与坐标系

### 圆

- 数据：中心 `(x, y)` + 半径 `radius`（**缩放为 1 时**的半径）
- API：`AddCircle(x, y, radius)`
- 实际判定半径 = 这个半径 × |缩放X|。两边一样大时就是 × 那个数。

### 矩形（第一版轴对齐）

- 数据：中心 `(x, y)` + **全宽** `width` + **全高** `height`（都是 **缩放为 1 时** 的尺寸）
- 中心在矩形正中间，不是左下角
- 内部按半宽半高存储
- 实际判定宽高 = 1 倍尺寸 × |缩放|
- `AddRect(..., angle)` 和 `SetRotation` **已留接口，第一版不参与判定**。现在矩形始终按轴对齐算。Gizmos 按当前缩放画，不转角。

### 坐标

底层是普通 2D 浮点，不绑定像素或 Unity 单位。游戏里建议和 `transform.position` 的 XY 用同一套世界单位。

查询时的圆/矩形**不会**登记进世界。它们是一次性问题：「我手里这块区域，叠到库里哪些 Body？」子弹、冲击波、近战框都应该这样问，不必把自己做成 Body。

---

## 分层掩码

`layer` 是**位旗标**，不是 0～31 的层号。

```csharp
body.Layer = 1;           // 第 0 位
const int Enemy = 1 << 0; // 1
const int Wall  = 1 << 2; // 4

world.OverlapCircle(x, y, r, mask: Enemy | Wall); // 1 | 4
```

- 查询 `mask` 默认 `-1`（`SpatialWorld.AllLayers`）：所有层都查
- 命中条件：`(body.Layer & mask) != 0`
- 组件 Inspector 里的 `Layer` 默认是 `1`

建议自己在游戏里建一套常量（单位、墙、拾取…），不要和 Unity `LayerMask` 混成一个数，除非你有意对齐。

---

## 底层 API（纯 C#）

命名空间：`TechCosmos.Spatial2D`。

### 创建世界

```csharp
var world = new SpatialWorld();      // 网格边长默认 4
var world = new SpatialWorld(8f);    // 更大格子，物体很大或很稀时用
```

### 登记 / 注销

```csharp
SpatialBody unit = world.AddCircle(0f, 0f, 0.5f, layer: 1);
SpatialBody box  = world.AddRect(2f, 0f, width: 1f, height: 2f, layer: 1);
SpatialBody box2 = world.AddRect(2f, 0f, 1f, 2f, angle: 0f, layer: 1); // angle 预留

world.Remove(unit);
```

`Add*` 返回的 `SpatialBody` 请自己拿着。注销后 `body.IsValid == false`。

### 改位置 / 改尺寸 / 改缩放 / 改层

```csharp
body.SetPosition(newX, newY);
body.GetPosition(out float x, out float y);

body.SetCircle(0.8f);          // 仅圆：改的是缩放为 1 时的半径
body.SetRect(1.2f, 2f);        // 仅矩形：改的是缩放为 1 时的全宽全高
body.SetScale(2f);             // 人现在是 2 倍，框也是 2 倍
body.SetScale(1.2f, 1.2f);     // 两边一样就两个数都写这个
body.GetScale(out float sx, out float sy);
body.Layer = 1 << 2;
body.UserData = myUnit;        // 任意引用，查询后找回业务对象

body.SetRotation(30f);         // 第一版写入即忘，不参与重叠
```

人的缩放是几就 `SetScale` 写几。变成 2 就是 2，变成 1 就是 1，变成 1.5 就是 1.5。**不是叠乘**：连续写 `SetScale(2)` 两次，框还是 2 倍，不会变成 4 倍。负数当翻转，大小按绝对值。

圆两边倍数不一样时，只用 X。你们平时两边一样，`SetScale(2)` 或 `SetScale(2, 2)` 都行。

没动的 Body 不要每帧 `SetPosition` / `SetScale`。没变会早退，但少调更好。缩放没变就别写。

### 重叠查询

都有「返回新 List」和「填进你传入的 List」两套。热路径请复用 List，少 GC。

```csharp
// 点
List<SpatialHit> hits = world.OverlapPoint(x, y);
world.OverlapPoint(x, y, hits, mask);

// 圆
world.OverlapCircle(x, y, radius, hits, mask);

// 轴对齐矩形（中心 + 全宽全高）
world.OverlapRect(x, y, width, height, hits, mask);

// 只要 Body，不要 Hit 细节
List<SpatialBody> bodies = world.QueryRect(x, y, width, height);
world.QueryRect(x, y, width, height, bodies, mask);
```

查询用的圆/矩形**不是**库里的 Body，只是问题描述。

### 扫掠（Cast）

`(dirX, dirY)` 是**这一步的位移向量**，不是单位方向。和文档示例一致：`vx * dt`。

```csharp
SpatialHit hit = world.CastCircle(x, y, radius, vx * dt, vy * dt, mask);
SpatialHit hit = world.CastRect(x, y, width, height, dx, dy, mask);

if (!hit.IsValid)
{
    // 这一步路上没有碰到已登记的 Body
}
```

返回**最先碰到的一个**。起点已经叠在别人身上时，`distance` 为 0。

Cast 适合「我准备走这么远，会不会撞墙」。冲击波那种「每帧问当前这块逻辑区域叠了谁」用 Overlap，不必 Cast。

---

## 查询结果 SpatialHit

```csharp
public struct SpatialHit
{
    public SpatialBody other; // 碰到的登记物体；未命中为 null
    public float pointX, pointY;
    public float normalX, normalY;
    public float distance;
    public bool IsValid;      // other 还活着
}
```

| 字段 | Overlap | Cast |
|------|---------|------|
| `other` | 叠上的 Body | 路上第一个 Body |
| `point` | 对方表面上的参考点 | 沿位移走到接触时，查询形状的中心位置 |
| `normal` | 大致从对方指向查询中心 | 接触法线（轴对齐盒为轴方向） |
| `distance` | 圆为穿透深度（≥0）；矩形重叠时可能为 0 | 沿位移走到接触的路程（位移长度 × t） |

`other.UserData` 是你登记时塞进去的对象。组件登记时会塞 `Area2D` 自身，所以：

```csharp
foreach (var hit in hits)
{
    if (hit.other.UserData is CircleArea2D area)
    {
        var unit = area.GetComponent<Unit>();
        // ...
    }
}
```

纯代码登记时，直接把 `Unit` 塞进 `UserData` 更干净。

---

## Unity 组件层

命名空间：`TechCosmos.Spatial2D.Unity`。

菜单：**Add Component → Tech-Cosmos → Spatial2D**

| 组件 | 对标 | Inspector |
|------|------|-----------|
| `CircleArea2D` | CircleCollider2D | 半径、Offset、Layer。拖边：对边不动、圆心跟着走；拖中间：只挪位置 |
| `RectArea2D` | BoxCollider2D | 宽、高、Offset、Layer、Angle（预留）。Scene 里拖四条边中间的点改大小，对边不动 |
| `Spatial2DWorld` | 无（可选） | 网格边长。场景里最多有效一份 |

一个 GameObject 可以挂多个 Area。`UserData` 指向该 Area 组件。

### 组件生命周期

1. `OnEnable`：按当前 `Transform + Offset` 算中心，用组件上的 1 倍尺寸登记，再按**当时**的缩放写一次 `SetScale`
2. 之后**不再**自己盯 Transform。走路写 `SetPosition`，缩放变了写 `SetScale`
3. `OnDisable`：`world.Remove(Body)`

对象池回收会走 Disable，Body 会卸掉；再取出 Enable 会重新登记。重新登记后必须再 `SetPosition` 到正确位置（Enable 时用的是当时的 Transform）。

### 世界从哪来

1. 组件上拖了 `Spatial2DWorld` → 用那份
2. 否则场景里有 `Spatial2DWorld` 单例 → 用那份
3. 否则用 `Spatial2D.Default` 静态世界（第一次访问时创建）

关卡建议挂一个 `Spatial2DWorld`，网格大小和场景尺度匹配。`Spatial2DWorld` 的 `Awake` 比较早（`DefaultExecutionOrder(-200)`），减少 Area 比世界先 Enable 的错位。

### Gizmos

Scene 里画两套线框（Play 开着 Gizmos 也能看）：

- **绿 / 选中黄**：跟 Transform + Inspector 尺寸（皮上的框）
- **品红 / 选中橙**：跟库里 `Body` 的真实坐标和已乘缩放的尺寸（查询用的框）

两套重合就是对齐了；只挪了 Transform、没 `SyncPosition`，品红会停在旧位置。矩形第一版不按 Angle 转。

选中组件后，和 BoxCollider2D 一样可以直接拖：

- 矩形：四条边中间各一个点。拖哪条边，对边不动，宽高和 Offset 一起变
- 圆：上下左右四个点，拖哪边对边不动，圆心和半径一起变；中间一个点只挪位置

Inspector 里有「编辑区域」按钮，默认关，和 Unity 的 Edit Collider 一样。点开后才能在 Scene 里拖边，并且这时不能点选或拖走物体、移动工具也藏起来，只能改框。再点「完成编辑」退出。拖出来的宽高/半径仍是 **缩放 1 时** 的数。

绿/黄跟皮，品红/橙跟 `Body`。只挪 Transform、没写 Body，两套框会分开，这是刻意的。

### 可选的一次对齐

这三个都不会自己调用。从 Transform 读，写到 Body。

```csharp
area.SyncPosition();           // 原点用当前 Transform，加 offset
area.SyncPosition(originX, originY); // 原点你给（ECS 位置），里面加 offset
area.SyncScale();              // 只同步缩放
area.SyncPose();               // 位置 + 缩放一起同步
```

原点来自 ECS 时用带参数的 `SyncPosition`，不要自己 `Body.SetPosition`。只改缩放用 `SyncScale` 或 `SetScale`。传送、出池子后两个都变了，用一次 `SyncPose`。

Unity 侧还有扩展：

```csharp
using TechCosmos.Spatial2D.Unity;

area.Body.SetPosition(new Vector2(x, y));
area.Body.SetScale(new Vector2(2f, 2f));
Vector2 p = area.Body.GetPosition();
Vector2 s = area.Body.GetScale();
```

---

## 位置同步（最容易用错）

错误预期：挂上组件，单位走了，判定跟着走。

实际：组件只在 Enable 时写一次位置。之后单位走了，**你必须**在移动代码里：

```csharp
transform.position = next;
area.SyncPosition();
```

或移动系统只改逻辑坐标、Transform 只是皮，那只 `SetPosition` 即可。

缩放变了（1 → 1.2 → 2 → 5.6）同样写一次：

```csharp
transform.localScale = new Vector3(2f, 2f, 1f);
area.SyncScale();
```

位置和缩放一起变了：

```csharp
area.SyncPose();
```

人的缩放是几，框就写几。不要每帧写。

为什么不每帧自动同步：

- 底层必须保持「你不问、它不算、它也不扫全部 Transform」
- 一万个静止物每帧写位置是白烧
- 你们自己有移动系统，动谁就更新谁，这是按需

---

## 典型用法

### 1. 纯代码：单位身体 + 范围问询

```csharp
using TechCosmos.Spatial2D;

var world = new SpatialWorld();

var soldier = world.AddCircle(0f, 0f, radius: 0.4f, layer: 1);
soldier.UserData = soldierUnit;

// 移动系统
soldier.SetPosition(soldierUnit.X, soldierUnit.Y);

// 治疗波 / 近战：用你们已经算好的逻辑矩形去问
var hits = world.OverlapRect(boxX, boxY, boxW, boxH, mask: 1);
foreach (var hit in hits)
{
    var unit = hit.other.UserData as Unit;
}
```

弹道、技能框**不要**登记成 Body。它们是查询参数。

### 2. 预制体挂 CircleArea2D

1. 单位预制体加 `CircleArea2D`，半径对齐体型
2. 出生后 Enable 自动进库
3. 移动系统每次改坐标后 `area.Body.SetPosition(...)`
4. 技能：`Spatial2D.Default.OverlapCircle(...)` 或场景里那份 `Spatial2DWorld.World`

### 3. 移动前探路（Cast）

```csharp
float dx = vx * dt;
float dy = vy * dt;
var hit = world.CastCircle(x, y, radius, dx, dy, mask: wallLayer);

if (!hit.IsValid)
{
    x += dx;
    y += dy;
    body.SetPosition(x, y);
}
else
{
    x = hit.pointX;
    y = hit.pointY;
    body.SetPosition(x, y);
}
```

### 4. AI 索敌

```csharp
world.OverlapCircle(selfX, selfY, detectRadius, hits, enemyMask);
```

---

## 性能与网格

世界按格子分桶。`SetPosition` 只更新该 Body 占的格子。查询先扫相关格子，再做圆/矩形精确判定。

| 规模（经验） | 预期 |
|--------------|------|
| 几百 | 查询开销通常可忽略 |
| 几千 | 60fps 正常 |
| 更大 | 调大 `cellSize`、少做全图查询、复用 List |

`cellSize` 建议和「一个普通单位大概多大」一个数量级。过小：占很多格、移动改桶多。过大：一格里东西太多，窄相变线性扫。

热路径：

```csharp
readonly List<SpatialHit> _hits = new List<SpatialHit>(16);
world.OverlapCircle(x, y, r, _hits, mask);
```

不要为了「快」去用 Unity Physics2D 再包一层。本包的点就是躲开那套。

---

## 第一版明确不做

- 独立编辑器窗口（用组件 Inspector + Gizmos）
- 物理材质、刚体、关节
- 碰撞回调
- CCD（连续碰撞）。高速子弹用 Cast 近似「这一步位移」
- 3D
- **旋转矩形真正参与判定**（字段和 API 已留）
- 组件每帧自动同步 Transform

---

## 程序集与安装

| 程序集 | 依赖 | 用途 |
|--------|------|------|
| `TechCosmos.Spatial2D.Core` | 无 Unity | `SpatialWorld` 等 |
| `TechCosmos.Spatial2D.Unity` | Core | 组件与 `Spatial2D.Default` |
| `TechCosmos.Spatial2D.Unity.Editor` | Unity | Scene 拖点改大小 |

UPM 名：`com.tech-cosmos.spatial2d`。Unity 2021.3+。

游戏工程里目前也有一份嵌入：`Assets/Package-TechCosmos/Tech-Cosmos.Framework.Spatial2D`。

 Dest 仓库：`Tech-Cosmos.Framework.Spatial2D`。

业务程序集引用 `TechCosmos.Spatial2D.Unity`（或只要 Core）即可。`autoReferenced` 为 true，默认能看到类型。

---

## 对照检查

挂上组件、单位在走，判定还在出生点 → 移动后没 `SetPosition`。  
Gizmos 跟着人走、技能打不中 → 你看的是 Transform，库里还是旧坐标。  
人已经 2 倍大、框还是 1 倍小 → 改了缩放没 `SetScale`。  
组件上的宽高按 2 倍外观调、场景里物体又是 2 倍 → 框会再乘一次，变成 4 倍。宽高按 **1、1** 调。  
矩形斜着飞、查询却像正放的盒子 → 第一版矩形不转；斜向框请自己在查询侧用旋转矩形（或先 Overlap 大一点的 AABB 再自己滤）。  
`hit.other.GetComponent` 编不过 → `SpatialBody` 不是 Unity 组件，走 `UserData`。
