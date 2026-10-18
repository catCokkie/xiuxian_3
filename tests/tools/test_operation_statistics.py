import importlib.util
import json
from pathlib import Path
import unittest
import uuid

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("operation_analysis", REPO / "tools/analyze_operation_statistics.py")
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)


def row(kind, raw=0, effective=0, start=0, end=0, scenario="typing"):
    return {"Schema": 1, "Session": "test", "Kind": kind, "StartSeconds": start, "EndSeconds": end,
            "Context": {"Scenario": scenario, "Action": "dungeon", "Capture": "isolated_test",
                        "BookOpen": False, "WorkMode": False, "Battle": "exploring"},
            "Counts": {"Keys": raw, "Clicks": 0, "Scroll": 0, "JoyButtons": 0, "JoyAxes": 0,
                       "AcceptedKeys": effective, "AcceptedClicks": 0, "AcceptedScroll": 0,
                       "AcceptedJoyButtons": 0, "AcceptedJoyAxes": 0, "MouseDistance": 0,
                       "RawAp": raw, "AcceptedAp": effective, "ActiveSeconds": 0,
                       "RawTotal": raw, "AcceptedTotal": effective}, "Event": "book_open"}


class OperationAnalysisTests(unittest.TestCase):
    def experience_rows(self):
        start = row("session_start"); end = row("session_end", 20, 20, 120, 120)
        start["Metadata"] = {"process_input_experience": "10", "process_effective_inputs": "100", "process_online_basic_experience": "5",
                             "base_input_experience_rate": ".28", "cultivation_input_experience_rate": ".35", "cultivation_input_bonus_enabled": "True"}
        end["Metadata"] = {"process_input_experience": "16.3", "process_effective_inputs": "120", "process_online_basic_experience": "8.36"}
        a = row("sample", 10, 10, 0, 60); a["Context"]["Action"] = "cultivation"
        return [start, a, row("sample", 10, 10, 60, 120), end]

    def test_experience_reconciles_mode_rates_and_excludes_time_growth(self):
        result = analysis.analyze([self.write("experience.jsonl", self.experience_rows())])
        comparison = result["ExperienceComparisons"][0]
        self.assertAlmostEqual(6.3, comparison["ExpectedInputExperience"])
        self.assertAlmostEqual(6.3, comparison["ActualInputExperience"])
        self.assertAlmostEqual(3.36, comparison["OnlineBasicExperience"])
        self.assertTrue(comparison["Balanced"])

    def test_export_checkpoint_does_not_claim_later_inputs_were_already_paid(self):
        rows = self.experience_rows(); rows[-1]["Kind"] = "checkpoint"
        rows.append(row("sample", 50, 50, 120, 180))
        result = analysis.analyze([self.write("ongoing.jsonl", rows)])
        self.assertEqual(20, result["ExperienceComparisons"][0]["EffectiveInputs"])
        self.assertTrue(result["ExperienceComparisons"][0]["Balanced"])
        self.assertEqual(70, sum(g["AcceptedTotal"] for g in result["Groups"]))

    def test_missing_experience_payment_is_reported(self):
        rows = self.experience_rows(); rows[-1]["Metadata"]["process_input_experience"] = "10"
        result = analysis.analyze([self.write("missing.jsonl", rows)])
        self.assertFalse(result["ExperienceComparisons"][0]["Balanced"])
        self.assertTrue(any("input experience" in warning for warning in result["Warnings"]))

    def setUp(self):
        cache = REPO / ".godot"
        cache.mkdir(exist_ok=True)
        self.directory = (cache / "operation_analysis_tests" / uuid.uuid4().hex).resolve()
        self.assertTrue(self.directory.is_relative_to(cache.resolve()))
        self.directory.mkdir(parents=True)

    def tearDown(self):
        for path in self.directory.iterdir():
            self.assertTrue(path.resolve().is_relative_to(self.directory))
            path.unlink()
        self.directory.rmdir()

    def write(self, name, rows, suffix=""):
        path = self.directory / name
        path.write_text("\n".join(json.dumps(r) for r in rows) + "\n" + suffix, encoding="utf-8")
        return path

    def test_totals_use_samples_and_weight_rates_by_duration(self):
        path = self.write("session.jsonl", [row("session_start"), row("sample", 60, 30, 0, 60),
                                          row("sample", 60, 60, 60, 180), row("game_event"), row("session_end", 120, 90)])
        result = analysis.analyze([path]); group = result["Groups"][0]
        self.assertEqual(120, group["RawTotal"])
        self.assertEqual(90, group["AcceptedTotal"])
        self.assertEqual(40, group["RawPerMinute"])
        self.assertEqual(30, group["AcceptedPerMinute"])
        self.assertEqual(.25, group["LimitedFraction"])
        self.assertEqual(1, group["Events"]["book_open"])
        self.assertEqual([], result["Warnings"])

    def test_contexts_and_idle_remain_separate(self):
        path = self.write("session.jsonl", [row("sample", 2, 2, 0, 60), row("sample", 0, 0, 60, 120, "reading")])
        result = analysis.analyze([path])
        self.assertEqual(2, len(result["Groups"]))
        self.assertEqual(0, result["Groups"][1]["RawPerMinute"])
        self.assertTrue(any("unfinished" in warning for warning in result["Warnings"]))

    def test_duplicate_sessions_and_truncated_last_row_are_reported(self):
        a = self.write("a.jsonl", [row("sample", 10, 5, 0, 60)], '{"partial":')
        b = self.write("b.jsonl", [row("sample", 10, 5, 0, 60)])
        result = analysis.analyze([a, a, b])
        self.assertEqual(1, result["Sessions"])
        self.assertEqual(10, result["Groups"][0]["RawTotal"])
        self.assertTrue(any("invalid row" in warning for warning in result["Warnings"]))
        self.assertTrue(any("duplicate" in warning for warning in result["Warnings"]))

    def test_zero_time_never_divides_by_zero(self):
        path = self.write("session.jsonl", [row("game_event")])
        self.assertIsNone(analysis.analyze([path])["Groups"][0]["RawPerMinute"])

    def test_end_total_mismatch_is_not_silently_accepted(self):
        path = self.write("session.jsonl", [row("sample", 10, 5, 0, 60), row("session_end", 11, 5)])
        self.assertTrue(any("differs" in warning for warning in analysis.analyze([path])["Warnings"]))


if __name__ == "__main__":
    unittest.main()
