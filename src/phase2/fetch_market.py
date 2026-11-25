# -*- coding: utf-8 -*-
"""第二阶段：最近 N 个交易日市值数据采集器（只抓原始 JSON，不写库）。

用法（项目根目录）：
    python src/phase2/fetch_market.py --days 3
    python src/phase2/fetch_market.py --days 3 --limit 20      # 小样本验证
    python src/phase2/fetch_market.py --days 3 --skip-existing # 跳过已抓公司

数据来源：东财 push2 快照 + 新浪历史日线（东财 push2his 限流时自动回退）。
输出目录：data/raw/market/，每公司一个 JSON（原始响应 + raw_hash + items）。
"""
import argparse
import concurrent.futures
import datetime as dt
import gzip
import hashlib
import json
import pathlib
import random
import sys
import threading
import time
import urllib.parse
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
LIST_PATH = ROOT / "data" / "raw" / "security_list.json"
OUT_DIR = ROOT / "data" / "raw" / "market"

QUOTE_URL = "https://push2.eastmoney.com/api/qt/ulist.np/get"
TENCENT_QUOTE_URL = "https://qt.gtimg.cn/q="
SINA_KLINE_URL = "https://money.finance.sina.com.cn/quotes_service/api/json_v2.php/CN_MarketData.getKLineData"
EM_KLINE_URL = "https://push2his.eastmoney.com/api/qt/stock/kline/get"
REF_SYMBOLS = ["sh600519", "sz000001"]

QUOTE_FIELDS = "f12,f2,f3,f5,f6,f8,f9,f20,f21,f23"
UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
_rate_lock = threading.Lock()
_last_call = [0.0]
_throttle = 0.0


def log(msg):
    print("[%s] %s" % (dt.datetime.now().strftime("%H:%M:%S"), msg), flush=True)


def secid(row):
    """SH -> 1，SZ/BJ -> 0（东财 secid 市场号）。"""
    return ("1." if row["exchange"] == "SSE" else "0.") + row["code"]


def sina_symbol(row):
    return ("sh" if row["exchange"] == "SSE" else "sz") + row["code"]


def get_json(url, params, retries=2):
    full = url + "?" + urllib.parse.urlencode(params)
    for attempt in range(retries + 1):
        try:
            with _rate_lock:
                wait = _throttle - (time.monotonic() - _last_call[0])
                if wait > 0:
                    time.sleep(wait)
                _last_call[0] = time.monotonic()
            req = urllib.request.Request(full, headers={
                "User-Agent": UA,
                "Accept": "application/json, text/plain, */*",
                "Accept-Encoding": "gzip",
            })
            with urllib.request.urlopen(req, timeout=20) as resp:
                raw = resp.read()
                if raw[:2] == b"\x1f\x8b":
                    raw = gzip.decompress(raw)
            return json.loads(raw.decode("utf-8", "replace")), full
        except Exception as ex:
            if attempt == retries:
                raise RuntimeError("请求失败 %s: %s" % (full[:100], ex))
            time.sleep(0.6 * (attempt + 1) + random.random() * 0.3)


def get_text(url, retries=2):
    """腾讯行情返回 GBK 文本，不能用 JSON 解析。"""
    for attempt in range(retries + 1):
        try:
            with _rate_lock:
                wait = _throttle - (time.monotonic() - _last_call[0])
                if wait > 0:
                    time.sleep(wait)
                _last_call[0] = time.monotonic()
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            with urllib.request.urlopen(req, timeout=20) as resp:
                raw = resp.read()
                if raw[:2] == b"\x1f\x8b":
                    raw = gzip.decompress(raw)
            return raw.decode("gbk", "replace")
        except Exception as ex:
            if attempt == retries:
                raise RuntimeError("请求失败 %s: %s" % (url[:100], ex))
            time.sleep(0.6 * (attempt + 1) + random.random() * 0.3)


