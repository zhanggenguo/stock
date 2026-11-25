-- V004 第二阶段：公司日市值与估值快照
-- 版本脚本一旦应用，禁止修改；后续变更请新增 V005__*.sql
-- 拆分规则：每条语句以分号结尾，不支持存储过程内的分号

CREATE TABLE IF NOT EXISTS fact_market_cap_daily (
  id                BIGINT AUTO_INCREMENT PRIMARY KEY,
  company_id        VARCHAR(16) NOT NULL COMMENT '关联 company.company_id',
  trade_date        DATE NOT NULL COMMENT '交易日',
  exchange          VARCHAR(8) NOT NULL DEFAULT '',

  close             DECIMAL(12,4)  NOT NULL COMMENT '未复权收盘价(元)',
  pct_change        DECIMAL(10,4)  DEFAULT NULL COMMENT '涨跌幅(%)',
  amplitude         DECIMAL(10,4)  DEFAULT NULL COMMENT '振幅(%)',
  volume            DECIMAL(20,2)  DEFAULT NULL COMMENT '成交量(手)',
  amount            DECIMAL(20,2)  DEFAULT NULL COMMENT '成交额(元)',
  turnover_rate     DECIMAL(10,4)  DEFAULT NULL COMMENT '换手率(%)',

  total_shares      DECIMAL(24,2)  DEFAULT NULL COMMENT '总股本(股)',
  float_shares      DECIMAL(24,2)  DEFAULT NULL COMMENT '流通股本(股)',
  total_market_cap  DECIMAL(20,2)  NOT NULL COMMENT '总市值(元)',
  float_market_cap  DECIMAL(20,2)  DEFAULT NULL COMMENT '流通市值(元)',

  pe_ttm            DECIMAL(12,4)  DEFAULT NULL COMMENT 'PE(TTM)',
  pe_static         DECIMAL(12,4)  DEFAULT NULL COMMENT 'PE(静态)',
  pe_dynamic        DECIMAL(12,4)  DEFAULT NULL COMMENT 'PE(动态)',
  pb                DECIMAL(12,4)  DEFAULT NULL COMMENT '市净率',
  ps_ttm            DECIMAL(12,4)  DEFAULT NULL COMMENT '市销率(TTM)，接口暂无则留空',
  dividend_yield    DECIMAL(10,4)  DEFAULT NULL COMMENT '股息率(%)，接口暂无则留空',

  is_suspended      TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否停牌',
  shares_asof       DATE           DEFAULT NULL COMMENT '股本口径日期；回算值时必填',
  quality_flag      VARCHAR(32)    NOT NULL DEFAULT '' COMMENT '数据质量标记',
  source            VARCHAR(32)    NOT NULL DEFAULT 'EASTMONEY',
  source_url        VARCHAR(1024)  DEFAULT NULL,
  raw_hash          VARCHAR(64)    DEFAULT NULL,
  fetched_at        VARCHAR(32)    NOT NULL,
  created_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  UNIQUE KEY uk_company_date (company_id, trade_date),
  INDEX idx_trade_date (trade_date),
  INDEX idx_exchange_date (exchange, trade_date),
  INDEX idx_market_cap (total_market_cap DESC, trade_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='公司日市值与估值快照';
