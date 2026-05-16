# 自定义规则引擎重构方案

## 目标

把当前“写死几种告警卡片”的实现，重构为一套真正通用的规则系统：

- 用户可以新增任意数量的规则。
- 每条规则可以由多个条件组合而成。
- 每条规则可以独立配置动作、冷却、命中目标、名称、启用状态。
- 不考虑前向兼容。
- 不做迁移逻辑。
- 不保留当前固定规则模型的兜底分支。

这次重构的目标不是“在现有枚举上继续打补丁”，而是直接把规则系统从“预置规则卡片”升级成“条件驱动的规则引擎”。

## 当前实现的问题

当前规则系统的核心问题不是 UI 不够灵活，而是数据模型本身决定了它不可能灵活：

1. `AlertRuleKind` 是写死的枚举。
2. `AlertRule` 的字段是按固定规则类型硬编码出来的。
3. 编辑器是按 `Kind` 分支渲染的，而不是按“条件类型”和“动作类型”渲染。
4. 部分页面直接写死了某个规则实例，比如呼号正则编辑页只服务固定规则。
5. 规则匹配逻辑不是通用求值器，而是按每个 `Kind` 分开写 switch。

当前相关位置：

- `Core/Models/AlertRuleKind.cs`
- `Core/Models/AlertRule.cs`
- `Core/Models/AlertRuleCatalog.cs`
- `Core/Services/AlertRuleMatcher.cs`
- `UI/Activities/AlertRulesActivity.cs`
- `UI/Activities/AlertRuleEditorActivity.cs`
- `Core/ViewModels/CallsignPatternViewModel.cs`

这意味着，如果继续沿用当前设计，每增加一种自定义能力，都要继续加枚举值、加字段、加 switch、加专用页面。长期一定失控。

## 目标架构

重构后，规则系统应拆成四层：

1. `Rule Definition`
   - 规则本身的静态定义
   - 包括名称、启用状态、条件树、动作、冷却等
2. `Rule Evaluation Context`
   - 一次解码消息或一次 logged QSO 触发时的上下文
   - 所有条件都从这里读数据
3. `Rule Evaluator`
   - 通用条件求值器
   - 不再按固定规则类型写分支
4. `Rule Editor UI`
   - 按“结构化树编辑页”动态渲染
   - 通过 `+ predicate` / `+ group` 编辑当前条件组
   - 不再依赖固定规则种类

## 核心设计

### 1. 删除 `AlertRuleKind` 的中心地位

`AlertRuleKind` 不应再承担“规则模板”职责。

建议做法：

- 可以直接删除 `AlertRuleKind`。
- 如果仍想保留触发源分类，只保留一个更小的枚举，例如：
  - `DecodeMessage`
  - `LoggedQso`

即：

- 规则的“行为”来自条件树，不来自 `Kind`
- 规则的“触发时机”来自 `TriggerType`

建议替代结构：

```csharp
public enum RuleTriggerType
{
    DecodeMessage,
    LoggedQso
}
```

### 2. 重写 `AlertRule`

当前 `AlertRule` 结构是字段拼盘，必须整体替换。

建议新结构：

```csharp
public sealed class AlertRule
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RuleSource Source { get; set; }
    public bool IsEnabled { get; set; }
    public RuleTriggerType TriggerType { get; init; }
    public RuleConditionGroup RootCondition { get; set; } = RuleConditionGroup.CreateAnd();
    public RuleActionConfig Actions { get; set; } = new();
    public int CooldownSeconds { get; set; } = 10;
    public int Priority { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

补充：

```csharp
public enum RuleSource
{
    SystemPreset,
    UserDefined
}
```

说明：

- 系统内置规则和自定义规则共用同一底层结构
- 系统内置规则和自定义规则共用同一求值器
- 区别只在于来源和 UI 权限，例如是否允许删除
- `TriggerType` 创建后不可切换
- `Priority` 数字越小优先级越高
- 优先级相同按 `CreatedAtUtc` 决定先后

### 3. 条件树模型

规则必须支持组合条件，所以不能只是一维列表，应该直接上树。

```csharp
public enum RuleConditionGroupMode
{
    All,
    Any
}

