# Phase 1 — 公司基本信息库

## 1. 目标与边界

把 A 股上市公司的基础档案抓下来、存好、能查、能更新。

**本期做**：证券清单、公司身份、行业归属、上市信息、治理与联系方式、简介与经营范围。
**本期不做**：财务数据、行情、业务构成、政策、评分、估值。

## 2. 技术路线与职责边界

| 环节 | 技术 | 职责 |
|---|---|---|
| 采集 | Python 3（`src/phase1/fetch_company.py`） | 只抓原始 JSON 落盘，不写库、不清洗 |
| 存储 | MySQL 9.x | 保存清洗后的结构化数据 |
| 数据库变更 | SQL 脚本 + .NET 驱动 | 版本化建表、升级、防篡改 |
| 处理/服务 | .NET 10（`src/dotnet/StockAnalysis.Api`） | 解析原始 JSON → 清洗 → 写 MySQL → 提供查询 API |

```
Python 采集 → data/raw/companies/*.json → .NET 解析/清洗 → MySQL company 表 → .NET API
```

采集脚本变动频繁、Python 抓取方便；数据处理和对外服务需要类型安全与长期维护，用 .NET 10。原始层保留，任一侧出问题都能单独重跑。

---

## 3. 数据源（已实测）

### 3.1 全市场证券清单

**主源**：东财 push2 `clist` 接口，三组 `fs` 分别对应沪市、深市、北交所。
- 实测数量：沪 2469 + 深 3096 + 京 366 = 5931 家（含少量 B 股与退市代码，脚本按号段过滤）。
- 北交所 `f13=0` 与深市相同 → 交易所必须按请求的 `fs` 分组判定。
- 清单混入退市代码（如 `810011`）→ 按号段过滤：沪 `60/68/90`、深 `00/30/20`、京 `43/83/87/92`。
- 连续快请求会被临时断连 → 每请求间隔 1 秒，断连按 1/2/4/8/10 秒退避。
- 部分响应为 gzip → 检测 `\x1f\x8b` 头后解压。

**备用源**：东财 push2 全域名被临时封禁时，自动回退新浪 `Market_Center` 接口：
- `sh_a` 节点 2264 家 + `sz_a` 节点 2842 家 = 5106 家 A 股。
- 新浪没有可用的北交所节点；其 `hs_a` 里的北交所是 `43/83/87` 旧号段，东财 F10 返回"股票代码不合法"，**不能用**。北交所 920 号段待东财清单恢复后单独补档。
- 回退在脚本内自动完成，打印来源；清单缓存 `data/raw/security_list.json` 记录实际抓到的代码。

### 3.2 公司概况与发行信息

```http
GET https://emweb.securities.eastmoney.com/PC_HSF10/CompanySurvey/PageAjax?code=SH600519
```

- `code` 格式：`SH600519` / `SZ000001` / `BJ920045`。
- `jbzl[0]`：名称、全称、英文名、曾用名、证券类型、东财行业 `EM2016`、证监会行业 `INDUSTRYCSRC1`、省份、法人、董事长、总经理、董秘、网址、电话、邮箱、地址、注册资本、员工数、律所、审计机构、简介、经营范围。
- `fxxg[0]`：成立日、上市日、发行价、面值、发行数量。
- **坑**：代码不存在时返回 `{"status": 非0, "message": "..."}`，无 `jbzl`，必须识别跳过。此接口在 push2 封禁期间仍可用。

### 3.3 实际控制人

```http
GET https://emweb.securities.eastmoney.com/PC_HSF10/ShareholderResearch/PageAjax?code=SH600519
```

取 `sjkzr[0].HOLDER_NAME`。实测茅台有值，平安银行为 `null`。**为空写 NULL，不用第一大股东代替**。

---

## 4. 原始层文件规范（Python 输出）

`data/raw/security_list.json`：
```json
[{"company_id": "SH600519", "code": "600519", "exchange": "SSE", "name": "贵州茅台"}]
```

`data/raw/companies/{company_id}.json`：
```json
{
  "company_id": "SH600519",
  "code": "600519",
  "exchange": "SSE",
  "fetched_at": "2026-11-25T20:15:00+08:00",
  "source_urls": ["...CompanySurvey...", "...ShareholderResearch..."],
  "raw_hash": "288a1b470784ec52",
  "payload": { "survey": {...}, "holder": {...} }
}
```

