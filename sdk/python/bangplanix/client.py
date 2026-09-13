import json
import time
import uuid
import urllib.request
import urllib.error
from dataclasses import dataclass, field
from typing import Optional, Dict, Any, Union, List
from concurrent.futures import ThreadPoolExecutor, as_completed

@dataclass
class RenderRequest:
    template_path: Optional[str] = None
    template_json: Optional[str] = None
    data: Optional[Union[Dict[str, Any], List[Dict[str, Any]], str]] = None
    parameters: Dict[str, Any] = field(default_factory=dict)
    format: str = "pdf"
    correlation_id: Optional[str] = None

@dataclass
class RenderResponse:
    data: bytes
    format: str
    content_type: str
    length: int
    correlation_id: str
    duration_ms: int

class BangplanixClient:
    def __init__(self, server_url: str = "http://localhost:9545", timeout: int = 30, api_key: Optional[str] = None, max_retries: int = 3, retry_delay_sec: float = 0.2):
        self.server_url = server_url.rstrip('/')
        self.timeout = timeout
        self.api_key = api_key
        self.max_retries = max_retries
        self.retry_delay_sec = retry_delay_sec

    def render_report(self, request: RenderRequest) -> RenderResponse:
        if not request.template_path and not request.template_json:
            raise ValueError("Either template_path or template_json must be provided.")

        data_json = "{}"
        if isinstance(request.data, str):
            data_json = request.data
        elif request.data is not None:
            data_json = json.dumps(request.data)

        correlation_id = request.correlation_id or str(uuid.uuid4())
        payload = {
            "templatePath": request.template_path,
            "templateJson": request.template_json,
            "dataJson": data_json,
            "parameters": request.parameters,
            "format": request.format.lower()
        }

        endpoint = f"{self.server_url}/api/v1/report/render"
        body_bytes = json.dumps(payload).encode("utf-8")

        headers = {
            "Content-Type": "application/json",
            "X-Correlation-ID": correlation_id
        }
        if self.api_key:
            headers["Authorization"] = f"Bearer {self.api_key}"

        start_time = time.time()
        attempt = 0
        last_exception = None

        while attempt <= self.max_retries:
            req = urllib.request.Request(endpoint, data=body_bytes, headers=headers, method="POST")
            try:
                with urllib.request.urlopen(req, timeout=self.timeout) as resp:
                    data = resp.read()
                    content_type = resp.headers.get("Content-Type", "application/pdf" if request.format == "pdf" else "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    duration_ms = int((time.time() - start_time) * 1000)
                    return RenderResponse(
                        data=data,
                        format=request.format.lower(),
                        content_type=content_type,
                        length=len(data),
                        correlation_id=correlation_id,
                        duration_ms=duration_ms
                    )
            except urllib.error.HTTPError as ex:
                last_exception = ex
                if ex.code in (429, 502, 503, 504) and attempt < self.max_retries:
                    attempt += 1
                    backoff = self.retry_delay_sec * (2 ** (attempt - 1))
                    time.sleep(backoff)
                    continue
                error_body = ex.read().decode("utf-8", errors="ignore")
                raise RuntimeError(f"Bangplanix render error [HTTP {ex.code}]: {error_body or ex.reason}") from ex
            except Exception as ex:
                last_exception = ex
                if attempt < self.max_retries:
                    attempt += 1
                    backoff = self.retry_delay_sec * (2 ** (attempt - 1))
                    time.sleep(backoff)
                    continue
                raise

        raise last_exception

    def render_to_file(self, request: RenderRequest, output_path: str) -> RenderResponse:
        res = self.render_report(request)
        with open(output_path, "wb") as f:
            f.write(res.data)
        return res

    def render_batch(self, requests: List[RenderRequest], max_workers: int = 4) -> List[Dict[str, Any]]:
        results = []
        with ThreadPoolExecutor(max_workers=max_workers) as executor:
            future_to_req = {executor.submit(self.render_report, req): req for req in requests}
            for future in as_completed(future_to_req):
                req = future_to_req[future]
                try:
                    res = future.result()
                    results.append({"success": True, "result": res})
                except Exception as ex:
                    results.append({"success": False, "error": str(ex), "request": req})
        return results

    def validate_template(self, template_json_or_path: str) -> Dict[str, Any]:
        is_json = template_json_or_path.strip().startswith("{")
        payload = {"templateJson": template_json_or_path} if is_json else {"templatePath": template_json_or_path}
        endpoint = f"{self.server_url}/api/v1/template/validate"
        body_bytes = json.dumps(payload).encode("utf-8")
        headers = {"Content-Type": "application/json"}

        req = urllib.request.Request(endpoint, data=body_bytes, headers=headers, method="POST")
        try:
            with urllib.request.urlopen(req, timeout=self.timeout) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as ex:
            return {"isValid": False, "errors": [f"HTTP {ex.code}: {ex.reason}"]}

    def health_check(self) -> Dict[str, Any]:
        req = urllib.request.Request(f"{self.server_url}/health", method="GET")
        try:
            with urllib.request.urlopen(req, timeout=self.timeout) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as ex:
            return {"status": "unhealthy", "httpStatus": ex.code}