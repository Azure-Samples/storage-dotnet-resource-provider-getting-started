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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;
using Microsoft.Azure.Management.Storage;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;

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
        const string subscriptionId = "<subscriptionid>";

        //Specify a resource group name of your choice. Specifying a new value will create a new resource group.
        const string rgName = "TestResourceGroup";        
        
        //Storage Account Name. Using random value to avoid conflicts.  Replace this with a storage account of your choice.
        static string accountName = "storagesample" + Guid.NewGuid().ToString().Substring(0,8);

        // To run the sample, you must first create an Azure service principal. To create the service principal, follow one of these guides:
        //      Azure Portal: https://azure.microsoft.com/documentation/articles/resource-group-create-service-principal-portal/) 
        //      PowerShell: https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal/
        //      Azure CLI: https://azure.microsoft.com/documentation/articles/resource-group-authenticate-service-principal-cli/
        // Creating the service principal will generate the values you need to specify for the constansts below.

        // Use the values generated when you created the Azure service principal.
        const string applicationId = "<applicationId>";
        const string password = "<password>";
        const string tenantId = "<tenantId>";

        // These values are used by the sample as defaults to create a new storage account. You can specify any location and any storage account type.
        const string DefaultLocation = "westus"; 
        public static Sku DefaultSku = new Sku(SkuName.StandardGRS);
        public static Kind DefaultKind = Kind.Storage;
        public static Dictionary<string, string> DefaultTags = new Dictionary<string, string> 
        {
            {"key1","value1"},
            {"key2","value2"}
        };

        //The following method will enable you to use the token to create credentials
        private static string GetAuthorizationHeader()
        {           
            ClientCredential cc = new ClientCredential(applicationId, password);
            var context = new AuthenticationContext("https://login.windows.net/" + tenantId);            
            var result = context.AcquireToken("https://management.azure.com/", cc);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }

            string token = result.AccessToken;

            return token;
        }

        static void Main(string[] args)
        {
            string token = GetAuthorizationHeader();
            TokenCredentials credential = new TokenCredentials(token);
            ResourceManagementClient resourcesClient = new ResourceManagementClient(credential) { SubscriptionId = subscriptionId };
            StorageManagementClient storageMgmtClient = new StorageManagementClient(credential) { SubscriptionId = subscriptionId };

            try
            {
                //Register the Storage Resource Provider with the Subscription
                RegisterStorageResourceProvider(resourcesClient);

                //Create a new resource group
                CreateResourceGroup(rgName, resourcesClient);

                //Create a new account in a specific resource group with the specified account name                     
                CreateStorageAccount(rgName, accountName, storageMgmtClient);

                //Get all the account properties for a given resource group and account name
                StorageAccount storAcct = storageMgmtClient.StorageAccounts.GetProperties(rgName, accountName);

                //Get a list of storage accounts within a specific resource group
                IEnumerable<StorageAccount> storAccts = storageMgmtClient.StorageAccounts.ListByResourceGroup(rgName);

                //Get all the storage accounts for a given subscription
                IEnumerable<StorageAccount> storAcctsSub = storageMgmtClient.StorageAccounts.List();

                //Get the storage account keys for a given account and resource group
                IList<StorageAccountKey> acctKeys = storageMgmtClient.StorageAccounts.ListKeys(rgName, accountName).Keys;

                //Regenerate the account key for a given account in a specific resource group
                IList<StorageAccountKey> regenAcctKeys = storageMgmtClient.StorageAccounts.RegenerateKey(rgName, accountName, "key1").Keys;

                //Update the storage account for a given account name and resource group
                UpdateStorageAccountSku(rgName, accountName, SkuName.StandardLRS, storageMgmtClient);

                //Check if the account name is available
                bool? nameAvailable = storageMgmtClient.StorageAccounts.CheckNameAvailability(accountName).NameAvailable;
                
                //Delete a storage account with the given account name and a resource group
                DeleteStorageAccount(rgName, accountName, storageMgmtClient);

                Console.ReadLine();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Registers the Storage Resource Provider in the subscription.
        /// </summary>
        /// <param name="resourcesClient"></param>
        public static void RegisterStorageResourceProvider(ResourceManagementClient resourcesClient)
        {
            Console.WriteLine("Registering Storage Resource Provider with subscription...");
            resourcesClient.Providers.Register("Microsoft.Storage");
            Console.WriteLine("Storage Resource Provider registered.");
        }
        
        /// <summary>
        /// Creates a new resource group with the specified name
        /// If one already exists then it gets updated
        /// </summary>
        /// <param name="resourcesClient"></param>
        public static void CreateResourceGroup(string rgname, ResourceManagementClient resourcesClient)
        {
            Console.WriteLine("Creating a resource group...");
            var resourceGroup = resourcesClient.ResourceGroups.CreateOrUpdate(
                    rgname,
                    new ResourceGroup
                    {
                        Location = DefaultLocation
                    });
            Console.WriteLine("Resource group created with name " + resourceGroup.Name);                                                                     

        }

        /// <summary>
        /// Create a new Storage Account. If one already exists then the request still succeeds
        /// </summary>
        /// <param name="rgname">Resource Group Name</param>
        /// <param name="acctName">Account Name</param>
        /// <param name="useCoolStorage">Use Cool Storage</param>
        /// <param name="useEncryption">Use Encryption</param>
        /// <param name="storageMgmtClient">Storage Management Client</param>
        private static void CreateStorageAccount(string rgname, string acctName, StorageManagementClient storageMgmtClient)
        {                                                                       
            StorageAccountCreateParameters parameters = GetDefaultStorageAccountParameters();

            Console.WriteLine("Creating a storage account...");
            var storageAccount = storageMgmtClient.StorageAccounts.Create(rgname, acctName, parameters);
            Console.WriteLine("Storage account created with name " + storageAccount.Name);                                                                     
        }

        /// <summary>
        /// Deletes a storage account for the specified account name
        /// </summary>
        /// <param name="rgname"></param>
        /// <param name="acctName"></param>
        /// <param name="storageMgmtClient"></param>
        private static void DeleteStorageAccount(string rgname, string acctName, StorageManagementClient storageMgmtClient)
        {
            Console.WriteLine("Deleting a storage account...");
            storageMgmtClient.StorageAccounts.Delete(rgname, acctName);
            Console.WriteLine("Storage account " + acctName + " deleted");
        }                                               

        /// <summary>
        /// Updates the storage account
        /// </summary>
        /// <param name="rgname">Resource Group Name</param>
        /// <param name="acctName">Account Name</param>
        /// <param name="storageMgmtClient"></param>
        private static void UpdateStorageAccountSku(string rgname, string acctName, SkuName skuName, StorageManagementClient storageMgmtClient)
        {
            Console.WriteLine("Updating storage account...");
            // Update storage account sku
            var parameters = new StorageAccountUpdateParameters
            {
                Sku = new Sku(skuName)
            };
            var storageAccount = storageMgmtClient.StorageAccounts.Update(rgname, acctName, parameters);
            Console.WriteLine("Sku on storage account updated to " + storageAccount.Sku.Name);           
        }

        /// <summary>
        /// Returns default values to create a storage account
        /// </summary>
        /// <returns>The parameters to provide for the account</returns>
        private static StorageAccountCreateParameters GetDefaultStorageAccountParameters()
        {
            StorageAccountCreateParameters account = new StorageAccountCreateParameters
            {
                Location = DefaultLocation,
                Kind = DefaultKind,
                Tags = DefaultTags,
                Sku = DefaultSku
            };

            return account;
        }              
    }
}
