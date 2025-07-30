using Microsoft.AspNetCore.Mvc;
using Azure.AI.OpenAI;
using Azure;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuration for Azure OpenAI
var endpoint = "xxxxx";
var apiKey = "xxxxx";

builder.Services.AddSingleton(new AzureOpenAIClient(
	new Uri(endpoint),
	new AzureKeyCredential(apiKey)
));

var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/", () => "Hello to CV-JD Analyzer. Call the /analyze api for output.");

app.MapPost("/analyze", async ([FromServices] AzureOpenAIClient client, HttpRequest request) =>
{
	var logger = app.Services.GetRequiredService<ILogger<Program>>();

	try
	{
		logger.LogInformation("Starting document analysis request");

		if (!request.HasFormContentType)
		{
			logger.LogWarning("Invalid form data received");
			return Results.BadRequest("Invalid form data.");
		}

		var form = await request.ReadFormAsync();
		var file = form.Files.GetFile("cv");
		var jd = form["job_description"].ToString();

		if (file == null || string.IsNullOrEmpty(jd))
		{
			logger.LogWarning("Missing file or job description. File: {FileExists}, JD: {JDExists}",
				file != null, !string.IsNullOrEmpty(jd));
			return Results.BadRequest("Missing file or job description.");
		}

		logger.LogInformation("Processing file: {FileName}, Size: {FileSize} bytes",
			file.FileName, file.Length);

		string? cvText = await ExtractTextFromFileAsync(file, logger);

		if (string.IsNullOrWhiteSpace(cvText))
		{
			logger.LogError("Could not extract text from resume file: {FileName}", file.FileName);
			return Results.BadRequest("Could not extract text from resume.");
		}

		logger.LogInformation("Extracted text length: {TextLength} characters", cvText.Length);

		string prompt = $@"
Compare the following CV and job description. Identify missing or weak skills and give an ATS score (0-100). Format the output as JSON with fields: skillGaps[], atsScore.

CV:
{cvText}

Job Description:
{jd}
";

		logger.LogInformation("Sending request to Azure OpenAI");

		var chatClient = client.GetChatClient("gpt-4.1");

		// Fixed ChatMessage syntax
		var messages = new ChatMessage[]
		{
			ChatMessage.CreateSystemMessage("You are a helpful AI assistant specialized in CV analysis and ATS scoring."),
			ChatMessage.CreateUserMessage(prompt)
		};

		var chatCompletions = await chatClient.CompleteChatAsync(messages);

		if (chatCompletions?.Value?.Content == null || chatCompletions.Value.Content.Count == 0)
		{
			logger.LogError("No response received from Azure OpenAI");
			return Results.Problem("No response from AI service");
		}

		var result = chatCompletions.Value.Content[0].Text;

		logger.LogInformation("Received response from Azure OpenAI");

		// Validate JSON before parsing
		try
		{
			var jsonDocument = JsonDocument.Parse(result);
			logger.LogInformation("Successfully parsed AI response as JSON");
			return Results.Ok(jsonDocument.RootElement);
		}
		catch (JsonException ex)
		{
			logger.LogError(ex, "Failed to parse AI response as JSON. Response: {Response}", result);
			return Results.Problem("Invalid JSON response from AI service");
		}
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Unexpected error during document analysis");
		return Results.Problem("An unexpected error occurred");
	}
});

app.Run();

static async Task<string?> ExtractTextFromFileAsync(IFormFile file, ILogger logger)
{
	if (file == null)
	{
		logger.LogWarning("File is null");
		return null;
	}

	var extension = Path.GetExtension(file.FileName)?.ToLower();

	if (string.IsNullOrEmpty(extension))
	{
		logger.LogWarning("File has no extension: {FileName}", file.FileName);
		return null;
	}

	logger.LogInformation("Extracting text from {Extension} file", extension);

	try
	{
		using var stream = file.OpenReadStream();

		if (extension == ".pdf")
		{
			logger.LogInformation("Processing PDF file");

			using var document = PdfDocument.Open(stream);
			var sb = new StringBuilder();

			logger.LogInformation("PDF has {PageCount} pages", document.NumberOfPages);

			foreach (Page page in document.GetPages())
			{
				try
				{
					var pageText = page.Text;
					if (!string.IsNullOrWhiteSpace(pageText))
					{
						sb.AppendLine(pageText);
					}
					else
					{
						logger.LogWarning("No text found on page {PageNumber}", page.Number);
					}
				}
				catch (Exception pageEx)
				{
					logger.LogWarning(pageEx, "Failed to extract text from page {PageNumber}", page.Number);
					// Continue with other pages even if one fails
				}
			}

			var extractedText = sb.ToString();
			logger.LogInformation("Extracted {TextLength} characters from PDF", extractedText.Length);

			if (string.IsNullOrWhiteSpace(extractedText))
			{
				logger.LogWarning("No text could be extracted from PDF file");
			}

			return extractedText;
		}
		else if (extension == ".docx")
		{
			logger.LogInformation("Processing DOCX file");

			using var ms = new MemoryStream();
			await stream.CopyToAsync(ms);
			ms.Position = 0; // Reset position after copying

			using var doc = WordprocessingDocument.Open(ms, false);

			if (doc?.MainDocumentPart?.Document?.Body == null)
			{
				logger.LogWarning("Could not access document body");
				return null;
			}

			var extractedText = doc.MainDocumentPart.Document.Body.InnerText;
			logger.LogInformation("Extracted {TextLength} characters from DOCX", extractedText?.Length ?? 0);

			return extractedText;
		}
		else
		{
			logger.LogWarning("Unsupported file extension: {Extension}", extension);
			return null;
		}
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Error extracting text from file: {FileName}", file.FileName);
		return null;
	}
}