using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    
    [ApiController]
    public class MainController : ControllerBase
    {

        protected IConfiguration configuration;

        public MainController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }



        protected async Task SetResponseJSONAsync(bool isError, object result)
        {

            Response.ContentType = "application/json";

            var o = new
            {
                code = isError ? 500 : 200,
                result = result
            };

            var bResponse = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(o));

            await Response.Body.WriteAsync(bResponse, 0, bResponse.Length);

        }

        protected async Task SetSuccessResponseJSONAsync(object result)
        {
            await SetResponseJSONAsync(false, result);
        }

        protected async Task SetErrorResponseJSONAsync(object result)
        {
            await SetResponseJSONAsync(true, result);
        }

    }
}
