# CLAUDE.md - 项目预警系统开发指南

## 项目概述

项目预警（ProjectAlert）是一个多数据源监控告警系统，支持 MySQL、SQL Server 和 API 数据源的定时查询监控，通过飞书、企业微信、钉钉等渠道发送告警通知。

## 开发命令

### 后端 API (.NET 8)

```bash
# 还原依赖
dotnet restore src/ProjectAlert.sln

# 构建解决方案
dotnet build src/ProjectAlert.sln

# 运行 API 服务 (默认端口 5000)
dotnet run --project src/ProjectAlert.Api

# 发布 API
dotnet publish src/ProjectAlert.Api -c Release -o publish/api
```

### WPF 桌面客户端

```bash
# 运行 WPF 客户端
dotnet run --project src/ProjectAlert.WPF

# 发布 WPF (自包含)
dotnet publish src/ProjectAlert.WPF -c Release -r win-x64 --self-contained -o publish/wpf
```

### 前端 Web 管理界面 (Vue 3)

```bash
cd web

# 安装依赖
npm install

# 开发模式 (端口 5173)
npm run dev

# 类型检查
npm run type-check

# 构建生产版本
npm run build
```

### Docker 部署

```bash
# 构建并启动所有服务
docker-compose up -d --build

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

## 项目架构

### 解决方案结构 (DDD 分层)

```
src/
├── ProjectAlert.Api/        # WebAPI 控制器层，依赖 UseCase
├── ProjectAlert.Domain/     # 领域层：实体、枚举、仓储接口
├── ProjectAlert.Dto/        # 数据传输对象：Request/Response DTO
├── ProjectAlert.Query/      # 查询层：Dapper 仓储实现
├── ProjectAlert.UseCase/    # 用例层：业务逻辑、Quartz 调度任务
├── ProjectAlert.Shared/     # 共享层：工具类、ApiResult
└── ProjectAlert.WPF/        # WPF 桌面客户端 (MVVM)
```

### 层级依赖关系

- **Api** → UseCase, Dto, Shared
- **UseCase** → Domain, Query, Dto, Shared
- **Query** → Domain, Shared
- **Domain** → 无依赖（纯领域模型）
- **Dto** → 无依赖
- **Shared** → 无依赖
- **WPF** → Shared（独立客户端）

### 核心技术栈

| 组件 | 技术 |
|------|------|
| 后端框架 | .NET 8 + ASP.NET Core |
| ORM | Dapper |
| 任务调度 | Quartz.NET |
| 数据库 | MySQL 8.0+ |
| 前端框架 | Vue 3 + TypeScript + Element Plus |
| WPF 模式 | CommunityToolkit.Mvvm |
| 容器化 | Docker Compose |

## 关键配置

### appsettings.json (API)

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=project_alert;Uid=root;Pwd=yourpassword;"
  },
  "ApiSettings": {
    "WpfApiKey": "your-api-key"
  }
}
```

### 环境变量 (Docker)

- `MYSQL_ROOT_PASSWORD` - MySQL root 密码（请自行设置）
- `ConnectionStrings__Default` - 数据库连接串（请自行配置）

## 数据库表结构

主要表：
- `systems` - 业务系统分类
- `data_sources` - 数据源配置（MySQL/SqlServer/API）
- `alert_rules` - 告警规则（SQL/API 查询 + Cron 表达式）
- `stat_configs` - 统计图表配置
- `settings` - 系统设置（Webhook 地址等）
- `ignored_alerts` - 已忽略的告警

## 代码规范

- 所有代码注释使用中文
- 实体类继承 `BaseEntity`，包含 `Id`、`CreatedAt`、`UpdatedAt`
- API 返回统一使用 `ApiResult<T>` 包装
- 仓储接口定义在 Domain 层，实现在 Query 层
- 前端 API 调用集中在 `web/src/api/index.ts`

## 调试端口

| 服务 | 端口 |
|------|------|
| API | 5000 |
| Vue Dev | 5173 |
| MySQL | 3306 |
| Nginx (Docker) | 80 |
