# SharePoint Taxonomy Toolkit

>
> **Important**
>
> This repository has been copied from http://taxonomytoolkit.codeplex.com/documentation because the project seemed to be
> orphaned but the effort that has been put into it is worth being preserved.
>

For use with Microsoft SharePoint, **Taxonomy Toolkit** provides PowerShell cmdlets and a C# API for importing, exporting, and bulk editing of taxonomy objects from the Managed Metadata Service.  The data is stored using TAXML, an XML-based file format.  All operations are performed using the [SharePoint Client OM](http://msdn.microsoft.com/en-us/library/office/jj164060(v=office.15).aspx#ClientManagedOM).  Taxonomy Toolkit does not require administrator permissions and is compatible with both on-prem and Office 365 cloud-hosted sites.

## Example Usage Scenarios

* Copy groups, term sets, or terms between SharePoint term stores (preserving all GUIDs and properties)
* Export snapshots of taxonomy data from different farms (or different points in time) and diff them to see what changed
* For Managed Navigation and cross-site publishing scenarios, perform bulk edits of Friendly URL and catalog category hierarchies
* Quickly populate new term stores, e.g. for demos or staged deployments
* Distribute "hotfixes" that update a live taxonomy service without writing custom code
* Using the Taxonomy Toolkit API, empower C# developers to manipulate taxonomy data in complex ways without being a CSOM "guru"

**See the [online documentation](http://taxonomytoolkit.codeplex.com/documentation) for complete examples.**

## Technical Features

* Captures all Taxonomy object properties including synonyms, translations, stakeholders, custom properties, reused terms, deprecated terms, etc.
* Supports incremental updates using a "SyncAction" directive, which allows you to specify IfMissing/IfPresent/IfElsewhere policies at each level of the tree
* Packaged as PowerShell cmdlets to facilitate scripting
* Authenticates with both Office 365 cloud and on-prem servers, using the PowerShell credentials model
* TAXML documents can reference external objects for reuse/pinning operations, both by name or by GUID
* Custom sort orders can be specified using a convenient abbreviated syntax
* The importer minimizes changelog churn by comparing each property to the server's version and avoiding unnecessary write operations
* The importer engine uses a priority queue and dependency solver to combine Client OM queries into optimal batches
* The C# API provides a functionally complete "LocalTermStore" class that allows arbitrary manipulation of objects before loading/saving to TAXML or importing/exporting to SharePoint
* Taxonomy Toolkit releases are validated using unit tests and automated functional tests (including CSOM protocol traces)

# Background

Taxonomy Toolkit was created by a Microsoft employee as part of the [Garage](https://news.microsoft.com/stories/garage/index.html) spirit of developing interesting project ideas on the side without official sponsorship.  The tool gained popularity internally, and after many requests to share it with customers, the author eventually obtained permission to publish the code under an open source license.  Taxonomy Toolkit is not officially supported or maintained by Microsoft, but hopefully you will find it useful!  Suggestions and feedback are welcome.

