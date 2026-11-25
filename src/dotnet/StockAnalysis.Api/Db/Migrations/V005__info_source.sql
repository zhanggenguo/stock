-- V005 信息来源目录：网站名称、定位描述、网页地址
-- 版本脚本一旦应用，禁止修改；后续变更请新增 V006__*.sql
-- 拆分规则：每条语句以分号结尾，不支持存储过程内的分号

CREATE TABLE IF NOT EXISTS info_source (
  id          BIGINT AUTO_INCREMENT PRIMARY KEY,
  site_name   VARCHAR(100)  NOT NULL COMMENT '网站名称',
  description VARCHAR(500)  NOT NULL DEFAULT '' COMMENT '网站定位描述',
  url         VARCHAR(1024) NOT NULL COMMENT '网页地址',
  created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX idx_site_name (site_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='信息来源目录';
