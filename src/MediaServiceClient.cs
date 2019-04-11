using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MediaServices
{
    /// <summary>
    /// Provides basic media services functionality.
    /// </summary>
    public class MediaServiceClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServiceClient"/> class.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        /// <param name="log">The log.</param>
        public MediaServiceClient(MediaServiceOptions options, ILogger log)
        {
            this.Options = options;
            this.Log = log;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaServiceClient"/> class.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        /// <param name="log">The log.</param>
        /// <param name="client">An instance of an existing <see cref="IAzureMediaServicesClient"/>.</param>
        public MediaServiceClient(MediaServiceOptions options, ILogger log, IAzureMediaServicesClient client)
        {
            this.Options = options;
            this.Log = log;
            this.Client = client;
        }

        /// <summary>
        /// Gets the configuration options.
        /// </summary>
        /// <value>The options.</value>
        private MediaServiceOptions Options
        {
            get;
        }

        /// <summary>
        /// Gets the active <see cref="ILogger"/>.
        /// </summary>
        /// <value>The log.</value>
        private ILogger Log
        {
            get;
        }

        /// <summary>
        /// Gets or sets the instance of <see cref="IAzureMediaServicesClient"/> to
        /// use for interactions.
        /// </summary>
        /// <value>The client.</value>
        private IAzureMediaServicesClient Client
        {
            get;
            set;
        }

        /// <summary>
        /// Performs an asynchronous login operation.
        /// </summary>
        /// <remarks>
        /// If the login was already been performed or a client instance was manually 
        /// provided in the constructor, this method will not re-initialize 
        /// the underlying client.
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task LoginAsync()
        {
            if (this.Client == null)
            {
                ClientCredential clientCredential = new ClientCredential(
                    this.Options.AadClientId,
                    this.Options.AadSecret);

                var login = await ApplicationTokenProvider.LoginSilentAsync(
                    this.Options.AadTenantId,
                    clientCredential,
                    ActiveDirectoryServiceSettings.Azure);

                var client = new AzureMediaServicesClient(this.Options.ArmEndpoint, login) {
                    SubscriptionId = this.Options.SubscriptionId
                };

                this.Client = client;
            }
        }

        /// <summary>
        /// Creates an asset containing a single file from a blob resource. This method assumes
        /// that the asset is an MP4 video file.
        /// </summary>
        /// <param name="assetName">The name of the asset to create</param>
        /// <param name="fileName">The name of the file to stored in the asset</param>
        /// <param name="source">Stream containing the file content</param>
        /// <returns>
        /// A task that represents the asynchronous operation with a result
        /// that contains the created <see cref="Asset"/>
        /// </returns>
        public async Task<Asset> CreateAssetFromBlobAsync(string assetName, string fileName, Stream source)
        {
            string resourceGroupName = this.Options.ResourceGroup;
            string accountName = this.Options.AccountName;

            this.Log.LogInformation("Creating Asset");

            var assetTemplate = new Asset {
                Description = $"Import - {Path.GetFileName(fileName)}"
            };

            Asset asset = await this.Client.Assets.CreateOrUpdateAsync(
                resourceGroupName,
                accountName,
                assetName,
                assetTemplate).ConfigureAwait(false);

            var uploadSasUrl = await this.GetAssetContainerAsync(assetName)
                                            .ConfigureAwait(false);

            await this.UploadVideoStreamToContainerAsync(fileName, source, uploadSasUrl)
                      .ConfigureAwait(false);

            return asset;
        }

        /// <summary>
        /// Upload an MP4 video stream to a Blob container as an asynchronous operation.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="stream">The file content.</param>
        /// <param name="sasUri">An SAS URL representing the target blob storage location</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task UploadVideoStreamToContainerAsync(string fileName, Stream stream, Uri sasUri)
        {
            string filename = Path.GetFileName(fileName);
            CloudBlobContainer container = new CloudBlobContainer(sasUri);
            var blob = container.GetBlockBlobReference(filename);
            blob.Properties.ContentType = "video/mp4";

            this.Log.LogInformation("Uploading File '{FileName}' to container URI: {Uri}", filename, sasUri);

            await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an Blob storage container from an asset as an asynchronous operation.
        /// </summary>
        /// <param name="assetName">Name of the asset.</param>
        /// <returns>A task that represents the asynchronous operation with a result
        /// that contains the SAS URI</returns>
        public async Task<Uri> GetAssetContainerAsync(string assetName)
        {
            ListContainerSasInput input = new ListContainerSasInput() {
                Permissions = AssetContainerPermission.ReadWrite,
                ExpiryTime = DateTime.Now.AddHours(1).ToUniversalTime()
            };

            var response = await this.Client.Assets.ListContainerSasAsync(
                this.Options.ResourceGroup,
                this.Options.AccountName,
                assetName,
                input.Permissions,
                input.ExpiryTime).ConfigureAwait(false);

            string uploadSasUrl = response.AssetContainerSasUrls.First();
            return new Uri(uploadSasUrl);
        }

        /// <summary>
        /// Gets an <see cref="Asset"/> as an asynchronous operation.
        /// </summary>
        /// <param name="name">The name of the asset to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation with a result
        /// that contains the created <see cref="Asset"/></returns>
        public async Task<Asset> GetAssetAsync(string name)
        {
            return await this.Client.Assets.GetAsync(
                this.Options.ResourceGroup,
                this.Options.AccountName,
                name).ConfigureAwait(false);
        }

        /// <summary>
        /// Create or update as <see cref="Asset"/> as an asynchronous operation.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        /// <param name="asset">The instance of an Asset to create or update in the Media Services account.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateOrUpdateAssetAsync(string name, Asset asset)
        {
            await this.Client.Assets.CreateOrUpdateAsync(
                this.Options.ResourceGroup,
                this.Options.AccountName,
                name,
                asset).ConfigureAwait(false);
        }

        /// <summary>
        /// Get or create a media encoding transformation as an asynchronous operation.
        /// </summary>
        /// <param name="name">The name of the transform.</param>
        /// <returns>A task that represents the asynchronous operation with a result
        /// that contains the created <see cref="Transform"/></returns>
        public async Task<Transform> GetOrCreateTransformAsync(string name)
        {
            // Check for existing transform
            Transform transform = await this.Client.Transforms.GetAsync(
                this.Options.ResourceGroup,
                this.Options.AccountName,
                name).ConfigureAwait(false);

            if (transform == null)
            {
                // The transform must be defined.
                TransformOutput[] output = new TransformOutput[]
                {
                    new TransformOutput
                    {
                        Preset = new BuiltInStandardEncoderPreset()
                        {
                            PresetName = EncoderNamedPreset.AdaptiveStreaming
                        }
                    }
                };

                // Create the Transform with the output defined above
                transform = await this.Client.Transforms.CreateOrUpdateAsync(
                    this.Options.ResourceGroup,
                    this.Options.AccountName,
                    name,
                    output).ConfigureAwait(false);
            }

            return transform;
        }

        /// <summary>
        /// Submit an encoding <see cref="Job"/> as an asynchronous operation.
        /// </summary>
        /// <param name="transformName">Name of the transform to apply.</param>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="inputAssetName">Name of the input asset.</param>
        /// <param name="outputAssetName">Name of the output asset.</param>
        /// <returns>A task that represents the asynchronous operation with a result
        /// that contains the created <see cref="Job"/></returns>
        public async Task<Job> SubmitJobAsync(
                        string transformName,
                        string jobName,
                        string inputAssetName,
                        string outputAssetName)
        {
            // Create the job instance
            JobInput jobInput = new JobInputAsset(assetName: inputAssetName);

            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(outputAssetName),
            };

            // In this example, we are assuming that the job name is unique and does not already exist.
            // This could be confirmed by calling <code>this.Client.Jobs.GetAsync</code>, which does a
            // case-insensitive search. If the call returns null, the Job does not exist.
            Job job = await this.Client.Jobs.CreateAsync(
                this.Options.ResourceGroup,
                this.Options.AccountName,
                transformName,
                jobName,
                new Job {
                    Input = jobInput,
                    Outputs = jobOutputs,
                }).ConfigureAwait(false);

            return job;
        }

        /// <summary>
        /// Publishes the Asset by creating an endpoint that can be used for streaming the
        /// video content.
        /// </summary>
        /// <param name="assetName">The name of the asset to publish.</param>
        /// <param name="streamingPolicyName">The name of the streaming policy to apply to the locator.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task PublishAsset(string assetName, string streamingPolicyName)
        {
            Guid streamingLocatorId = Guid.NewGuid();
            string streamingLocatorName = "streaminglocator-" + streamingLocatorId.ToString();

            try
            {
                var asset = await this.GetAssetAsync(assetName).ConfigureAwait(false);

                var streamingPolicy = await this.Client.StreamingPolicies.GetAsync(
                    this.Options.ResourceGroup,
                    this.Options.AccountName,
                    streamingPolicyName)
                    .ConfigureAwait(false);

                var streamingLocator = new StreamingLocator() {
                    AssetName = assetName,
                    StreamingPolicyName = streamingPolicyName,
                    AlternativeMediaId = streamingLocatorId.ToString(),
                    DefaultContentKeyPolicyName = null,
                    StartTime = null,
                    EndTime = null,
                    StreamingLocatorId = streamingLocatorId,
                };

                streamingLocator.Validate();

                await this.Client.StreamingLocators.CreateAsync(
                    this.Options.ResourceGroup,
                    this.Options.AccountName,
                    streamingLocatorName,
                    streamingLocator).ConfigureAwait(false);
            }
            catch (ApiErrorException e)
            {
                this.Log.LogError("API error {Code} coccurred: {Message}", e.Body.Error.Code, e.Body.Error.Message);
            }
            catch (Exception e)
            {
                this.Log.LogError("An exception occurred: {Message}", e.Message);
            }

            this.Log.LogInformation("Created '{LocatorName}' with Id '{Id}'", streamingLocatorName, streamingLocatorId);
        }
    }
}