`raw_hash` 是原始内容（不含 hash 字段本身）的 SHA256 前 16 位，用于追溯入库数据对应的原始快照。
`data/raw/fetch_log.json` 记录本次抓取总数、成功数、失败清单。原始文件只覆盖当日快照，解析失败可随时重跑。

**并发与续跑**：`--workers 8` 控制并发（默认 6），每任务完成后轻度限速 0.3 秒；`--skip-existing` 跳过已有原始文件，用于中断后续跑。实测 8 并发抓 60 家耗时 6.3 秒、零失败，说明 F10 接口对适度并发稳定；不要盲目加大并发，遇到失败增多应降回 4~6。

---

## 5. MySQL 表设计（SQL 脚本驱动）

数据库：`stock_analysis`，字符集 `utf8mb4`。建表语句不写在 C# 里，全部放在：

```
src/dotnet/StockAnalysis.Api/Db/Migrations/V###__name.sql
```

脚本编译为嵌入式资源随程序发布，由代码扫描、校验、按序执行。数据库：`stock_analysis`。

### 5.1 `company`（主表，一司一行）

| 字段 | 类型 | 说明 |
|---|---|---|
| company_id | VARCHAR(16) PK | `SH600519` / `SZ000001` / `BJ920045` |
| code | VARCHAR(6) | 6 位代码 |
| exchange | VARCHAR(8) | SSE / SZSE / BSE |
| board | VARCHAR(32) | 从 `SECURITY_TYPE` 解析 |
| name / name_full / name_en | VARCHAR | 简称 / 全称 / 英文名 |
| former_names | VARCHAR(512) | 曾用名 |
| list_date / found_date | VARCHAR(10) | YYYY-MM-DD |
| industry_em / industry_csrc | VARCHAR | 东财行业 / 证监会行业 |
| province | VARCHAR(32) | 省份 |
| legal_person / chairman / president / secretary | VARCHAR | 法人 / 董事长 / 总经理 / 董秘 |
| actual_controller | VARCHAR(256) | 实控人，可空 |
| reg_capital_wan | DECIMAL(20,4) | 注册资本（万元） |
| emp_num | INT | 员工数 |
| auditor / law_firm | VARCHAR | 审计机构 / 律所 |
| website / tel / email | VARCHAR | 联系方式 |
| address / reg_address | TEXT | 办公地址 / 注册地址 |
| issue_price / par_value / total_issue_num | DECIMAL | 发行信息 |
| profile / business_scope | LONGTEXT | 简介 / 经营范围 |
| source_url | VARCHAR(1024) | 来源链接（两条原始 URL 以 ` | ` 拼接） |
| fetched_at | VARCHAR(32) | 抓取时点 |
| raw_hash | VARCHAR(32) | 原始文件哈希 |

索引：`code`、`exchange`、`industry_em`、`name(32)`（V002 增加）。

### 5.2 `fetch_log`

`id`, `task`, `source`, `fetched_at`, `total`, `ok`, `fail`, `note`。记录每次导入结果。

### 5.3 `schema_migrations`（版本记录，由迁移器维护）

`version` PK、`name`、`checksum`、`applied_at`、`execution_ms`。不手工修改。

### 5.4 行业维度（V003，EM 三级）

`company.industry_em` 存的是东财 EM2016 三级串（如 `金融-银行-股份制与城商行`），5108 条**全部恰好 2 个半角连字符**，无内部连字符。V003 用 MySQL `SUBSTRING_INDEX` 拆分为结构化层级，并把公司挂到三级叶子节点。

| 表 | 字段 | 说明 |
|---|---|---|
| `dim_industry` | industry_id VARCHAR(255) PK | 稳定路径 ID，如 `EM/金融/银行/股份制与城商行`，重建后不变 |
| | scheme / level / parent_id / name / path | 口径（本期仅 EM）/ 层级 1-3 / 父节点 / 本级名 / 完整层级串 |
| `company_industry_map` | company_id + industry_id + scheme (PK) | 公司挂三级叶子，一司一条 EM 映射 |
| | is_primary / effective_date / source | 主行业标识 / 生效日期 / 来源，均为后续多口径预留 |

