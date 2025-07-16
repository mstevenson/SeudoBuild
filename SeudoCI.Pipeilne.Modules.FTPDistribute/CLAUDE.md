# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Pipeline.Modules.FTPDistribute project.

## Project Overview

SeudoCI.Pipeline.Modules.FTPDistribute is a distribution pipeline module that enables automatic uploading of build artifacts to FTP servers as part of the SeudoCI build pipeline. This module handles the final distribution phase, taking archived build outputs and transferring them to remote FTP locations for deployment, sharing, or storage.

## Build Commands

- **Build FTP distribute module**: `dotnet build SeudoCI.Pipeilne.Modules.FTPDistribute/SeudoCI.Pipeilne.Modules.FTPDistribute.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Pipeilne.Modules.FTPDistribute/SeudoCI.Pipeilne.Modules.FTPDistribute.csproj`

**Note**: The project directory name has a typo ("Pipeilne" instead of "Pipeline") which should be noted when working with file paths.

## Module Architecture

This module follows the standard SeudoCI module pattern with separate projects for implementation and configuration:

- **Implementation Project**: `SeudoCI.Pipeilne.Modules.FTPDistribute` (this project)
- **Configuration Project**: `SeudoCI.Pipeline.Modules.FTPDistribute.Shared`

## Core Components

### FTPDistributeConfig

**Location**: `../SeudoCI.Pipeline.Modules.FTPDistribute.Shared/FTPDistributeConfig.cs`

**Purpose**: Configuration class that defines FTP connection parameters and upload settings.

#### Properties
- `string Name` - Always returns "FTP Upload" (override from base class)
- `string URL` - FTP server hostname or IP address
- `int Port` - FTP server port (defaults to 21)
- `string BasePath` - Remote directory path where files will be uploaded
- `string Username` - FTP authentication username
- `string Password` - FTP authentication password (stored in plain text)

#### Configuration Example
```json
{
  "Type": "FTPDistributeConfig",
  "URL": "ftp.example.com",
  "Port": 21,
  "BasePath": "/builds/myproject",
  "Username": "builduser",
  "Password": "secretpassword"
}
```

### FTPDistributeStep

**Location**: `FTPDistributeStep.cs`

**Purpose**: Implements the actual FTP upload logic for archived build artifacts.

#### Key Features
- **Multiple file upload**: Processes all files from archive sequence results
- **Individual file tracking**: Records success/failure for each uploaded file
- **Streaming upload**: Uses 16KB buffer for efficient large file transfers
- **Binary transfer mode**: Ensures data integrity for compiled binaries
- **Connection management**: Maintains persistent FTP connections during upload

#### Implementation Details

**Initialization**:
```csharp
public void Initialize(FTPDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
{
    _config = config;
    _logger = logger;
}
```

**Execution Flow**:
1. Iterates through all archived files from previous pipeline step
2. Creates FTP connection for each file upload
3. Streams file content in 16KB chunks
4. Tracks individual file upload results
5. Logs upload status and completion

**FTP Connection Setup**:
```csharp
FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{_config.URL}:{_config.Port}/{_config.BasePath}/{archiveInfo.ArchiveFileName}");
request.Credentials = new NetworkCredential(_config.Username, _config.Password);
request.Method = WebRequestMethods.Ftp.UploadFile;
request.UseBinary = true;
request.KeepAlive = true;
```

**Streaming Upload Logic**:
- Uses `FileStream` to read source files
- 16KB buffer for memory-efficient transfers
- Direct stream copying to FTP request stream
- Proper resource disposal with stream closing

### FTPDistributeModule

**Location**: `FTPDistributeModule.cs`

**Purpose**: Module registration class that integrates the FTP distribute functionality into the SeudoCI pipeline system.

#### Module Registration Properties
- `string Name` - Returns "FTP" for module identification
- `Type StepType` - References `FTPDistributeStep` implementation
- `Type StepConfigType` - References `FTPDistributeConfig` configuration
- `string StepConfigName` - Returns "FTP Upload" for UI display

## Pipeline Integration

### Step Type
This module implements `IDistributeStep<FTPDistributeConfig>`, placing it in the **Distribute** phase of the SeudoCI pipeline sequence:

**Pipeline Order**: Source → Build → Archive → **Distribute** → Notify

### Input Requirements
- **Archive Results**: Requires successful archive step completion
- **Archive Files**: Uploads files specified in `ArchiveSequenceResults`
- **Workspace Access**: Reads from `TargetDirectory.Archives` workspace folder

