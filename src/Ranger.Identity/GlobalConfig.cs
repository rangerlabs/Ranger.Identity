using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Ranger.Identity
{
    public static class GlobalConfig
    {
        public static string Host
        {
            get
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
                {
                    return "localhost.io";
                }
                else
                {
                    return "rangerlabs.io";
                }
            }
        }

        public static string PostLogoutRedirectUri
        {
            get
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
                {
                    return "localhost.io:8080";
                }
                else
                {
                    return "rangerlabs.io";
                }
            }
        }
    }
}