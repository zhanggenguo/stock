# 上市公司分析软件

按阶段推进。**第一阶段已完成并验收；第二阶段（市值数据采集与存储）已完成最近 3 个交易日回填并验收**。更早的复杂总体设计放 `docs/_archive/`。

## 技术路线（已确定）

| 环节 | 技术 |
|---|---|
| 数据采集 | Python 3 脚本，只抓原始数据，不写库 |
| 存储 | MySQL 9.x（`stock_analysis` 库，utf8mb4） |
| 数据库变更 | SQL 建表脚本 + .NET 代码驱动，版本化升级 |
| 数据处理与服务 | .NET 10（ASP.NET Core Web API + Dapper） |

数据流向：`Python 抓原始 JSON → data/raw/ → .NET 解析/清洗 → MySQL → .NET API 提供查询`

## 阶段文档

- Phase 1（已完成）：[docs/phase1-公司基本信息.md](docs/phase1-公司基本信息.md) — 基础档案采集、建库、查询与 WinForms 客户端。
- Phase 2（已实施）：[docs/phase2-市值数据.md](docs/phase2-市值数据.md) — 每日市值、股本、估值与交易快照；单宽事实表 `fact_market_cap_daily`（V004）；已入库 2026-11-23/24/25 三日共 14985 行。
- 配套功能：[docs/info-source-信息来源目录.md](docs/info-source-信息来源目录.md) — 右上角“信息来源”按钮、清单增删改查（V005+V006 + CRUD API），并已按《投资研究信息来源网站清单》文档完成改造：95 条信息（46 站点+12 最小包+组合/模板/方法论）自动录入数据库，支持分类筛选与文档幂等导入。
- 更早的多阶段总体设想：`docs/_archive/`（仅作参考，不实施）。

Phase 2 暂不涉及：龙头评分、业务构成、政策权重。

## 快速运行

```bash
# 1. 采集（Python，只用标准库；东财清单不可用时自动回退新浪）
python src/phase1/fetch_company.py --sample               # 小样本，每市场 2 家
python src/phase1/fetch_company.py                        # 全市场（约 5100 家，8 并发约 10 分钟）
python src/phase1/fetch_company.py --from-cache --workers 8 --skip-existing  # 续跑/增量，跳过已有文件
python src/phase1/fetch_company.py --sample --from-cache  # 用已有清单，只重抓档案

# 2. 数据库账号已在 src/dotnet/StockAnalysis.Api/appsettings.json 配置

# 3. 启动 .NET 服务（端口统一为 5000）
dotnet run --project src/dotnet/StockAnalysis.Api/StockAnalysis.Api.csproj --urls http://localhost:5000

# 4. 数据库迁移（幂等，可重复调用）
curl http://localhost:5000/api/migrate/status    # 查看版本状态
curl -X POST http://localhost:5000/api/migrate   # 执行待应用脚本（含 V003 行业维度）

# 5. 业务接口
curl -X POST http://localhost:5000/api/company/import              # 原始 JSON 入库
curl -X POST "http://localhost:5000/api/industry/rebuild?scheme=EM" # 入库后同步行业树（必做）
curl http://localhost:5000/api/company/stats                       # 入库统计
curl -G --data-urlencode "keyword=银行" http://localhost:5000/api/company
curl http://localhost:5000/api/company/SZ000001                    # 单个详情

# 6. 行业分级筛选
curl "http://localhost:5000/api/industry/tree?scheme=EM"           # 三级行业树 + 各级公司数
curl -G --data-urlencode "industryId=EM/金融" http://localhost:5000/api/company            # 一级筛选（含子行业）
curl -G --data-urlencode "industryId=EM/金融/银行/股份制与城商行" http://localhost:5000/api/company  # 三级细分筛选

# 7. 第二阶段：最近 3 个交易日市值回填（腾讯快照 + 新浪日线）
python src/phase2/fetch_market.py --days 3                         # 采集原始 JSON 到 data/raw/market
curl -X POST http://localhost:5000/api/market/import               # 入库（幂等 UPSERT）
curl http://localhost:5000/api/market/status                       # 已入库交易日与行数
curl "http://localhost:5000/api/market/daily?companyId=SZ002594&limit=3"  # 单公司日序列

# 8. 信息来源目录（增删改查 + 文档导入）
curl http://localhost:5000/api/info-source                       # 95 条（含分类/类型/重点查询内容）
curl "http://localhost:5000/api/info-source?category=二、中国宏观与政策：第一优先级"  # 按分类过滤
python tools/extract_info_source.py                              # 重新提取文档 → data/raw/info_source.json
curl -X POST -H "Content-Type: application/json" --data @data/raw/info_source_entries.json http://localhost:5000/api/info-source/import  # 幂等导入
curl -H "Content-Type: application/json" -d '{"siteName":"东方财富","description":"行情与F10","url":"https://quote.eastmoney.com"}' http://localhost:5000/api/info-source
curl -X PUT -H "Content-Type: application/json" -d '{"siteName":"东方财富网","description":"行情、公告与F10","url":"https://quote.eastmoney.com/"}' http://localhost:5000/api/info-source/1
curl -X DELETE http://localhost:5000/api/info-source/1
```

