# 第二阶段：市值数据采集与存储设计（v0.1）

- 状态：**已实施并验收**。用户 2026-11-25 22:22 指定先回填最近 3 个交易日；数据已入库，之后改为每日增量。
- 日期：2026-11-25
- 上一阶段：[第一阶段：公司基本信息](phase1-公司基本信息.md)
- 技术路线不变：Python 采集 → 原始 JSON → .NET 10 API → MySQL，SQL 迁移是唯一 Schema 来源（见 `README.md` 与 V001~V003）。

## 1. 目标与非目标

**目标**：按交易日存储每家公司的市值快照，以及能支撑后续分析的有价值参数（估值、交易活跃度），可幂等重复导入，可按时点查询。

**本阶段非目标**：投资建议、龙头评分、政策权重、WinForms 界面改造、财报历史股本还原。这些留给后续阶段。

## 2. 已确认决策与默认假设

| 项 | 结论 | 依据 |
|---|---|---|
| 表结构 | **A：单宽事实表** `fact_market_cap_daily`，一公司一交易日一行 | 用户已确认 |
| 历史范围 | **先回填最近 3 个交易日**（用户 2026-11-25 22:22 确认），之后每个交易日收盘后增量采一次 | 用户确认，优先拿到可用数据 |
| 指标范围 | 市值 + 股本 + 估值 + 交易活跃度**一次采齐** | 推荐默认，避免第二阶段反复改表 |
| 股本口径 | 当日市值直接采接口值，不自行回算；保留 `shares_asof` 标记股本时点 | 推荐默认 |
| 数据源 | 腾讯 `qt.gtimg.cn` 快照 + 新浪历史日线为主；东财 push2 自动回退 | 本轮实测，见第 3 节 |
| 采集频率 | 交易日收盘后一次（如 15:30 后） | 推荐默认 |

> 上表中「默认假设」项如有不同意见，请在实施前指出；否则按此执行。

## 3. 数据源实测结果（2026-11-25，只读探测）

此前东财 push2 曾被 IP 级封禁，本轮重新探测已恢复。**以下字段均来自实际返回样本**，缩放系数已用贵州茅台（600519）与比亚迪（002594）交叉验证。

### 3.1 单股实时快照 `push2.eastmoney.com/api/qt/stock/get`

| 字段 | 含义 | 缩放 | 样本值（比亚迪） |
|---|---|---|---|
| `f43` / `f60` | 现价 / 昨收 | ÷100 | 7445 → 74.45 元 |
| `f84` / `f85` | 总股本 / 流通股本 | 原值（股） | 9,117,197,565 / 3,487,068,722 |
| `f116` / `f117` | 总市值 / 流通市值 | 原值（元） | 678,775,358,714 / 259,612,266,353 |
| `f162` / `f163` / `f164` | PE(TTM) / PE(静态) / PE(动态) | ÷100 | 22.78 / 20.81 / 21.46 |
| `f167` | PB | ÷100 | 2.73 |
| `f168` | 换手率 | ÷100 | 0.93% |
| `f170` / `f171` | 涨跌幅 / 振幅 | ÷100 | -1.99% / 2.29% |
| `f86` | 行情时间戳（Unix 秒） | — | 用于判定交易日 |

### 3.2 批量快照 `push2.eastmoney.com/api/qt/ulist.np/get`

一次可传多个 `secids`（如 `1.600519,0.002594`），实测返回：

| 字段 | 含义 | 缩放 |
|---|---|---|
| `f2` / `f3` | 现价 / 涨跌幅 | ÷100 |
| `f5` / `f6` | 成交量（手）/ 成交额（元） | 原值 |
| `f8` / `f9` / `f23` | 换手率 / PE(TTM) / PB | ÷100 |
| `f12` / `f14` | 代码 / 名称 | 原值 |
| `f20` / `f21` | 总市值 / 流通市值 | 原值（元） |

**坑**：`ulist.np` 返回的 `f84/f85` 语义与单股接口不同（比亚迪返回 252269776 / 3045），**不要从批量接口取股本**。股本只用单股 `stock/get` 的 `f84/f85`，或用 `总市值 ÷ 现价` 反算并标记精度。

### 3.3 历史日线 `push2his.eastmoney.com/api/qt/stock/kline/get`

`klt=101&fqt=0` 返回未复权日线，`fields2=f51..f61` 依次为：
日期、开盘、收盘、最高、最低、成交量（手）、成交额（元）、振幅(%)、涨跌幅(%)、涨跌额、换手率(%)。

样本（600519，2026-11-25）：`2026-11-25,1458.00,1474.84,1480.00,1447.00,46674,6842335769.00,2.26,1.02,14.84,0.37`。

**坑（实测踩过）**：K 线的振幅、涨跌幅、换手率（f58/f59/f61）**本身就是百分数**（2.26 表示 2.26%），不能再 ÷100；只有行情快照接口的 `f3/f8/f9/f23` 等字段才是放大 100 倍的整数。小样本验证时曾因此把 -1.1% 写成 -0.011，已修正。

