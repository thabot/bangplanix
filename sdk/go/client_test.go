package bangplanix

import (
	"context"
	"net/http"
	"net/http/httptest"
	"os"
	"sync/atomic"
	"testing"
	"time"
)

func TestClient_MissingTemplate_ReturnsError(t *testing.T) {
	client := NewClient("http://localhost:9545")
	_, err := client.RenderReport(context.Background(), RenderRequest{})
	if err == nil {
		t.Fatal("expected error for missing template, got nil")
	}
}

func TestClient_RenderReport_SuccessWithTelemetry(t *testing.T) {
	var capturedCorrelationID string
	mockServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/v1/report/render" {
			t.Errorf("unexpected path: %s", r.URL.Path)
		}
		capturedCorrelationID = r.Header.Get("X-Correlation-ID")
		w.Header().Set("Content-Type", "application/pdf")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("%PDF-1.4 Mock Go PDF"))
	}))
	defer mockServer.Close()

	client := NewClient(mockServer.URL)
	res, err := client.RenderReport(context.Background(), RenderRequest{
		TemplatePath:  "schema/v1/samples/invoice.bpx",
		Format:        "pdf",
		CorrelationID: "go-corr-999",
	})

	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	if res.Format != "pdf" {
		t.Errorf("expected format pdf, got %s", res.Format)
	}

	if res.CorrelationID != "go-corr-999" || capturedCorrelationID != "go-corr-999" {
		t.Errorf("expected correlation ID go-corr-999, got %s", res.CorrelationID)
	}

	if string(res.Data) != "%PDF-1.4 Mock Go PDF" {
		t.Errorf("unexpected body content: %s", string(res.Data))
	}
}

func TestClient_RenderToFile_WritesFile(t *testing.T) {
	mockServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/pdf")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("%PDF-1.4 Disk Output"))
	}))
	defer mockServer.Close()

	client := NewClient(mockServer.URL)
	tempPath := "./temp_go_test.pdf"
	res, err := client.RenderToFile(context.Background(), RenderRequest{TemplatePath: "inv.bpx"}, tempPath)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	if res.Length != len("%PDF-1.4 Disk Output") {
		t.Errorf("expected length %d, got %d", len("%PDF-1.4 Disk Output"), res.Length)
	}

	_ = os.Remove(tempPath)
}

func TestClient_RetryLogic_SucceedsAfterTransientFailure(t *testing.T) {
	var attempts int32
	mockServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		count := atomic.AddInt32(&attempts, 1)
		if count < 2 {
			w.WriteHeader(http.StatusServiceUnavailable)
			w.Write([]byte("Service temporarily unavailable"))
			return
		}
		w.Header().Set("Content-Type", "application/pdf")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("%PDF-1.4 Retry Success"))
	}))
	defer mockServer.Close()

	client := NewClient(mockServer.URL)
	client.RetryDelay = 10 * time.Millisecond
	res, err := client.RenderReport(context.Background(), RenderRequest{
		TemplatePath: "sample.bpx",
	})

	if err != nil {
		t.Fatalf("expected success after retry, got err: %v", err)
	}
	if string(res.Data) != "%PDF-1.4 Retry Success" {
		t.Errorf("unexpected body: %s", string(res.Data))
	}
	if atomic.LoadInt32(&attempts) != 2 {
		t.Errorf("expected 2 attempts, got %d", attempts)
	}
}

func TestClient_RenderBatch_ExecutesConcurrently(t *testing.T) {
	mockServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/pdf")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("%PDF-1.4 Batch Item"))
	}))
	defer mockServer.Close()

	client := NewClient(mockServer.URL)
	requests := []RenderRequest{
		{TemplatePath: "doc1.bpx"},
		{TemplatePath: "doc2.bpx"},
		{TemplatePath: "doc3.bpx"},
	}

	results := client.RenderBatch(context.Background(), requests, 2)
	if len(results) != 3 {
		t.Fatalf("expected 3 batch results, got %d", len(results))
	}

	for i, item := range results {
		if !item.Success {
			t.Errorf("expected batch item %d to succeed, got error: %s", i, item.Error)
		}
		if string(item.Result.Data) != "%PDF-1.4 Batch Item" {
			t.Errorf("unexpected batch data: %s", string(item.Result.Data))
		}
	}
}