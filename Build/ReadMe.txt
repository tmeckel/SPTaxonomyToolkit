
Taxonomy Toolkit for SharePoint
-------------------------------

For use with Microsoft SharePoint, Taxonomy Toolkit provides PowerShell cmdlets
and a C# API for importing, exporting, and bulk editing of taxonomy objects
from the Managed Metadata Service.  The data is stored using TAXML, an 
XML-based file format.  All operations are performed using the SharePoint
Client OM.  Taxonomy Toolkit does not require administrator permissions and 
is compatible with both on-prem and Office 365 cloud-hosted sites.

To download the latest release, including complete source code,
please visit this web site:

    https://taxonomytoolkit.codeplex.com/

Usage of this toolkit is subject to the license terms in the accompanying
file "TaxonomyToolkit-License.txt".


System Requirements
-------------------

- The client machine must have PowerShell 3.0 or newer, with CLRVersion 4.0
  or newer

- The remote server must be running SharePoint Server 2013 Enterprise or newer

- The Managed Metadata Service must be enabled on the server

- The Client-Side Object Model (CSOM) service must be enabled on the server


Quick Start
-----------

1.  Extract the zip archive into a folder, e.g. C:\TaxonomyToolkit

2.  Open the Windows PowerShell command prompt.

3.  Check the $PSVersionTable variable to confirm that PSVersion is at least 3.0
    and CLRVersion is at least 4.0. If not, install the free upgrade from here:

    Windows Management Framework 3.0
    http://www.microsoft.com/en-us/download/details.aspx?id=34595

4.  To enable the TaxonomyToolkit commands, execute this command (substituting
    the folder location from step 1):

    Import-Module C:\TaxonomyToolkit\TaxonomyToolkit.PowerShell.psd1

5.  To see instructions for the cmdlets, execute these commands:

    Get-Help Export-Taxml -Full
    Get-Help Import-Taxml -Full

6.  To export taxonomy data from a SharePoint site:

    Export-Taxml -Path Output.taxml -SiteUrl http://www.example.com/ -Verbose
    
    NOTE: If your site is hosted by the Office 365 cloud service, include the 
    "-CloudCredential" switch in your command line.

7.  To create/update a SharePoint site with data from a TAXML file:

    Export-Taxml -Path Import.taxml -SiteUrl http://www.example.com/ -Verbose


More Information
----------------    

For additional documentation and discussions, please visit this web site:

    https://taxonomytoolkit.codeplex.com/documentation

