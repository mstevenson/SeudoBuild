namespace SeudoCI.Agent;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Core;
using Core.FileSystems;
using Pipeline;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        
        // Register core services as singletons since they maintain state
        services.AddSingleton<ILogger, Logger>();

        // Register file system based on platform
        services.AddSingleton<IFileSystem>(_ => PlatformUtils.RunningPlatform == Platform.Windows 
            ? new WindowsFileSystem() 
            : new MacFileSystem());

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