## 验证状态（2026-11-25）

- MySQL 9.7.4 已连通，账号已配置；`stock_analysis` 库由迁移自动创建。
- 迁移 V001（建表）、V002（简称索引）、V003（行业维度）、V004（日市值事实表）、V005（信息来源目录）、V006（信息来源扩展：分类/类型/重点查询内容/排序 + 幂等键）已应用；重复执行跳过，脚本被改动时拒绝执行。
- 市值数据：2026-11-23/24/25 三日共 14985 行（4992/4998/4995），重复导入不新增行；比亚迪 11-25 总市值 6787.75 亿、流通 2596.12 亿、PE 22.78、PB 2.73 与接口一致。
- 样本已入库并通过详情接口验证：代码、名称、法人、注册资本、上市日、来源链接均正确。
- 全量沪深采集完成：5106 家（新浪 sh_a 2264 + sz_a 2842），8 并发耗时 10 分 32 秒，零失败；另保留 2 家北交所 920 样本，原始 JSON 共 5108 份。
- 全量入库完成：`company` 表 5108 行（沪 2264 / 深 2842 / 京 2），5 家抽查字段与原文一致，重复导入不产生重复记录。
- 行业维度（V003）：EM2016 三级拆分，374 个节点（一级 27 / 二级 103 / 三级 244），5108 条公司映射；树节点计数合计 5108；叶子筛选（股份制与城商行）与重建幂等均验证通过。
- 北交所 920 号段待东财清单恢复后补齐：新浪清单只有 43/83/87 旧号段，东财 F10 返回"股票代码不合法"。

## WinForms 查询客户端（Framework 4.6）

- 项目：`src/winforms/StockQuery.WinForms/`；编译：`dotnet build src/winforms/StockQuery.WinForms/StockQuery.WinForms.csproj`
- 数据访问：**调用现有 .NET 10 API，不直连 MySQL**，决策依据见 [ADR-001](docs/winforms/ADR-001-数据访问方式.md)
- 功能：左侧三级行业树筛选（节点显示公司数）、树与列表之间可拖动分隔条（树最小 180px / 列表最小 420px）、关键字/交易所筛选、公司列表、双击或右键“详情”、Tab 详情窗口（历史股价表格+折线图、公司资料）、右上角“信息来源”按钮（分类筛选 + 站点/组合/模板/笔记增删改查）、统计与迁移版本查看
- 界面可在 VS 设计器中可视化调整：`MainForm.cs` / `DetailForm.cs` / `InfoSourceForm.cs` 均配 `.Designer.cs`（`InitializeComponent()` + 无参构造 + `SubType=Form`），改动界面请切到"设计"视图拖动控件
- 一键重建：`python tools/rebuild_winforms.py`（幂等，11 个源码文件内置基准）
- 完整留痕：[开发日志](docs/winforms/02-开发日志.md) · [复现手册](docs/winforms/03-复现手册.md)
- 实测：编译 0 错误 0 警告；API 联调通过（5108 家、详情 35 字段、行业树 374 节点、市值 3 日 14985 行、信息来源 CRUD 全链路）；客户端源码无数据库凭据与 SQL
- 运行前先启动 API（端口 5000），WinForms 在 `src/winforms/StockQuery.WinForms/appsettings.json` 配置地址

## 免责声明

以上内容基于公开数据和量化分析，仅供参考，不构成投资建议。市场有风险，投资需谨慎。任何投资决策应结合个人风险承受能力、资金状况和投资目标独立判断，必要时咨询持牌专业机构。过往表现不预示未来收益。
#   s t o c k  
 