package io.bangplanix.client;

import java.util.Collections;
import java.util.Map;

public class RenderRequest {
    private final String templatePath;
    private final String templateJson;
    private final String dataJson;
    private final Map<String, Object> parameters;
    private final String format;
    private final String correlationId;

    private RenderRequest(Builder builder) {
        this.templatePath = builder.templatePath;
        this.templateJson = builder.templateJson;
        this.dataJson = builder.dataJson != null ? builder.dataJson : "{}";
        this.parameters = builder.parameters != null ? builder.parameters : Collections.emptyMap();
        this.format = builder.format != null ? builder.format.toLowerCase() : "pdf";
        this.correlationId = builder.correlationId;
    }

    public String getTemplatePath() { return templatePath; }
    public String getTemplateJson() { return templateJson; }
    public String getDataJson() { return dataJson; }
    public Map<String, Object> getParameters() { return parameters; }
    public String getFormat() { return format; }
    public String getCorrelationId() { return correlationId; }

    public static Builder builder() { return new Builder(); }

    public static class Builder {
        private String templatePath;
        private String templateJson;
        private String dataJson;
        private Map<String, Object> parameters;
        private String format = "pdf";
        private String correlationId;

        public Builder templatePath(String templatePath) { this.templatePath = templatePath; return this; }
        public Builder templateJson(String templateJson) { this.templateJson = templateJson; return this; }
        public Builder dataJson(String dataJson) { this.dataJson = dataJson; return this; }
        public Builder parameters(Map<String, Object> parameters) { this.parameters = parameters; return this; }
        public Builder format(String format) { this.format = format; return this; }
        public Builder correlationId(String correlationId) { this.correlationId = correlationId; return this; }

        public RenderRequest build() {
            if (templatePath == null && templateJson == null) {
                throw new IllegalArgumentException("Either templatePath or templateJson must be provided.");
            }
            return new RenderRequest(this);
        }
    }
}