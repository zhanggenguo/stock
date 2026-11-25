"""Phase 1 采集器：只抓原始数据，不写数据库、不做清洗。

数据落盘：
  data/raw/security_list.json          证券清单
  data/raw/companies/{company_id}.json 每家公司原始 JSON（含来源、时点）

清洗、解析、写 MySQL 由 .NET 10 服务负责。

用法：
  python src/phase1/fetch_company.py --sample            # 每个市场取 2 家
  python src/phase1/fetch_company.py                     # 全市场
  python src/phase1/fetch_company.py --sample --from-cache  # 用已有清单，只抓档案
依赖：仅 Python 标准库。
"""

import argparse
import concurrent.futures
import gzip
import hashlib
import json
import time
import urllib.parse
import urllib.request
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RAW_DIR = ROOT / "data" / "raw"
COMPANY_DIR = RAW_DIR / "companies"
LIST_CACHE = RAW_DIR / "security_list.json"
FETCH_LOG = RAW_DIR / "fetch_log.json"

CLIST_URL = "https://push2.eastmoney.com/api/qt/clist/get"
SINA_LIST_URL = "https://vip.stock.finance.sina.com.cn/quotes_service/api/json_v2.php/Market_Center.getHQNodeData"
SINA_COUNT_URL = "https://vip.stock.finance.sina.com.cn/quotes_service/api/json_v2.php/Market_Center.getHQNodeStockCount"
SURVEY_URL = "https://emweb.securities.eastmoney.com/PC_HSF10/CompanySurvey/PageAjax"
HOLDER_URL = "https://emweb.securities.eastmoney.com/PC_HSF10/ShareholderResearch/PageAjax"
UT = "bd1d9ddb04089700cf9c27f6f7426281"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
    "Referer": "https://quote.eastmoney.com/",
}
GROUPS = [
    ("SSE", "m:1+t:2,m:1+t:23"),
    ("SZSE", "m:0+t:6,m:0+t:80"),
    ("BSE", "m:0+t:81+s:2048"),
]
VALID_PREFIX = {
    "SSE": ("60", "68", "90"),
    "SZSE": ("00", "30", "20"),
    "BSE": ("43", "83", "87", "92"),
}


def get_json(url, params):
    full = url + "?" + urllib.parse.urlencode(params)
    referer = "https://finance.sina.com.cn/" if "sina.com.cn" in url else "https://quote.eastmoney.com/"
    req = urllib.request.Request(full, headers={**HEADERS, "Referer": referer})
    with urllib.request.urlopen(req, timeout=20) as resp:
        payload = resp.read()
        if payload[:2] == b"\x1f\x8b":
            payload = gzip.decompress(payload)
        return json.loads(payload.decode("utf-8")), full


def get_with_retry(url, params, retries=4):
    for attempt in range(retries + 1):
        try:
            return get_json(url, params)
        except Exception:
            if attempt == retries:
                raise
            time.sleep(min(2 ** attempt, 10))
    raise RuntimeError("unreachable")


def now():
    return datetime.now().astimezone().isoformat(timespec="seconds")


def fetch_list_em(sample):
    rows = []
    for exchange, fs in GROUPS:
        pn, total = 1, None
        while True:
            data, _ = get_with_retry(CLIST_URL, {
                "fs": fs, "fields": "f12,f13,f14", "pn": pn, "pz": 100,
                "fid": "f12", "po": 0, "ut": UT,
            })
            block = data.get("data") or {}
            total = block.get("total", 0)
            diff = block.get("diff") or []
            items = diff.values() if isinstance(diff, dict) else diff
            for item in items:
                code = str(item.get("f12", ""))
                if len(code) != 6 or not code.startswith(VALID_PREFIX[exchange]):
                    continue
                prefix = {"SSE": "SH", "SZSE": "SZ", "BSE": "BJ"}[exchange]
                rows.append({"company_id": f"{prefix}{code}", "code": code,
                             "exchange": exchange, "name": item.get("f14", "")})
            if sample or pn * 100 >= total or not items:
                break
            pn += 1
            time.sleep(1)
        if sample:
            rows = [r for r in rows if r["exchange"] == exchange][:2] + [
                r for r in rows if r["exchange"] != exchange]
        else:
            time.sleep(1)
    seen, unique = set(), []
    for row in rows:
        if row["company_id"] not in seen:
            seen.add(row["company_id"])
            unique.append(row)
    return unique


def fetch_list_sina(sample):
    """东财 push2 被限流时的备用清单：新浪 sh_a / sz_a 节点（沪深 A 股）。
    新浪北交所节点仅含 43/83/87 旧号段，东财 F10 不识别，故北交所暂不从这里取。
    """
    rows = []
    page_size = 100
    id_prefix = {"sh": "SH", "sz": "SZ"}
    market = {"sh": "SSE", "sz": "SZSE"}
    for node, symbol_prefix in (("sh_a", "sh"), ("sz_a", "sz")):
        count_data, _ = get_with_retry(SINA_COUNT_URL, {"node": node})
        total = int(count_data) if isinstance(count_data, (int, str)) else 0
        pages = 1 if sample else (total + page_size - 1) // page_size
        for pn in range(1, pages + 1):
            data, _ = get_with_retry(SINA_LIST_URL, {
                "page": pn, "num": page_size, "sort": "symbol", "asc": 1, "node": node,
            })
            if not isinstance(data, list) or not data:
                break
            for item in data:
                symbol = str(item.get("symbol", ""))
                code = str(item.get("code", ""))
                if not symbol.startswith(symbol_prefix):
                    continue
                m = market[symbol_prefix]
                if len(code) != 6 or not code.startswith(VALID_PREFIX[m]):
                    continue
                rows.append({"company_id": f"{id_prefix[symbol_prefix]}{code}", "code": code,
                             "exchange": m, "name": item.get("name", "")})
            time.sleep(0.5)
    if sample:
        by_exchange = {}
        for row in rows:
            by_exchange.setdefault(row["exchange"], []).append(row)
        rows = [r for ex in ("SSE", "SZSE") for r in by_exchange.get(ex, [])[:2]]
    seen, unique = set(), []
    for row in rows:
        if row["company_id"] not in seen:
            seen.add(row["company_id"])
            unique.append(row)
    return unique


