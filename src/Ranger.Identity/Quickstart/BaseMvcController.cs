using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Ranger.Common;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    public class BaseMvcController : Controller
    {
        protected string Domain
        {
            get
            {
                return Request.Host.GetDomainFromHost();
            }
        }
    }
}