# -*- coding: utf-8 -*-
"""一次性生成器：从当前 WinForms 源码生成 tools/rebuild_winforms.py。"""
import json
import pathlib

root = pathlib.Path(__file__).resolve().parents[1]
rels = [
    "src/winforms/StockQuery.WinForms/StockQuery.WinForms.csproj",
    "src/winforms/StockQuery.WinForms/appsettings.json",
    "src/winforms/StockQuery.WinForms/Program.cs",
    "src/winforms/StockQuery.WinForms/Models.cs",
    "src/winforms/StockQuery.WinForms/ApiClient.cs",
    "src/winforms/StockQuery.WinForms/MainForm.cs",
    "src/winforms/StockQuery.WinForms/MainForm.Designer.cs",
    "src/winforms/StockQuery.WinForms/DetailForm.cs",
    "src/winforms/StockQuery.WinForms/DetailForm.Designer.cs",
    "src/winforms/StockQuery.WinForms/InfoSourceForm.cs",
    "src/winforms/StockQuery.WinForms/InfoSourceForm.Designer.cs",
]
data = {r: (root / r).read_text(encoding="utf-8") for r in rels}

head = '''#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""从基准内容重建 WinForms 项目（Framework 4.6）。

用法（项目根目录）：
    python tools/rebuild_winforms.py

幂等：内容相同的文件跳过；被改动过的文件恢复为基准内容。
基准内容与 docs/winforms/03-复现手册.md 记录的版本一致。
"""
import pathlib
import sys

FILES = '''

tail = '''


def main():
    root = pathlib.Path(__file__).resolve().parents[1]
    written = skipped = 0
    for rel, text in FILES.items():
        path = root / rel
        if path.exists() and path.read_text(encoding="utf-8") == text:
            skipped += 1
            print("SKIP  ", rel)
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        written += 1
        print("WRITE ", rel)
    print("完成：写入 %d，跳过 %d" % (written, skipped))
    print("下一步：dotnet build src/winforms/StockQuery.WinForms/StockQuery.WinForms.csproj")
    return 0


if __name__ == "__main__":
    sys.exit(main())
'''

out = root / "tools" / "rebuild_winforms.py"
out.write_text(head + json.dumps(data, ensure_ascii=False, indent=2) + tail, encoding="utf-8")
print("生成", out, "包含", len(data), "个文件")
