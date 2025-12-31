-- ProjectAlert 数据库初始化脚本
-- 创建数据库
CREATE DATABASE IF NOT EXISTS projectalert
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE projectalert;

-- =====================================================
-- 系统管理表
-- =====================================================
CREATE TABLE IF NOT EXISTS systems (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL COMMENT '系统名称',
    sort_order INT DEFAULT 0 COMMENT '排序',
    enabled TINYINT(1) DEFAULT 1 COMMENT '是否启用',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='系统管理表';

-- =====================================================
-- 数据源配置表
-- =====================================================
CREATE TABLE IF NOT EXISTS data_sources (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL COMMENT '数据源名称',
    type ENUM('mysql', 'sqlserver', 'api') NOT NULL COMMENT '数据源类型',
    -- 数据库连接配置
    connection_string TEXT COMMENT '数据库连接串',
    -- API 配置
    api_url VARCHAR(500) COMMENT 'API地址',
    api_method VARCHAR(10) DEFAULT 'GET' COMMENT 'HTTP方法',
    api_headers TEXT COMMENT '请求头JSON',
    api_body TEXT COMMENT '请求体',
    -- 通用配置
    timeout INT DEFAULT 30 COMMENT '超时秒数',
    enabled TINYINT(1) DEFAULT 1 COMMENT '是否启用',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='数据源配置表';

-- =====================================================
-- 预警规则表
-- =====================================================
CREATE TABLE IF NOT EXISTS alert_rules (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL COMMENT '规则名称',
    system_id INT COMMENT '所属系统ID',
    data_source_id INT NOT NULL COMMENT '数据源ID',

    -- 查询内容
    query_content TEXT NOT NULL COMMENT 'SQL语句或API配置',

    -- 判断配置
    -- SQL: single_value(单值预警), multi_row(多行预警)
    -- API: request(请求层), response_text(响应文本层), json(JSON解析层)
    judge_type VARCHAR(30) NOT NULL COMMENT '判断方式',

    -- API JSON解析配置
    data_path VARCHAR(200) COMMENT 'JSON数据路径 (如: data.items)',

    -- 判断条件
    judge_field VARCHAR(50) COMMENT '判断字段名',
    judge_operator VARCHAR(20) COMMENT '比较运算符: >, >=, <, <=, =, !=, contains, not_contains',
    judge_value VARCHAR(200) COMMENT '比较值/阈值',

    -- 多行预警配置
    key_field VARCHAR(50) COMMENT '唯一标识字段名（多行预警用于区分）',

    -- 预警配置
    alert_level ENUM('info', 'warning', 'critical') DEFAULT 'warning' COMMENT '预警级别',
    alert_message_template VARCHAR(500) COMMENT '预警消息模板，支持变量如 {$name}, {$table.field}',

    -- 调度配置
    cron_expression VARCHAR(100) NOT NULL COMMENT 'Cron表达式',

    -- 连续失败配置
    fail_threshold INT DEFAULT 1 COMMENT '连续失败N次才预警',
    current_fail_count INT DEFAULT 0 COMMENT '当前连续失败次数',

    -- 状态信息
    enabled TINYINT(1) DEFAULT 1 COMMENT '是否启用',
    last_run_time DATETIME COMMENT '上次执行时间',
    last_run_success TINYINT(1) COMMENT '上次执行是否成功',
    last_run_result TEXT COMMENT '上次执行结果',
    last_alert_time DATETIME COMMENT '上次预警时间',

    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',

    INDEX idx_system_id (system_id),
    INDEX idx_data_source_id (data_source_id),
    INDEX idx_enabled (enabled),
    FOREIGN KEY (system_id) REFERENCES systems(id) ON DELETE SET NULL,
    FOREIGN KEY (data_source_id) REFERENCES data_sources(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='预警规则表';

-- =====================================================
-- 当前预警状态表（运行时状态）
-- =====================================================
CREATE TABLE IF NOT EXISTS current_alerts (
    id INT PRIMARY KEY AUTO_INCREMENT,
    rule_id INT NOT NULL COMMENT '关联规则ID',
    alert_key VARCHAR(200) COMMENT '预警唯一标识（多行预警时为key_field的值）',
    alert_level ENUM('info', 'warning', 'critical') NOT NULL COMMENT '预警级别',
    status ENUM('pending', 'processing', 'ignored', 'recovered') DEFAULT 'pending' COMMENT '处理状态',
    alert_message VARCHAR(500) COMMENT '预警消息（模板渲染后）',

    -- 时间统计
    first_trigger_time DATETIME NOT NULL COMMENT '首次触发时间',
    last_trigger_time DATETIME NOT NULL COMMENT '最后异常时间',
    trigger_count INT DEFAULT 1 COMMENT '累计异常次数',

    -- 数据快照
    last_value TEXT COMMENT '最后一次的检测值/行数据JSON',

    -- 状态变更
    status_changed_at DATETIME COMMENT '状态变更时间',
    status_changed_by VARCHAR(50) COMMENT '状态变更来源（user/system）',

    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',

    UNIQUE KEY uk_rule_key (rule_id, alert_key),
    INDEX idx_status (status),
    INDEX idx_alert_level (alert_level),
    FOREIGN KEY (rule_id) REFERENCES alert_rules(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='当前预警状态表';

-- =====================================================
-- 忽略规则表（记录被忽略的预警key）
-- =====================================================
CREATE TABLE IF NOT EXISTS ignored_alerts (
    id INT PRIMARY KEY AUTO_INCREMENT,
    rule_id INT NOT NULL COMMENT '规则ID',
    alert_key VARCHAR(200) COMMENT '被忽略的预警key',
    ignored_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '忽略时间',
    ignored_reason VARCHAR(200) COMMENT '忽略原因（可选）',

    UNIQUE KEY uk_rule_key (rule_id, alert_key),
    FOREIGN KEY (rule_id) REFERENCES alert_rules(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='忽略规则表';

-- =====================================================
-- 统计配置表
-- =====================================================
CREATE TABLE IF NOT EXISTS stat_configs (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL COMMENT '统计名称',
    system_id INT COMMENT '所属系统',
    data_source_id INT NOT NULL COMMENT '数据源ID',

    -- 查询配置
    query_content TEXT NOT NULL COMMENT 'SQL语句或API配置',
    data_path VARCHAR(200) COMMENT 'API数据路径',

    -- 图表类型
    chart_type ENUM('table', 'line') DEFAULT 'table' COMMENT '图表类型',

    -- 表格配置
    display_columns TEXT COMMENT '显示列配置JSON [{field, title, width}]',
    max_rows INT DEFAULT 20 COMMENT '最大行数',

    -- 折线图配置
    x_field VARCHAR(50) COMMENT 'X轴字段',
    y_fields VARCHAR(200) COMMENT 'Y轴字段（逗号分隔）',
    y_labels VARCHAR(200) COMMENT 'Y轴图例名称（逗号分隔）',
    max_points INT DEFAULT 30 COMMENT '最大数据点数',

    -- 通用配置
    refresh_interval INT DEFAULT 60 COMMENT '刷新间隔（秒）',
    sort_order INT DEFAULT 0 COMMENT '排序',
    enabled TINYINT(1) DEFAULT 1 COMMENT '是否启用',

    -- 状态信息
    last_run_time DATETIME COMMENT '上次执行时间',
    last_run_result TEXT COMMENT '上次执行结果JSON',

    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',

    INDEX idx_system_id (system_id),
    INDEX idx_enabled (enabled),
    FOREIGN KEY (system_id) REFERENCES systems(id) ON DELETE SET NULL,
    FOREIGN KEY (data_source_id) REFERENCES data_sources(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='统计配置表';

-- =====================================================
-- 应用设置表
-- =====================================================
CREATE TABLE IF NOT EXISTS app_settings (
    key_name VARCHAR(100) PRIMARY KEY COMMENT '配置键',
    value TEXT COMMENT '配置值',
    description VARCHAR(500) COMMENT '配置描述',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='应用设置表';

-- =====================================================
-- 初始化数据
-- =====================================================

-- 默认系统设置
INSERT INTO app_settings (key_name, value, description) VALUES
('floating_window_opacity', '0.85', '悬浮窗透明度'),
('floating_window_width', '300', '悬浮窗默认宽度'),
('floating_window_height', '200', '悬浮窗默认高度'),
('refresh_interval', '30', '数据刷新间隔（秒）'),
('api_base_url', 'http://localhost:5000', 'API服务地址'),
('auto_start', 'false', '是否开机自启动')
ON DUPLICATE KEY UPDATE updated_at = CURRENT_TIMESTAMP;

-- 示例系统
INSERT INTO systems (name, sort_order, enabled) VALUES
('订单中心', 1, 1),
('支付中心', 2, 1),
('用户中心', 3, 1)
ON DUPLICATE KEY UPDATE updated_at = CURRENT_TIMESTAMP;
