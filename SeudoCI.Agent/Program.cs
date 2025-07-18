﻿using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SeudoCI.Core;
using SeudoCI.Core.FileSystems;
using SeudoCI.Pipeline;
using SeudoCI.Net;

namespace SeudoCI.Agent;

public static class Program
{
    private const string Header = @"
                _     _       _ _   _ 
  ___ ___ _ _ _| |___| |_ _ _|_| |_| |
 |_ -| -_| | | . | . | . | | | | | . |
 |___|___|___|___|___|___|___|_|_|___|
                                      
";

    private static ILogger _logger = null!;

    [Verb("build", HelpText = "Create a local build.")]
    private class BuildSubOptions
    {
        [Option('t', "build-target", HelpText = "Name of the build target as specified in the project configuration file. If no build target is specified, the first target will be used.")]
        public string BuildTarget { get; set; } = string.Empty;

        [Option('o', "output-folder", HelpText = "Path to the build output folder.")]
        public string OutputPath { get; set; } = string.Empty;

        [Value(0, MetaName = "project", HelpText = "Path to a project configuration file.", Required = true)]
        public string ProjectConfigPath { get; set; } = string.Empty;
    }

    [Verb("scan", HelpText = "List build agents found on the local network.")]
    private class ScanSubOptions
    {
    }

    [Verb("submit", HelpText = "Submit a build request for a remote build agent to fulfill.")]
    private class SubmitSubOptions
    {
        [Option('p', "project-config", HelpText = "Path to a project configuration file.", Required = true)]
        public string ProjectConfigPath { get; set; } = string.Empty;

        [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.")]
        public string BuildTarget { get; set; } = string.Empty;

        [Option('a', "agent-name", HelpText = "The unique name of a specific build agent. If not set, the job will be broadcast to all available agents.")]
        public string AgentName { get; set; } = string.Empty;
    }

    [Verb("queue", HelpText = "Queue build requests received over the network.")]
    private class QueueSubOptions
    {
        [Option('n', "agent-name", HelpText = "A unique name for the build agent. If not set, a name will be generated.")]
        public string AgentName { get; set; } = string.Empty;

        [Option('p', "port", HelpText = "Port on which to listen for build queue messages.")]
        public int? Port { get; set; }
    }

    [Verb("deploy", HelpText = "Listen for deployment messages.")]
    private class DeploySubOptions
    {
    }

    [Verb("name", Hidden = true)]
    private class NameSubOptions
    {
        [Option('r', "random")]
        public bool Random { get; set; }
    }

    public static async Task<int> Main(string[] args)
    {
        _logger = new Logger();

        Console.Title = "SeudoCI";

        return await Parser.Default.ParseArguments<BuildSubOptions, ScanSubOptions, SubmitSubOptions, QueueSubOptions, DeploySubOptions, NameSubOptions>(args)
            .MapResult(
                (BuildSubOptions opts) => Task.FromResult(Build(opts)),
                (ScanSubOptions opts) => Task.FromResult(Scan(opts)),
                (SubmitSubOptions opts) => Task.FromResult(Submit(opts)),
                (QueueSubOptions opts) => Queue(opts),
                (DeploySubOptions opts) => Task.FromResult(Deploy(opts)),
                (NameSubOptions opts) => Task.FromResult(ShowAgentName(opts)),
                errs => Task.FromResult(1)
            );
    }

    /// <summary>
    /// Build a single target, then exit.
    /// </summary>
    private static int Build(BuildSubOptions opts)
    {
        Console.Title = "SeudoCI • Build";
        Console.WriteLine(Header);

        // Load pipeline modules
        IModuleLoader moduleLoader = ModuleLoaderFactory.Create(_logger);

        // Load project config
        ProjectConfig projectConfig;
        try
        {
            var fs = new WindowsFileSystem();
            var serializer = new Serializer(fs);
            var converters = moduleLoader.Registry.GetJsonConverters();
            projectConfig = serializer.DeserializeFromFile<ProjectConfig>(opts.ProjectConfigPath, converters);
        }
        catch (Exception e)
        {
            Console.WriteLine("Can't parse project config:");
            Console.WriteLine(e.Message);
            return 1;
        }

        // Execute build
        var builder = new Builder(moduleLoader, _logger);

        var parentDirectory = opts.OutputPath;
        if (string.IsNullOrEmpty(parentDirectory))
        {
            // Config file's directory
            parentDirectory = new FileInfo(opts.ProjectConfigPath).Directory?.FullName;
        }

        if (string.IsNullOrEmpty(parentDirectory))
        {
            Console.WriteLine("Could not determine output directory");
            return 1;
        }

        var pipeline = new PipelineRunner(new PipelineConfig { BaseDirectory = parentDirectory }, _logger);
        bool success = builder.Build(pipeline, projectConfig, opts.BuildTarget);

        return success ? 0 : 1;
    }

