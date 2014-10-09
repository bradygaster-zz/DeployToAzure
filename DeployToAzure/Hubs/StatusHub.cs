using Microsoft.AspNet.SignalR;

namespace DeployToAzure.Hubs
{
    public class StatusHub : Hub
    {
        public void AddToGroup(string webSiteName)
        {
            base.Groups.Add(base.Context.ConnectionId, webSiteName);
        }
    }
}