### 3.4 实际采用的数据源组合（探测后调整）

| 用途 | 主源 | 备源 | 原因 |
|---|---|---|---|
| 最新交易日快照（市值/PE/PB/换手/量额） | 腾讯 `qt.gtimg.cn/q=`（GBK 文本，按 `~` 分隔） | 东财 `ulist.np` | 探测后东财 push2 再次被限流；腾讯字段齐全且稳定 |
| 最近 3 日历史日线（收盘价/高低/成交量） | 新浪 `CN_MarketData.getKLineData` | 东财 `push2his` | 东财历史接口同样出现连接重置；新浪可用 |

**腾讯字段索引**（`~` 分隔后的下标）：`2` 代码、`3` 现价、`4` 昨收、`30` 时间、`32` 涨跌幅、`33/34` 最高/最低、`36` 成交量（手）、`37` 成交额（万元）、`38` 换手率、`43` 振幅、`44` **流通**市值、`45` **总**市值（**亿元**，顺序与东财相反）、`46` PB、`52` PE(TTM)。注意索引 39 也是 PE 但与东财口径不符，**以 52 为准**（已用茅台/浦发交叉验证）。

**新浪日线字段**：`day/open/high/low/close/volume`（volume 单位为股，入库换算成手）。新浪不提供历史成交额与换手率，历史两天这两列留 NULL；涨跌幅与振幅用相邻两日收盘价自行计算。

> 东财接口并非永久不可用，采集器保留自动回退：腾讯/新浪失败时尝试东财对应接口。此处的调整只影响数据来源，不改变表结构与口径。

## 4. 口径与单位约定

1. 市值、成交额一律存**元**，`DECIMAL(20,2)`；界面展示时再换算成亿元。
2. 股本存**股**，`DECIMAL(24,2)`；比例类（PE/PB/换手率/涨跌幅/振幅）按接口值 ÷100 后入库。
3. 价格取**未复权**收盘价；复权价需要时由后续阶段另存，不混入本表。
4. 总市值/流通市值以接口返回值为准；`总市值 ÷ 现价` 只用于交叉校验股本，不反向覆盖接口值。
5. 交易日以 `f86` 时间戳对应日期为准；非交易日跳过，不写 0 值行。
6. 每行保留 `raw_hash` 与 `source_url`，原始响应按日归档，满足可追溯与可重建要求。

## 5. 表结构（V004 草案，尚未建表）

新增迁移脚本 `V004__market_cap_daily.sql`，风格与 V001 一致（`utf8mb4_unicode_ci`、注释、语句以分号结尾）：

```sql
-- V004 第二阶段：公司日市值与估值快照
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
```

**设计说明**

- `UNIQUE(company_id, trade_date)` 是幂等基石：同一天重复导入走 `INSERT ... ON DUPLICATE KEY UPDATE`，只更新可变字段与 `fetched_at/raw_hash`，不新增行。
- `ps_ttm`、`dividend_yield` 当前接口不直接给，先留列（可为 NULL），后续有稳定来源再补，不需要改表。
- `quality_flag` 取值约定：`''` 正常、`BACKFILLED_SHARES` 历史回算股本、`NO_FLOAT_CAP` 流通市值缺失、`PRICE_ZERO` 价格异常。
- 表与 `company` 不建外键：沿用第一阶段风格，由导入逻辑保证 `company_id` 存在，避免迁移顺序耦合。

## 6. 采集与导入流程

```text
data/raw/security_list.json（第一阶段已有）
        │  取 code + market 生成 secid
        ▼
python src/phase2/fetch_market.py
        │  ① 单股 kline 取最近 3 个交易日（每公司 1 次，8 并发 + 限速）
        │  ② 分批 qt.gtimg.cn 取最新交易日快照（每批 ~50 个代码）
        │  ③ 由最新市值 ÷ 现价反算股本，historical 两日按收盘价 × 股本计算
        ▼
data/raw/market/manifest.json          （交易日列表、任务元信息）
data/raw/market/{company_id}.json      （3 天 items + 原始响应 + raw_hash）
        ▼
POST /api/market/import                （可选 ?tradeDate= 过滤某一天）
        │  解析/校验/字段缩放/UPSERT
        ▼
fact_market_cap_daily
```

**Python 采集器**（`src/phase2/fetch_market.py`）

- 只抓原始 JSON，不写库（沿用第一阶段职责划分）；
- 参数：`--days 3`（默认 3，回填最近 N 个交易日）、`--date YYYY-MM-DD`（截止日，默认今天）、`--workers 8`、`--batch-size 50`、`--limit`、`--skip-existing`；
- 失败重试 2 次，记录 `data/raw/market/{date}/fetch_log.json`；
- 非交易日检测：用单股 `f86` 或 kline 最新日期校验，不一致则跳过当日任务。

**.NET 10 API 新增接口**

