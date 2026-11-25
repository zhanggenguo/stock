# 信息来源目录（数据库 + API + WinForms + 文档导入）

状态：**已实施**（2026-11-25；V006 文档导入改造同日完成）。本文记录表结构、接口契约、界面位置与验收证据。

## 1. 需求

1. 客户端主界面右上角，从右向左逐步增加工具栏。第一个按钮是「信息来源」：点击弹窗展示清单，支持添加、删除、更新。
2. 按《投资研究信息来源网站清单_2026-11-1.docx》对模块改造：**容纳文档内所有信息**并自动录入数据库。文档含 11 个章节：5 张网站 4 列表（名称/地址/定位/重点查询内容，46 个站点）、按研究方向组合使用表（8 条）、信息记录模板表（9 字段）、以及方法论正文（怎么用/每周流程/使用原则/最小可用 12 站包等）。

## 2. 数据库

- `V005__info_source.sql`：建表（id、site_name、description、url、时间戳）。
- `V006__info_source_extend.sql`：扩展为通用信息条目（已应用，禁止修改）：

```sql
ALTER TABLE info_source
  ADD COLUMN category   VARCHAR(100) NOT NULL DEFAULT '' COMMENT '所属分类（清单章节名）',
  ADD COLUMN entry_type VARCHAR(16)  NOT NULL DEFAULT 'site' COMMENT 'site/combo/template/note',
  ADD COLUMN key_content TEXT NULL COMMENT '重点查询内容/组合来源/记录目的',
  ADD COLUMN sort_order INT NOT NULL DEFAULT 0 COMMENT '分类内排序号，导入幂等键之一',
  MODIFY COLUMN url VARCHAR(1024) NULL;   -- 非网站条目可为空

UPDATE info_source SET sort_order = id WHERE sort_order = 0;
ALTER TABLE info_source ADD UNIQUE KEY uq_info_source_entry (category, entry_type, sort_order);
```

条目类型语义：

| entry_type | 含义 | url | key_content |
|---|---|---|---|
| site | 网站（含“最小可用 12 站包”） | 必填 http(s) | 重点查询内容 |
| combo | 按研究方向组合使用 | 空 | 推荐组合（来源机构串） |
| template | 信息记录模板字段 | 空 | 记录目的 |
| note | 方法论/流程/原则正文段落、编制说明 | 空 | — |

幂等键：`(category, entry_type, sort_order)`。重复导入同一文档按此 UPSERT，不产生重复行。手工新增条目的 sort_order 由服务端在同分类同类型内自动 +1。

## 3. 文档提取与导入链路

1. `tools/extract_info_source.py`：读取 `doc_convert_to_html` 导出的 HTML（本机路径 `C:/Users/Admin/AppData/Local/Temp/info_source_doc.html`），解析 11 个章节 → 扁平条目，输出 `data/raw/info_source.json`（含 raw sections 供核对）。
   - 第十一章 `名称：URL` 行解析为 site 条目，并用二~六章全表按 URL 回填定位描述与重点查询内容；
   - 网址栏填非地址文本的行（如“以公司官网为准”）转为 note 条目，文本并入描述。
2. `POST /api/info-source/import`：请求体为条目数组，按幂等键 UPSERT，返回 `{received, inserted, updated, total}`。

## 4. API 契约

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/info-source?category=` | 按 category/entry_type/sort_order 排序；可按分类过滤 |
| GET | `/api/info-source/{id}` | 单条；不存在 404 |
| POST | `/api/info-source` | 新增（支持 category/entryType/keyContent），201 + 新记录 |
| POST | `/api/info-source/import` | 批量幂等导入（见上） |
| PUT | `/api/info-source/{id}` | 全量更新；不存在 404 |
| DELETE | `/api/info-source/{id}` | 删除；成功 204 |

字段：`id`、`category`、`entryType`、`siteName`、`description`、`keyContent`、`url`、`sortOrder`、`createdAt`、`updatedAt`。

校验：名称必填（≤100）、分类（≤100）、描述（≤500）；`entryType` 仅 site/combo/template/note；site 必填合法 http(s) 地址（≤1024），其他类型地址可空、给了则必须合法。

## 5. WinForms 界面

- 入口：`MainForm` 顶部面板最右侧「信息来源」按钮。
- `InfoSourceForm`（设计器可编辑）：
  - 顶部「分类筛选」下拉（全部分类 + 11 个章节名，客户端过滤）；
  - 表格列：ID / 分类 / 类型 / 名称 / 定位描述·说明 / 重点查询内容·组合·目的 / 网页地址 / 更新时间；
  - 编辑区：分类、类型（网站/组合/模板/笔记 下拉）、名称、网页地址、定位描述、重点查询内容 + 新增/保存/取消编辑/删除/关闭；
  - 列表一次拉全量，筛选在客户端完成。
- 客户端只调 API，无数据库账号、连接串或 SQL（延续 ADR-001）。

## 6. 验收证据（2026-11-25 实测）

| 检查 | 结果 |
|---|---|
| 服务端 / WinForms 编译 | 均 0 错误 0 警告 |
| 迁移状态 | V001~V006 全部 `applied`（V006 applied 147ms） |
| 首次导入 95 条 | `{"received":95,"inserted":95,"updated":0,"total":95}` |
| 重复导入两次 | 均 `inserted:0, updated:95, total:95`，无重复行 |
| 抽样：国家统计局 | site，定位/重点查询内容/URL 与原文一致 |
| 抽样：电网与电力设备 | combo，描述=使用说明，keyContent=推荐组合，url 为空 |
| 抽样：发现日期 | template，描述=记录内容说明，keyContent=目的 |
| 抽样：公司投资者关系页面 | note，描述含“网址：以公司官网为准” |
| 分类过滤 `?category=十一…` | 13 条（12 站 + 编制说明 note） |
| site 缺地址 | 400 `网站条目的网页地址不能为空` |
| note 无地址新增/更新/删除 | 201 / 200 / 204，测试记录已清理，总数回到 95 |
| 重建基准 | 11 文件全部 SKIP |

导入后数据库内容 = 文档全部信息：46 全表站点 + 12 最小包（已回填）+ 1 编制说明 + 19 方法论段落 + 8 组合 + 9 模板字段 = 95 条。

GUI 实际渲染需在 VS / 桌面端确认；本环境只完成编译与接口层验证。
