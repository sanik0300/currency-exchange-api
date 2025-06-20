using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace CurrencyExchangeAPI.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UppercaseCodeFilterAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuted(ResourceExecutedContext context) { }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            string? codeValue = context.HttpContext.Request.Query["code"].FirstOrDefault();

            if (!string.IsNullOrEmpty(codeValue))
            {
                Dictionary<string, StringValues> queryParams = new Dictionary<string, StringValues>()
                {
                    { "code", codeValue.ToUpper() }
                };

                context.HttpContext.Request.Query = new QueryCollection(queryParams);
            }
        }
    }
}
