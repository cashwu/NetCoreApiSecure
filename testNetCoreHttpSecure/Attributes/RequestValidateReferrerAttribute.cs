using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace testNetCoreHttpSecure.Attributes
{
    public class RequestValidateReferrerAttribute : ActionFilterAttribute
    {
        private IConfiguration _configuration;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            base.OnActionExecuting(context);

            if (IsValidRequest(context.HttpContext.Request))
            {
                return;
            }

            context.Result = new ContentResult
            {
                Content = "Invalid referer header",
            };

            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.ExpectationFailed;
        }

        private bool IsValidRequest(HttpRequest request)
        {
            var referrerUrl = "";

            if (request.Headers.ContainsKey("Referer"))
            {
                referrerUrl = request.Headers["Referer"];
            }

            if (string.IsNullOrWhiteSpace(referrerUrl))
            {
                return true;
            }

            var configCorsOrigin = _configuration.GetSection("CorsOrigin").Get<string[]>();

            if (configCorsOrigin == null || configCorsOrigin.Length == 0)
            {
                return true;
            }

            return configCorsOrigin.Select(url => new Uri(url).Authority)
                                   .Contains(new Uri(referrerUrl).Authority);
        }
    }
}