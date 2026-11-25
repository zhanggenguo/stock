# -*- coding: utf-8 -*-
"""从 doc_convert_to_html 导出的 HTML 中完整提取《投资研究信息来源网站清单》全部内容。

输出 data/raw/info_source.json：
  intro: 文档标题与开篇说明段落
  entries: [{category, entryType, name, url, description, keyContent, sortOrder}]
      entryType: site（网站，含第十一章最小可用包）| combo（研究方向组合）|
                 template（信息记录模板字段）| note（方法论/流程/原则正文段落）
  sections: 章节原始结构（供人工核对，不直接入库）
"""
import html
import json
import pathlib
import re

HTML_PATH = pathlib.Path(r"C:/Users/Admin/AppData/Local/Temp/info_source_doc.html")
OUT_PATH = pathlib.Path(r"C:/Users/Admin/Desktop/股票分析软件/data/raw/info_source.json")


def strip_tags(fragment: str) -> str:
    text = re.sub(r"<br\s*/?>", "\n", fragment, flags=re.I)
    text = re.sub(r"<[^>]+>", "", text)
    text = html.unescape(text)
    return re.sub(r"\s+", " ", text).strip()


def cell_texts(row_html: str):
    return [strip_tags(c) for c in re.findall(r"<t[dh][^>]*>(.*?)</t[dh]>", row_html, re.S)]


def main():
    src = HTML_PATH.read_text(encoding="utf-8")
    body = re.search(r"<body[^>]*>(.*)</body>", src, re.S).group(1)

    token_re = re.compile(
        r"<h([1-6])[^>]*>(.*?)</h\1>|<p[^>]*>(.*?)</p>|<table[^>]*>(.*?)</table>",
        re.S,
    )
    sections = []  # 原始章节结构，供核对
    entries = []   # 扁平化导入条目
    intro = []

    cur = None
    sort_counter = 0

    def add(category, entry_type, name, url, description, key_content):
        nonlocal sort_counter
        sort_counter += 1
        entries.append(
            {
                "category": category,
                "entryType": entry_type,
                "name": name,
                "url": url or None,
                "description": description or None,
                "keyContent": key_content or None,
                "sortOrder": sort_counter,
            }
        )

    for m in token_re.finditer(body):
        if m.group(1):  # heading
            cur = {"heading": strip_tags(m.group(2)), "paragraphs": [], "entries": []}
            sections.append(cur)
        elif m.group(3):  # paragraph
            text = strip_tags(m.group(3))
            if not text:
                continue
            if cur is None:
                intro.append(text)
                continue
            cur["paragraphs"].append(text)
            h = cur["heading"]
            if h.startswith("十一、"):
                pair = re.match(r"^(.+?)[：:]\s*(https?://\S+?)\s*$", text)
                if pair:
                    add(h, "site", pair.group(1), pair.group(2), None, None)
                else:  # 编制说明等非网址段落按 note 入库
                    add(h, "note", h, None, text, None)
            elif h.startswith(("一、", "八、", "十、")):
                add(h, "note", h, None, text, None)
        elif m.group(4):  # table
            if cur is None:
                continue
            rows = [cell_texts(r) for r in re.findall(r"<tr[^>]*>(.*?)</tr>", m.group(4), re.S)]
            rows = [r for r in rows if any(c for c in r)]
            if not rows:
                continue
            h = cur["heading"]
            if h.startswith("七、"):  # 组合使用表：研究方向 | 推荐组合 | 说明 | (重点)
                data = rows[1:] if rows[0][0] == "网站名称" else rows
                for r in data:
                    add(
                        h, "combo",
                        r[0] if len(r) > 0 else "",
                        None,
                        r[2] if len(r) > 2 else None,      # 使用说明
                        "；".join(x for x in [r[1] if len(r) > 1 else "", r[3] if len(r) > 3 else ""] if x),
                    )
            elif h.startswith("九、"):  # 记录模板表：字段 | 占位 | 记录内容说明 | 目的
                data = rows[1:] if rows[0][0] == "网站名称" else rows
                for r in data:
                    add(
                        h, "template",
                        r[0] if len(r) > 0 else "",
                        None,
                        r[2] if len(r) > 2 else None,      # 记录内容说明
                        r[3] if len(r) > 3 else None,      # 目的
                    )
            else:  # 网站 4 列表：名称 | 地址 | 定位 | 重点查询内容
                data = rows[1:] if rows[0][0] == "网站名称" else rows
                for r in data:
                    name = r[0] if len(r) > 0 else ""
                    url = r[1] if len(r) > 1 else ""
                    desc = r[2] if len(r) > 2 else None
                    key = r[3] if len(r) > 3 else None
                    if url and not re.match(r"^https?://", url, re.I):
                        # 网址栏填的不是地址（如“以公司官网为准”）：并入描述，按 note 入库
                        desc = ((desc + "；") if desc else "") + "网址：" + url
                        add(h, "note", name, None, desc, key)
                    else:
                        add(h, "site", name, url, desc, key)

    # 第十一章“最小可用网站包”只有名称+网址，用二~六章完整表回填定位与重点查询内容
    full = {
        e["url"]: (e["description"], e["keyContent"])
        for e in entries
        if e["entryType"] == "site" and e["category"].startswith(("二、", "三、", "四、", "五、", "六、")) and e["url"]
    }
    for e in entries:
        if e["entryType"] == "site" and e["category"].startswith("十一、") and not e["description"]:
            desc, key = full.get(e["url"], (None, None))
            e["description"] = desc
            e["keyContent"] = key

    result = {
        "sourceDoc": "投资研究信息来源网站清单_2026-11-1.docx",
        "intro": intro,
        "entries": entries,
        "sections": sections,
    }
    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    by_type = {}
    for e in entries:
        by_type[e["entryType"]] = by_type.get(e["entryType"], 0) + 1
    print("sections:", len(sections), "| entries:", len(entries), "| byType:", by_type)
    for s in sections:
        n = sum(1 for e in entries if e["category"] == s["heading"])
        print(f"  {s['heading']}  entries={n}")
    # 校验最小包回填
    filled = [e for e in entries if e["category"].startswith("十一、") and e["entryType"] == "site" and e["description"]]
    print("十一 sites with backfilled desc:", len(filled), "/", sum(1 for e in entries if e["entryType"] == "site" and e["category"].startswith("十一、")))


if __name__ == "__main__":
    main()
