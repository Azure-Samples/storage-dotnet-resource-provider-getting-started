---
services: storage
platforms: dotnet
author: lakasa-MSFT
---

# Getting Started with Azure Storage Resource Provider in .NET
Demonstrates Basic Operations with Azure Storage Resource Provider

## About the code
This sample demonstrates how to manage your storage accounts using the Storage Resource Provider .NET APIs. 
Storage Resource Provider enables you to manage your storage account and keys programmatically while inheriting the benefits of Azure Resource Manager

Note: If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

## Running this sample

This sample can be run using either the Azure Storage Emulator (Windows) or by updating the parameters indicated in Program.cs using Visual Studio

To run the sample using the Storage Emulator (default option):

1. Download and install the Azure Storage Emulator https://azure.microsoft.com/en-us/downloads/ 
2. Start the emulator (once only) by pressing the Start button or the Windows key and searching for it by typing "Azure Storage Emulator". 
Select it from the list of applications to start it.
3. Set breakpoints and run the project. 

To run the sample using the Visual Studio

1. Update the Program.cs with values of subscriptionid, applicationId, password, tenantId by following this link - https://azure.microsoft.com/en-gb/documentation/articles/virtual-machines-arm-deployment/
2. Set breakpoints and run the project.

## More information
- [What is a Storage Account](http://azure.microsoft.com/en-us/documentation/articles/storage-whatis-account/)
- [Azure Storage Resource Provider REST API reference](https://msdn.microsoft.com/en-us/library/azure/Mt163683.aspx)
