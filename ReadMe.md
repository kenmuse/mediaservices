# Media Services v3 Functions Sample

This project includes sample code which demonstrates how to utilize
Azure Media Services v3 APIs with Azure Functions and Azure Event Grid. 
This sample uses an Blob Trigger to automatically process an uploaded Azure storage 
blob. It also utilizes Event Grid in order to automatically publish the file once
the transform job has completed.

This sample targets Visual Studio 2017 version 15.5 or later with the **Azure development** workload
installed [as described here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs#prerequisites).

## Setup
To use this sample, your will need to do the following:

1. Setup Azure Media Services as described here: https://docs.microsoft.com/en-us/azure/media-services/previous/media-services-portal-create-account
1. Create a service principal for Azure Media Services using the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/ams/account/sp?view=azure-cli-latest),
   using the name of your Media Services account, the Resource group containing that account, and
   defining a name for the service principal

   ``` 
   az ams account sp create -a mediaServicesAccountName -g mediaServicesResourceGroup -n principalName
   ```

    The output of this call will be needed for configuring the sample. It can be added to your `local.settings.json` file
    as detailed below.
1. Deploy the Functions from the MediaServices.csproj to Azure.
1. Add an EventGrid trigger to the MonitorEncoding Function.

## Function Configuration
Settings will need to be configured in order to use use these Functions. These are detailed below.

### local.settings.json

For local development, you will need to manually add a `local.settings.json` file to your project, replacing 
the body of MediaServices (from `AadTenantId` to `AccountName`) with the output from creating the SPN using the
Azure CLI command, above:

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "IngestStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet"
    },
    "MediaServices": {
        "AadTenantId":"",
        "ArmEndpoint":  "https://management.azure.com/",
        "AadClientId": "",
        "AadSecret": "",
        "SubscriptionId":"",
        "ResourceGroup" : "",
        "AccountName" :  ""
    }
}
```

### Azure Application Settings
Deployed to Azure, the functions will require the following Application Settings
values to be configured:

- AzureWebJobsStorage
- IngestStorage
- MediaServices:AadTenantId
- MediaServices:ArmEndpoint
- MediaServices:AadClientId
- MediaServices:AadSecret
- MediaServices:SubscriptionId
- MediaServices:ResourceGroup
- MediaServices:AccountName