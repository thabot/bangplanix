package io.bangplanix.client;

public class RenderResponse {
    private final byte[] data;
    private final String format;
    private final String contentType;
    private final int length;
    private final String correlationId;
    private final long durationMs;

    public RenderResponse(byte[] data, String format, String contentType, String correlationId, long durationMs) {
        this.data = data;
        this.format = format;
        this.contentType = contentType;
        this.length = data != null ? data.length : 0;
        this.correlationId = correlationId;
        this.durationMs = durationMs;
    }

    public byte[] getData() { return data; }
    public String getFormat() { return format; }
    public String getContentType() { return contentType; }
    public int getLength() { return length; }
    public String getCorrelationId() { return correlationId; }
    public long getDurationMs() { return durationMs; }
}