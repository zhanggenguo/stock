-- V003 行业维度：EM2016 三级行业树与公司映射
-- 口径：仅 EM，scheme 字段预留供后续 CSRC / SW / CUSTOM 扩展
-- 已验证 5108 条 industry_em 均为 3 级且恰好 2 个半角连字符，无行业名内部连字符
-- 拆分规则：每条语句以分号结尾，注释独立成行，SQL 中不得出现字面分号

CREATE TABLE IF NOT EXISTS dim_industry (
  industry_id VARCHAR(255) NOT NULL PRIMARY KEY COMMENT '稳定路径ID 如 EM/金融/银行/股份制与城商行',
  scheme      VARCHAR(16)  NOT NULL COMMENT '分类方案 本期仅 EM',
  level       TINYINT      NOT NULL COMMENT '层级 1/2/3',
  parent_id   VARCHAR(255) NULL COMMENT '父节点ID 一级为 NULL',
  name        VARCHAR(128) NOT NULL COMMENT '本级名称',
  path        VARCHAR(512) NOT NULL COMMENT '完整层级串 便于前缀匹配',
  INDEX idx_dim_industry_scheme_level (scheme, level),
  INDEX idx_dim_industry_parent (parent_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='行业维度表';

CREATE TABLE IF NOT EXISTS company_industry_map (
  company_id     VARCHAR(16)  NOT NULL COMMENT '公司ID',
  industry_id    VARCHAR(255) NOT NULL COMMENT '挂三级叶子节点',
  scheme         VARCHAR(16)  NOT NULL COMMENT '分类方案',
  is_primary     TINYINT      NOT NULL DEFAULT 1 COMMENT '主行业标识 预留多口径',
  effective_date DATE         NULL COMMENT '生效日期 预留',
  source         VARCHAR(64)  NOT NULL DEFAULT 'EM_F10/EM2016' COMMENT '数据来源 预留',
  PRIMARY KEY (company_id, industry_id, scheme),
  INDEX idx_cim_industry (industry_id),
  INDEX idx_cim_scheme (scheme)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='公司行业映射表';

-- 幂等保护：仅清理 EM 口径 不影响未来其他 scheme
DELETE FROM company_industry_map WHERE scheme = 'EM';

DELETE FROM dim_industry WHERE scheme = 'EM';

-- 一级行业
INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
SELECT DISTINCT CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1)), 'EM', 1, NULL,
       SUBSTRING_INDEX(industry_em, '-', 1), SUBSTRING_INDEX(industry_em, '-', 1)
FROM company
WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%';

-- 二级行业
INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
SELECT DISTINCT
       CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/', SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1)),
       'EM', 2,
       CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1)),
       SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1),
       SUBSTRING_INDEX(industry_em, '-', 2)
FROM company
WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%';

-- 三级行业
INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
SELECT DISTINCT
       CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
              SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1), '/',
              SUBSTRING_INDEX(industry_em, '-', -1)),
       'EM', 3,
       CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
              SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1)),
       SUBSTRING_INDEX(industry_em, '-', -1),
       industry_em
FROM company
WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%';

-- 公司到三级叶子的映射
INSERT INTO company_industry_map (company_id, industry_id, scheme, is_primary, source)
SELECT company_id,
       CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
              SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1), '/',
              SUBSTRING_INDEX(industry_em, '-', -1)),
       'EM', 1, 'EM_F10/EM2016'
FROM company
WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%';
