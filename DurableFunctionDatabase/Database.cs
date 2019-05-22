using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunctionDatabase
{
    public static class Database
    {
        public static string KEY = "1";

        [FunctionName("Database_GET_Orchestrator")]
        public static async Task<string> DatabaseGetOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string key = context.GetInput<string>();
            return await context.CallActivityAsync<string>("GET", key);
        }

        [FunctionName("Database_POST_Orchestrator")]
        public static async Task<string> DatabasePostOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string key = context.GetInput<string>();
            return await context.CallActivityAsync<string>("POST", key);
        }

        [FunctionName("Database_GET")]
        public static string GET([ActivityTrigger] string key, ILogger log)
        {
            return "GET";
        }

        [FunctionName("Database_POST")]
        public static string POST([ActivityTrigger] string key, ILogger log)
        {
            return "POST";
        }

        [FunctionName("Database_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [OrchestrationClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId;

            // GET request
            if(req.Method == HttpMethod.Get)
            {
                instanceId = await starter.StartNewAsync("Database_POST_Orchestrator", KEY);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
                return starter.CreateCheckStatusResponse(req, instanceId);
            }

            // POST request
            else if(req.Method == HttpMethod.Post)
            {
                instanceId = await starter.StartNewAsync("Database_POST_Orchestrator", KEY);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
                return starter.CreateCheckStatusResponse(req, instanceId);
            }

            // Otherwise.
            else
            {
                return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}