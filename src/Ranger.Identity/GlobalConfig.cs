using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Ranger.Identity
{
    public static class GlobalConfig
    {

        public static IdentityServerOptions IdentityServerOptions { get; set; }

        public static string TempPassword
        {
            get
            {
                return "fnV{Q$2Yk,Www`@+6uK?oe:sFSsH=AK$Zug6VsjT^AaT(xbD2_X*5VTLLrF#.8*P";
            }
        }
    }
}