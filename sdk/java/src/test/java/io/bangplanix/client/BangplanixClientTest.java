package io.bangplanix.client;

import org.junit.jupiter.api.Test;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;
import com.sun.net.httpserver.HttpServer;

import static org.junit.jupiter.api.Assertions.*;

public class BangplanixClientTest {

    @Test
    public void testRenderReportSuccessWithMockServer() throws Exception {
        HttpServer server = HttpServer.create(new InetSocketAddress(0), 0);
        server.createContext("/api/v1/report/render", exchange -> {
            byte[] mockPdf = "%PDF-1.4 Mock Java PDF".getBytes();
            exchange.getResponseHeaders().set("Content-Type", "application/pdf");
            exchange.sendResponseHeaders(200, mockPdf.length);
            exchange.getResponseBody().write(mockPdf);
            exchange.close();
        });
        server.start();

        try {
            int port = server.getAddress().getPort();
            BangplanixClient client = new BangplanixClient("http://localhost:" + port);
            RenderRequest req = new RenderRequest.Builder()
                .templatePath("schema/v1/samples/invoice.bpx")
                .dataJson("{}")
                .format("pdf")
                .correlationId("java-corr-123")
                .build();

            RenderResponse res = client.renderReport(req);
            assertNotNull(res);
            assertEquals("pdf", res.getFormat());
            assertEquals("application/pdf", res.getContentType());
            assertEquals("java-corr-123", res.getCorrelationId());
            assertTrue(new String(res.getData()).startsWith("%PDF-"));
        } finally {
            server.stop(0);
        }
    }

    @Test
    public void testRenderToFileWritesFile() throws Exception {
        HttpServer server = HttpServer.create(new InetSocketAddress(0), 0);
        server.createContext("/api/v1/report/render", exchange -> {
            byte[] mockXlsx = "PK\u0003\u0004MockExcel".getBytes();
            exchange.getResponseHeaders().set("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            exchange.sendResponseHeaders(200, mockXlsx.length);
            exchange.getResponseBody().write(mockXlsx);
            exchange.close();
        });
        server.start();

        try {
            int port = server.getAddress().getPort();
            BangplanixClient client = new BangplanixClient("http://localhost:" + port);
            Path tempOut = Files.createTempFile("bangplanix_java_test", ".xlsx");

            RenderRequest req = new RenderRequest.Builder()
                .templatePath("schema/v1/samples/invoice.bpx")
                .format("xlsx")
                .build();

            RenderResponse res = client.renderToFile(req, tempOut);
            assertTrue(Files.exists(tempOut));
            assertTrue(Files.size(tempOut) > 0);
            Files.deleteIfExists(tempOut);
        } finally {
            server.stop(0);
        }
    }

    @Test
    public void testRenderBatchConcurrent() throws Exception {
        HttpServer server = HttpServer.create(new InetSocketAddress(0), 0);
        server.createContext("/api/v1/report/render", exchange -> {
            byte[] mockPdf = "%PDF-1.4 Batch Item".getBytes();
            exchange.getResponseHeaders().set("Content-Type", "application/pdf");
            exchange.sendResponseHeaders(200, mockPdf.length);
            exchange.getResponseBody().write(mockPdf);
            exchange.close();
        });
        server.start();

        try {
            int port = server.getAddress().getPort();
            BangplanixClient client = new BangplanixClient("http://localhost:" + port);
            List<RenderRequest> requests = List.of(
                new RenderRequest.Builder().templatePath("t1.bpx").build(),
                new RenderRequest.Builder().templatePath("t2.bpx").build(),
                new RenderRequest.Builder().templatePath("t3.bpx").build()
            );

            List<RenderResponse> results = client.renderBatch(requests, 2);
            assertEquals(3, results.size());
            for (RenderResponse r : results) {
                assertEquals("pdf", r.getFormat());
                assertTrue(new String(r.getData()).contains("Batch Item"));
            }
        } finally {
            server.stop(0);
        }
    }
}
