using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Ranger.RabbitMQ;

namespace Ranger.Identity
{
    public class BaseApiController : ControllerBase
    {
        protected IActionResult InternalServerError(string errors = "")
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { errors = errors });
        }

        protected string Domain
        {
            get
            {
                return Request.Headers["x-ranger-domain"].First();
            }
        }
    }
}