public abstract class RuleConditionNode
{
}

public sealed class RuleConditionGroup : RuleConditionNode
{
    public RuleConditionGroupMode Mode { get; set; }

    // 递归子节点:
    // Children 里既可以放 RulePredicate, 也可以再放 RuleConditionGroup
    public List<RuleConditionNode> Children { get; set; } = [];
}

public sealed class RulePredicate : RuleConditionNode
{
    public RuleField Field { get; set; }
    public RuleOperator Operator { get; set; }
    public RuleOperand Operand { get; set; } = new();
}

public sealed class RuleConstantPredicate : RuleConditionNode
{
    public bool Value { get; set; }
}
```

这里的“树”是递归定义出来的，不是一层列表。

原因在于：

- `RuleConditionGroup` 自己是 `RuleConditionNode`
- 但 `RuleConditionGroup.Children` 的元素类型也是 `RuleConditionNode`

所以一个 `Group` 的子节点里可以继续放另一个 `Group`，于是可以形成：

```txt
All
├── Predicate
├── Predicate
└── Any
    ├── Predicate
    └── All
        ├── Predicate
        └── Predicate
```

也就是说：

- `RuleConditionGroup` 表示条件组
- `RulePredicate` 表示原子条件
- `RuleConstantPredicate` 表示显式恒真/恒假节点
- 条件组里可以嵌套条件组
- 这才是真正能表达 `(A and B) or (C and D)` 这种结构的树

如果 `Children` 的类型只是 `List<RulePredicate>`，那就只能有一层，不能嵌套，不足以表达复杂逻辑。

这里再明确一个约束：

- 每条规则的 `RootCondition` 永远必须是 `RuleConditionGroup`
- 不允许根节点直接是 `RulePredicate`

这样做的原因是：

- 编辑页永远有稳定的根容器
- `+ predicate` / `+ group` 的挂载点永远明确
- UI 不需要处理“根节点先是叶子、后来再升格成 group”的额外逻辑

### 3.1 `RulePredicate` 是什么

`RulePredicate` 是一条“原子判断”，可以理解成规则树里最小的一片叶子。

它负责表达这种句子：

- `Mode == "FT8"`
- `Snr >= -15`
- `TransmitterCallsign regex "^JA"`
- `ReceiverCallsign == MyCallsign`

结构上它由三部分组成：

1. `Field`
   - 左值，要检查哪个字段
2. `Operator`
   - 比较方式，怎么检查
3. `Operand`
   - 右值，和什么比较

所以它本质上就是：

```txt
Field Operator Operand
```

例如：

```txt
TransmitterCallsign Equals MyCallsign
Snr GreaterThanOrEqual -15
MessageText Regex "^CQ"
```

这里需要特别说明：

- `RulePredicate` 不是语法分析器里的 token
- `RulePredicate` 也不是一整条规则
- `RulePredicate` 更不是用户输入的一段表达式字符串

它只是规则树里的一个叶子节点，由 UI 组装、由 JSON 持久化、由求值器直接执行。

### 3.1.1 为什么引入显式恒真 predicate

这次不依赖空 group 的隐式语义，而是引入显式恒真节点。

原因：

- 语义更明确
- 持久化结果更直观
- UI 可以明确展示“恒真条件”
- 不需要靠 `All([])` / `Any([])` 的约定来表达“任意消息”或“任意 logged QSO”

因此：

- `任意消息` 规则建议直接使用 `RuleConstantPredicate { Value = true }`
- `QSO 完成` 规则也建议用显式恒真节点

### 3.2 `Operand` 设计

右值不应该继续用 `List<string> Values` 这种过于松散的结构，否则类型系统帮不上忙。

建议改成显式的 operand 模型：

```csharp
public enum RuleValueType
{
    String,
    Number,
    Boolean,
    StringList,
    NumberList,
    ContextRef,
    NamedSetRef
}