def normalize_tencent_line(line):
    """腾讯行情 -> 与东财 ulist 同构的字典，便于统一后续计算。"""
    if "=" not in line:
        return None, None
    symbol = line[2:line.index("=")]
    payload = line[line.index('"') + 1:line.rindex('"')]
    p = payload.split("~")
    if len(p) < 53 or not p[3]:
        return None, None
    price = num(p[3], 4)
    cap_float = num(p[44], 2)
    cap_total = num(p[45], 2)
    return {
        "f12": p[2], "f14": p[1],
        "f2": round(price * 100, 2) if price is not None else None,
        "f3": round(num(p[32], 4) * 100, 2) if p[32] else None,
        "f5": num(p[36], 2),
        "f6": round(num(p[37], 2) * 10000, 2) if p[37] else None,
        "f8": round(num(p[38], 4) * 100, 2) if p[38] else None,
        "f9": round(num(p[52], 4) * 100, 2) if p[52] else None,
        "f20": round(cap_total * 1e8, 2) if cap_total is not None else None,
        "f21": round(cap_float * 1e8, 2) if cap_float is not None else None,
        "f23": round(num(p[46], 4) * 100, 2) if p[46] else None,
    }, line


def div100(value):
    if value is None or value == "-":
        return None
    try:
        return round(float(value) / 100.0, 4)
    except (TypeError, ValueError):
        return None


def num(value, digits=4):
    if value is None or value == "-":
        return None
    try:
        return round(float(value), digits)
    except (TypeError, ValueError):
        return None


