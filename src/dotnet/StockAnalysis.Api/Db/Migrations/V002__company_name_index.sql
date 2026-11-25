-- V002 第一阶段优化：为证券简称增加前缀索引，支撑按名称检索
ALTER TABLE company ADD INDEX idx_company_name (name(32));
