package bangplanix

import (
	"bytes"
	"context"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"math"
	"net/http"
	"os"
	"path/filepath"
	"strings"
	"sync"
	"time"
)

type RenderRequest struct {
	TemplatePath  string                 `json:"templatePath,omitempty"`
	TemplateJSON  string                 `json:"templateJson,omitempty"`
	DataJSON      string                 `json:"dataJson,omitempty"`
	Parameters    map[string]interface{} `json:"parameters,omitempty"`
	Format        string                 `json:"format,omitempty"` // "pdf" or "xlsx"
	CorrelationID string                 `json:"correlationId,omitempty"`
}

type RenderResponse struct {
	Data          []byte
	Format        string
	ContentType   string
	Length        int
	CorrelationID string
	DurationMs    int64
}

type BatchResult struct {
	Success  bool
	Result   *RenderResponse
	Error    string
	Request  RenderRequest
}

type Client struct {
	ServerURL   string
	HTTPClient  *http.Client
	APIKey      string
	MaxRetries  int
	RetryDelay  time.Duration
}

func NewClient(serverURL string) *Client {
	return &Client{
		ServerURL:  strings.TrimRight(serverURL, "/"),
		HTTPClient: &http.Client{Timeout: 30 * time.Second},
		MaxRetries: 3,
		RetryDelay: 200 * time.Millisecond,
	}
}

func (c *Client) RenderReport(ctx context.Context, req RenderRequest) (*RenderResponse, error) {
	if req.TemplatePath == "" && req.TemplateJSON == "" {
		return nil, errors.New("either TemplatePath or TemplateJSON must be provided")
	}

	if req.Format == "" {
		req.Format = "pdf"
	}
	req.Format = strings.ToLower(req.Format)

	if req.DataJSON == "" {
		req.DataJSON = "{}"
	}

	correlationID := req.CorrelationID
	if correlationID == "" {
		correlationID = generateUUID()
	}

	bodyBytes, err := json.Marshal(req)
	if err != nil {
		return nil, fmt.Errorf("failed to serialize request: %w", err)
	}

	endpoint := fmt.Sprintf("%s/api/v1/report/render", c.ServerURL)
	startTime := time.Now()
	attempt := 0
	var lastErr error

	for attempt <= c.MaxRetries {
		httpReq, err := http.NewRequestWithContext(ctx, http.MethodPost, endpoint, bytes.NewReader(bodyBytes))
		if err != nil {
			return nil, fmt.Errorf("failed to create http request: %w", err)
		}

		httpReq.Header.Set("Content-Type", "application/json")
		httpReq.Header.Set("X-Correlation-ID", correlationID)
		if c.APIKey != "" {
			httpReq.Header.Set("Authorization", "Bearer "+c.APIKey)
		}

		resp, err := c.HTTPClient.Do(httpReq)
		if err != nil {
			lastErr = err
			if attempt < c.MaxRetries {
				attempt++
				backoff := c.RetryDelay * time.Duration(math.Pow(2, float64(attempt-1)))
				select {
				case <-time.After(backoff):
					continue
				case <-ctx.Done():
					return nil, ctx.Err()
				}
			}
			return nil, fmt.Errorf("bangplanix request failed: %w", err)
		}

		if resp.StatusCode >= 400 {
			isTransient := resp.StatusCode == 429 || resp.StatusCode == 502 || resp.StatusCode == 503 || resp.StatusCode == 504
			errBody, _ := io.ReadAll(resp.Body)
			resp.Body.Close()

			if isTransient && attempt < c.MaxRetries {
				attempt++
				backoff := c.RetryDelay * time.Duration(math.Pow(2, float64(attempt-1)))
				select {
				case <-time.After(backoff):
					continue
				case <-ctx.Done():
					return nil, ctx.Err()
				}
			}
			return nil, fmt.Errorf("bangplanix render error [HTTP %d]: %s", resp.StatusCode, string(errBody))
		}

		data, err := io.ReadAll(resp.Body)
		resp.Body.Close()
		if err != nil {
			return nil, fmt.Errorf("failed to read response body: %w", err)
		}

		contentType := resp.Header.Get("Content-Type")
		if contentType == "" {
			if req.Format == "xlsx" {
				contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
			} else {
				contentType = "application/pdf"
			}
		}

		durationMs := time.Since(startTime).Milliseconds()
		return &RenderResponse{
			Data:          data,
			Format:        req.Format,
			ContentType:   contentType,
			Length:        len(data),
			CorrelationID: correlationID,
			DurationMs:    durationMs,
		}, nil
	}

	return nil, lastErr
}

func (c *Client) RenderToFile(ctx context.Context, req RenderRequest, outputPath string) (*RenderResponse, error) {
	resp, err := c.RenderReport(ctx, req)
	if err != nil {
		return nil, err
	}

	dir := filepath.Dir(outputPath)
	if dir != "" && dir != "." {
		if err := os.MkdirAll(dir, 0755); err != nil {
			return nil, err
		}
	}

	if err := os.WriteFile(outputPath, resp.Data, 0644); err != nil {
		return nil, err
	}

	return resp, nil
}

func (c *Client) RenderBatch(ctx context.Context, requests []RenderRequest, concurrencyLimit int) []BatchResult {
	if concurrencyLimit <= 0 {
		concurrencyLimit = 4
	}

	results := make([]BatchResult, len(requests))
	sem := make(chan struct{}, concurrencyLimit)
	var wg sync.WaitGroup

	for i, req := range requests {
		wg.Add(1)
		go func(idx int, r RenderRequest) {
			defer wg.Done()
			sem <- struct{}{}
			defer func() { <-sem }()

			res, err := c.RenderReport(ctx, r)
			if err != nil {
				results[idx] = BatchResult{Success: false, Error: err.Error(), Request: r}
			} else {
				results[idx] = BatchResult{Success: true, Result: res, Request: r}
			}
		}(i, req)
	}

	wg.Wait()
	return results
}

func generateUUID() string {
	b := make([]byte, 16)
	_, _ = rand.Read(b)
	return hex.EncodeToString(b)
}