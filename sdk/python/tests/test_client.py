import os
import sys
import unittest
import urllib.error
from unittest.mock import patch, MagicMock

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from bangplanix.client import BangplanixClient, RenderRequest

class TestBangplanixClient(unittest.TestCase):
    def test_missing_template_raises_value_error(self):
        client = BangplanixClient()
        with self.assertRaises(ValueError):
            client.render_report(RenderRequest())

    @patch("urllib.request.urlopen")
    def test_render_report_pdf_success_with_telemetry(self, mock_urlopen):
        mock_resp = MagicMock()
        mock_resp.read.return_value = b"%PDF-1.4 Mock PDF Content"
        mock_resp.headers = {"Content-Type": "application/pdf"}
        mock_resp.__enter__.return_value = mock_resp
        mock_urlopen.return_value = mock_resp

        client = BangplanixClient()
        req = RenderRequest(
            template_path="schema/v1/samples/invoice.bpx",
            data=[{"item": "Sample", "price": 100}],
            format="pdf",
            correlation_id="corr-test-1234"
        )
        res = client.render_report(req)

        self.assertEqual(res.format, "pdf")
        self.assertEqual(res.content_type, "application/pdf")
        self.assertTrue(res.data.startswith(b"%PDF-"))
        self.assertEqual(res.correlation_id, "corr-test-1234")
        self.assertTrue(res.duration_ms >= 0)

    @patch("urllib.request.urlopen")
    def test_render_to_file_writes_to_disk(self, mock_urlopen):
        mock_resp = MagicMock()
        mock_resp.read.return_value = b"%PDF-1.4 File Output"
        mock_resp.headers = {"Content-Type": "application/pdf"}
        mock_resp.__enter__.return_value = mock_resp
        mock_urlopen.return_value = mock_resp

        client = BangplanixClient()
        out_path = os.path.join(os.path.dirname(__file__), "temp_test.pdf")
        res = client.render_to_file(RenderRequest(template_path="inv.bpx"), out_path)
        self.assertTrue(os.path.exists(out_path))
        os.remove(out_path)

    @patch("urllib.request.urlopen")
    def test_render_batch_executes_parallel_requests(self, mock_urlopen):
        mock_resp = MagicMock()
        mock_resp.read.return_value = b"%PDF-1.4 Batch item"
        mock_resp.headers = {"Content-Type": "application/pdf"}
        mock_resp.__enter__.return_value = mock_resp
        mock_urlopen.return_value = mock_resp

        client = BangplanixClient()
        requests = [RenderRequest(template_path=f"t_{i}.bpx") for i in range(4)]
        results = client.render_batch(requests, max_workers=2)

        self.assertEqual(len(results), 4)
        self.assertTrue(all(r["success"] for r in results))

    @patch("urllib.request.urlopen")
    def test_health_check_success(self, mock_urlopen):
        mock_resp = MagicMock()
        mock_resp.read.return_value = b'{"status": "Healthy"}'
        mock_resp.__enter__.return_value = mock_resp
        mock_urlopen.return_value = mock_resp

        client = BangplanixClient()
        res = client.health_check()
        self.assertEqual(res.get("status"), "Healthy")

    @patch("urllib.request.urlopen")
    def test_render_report_retry_on_503(self, mock_urlopen):
        mock_resp_fail = urllib.error.HTTPError(
            url="http://localhost:9545/api/v1/report/render",
            code=503,
            msg="Service Unavailable",
            hdrs={},
            fp=None
        )
        mock_resp_success = MagicMock()
        mock_resp_success.read.return_value = b"%PDF-1.4 Recovered PDF"
        mock_resp_success.headers = {"Content-Type": "application/pdf"}
        mock_resp_success.__enter__.return_value = mock_resp_success

        mock_urlopen.side_effect = [mock_resp_fail, mock_resp_success]

        client = BangplanixClient(retry_delay_sec=0.01)
        req = RenderRequest(template_path="retry_doc.bpx")
        res = client.render_report(req)

        self.assertEqual(res.data, b"%PDF-1.4 Recovered PDF")
        self.assertEqual(mock_urlopen.call_count, 2)

    @patch("urllib.request.urlopen")
    def test_validate_template(self, mock_urlopen):
        mock_resp = MagicMock()
        mock_resp.read.return_value = b'{"isValid": true, "errors": []}'
        mock_resp.__enter__.return_value = mock_resp
        mock_urlopen.return_value = mock_resp

        client = BangplanixClient()
        res = client.validate_template('{"version": "1.0"}')
        self.assertTrue(res.get("isValid"))

if __name__ == "__main__":
    unittest.main()