def sha256(obj):
    text = json.dumps(obj, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def normalize_sina(data):
    """新浪日线 -> 统一结构；volume 单位：股。"""
    rows = []
    for x in data if isinstance(data, list) else []:
        rows.append({
            "day": x.get("day"),
            "open": num(x.get("open")), "high": num(x.get("high")),
            "low": num(x.get("low")), "close": num(x.get("close")),
            "volume": num(x.get("volume"), 2),
        })
    return rows


def normalize_em(data):
    """东财 kline 备用源 -> 统一结构；成交量 手 -> 股。"""
    klines = ((data.get("data") or {}).get("klines")) or []
    rows = []
    for line in klines:
        p = line.split(",")
        if len(p) < 11:
            continue
        volume = num(p[5], 2)
        rows.append({
            "day": p[0], "open": num(p[1]), "high": num(p[3]),
            "low": num(p[4]), "close": num(p[2]),
            "volume": round(volume * 100, 2) if volume is not None else None,
        })
    return rows


def fetch_kline(symbol, datalen, secid_value=None):
    """新浪为主，东财 push2his 为备。返回 (归一化行, 原始响应, 来源名, URL)。"""
    try:
        data, url = get_json(SINA_KLINE_URL, {
            "symbol": symbol, "scale": 240, "ma": "no", "datalen": datalen,
        }, retries=2)
        rows = normalize_sina(data)
        if rows:
            return rows, data, "SINA", url
    except Exception as ex:
        log("新浪日线失败 %s：%s" % (symbol, ex))
    if secid_value:
        data, url = get_json(EM_KLINE_URL, {
            "secid": secid_value,
            "fields1": "f1,f2,f3,f4,f5,f6",
            "fields2": "f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61",
            "klt": 101, "fqt": 0,
            "beg": (dt.date.today() - dt.timedelta(days=90)).strftime("%Y%m%d"),
            "end": dt.date.today().strftime("%Y%m%d"),
        }, retries=1)
        rows = normalize_em(data)
        if rows:
            return rows, data, "EASTMONEY", url
    raise RuntimeError("日线无数据：%s" % symbol)


def recent_trade_dates(days):
    """用参考股日线确定最近 N 个交易日。"""
    rows, _, _, _ = fetch_kline(REF_SYMBOLS[0], days + 3)
    dates = [r["day"] for r in rows if r["day"]]
    if len(dates) < days:
        raise RuntimeError("无法确定最近 %d 个交易日" % days)
    return dates[-days:]


def fetch_quotes(rows, batch_size):
    """腾讯行情为主、东财 ulist 为备。返回 (归一化快照, 原始文本行)。"""
    quotes, raw_lines = {}, {}
    total = (len(rows) + batch_size - 1) // batch_size
    for i in range(0, len(rows), batch_size):
        batch = rows[i:i + batch_size]
        symbols = ",".join(sina_symbol(r) for r in batch)
        try:
            text = get_text(TENCENT_QUOTE_URL + symbols, retries=2)
            for line in text.splitlines():
                item, raw = normalize_tencent_line(line)
                if item:
                    quotes[item["f12"]] = item
                    raw_lines[item["f12"]] = raw
        except Exception as ex:
            log("腾讯快照批次失败 %d：%s" % (i // batch_size + 1, ex))
        log("快照批次 %d/%d，累计 %d" % (i // batch_size + 1, total, len(quotes)))
        time.sleep(0.12)

    missing = [r for r in rows if r["code"] not in quotes]
    if missing:
        log("腾讯缺失 %d 家，尝试东财补齐" % len(missing))
        for i in range(0, len(missing), batch_size):
            batch = missing[i:i + batch_size]
            try:
                data, _ = get_json(QUOTE_URL, {
                    "secids": ",".join(secid(r) for r in batch),
                    "fields": QUOTE_FIELDS,
                })
                for item in ((data.get("data") or {}).get("diff")) or []:
                    quotes[item.get("f12")] = item
                    raw_lines[item.get("f12")] = json.dumps(item, ensure_ascii=False)
            except Exception as ex:
                log("东财补齐批次失败：%s" % ex)
    return quotes, raw_lines


def build_items(kline_rows, quote, trade_dates, asof, kline_source, kline_url):
    """把日线 + 最新快照整理成逐日 values；字段缩放只在这里做一次。"""
    by_date = {r["day"]: r for r in kline_rows if r.get("day")}
    ordered = sorted(by_date.values(), key=lambda r: r["day"])
    prev_close = {}
    for i, r in enumerate(ordered):
        prev_close[r["day"]] = ordered[i - 1]["close"] if i > 0 else None

    price = div100(quote.get("f2")) if quote else None
    total_cap = num(quote.get("f20"), 2) if quote else None
    float_cap = num(quote.get("f21"), 2) if quote else None
    total_shares = round(total_cap / price, 2) if price and total_cap and price > 0 else None
    float_shares = round(float_cap / price, 2) if price and float_cap and price > 0 else None
    latest = trade_dates[-1]
    fetched_at = dt.datetime.now().isoformat(timespec="seconds")

    items = []
    for date in trade_dates:
        r = by_date.get(date)
        if not r:
            continue
        close = r.get("close")
        if not close or close <= 0:
            continue
        base = prev_close.get(date)
        pct = round((close - base) / base * 100, 4) if base else None
        amplitude = round((r["high"] - r["low"]) / base * 100, 4) if base and r.get("high") and r.get("low") else None
        is_latest = date == latest
        if is_latest:
            cap_total, cap_float = total_cap, float_cap
            pe = div100(quote.get("f9")) if quote else None
            pb = div100(quote.get("f23")) if quote else None
            turnover = div100(quote.get("f8")) if quote else None
            volume = num(quote.get("f5"), 2) if quote else None
            amount = num(quote.get("f6"), 2) if quote else None
        else:
            cap_total = round(close * total_shares, 2) if total_shares else None
            cap_float = round(close * float_shares, 2) if float_shares else None
            pe = pb = turnover = amount = None
            volume = round(r["volume"] / 100, 2) if r.get("volume") is not None else None
        if cap_total is None:
            continue
        items.append({
            "trade_date": date,
            "close": close,
            "pct_change": pct,
            "amplitude": amplitude,
            "volume": volume,
            "amount": amount,
            "turnover_rate": turnover,
            "total_shares": total_shares,
            "float_shares": float_shares,
            "total_market_cap": cap_total,
            "float_market_cap": cap_float,
            "pe_ttm": pe,
            "pb": pb,
            "shares_asof": asof,
            "quality_flag": "BACKFILLED_SHARES",
            "is_suspended": 0,
            "source": "TENCENT+SINA",
            "source_url": kline_url,
            "fetched_at": fetched_at,
        })
    return items


def main():
    parser = argparse.ArgumentParser(description="最近 N 个交易日市值采集")
    parser.add_argument("--days", type=int, default=3, help="回填交易日数量，默认 3")
    parser.add_argument("--date", default="", help="截止日期 YYYY-MM-DD，默认今天")
    parser.add_argument("--workers", type=int, default=8)
    parser.add_argument("--batch-size", type=int, default=50)
    parser.add_argument("--throttle", type=float, default=0.05,
                        help="全局请求最小间隔秒数，默认 0.05（20 次/秒）")
    parser.add_argument("--limit", type=int, default=0, help="只处理前 N 家，0=全部")
    parser.add_argument("--skip-existing", action="store_true")
    args = parser.parse_args()

    if not LIST_PATH.exists():
        print("缺少证券清单：%s" % LIST_PATH, file=sys.stderr)
        return 1
    rows = json.loads(LIST_PATH.read_text(encoding="utf-8"))
    if args.limit:
        rows = rows[:args.limit]

    asof = dt.date.fromisoformat(args.date) if args.date else dt.date.today()
    global _throttle
    _throttle = args.throttle
    trade_dates = recent_trade_dates(args.days)
    log("交易日：" + ", ".join(trade_dates))

    quotes, quote_lines = fetch_quotes(rows, args.batch_size)
    log("快照覆盖 %d / %d" % (len(quotes), len(rows)))

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ok = fail = skipped = 0
    failures = []
    kline_sources = {}

    def work(row):
        nonlocal ok, fail, skipped
        path = OUT_DIR / (row["company_id"] + ".json")
        if args.skip_existing and path.exists():
            skipped += 1
            return
        try:
            kline_rows, kline_raw, source, kline_url = fetch_kline(
                sina_symbol(row), args.days + 3, secid(row))
            kline_sources[source] = kline_sources.get(source, 0) + 1
            quote = quotes.get(row["code"])
            raw = {"kline": kline_raw, "quote": quote,
                   "quote_raw": quote_lines.get(row["code"])}
            items = build_items(kline_rows, quote, trade_dates, asof.isoformat(), source, kline_url)
            if not items:
                fail += 1
                failures.append({"company_id": row["company_id"], "reason": "无有效日线"})
                return
            envelope = {
                "company_id": row["company_id"],
                "code": row["code"],
                "exchange": row["exchange"],
                "name": row.get("name", ""),
                "trade_dates": trade_dates,
                "fetched_at": dt.datetime.now().isoformat(timespec="seconds"),
                "source": "TENCENT+SINA",
                "source_urls": [kline_url, TENCENT_QUOTE_URL + sina_symbol(row)],
                "raw_hash": sha256(raw),
                "raw": raw,
                "items": items,
            }
            path.write_text(json.dumps(envelope, ensure_ascii=False), encoding="utf-8")
            ok += 1
        except Exception as ex:
            fail += 1
            failures.append({"company_id": row["company_id"], "reason": str(ex)})

    started = time.monotonic()
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
        list(pool.map(work, rows))
    log("逐股完成 %d，失败 %d，跳过 %d" % (ok, fail, skipped))

    manifest = {
        "task": "market_cap_daily",
        "source": "TENCENT+SINA",
        "trade_dates": trade_dates,
        "fetched_at": dt.datetime.now().isoformat(timespec="seconds"),
        "total": len(rows), "ok": ok, "fail": fail, "skip": skipped,
        "kline_sources": kline_sources,
        "elapsed_seconds": round(time.monotonic() - started, 1),
    }
    (OUT_DIR / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT_DIR / "fetch_log.json").write_text(
        json.dumps({"fetched_at": manifest["fetched_at"], "total": len(rows),
                    "ok": ok, "fail": fail, "failures": failures[:100]},
                   ensure_ascii=False, indent=2), encoding="utf-8")
    log("完成：%s" % json.dumps(manifest, ensure_ascii=False))
    return 0 if fail == 0 else 2


if __name__ == "__main__":
    sys.exit(main())