| 接口 | 用途 |
|---|---|
| `POST /api/market/import?tradeDate=` | 从原始层导入当日快照，幂等 UPSERT，返回 ok/fail/updated |
| `GET /api/market/daily?companyId=&from=&to=&limit=` | 按公司查日序列，供后续画图/分析 |
| `GET /api/market/stats?tradeDate=` | 当日覆盖公司数、总市值分位、缺失字段统计 |
| `GET /api/market/status` | 已入库交易日列表与最新交易日 |

新增文件：`Models/MarketCap.cs`、`Services/MarketImport.cs`、`Services/MarketQuery.cs`；`Program.cs` 注册路由；迁移 V004 由既有 `MigrationRunner` 自动执行。

## 7. 最近 3 个交易日回填策略（本阶段先做）

- 用新浪日线取最近 3 个交易日的未复权收盘价、最高/最低、成交量；涨跌幅与振幅用相邻收盘价/高低价自行计算；新浪不提供历史成交额与换手率，历史两天留 NULL；
- 股本用**当前**快照的 `总市值 ÷ 现价` 反算，写入 `shares_asof = 采集日期`；
- 3 天均置 `quality_flag = 'BACKFILLED_SHARES'`：最新一天的市值/PE/PB/换手率/量额取腾讯快照接口原值，历史两天由「历史收盘价 × 当前股本」计算；
- PE/PB 只有最新一天能从接口取到，历史两天留 NULL；不做财报口径的 PE 还原；
- 之后的每日增量走同一条流程，只是只取最新一天，同样保留质量标记，避免不同来源口径混淆。

## 8. 风险与限制

1. 腾讯/新浪/东财接口均无 SLA；采集器已实现主备自动回退。任何来源切换都会写进 `source` 与 `source_url`，`quality_flag` 保留回算标记。
2. 接口字段缩放系数（÷100）会随上游改版变化；导入时做量纲校验（例如市值/现价与股本偏差超过 5% 则标记异常）。
3. 单日全量约 5108 行，一年约 128 万行；当前单表足够，暂不分表分区。
4. 本阶段只存快照，不做市值排名、行业聚合的预计算表；查询量上来后再考虑汇总表。

## 9. 验收结果（2026-11-25 实测）

| 检查项 | 结果 |
|---|---|
| `dotnet build` | 0 错误，0 警告 |
| V004 迁移 | `applied`；V001~V004 全部 applied |
| 采集覆盖 | 5106 家清单，成功 4998、失败 108（均为 2025 年 1 月前退市、近 3 日无行情）；腾讯快照 5105/5106，1 家（300114）缺失且无近期日线，正常排除 |
| 原始层 | `data/raw/market/{company_id}.json` 4998 份 + `manifest.json` + `fetch_log.json`，均含 `raw_hash` 与 `source_urls` |
| 导入 | 4998 文件 / 14985 行，0 错误；按 `(company_id, trade_date)` UPSERT |
| 入库分布 | 2026-11-23：4992 行；11-24：4998 行；11-25：4995 行 |
| 比亚迪抽样（11-25） | 收盘 74.45；总市值 6787.75 亿；流通市值 2596.12 亿；PE(TTM) 22.78；PB 2.73；与腾讯接口一致 |
| 历史两日 | 由收盘价 × 当前股本计算，`quality_flag = BACKFILLED_SHARES`，PE/PB 为 NULL |
| 统计接口 | 11-25 总市值 102.92 万亿、流通 82.32 万亿、中位数 49.28 亿；总市值 > 流通市值，量纲正确 |
| 幂等验证 | 再次 `POST /api/market/import?tradeDate=2026-11-25`，写入 4995 行，状态接口行数仍为 4995，无新增重复行 |
| 源码合规 | 新增源码无数据库账号密码硬编码；连接串只在服务端 `appsettings.json` |

**实施期间修正的三个缺陷**（均已回归验证）：

1. K 线涨跌幅/振幅/换手率本身就是百分数，曾多除 100（-1.1% 写成 -0.011）；
2. 全局限速使 8 并发实际只有 3.3 次/秒，改为 `--throttle` 参数（默认 0.05 秒）；
3. 腾讯行情第 44/45 位是**流通市值/总市值**（与东财相反），曾导致总市值与流通市值对调；修正后重采重导并复核。

**运行命令**：

```bash
python src/phase2/fetch_market.py --days 3
curl -X POST http://127.0.0.1:5000/api/market/import
curl http://127.0.0.1:5000/api/market/status
curl "http://127.0.0.1:5000/api/market/daily?companyId=SZ002594&limit=3"
```

## 10. 变更规则

1. V004 一旦 `applied`，**禁止修改**；新增字段一律新增 V005；
2. 任何字段口径变化（单位、缩放、复权方式）必须先更新本文档，再改采集与导入；
3. 实施过程按项目约定留痕：`docs/winforms/02-开发日志.md` 追加时间线，`README.md` 更新阶段状态，工作记忆追加结论。

---

> **免责声明**：本项目使用公开数据，仅用于个人研究与技术开发，不构成投资建议。市场有风险，决策需谨慎。