def fetch_list(sample):
    try:
        return fetch_list_em(sample), "eastmoney"
    except Exception as exc:
        print(f"东财证券列表不可用（{str(exc)[:80]}），回退新浪清单")
        return fetch_list_sina(sample), "sina"


def fetch_profile(row):
    company_id, code, exchange = row["company_id"], row["code"], row["exchange"]
    code_arg = company_id
    survey, survey_url = get_with_retry(SURVEY_URL, {"code": code_arg})
    if "jbzl" not in survey or not survey["jbzl"]:
        return None, "no_jbzl"
    holder, holder_url = get_with_retry(HOLDER_URL, {"code": code_arg})

    secucode = survey["jbzl"][0].get("SECUCODE") or ""
    suffix = secucode.split(".")[-1] if "." in secucode else ""
    expected = {"SSE": "SH", "SZSE": "SZ", "BSE": "BJ"}[exchange]
    if suffix and suffix != expected:
        return None, f"exchange_mismatch:{suffix}!={expected}"

    COMPANY_DIR.mkdir(parents=True, exist_ok=True)
    envelope = {
        "company_id": company_id,
        "code": code,
        "exchange": exchange,
        "fetched_at": now(),
        "source_urls": [survey_url, holder_url],
        "payload": {"survey": survey, "holder": holder},
    }
    body = json.dumps(envelope, ensure_ascii=False, indent=2)
    envelope["raw_hash"] = hashlib.sha256(body.encode("utf-8")).hexdigest()[:16]
    raw = json.dumps(envelope, ensure_ascii=False, indent=2)
    path = COMPANY_DIR / f"{company_id}.json"
    path.write_text(raw, encoding="utf-8")
    return {"company_id": company_id, "raw_hash": envelope["raw_hash"]}, None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sample", action="store_true", help="每个市场只取 2 家")
    ap.add_argument("--from-cache", action="store_true", help="使用已缓存的证券清单")
    ap.add_argument("--workers", type=int, default=6, help="并发数（默认 6）")
    ap.add_argument("--skip-existing", action="store_true", help="跳过已有原始文件")
    ap.add_argument("--limit", type=int, help="只处理前 N 家（测试用）")
    args = ap.parse_args()

    list_source = "缓存"
    if args.from_cache:
        if not LIST_CACHE.exists():
            raise SystemExit(f"缓存不存在：{LIST_CACHE}")
        cached = json.loads(LIST_CACHE.read_text(encoding="utf-8"))
        universe = []
        for item in cached:
            if isinstance(item, dict):
                universe.append(item)
            elif isinstance(item, list) and len(item) >= 3:
                universe.append({"company_id": item[0], "code": item[1],
                                 "exchange": item[2], "name": item[3] if len(item) > 3 else ""})
    else:
        universe, list_source = fetch_list(args.sample)
        LIST_CACHE.parent.mkdir(parents=True, exist_ok=True)
        LIST_CACHE.write_text(json.dumps(universe, ensure_ascii=False), encoding="utf-8")
    if args.sample:
        by_exchange = {}
        for row in universe:
            by_exchange.setdefault(row["exchange"], []).append(row)
        universe = [r for ex in ("SSE", "SZSE", "BSE") for r in by_exchange.get(ex, [])[:2]]
    if args.limit:
        universe = universe[:args.limit]
    print(f"证券清单 {len(universe)} 家（来源：{list_source}，并发 {args.workers}）", flush=True)

    ok, skipped, failures = 0, 0, []

    def work(row):
        cid = row["company_id"]
        if args.skip_existing and (COMPANY_DIR / f"{cid}.json").exists():
            return cid, "skipped", None
        try:
            _rec, err = fetch_profile(row)
            time.sleep(0.3)  # 并发下轻度限速，避免触发源站保护
            return cid, "ok" if not err else err, None
        except Exception as exc:
            time.sleep(0.3)
            return cid, "error", str(exc)[:120]

    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
        for idx, (cid, state, detail) in enumerate(pool.map(work, universe), 1):
            if state == "ok":
                ok += 1
            elif state == "skipped":
                skipped += 1
            elif state == "error":
                failures.append({"company_id": cid, "error": detail})
            else:
                failures.append({"company_id": cid, "error": state})
            if idx % 100 == 0:
                print(f"  已处理 {idx}/{len(universe)}，成功 {ok}，失败 {len(failures)}", flush=True)

    processed = len(universe) - skipped
    log = {"fetched_at": now(), "total": processed, "ok": ok,
           "fail": len(failures), "failures": failures[:100]}
    FETCH_LOG.write_text(json.dumps(log, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"完成：成功 {ok}，跳过 {skipped}，失败 {len(failures)}，日志 -> {FETCH_LOG}", flush=True)


if __name__ == "__main__":
    main()
