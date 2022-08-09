// 
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Core;

/// <summary>
/// Azure Storage Resource Provider Sample - Demonstrate how to create and manage storage accounts using Storage Resource Provider. 
/// Azure Storage Resource Provider enables customers to create and manage storage accounts 
///  
/// Documentation References: 
/// - How to create, manage, or delete a storage account in the Azure Portal - https://azure.microsoft.com/en-us/documentation/articles/storage-create-storage-account/
/// - Storage Resource Provider REST API  documentation - https://msdn.microsoft.com/en-us/library/azure/mt163683.aspx 
/// </summary>

namespace AzureStorageNew
{
    public class StorageAccountTests
    {
        // You can locate your subscription ID on the Subscriptions blade of the Azure Portal (https://portal.azure.com).
        const string subscriptionId = "<subscriptionId";

        //Specify a resource group name of your choice. Specifying a new value will create a new resource group.
        const string rgName = "TestResourceGroup";

        //Storage account name. Using random value to avoid conflicts. Replace this with a storage account of your choice.
        static readonly string storAccountName = $"storagesample{Guid.NewGuid().ToString().Substring(0, 8)}";

        // To run the sample, you must first create an Azure service principal. To create the service principal, follow one of these guides:
        //      Azure Portal: https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/) 
        //      PowerShell: https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/
        //      Azure CLI: https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/
        // Creating the service principal will generate the values you need to specify for the constansts below.

        // These values are used by the sample as defaults to create a new storage account. You can specify any location and any storage account type.
        static readonly AzureLocation location = AzureLocation.WestUS;
        static readonly StorageSku sku = new StorageSku(StorageSkuName.StandardGrs);
        static readonly StorageKind kind = StorageKind.StorageV2;

        static async Task Main()
        {
            //Authenticate to Azure and create the top-level ArmClient
            ArmClient armClient = new ArmClient(new DefaultAzureCredential());

            try
            {
                //Create a resource identifier, then get the subscription resource
                ResourceIdentifier resourceIdentifier = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
                
                SubscriptionResource subscription = armClient.GetSubscriptionResource(resourceIdentifier);

                //Register the Storage resource provider in the subscription
                ResourceProviderResource resourceProvider = await subscription.GetResourceProviderAsync("Microsoft.Storage");
                resourceProvider.Register();

                //Create a new resource group (if one already exists then it gets updated)
                ArmOperation<ResourceGroupResource> rgOperation = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(location));
                ResourceGroupResource resourceGroup = rgOperation.Value;
                Console.WriteLine($"Resource group: {resourceGroup.Id.Name}");

                //Create a new storage account in a specific resource group with the specified account name (request still succeeds if one already exists)

                //First we need to define the StorageAccountCreateOrUpdateContent parameters
                StorageAccountCreateOrUpdateContent parameters = GetStorageAccountParameters();

                //Now we can create a storage account with defined account name and parameters
                Console.WriteLine("Creating a storage account...");
                StorageAccountCollection accountCollection = resourceGroup.GetStorageAccounts();
                ArmOperation<StorageAccountResource> acctOperation = await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, storAccountName, parameters);
                StorageAccountResource storageAccount = acctOperation.Value;
                Console.WriteLine($"(Storage account created with name {storageAccount.Id.Name}");

                //Get all the storage accounts for a given subscription
                await GetStorageAccountsForSubscription(subscription);

                //Get a list of storage accounts within a specific resource group
                await GetStorageAccountsInResourceGroup(resourceGroup);

                //Get the storage account keys for a given account and resource group
                GetStorageAccountKeys(storageAccount);

                //Regenerate an account key for a given account
                StorageAccountRegenerateKeyContent regenKeyContent = new StorageAccountRegenerateKeyContent("key1");
                StorageAccountGetKeysResult regenAcctKeys = storageAccount.RegenerateKey(regenKeyContent);

                //Update the storage account for a given account name and resource group
                await UpdateStorageAccountSkuAsync(storageAccount, accountCollection);

                ////Delete a storage account with the given account name and a resource group
                storageAccount = await accountCollection.GetAsync(storAccountName);
                Console.WriteLine($"Deleting storage account {storageAccount.Id.Name}");
                await storageAccount.DeleteAsync(WaitUntil.Completed);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        /// <summary>
        private static async Task GetStorageAccountsInResourceGroup(ResourceGroupResource resourceGroup)
        {
            AsyncPageable<StorageAccountResource> storAccts = resourceGroup.GetStorageAccounts().GetAllAsync();
            Console.WriteLine($"List of storage accounts in {resourceGroup.Id.Name}:");
            await foreach (StorageAccountResource storAcct in storAccts)
            {
                Console.WriteLine($"\t{storAcct.Id.Name}");
            }
        }

        private static async Task GetStorageAccountsForSubscription(SubscriptionResource subscription)
        {
            AsyncPageable<StorageAccountResource> storAcctsSub = subscription.GetStorageAccountsAsync();
            Console.WriteLine($"List of storage accounts in subscription {subscription.Get().Value.Data.DisplayName}:");
            await foreach (StorageAccountResource storAcctSub in storAcctsSub)
            {
                Console.WriteLine($"\t{storAcctSub.Id.Name}");
            }
        }
        private static void GetStorageAccountKeys(StorageAccountResource storageAccount)
        {
            StorageAccountGetKeysResult result = storageAccount.GetKeys();
            IReadOnlyList<StorageAccountKey> acctKeys = result.Keys;
            Console.WriteLine($"List of storage account keys in {storageAccount.Id.Name}:");
            foreach (StorageAccountKey acctKey in acctKeys)
            {
                Console.WriteLine($"\t{acctKey.KeyName}");
            }
        }

        private static async Task UpdateStorageAccountSkuAsync(StorageAccountResource storageAccount, StorageAccountCollection accountCollection)
        {
            Console.WriteLine("Updating storage account...");
            // Update storage account sku
            var currentSku = storageAccount.Get().Value.Data.Sku.Name;  //capture the current Sku value before updating
            StorageSku updateSku = new StorageSku(StorageSkuName.StandardLrs);
            StorageAccountCreateOrUpdateContent updateParams = new StorageAccountCreateOrUpdateContent(updateSku, kind, location);
            await accountCollection.CreateOrUpdateAsync(WaitUntil.Completed, storAccountName, updateParams);
            Console.WriteLine($"Sku on storage account updated from {currentSku} to {storageAccount.Get().Value.Data.Sku.Name}");
        }

    /// <returns>The parameters to provide for the account</returns>
    private static StorageAccountCreateOrUpdateContent GetStorageAccountParameters()
        {
            StorageAccountCreateOrUpdateContent parameters = new StorageAccountCreateOrUpdateContent(sku, kind, location);

            return parameters;
        }
    }
}
