import importlib.util
from pathlib import Path
import unittest
import json
import tempfile

spec=importlib.util.spec_from_file_location('analysis',Path(__file__).with_name('Analyze-VisualProfile.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)

class StatisticsTests(unittest.TestCase):
    def test_diagnostic_execution_cannot_qualify(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            (root/'definition.json').write_text('{}')
            (root/'execution.json').write_text(json.dumps({'passed':True,'diagnostic_only':True}))
            with self.assertRaisesRegex(AssertionError,'Diagnostic ablation'):module.analyze(root)
    def test_diagnostic_sample_cannot_qualify_even_with_full_case_list(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            (root/'definition.json').write_text(json.dumps({'cases':[{'id':'case'}]}))
            (root/'execution.json').write_text(json.dumps({'passed':True,'case_ids':['case']}))
            (root/'case.json').write_text(json.dumps({'diagnostic_only':True}))
            with self.assertRaisesRegex(AssertionError,'Diagnostic ablation'):module.analyze(root)
    def test_missing_is_not_zero(self):
        s=module.summarize([-1,-1,0,10]);self.assertEqual(s['available'],2);self.assertEqual(s['mean'],5);self.assertEqual(s['p95'],10)
        self.assertIsNone(module.summarize([-1,-1])['p95'])
    def test_nearest_rank_ignores_missing_and_does_not_reorder_input(self):
        values=list(range(100,0,-1))+[-1];original=values.copy();s=module.summarize(values)
        self.assertEqual(s['p95'],95);self.assertEqual(s['p50'],50);self.assertEqual(values,original)
    def test_invalid_is_not_silently_discarded(self):
        for value in (float('nan'),float('inf'),-2,-.5):
            with self.assertRaises(ValueError):module.summarize([1,value])

if __name__=='__main__':unittest.main()
