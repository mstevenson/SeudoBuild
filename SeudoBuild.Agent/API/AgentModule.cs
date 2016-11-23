using System;
using System.Net.Http;
using Nancy;
using Nancy.ModelBinding;
using System.IO;

namespace SeudoBuild.Agent
{
    public class AgentModule : NancyModule
    {
        public AgentModule(IBuildQueue buildQueue, IModuleLoader moduleLoader, IFileSystem filesystem)
        {
            // Build agent info
            Get["/"] = parameters =>
            {
                //moduleLoader.Registry.

                var proj = new Agent { AgentName = AgentName.GetUniqueAgentName() };
                return Response.AsJson(proj);
            };

            // Build the default target in a project configuration
            Post["/build"] = parameters =>
            {
                try
                {
                    ProjectConfig config = ProcessReceivedBuildRequest(Request, null, moduleLoader, filesystem);
                    BuildConsole.WriteLine($"Received build request for project '{config.ProjectName}' with default target from host {Request.UserHostAddress}");
                    var buildRequest = buildQueue.Build(config);
                    return buildRequest.Id.ToString();
                }
                catch (Exception e)
                {
                    BuildConsole.WriteFailure(e.Message);
                    return HttpStatusCode.BadRequest;
                }
            };

            // Build a specific target within a given project configuration
            Post["/build/{target}"] = parameters =>
            {
                try
                {
                    string target = parameters.value;
                    ProjectConfig config = ProcessReceivedBuildRequest(Request, target, moduleLoader, filesystem);
                    BuildConsole.WriteLine($"Queuing build request for project '{config.ProjectName}' with target '{target}' from host {Request.UserHostAddress}");
                    var buildRequest = buildQueue.Build(config, target);
                    return buildRequest.Id.ToString();
                }
                catch (Exception e)
                {
                    BuildConsole.WriteFailure(e.Message);
                    return HttpStatusCode.BadRequest;
                }
            };

            // Get info for a specific build task
            Post["/queue/{id:guid}"] = parameters =>
            {
                try
                {
                    Guid guid = Guid.Parse(parameters.value);
                    var result = buildQueue.GetBuildResult(guid);
                    return Response.AsJson(result);
                }
                catch
                {
                    return HttpStatusCode.BadRequest;
                }
            };

            // Cancel a build task
            Post["/queue/{id:guid}/cancel"] = parameters =>
            {
                try
                {
                    Guid guid = Guid.Parse(parameters.value);
                    buildQueue.CancelBuild(guid);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.BadRequest;
                }
            };
        }

        ProjectConfig ProcessReceivedBuildRequest(Request request, string target, IModuleLoader moduleLoader, IFileSystem filesystem)
        {
            // We'd ordinarily use Nancy's Bind method, but we need to use custom
            // JSON converters to propertly deserialize the ProjectConfig object
            string json = "";
            using (var sr = new StreamReader(request.Body))
            {
                json = sr.ReadToEnd();
            }
            var converters = moduleLoader.Registry.GetJsonConverters();
            var serializer = new Serializer(filesystem);
            var config = serializer.Deserialize<ProjectConfig>(json, converters);

            if (!string.IsNullOrEmpty(target))
            {
                if (!config.BuildTargets.Exists(t => t.TargetName == target))
                {
                    throw new Exception($"Received project configuration from {request.UserHostAddress} but could not find a build target named '{target}'");
                }
            }

            if (string.IsNullOrEmpty(config.ProjectName))
            {
                throw new Exception($"Received invalid project configuration from {request.UserHostAddress}");
            }

            return config;
        }
    }
}
