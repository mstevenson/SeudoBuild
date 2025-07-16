# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Pipeline.Modules.EmailNotify project.

## Project Overview

SeudoCI.Pipeline.Modules.EmailNotify is a notification module for the SeudoCI build system that sends email notifications when build pipelines complete. This module implements the INotifyModule interface and provides email delivery capabilities using SMTP protocols. It serves as the final step in the build pipeline, informing stakeholders about build completion status and results.

## Build Commands

- **Build email notification module**: `dotnet build SeudoCI.Pipeline.Modules.EmailNotify/SeudoCI.Pipeline.Modules.EmailNotify.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Pipeline.Modules.EmailNotify/SeudoCI.Pipeline.Modules.EmailNotify.csproj`
- **Run tests**: `dotnet test` (when test projects reference this module)

## Architecture Overview

The EmailNotify module follows the standard SeudoCI module pattern with two main components:

1. **EmailNotifyModule**: Module registration and metadata provider
2. **EmailNotifyStep**: Email sending implementation and SMTP client management
3. **EmailNotifyConfig**: Configuration schema (in companion Shared project)

## Core Components

### EmailNotifyModule

**Location**: `EmailNotifyModule.cs`

**Purpose**: Registers the email notification module with the SeudoCI pipeline system and provides module metadata.

#### Module Registration Properties
```csharp
public string Name => "Email";
public Type StepType { get; } = typeof(EmailNotifyStep);
public Type StepConfigType { get; } = typeof(EmailNotifyConfig);
public string StepConfigName => "Email Notification";
```

#### Key Features
- **Module Identity**: Provides "Email" as the module name for discovery
- **Type Mapping**: Links configuration type to implementation type
- **Pipeline Integration**: Enables automatic module loading and registration

### EmailNotifyStep

**Location**: `EmailNotifyStep.cs`

**Purpose**: Implements email notification functionality using MailKit SMTP client for reliable email delivery.

#### Key Features
- **SMTP Email Delivery**: Uses MailKit library for robust email sending
- **Macro Variable Support**: Expands project and build variables in email content
- **Error Handling**: Graceful handling of SMTP connection and authentication failures
- **Security Configuration**: Supports SMTP authentication with custom certificate validation

#### Core Methods

**`Initialize(EmailNotifyConfig config, ITargetWorkspace workspace, ILogger logger)`**
- Stores configuration, workspace, and logger references for step execution
- Prepares module for email notification operations

**`ExecuteStep(DistributeSequenceResults distributeResults, ITargetWorkspace workspace)`**
- Main execution method called by pipeline runner
- Constructs email subject with macro variable expansion
- Sends notification email with build completion details
- Returns success/failure status with exception details

#### Email Content Generation
```csharp
string subject = "Build Completed • %project_name% • %build_target_name%";
subject = workspace.Macros.ReplaceVariablesInText(subject);
```

**Standard Subject Template**: "Build Completed • {ProjectName} • {TargetName}"
**Body Content**: Simple completion message with build duration

#### SMTP Implementation Details

**`SendMessage(string fromAddress, string toAddress, string subject, string body)`**
- Creates MimeMessage with plain text content
- Establishes SMTP connection with configurable timeout (10 seconds)
- Disables certificate validation for internal/development SMTP servers
- Removes OAuth2 authentication mechanism (uses username/password only)
- Handles connection, authentication, and message sending

#### SMTP Configuration
```csharp
client.Timeout = 10000;
client.ServerCertificateValidationCallback = (s, c, h, e) => true;
client.Connect(_config.Host, _config.Port, false);
client.AuthenticationMechanisms.Remove("XOAUTH2");
client.Authenticate(_config.SMTPUser, _config.SMTPPassword);
```

#### Security Considerations
- **Certificate Validation Disabled**: Accepts all SSL certificates (development-friendly)
- **No OAuth2 Support**: Uses traditional username/password authentication
- **Plain Text Authentication**: SMTP credentials stored in configuration
- **No TLS Enforcement**: Connects without mandatory encryption

### EmailNotifyConfig

**Location**: `SeudoCI.Pipeline.Modules.EmailNotify.Shared/EmailNotifyConfig.cs`

**Purpose**: Defines configuration schema for email notification settings.

#### Configuration Properties

**Email Addresses**
- `FromAddress`: Sender email address
- `ToAddress`: Recipient email address

**SMTP Server Settings**
- `Host`: SMTP server hostname (default: "smtp.google.com")
- `Port`: SMTP server port (default: 587)

**Authentication**
- `SMTPUser`: SMTP username for authentication
- `SMTPPassword`: SMTP password for authentication

#### Default Configuration
```json
{
  "Name": "Email Notification",
  "Host": "smtp.google.com",
  "Port": 587,
  "FromAddress": "builds@company.com",
  "ToAddress": "developers@company.com",
  "SMTPUser": "smtp_username",
  "SMTPPassword": "smtp_password"
}
```

## Pipeline Integration

### Execution Context
EmailNotify executes as the final phase of the SeudoCI pipeline:
```
Source → Build → Archive → Distribute → **Notify**
```

### Input Requirements
- **DistributeSequenceResults**: Results from distribution phase containing:
  - Build completion status
  - Distribution duration timing
  - Success/failure information

### Output Generation
- **NotifyStepResults**: Notification execution results containing:
  - Success/failure status
  - Exception details (if failed)
  - Execution timing information

