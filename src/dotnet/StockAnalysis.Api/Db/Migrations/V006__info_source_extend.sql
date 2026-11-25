-- V006 信息来源目录扩展：容纳《投资研究信息来源网站清单》全部信息
-- 版本脚本一旦应用，禁止修改；后续变更请新增 V007__*.sql
-- 拆分规则：每条语句以分号结尾，不支持存储过程内的分号
--
-- entry_type：site=网站（含“最小可用网站包”）| combo=按研究方向组合 |
--             template=信息记录模板字段 | note=方法论/流程/原则正文段落
-- url 改为可空：combo/template/note 三类条目没有网页地址。
-- 唯一键 (category, entry_type, sort_order) 作为文档导入的幂等键：
--   同一章节同一类型同一顺序号的条目重复导入时 UPSERT，不产生重复行。

ALTER TABLE info_source
  ADD COLUMN category   VARCHAR(100) NOT NULL DEFAULT '' COMMENT '所属分类（清单章节名）' AFTER site_name,
  ADD COLUMN entry_type VARCHAR(16)  NOT NULL DEFAULT 'site' COMMENT '条目类型：site/combo/template/note' AFTER category,
  ADD COLUMN key_content TEXT NULL COMMENT '重点查询内容/组合来源/记录目的' AFTER description,
  ADD COLUMN sort_order INT NOT NULL DEFAULT 0 COMMENT '分类内排序号，导入幂等键之一' AFTER key_content,
  MODIFY COLUMN url VARCHAR(1024) NULL COMMENT '网页地址（非网站条目可为空）';

-- 存量手工条目：用 id 作为顺序号，避免唯一键冲突
UPDATE info_source SET sort_order = id WHERE sort_order = 0;

ALTER TABLE info_source
  ADD UNIQUE KEY uq_info_source_entry (category, entry_type, sort_order);
