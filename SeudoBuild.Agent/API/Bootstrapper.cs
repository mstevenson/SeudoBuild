using System;
using Nancy;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // http://stackoverflow.com/questions/34660245/nancy-create-singleton-with-constructor-parameters

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            StaticConfiguration.DisableErrorTraces = false;

            base.ConfigureApplicationContainer(container);
            container.Register<IModuleLoader>(new ModuleLoaderFactory().Create());

            var fs = new FileSystem();
            container.Register<IFileSystem>(fs);

            try
            {
                var serverInfo = new ServerInfo();
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
