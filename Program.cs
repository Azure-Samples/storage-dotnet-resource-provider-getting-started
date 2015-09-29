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

/// <summary>
/// Azure Storage Resource Provider Sample - Demonstrate how to create and manage storage accounts using Storage Resource Provider. 
/// Azure Storage Resource Provider enables customers to create and manage storage accounts 
///  
/// Documentation References: 
/// - What is a Storage Account - http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/
/// - Storage Resource Provider REST API  documentation - https://msdn.microsoft.com/en-us/library/azure/mt163683.aspx 
/// </summary>

namespace AzureStorageNew
{
    public class StorageAccountTests
    {
        const string subscriptionId = "<subscriptionid>";        

        //Resource Group Name. Replace this with a Resource Group of your choice.
        const string rgName = "TestResourceGroup";        
        
        //Storage Account Name. Replace this with a storage account of your choice.
        const string accountName = "teststorageaccount";

        //Please follow the Tutorial - at the link - https://azure.microsoft.com/en-gb/documentation/articles/virtual-machines-arm-deployment/
        //This tutorial shows you how to use some of the available clients in the Compute, Storage, and Network .NET libraries 
        //to create and delete resources in Microsoft Azure. 
        //It also shows you how to authenticate the requests to Azure Resource Manager by using Azure Active Directory.
        //Please follow steps 1 through 3 that will allow you to switch to the ARM mode, add your application to Azure AD and set permissions, install all the 
        //necessary .NET libraries for Storage, Compute and Network and create credentials that are used to authenticate requests.


        //The following string values need to be replaced from what you obtain by 
        //following the steps 1 using link https://azure.microsoft.com/en-gb/documentation/articles/virtual-machines-arm-deployment/

        const string applicationId = "<applicationId>";
        const string password = "<password>";
        const string tenantId = "<tenantId>";

        // These are used to create default accounts. You can choose any location and any storage account type.

        const string DefaultLocation = "westus"; 
        public static AccountType DefaultAccountType = AccountType.StandardGRS;
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
            TokenCloudCredentials credential = new TokenCloudCredentials(subscriptionId, token);
            ResourceManagementClient resourcesClient = new ResourceManagementClient(credential);
            StorageManagementClient storageMgmtClient = new StorageManagementClient(credential);

            try
            {
                //Create a new resource group
                CreateResourceGroup(rgName, resourcesClient);

                //Create a new account in a specific resource group with the specified account name                     
                CreateStorageAccount(rgName, accountName, storageMgmtClient);

                //Get all the account properties for a given resource group and account name
                StorageAccount storAcct = storageMgmtClient.StorageAccounts.GetProperties(rgName, accountName).StorageAccount;

                //Get a list of storage accounts within a specific resource group
                IList<StorageAccount> storAccts = storageMgmtClient.StorageAccounts.ListByResourceGroup(rgName).StorageAccounts;

                //Get all the storage accounts for a given subscription
                IList<StorageAccount> storAcctsSub = storageMgmtClient.StorageAccounts.List().StorageAccounts;

                //Get the storage account keys for a given account and resource group
                StorageAccountKeys acctKeys = storageMgmtClient.StorageAccounts.ListKeys(rgName, accountName).StorageAccountKeys;

                //Regenerate the account key for a given account in a specific resource group
                StorageAccountKeys regenAcctKeys = storageMgmtClient.StorageAccounts.RegenerateKey(rgName, accountName, KeyName.Key1).StorageAccountKeys;

                //Update the storage account for a given account name and resource group
                UpdateStorageAccount(rgName, accountName, storageMgmtClient);

                //Check if the account name is available
                bool nameAvailable = storageMgmtClient.StorageAccounts.CheckNameAvailability(accountName).NameAvailable;
                
                //Delete a storage account with the given account name and a resource group
                DeleteStorageAccount(rgName, accountName, storageMgmtClient);
            }
            catch (Hyak.Common.CloudException ce)
            {
                Console.WriteLine(ce.Message);
                Console.ReadLine();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Creates a new resource group with the specified name
        /// If one already exists then it gets updated
        /// </summary>
        /// <param name="resourcesClient"></param>
        public static void CreateResourceGroup(string rgname, ResourceManagementClient resourcesClient)
        {                        
            var resourceGroup = resourcesClient.ResourceGroups.CreateOrUpdate(
                    rgname,
                    new ResourceGroup
                    {
                        Location = DefaultLocation
                    });
        }

        /// <summary>
        /// Create a new Storage Account. If one already exists then the request still succeeds
        /// </summary>
        /// <param name="rgname">Resource Group Name</param>
        /// <param name="acctName">Account Name</param>
        /// <param name="storageMgmtClient"></param>
        private static void CreateStorageAccount(string rgname, string acctName, StorageManagementClient storageMgmtClient)
        {                                                                       
            StorageAccountCreateParameters parameters = GetDefaultStorageAccountParameters();
            Console.WriteLine("Creating a storage account...");
            var storageAccountCreateResponse = storageMgmtClient.StorageAccounts.Create(rgname, acctName, parameters);
            Console.WriteLine("Storage account created with name " + storageAccountCreateResponse.StorageAccount.Name);                                                                     
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
            var deleteRequest = storageMgmtClient.StorageAccounts.Delete(rgname, acctName);
            Console.WriteLine("Storage account " + acctName + " deleted");
        }                                               

        /// <summary>
        /// Updates the storage account
        /// </summary>
        /// <param name="rgname">Resource Group Name</param>
        /// <param name="acctName">Account Name</param>
        /// <param name="storageMgmtClient"></param>
        private static void UpdateStorageAccount(string rgname, string acctName, StorageManagementClient storageMgmtClient)
        {
            Console.WriteLine("Updating storage account...");
            // Update storage account type
            var parameters = new StorageAccountUpdateParameters
            {
                AccountType = AccountType.StandardLRS
            };
            var response = storageMgmtClient.StorageAccounts.Update(rgname, acctName, parameters);
            Console.WriteLine("Account type on storage account updated to " + response.StorageAccount.AccountType);           
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
                Tags = DefaultTags,
                AccountType = DefaultAccountType
            };

            return account;
        }              
    }
}