**为什么回灌放在 SQL 而不是 C#**：5108 家存量数据必须回灌；项目铁律是"SQL 是唯一建表/改表来源"；回灌 SQL 随 V003 被 checksum 锁定，天然可复现可审计。C# 侧 `IndustryQuery.RebuildStatements` 保留同一段 SQL 供 rebuild 端点复用——**修改回灌逻辑必须新增 V004 并同步该常量**。

**节点计数口径**：`directCount` = 直属公司数（只有三级节点有值）；`totalCount` = 自身及全部后代公司数，由服务端自底向上累加。界面显示与筛选均用 `totalCount` 口径。

**实测**：374 个节点（一级 27 / 二级 103 / 三级 244），映射 5108 条，一级 totalCount 合计 5108，三级 directCount 合计 5108。

---

## 6. 数据库版本管理规范

**原则：SQL 是唯一建表/改表来源，代码只负责执行与记账。**

1. **命名**：`V<三位版本号>__<英文简述>.sql`，如 `V001__init_company.sql`、`V002__company_name_index.sql`。版本号必须递增且不重复。
2. **脚本内容**：一条语句以分号结尾；不支持存储过程内部的字符串分号；注释用 `-- ` 开头。
3. **执行顺序**：按版本号从小到大，已应用的跳过；遇到失败立即停止，不再应用后续版本。
4. **防篡改**：每个脚本按全文计算 SHA256 存入版本表。已应用脚本内容变更 → 状态显示 `modified`，执行迁移返回明确错误并拒绝。
   **禁止修改已应用脚本**，变更只能新增 `V00N__*.sql`（当前最新为 V003，如 `ALTER TABLE ... ADD COLUMN`）。
5. **建库**：连接串里的库不存在时，代码执行 `CREATE DATABASE IF NOT EXISTS`；建表与后续变更全部由 SQL 脚本完成。
6. **触发方式**：`POST /api/migrate` 或 `POST /api/company/init`（兼容旧名）；`POST /api/company/import` 会先自动迁移。配置 `Database:AutoMigrate=true` 可在服务启动时自动执行。
7. **回滚**：不提供自动回滚。需要撤销时新增反向迁移版本（如删列、删索引），在脚本中写清原因。执行迁移前对生产库做备份。

**新增字段的流程**：写 `V00N__xxx.sql`（含 `ALTER TABLE ... ADD COLUMN`）→ 编译 → 查看 `/api/migrate/status` → 执行 `/api/migrate` → 同步修改 `Company.cs` / `CompanyImporter.cs` 的映射 → 重新导入。

**已验证的迁移行为（2026-11-25）**：
- 首次执行：V001 `applied`（48ms）；状态由 `pending` 变 `applied`。
- 重复执行：`skipped`，不重复建表。
- 新增 V002：V001 `skipped`、V002 `applied`（30ms）。
- 临时修改 V002 内容：状态变 `modified`，执行返回 `hasError=true` 且提示"已应用版本禁止修改"；恢复脚本后状态回到 `applied`。
- 新增 V003（行业维度）：V001/V002 `skipped`、V003 `applied`（264ms，含 5108 条回灌）；重复执行全部 `skipped`。

---

## 7. .NET 10 服务接口

项目：`src/dotnet/StockAnalysis.Api`，依赖 MySqlConnector + Dapper。

| 方法 | 路径 | 用途 |
|---|---|---|
| GET | `/health` | 服务与 MySQL 连接状态 |
| GET | `/api/migrate/status` | 各迁移版本状态：`pending` / `applied` / `modified` |
| POST | `/api/migrate` | 执行全部待应用迁移（幂等） |
| POST | `/api/company/init` | 兼容旧名，等价 `/api/migrate` |
| GET | `/api/company/preview?take=3` | 只解析原始文件，不连库（验证映射用） |
| POST | `/api/company/import?dryRun=false` | 先迁移，再解析原始 JSON 写入 MySQL |
| GET | `/api/company/stats` | 总数、各交易所数量、最近抓取时间 |
| GET | `/api/company?keyword=银行&exchange=SSE` | 条件查询；可加 `industryId=EM/金融` 按行业及子行业筛选 |
| GET | `/api/company/{companyId}` | 单个公司详情（完整字段） |
| GET | `/api/industry/tree?scheme=EM` | 三级行业树，节点含 directCount / totalCount |
| POST | `/api/industry/rebuild?scheme=EM` | 从 `company.industry_em` 全量重建行业映射（幂等） |

