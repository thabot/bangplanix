package io.bangplanix.client;

import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class BangplanixClient {
    private final String serverUrl;
    private final HttpClient httpClient;
    private final Duration timeout;
    private final int maxRetries;
    private final Duration retryDelay;

    public BangplanixClient(String serverUrl) {
        this(serverUrl, Duration.ofSeconds(30), HttpClient.newHttpClient(), 3, Duration.ofMillis(200));
    }

    public BangplanixClient(String serverUrl, Duration timeout, HttpClient httpClient, int maxRetries, Duration retryDelay) {
        this.serverUrl = serverUrl.replaceAll("/+$", "");
        this.timeout = timeout;
        this.httpClient = httpClient != null ? httpClient : HttpClient.newHttpClient();
        this.maxRetries = maxRetries;
        this.retryDelay = retryDelay != null ? retryDelay : Duration.ofMillis(200);
    }

    public RenderResponse renderReport(RenderRequest request) throws IOException, InterruptedException {
        String correlationId = request.getCorrelationId() != null ? request.getCorrelationId() : UUID.randomUUID().toString();
        String jsonPayload = String.format(
            "{\"templatePath\":%s,\"templateJson\":%s,\"dataJson\":%s,\"format\":\"%s\"}",
            request.getTemplatePath() != null ? "\"" + escape(request.getTemplatePath()) + "\"" : "null",
            request.getTemplateJson() != null ? "\"" + escape(request.getTemplateJson()) + "\"" : "null",
            "\"" + escape(request.getDataJson()) + "\"",
            request.getFormat()
        );

        long startTime = System.currentTimeMillis();
        int attempt = 0;
        IOException lastException = null;

        while (attempt <= maxRetries) {
            try {
                HttpRequest httpRequest = HttpRequest.newBuilder()
                    .uri(URI.create(serverUrl + "/api/v1/report/render"))
                    .timeout(timeout)
                    .header("Content-Type", "application/json")
                    .header("X-Correlation-ID", correlationId)
                    .POST(HttpRequest.BodyPublishers.ofString(jsonPayload))
                    .build();

                HttpResponse<byte[]> response = httpClient.send(httpRequest, HttpResponse.BodyHandlers.ofByteArray());
                int status = response.statusCode();

                if (status >= 400) {
                    boolean isTransient = (status == 429 || status == 502 || status == 503 || status == 504);
                    if (isTransient && attempt < maxRetries) {
                        attempt++;
                        Thread.sleep(retryDelay.toMillis() * (long) Math.pow(2, attempt - 1));
                        continue;
                    }
                    throw new IOException("Bangplanix render error [HTTP " + status + "]: " + new String(response.body()));
                }

                String contentType = response.headers().firstValue("Content-Type")
                    .orElse(request.getFormat().equalsIgnoreCase("xlsx") ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf");

                long duration = System.currentTimeMillis() - startTime;
                return new RenderResponse(response.body(), request.getFormat(), contentType, correlationId, duration);
            } catch (IOException ex) {
                lastException = ex;
                if (attempt < maxRetries) {
                    attempt++;
                    Thread.sleep(retryDelay.toMillis() * (long) Math.pow(2, attempt - 1));
                    continue;
                }
                throw lastException;
            }
        }

        throw lastException != null ? lastException : new IOException("Render request failed after retries.");
    }

    public RenderResponse renderToFile(RenderRequest request, Path outputPath) throws IOException, InterruptedException {
        RenderResponse response = renderReport(request);
        if (outputPath.getParent() != null && !Files.exists(outputPath.getParent())) {
            Files.createDirectories(outputPath.getParent());
        }
        Files.write(outputPath, response.getData());
        return response;
    }

    public List<RenderResponse> renderBatch(List<RenderRequest> requests, int maxThreads) {
        ExecutorService executor = Executors.newFixedThreadPool(Math.min(maxThreads, requests.size()));
        try {
            List<CompletableFuture<RenderResponse>> futures = requests.stream()
                .map(req -> CompletableFuture.supplyAsync(() -> {
                    try {
                        return renderReport(req);
                    } catch (Exception e) {
                        throw new RuntimeException(e);
                    }
                }, executor))
                .toList();

            return futures.stream().map(CompletableFuture::join).toList();
        } finally {
            executor.shutdown();
        }
    }

    private String escape(String raw) {
        return raw.replace("\\", "\\\\").replace("\"", "\\\"");
    }
}