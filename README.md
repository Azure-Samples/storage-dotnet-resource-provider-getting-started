---
page_type: sample
languages:
- csharp
products:
- azure
description: "This sample shows how to manage your storage account using the Azure Storage resource provider for .NET."
urlFragment: storage-dotnet-resource-provider-getting-started
---
# Getting Started with Azure Storage Resource Provider in .NET

This sample shows how to manage your storage account using the Azure Storage resource provider for .NET. The Storage resource provider is a service based on Azure Resource Manager that provides access to management resources for Azure Storage. You can use the Azure Storage resource provider to create a new storage account, read its properties, list all storage accounts in a given subscription or resource group, read and regenerate the storage account keys, and delete a storage account.  

**On this page**

- Run the code sample
- Understand what this sample is doing

## Run the code sample

To run the sample, follow these steps:

1. If you don't already have a Microsoft Azure subscription, you can register for a [free trial account](http://go.microsoft.com/fwlink/?LinkId=330212).
1. Install [Visual Studio](https://www.visualstudio.com/downloads/download-visual-studio-vs.aspx) if you don't have it already. 
1. Install the [Azure SDK for .NET](https://azure.microsoft.com/downloads/) if you have not already done so. We recommend using the most recent version.
1. Clone the sample repository.

    `https://github.com/Azure-Samples/storage-dotnet-resource-provider-getting-started.git`

1. Create an Azure service principal using [Azure CLI](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/), [PowerShell](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/), or the [Azure portal](https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/). Note that you will need to specify the values shown in step 8 in order to run the sample, so it's recommended that you copy and save them during this step.

1. Open the sample solution in Visual Studio, and restore any packages if prompted.
1. In the sample source code, locate the constants for your subscription ID and resource group name, and specify values for them. 

    ```csharp
    const string subscriptionId = "<subscriptionId>";
    
    //Specify a resource group name of your choice. Specifying a new value will create a new resource group.
    const string rgName = "TestResourceGroup";
    ```

## Understand what this sample is doing

The sample walks you through several Storage resource provider operations.

Namespaces for this example:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
```

### Authenticate to Azure and create a client

The default option to create an authenticated client is to use `DefaultAzureCredential`. Since all management APIs go through the same endpoint, only one top-level `ArmClient` needs to be created to interact with resources.

```csharp
// Authenticate to Azure and create the top-level ArmClient
ArmClient armClient = new ArmClient(new DefaultAzureCredential());
```

Additional documentation for `DefaultAzureCredential` can be found in the `Azure.Identity.DefaultAzureCredential` [class definition](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential).

### Create a resource identifier and get a subscription

The sample creates a `ResourceIdentifier` object using the `subscriptionId` constant, then gets the subscription:

```csharp
// Create a resource identifier, then get the subscription resource
ResourceIdentifier resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
SubscriptionResource subscription = armClient.GetSubscriptionResource(resourceIdentifier);
```

### Register the Storage resource provider

The sample registers the Storage resource provider in the subscription:

```csharp
// Register the Storage resource provider in the subscription
ResourceProviderResource resourceProvider = await subscription.GetResourceProviderAsync("Microsoft.Storage");
resourceProvider.Register();
```

### Specify a resource group

The sample creates a new resource group or specifies an existing resource group for the new storage account:

```csharp
// Create a new resource group (if one already exists then it gets updated)
ArmOperation<ResourceGroupResource> rgOperation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(location));
ResourceGroupResource resourceGroup = rgOperation.Value;
```

### Create a new storage account

Next, the sample creates a new storage account that is associated with the resource group specified in the previous step.

In this example, the storage account name is randomly generated to assure uniqueness. However, the request to create a new storage account will still succeed if an account with the same name already exists in the subscription.

```csharp
// Create a new storage account in a specific resource group with the specified account name (request still succeeds if one already exists)

// First we need to define the StorageAccountCreateOrUpdateContent parameters
// This includes, but is not limited to, account location, kind, and replication type
StorageAccountCreateOrUpdateContent parameters = GetStorageAccountParameters();

// Now we can create a storage account resource with the defined account name and parameters
StorageAccountCollection accountCollection = resourceGroup.GetStorageAccounts();
ArmOperation<StorageAccountResource> acctOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, storAccountName, parameters);
StorageAccountResource storageAccount = acctOperation.Value;
```

### List storage accounts in the subscription or resource group

The sample lists all of the storage accounts in a given subscription:

```csharp
// Get all the storage accounts for a given subscription
AsyncPageable<StorageAccountResource> storAcctsSub = subscription.GetStorageAccountsAsync();
```

It also lists storage accounts in the resource group:

```csharp
// Get a list of storage accounts within a specific resource group
AsyncPageable<StorageAccountResource> storAccts = resourceGroup.GetStorageAccounts().GetAllAsync();
```

### List or regenerate storage account keys

The sample gets storage account keys for the newly created storage account:

```csharp
// Get the storage account keys for a given account and resource group
Pageable<StorageAccountKey> acctKeys = storageAccount.GetKeys();
```

It also regenerates the account keys:

```csharp
// Regenerate an account key for a given account
StorageAccountRegenerateKeyContent regenKeyContent = new StorageAccountRegenerateKeyContent("key1");
Pageable<StorageAccountKey> regenAcctKeys = storageAccount.RegenerateKey(regenKeyContent);
```

### Modify the storage account SKU

The storage account SKU specifies what type of replication applies to the storage account. You can update the storage account SKU to change how the storage account is replicated, as shown in the sample:

```csharp
// Update storage account sku
StorageSku updateSku = new StorageSku(StorageSkuName.StandardLrs);
StorageAccountCreateOrUpdateContent updateParams = new StorageAccountCreateOrUpdateContent(updateSku, kind, location);
await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, storAccountName, updateParams);
```

Note that modifying the SKU for a production storage account may have associated costs. For example, if you convert a locally redundant storage account to a geo-redundant storage account, you will be charged for replicating your data to the secondary region. Before you modify the SKU for a production account, be sure to consider any cost implications. See [Azure Storage replication](https://azure.microsoft.com/documentation/articles/storage-redundancy/) for additional information about storage replication.

### Check storage account name availability

The sample checks whether a given storage account name is available in Azure: 

```csharp
// Check if the account name is available
bool? nameAvailable = subscription.CheckStorageAccountNameAvailability(new StorageAccountNameAvailabilityContent(storAccountName)).Value.IsNameAvailable;
```

### Delete the storage account

The sample deletes the storage account that it previously created:

```csharp
await storageAccount.DeleteAsync(WaitUntil.Completed);
```

## More information

- [Create a storage account](https://azure.microsoft.com/documentation/articles/storage-create-storage-account/)
- [Storage Resource Provider Client Library for .NET](https://docs.microsoft.com/dotnet/api/overview/azure/storage/management)
- [Azure Storage Resource Provider REST API Reference](https://docs.microsoft.com/rest/api/storagerp)