public enum RuleContextRef
{
    MyCallsign,
    MyGrid
}

public enum RuleNamedSetRef
{
    IgnoredCallsigns
}

public enum NamedSetBandMatchMode
{
    MatchBand,
    IgnoreBand
}

public sealed class RuleOperand
{
    public RuleValueType Kind { get; set; }

    public string? StringValue { get; set; }
    public double? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }

    public List<string> StringListValue { get; set; } = [];
    public List<double> NumberListValue { get; set; } = [];

    public RuleContextRef? ContextRefValue { get; set; }
    public RuleNamedSetRef? NamedSetRefValue { get; set; }
    public NamedSetBandMatchMode? NamedSetBandMatchModeValue { get; set; }
}
```

这样“匹配我的呼号”就不是特殊规则语义，而只是普通条件：

```txt
TransmitterCallsign == $MyCallsign
ReceiverCallsign == $MyCallsign
```

其中 `$MyCallsign` 在结构里就是 `ContextRefValue = MyCallsign`。

### 3.3 `NamedSet` 设计

除了普通常量和上下文引用，规则树还需要支持“引用一个外部命名数据集”。

第一版最重要的命名数据集就是：

- `IgnoredCallsigns`

这不是把 ignored list 直接内嵌进规则 JSON，而是：

- ignored callsign 仍然作为独立数据集单独存储
- 规则树通过 `NamedSetRef` 去引用它
- 求值时由 resolver 根据当前上下文查询该数据集

这样树里可以表达：

```txt
TransmitterCallsign InNamedSet IgnoredCallsigns
ReceiverCallsign NotInNamedSet IgnoredCallsigns
```

这比保留单独的“忽略呼号匹配目标”全局设置更统一。

### 3.4 `IgnoredCallsigns` 的 band 匹配语义

`IgnoredCallsigns` 不是普通字符串集合，它本质上是一个带 band 语义的数据集。

因此：

```txt
TransmitterCallsign InNamedSet IgnoredCallsigns
```

不能简单理解成：

```txt
TransmitterCallsign in ["BA1XYZ", "JA1ABC", ...]
```

更准确的理解应当是：

```txt
(TransmitterCallsign, CurrentBand) in IgnoredCallsigns
```

但为了兼顾更粗粒度的忽略需求，规则树应支持 predicate 级别选择是否按 band 匹配。

建议规则右值携带：

- `NamedSetRefValue = IgnoredCallsigns`
- `NamedSetBandMatchModeValue = MatchBand / IgnoreBand`

设计原则：

- 默认 `MatchBand`
- 允许用户显式改成 `IgnoreBand`
- 不做全局开关
- 这个选项属于 predicate 本身，而不是整个 app 的全局设置

推荐语义：

- `MatchBand`
  - 对 decode message: 用 `(callsign, current band)` 查询 ignored dataset
  - 对 logged QSO: 用 `(callsign, logged qso band)` 查询 ignored dataset
- `IgnoreBand`
  - 只按 callsign 查询 ignored dataset，忽略 band

这样既保留当前 ignored callsign 的主语义，又给用户更灵活的选择。

`CurrentBand` 的来源不另起一套新逻辑，直接沿用当前代码里已有的 band 解析逻辑。

### 3.5 是否需要语法分析器

不需要。

这次方案不是 DSL parser，也不是让用户手写表达式。

第一版链路应当是：

```txt
树编辑页 -> 结构化 JSON -> RuleEvaluator
```

不是：

```txt
用户输入字符串表达式 -> Parser -> AST -> Evaluator
```

也就是说：

- 内部结构虽然像 AST
- 但用户不会直接编辑语法文本
- `RulePredicate` 和 `RuleConditionGroup` 是业务数据结构，不是 parser 产物

### 4. 字段与操作符设计

第一版不要做完全开放式脚本，应该做“有限字段 + 有限操作符”。

建议字段：

```csharp
public enum RuleField
{
    MessageText,
    TransmitterCallsign,
    ReceiverCallsign,
    Mode,
    Snr,
    OffsetFrequencyHz,
    OffsetTimeSeconds,
    DialFrequencyHz,
    TransmitterGrid,
    FromCountryId,
    ToCountryId,
    IsLowConfidence,
    IsOffAir,
    IsIgnored,
    IsUserTransmit,
    IsSystemNotice,
    LoggedQsoCallsign,
    LoggedQsoBand
}
```

建议操作符：

```csharp
public enum RuleOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    Regex,
    In,
    NotIn,
    InNamedSet,
    NotInNamedSet,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    StartsWith,
    EndsWith,
    IsTrue,
    IsFalse,
    Exists,
    NotExists
}
```

这已经足够覆盖大多数需求：

- 我的呼号
- 指定呼号正则
- 选中的 DXCC
- 仅 CQ
- 仅 FT8
- 仅高 SNR
- 仅某频段
- 仅含 RR73
- 仅接收方命中
- 仅忽略名单外
- 引用 `IgnoredCallsigns` 这种外部命名数据集

### 5. 动作模型

当前动作只有通知和振动，但结构上应该单独抽出来。

```csharp
public sealed class RuleActionConfig
{
    public bool SendNotification { get; set; }
    public bool Vibrate { get; set; }
    public string NotificationTitleTemplate { get; set; } = string.Empty;
    public string NotificationBodyTemplate { get; set; } = string.Empty;
    public int HighlightColorArgb { get; set; }
}
```

先不一定要把模板和颜色都做完，但结构要预留到位。

## 上下文模型

当前 `DecodedRadioMessage` 还带有一些“为了旧规则准备的派生字段”，例如：

- `ContainsMyCallsign`
- `MatchesWatchedCallsignPattern`
- `MatchesSelectedDxcc`

这类字段不应该继续作为规则系统输入，而应该改成“通用上下文 + 可选派生缓存”。

建议新增：

```csharp
public sealed class RuleEvaluationContext
{
    public RuleTriggerType TriggerType { get; init; }
    public DecodedRadioMessage? Message { get; init; }
    public WsjtQsoLoggedEvent? LoggedQso { get; init; }
    public AppSettings Settings { get; init; } = new();
    public Dictionary<RuleField, object?> CachedValues { get; init; } = new();
    public string CurrentBand { get; init; } = string.Empty;
}
```

所有规则判断都基于这个上下文取值。

## 求值器设计

### 1. 替换 `AlertRuleMatcher`

当前 `AlertRuleMatcher` 是按规则类型写死的：

- `MatchesMyCallsign(...)`
- `MatchesWatchedCallsign(...)`
- `MatchesSelectedDxcc(...)`

这套要整体删除，替换成通用求值器：

```csharp
public interface IRuleEvaluator
{
    bool Evaluate(AlertRule rule, RuleEvaluationContext context);
}
```

内部结构：

- `EvaluateRule()`
- `EvaluateGroup()`
- `EvaluatePredicate()`
- `ResolveFieldValue()`
- `ApplyOperator()`

### 2. 字段解析器

建议单独抽一个字段访问器：

```csharp
public interface IRuleFieldValueResolver
{
    object? Resolve(RuleField field, RuleEvaluationContext context);
}
```

这样好处是：

- 规则引擎不直接依赖 UI 或特定消息对象结构
- 新增字段时只改 resolver，不改整个 matcher

### 3. 触发链路

当前链路：

- 解码消息 -> 构造 `DecodedRadioMessage`
- 直接调用固定规则匹配器

重构后：

- 解码消息 -> 构造 `RuleEvaluationContext`
- 过滤出 `TriggerType = DecodeMessage` 的规则
- 统一评估
- 生成 `AlertRuleEvaluation`

logged QSO 同理。

## UI 重构方案

### 1. 规则列表页

当前规则列表页是“展示固定卡片”。

重构后改成：

- 顶部：`新建自定义规则`
- 中间：系统内置规则列表
- 下方：自定义规则列表
- 每条规则展示：
  - 名称
  - 启用状态
  - 触发类型
  - 优先级
  - 条件摘要
  - 动作摘要
  - 冷却
- 支持排序
- 系统规则默认不可删除
- 自定义规则可删除
- 支持复制规则

用户流程应当是：

1. 点击 `新建自定义规则`
2. 进入新页面
3. 先选择 `TriggerType`
4. 再在根组下通过 `+ predicate` / `+ group` 开始搭树

系统内置规则在列表页中应明确标注为只读。

### 2. 规则编辑页

当前编辑页是根据 `Kind` 决定显示哪些控件。

重构后应改成结构化树编辑页：

- 规则名称
- 规则来源
- 启用
- 触发类型
  - 自定义规则创建后不可切换
- 条件编辑区
- 动作编辑区
- 冷却时间
- 优先级
- 删除按钮（仅自定义规则）

条件编辑区的交互模型应固定为：

- 页面总是绑定一条规则的 `RootCondition`
- 当前展开的每个 `RuleConditionGroup` 都显示：
  - `全部满足 / 任一满足`
  - `+ predicate`
  - `+ group`
  - 删除当前 group（根组除外）
- 每个 `RulePredicate` 都显示：
  - 字段
  - 操作符
  - 右值
  - 删除当前 predicate

### 3. 条件编辑器

不建议第一版做拖拽式可视化编辑器。

建议第一版 UI：

- 根节点固定为一个条件组
- 允许在任意 group 下点击 `+`
- `+` 只提供两种动作：
  - 新增 `predicate`
  - 新增 `group`
- 每个 predicate 一行：
  - 字段 spinner
  - 操作符 spinner
  - 值输入控件
- 每个 group 支持切换：
  - `全部满足`
  - `任一满足`
- 支持删除非根 group
- 支持删除 predicate

控件类型按字段/操作符自动切换：

- bool 字段 -> 开关或真假选择
- 数值字段 -> 数字输入
- callsign / 文本 -> 文本框
- DXCC -> 多选列表
- `InNamedSet / NotInNamedSet` -> 命名集合选择
- 当命名集合为 `IgnoredCallsigns` 时，再显示 band 匹配模式：
  - `按 band 匹配`
  - `忽略 band`
- 不再保留独立 `match target` 控件

这里特别约定：

- 第一版允许单 child group
- 第一版不做树结构 normalize
- 用户搭出来的树按原样存储
- 不做“自动折叠无意义 group”这种隐式改写

### 4. 取消“匹配目标”这个特殊概念

当前 `MatchTarget` 是规则层面的特殊字段，这会妨碍通用化。

重构后建议删除 `AlertRuleMatchTarget` 这层特殊抽象。

改成更明确的条件：

- `TransmitterCallsign regex JA.*`
- `ReceiverCallsign equals BA1XYZ`
- `FromCountryId in [339, 291]`
- `ToCountryId in [339, 291]`

这样表达能力更强，而且不需要额外的 target 语义分支。

## 系统内置规则与自定义规则

不考虑兼容和迁移的前提下，建议保留若干系统内置规则，但它们只是“预先创建好的规则实例”，不是特殊匹配逻辑。

应用启动后直接存在几条系统规则，例如：

- 我的呼号
- 指定呼号
- 任意消息
- DXCC
- Logged QSO

同时允许用户点击按钮新建自定义规则。

必须满足：

- 系统规则和自定义规则共用同一数据结构
- 系统规则和自定义规则共用同一求值器
- 系统规则和自定义规则共用同一编辑页

区别只在：

- 是否允许删除
- 是否允许重命名
- 排序策略

这里进一步明确：

- 系统内置规则完全只读
- 不允许修改名称
- 不允许修改条件树
- 不允许修改动作
- 不允许修改优先级
- 不允许修改启用状态
- 不允许删除
- 仅用于展示系统默认逻辑

## 建议的系统内置规则

### 规则 1：我的呼号

- TriggerType: `DecodeMessage`
- 条件组：`Any`
  - `TransmitterCallsign == $MyCallsign`
  - `ReceiverCallsign == $MyCallsign`

### 规则 2：指定呼号正则

- TriggerType: `DecodeMessage`
- 根组建议先预置为 `Any`
- 用户后续可以继续往里面加更多 predicate / group
- 不再保留 `CallsignPatterns` 专属字段
- 多个正则条件直接由树表达，例如：
  - `TransmitterCallsign regex "^JA"`
  - `ReceiverCallsign regex "^JA"`
  - `TransmitterCallsign regex "^BY"`
  - `ReceiverCallsign regex "^BY"`

### 规则 2.5：忽略呼号过滤

旧的“忽略呼号匹配目标”全局设置应删除，改成规则树表达。

例如：

- 仅过滤发射方在 ignored list 中的消息
  - `TransmitterCallsign InNamedSet IgnoredCallsigns [MatchBand]`
- 仅过滤接收方在 ignored list 中的消息
  - `ReceiverCallsign InNamedSet IgnoredCallsigns [MatchBand]`
- 过滤双方任一命中的消息
  - `Any`
    - `TransmitterCallsign InNamedSet IgnoredCallsigns [MatchBand]`
    - `ReceiverCallsign InNamedSet IgnoredCallsigns [MatchBand]`

如果用户希望忽略某个 callsign 的全部 band，也可以显式改成：

- `TransmitterCallsign InNamedSet IgnoredCallsigns [IgnoreBand]`

### 规则 3：指定 DXCC

- TriggerType: `DecodeMessage`
- 条件组：`Any`
  - `FromCountryId in <selected>`
  - `ToCountryId in <selected>`

### 规则 4：任意消息

- TriggerType: `DecodeMessage`
- 条件组：包含一个 `RuleConstantPredicate { Value = true }`

### 规则 5：QSO 完成

- TriggerType: `LoggedQso`
- 条件组：包含一个 `RuleConstantPredicate { Value = true }`

## 数据持久化策略

当前仍然可以继续使用 `alert_rules` JSON 存储，但结构要完全切换。

建议：

- 直接替换 `AlertRule` JSON schema
- 不保留旧字段
- 不保留旧规则 kind 的兼容分支
- 老数据读不出来就按空规则列表处理，或者 reset 为新默认模板列表

这符合这次“不考虑兼容”的要求。

## 需要删除的旧设计

以下设计应直接删除，不建议保留双轨：

1. `AlertRuleKind` 对规则行为的支配作用
2. `AlertRuleMatchTarget`
3. `CallsignPatterns` 这种规则专属字段
4. `SelectedDxccIds` 这种规则专属字段
5. `ContainsMyCallsign / MatchesWatchedCallsignPattern / MatchesSelectedDxcc` 这种为固定规则服务的派生布尔值
6. `MatchesMyCallsign / MatchesWatchedCallsign / MatchesSelectedDxcc` 这种专用匹配函数
7. `AlertRuleCatalog.CreateDefaultRules()` 这种预置固定规则清单
8. 直接绑定固定 rule id 的 ViewModel / Activity

## 代码级重构清单

### Core 层

重写或新增：

- `Core/Models/AlertRule.cs`
- `Core/Models/RuleTriggerType.cs`
- `Core/Models/RuleConditionGroup.cs`
- `Core/Models/RuleConditionNode.cs`
- `Core/Models/RuleConstantPredicate.cs`
- `Core/Models/RuleField.cs`
- `Core/Models/RuleOperator.cs`
- `Core/Models/RuleNamedSetRef.cs`
- `Core/Models/NamedSetBandMatchMode.cs`
- `Core/Models/RuleActionConfig.cs`
- `Core/Models/RuleEvaluationContext.cs`
- `Core/Services/RuleEvaluator.cs`
- `Core/Services/RuleFieldValueResolver.cs`
- `Core/Services/RuleNamedSetResolver.cs`

删除或废弃：

- `Core/Models/AlertRuleKind.cs`
- `Core/Models/AlertRuleMatchTarget.cs`
- `Core/Services/AlertRuleMatcher.cs`
- `Core/Models/AlertRuleCatalog.cs` 的固定规则部分

### ViewModel 层

重写：

- `AlertRulesViewModel`
- `AlertRuleEditorViewModel`
- `CallsignPatternViewModel`
- `DxccSelectionViewModel`

建议新增：

- `RuleTemplatePickerViewModel`
- `RuleConditionEditorViewModel`
- `RuleActionEditorViewModel`

### UI 层

重写：

- `UI/Activities/AlertRulesActivity.cs`
- `UI/Activities/AlertRuleEditorActivity.cs`

删除专用页面，或把它们降级为通用子编辑器：

- `CallsignPatternActivity`
- `DxccSelectionActivity`

建议新增：

- `RuleTemplatePickerActivity`
- `RuleConditionEditorDialog` 或同等编辑区
- `RuleValuePickerActivity`（用于 DXCC、多值条件）

## 推荐实施顺序

### 第一阶段：重建数据模型和求值器

目标：让新规则引擎在 Core 层跑通。

步骤：

1. 定义新的规则模型。
2. 实现 `RuleEvaluationContext`。
3. 实现字段解析器。
4. 实现通用条件求值器。
5. 用单元测试覆盖常见规则组合。

先不碰 UI。

### 第二阶段：接入解码消息与 logged QSO 触发链路

步骤：

1. `DecodedMessageFactory` 改成只构造消息，不再计算固定规则派生字段。
2. `WatcherController` 在消息到达时创建 `RuleEvaluationContext`。
3. 通知链路改成直接消费新规则求值结果，并且只触发最高优先级命中的那条通知。
4. 主列表高亮也改成基于规则求值结果，而不是固定布尔字段。

### 第三阶段：重做规则列表和编辑页

步骤：

1. 规则列表页支持增删改查。
2. 新建规则时先选模板。
3. 编辑页改成通用条件编辑模式。
4. 去掉旧的正则专页和 DXCC 专页依赖。

### 第四阶段：清理旧实现

步骤：

1. 删除旧枚举和固定匹配函数。
2. 删除旧字符串文案。
3. 删除旧默认规则结构。
4. 清理旧测试。

## 测试策略

这次重构最重要的是 Core 层测试，而不是 UI 测试。

至少要覆盖：

1. `All` / `Any` 条件组
2. 文本 `contains / regex / equals`
3. 数值比较
4. bool 条件
5. 空值处理
6. logged QSO 与 decode message 上下文分离
7. 冷却逻辑不回归
8. 多规则并发命中
9. 优先级数字越小越优先
10. 同优先级按创建时间决胜

建议直接在 `WsjtxWatcher.Tests` 中新增：

- `RuleEvaluatorTests`
- `RuleFieldValueResolverTests`
- `RuleTemplateFactoryTests`

## 我建议的最终落地范围

如果这次要一步到位，我建议你接受下面这些破坏性调整：

1. 彻底换掉旧 `AlertRule` schema
2. 彻底删掉固定规则卡片思路
3. 彻底删掉 `MatchTarget` 这种特殊字段
4. 彻底改成“模板 + 条件树 + 动作配置”
5. 允许第一版 UI 不完美，但 Core 设计必须一次到位

## 一句话结论

这次不要做“在现有规则系统上支持一点自定义”。

应该直接把告警系统重构成：

- `规则模板创建`
- `条件树编辑`
- `通用求值器`
- `动作配置`
- `统一规则列表`

这样后面再加“CQ 过滤、波段过滤、模式过滤、SNR 过滤、消息文本过滤、国家过滤、组合条件”都不会再需要重写整套架构。
