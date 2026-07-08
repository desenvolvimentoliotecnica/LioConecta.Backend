#!/usr/bin/env python3
"""Extract a representative subset from Hyperion YTD Excel export for Compass IBP seed."""

from __future__ import annotations

import argparse
import json
from collections import defaultdict
from pathlib import Path


def parse_sku(raw: object) -> tuple[str, str]:
    text = str(raw or "").strip()
    if " - " in text:
        code, desc = text.split(" - ", 1)
        return code.strip(), desc.strip()
    return text, text


def row_key(row: tuple) -> tuple:
    return (row[0], row[1], row[2], row[3])


def main() -> None:
    parser = argparse.ArgumentParser(description="Seed Compass IBP JSON from Hyperion export")
    parser.add_argument(
        "--input",
        default=r"c:\Users\leonardo.mendes\Downloads\Base Análise YTD (1).xlsx",
        help="Path to Hyperion Excel export",
    )
    parser.add_argument(
        "--output",
        default="src/LioConecta.Infrastructure/Seed/Data/compass-ibp-sample.json",
        help="Output JSON path (relative to backend repo root)",
    )
    parser.add_argument("--max-rows", type=int, default=3000)
    args = parser.parse_args()

    try:
        import openpyxl
    except ImportError:
        import subprocess
        import sys

        subprocess.check_call([sys.executable, "-m", "pip", "install", "openpyxl", "-q"])
        import openpyxl

    input_path = Path(args.input)
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    wb = openpyxl.load_workbook(input_path, data_only=True, read_only=True)
    ws = wb["BD"]

    all_rows: list[tuple] = []
    for i, row in enumerate(ws.iter_rows(values_only=True)):
        if i == 0:
            continue
        all_rows.append(row)

    selected: dict[tuple, tuple] = {}
    by_tipo_dir: dict[tuple[str, str], list[tuple]] = defaultdict(list)
    top_faturamento: list[tuple[float, tuple]] = []
    top_contrib: list[tuple[float, tuple]] = []

    for row in all_rows:
        tipo = str(row[0] or "").strip()
        dir_ = str(row[6] or "").strip()
        var = float(row[10] or 0)
        by_tipo_dir[(tipo, dir_)].append(row)
        if "Faturamento" in tipo:
            top_faturamento.append((abs(var), row))
        if "Contribui" in tipo and "L" in tipo:
            top_contrib.append((abs(var), row))

    for items in by_tipo_dir.values():
        for r in sorted(items, key=lambda x: abs(float(x[10] or 0)), reverse=True)[:30]:
            selected[row_key(r)] = r

    for _, row in sorted(top_faturamento, reverse=True)[:400]:
        selected[row_key(row)] = row
    for _, row in sorted(top_contrib, reverse=True)[:400]:
        selected[row_key(row)] = row

    final = list(selected.values())[: args.max_rows]
    rows_out = []
    for row in final:
        sku_code, sku_desc = parse_sku(row[2])
        rows_out.append(
            {
                "tipo": str(row[0] or "").strip(),
                "familiaComercial": str(row[1] or "").strip(),
                "skuCode": sku_code,
                "skuDescription": sku_desc,
                "clienteHyperion": str(row[3] or "").strip(),
                "cliente": str(row[4] or "").strip(),
                "matriz": str(row[5] or "").strip(),
                "diretoria": str(row[6] or "").strip(),
                "unidade": str(row[7] or "").strip(),
                "ibpAtual": float(row[8] or 0),
                "ibpAnterior": float(row[9] or 0),
                "variacao": float(row[10] or 0),
            }
        )

    payload = {
        "snapshot": {
            "label": "Jul/2026 YTD",
            "versionAtual": "IBP 2026 v3.2 (Jul)",
            "versionAnterior": "IBP 2026 v3.1 (Jun)",
            "sourceSystem": "Hyperion",
            "rowCount": len(rows_out),
        },
        "rows": rows_out,
    }

    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {len(rows_out)} rows to {output_path.resolve()}")
    wb.close()


if __name__ == "__main__":
    main()
