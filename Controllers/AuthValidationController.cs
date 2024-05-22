using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AgentValidationsFunctionSample.Controllers
{
    [Route("api/auth-validation")]
    public class AuthValidationController : ControllerBase
    {
        [HttpPost]
        [Route("validate-insured-registration")]
        public async Task<IActionResult> ValidateInsuredRegistration([FromBody] object modelObj)
        {
            var modelString = modelObj.ToString();
            JsonSerializerSettings jsonSettings = new()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            InsuredValidationRequest? insuredValidationRequest = null;

            if (!string.IsNullOrEmpty(modelString))
            {
                insuredValidationRequest = JsonConvert.DeserializeObject<InsuredValidationRequest>(modelString, jsonSettings);
            }
            //Checking the validations here.
            if (insuredValidationRequest != null && !string.IsNullOrEmpty( insuredValidationRequest.SurName) && !insuredValidationRequest.SurName.ToLower().Contains("fail"))
            {
                return Ok();
            }
            else
            {
                var insuredValidationResponse = new InsuredValidationResponse
                {
                    version = "1.0.0",
                    status = 409,
                    code = "API12345",
                    requestId = "50f0bd91-2ff4-4b8f-828f-00f170519ddb",
                    userMessage = "Message for the user",
                    developerMessage = $"You are not authorized to sign up for an account. {modelString}",
                    moreInfo = "https://restapi/error/API12345/moreinfo"
                };
                return Conflict(insuredValidationResponse);
            }
        }
    }

    public class InsuredValidationResponse
    {
        public string version { get; set; }
        public int status { get; set; }
        public string code { get; set; }
        public string userMessage { get; set; }
        public string developerMessage { get; set; }
        public string requestId { get; set; }
        public string moreInfo { get; set; }
    }

    public class InsuredValidationRequest
    {
        public string Email { get; set; }
        public string extension_ClientCode { get; set; }
        public string SurName { get; set; }
        public string GivenName { get; set; }
    }

}