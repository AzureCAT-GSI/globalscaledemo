# Global Demo
This sample demonstrates multiple Azure services in an active-active replication scenario.  From deployment using ARM to securing access to Azure storage, this sample has a number of interesting patterns and approaches to use in your solutions. 

The sample starts with an Azure Resource Manager template deployment that provisions the necessary services for multiple regions around the world and updates the web configuration for every region.
The deployment ARM template deploys the web application from this Git repository, including a Web Job that is used for asynchronous processing.
The web application shows how to create an Angular client that uploads directly to Azure storage using the library written by Stephen Brannan (https://github.com/kinstephen/angular-azure-blob-upload).
The Angular client accesses a Web API that uses output caching, leveraging a library written by Filip W (https://github.com/filipw/AspNetWebApi-OutputCache/).
When the file is uploaded, it is replicated to multiple storage accounts around the world, a thumbnail is created locally in each region, and the local Redis cache is updated.

<a href="http://armviz.io/#/?load=https://raw.githubusercontent.com/kaevans/GlobalDemo/GlobalDemo.Deploy/Templates/WebSite.json" target="_blank">
  <img src="http://armviz.io/visualizebutton.png"/>
</a>
  
##Deployment
Deployment is performed using an Azure Resource Manager template.  The template will deploy the solution to as many regions as you wish, creating a local storage account and Azure Redis Cache.

1. Clone this repository locally.  
2. Create an Azure AD application as shown above.  
3. Update the app.js file with the tenant and client ID for your Azure AD application.
4. Update the WebSite.param.dev.json file with the parameters for your deployment.  For the siteLocations parameter, provide the names of the Azure regions that the solution will be deployed to.  
5. Open Windows PowerShell and run the DeployAzureResourceGroup.ps1.  

Alternatively, open the solution in Visual Studio 2015.  Right-click on the GlobalDemo.Deploy project and choose Deploy.  Provide the subscription and resource group as well as parameter values.

The following parameters are used:
**uniqueDnsName**
*	Not implemented yet, but will be used when I get around to adding Traffic Manager to the template deployment
**siteLocations**
*	An array of locations for the solution to be deployed to.  Note a web application, storage account, and Redis cache are deployed to each region.
**aadTenant**
*	The AAD tenant (such as contoso.onmicrosoft.com) used to authenticate.  See the Authentication section below for more information.
**aadAudience**
*	The client ID for the AAD application.  See the Authentication section below for more information.


## Authentication
Getting started is simple!  To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [http://www.windowsazure.com](http://www.windowsazure.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:
`git clone https://github.com/Azure-Samples/SinglePageApp-DotNet.git`

### Step 2:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "SinglePageApp-DotNet", select "Web Application and/or Web API", and click next.
8. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44326/`.
9. For the App ID URI, enter `https://<your_tenant_name>/SinglePageApp-DotNet`, replacing `<your_tenant_name>` with the name of your Azure AD tenant.

All done!  Before moving on to the next step, you need to find the Client ID of your application.

1. While still in the Azure portal, click the Configure tab of your application.
2. Find the Client ID value and copy it to the clipboard.


### Step 3:  Enable the OAuth2 implicit grant for your application

By default, applications provisioned in Azure AD are not enabled to use the OAuth2 implicit grant. In order to run this sample, you need to explicitly opt in.

1. From the former steps, your browser should still be on the Azure management portal - and specifically, displaying the Configure tab of your application's entry.
2. Using the Manage Manifest button in the drawer, download the manifest file for the application and save it to disk.
3. Open the manifest file with a text editor. Search for the `oauth2AllowImplicitFlow` property. You will find that it is set to `false`; change it to `true` and save the file.
4. Using the Manage Manifest button, upload the updated manifest file. Save the configuration of the app.

### Step 4:  Configure the sample to use your Azure Active Directory tenant to debug locally

1. Open the solution in Visual Studio 2015.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
4. Find the app key `ida:Audience` and replace the value with the Client ID from the Azure portal.
5. Open the file `App/Scripts/App.js` and locate the line `adalAuthenticationServiceProvider.init(`.
6. Replace the value of `tenant` with your AAD tenant name.
7. Replace the value of `clientId` with the Client ID from the Azure portal.

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it. 

You can trigger the sign in experience by either clicking on the sign in link on the top right corner, or by clicking directly on the My Photos tab.
Explore the sample by signing in, uploading a photo, and playing with the cache to see performance implications. 
Notice that you can close and reopen the browser without losing your session. ADAL JS saves tokens in localStorage and keeps them there until you sign out.

