# 项目预警

定时执行 SQL 查询或 API 调用的桌面监控工具，当结果不符合预期时在桌面悬浮窗中展示预警信息。
（项目使用 claudeai 的代码生成能力辅助开发）

## 功能特性

### 预警规则
- 支持 SQL Server、MySQL 数据库查询
- 支持 HTTP API 调用监控
- Cron 表达式定时执行
- 预警分级（信息/警告/严重）
- 规则分组管理（按系统分类）

### 数据源类型

**SQL 数据源**
- 单值预警：SQL返回单个值，与阈值比较
- 多行预警：SQL返回需预警的数据，有数据即预警

**API 数据源**
- 请求层判断：判断HTTP状态码
- 响应文本层判断：响应体字符串匹配（包含/不包含/等于/不等于）
- JSON解析层_单值：解析JSON后提取数据与阈值比较
- JSON解析层_多行：解析JSON数组，有数据即预警

### 统计展示
- 表格统计：直接展示查询结果
- 折线图统计：时序数据趋势展示
- 自定义刷新间隔

### 桌面悬浮窗
- 实时展示预警信息
- 实时展示统计数据
- 支持拖拽调整位置
- 支持窗口置顶

## 技术栈

- WPF (.NET 8)
- HandyControl（UI组件库）
- Quartz.NET（定时调度）
- Dapper（数据库访问）
- SQLite（本地配置存储）
- Microsoft.Extensions.Hosting（主机服务）

## 目录结构

```
项目监控/
├── docs/                           # 文档目录
│   └── prd/                        # 产品需求文档
│       └── admin/                  # 管理端产品原型
│       └── PRD.md                  # 产品需求文档
│   └── 技术选型.md                  # 技术选型
├── src/                            # 源代码
│   ├── ProjectAlert.Domain/        # 领域层：实体、枚举、接口
│   ├── ProjectAlert.Repository/    # 数据访问层：SQLite仓储实现
│   ├── ProjectAlert.Shared/        # 共享层：工具类、扩展方法
│   └── ProjectAlert.WPF/           # WPF应用层：视图、视图模型、服务
├── sql/                            # 数据库脚本
└── README.md                       # 项目说明
```

## 核心模块

### ProjectAlert.Domain
- `Entities/` - 实体类（AlertRule、DbConnection、StatConfig等）
- `Enums/` - 枚举定义（SourceType、JudgeType、JudgeOperator等）
- `Interfaces/` - 仓储接口定义

### ProjectAlert.Repository
- SQLite数据库访问实现
- 数据库初始化和迁移

### ProjectAlert.WPF
- `Views/` - XAML视图
- `ViewModels/` - MVVM视图模型
- `Services/` - 业务服务（AlertCheckService、StatExecutionService）

## 文档

- [产品需求文档](docs/prd/PRD.md)
- [技术选型](docs/技术选型.md)
- [管理端原型](docs/prd/admin/index.html)

## 运行说明

```bash
# 构建项目
dotnet build

# 运行应用
dotnet run --project src/ProjectAlert.WPF
```

## 配置说明

应用首次运行会自动创建 SQLite 数据库文件，用于存储：
- 数据库连接配置
- 预警规则配置
- 统计配置
- 预警历史记录

## 数据库初始化

### 自动初始化

应用首次启动时会自动创建数据库和表结构。数据库文件位置：应用程序目录下的 `data.db`

### 手动初始化测试数据

项目提供了 `TestDataInitializer` 类用于初始化测试数据。可以通过以下方式调用：

```csharp
// 在需要的地方注入或创建实例
var context = new SqliteContext();
var initializer = new TestDataInitializer(context);
initializer.Initialize();
```

### 测试数据内容

测试数据初始化器会创建以下内容：

**数据库连接（4条）**
- 本地测试SQLite
- 本地MySQL-药入库
- 本地MySQL-抽奖
- 测试SQLServer（禁用）

**预警规则（10条）**

| 类型 | 名称 | 判断方式 |
|------|------|----------|
| SQL单值 | 待处理订单数量监控 | cnt > 100 |
| SQL单值 | 库存预警 | low_stock > 5 |
| SQL单值 | 今日销售额监控 | total < 10000 |
| SQL多行 | 异常订单检测 | 有数据即预警 |
| SQL多行 | 超时未发货订单 | 有数据即预警 |
| API请求层 | 健康检查-药入库服务 | 请求失败即预警 |
| API请求层 | 健康检查-抽奖服务 | 请求失败即预警 |
| API JSON单值 | API队列积压监控 | pending_count > 1000 |
| API JSON单值 | API响应时间监控 | avg_ms > 500 |

**统计配置（8条）**

| 类型 | 名称 | 图表类型 |
|------|------|----------|
| SQL | 今日订单列表 | 表格 |
| SQL | 库存预警商品 | 表格 |
| SQL | 抽奖活动统计 | 表格 |
| SQL | 近7天销售趋势 | 折线图 |
| SQL | 24小时订单量 | 折线图 |
| API | 服务器监控 | 表格 |
| API | 实时流量监控 | 折线图 |

**当前预警（11条）**
- 包含不同级别（信息/警告/严重）的预警
- 包含不同状态（未处理/已确认/已忽略）的预警

### 清空数据库

如需清空所有数据重新开始，删除 `data.db` 文件后重启应用即可自动重建空数据库。

### 数据库表结构

| 表名 | 说明 |
|------|------|
| db_connections | 数据库连接配置 |
| alert_rules | 预警规则配置 |
| stat_configs | 统计配置 |
| current_alerts | 当前预警记录 |
| ignored_alerts | 已忽略的预警 |
| app_settings | 应用设置 |
| floating_window_states | 浮窗状态 |
