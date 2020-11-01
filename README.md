# elFinder.NetCore.AzureBlobStorage
Microsoft Azure Blob Storage driver for elFinder.NetCore

<img src="https://github.com/brunomel/elFinder.NetCore.AzureBlobStorage/blob/main/_misc/logo.png" alt="logo" width="350" />
<img src="https://github.com/brunomel/elFinder.NetCore.AzureBlobStorage/blob/main/_misc/azureblobstorage.png" alt="logo" width="350" />

## Instructions

1. Install the NuGet package: https://www.nuget.org/packages/elFinder.NetCore.AzureBlobStorage.Driver/

2. Look at the [demo project](https://github.com/brunomel/elFinder.NetCore.AzureBlobStorage/tree/main/src/elFinder.NetCore.AzureBlobStorage.Web) for an example on how to integrate it into your own project.

## Azure Blob Storage Connector

In order to use the Azure Blob Storage Connector

1. Open your **appsettings.json** file and look for the **AzureBlobStorage** section:

> Replace `ConnectionString`, `ContainerName` and `OriginHostName` with the appropriate values for your Azure account.

2. The thumbnails are stored in the local file system in folder **./wwwroot/thumbnails**. You can change this folder location in `ElFinderController` constructor.

## Description

This plugin has been inspired by [**elFinder.NetCore.AzureStorage**](https://github.com/fsmirne/elFinder.NetCore.AzureStorage) by [Flavio Smirne](https://github.com/fsmirne)

In order to increase the performance it uses the **CustomBaseModel** class instead of the original **BaseModel** in **elFinder.NetCore**, which:
* it does not download from Azure all the files in the opening folder to create the thumbnails on the fly, every time a folder is opened
* it does only one single call to Azure for any file in the opening folder in order to get the file properties
* it does not instructs elFinder UI to generate the thumbnails for the original files bigger than 2Mb

The thumbnails are generated and stored in the local file system by **ElFinderController**'s methods. 


## Dependencies

This plugin depends on [**elFinder.NetCore**](https://github.com/gordon-matt/elFinder.NetCore) by [Matt Gordon](https://github.com/gordon-matt)
