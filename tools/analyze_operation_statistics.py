"""Summarize opt-in operation JSONL files. Uses only aggregated samples, never end totals twice."""
import argparse
import collections
import csv
import json
from pathlib import Path


def analyze(paths):
    groups = {}
    sessions = set()
    warnings = []
    numeric = ("Keys", "Clicks", "Scroll", "JoyButtons", "JoyAxes", "AcceptedKeys", "AcceptedClicks",
               "AcceptedScroll", "AcceptedJoyButtons", "AcceptedJoyAxes", "MouseDistance", "RawAp", "AcceptedAp", "ActiveSeconds")
    for path in sorted({Path(p).resolve() for p in paths}):
        rows = []
        with path.open(encoding="utf-8-sig") as source:
            for index, line in enumerate(source, 1):
                try:
                    row = json.loads(line)
                    if row.get("Schema") != 1:
                        raise ValueError("unsupported schema")
                    rows.append(row)
                except (json.JSONDecodeError, ValueError) as error:
                    warnings.append(f"{path.name}:{index}: skipped invalid row ({error})")
        if not rows:
            continue
        session_id = rows[0]["Session"]
        if session_id in sessions:
            warnings.append(f"{path.name}: duplicate session skipped")
            continue
        if any(r["Session"] != session_id for r in rows):
            raise ValueError(f"{path}: multiple session IDs in one file")
        sessions.add(session_id)
        ends = [r for r in rows if r["Kind"] == "session_end"]
        samples = [r for r in rows if r["Kind"] == "sample"]
        if not ends:
            warnings.append(f"{path.name}: unfinished session; only saved samples included")
        elif sum(r["Counts"]["RawTotal"] for r in samples) != ends[-1]["Counts"]["RawTotal"]:
            warnings.append(f"{path.name}: saved sample total differs from session end")
        for row in rows:
            if row["Kind"] not in ("sample", "game_event"):
                continue
            context = row["Context"]
            key = tuple(context[name] for name in ("Scenario", "Action", "Capture", "BookOpen", "WorkMode", "Battle"))
            group = groups.setdefault(key, dict(zip(("Scenario", "Action", "Capture", "BookOpen", "WorkMode", "Battle"), key),
                                                Samples=0, Seconds=0, Events=collections.Counter(), **{n: 0 for n in numeric}))
            if row["Kind"] == "game_event":
                group["Events"][row["Event"]] += 1
                continue
            group["Samples"] += 1
            duration = row["EndSeconds"] - row["StartSeconds"]
            if duration < 0:
                raise ValueError(f"{path}: negative sample duration")
            group["Seconds"] += duration
            for name in numeric:
                group[name] += row["Counts"][name]
    result = []
    for group in groups.values():
        group["RawTotal"] = sum(group[n] for n in numeric[:5])
        group["AcceptedTotal"] = sum(group[n] for n in numeric[5:10])
        group["LimitedTotal"] = group["RawTotal"] - group["AcceptedTotal"]
        group["LimitedAp"] = group["RawAp"] - group["AcceptedAp"]
        group["RawPerMinute"] = round(group["RawTotal"] * 60 / group["Seconds"], 3) if group["Seconds"] else None
        group["AcceptedPerMinute"] = round(group["AcceptedTotal"] * 60 / group["Seconds"], 3) if group["Seconds"] else None
        group["LimitedFraction"] = round(group["LimitedTotal"] / group["RawTotal"], 6) if group["RawTotal"] else 0
        group["Events"] = dict(sorted(group["Events"].items()))
        result.append(group)
    return {"Sessions": len(sessions), "Groups": result, "Warnings": warnings,
            "Notes": "Scenario is self-reported. Counts are input events, not characters or productivity. "
                     "Mouse distance is separate. Events are selected game transitions/outcomes, not total UI clicks. "
                     "Seconds are monotonic recording time, including idle/suspend gaps while process remains alive; not measured work time. "
                     "Unfinished sessions may lose the last unsaved partial bucket."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("paths", nargs="+", type=Path, help="JSONL files or a statistics directory")
    parser.add_argument("--output", type=Path, required=True, help="Output JSON path; matching CSV is also written")
    args = parser.parse_args()
    paths = [file for path in args.paths for file in (path.glob("*.jsonl") if path.is_dir() else [path])]
    if not paths:
        parser.error("no JSONL files found")
    report = analyze(paths)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with args.output.with_suffix(".csv").open("w", encoding="utf-8-sig", newline="") as target:
        fields = list(report["Groups"][0]) if report["Groups"] else ["Scenario", "Action", "RawTotal", "AcceptedTotal"]
        writer = csv.DictWriter(target, fieldnames=fields)
        writer.writeheader()
        for row in report["Groups"]:
            writer.writerow({**row, "Events": json.dumps(row["Events"], ensure_ascii=False)})
    print(f"Sessions={report['Sessions']} Groups={len(report['Groups'])} "
          f"Raw={sum(g['RawTotal'] for g in report['Groups'])} Effective={sum(g['AcceptedTotal'] for g in report['Groups'])}")
    for warning in report["Warnings"]:
        print("WARNING: " + warning)
    print(args.output)


if __name__ == "__main__":
    main()
