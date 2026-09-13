using System;
using System.IO;
using Bangplanix.Adapters.Crystal;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure maximum multipart upload size (100MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600;
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "bangplanix-crystal-worker",
    version = "1.0.0",
    engine = "headless-wine-bridge",
    timestamp = DateTime.UtcNow
}));

app.MapPost("/convert", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Request must be multipart/form-data with a 'file' field." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);

    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "No report file provided in request." });
    }

    try
    {
        using var stream = file.OpenReadStream();
        var adapter = new CrystalReportsXmlAdapter();
        var report = adapter.Convert(stream);

        var json = BpxParser.ToJson(report, indented: false);
        return Results.Content(json, "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Crystal Reports Conversion Failed"
        );
    }
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
