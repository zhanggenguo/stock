-- V001 初始化：公司基本信息库（第一阶段）
-- 版本脚本一旦应用，禁止修改；后续变更请新增 V002__*.sql
-- 拆分规则：每条语句以分号结尾，不支持存储过程内的分号

CREATE TABLE IF NOT EXISTS company (
  company_id       VARCHAR(16) PRIMARY KEY COMMENT 'SH600519/SZ000001/BJ920045',
  code             VARCHAR(6) NOT NULL,
  exchange         VARCHAR(8) NOT NULL,
  board            VARCHAR(32),
  name             VARCHAR(128) NOT NULL,
  name_full        VARCHAR(256),
  name_en          VARCHAR(256),
  former_names     VARCHAR(512),
  list_date        VARCHAR(10),
  found_date       VARCHAR(10),
  industry_em      VARCHAR(128),
  industry_csrc    VARCHAR(160),
  province         VARCHAR(32),
  legal_person     VARCHAR(64),
  chairman         VARCHAR(64),
  president        VARCHAR(64),
  secretary        VARCHAR(64),
  actual_controller VARCHAR(256),
  reg_capital_wan   DECIMAL(20,4),
  emp_num          INT,
  auditor          VARCHAR(256),
  law_firm         VARCHAR(256),
  website          VARCHAR(256),
  tel              VARCHAR(128),
  email            VARCHAR(128),
  address          TEXT,
  reg_address      TEXT,
  issue_price      DECIMAL(20,4),
  par_value        DECIMAL(20,4),
  total_issue_num  DECIMAL(24,4),
  profile          LONGTEXT,
  business_scope   LONGTEXT,
  source_url       VARCHAR(1024),
  fetched_at       VARCHAR(32) NOT NULL,
  raw_hash         VARCHAR(32),
  INDEX idx_company_code (code),
  INDEX idx_company_exchange (exchange),
  INDEX idx_company_industry (industry_em)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='上市公司基本信息';

CREATE TABLE IF NOT EXISTS fetch_log (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  task VARCHAR(64) NOT NULL,
  source VARCHAR(64) NOT NULL,
  fetched_at VARCHAR(32) NOT NULL,
  total INT NOT NULL,
  ok INT NOT NULL,
  fail INT NOT NULL,
  note TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='采集任务日志';
