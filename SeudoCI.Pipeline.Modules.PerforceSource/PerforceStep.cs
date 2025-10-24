using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.PerforceSource;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Core;
using Perforce.P4;
using Shared;

public class PerforceStep : ISourceStep<PerforceConfig>
{
    private readonly SyncFilesCmdOptions _defaultSyncOptions = new(SyncFilesCmdFlags.None, -1, -1, -1, -1, -1, -1);
    private readonly SyncFilesCmdOptions _forcedSyncOptions = new(SyncFilesCmdFlags.Force, -1, -1, -1, -1, -1, -1);

    private PerforceConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;

    private Repository? _repository;
    private Connection? _connection;
    private Client? _client;
    private bool _isConnected;

    public SourceStepResults ExecuteStep(ITargetWorkspace workspace)
    {
        var results = new SourceStepResults();

        try
        {
            ValidateConfiguration();
            EnsureConnection();

            if (IsWorkingCopyInitialized)
            {
                Update();
            }
            else
            {
                Download();
            }

            results.CommitIdentifier = CurrentCommit;
            results.IsSuccess = true;
            _logger.Write("Perforce workspace synchronized successfully", LogType.Success);
        }
        catch (ArgumentException ex)
        {
            results.IsSuccess = false;
            results.Exception = ex;
            _logger.Write($"Configuration error: {ex.Message}", LogType.Failure);
        }
        catch (P4Exception ex)
        {
            results.IsSuccess = false;
            results.Exception = ex;
            _logger.Write($"Perforce operation failed: {ex.Message}", LogType.Failure);
        }
        catch (TimeoutException ex)
        {
            results.IsSuccess = false;
            results.Exception = ex;
            _logger.Write("Perforce operation timed out", LogType.Failure);
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.Exception = ex;
            _logger.Write("An unexpected error occurred while synchronizing Perforce", LogType.Failure);
        }
        finally
        {
            Disconnect();
        }

        return results;
    }

    public string? Type => "Perforce";

    public bool IsWorkingCopyInitialized
    {
        get
        {
            var sourcePath = _workspace.GetDirectory(TargetDirectory.Source);

            if (!_workspace.FileSystem.DirectoryExists(sourcePath))
            {
                return false;
            }

            return _workspace.FileSystem.GetFiles(sourcePath).Any() ||
                   _workspace.FileSystem.GetDirectories(sourcePath).Any();
        }
    }

    public string CurrentCommit
    {
        get
        {
            try
            {
                EnsureConnection();

                if (_repository == null || _client == null)
                {
                    return string.Empty;
                }

                var clientName = _client.Name;
                var options = new ChangesCmdOptions(ChangesCmdFlags.None, clientName, 1, ChangeListStatus.Submitted, _config.User,
                    -1);
                var fileSpecs = new[] { FileSpec.ClientSpec($"//{clientName}/...") };
                var changes = _repository.GetChangelists(options, fileSpecs);
                var change = changes?.FirstOrDefault();
                return change?.Id.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Write($"Unable to determine Perforce changelist: {ex.Message}", LogType.Debug);
                return string.Empty;
            }
        }
    }

    [UsedImplicitly]
    public void Initialize(PerforceConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
    }

    public void Download()
    {
        _logger.Write("Preparing for initial Perforce sync", LogType.Debug);
        _workspace.CleanDirectory(TargetDirectory.Source);

        EnsureConnection();
        SyncWorkspace(_forcedSyncOptions, "Performing initial Perforce sync with force option");
    }

    public void Update()
    {
        EnsureConnection();
        SyncWorkspace(_defaultSyncOptions, "Updating existing Perforce workspace");
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Server))
        {
            throw new ArgumentException("Perforce server address cannot be empty");
        }

        if (!Regex.IsMatch(_config.Server, "^[A-Za-z0-9:@./_-]+$"))
        {
            throw new ArgumentException("Perforce server address contains invalid characters");
        }

        if (string.IsNullOrWhiteSpace(_config.User))
        {
            throw new ArgumentException("Perforce username cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(_config.Pass))
        {
            throw new ArgumentException("Perforce password cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(_config.Client))
        {
            throw new ArgumentException("Perforce client workspace cannot be empty");
        }
    }

    private void EnsureConnection()
    {
        if (_isConnected && _repository != null && _connection != null && _client != null)
        {
            return;
        }

        var server = new Server(new ServerAddress(_config.Server));
        _repository = new Repository(server);
        _connection = _repository.Connection;
        _connection.UserName = _config.User;

        _logger.Write($"Connecting to Perforce server {_config.Server}", LogType.Debug);

        if (!_connection.Connect(null))
        {
            throw new InvalidOperationException("Failed to connect to Perforce server");
        }

        _connection.Login(_config.Pass);

        _client = _repository.GetClient(_config.Client)
                  ?? throw new InvalidOperationException($"Perforce client '{_config.Client}' was not found");

        var sourcePath = _workspace.GetDirectory(TargetDirectory.Source);
        if (!_workspace.FileSystem.DirectoryExists(sourcePath))
        {
            _workspace.FileSystem.CreateDirectory(sourcePath);
        }

        if (!string.Equals(NormalizePath(_client.Root), NormalizePath(sourcePath), StringComparison.Ordinal))
        {
            _client.Root = sourcePath;
            _repository.UpdateClient(_client);
            _logger.Write($"Updated Perforce client root to '{sourcePath}'", LogType.Debug);
        }

        _client.Initialize(_connection);
        _connection.SetClient(_client.Name);
        _isConnected = true;
    }

    private void SyncWorkspace(SyncFilesCmdOptions options, string message)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Perforce client is not initialized");
        }

        _logger.Write(message, LogType.SmallBullet);
        var results = _client.SyncFiles(options, new[] { FileSpec.ClientSpec($"//{_client.Name}/...") });

        if (results == null)
        {
            throw new InvalidOperationException("Perforce sync returned no results");
        }

        _logger.Write($"Perforce sync processed {results.Count} records", LogType.Debug);
        _logger.Write("Perforce sync completed", LogType.Success);
    }

    private void Disconnect()
    {
        if (_connection == null)
        {
            return;
        }

        try
        {
            if (_isConnected)
            {
                _connection.Disconnect();
            }
        }
        catch (P4Exception ex)
        {
            _logger.Write($"Failed to disconnect from Perforce cleanly: {ex.Message}", LogType.Debug);
        }
        finally
        {
            _isConnected = false;
        }
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}
