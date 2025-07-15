namespace SeudoCI.Agent.Tests;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeudoCI.Agent;
using SeudoCI.Core;
using SeudoCI.Core.FileSystems;
using SeudoCI.Pipeline;

/// <summary>
/// Test web application factory for integration tests with SeudoCI.Agent API.
/// This factory provides a configured test environment with mocked dependencies.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<TestStartup>
{
    /// <summary>
    /// Mock logger that can be used to verify logging behavior in tests.
    /// </summary>
    public Core.ILogger MockLogger { get; private set; } = null!;

    /// <summary>
    /// Mock build queue that can be used to setup test scenarios.
    /// </summary>
    public IBuildQueue MockBuildQueue { get; private set; } = null!;

    /// <summary>
    /// Mock module loader for testing module-related functionality.
    /// </summary>
    public IModuleLoader MockModuleLoader { get; private set; } = null!;

    /// <summary>
    /// Mock file system for testing file operations.
    /// </summary>
    public IFileSystem MockFileSystem { get; private set; } = null!;


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing registrations
            RemoveService<Core.ILogger>(services);
            RemoveService<IBuildQueue>(services);
            RemoveService<IModuleLoader>(services);
            RemoveService<IFileSystem>(services);
            RemoveService<Builder>(services);

            // Create and register mock services
            MockLogger = Substitute.For<Core.ILogger>();
            MockBuildQueue = Substitute.For<IBuildQueue>();
            MockModuleLoader = Substitute.For<IModuleLoader>();
            MockFileSystem = Substitute.For<IFileSystem>();

            services.AddSingleton(MockLogger);
            services.AddSingleton(MockBuildQueue);
            services.AddSingleton(MockModuleLoader);
            services.AddSingleton(MockFileSystem);

            // Register Builder with mocked dependencies
            services.AddSingleton<Builder>(serviceProvider =>
                new Builder(MockModuleLoader, MockLogger));

            // Setup default module registry behavior
            var mockRegistry = Substitute.For<IModuleRegistry>();
            mockRegistry.GetJsonConverters().Returns([]);
            MockModuleLoader.Registry.Returns(mockRegistry);

            // Setup default file system behavior
            MockFileSystem.DocumentsPath.Returns("/tmp/test-documents");
            MockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
        });

        // Use test environment
        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Remove a service type from the service collection.
    /// </summary>
    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Setup a successful build queue scenario for testing.
    /// </summary>
    public void SetupSuccessfulBuildQueue()
    {
        var projectConfig = new ProjectConfig
        {
            ProjectName = "TestProject"
        };
        projectConfig.BuildTargets.Add(new BuildTargetConfig { TargetName = "Debug" });

        var buildResult = new BuildResult
        {
            Id = 123,
            ProjectConfiguration = projectConfig,
            TargetName = "Debug",
            BuildStatus = BuildResult.Status.Queued
        };

        MockBuildQueue.EnqueueBuild(Arg.Any<ProjectConfig>()).Returns(buildResult);
        MockBuildQueue.EnqueueBuild(Arg.Any<ProjectConfig>(), Arg.Any<string>()).Returns(buildResult);
        MockBuildQueue.GetBuildResult(123).Returns(buildResult);
        MockBuildQueue.GetAllBuildResults().Returns([buildResult]);
    }

    /// <summary>
    /// Setup an empty build queue scenario for testing.
    /// </summary>
    public void SetupEmptyBuildQueue()
    {
        MockBuildQueue.GetAllBuildResults().Returns([]);
        MockBuildQueue.GetBuildResult(Arg.Any<int>()).Returns((BuildResult)null);
    }

    /// <summary>
    /// Setup build queue to throw exceptions for testing error scenarios.
    /// </summary>
    public void SetupBuildQueueExceptions()
    {
        MockBuildQueue.When(x => x.EnqueueBuild(Arg.Any<ProjectConfig>()))
                     .Do(x => throw new InvalidOperationException("Build queue is full"));
        
        MockBuildQueue.When(x => x.EnqueueBuild(Arg.Any<ProjectConfig>(), Arg.Any<string>()))
                     .Do(x => throw new InvalidOperationException("Build queue is full"));
        
        MockBuildQueue.When(x => x.GetAllBuildResults())
                     .Do(x => throw new InvalidOperationException("Database connection failed"));
    }

    /// <summary>
    /// Setup module loader with specific configuration for testing.
    /// </summary>
    public void SetupModuleLoader()
    {
        var mockRegistry = Substitute.For<IModuleRegistry>();
        mockRegistry.GetJsonConverters().Returns([]);
        MockModuleLoader.Registry.Returns(mockRegistry);
    }

    /// <summary>
    /// Reset all mock configurations to their default state.
    /// </summary>
    public void ResetMocks()
    {
        MockLogger.ClearReceivedCalls();
        MockBuildQueue.ClearReceivedCalls();
        MockModuleLoader.ClearReceivedCalls();
        MockFileSystem.ClearReceivedCalls();
        
        // Reset to default behaviors
        SetupModuleLoader();
        MockFileSystem.DocumentsPath.Returns("/tmp/test-documents");
        MockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(true);
    }
}

/// <summary>
/// Test startup class for web application factory.
/// </summary>
public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        
        // Register core services as singletons since they maintain state
        services.AddSingleton<ILogger, Logger>();

        // Register file system based on platform
        services.AddSingleton<IFileSystem>(serviceProvider =>
        {
            return PlatformUtils.RunningPlatform == Platform.Windows 
                ? new WindowsFileSystem() 
                : new MacFileSystem();
        });

        // Register module loader as singleton
        services.AddSingleton<IModuleLoader>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            return ModuleLoaderFactory.Create(logger);
        });

        // Configure HttpClient
        services.AddHttpClient();

        // Register builder and build queue
        services.AddSingleton<Builder>(serviceProvider =>
        {
            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            return new Builder(moduleLoader, logger);
        });

        services.AddSingleton<IBuildQueue>(serviceProvider =>
        {
            var builderService = serviceProvider.GetRequiredService<Builder>();
            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
            
            var queue = new BuildQueue(builderService, moduleLoader, logger);
            queue.StartQueue(fileSystem);
            return queue;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}