    /// <summary>
    /// Discover build agents on the network.
    /// </summary>
    private static int Scan(ScanSubOptions opts)
    {
        Console.Title = "SeudoCI • Scan";
        Console.WriteLine(Header);

        Console.WriteLine("Looking for build agents. Press any key to exit.");
        // FIXME fill in port from command line argument
        var client = new AgentDiscoveryClient();
        try
        {
            client.Start();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not start build agent discovery client");
            Console.ResetColor();
            return 1;
        }
        // FIXME don't hard-code port
        // locator.AgentFound += (agent) =>
        // {
        //     _logger.Write($"{agent.AgentName} ({agent.Address})", LogType.Bullet);
        // };
        // locator.AgentLost += (agent) =>
        // {
        //     _logger.Write($"Lost agent: {agent.AgentName} ({agent.Address})", LogType.Bullet);
        // };
        Console.WriteLine();
        Console.ReadKey();
        return 0;
    }

    /// <summary>
    /// Submit a build job to another agent.
    /// </summary>
    private static int Submit(SubmitSubOptions opts)
    {
        Console.Title = "SeudoCI • Submit";
        Console.WriteLine(Header);

        string? configJson = null;
        try
        {
            configJson = File.ReadAllText(opts.ProjectConfigPath);
        }
        catch
        {
            _logger.Write("Project could not be read from " + opts.ProjectConfigPath, LogType.Failure);
            return 1;
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SeudoCI-Agent/1.0");
        var buildSubmitter = new BuildSubmitter(_logger, httpClient);
        try
        {
            // Find agent on the network, with timeout
            var discoveryClient = new AgentDiscoveryClient();
            var success = buildSubmitter.SubmitAsync(discoveryClient, configJson, opts.BuildTarget, opts.AgentName).GetAwaiter().GetResult();
            return success ? 0 : 1;
        }
        catch (Exception e)
        {
            _logger.Write("Could not submit job: " + e.Message, LogType.Failure);
            return 1;
        }
    }

    /// <summary>
    /// Receive build jobs from other agents or clients, queue them, and execute them.
    /// Continue listening until user exits.
    /// </summary>
    private static async Task<int> Queue(QueueSubOptions opts)
    {
        Console.Title = "SeudoCI • Queue";
        Console.WriteLine(Header);

        //string agentName = string.IsNullOrEmpty(opts.AgentName) ? AgentName.GetUniqueAgentName() : opts.AgentName;
        // FIXME pull port from command line argument, and incorporate into ServerBeacon object
        ushort port = 5511;
        if (opts.Port.HasValue)
        {
            port = (ushort)opts.Port.Value;
        }

        // Create ASP.NET Core web application
        var builder = WebApplication.CreateBuilder();
        
        // Configure services
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.ListenLocalhost(port);
        });
        
        builder.Services.AddControllers();
        
        // Register core services as singletons since they maintain state
        builder.Services.AddSingleton<ILogger, Logger>();

        // Register file system based on platform
        builder.Services.AddSingleton<IFileSystem>(serviceProvider =>
        {
            return PlatformUtils.RunningPlatform == Platform.Windows 
                ? new WindowsFileSystem() 
                : new MacFileSystem();
        });

        // Register module loader as singleton
        builder.Services.AddSingleton<IModuleLoader>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            return ModuleLoaderFactory.Create(logger);
        });

        // Configure HttpClient
        builder.Services.AddHttpClient();

        // Register builder and build queue
        builder.Services.AddSingleton<Builder>(serviceProvider =>
        {
            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            return new Builder(moduleLoader, logger);
        });

        builder.Services.AddSingleton<IBuildQueue>(serviceProvider =>
        {
            var builderService = serviceProvider.GetRequiredService<Builder>();
            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
            
            var queue = new BuildQueue(builderService, moduleLoader, logger);
            queue.StartQueue(fileSystem);
            return queue;
        });
        
        var app = builder.Build();
        
        // Configure middleware pipeline
        app.UseRouting();
        app.MapControllers();

        await using (app);
        
        _logger.Write("");
        try
        {
            await app.StartAsync();

            var uri = new Uri($"http://localhost:{port}");
            _logger.Write("Build Queue", LogType.Header);
            _logger.Write("");
            _logger.Write("Started build agent server: " + uri, LogType.Bullet);

            try
            {
                var name = AgentName.GetUniqueAgentName();
                var discovery = new AgentDiscoveryServer(name, port);
                discovery.Start();
                _logger.Write($"Build agent server started: {name} {port}", LogType.Bullet);
            }
            catch
            {
                _logger.Write("Could not initialize build agent server", LogType.Alert);
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not start build server: " + e.Message);
            Console.ResetColor();
            return 1;
        }
        Console.WriteLine("");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
        
        await app.StopAsync();

        return 0;
    }

    /// <summary>
    /// Deploy a build product on the local machine.
    /// </summary>
    private static int Deploy(DeploySubOptions opts)
    {
        return 0;
    }

    /// <summary>
    /// Display the unique name for this agent.
    /// </summary>
    private static int ShowAgentName(NameSubOptions opts)
    {
        var name = opts.Random ? AgentName.GetRandomName() : AgentName.GetUniqueAgentName();
        Console.WriteLine();
        Console.WriteLine(name);
        Console.WriteLine();
        return 0;
    }
}