### Macro Variable Expansion
The module automatically expands standard SeudoCI macro variables in email content:
- `%project_name%` - Project identifier
- `%build_target_name%` - Build target name
- `%build_date%` - Build timestamp
- `%app_version%` - Application version
- `%commit_id%` - Source control commit hash

## Dependencies

### External Dependencies
- **MailKit 4.5.0**: Cross-platform email client library
  - SMTP protocol implementation
  - MIME message construction
  - SSL/TLS security support

### Internal Dependencies
- **SeudoCI.Pipeline.Shared**: Pipeline interfaces and base classes
- **SeudoCI.Pipeline.Modules.EmailNotify.Shared**: Configuration types
- **SeudoCI.Core**: Logging and workspace management

### Framework Dependencies
- **.NET 8.0**: Target framework
- **System namespaces**: Basic .NET functionality

## Configuration Examples

### Basic Email Configuration
```json
{
  "Type": "Email Notification",
  "FromAddress": "noreply@company.com",
  "ToAddress": "team@company.com",
  "Host": "smtp.company.com",
  "Port": 587,
  "SMTPUser": "build-notifications",
  "SMTPPassword": "secure-password"
}
```

### Gmail Configuration
```json
{
  "Type": "Email Notification",
  "FromAddress": "builds@gmail.com",
  "ToAddress": "developers@gmail.com",
  "Host": "smtp.gmail.com",
  "Port": 587,
  "SMTPUser": "builds@gmail.com",
  "SMTPPassword": "app-specific-password"
}
```

### Multiple Recipients Pattern
**Note**: Current implementation supports single recipient only. For multiple recipients, configure multiple EmailNotify steps:

```json
{
  "NotifySteps": [
    {
      "Type": "Email Notification",
      "ToAddress": "team-lead@company.com",
      "FromAddress": "builds@company.com",
      "Host": "smtp.company.com",
      "Port": 587,
      "SMTPUser": "notifications",
      "SMTPPassword": "password"
    },
    {
      "Type": "Email Notification", 
      "ToAddress": "qa-team@company.com",
      "FromAddress": "builds@company.com",
      "Host": "smtp.company.com",
      "Port": 587,
      "SMTPUser": "notifications",
      "SMTPPassword": "password"
    }
  ]
}
```

## Common Issues and Solutions

### SMTP Authentication Failures
- **Issue**: "Authentication failed" errors
- **Solution**: Verify SMTP username/password credentials
- **Gmail Specific**: Use app-specific passwords instead of account passwords

### SSL Certificate Errors
- **Issue**: SSL certificate validation failures
- **Current Behavior**: All certificates accepted (insecure)
- **Improvement**: Implement proper certificate validation for production

### Connection Timeouts
- **Issue**: SMTP server connection timeouts
- **Current Setting**: 10-second timeout
- **Solution**: Verify SMTP server hostname and port accessibility

### OAuth2 Compatibility
- **Issue**: Some SMTP servers require OAuth2 authentication
- **Limitation**: Module explicitly removes OAuth2 authentication
- **Workaround**: Use SMTP servers supporting username/password authentication

## Security Considerations

### Current Security Limitations
1. **Disabled Certificate Validation**: Accepts all SSL certificates
2. **Plain Text Passwords**: SMTP credentials stored in configuration files
3. **No OAuth2 Support**: Limited to basic authentication methods
4. **No Encryption Requirements**: Doesn't enforce TLS/SSL connections

### Recommended Security Improvements
1. **Enable Certificate Validation**: Implement proper SSL certificate checking
2. **Secure Credential Storage**: Use environment variables or secure vaults
3. **OAuth2 Integration**: Support modern authentication mechanisms
4. **Mandatory Encryption**: Require TLS/SSL for all SMTP connections

## Development Patterns

### Adding Email Template Support
```csharp
// Enhanced email content generation
private string GenerateEmailBody(DistributeSequenceResults distributeResults)
{
    var template = _config.EmailTemplate ?? DefaultTemplate;
    return workspace.Macros.ReplaceVariablesInText(template);
}
```

### Supporting Multiple Recipients
```csharp
// Multiple recipient support
public class EmailNotifyConfig : NotifyStepConfig
{
    public string[] ToAddresses { get; set; }
    public string[] CcAddresses { get; set; }
    public string[] BccAddresses { get; set; }
}
```

### Error Handling Best Practices
- Always return NotifyStepResults with appropriate success/failure status
- Log SMTP errors at appropriate levels for debugging
- Provide actionable error messages for configuration issues
- Handle network connectivity issues gracefully

## Performance Considerations

### Email Delivery Optimization
- **Connection Reuse**: Current implementation creates new connection per email
- **Batch Sending**: Consider connection pooling for multiple notifications
- **Async Operations**: Email sending blocks pipeline completion

### Memory Management
- **Message Disposal**: MimeMessage objects properly disposed
- **Connection Cleanup**: SMTP client connections properly closed
- **Large Attachments**: Current implementation doesn't support attachments

## Future Enhancement Opportunities

1. **Rich Email Templates**: HTML email support with custom templates
2. **Attachment Support**: Include build logs or artifacts in emails
3. **Multiple Recipients**: Native support for multiple email addresses
4. **OAuth2 Authentication**: Modern authentication for cloud email providers
5. **Email Queuing**: Asynchronous email delivery with retry logic
6. **Conditional Notifications**: Send emails only on build failures or success
7. **Custom Subject Templates**: User-configurable email subject patterns