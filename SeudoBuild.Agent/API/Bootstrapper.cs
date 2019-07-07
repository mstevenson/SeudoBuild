using Nancy;
using SeudoBuild.Core;
using SeudoBuild.Core.FileSystems;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
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
            var moduleLoader = new ModuleLoaderFactory().Create(logger);
            container.Register(moduleLoader);

            // TODO support Linux
            IFileSystem fileSystem = PlatformUtils.RunningPlatform == Platform.Windows ? new WindowsFileSystem() : new MacFileSystem();
            container.Register(fileSystem);

            var builder = new Builder(moduleLoader, logger);
            var queue = new BuildQueue(builder, moduleLoader, logger);
            container.Register<IBuildQueue>(queue);
            queue.StartQueue(fileSystem);
        }
    }
}
