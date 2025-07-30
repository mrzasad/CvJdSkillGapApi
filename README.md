# CV-JD Analyzer

A .NET 8 Web API that analyzes CVs/resumes against job descriptions using Azure OpenAI to provide skill gap analysis and ATS scoring.

## Features

- 📄 **Multi-format Document Support**: Supports PDF (.pdf) and Word (.docx) file formats
- 🤖 **AI-Powered Analysis**: Uses Azure OpenAI GPT-4.1 for intelligent CV analysis
- 📊 **ATS Scoring**: Provides Applicant Tracking System compatibility scores (0-100)
- 🔍 **Skill Gap Analysis**: Identifies missing or weak skills compared to job requirements
- 📝 **JSON Response**: Returns structured analysis results in JSON format
- 🛡️ **Error Handling**: Comprehensive error handling and logging

## Prerequisites

- .NET 8 SDK
- Azure OpenAI Service account with GPT-4.1 model deployment
- Visual Studio 2022 or VS Code (recommended)

## Installation

### 1. Clone the repository

### 2. Install Required NuGet Packages
```bash
dotnet add package Azure.AI.OpenAI
dotnet add package DocumentFormat.OpenXml
dotnet add package PdfPig
```

Or add these to your `.csproj` file:
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
<PackageReference Include="DocumentFormat.OpenXml" Version="3.1.0" />
<PackageReference Include="PdfPig" Version="0.1.8" />
```

### 3. Configure Azure OpenAI
Edit `Program.cs` and replace the placeholder values:

```csharp
var endpoint = "xxxxx";
var apiKey = "xxxxx";
```

Ensure you have a GPT-4.1 model deployed named `gpt-4.1` or just change the name of the deployment model in the program.cs to your available one.

### 4. Run the Application
```bash
cd ./CvJdSkillGapApi/
dotnet run
```

The API will be available at `http://localhost:5161` (or the port shown in console output).

## API Usage

### Endpoint
```
POST /analyze
```

### Request Format
Send a `multipart/form-data` request with:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `cv` | File | Yes | CV/Resume file (.pdf or .docx) |
| `job_description` | Text | Yes | Job description text |

### Example Request (cURL)
```bash
curl -X POST http://localhost:5161/analyze \
  -F "cv=@resume.pdf" \
  -F "job_description=We are looking for a Senior Software Developer with experience in C#, .NET, Azure, and microservices architecture..."
```

### Example Request (Postman)
1. Set method to `POST`
2. URL: `http://localhost:5161/analyze`
3. Go to Body → form-data
4. Add key `cv` (type: File) and select your CV file
5. Add key `job_description` (type: Text) and paste the job description

### Response Format
```json
{
  "skillGaps": [
    "Machine Learning frameworks (TensorFlow, PyTorch)",
    "Kubernetes orchestration",
    "Advanced DevOps practices"
  ],
  "atsScore": 75
}
```

### Response Fields
- `skillGaps`: Array of strings listing missing or weak skills
- `atsScore`: Integer from 0-100 indicating ATS compatibility

## Supported File Formats

- **PDF (.pdf)**: Uses PdfPig library for accurate text extraction
- **Word (.docx)**: Uses DocumentFormat.OpenXml for text extraction

## Development

### Project Structure
```
├── Program.cs              # Main application entry point
├── CV-JD-Analyzer.csproj   # Project file with dependencies
└── README.md              # This file
```

### Key Components
- **File Processing**: Handles PDF and DOCX text extraction
- **Azure OpenAI Integration**: Manages API calls to GPT-4.1
- **Error Handling**: Comprehensive exception handling and logging
- **Validation**: Input validation and null checks

### Common Issues

1. **"Could not extract text from resume"**
   - Ensure the PDF/DOCX file is not corrupted
   - Check if the file contains extractable text (not just images)
   - Verify file size is reasonable (< 10MB recommended)

2. **Azure OpenAI Connection Issues**
   - Verify your API key and endpoint URL
   - Ensure your Azure OpenAI resource is active
   - Check that GPT-4 model is deployed and accessible

3. **File Upload Issues**
   - Ensure you're sending `multipart/form-data`
   - Check file field name is exactly `cv`
   - Verify job description field name is exactly `job_description`

## License

This project is licensed under the MIT License.

---

**Note**: This is a backend API only. You'll need to create a frontend application or use tools like Postman/cURL to interact with the API.