using System;
using System.Net.Http;
using Nancy;
using Nancy.ModelBinding;

namespace SeudoBuild.Agent
{
    public class AgentModule : NancyModule
    {
        public AgentModule(IBuildQueue buildQueue)
        {
            // Build agent info
            Get["/"] = parameters =>
            {
                var proj = new Agent { AgentName = AgentName.GetUniqueAgentName() };
                return Response.AsJson(proj);
            };

            // Build the default target in a project configuration
            Post["/build"] = parameters =>
            {
                var config = this.Bind<ProjectConfig>();
                if (string.IsNullOrEmpty(config.ProjectName))
                {
                    BuildConsole.WriteFailure($"Received invalid project configuration from {Request.UserHostAddress}");
                    return HttpStatusCode.BadRequest;
                }

                BuildConsole.WriteLine("Received build request for project: " + config.ProjectName);

                var buildRequest = buildQueue.Build(config, parameters.value);
                return buildRequest.Id.ToString();
            };

            // Build a specific target within a given project configuration
            Post["/build/{target}"] = parameters =>
            {
                var projectConfig = this.Bind<ProjectConfig>();
                string target = parameters.value;
                var buildRequest = buildQueue.Build(projectConfig, target);
                return buildRequest.Id.ToString();
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
    }
}
