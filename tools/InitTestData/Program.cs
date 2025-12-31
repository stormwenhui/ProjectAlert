using Dapper;
using ProjectAlert.Repository;

Console.WriteLine("ProjectAlert 测试数据初始化工具");
Console.WriteLine("================================");

var context = new SqliteContext();

// 检查是否已有数据
using (var conn = context.CreateConnection())
{
    var dbCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM db_connections");
    var ruleCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM alert_rules");
    var statCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM stat_configs");
    var alertCount = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM current_alerts");

    if (dbCount > 0 || ruleCount > 0)
    {
        Console.WriteLine($"当前数据库已有数据:");
        Console.WriteLine($"  - 数据库连接: {dbCount} 条");
        Console.WriteLine($"  - 预警规则: {ruleCount} 条");
        Console.WriteLine($"  - 统计配置: {statCount} 条");
        Console.WriteLine($"  - 当前预警: {alertCount} 条");
        Console.WriteLine();

        if (args.Length == 0 || args[0] != "--force")
        {
            Console.WriteLine("如需重新初始化，请使用 --force 参数运行");
            return;
        }
        Console.WriteLine("使用 --force 参数，将重新初始化数据...");
    }
}

try
{
    var initializer = new TestDataInitializer(context);
    Console.WriteLine("正在初始化测试数据...");
    initializer.Initialize();
    Console.WriteLine("测试数据初始化完成！");
}
catch (Exception ex)
{
    Console.WriteLine($"初始化失败: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return;
}

// 显示统计
using (var conn = context.CreateConnection())
{
    Console.WriteLine();
    Console.WriteLine("=== 数据库连接 ===");
    var dbs = conn.Query("SELECT id, name, db_type FROM db_connections");
    foreach (var db in dbs)
    {
        var typeName = db.db_type switch { 0 => "MySQL", 1 => "SqlServer", 2 => "SQLite", _ => "未知" };
        Console.WriteLine($"  [{db.id}] {db.name} ({typeName})");
    }

    Console.WriteLine();
    Console.WriteLine("=== 预警规则 ===");
    var rules = conn.Query("SELECT id, name, category, source_type FROM alert_rules");
    foreach (var rule in rules)
    {
        var cat = rule.category switch { 1 => "药入库", 2 => "抽奖", 3 => "采销", 4 => "电销", _ => "未知" };
        var src = rule.source_type == 0 ? "SQL" : "API";
        Console.WriteLine($"  [{rule.id}] {rule.name} ({cat}/{src})");
    }

    Console.WriteLine();
    Console.WriteLine("=== 统计配置 ===");
    var stats = conn.Query("SELECT id, name, chart_type FROM stat_configs");
    foreach (var stat in stats)
    {
        var chart = stat.chart_type == 1 ? "表格" : "折线图";
        Console.WriteLine($"  [{stat.id}] {stat.name} ({chart})");
    }

    Console.WriteLine();
    Console.WriteLine("=== 当前预警 ===");
    var alerts = conn.Query("SELECT id, message, alert_level, status FROM current_alerts");
    foreach (var alert in alerts)
    {
        var level = alert.alert_level switch { 1 => "信息", 2 => "警告", 3 => "严重", _ => "未知" };
        var status = alert.status switch { 0 => "待处理", 1 => "处理中", 2 => "已忽略", 3 => "已恢复", _ => "未知" };
        var msg = alert.message.Length > 35 ? alert.message.Substring(0, 35) + "..." : alert.message;
        Console.WriteLine($"  [{alert.id}] [{level}][{status}] {msg}");
    }
}
