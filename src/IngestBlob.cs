using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MediaServices
{
    /// <summary>
    /// Implementation of an Azure function which encodes a video blob
    /// when it is uploaded to Blob Storage.
    /// </summary>
    public static class IngestBlob
    {
        /// <summary>
        /// Entrypoint for the Function, triggered by an event from Blob Storage.
        /// </summary>
        /// <param name="blob">A <see cref="Stream"/> representing the contents of the Blob.</param>
        /// <param name="name">The name of the blob.</param>
        /// <param name="context">The execution context.</param>
        /// <param name="log">The logger instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [FunctionName(nameof(IngestBlob))]
        public static async Task Run(
            [BlobTrigger("ingest/{name}", Connection = "IngestStorage")]Stream blob,
            string name,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("Blob Trigger - Received Blob '{Name}', Size: {Length} bytes", name, blob.Length);
            var client = new MediaServiceClient(context.GetMediaServiceConfiguration(), log);
            await client.LoginAsync().ConfigureAwait(false);

            // Create unique asset names
            string uniqueness = Guid.NewGuid().ToString().Substring(0, 10).Replace("-", string.Empty);
            string jobName = "job-" + uniqueness;
            string inputAssetName = "input-" + uniqueness;
            string outputAssetName = "output-" + uniqueness;

            // Create input asset and upload blob
            var inputAsset = await client.CreateAssetFromBlobAsync(inputAssetName, name, blob)
                                         .ConfigureAwait(false);

            // Check if an output Asset already exists. If it does, define a new unique name
            Asset outputAsset = await client.GetAssetAsync(outputAssetName)
                                            .ConfigureAwait(false);

            if (outputAsset != null)
            {
                outputAssetName = $"{name}-{Guid.NewGuid()}".ToLower();
            }

            // Create output asset
            await client.CreateOrUpdateAssetAsync(outputAssetName, new Asset()).ConfigureAwait(false);

            // Create encoding pipeline
            var transform = await client.GetOrCreateTransformAsync("Content Adaptive Multiple Bitrate MP4")
                                        .ConfigureAwait(false);

            // Submit a new encoding job, transforming the input Asset and placing 
            // the results in the output Asset
            await client.SubmitJobAsync(transform.Name, jobName, inputAssetName, outputAssetName)
                        .ConfigureAwait(false);
        }
    }
}
