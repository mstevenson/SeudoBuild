using Nancy;
using SeudoBuild.Net;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // http://stackoverflow.com/questions/34660245/nancy-create-singleton-with-constructor-parameters

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            StaticConfiguration.DisableErrorTraces = false;

            base.ConfigureApplicationContainer(container);
            // FIXME the entrypoint method also initializes a logger
            var logger = new BuildConsole();
            container.Register<ILogger>(logger);
            var moduleLoader = new ModuleLoaderFactory().Create(logger);
            container.Register<IModuleLoader>(moduleLoader);

            var fs = new FileSystem();
            container.Register<IFileSystem>(fs);

            var queue = new BuildQueue(logger);
            container.Register<IBuildQueue>(queue);
            var builder = new Builder();
            queue.StartQueue(builder, moduleLoader);


        }
    }
}
