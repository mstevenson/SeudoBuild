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
            var moduleLoader = new ModuleLoaderFactory().Create();
            container.Register<IModuleLoader>(moduleLoader);

            var fs = new FileSystem();
            container.Register<IFileSystem>(fs);

            var queue = new BuildQueue();
            container.Register<IBuildQueue>(queue);
            var builder = new Builder();
            queue.StartQueue(builder, moduleLoader);

            try
            {
                // FIXME configure the port from a command line argument
                var serverInfo = new UdpDiscoveryResponse { port = 5511 };
                var discovery = new UdpDiscoveryServer(serverInfo);
                discovery.Start();
            }
            catch
            {
                BuildConsole.WriteAlert("Could not initialize build agent discovery beacon");
            }
        }
    }
}
