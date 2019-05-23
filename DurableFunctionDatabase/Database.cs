using System;
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

        public class WriteOperation
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public WriteOperation(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        [FunctionName("Register")]
        public static void Register(
            [EntityTrigger] IDurableEntityContext ctx)
        {
            string currentValue = ctx.GetState<string>();
            string operand = ctx.GetInput<string>();

            switch (ctx.OperationName)
            {
                case "set":
                    currentValue = operand;
                    ctx.SetState(currentValue);
                    ctx.Return(currentValue);
                    break;
                case "get":
                    ctx.Return(currentValue);
                    break;
            }
        }

        [FunctionName("Database_GET_Orchestrator")]
        public static async Task<string> DatabaseGetOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var key = context.GetInput<string>();

            Random random = new Random();
            var value = random.Next().ToString();

            EntityId id = new EntityId(nameof(Register), key);

            return await context.CallEntityAsync<string>(id, "get", value);
        }

        [FunctionName("Database_POST_Orchestrator")]
        public static async Task<string> DatabasePostOrchestratorAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var operation = context.GetInput<WriteOperation>();

            EntityId id = new EntityId(nameof(Register), operation.Key);

            return await context.CallEntityAsync<string>(id, "set", operation.Value);
        }

        [FunctionName("Database_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [OrchestrationClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId;

            // GET request
            if (req.Method == HttpMethod.Get)
            {
                instanceId = await starter.StartNewAsync("Database_GET_Orchestrator", KEY);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, System.TimeSpan.MaxValue);
            }

            // POST request
            else if(req.Method == HttpMethod.Post)
            {
                var content = req.Content;
                string value = content.ReadAsStringAsync().Result;
                instanceId = await starter.StartNewAsync("Database_POST_Orchestrator", new WriteOperation(KEY, value));
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
                return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, System.TimeSpan.MaxValue);
            }

            // Otherwise.
            else
            {
                return req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            }
        }
    }
}