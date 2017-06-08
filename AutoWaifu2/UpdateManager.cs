using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    class UpdateManager
    {
        public string UpdateInfoUrl { get; set; } = "http://autowaifu.azurewebsites.net/app/version";

        public class UpdateInfo
        {
            public string VersionString;
            public string NewVersionUrl;
        }

        public async Task<UpdateInfo> CheckForUpdates()
        {
            HttpClient client = new HttpClient();
            var updateFile = await client.GetStringAsync(UpdateInfoUrl);

            var updateLines = updateFile.Split('\n');
            
            var info = new UpdateInfo();
            info.VersionString = updateLines[0];
            info.NewVersionUrl = updateLines[1];

            return info;
        }

        //  TODO - Auto apply updates
    }
}
