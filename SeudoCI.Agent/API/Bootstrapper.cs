namespace SeudoCI.Agent;

using Nancy;
using Core;
using Core.FileSystems;
using Pipeline;
using System.Net.Http;
using Services;

/// <inheritdoc />
/// <summary>
/// Entry point for Agent's RESTful API.
/// </summary>
public class Bootstrapper : DefaultNancyBootstrapper
{
    // http://stackoverflow.com/questions/34660245/nancy-create-singleton-with-constructor-parameters

    protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
    {
//            StaticConfiguration.DisableErrorTraces = false;

        base.ConfigureApplicationContainer(container);
        
        // FIXME the entry point method also initializes a logger
        var logger = new Logger();
        container.Register<ILogger>(logger);
        
        var moduleLoader = ModuleLoaderFactory.Create(logger);
        container.Register(moduleLoader);

        // TODO support Linux
        IFileSystem fileSystem = PlatformUtils.RunningPlatform == Platform.Windows ? new WindowsFileSystem() : new MacFileSystem();
        container.Register(fileSystem);

        // Configure HttpClient with proper settings
        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SeudoCI-Agent/1.0");
        container.Register(httpClient);

        // Register HTTP service abstraction
        var httpService = new HttpService(httpClient);
        container.Register<IHttpService>(httpService);

        var builder = new Builder(moduleLoader, logger);
        var queue = new BuildQueue(builder, moduleLoader, logger);
        container.Register<IBuildQueue>(queue);
        queue.StartQueue(fileSystem);
    }
}