using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace MediaServices
{
    /// <summary>
    /// Implementation of a Function for monitoring the encoding/transform
    /// process.
    /// </summary>
    public static class MonitorEncoding
    {
        /// <summary>
        /// Entrypoint for the Function, triggered by an event from EventGrid.
        /// </summary>
        /// <remarks>
        /// Default URL for triggering event grid function in the local environment.
        /// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
        /// </remarks>
        /// <param name="eventGridEvent">The event from EventGrid.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="log">The log instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [FunctionName(nameof(MonitorEncoding))]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ExecutionContext context,
            ILogger log)
        {
            // Dump the event details 
            log.LogInformation($"EventGridEvent" +
                              "\n\tId: {EventId}" +
                              "\n\tTopic: {EventTopic}" +
                              "\n\tSubject: {EventSubject}" +
                              "\n\tType: {EventType}" +
                              "\n\tData: {EventData}",
                              eventGridEvent.Id,
                              eventGridEvent.Topic,
                              eventGridEvent.Subject,
                              eventGridEvent.EventType,
                              eventGridEvent.Data.ToString());

            var client = new MediaServiceClient(context.GetMediaServiceConfiguration(), log);
            await client.LoginAsync().ConfigureAwait(false);

            // Find the right event
            if (eventGridEvent.EventType == "Microsoft.Media.JobOutputStateChange")
            {
                log.LogInformation("Detected JobOutputStateChange event");

                dynamic data = eventGridEvent.Data;
                var output = data.output;

                // Only react if this is the last step in the process
                if (output.state == "Finished")
                {
                    string assetName = output.assetName;
                    log.LogInformation("Publishing asset '{AssetName}'", assetName);
                    await client.PublishAsset(assetName, "Predefined_ClearStreamingOnly")
                                .ConfigureAwait(false);
                }
            }
        }
    }
}
