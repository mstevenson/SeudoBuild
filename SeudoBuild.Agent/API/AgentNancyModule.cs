using System;
using Nancy;
using System.IO;
using SeudoBuild.Core;
using SeudoBuild.Pipeline;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    /// <inheritdoc />
    /// <summary>
    /// RESTful API for controlling an Agent via HTTP requests.
    /// </summary>
    public class AgentNancyModule : NancyModule
    {
        public AgentNancyModule(IBuildQueue buildQueue, IModuleLoader moduleLoader, IFileSystem filesystem, ILogger logger)
        {
            // Build agent info
            Get("/info", parameters =>
            {
                //moduleLoader.Registry.

                var proj = new AgentLocation { AgentName = AgentName.GetUniqueAgentName() };
                return Response.AsJson(proj);
            });

            // Build the default target in a project configuration
            Post("/build", parameters =>
            {
                try
                {
                    ProjectConfig config = ProcessReceivedBuildRequest(Request, null, moduleLoader, filesystem);
                    logger.QueueNotification($"Received build request: project '{config.ProjectName}', default target, from {Request.UserHostAddress}");
                    var buildRequest = buildQueue.EnqueueBuild(config);
                    return buildRequest.Id.ToString();
                }
                catch (Exception e)
                {
                    logger.Write(e.Message, LogType.Failure);
                    return HttpStatusCode.BadRequest;
                }
            });

            // Build a specific target within a given project configuration
            Post("/build/{target}", parameters =>
            {
                try
                {
                    string target = parameters.target;
                    ProjectConfig config = ProcessReceivedBuildRequest(Request, target, moduleLoader, filesystem);
                    logger.QueueNotification($"Queuing build request: project '{config.ProjectName}', target '{target}', from {Request.UserHostAddress}");
                    var buildRequest = buildQueue.EnqueueBuild(config, target);
                    return buildRequest.Id.ToString();
                }
                catch (Exception e)
                {
                    logger.Write(e.Message, LogType.Failure);
                    return HttpStatusCode.BadRequest;
                }
            });

            // Get info for a specific build task
            Get("/queue", parameters =>
            {
                try
                {
                    var result = buildQueue.GetAllBuildResults();
                    return Response.AsJson(result);
                }
                catch
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            // Get info for a specific build task
            Get("/queue/{id:int}", parameters =>
            {
                try
                {
                    int id = parameters.id;
                    var result = buildQueue.GetBuildResult(id);
                    return Response.AsJson(result);
                }
                catch
                {
                    return HttpStatusCode.BadRequest;
                }
            });

            // Cancel a build task
            Post("/queue/{id:int}/cancel", parameters =>
            {
                try
                {
                    int id = parameters.id;
                    buildQueue.CancelBuild(id);
                    return HttpStatusCode.OK;
                }
                catch
                {
                    return HttpStatusCode.BadRequest;
                }
            });
        }

        ProjectConfig ProcessReceivedBuildRequest(Request request, string target, IModuleLoader moduleLoader, IFileSystem filesystem)
        {
            // We'd ordinarily use Nancy's Bind method, but we need to use custom
            // JSON converters to properly deserialize the ProjectConfig object
            string json;
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
                    throw new Exception($"‣ Received project configuration from {request.UserHostAddress} but could not find a build target named '{target}'");
                }
            }

            if (string.IsNullOrEmpty(config.ProjectName))
            {
                throw new Exception($"‣ Received invalid project configuration from {request.UserHostAddress}");
            }

            return config;
        }
    }
}