### Output Results
Returns `DistributeStepResults` containing:
- Overall success/failure status
- Individual file upload results with success flags and error messages
- Exception details for troubleshooting failed uploads

## Usage in Build Pipeline

### Configuration in Project JSON
```json
{
  "BuildTargets": [
    {
      "TargetName": "Production",
      "DistributeSteps": [
        {
          "Type": "FTPDistributeConfig", 
          "URL": "deploy.mycompany.com",
          "Port": 21,
          "BasePath": "/web/builds/%project_name%/%build_target_name%",
          "Username": "deploy_user",
          "Password": "deploy_password"
        }
      ]
    }
  ]
}
```

### Macro Support
The `BasePath` configuration supports SeudoCI macro expansion:
- `%project_name%` - Project identifier
- `%build_target_name%` - Build target name
- `%commit_id%` - Source control commit hash
- `%build_date%` - Build completion timestamp

## Technical Implementation Details

### FTP Protocol Usage
- **Protocol**: Standard FTP (RFC 959)
- **Transfer Mode**: Binary mode for file integrity
- **Authentication**: Username/password (plain text)
- **Connection**: Uses .NET `FtpWebRequest` class
- **Default Port**: 21 (configurable)

### File Transfer Process
1. **File Discovery**: Reads archive file list from previous pipeline step
2. **Connection Setup**: Creates authenticated FTP connection per file
3. **Upload Preparation**: Sets binary mode and content length
4. **Streaming Transfer**: 16KB buffered upload for memory efficiency
5. **Validation**: Confirms upload success via FTP response status
6. **Cleanup**: Properly closes streams and connections

### Error Handling
- **Individual File Failures**: Captured and recorded without stopping entire process
- **Connection Errors**: Network and authentication failures logged
- **Stream Errors**: File access and transfer issues handled gracefully
- **Aggregate Results**: Overall step fails if any individual file fails

## Dependencies

### Internal Dependencies
- **SeudoCI.Pipeline.Modules.FTPDistribute.Shared**: Configuration classes
- **SeudoCI.Pipeline.Shared**: Base interfaces and result types
- **SeudoCI.Core**: Logging and workspace management

### Framework Dependencies
- **.NET 8.0**: Target framework
- **System.Net**: FTP connectivity via `FtpWebRequest`
- **System.IO**: File system access and streaming

### No External Packages
This module uses only built-in .NET FTP capabilities, avoiding external dependencies that could complicate deployment.

## Security Considerations

### Credential Storage
**Warning**: FTP credentials are stored in plain text in configuration files. Consider:
- Using environment variables for sensitive deployments
- Implementing configuration encryption for production use
- Restricting file system access to configuration files
- Using dedicated FTP accounts with minimal privileges

### Network Security
- **Plain Text Protocol**: FTP transmits credentials and data unencrypted
- **Firewall Configuration**: Ensure FTP ports (21, passive ports) are properly configured
- **Network Isolation**: Use private networks or VPNs when possible
- **Account Security**: Use dedicated build accounts with restricted server access

## Troubleshooting

### Common Issues

**Connection Failures**:
- Verify FTP server accessibility and port configuration
- Check firewall rules for FTP traffic
- Validate username/password credentials
- Ensure FTP server supports binary transfers

**Upload Failures**:
- Check remote directory permissions and existence
- Verify sufficient disk space on FTP server
- Validate file paths and naming conventions
- Monitor for network timeouts during large transfers

**Configuration Errors**:
- Ensure BasePath uses forward slashes (Unix-style paths)
- Verify macro expansion in remote paths
- Check for special characters in filenames
- Validate URL format excludes protocol prefix (no "ftp://")

### Known Limitations

1. **No FTPS/SFTP Support**: Only supports plain FTP protocol
2. **Sequential Uploads**: Files uploaded one at a time (no parallel transfers)
3. **No Resume Capability**: Failed uploads restart from beginning
4. **Limited Error Recovery**: No automatic retry logic for temporary failures
5. **File System Abstraction**: Direct FileInfo usage instead of IFileSystem (noted as FIXME)

## Future Enhancement Opportunities

### Security Improvements
- FTPS (FTP over SSL/TLS) support
- SFTP (SSH File Transfer Protocol) implementation
- Configuration encryption for credentials
- Token-based authentication where supported

### Performance Optimizations
- Parallel file uploads for multiple archives
- Resumable upload support for large files
- Connection pooling for multiple file transfers
- Compression support during transfer

### Feature Enhancements
- Directory creation for non-existent remote paths
- File versioning and backup management
- Upload progress reporting for large files
- Conditional uploads based on file modification dates