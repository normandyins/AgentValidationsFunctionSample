using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace InsuredValidationsFunctionSample.Controllers
{
    [Route("api/auth-validation")]
    public class AuthValidationController : ControllerBase
    {
        [HttpPost]
        [Route("validate-insured-registration")]
        [Consumes("application/xml", "application/json")]
        [Produces("application/xml", "application/json")]
        public async Task<IActionResult> ValidateInsuredRegistration([FromBody] object obj)
        {

            var modelString = obj.ToString();

            var cert = this.HttpContext.Connection.GetClientCertificateAsync().Result;
            JsonSerializerSettings jsonSettings = new()
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            UserInfo? insuredValidationRequest = null;

            if (!string.IsNullOrEmpty(modelString))
            {
                insuredValidationRequest = JsonConvert.DeserializeObject<UserInfo>(modelString, jsonSettings);
            }

            //Checking the validations here.
            //Dummy: Right now, if you put keyword "fail" in the surName field or the field is missing, it will throw the error.
            if (insuredValidationRequest != null && !string.IsNullOrEmpty(insuredValidationRequest.LastName) && !insuredValidationRequest.LastName.ToLower().Contains("fail"))
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
                    //Dummy: Concatinating the request payload, just for troubleshooting, so that we can see if all the fields are coming through.
                    developerMessage = $"You are not authorized to sign up for an account.",
                    moreInfo = "https://restapi/error/API12345/moreinfo"
                };
                return Conflict(insuredValidationResponse);
            }

        }
        [HttpPost]
        [Route("validate-insured-registration1")]
        [Consumes("application/xml", "application/json")]
        [Produces("application/xml", "application/json")]
        //[Consumes("application/xml")]
        //[Produces("application/xml")]
        public async Task<IActionResult> ValidateInsuredRegistration1([FromBody] object obj)
        {

            var modelString = obj.ToString();

            var cert = this.HttpContext.Connection.GetClientCertificateAsync().Result;

            string headers = "";
            foreach (var header in Request.Headers)
            {
                headers +=" | "+($"{header.Key}: {header.Value}");
            }


            var insuredValidationResponse = new InsuredValidationResponse
            {
                version = "1.0.0",
                status = 409,
                code = "API12345",
                requestId = "50f0bd91-2ff4-4b8f-828f-00f170519ddb",
                userMessage = "Message for the user",
                //Dummy: Concatinating the request payload, just for troubleshooting, so that we can see if all the fields are coming through.
                developerMessage = cert != null ? "The Certificate is Present" + cert.FriendlyName+ headers : "The Certificate is not available. "+ headers,
                moreInfo = "https://restapi/error/API12345/moreinfo"
            };
            return Conflict(insuredValidationResponse);
        }


        public static XmlDocument CreateXmlDoc(string EmailAddress, string CompanyID)
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement bodyElement = doc.CreateElement(string.Empty, "body", string.Empty);
            doc.AppendChild(bodyElement);

            XmlElement userElement = doc.CreateElement(string.Empty, "UserInfo", string.Empty);
            bodyElement.AppendChild(userElement);

            XmlElement emailElement = doc.CreateElement(string.Empty, "EmailAddress", string.Empty);
            XmlText emailText = doc.CreateTextNode(EmailAddress);
            emailElement.AppendChild(emailText);
            userElement.AppendChild(emailElement);

            XmlElement companyElement = doc.CreateElement(string.Empty, "CompanyID", string.Empty);
            XmlText companyText = doc.CreateTextNode(CompanyID);
            companyElement.AppendChild(companyText);
            userElement.AppendChild(companyElement);

            return doc;
        }
        public static XmlDocument SignWithX509(XmlDocument xmlDoc, string certPath, string certPassword)
        {
            try
            {
                var certBytes = System.IO.File.ReadAllBytes(certPath);

                X509Certificate2 cert = new X509Certificate2(certBytes, certPassword);
                var privateKey = cert.GetRSAPrivateKey();

                if (xmlDoc == null)
                    throw new ArgumentException(null, nameof(xmlDoc));
                if (privateKey == null)
                    throw new ArgumentException(null, nameof(privateKey));

                SignedXml signedXml = new(xmlDoc)
                {
                    SigningKey = privateKey
                };

                // Create a reference to be signed.
                Reference reference = new()
                {
                    Uri = ""
                };

                // Add an enveloped transformation to the reference.
                XmlDsigEnvelopedSignatureTransform env = new();
                reference.AddTransform(env);

                // Add the reference to the SignedXml object.
                signedXml.AddReference(reference);

                // Compute the signature.
                signedXml.ComputeSignature();

                // Get the XML representation of the signature and save
                // it to an XmlElement object.
                XmlElement xmlDigitalSignature = signedXml.GetXml();

                // Append the element to the XML document.
                xmlDoc.DocumentElement?.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return xmlDoc;
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

    public class UserInfo
    {
        public string EmailAddress { get; set; }
        public string CompanyID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

}
