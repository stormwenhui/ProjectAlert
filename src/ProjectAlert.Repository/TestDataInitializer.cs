using Dapper;

namespace ProjectAlert.Repository;

/// <summary>
/// 测试数据初始化器
/// </summary>
public class TestDataInitializer
{
    private readonly SqliteContext _context;

    public TestDataInitializer(SqliteContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    public void Initialize()
    {
        using var connection = _context.CreateConnection();
        connection.Open();

        // 禁用外键约束
        connection.Execute("PRAGMA foreign_keys = OFF");

        // 清空现有数据
        connection.Execute("DELETE FROM current_alerts");
        connection.Execute("DELETE FROM ignored_alerts");
        connection.Execute("DELETE FROM alert_rules");
        connection.Execute("DELETE FROM stat_configs");
        connection.Execute("DELETE FROM db_connections");
        connection.Execute("DELETE FROM app_settings");

        // 重置自增序列
        connection.Execute("DELETE FROM sqlite_sequence WHERE name IN ('db_connections', 'alert_rules', 'stat_configs', 'current_alerts', 'ignored_alerts')");

        // 重新启用外键约束
        connection.Execute("PRAGMA foreign_keys = ON");

        // 插入数据库连接
        InsertDbConnections(connection);

        // 插入预警规则
        InsertAlertRules(connection);

        // 插入统计配置
        InsertStatConfigs(connection);

        // 插入当前预警
        InsertCurrentAlerts(connection);

        // 插入已忽略预警
        InsertIgnoredAlerts(connection);

        // 插入应用设置
        InsertAppSettings(connection);
    }

    private void InsertDbConnections(System.Data.IDbConnection connection)
    {
        var sql = @"INSERT INTO db_connections (name, db_type, connection_string, enabled) VALUES (@Name, @DbType, @ConnectionString, @Enabled)";

        connection.Execute(sql, new { Name = "本地测试SQLite", DbType = 2, ConnectionString = @"Data Source=./data.db", Enabled = 1 });
        connection.Execute(sql, new { Name = "本地MySQL", DbType = 0, ConnectionString = "Server=localhost;Port=3306;Database=mydb;User=root;Password=yourpassword;", Enabled = 1 });
        connection.Execute(sql, new { Name = "本地MySQL-备用", DbType = 0, ConnectionString = "Server=localhost;Port=3306;Database=mydb2;User=root;Password=yourpassword;", Enabled = 1 });
        connection.Execute(sql, new { Name = "测试SQLServer", DbType = 1, ConnectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=yourpassword;TrustServerCertificate=True;", Enabled = 0 });
    }

    private void InsertAlertRules(System.Data.IDbConnection connection)
    {
        // SQL 单值预警
        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, db_connection_id, sql_query, judge_type, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('待处理订单数量监控', 1, 0, 2, 'SELECT COUNT(*) as cnt FROM orders WHERE status = 0', 1, 'cnt', 1, '100', 2, '待处理订单数量已达 {cnt} 个，请及时处理！', '0 */5 * * * ?', 1, 1)");

        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, db_connection_id, sql_query, judge_type, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('库存预警', 1, 0, 2, 'SELECT COUNT(*) as low_stock FROM products WHERE stock < 10', 1, 'low_stock', 1, '5', 3, '有 {low_stock} 个商品库存不足，请及时补货！', '0 0 */1 * * ?', 2, 1)");

        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, db_connection_id, sql_query, judge_type, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('今日销售额监控', 3, 0, 2, 'SELECT SUM(amount) as total FROM sales WHERE DATE(create_time) = CURDATE()', 1, 'total', 3, '10000', 1, '今日销售额 {total} 元，低于目标值', '0 0 18 * * ?', 1, 1)");

        // SQL 多行预警
        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, db_connection_id, sql_query, judge_type, key_field, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('异常订单检测', 1, 0, 2, 'SELECT order_id, customer_name, amount FROM orders WHERE amount > 50000 AND status = 1', 2, 'order_id', 'amount', 1, '50000', 3, '订单 {order_id} 金额异常：{amount}元，客户：{customer_name}', '0 */10 * * * ?', 1, 1)");

        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, db_connection_id, sql_query, judge_type, key_field, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('超时未发货订单', 3, 0, 2, 'SELECT order_id, create_time FROM orders WHERE status = 1 AND DATEDIFF(NOW(), create_time) > 3', 2, 'order_id', 2, '订单 {order_id} 已超过3天未发货，创建时间：{create_time}', '0 0 9 * * ?', 1, 1)");

        // API 请求层判断
        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, api_url, api_method, api_timeout, judge_type, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('健康检查-药入库服务', 1, 1, 'http://localhost:8080/health', 'GET', 10, 3, 3, '药入库服务健康检查失败！', '0 */1 * * * ?', 3, 1)");

        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, api_url, api_method, api_headers, api_timeout, judge_type, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('健康检查-抽奖服务', 2, 1, 'http://localhost:8081/api/health', 'GET', '{""Authorization"": ""Bearer your_token""}', 10, 3, 3, '抽奖服务不可用！', '0 */1 * * * ?', 3, 1)");

        // API JSON 解析层
        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, api_url, api_method, api_timeout, data_path, judge_type, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('API队列积压监控', 4, 1, 'http://localhost:8082/api/queue/status', 'GET', 30, 'data', 5, 'pending_count', 1, '1000', 2, '消息队列积压 {pending_count} 条，请检查消费者状态', '0 */5 * * * ?', 2, 1)");

        connection.Execute(@"
            INSERT INTO alert_rules (name, category, source_type, api_url, api_method, api_timeout, data_path, judge_type, judge_field, judge_operator, judge_value, alert_level, message_template, cron_expression, fail_threshold, enabled)
            VALUES ('API响应时间监控', 4, 1, 'http://localhost:8082/api/metrics', 'GET', 30, 'data.response_time', 5, 'avg_ms', 1, '500', 2, 'API平均响应时间 {avg_ms}ms 超过阈值', '0 */5 * * * ?', 1, 1)");
    }

    private void InsertStatConfigs(System.Data.IDbConnection connection)
    {
        // 表格类型
        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, db_connection_id, sql_query, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('今日订单列表', 1, 0, 2, 'SELECT order_id as 订单号, customer_name as 客户, amount as 金额, status as 状态, create_time as 创建时间 FROM orders WHERE DATE(create_time) = CURDATE() ORDER BY create_time DESC LIMIT 20', 1, '{""columns"": [{""field"": ""订单号"", ""width"": 120}, {""field"": ""客户"", ""width"": 100}, {""field"": ""金额"", ""width"": 80}, {""field"": ""状态"", ""width"": 60}, {""field"": ""创建时间"", ""width"": 150}]}', 60, 1, 1)");

        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, db_connection_id, sql_query, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('库存预警商品', 1, 0, 2, 'SELECT product_name as 商品名, stock as 库存, min_stock as 最低库存 FROM products WHERE stock < min_stock * 1.5 ORDER BY stock ASC LIMIT 10', 1, '{""columns"": [{""field"": ""商品名"", ""width"": 200}, {""field"": ""库存"", ""width"": 80}, {""field"": ""最低库存"", ""width"": 80}]}', 300, 2, 1)");

        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, db_connection_id, sql_query, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('抽奖活动统计', 2, 0, 3, 'SELECT activity_name as 活动, total_count as 参与人数, win_count as 中奖人数, create_time as 开始时间 FROM activities WHERE status = 1 ORDER BY create_time DESC', 1, '{""columns"": [{""field"": ""活动"", ""width"": 150}, {""field"": ""参与人数"", ""width"": 100}, {""field"": ""中奖人数"", ""width"": 100}, {""field"": ""开始时间"", ""width"": 150}]}', 120, 1, 1)");

        // 折线图类型
        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, db_connection_id, sql_query, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('近7天销售趋势', 3, 0, 2, 'SELECT DATE(create_time) as date, SUM(amount) as sales, COUNT(*) as orders FROM sales WHERE create_time >= DATE_SUB(NOW(), INTERVAL 7 DAY) GROUP BY DATE(create_time) ORDER BY date', 2, '{""xField"": ""date"", ""yFields"": [""sales"", ""orders""], ""yLabels"": [""销售额"", ""订单数""]}', 300, 1, 1)");

        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, db_connection_id, sql_query, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('24小时订单量', 1, 0, 2, 'SELECT DATE_FORMAT(create_time, ''%H:00'') as hour, COUNT(*) as count FROM orders WHERE create_time >= DATE_SUB(NOW(), INTERVAL 24 HOUR) GROUP BY HOUR(create_time) ORDER BY hour', 2, '{""xField"": ""hour"", ""yFields"": [""count""], ""yLabels"": [""订单量""]}', 60, 3, 1)");

        // API 类型
        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, api_url, api_method, api_timeout, data_path, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('服务器监控', 4, 1, 'http://localhost:8082/api/server/status', 'GET', 30, 'data.servers', 1, '{""columns"": [{""field"": ""name"", ""title"": ""服务器"", ""width"": 120}, {""field"": ""cpu"", ""title"": ""CPU%"", ""width"": 80}, {""field"": ""memory"", ""title"": ""内存%"", ""width"": 80}, {""field"": ""status"", ""title"": ""状态"", ""width"": 60}]}', 30, 1, 1)");

        connection.Execute(@"
            INSERT INTO stat_configs (name, category, source_type, api_url, api_method, api_timeout, data_path, chart_type, chart_config, refresh_interval, sort_order, enabled)
            VALUES ('实时流量监控', 4, 1, 'http://localhost:8082/api/metrics/traffic', 'GET', 30, 'data.history', 2, '{""xField"": ""time"", ""yFields"": [""qps"", ""error_rate""], ""yLabels"": [""QPS"", ""错误率%""]}', 10, 2, 1)");
    }

    private void InsertCurrentAlerts(System.Data.IDbConnection connection)
    {
        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (1, NULL, '待处理订单数量已达 156 个，请及时处理！', 2, 0, datetime('now', '-2 hours'), datetime('now', '-5 minutes'), 8)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (2, NULL, '有 12 个商品库存不足，请及时补货！', 3, 0, datetime('now', '-1 day'), datetime('now', '-30 minutes'), 15)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (4, 'ORD20231226001', '订单 ORD20231226001 金额异常：68000元，客户：张三', 3, 0, datetime('now', '-1 hour'), datetime('now', '-1 hour'), 1)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (4, 'ORD20231226002', '订单 ORD20231226002 金额异常：52000元，客户：李四', 3, 1, datetime('now', '-45 minutes'), datetime('now', '-45 minutes'), 1)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (7, NULL, '药入库服务健康检查失败！', 3, 0, datetime('now', '-10 minutes'), datetime('now', '-2 minutes'), 5)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (8, NULL, '抽奖服务不可用！', 3, 2, datetime('now', '-3 hours'), datetime('now', '-1 hour'), 12)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (3, NULL, '今日销售额 8520 元，低于目标值', 1, 0, datetime('now', '-6 hours'), datetime('now', '-6 hours'), 1)");

        // 警告级别预警
        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (8, NULL, '消息队列积压 1520 条，请检查消费者状态', 2, 0, datetime('now', '-30 minutes'), datetime('now', '-10 minutes'), 3)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (9, NULL, 'API平均响应时间 680ms 超过阈值', 2, 0, datetime('now', '-1 hour'), datetime('now', '-15 minutes'), 5)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (5, 'ORD20231223015', '订单 ORD20231223015 已超过3天未发货，创建时间：2023-12-23 10:30:00', 2, 0, datetime('now', '-4 hours'), datetime('now', '-4 hours'), 1)");

        connection.Execute(@"
            INSERT INTO current_alerts (rule_id, alert_key, message, alert_level, status, first_time, last_time, occur_count)
            VALUES (5, 'ORD20231222008', '订单 ORD20231222008 已超过3天未发货，创建时间：2023-12-22 14:20:00', 2, 1, datetime('now', '-5 hours'), datetime('now', '-5 hours'), 1)");
    }

    private void InsertIgnoredAlerts(System.Data.IDbConnection connection)
    {
        connection.Execute(@"INSERT INTO ignored_alerts (rule_id, alert_key, ignored_at) VALUES (4, 'ORD20231220001', datetime('now', '-5 days'))");
        connection.Execute(@"INSERT INTO ignored_alerts (rule_id, alert_key, ignored_at) VALUES (5, 'ORD20231218003', datetime('now', '-7 days'))");
    }

    private void InsertAppSettings(System.Data.IDbConnection connection)
    {
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('floating_window_opacity', '0.9')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('floating_window_width', '350')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('floating_window_height', '250')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('refresh_interval', '30')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('auto_start', 'false')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('theme', 'light')");
        connection.Execute("INSERT INTO app_settings (key, value) VALUES ('notification_sound', 'true')");
    }
}
