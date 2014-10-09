# Deploy To Azure #

This is the code for the "Deploy to Azure" button, which is described in detail in [Brady Gaster's post on the topic of the button](http://www.bradygaster.com/post/the-deploy-to-azure-button), and in [Vittorio Bertocci's post taking a deeper dive on the identity components](http://www.cloudidentity.com/blog/2014/10/09/the-use-of-azure-ad-behind-deploy-to-azure/) of the button's functionality.

Deploy to Azure provides a one-click method of deploying applications to Azure Websites directly from GitHub repositories. 

## What is the Deploy to Azure Button? ##
If you want to deploy the code for a Website in a GitHub repository and you:

1. Have an Azure subscription
2. Have an Active Directory user in your subscription's directory with Global Administrator permissions
3. You're logged in with that Global Administrator user account 
4. You click the button below when you see it on a GitHub repository

... you'd be able to deploy the code in the GitHub repository to your own Azure subscription.

[![](http://deployto.azurewebsites.net/content/deploy-to-azure.png)](https://deployto.azurewebsites.net)

## Do You Develop Open-source Web Sites? ##
If so, feel free to use the Deploy to Azure button in your own GitHub repositories. **Deploy to Azure is still in beta**, so it may not work in every context, and has the above requirements for an Azure subscriber. That said, we'd love to have you try it out and provide feedback. Of course, if you have more ideas for improving the app, you could submit a pull request, too.  