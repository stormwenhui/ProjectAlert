using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ProjectAlert.Repository;

/// <summary>
/// SQLite 数据库上下文
/// </summary>
public class SqliteContext
{
    private readonly string _connectionString;
    private static bool _dapperConfigured;

    public SqliteContext()
    {
        // 配置 Dapper 支持下划线命名映射
        if (!_dapperConfigured)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            _dapperConfigured = true;
        }

        // 数据库文件放在应用程序目录下
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dbPath = Path.Combine(appDir, "data.db");

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    public IDbConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        var sql = GetInitializeSql();
        connection.Execute(sql);
    }

    /// <summary>
    /// 获取数据库初始化 SQL
    /// </summary>
    private static string GetInitializeSql()
    {
        return """
            -- 数据库连接配置
            CREATE TABLE IF NOT EXISTS db_connections (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                db_type INTEGER NOT NULL,
                connection_string TEXT NOT NULL,
                enabled INTEGER DEFAULT 1,
                created_at TEXT DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT DEFAULT (datetime('now', 'localtime'))
            );

            -- 预警规则
            CREATE TABLE IF NOT EXISTS alert_rules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category INTEGER DEFAULT 0,
                source_type INTEGER NOT NULL,

                -- SQL 配置
                db_connection_id INTEGER,
                sql_query TEXT,

                -- API 配置（内联）
                api_url TEXT,
                api_method TEXT,
                api_headers TEXT,
                api_body TEXT,
                api_timeout INTEGER DEFAULT 30,

                -- 判断配置
                judge_level INTEGER,
                data_path TEXT,
                judge_type INTEGER NOT NULL,
                judge_field TEXT,
                judge_operator INTEGER,
                judge_value TEXT,
                key_field TEXT,

                -- 预警配置
                alert_level INTEGER DEFAULT 1,
                message_template TEXT,
                cron_expression TEXT NOT NULL,
                fail_threshold INTEGER DEFAULT 1,
                current_fail_count INTEGER DEFAULT 0,

                -- 状态
                enabled INTEGER DEFAULT 1,
                last_run_time TEXT,
                last_run_success INTEGER,
                last_run_result TEXT,

                created_at TEXT DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT DEFAULT (datetime('now', 'localtime')),

                FOREIGN KEY (db_connection_id) REFERENCES db_connections(id)
            );

            -- 统计配置
            CREATE TABLE IF NOT EXISTS stat_configs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                category INTEGER DEFAULT 0,
                source_type INTEGER NOT NULL,

                -- SQL 配置
                db_connection_id INTEGER,
                sql_query TEXT,

                -- API 配置（内联）
                api_url TEXT,
                api_method TEXT,
                api_headers TEXT,
                api_body TEXT,
                api_timeout INTEGER DEFAULT 30,
                data_path TEXT,

                -- 图表配置
                chart_type INTEGER NOT NULL,
                chart_config TEXT,
                refresh_interval INTEGER DEFAULT 60,
                sort_order INTEGER DEFAULT 0,

                enabled INTEGER DEFAULT 1,
                created_at TEXT DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT DEFAULT (datetime('now', 'localtime')),

                FOREIGN KEY (db_connection_id) REFERENCES db_connections(id)
            );

            -- 当前预警
            CREATE TABLE IF NOT EXISTS current_alerts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rule_id INTEGER NOT NULL,
                alert_key TEXT,
                message TEXT NOT NULL,
                alert_level INTEGER NOT NULL,
                status INTEGER DEFAULT 0,
                first_time TEXT NOT NULL,
                last_time TEXT NOT NULL,
                occur_count INTEGER DEFAULT 1,
                created_at TEXT DEFAULT (datetime('now', 'localtime')),
                updated_at TEXT DEFAULT (datetime('now', 'localtime')),

                FOREIGN KEY (rule_id) REFERENCES alert_rules(id)
            );

            -- 已忽略预警
            CREATE TABLE IF NOT EXISTS ignored_alerts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                rule_id INTEGER NOT NULL,
                alert_key TEXT,
                ignored_at TEXT NOT NULL,

                FOREIGN KEY (rule_id) REFERENCES alert_rules(id)
            );

            -- 应用设置
            CREATE TABLE IF NOT EXISTS app_settings (
                key TEXT PRIMARY KEY,
                value TEXT
            );

            -- 浮窗状态
            CREATE TABLE IF NOT EXISTS floating_window_states (
                window_id TEXT PRIMARY KEY,
                is_visible INTEGER DEFAULT 0,
                left REAL DEFAULT 0,
                top REAL DEFAULT 0,
                width REAL DEFAULT 320,
                height REAL DEFAULT 200,
                opacity REAL DEFAULT 0.7,
                is_locked INTEGER DEFAULT 0,
                is_topmost INTEGER DEFAULT 1,
                updated_at TEXT DEFAULT (datetime('now', 'localtime'))
            );

            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_alert_rules_category ON alert_rules(category);
            CREATE INDEX IF NOT EXISTS idx_alert_rules_enabled ON alert_rules(enabled);
            CREATE INDEX IF NOT EXISTS idx_current_alerts_rule_id ON current_alerts(rule_id);
            CREATE INDEX IF NOT EXISTS idx_current_alerts_status ON current_alerts(status);
            CREATE INDEX IF NOT EXISTS idx_stat_configs_category ON stat_configs(category);
            """;
    }
}
