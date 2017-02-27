using Nancy;
using SeudoBuild.Net;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Entrypoint for Agent's RESTful API.
    /// </summary>
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // http://stackoverflow.com/questions/34660245/nancy-create-singleton-with-constructor-parameters

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            StaticConfiguration.DisableErrorTraces = false;

            base.ConfigureApplicationContainer(container);
            // FIXME the entrypoint method also initializes a logger
            var logger = new Logger();
            container.Register<ILogger>(logger);
            var moduleLoader = new ModuleLoaderFactory().Create(logger);
            container.Register<IModuleLoader>(moduleLoader);

            var fs = new FileSystem();
            container.Register<IFileSystem>(fs);

            var builder = new Builder(moduleLoader, logger);
            var queue = new BuildQueue(builder, moduleLoader, logger);
            container.Register<IBuildQueue>(queue);
            queue.StartQueue();
        }
    }
}