配置：`appsettings.json` 的 `ConnectionStrings:MySQL`、`RawData:Directory`（原始目录绝对路径）、`Database:AutoMigrate`、`Database:MigrationsDirectory`（留空则用嵌入式脚本）。

---

## 8. 更新策略

| 任务 | 频率 | 做法 |
|---|---|---|
| 证券清单 | 每周一次 | 优先东财三组 `fs`；失败自动回退新浪 `sh_a` / `sz_a` |
| 公司档案 | 每月一次 | Python 逐个抓档案原始 JSON |
| 新股补档 | 清单发现新代码时 | 立即抓一次档案 |
| 解析入库 | 采集后手动触发 | `POST /api/company/import`（内部先跑迁移） |
| 全量重建 | 手动 | 清空 MySQL 表后重新 import |

---

## 9. 实施步骤与当前进度

1. [x] Python 采集器：清单抓取（含新浪回退）→ 档案原始 JSON → fetch_log。
2. [x] 样本验证：4 家沪深样本全部成功，原始 JSON 含 `raw_hash`。
3. [x] .NET 10 服务：解析 → MySQL 写库 → 查询 API，编译通过。
4. [x] SQL 迁移机制：版本表、按序执行、幂等、防篡改，全部实测通过。
5. [x] MySQL 账号配置、V001/V002 迁移应用、样本入库与详情接口验证。
6. [x] 全市场沪深采集：5106 家，8 并发耗时 10 分 32 秒，零失败。
7. [ ] 北交所 920 号段补档（待东财 push2 清单恢复；当前仅 2 家样本）。
8. [x] 执行 `/api/company/import`，5108 份原始文件全部入库，零错误，重复导入幂等。
9. [x] V003 行业维度：EM 三级拆分、374 节点/5108 映射、行业树与筛选接口、WinForms 树形筛选，全部验证通过。

## 10. 验收标准

- 库内沪深公司数与清单数量一致（约 5100），代码唯一；北交所单独说明。
- 5 家抽查样本字段与接口原文一致，含 `raw_hash` 与来源链接。
- 重复运行不产生重复记录，只更新 `fetched_at` 与变化字段。
- `fetch_log` 能说明每次抓取的时间、数量、失败数。
- 迁移状态查询所有版本为 `applied`，无 `modified`。

**实测验收结果（2026-11-25）**：

| 项目 | 结果 |
|---|---|
| 原始文件 | 5108 份（沪深 5106 + 北交所样本 2），采集失败 0 |
| 入库统计 | 5108 行：SSE 2264 / SZSE 2842 / BSE 2 |
| 导入错误 | 0；第二次导入后总数不变（幂等） |
| 5 家抽查 | 贵州茅台、平安银行、宁德时代、南模生物、蘅东光：名称/行业/上市日/注册资本/raw_hash 全部与原始文件一致 |
| 迁移状态 | V001、V002、V003 均为 `applied` |

**行业维度验收（2026-11-25）**：

| 项目 | 结果 |
|---|---|
| 节点数 | 374（一级 27 / 二级 103 / 三级 244），与 5108 条 EM2016 去重结果一致 |
| 计数一致性 | 一级 totalCount 合计 5108；三级 directCount 合计 5108 |
| 叶子筛选 | `EM/金融/银行/股份制与城商行` → 浦发银行、华夏银行、民生银行 |
| 一级筛选 | `EM/金融` → 浦发/华夏/民生/中信证券/招商银行（含全部子行业） |
| rebuild 幂等 | 连续两次均返回 industries=374, mappings=5108 |
| 非法 scheme | `scheme=CSRC` 返回 400，不破坏 EM 数据 |

**日常操作流程**：采集 → `POST /api/company/import` → **`POST /api/industry/rebuild?scheme=EM`（必做，同步行业树）** → WinForms 查询。

---

> **免责声明**：以上内容基于公开数据和量化分析，仅供参考，不构成投资建议。市场有风险，投资需谨慎。任何投资决策应结合个人风险承受能力、资金状况和投资目标独立判断，必要时咨询持牌专业机构。过往表现不预示未来收益。
