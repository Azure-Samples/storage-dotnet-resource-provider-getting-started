---
services: storage
platforms: dotnet
author: tamram
---

# Getting Started with Azure Storage Resource Provider in .NET

This sample shows how to manage your storage account using the Azure Storage Resource Provider for .NET. The Storage Resource Provider is a client library for working with the storage accounts in your Azure subscription. Using the client library, you can create a new storage account, read its properties, list all storage accounts in a given subscription or resource group, read and regenerate the storage account keys, and delete a storage account.  

**On this page**

- Run this sample
- What is program.cs doing?

## Run this sample

To run the sample, follow these steps:

1. If you don't already have a Microsoft Azure subscription, you can register for a [free trial account](http://go.microsoft.com/fwlink/?LinkId=330212).
2. Install [Visual Studio](https://www.visualstudio.com/downloads/download-visual-studio-vs.aspx) if you don't have it already. 
3. Install the [Azure SDK for .NET](https://azure.microsoft.com/downloads/) if you have not already done so. We recommend using the most recent version.
4. Clone the sample repository.

		https://github.com/Azure-Samples/storage-dotnet-resource-provider-getting-started.git

5. Create an Azure service principal either through
    [Azure CLI](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/),
    [PowerShell](https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/)
    or [the portal](https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/). Note that you will need to specify the values shown in step 8 in order to run the sample, so it's recommended that you copy and save them during this step.

6. Open the sample solution in Visual Studio, and restore any packages if prompted.
7. In the sample source code, locate the constants for your subscription ID and resource group name, and specify values for them. 
	
		const string subscriptionId = "<subscriptionid>";         
	
	    //Specify a resource group name of your choice. Specifying a new value will create a new resource group.
	    const string rgName = "TestResourceGroup";        

8. In the sample source code, locate the following variables, and provide the values that you generated when you created the Azure service principal in step 5 above:

        const string applicationId = "<applicationId>";
        const string password = "<password>";
        const string tenantId = "<tenantId>";

## What is program.cs doing?

The sample walks you through several resource and resource group management operations. 

### Get credentials and create management clients

The sample gets an authorization token and constructs the necessary credentials based on the token. The values generated when you created the Azure service principle above are used for this step.

Next, the sample sets up a ResourceManagementClient object and a StorageManagementClient object using your subscription and the credentials.

    string token = GetAuthorizationHeader();
    TokenCredentials credential = new TokenCredentials(token);
    ResourceManagementClient resourcesClient = new ResourceManagementClient(credential) { SubscriptionId = subscriptionId };
    StorageManagementClient storageMgmtClient = new StorageManagementClient(credential) { SubscriptionId = subscriptionId };

### Register the Storage Resource Provider

The sample registers the Storage Resource Provider for the subscription: 

	resourcesClient.Providers.Register("Microsoft.Storage");

### Specify a resource group

The sample creates a new resource group or specifies an existing resource group for the new storage account. 

    var resourceGroup = resourcesClient.ResourceGroups.CreateOrUpdate(
            rgname,
            new ResourceGroup
            {
                Location = DefaultLocation
            });

### Create a new storage account

Next, the sample creates a new storage account that is associated with the resource group created in the previous step. 

In this case, the storage account name is randomly generated to assure uniqueness. However, the call to create a new storage account will succeed if an account with the same name already exists in the subscription.

	// This call gets a set of values to use in creating the storage account, including the account location, 
	// the kind of account, and the type of replication to use for the new account.
    StorageAccountCreateParameters parameters = GetDefaultStorageAccountParameters();

	// This call creates the new storage account, using the newly created resource group and 
	// the specified parameters.
    var storageAccount = storageMgmtClient.StorageAccounts.Create(rgname, acctName, parameters);

### List storage accounts in the subscription or resource group

The sample lists all of the storage accounts in a given subscription: 

    //Get all the storage accounts for a given subscription
    IEnumerable<StorageAccount> storAcctsSub = storageMgmtClient.StorageAccounts.List();

It also lists storage accounts in the newly created resource group:

    //Get the storage account keys for a given account and resource group
    IList<StorageAccountKey> acctKeys = storageMgmtClient.StorageAccounts.ListKeys(rgName, accountName).Keys;

### Read and regenerate storage account keys

The sample lists storage account keys for the newly created storage account and resource group:

    //Get the storage account keys for a given account and resource group
    IList<StorageAccountKey> acctKeys = storageMgmtClient.StorageAccounts.ListKeys(rgName, accountName).Keys;

It also regenerates the account access keys:

    //Regenerate the account key for a given account in a specific resource group
    IList<StorageAccountKey> regenAcctKeys = storageMgmtClient.StorageAccounts.RegenerateKey(rgName, accountName, "key1").Keys;

### Modify the storage account SKU

The storage account SKU specifies what type of replication applies to the storage account. You can update the storage account SKU to change how the storage account is replicated, as shown in the sample:

    // Update storage account sku
    var parameters = new StorageAccountUpdateParameters
    {
        Sku = new Sku(skuName)
    };
    var storageAccount = storageMgmtClient.StorageAccounts.Update(rgname, acctName, parameters);

Note that modifying the SKU for a production storage account may have associated costs. For example, if you convert a locally redundant storage account to a geo-redundant storage account, you will be charged for replicating your data to the secondary region. Before you modify the SKU for a production account, be sure to consider any cost implications. See [Azure Storage replication](https://azure.microsoft.com/documentation/articles/storage-redundancy/) for additional information about storage replication.

### Check storage account name availability

The sample checks whether a given storage account name is available in Azure: 

    //Check if the account name is available
    bool? nameAvailable = storageMgmtClient.StorageAccounts.CheckNameAvailability(accountName).NameAvailable;

### Delete the storage account

Finally, the sample deletes the storage account that it created:

    storageMgmtClient.StorageAccounts.Delete(rgname, acctName);

## More information
- [How to create, manage, or delete a storage account in the Azure Portal](https://azure.microsoft.com/documentation/articles/storage-create-storage-account/)
- [Storage Resource Provider Client Library for .NET](https://msdn.microsoft.com/library/azure/mt131037.aspx)
- [Azure Storage Resource Provider REST API Reference](https://msdn.microsoft.com/library/azure/Mt163683.aspx)

