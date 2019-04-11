using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;

namespace MediaServices
{
    // Extension method to load the configuration settings from the execution context.
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// The key name to use for retrieving the options.
        /// </summary>
        private const string SectionName = "MediaServices";

        /// <summary>
        /// Cache the options until a restart of the service
        /// </summary>
        private static MediaServiceOptions options = null;

        /// <summary>
        /// Retrieves the media services related configuration settings
        /// </summary>
        /// <param name="context">The function execution context</param>
        /// <returns>An instance of <see cref="MediaServiceOptions"/> representing the current configuration</returns>
        public static MediaServiceOptions GetMediaServiceConfiguration(this ExecutionContext context)
        {
            if (options == null)
            {
                var config = new ConfigurationBuilder()
                     .SetBasePath(context.FunctionAppDirectory)
                     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables()
                     .Build();

                options = config.GetSection(SectionName)
                                .Get<MediaServiceOptions>();
            }

            return options;
        }
    }
}
