using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using Newtonsoft.Json;
using SignitIntegrationClient.Client;
using SignitIntegrationClient.RemoteOrderService;
using SignitIntegrationSample.Models;

namespace SignitIntegrationSample.Controllers
{
    public class SignitIntegrationApiController : ApiController
    {
        private readonly string _merchantLogin = ConfigurationManager.AppSettings["SignitMerchantName"];
        private readonly string _merchantPassword = ConfigurationManager.AppSettings["SignitMerchantPassword"];
        private readonly string _apiBaseUri = ConfigurationManager.AppSettings["SignitApiBaseUri"];
        private readonly string _apiExitUri = ConfigurationManager.AppSettings["SignitApiExitUri"];
        private readonly string _signitClientId = ConfigurationManager.AppSettings["SignitClientId"];
        private readonly string _signitClientSecret = ConfigurationManager.AppSettings["SignitClientSecret"];

        [HttpPost]
        public async Task<IHttpActionResult> RequestToken(TokenRequest request)
        {
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = request.UserName,
                Password = request.Secret,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret,
            };
            var token = await serviceClient.GetApiToken();
            return Ok(token);
        }

        [Route("api/v1/order")]
        public async Task<IHttpActionResult> Post()
        {   
            var root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            //Getting file data from request
            byte[] fileData = null;
            if (!Request.Content.IsMimeMultipartContent())
            {
                return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType, this);
            }
            var fileName = string.Empty;
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.FileData)
            {
                fileData = File.ReadAllBytes(file.LocalFileName);
                fileName = file.Headers.ContentDisposition.FileName.Replace("\"", "");
            }

            //We might need to get some additional settings
            var orderId = provider.FormData["OrderId"] == null ? Guid.NewGuid() : Guid.Parse(provider.FormData["OrderId"]);

            int ownerId;
            int.TryParse(provider.FormData["ownerId"], out ownerId);

            bool isPrivate;
            bool.TryParse(provider.FormData["isPrivate"], out isPrivate);
            var signers = JsonConvert.DeserializeObject<List<SignerDataViewModel>>(provider.FormData["signers"]);
            var message = provider.FormData["message"];
            var orderDeadline = DateTime.Now.AddDays(1);

            try
            {
                //Initializing the api client
                var serviceClient = new SignitWebApiClient
                {
                    ApiBaseUri = _apiBaseUri,
                    UserName = _merchantLogin,
                    Password = _merchantPassword,
                    ClientId = _signitClientId,
                    ClientSecret = _signitClientSecret,
                };
                //Requesting the document to be converted to proper format. Size limit here is 3MB.
                var documentToInclude = await GetDocumentToInclude(fileName, fileData, serviceClient);
               
                //Building the order entity
                var orderBatch = new SigningOrderModel
                {
                    UniqueId = orderId,
                    OrderDescription = message,
                    Deadline = orderDeadline,
                    Message = message,
                    OwnerId = ownerId,
                    IsPrivate = isPrivate,
                    Documents = new[]
                    {
                        documentToInclude
                    },
                    Signers = signers.ToArray(),
                    SigningExecutionDetails = new[]
                    {
                        new SigningExecutionDetailsViewModel
                        {
                            OrderDeadline = orderDeadline,
                            SigningSteps = new[]
                            {
                                new SigningStepViewModel
                                {
                                    StepDeadline = orderDeadline,
                                    //Has to always start from "1"
                                    StepNumber = 1,
                                    //Direct relation signer-document has to present
                                    SigningProcesses = signers.Select(signer=>new SigningProcessViewModel
                                    {
                                        LocalSignerReference = signer.LocalSignerReference,
                                        LocalDocumentReference = documentToInclude.LocalDocumentReference
                                    }).ToArray()
                                }
                            }
                        }
                    },
                    SigningWebContexts = 
                        signers.Select(x=>new SigningWebContextViewModel
                        {
                            ExitUrl = _apiExitUri+orderId.ToString(),
                            LocalWebContextRef = x.LocalSignerReference
                        }).ToArray()   
                };

                try
                {
                    var response = await serviceClient.InsertOrder(orderBatch);
                    return Ok(response.SigningOrder.UniqueId);
                }
                catch (CommunicationException e)
                {
                    return BadRequest("Communication error occured" + e.Message);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Route("api/v1/myaccounts")]
        public async Task<IHttpActionResult> GetMyAccounts()
        {
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret,
            };
            var tokenCookie = HttpContext.Current.Request.Cookies["AccessToken"];
            var tokenCookieExpires = HttpContext.Current.Request.Cookies["AccessTokenExpires"];
            if (tokenCookie != null && tokenCookieExpires != null)
                serviceClient.Token = new BearerToken { Token = tokenCookie.Value, Expires = DateTime.Parse(tokenCookieExpires.Value) };

            var accounts = await serviceClient.GetAccounts();
            return Ok(accounts.SigningGroups);
        }

        [Route("api/v1/details/{orderId}")]
        public async Task<IHttpActionResult> Get(Guid orderId)
        {
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret,
            };

            var tokenCookie = HttpContext.Current.Request.Cookies["AccessToken"];
            var tokenCookieExpires = HttpContext.Current.Request.Cookies["AccessTokenExpires"];
            if (tokenCookie != null && tokenCookieExpires != null)
                serviceClient.Token = new BearerToken { Token = tokenCookie.Value, Expires = DateTime.Parse(tokenCookieExpires.Value) };

            var details = await serviceClient.GetOrderDetails(orderId.ToString());
            return Ok(details);
        }

        [Route("api/v1/sign")]
        public async Task<IHttpActionResult> GetSignInfo(string localSignerReference, string orderId,
            string localDocumentReference,
            int successRedirectPage)
        {
            var model = new SigningModel();

            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret
            };

            var signingProcess =
                await serviceClient.RequestSigningProcess(new GetSigningProcesses
                {
                    LocalSignerReference = localSignerReference,
                    OrderID = orderId
                });

            var sref =
                HttpUtility.ParseQueryString(
                    new Uri(signingProcess.SigningProcessProcess.SigningProcessResults.First().SignUrl).Query)["sref"];

            model.Sref = sref;
            /*await serviceClient.PreInitOrderBeforeSigning(new PreInitOrder
            {
                Sref = sref,
                ExitUrl = _apiExitUri + orderId
            });*/

            return Ok(model);
        }

        [Route("api/v1/order/pades")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetPadEs(string orderId, string localDocumentReference)
        {
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret
            };
            var response = await serviceClient.GetPades(new GetPAdES
            {
                OrderID = orderId,
                LocalDocumentReference = localDocumentReference,
                PAdESDocumentReference = localDocumentReference,
            });

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(response.PAdESSignedDocumentBytes)
            };
            result.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment")
                {
                    FileName = localDocumentReference
                };
            result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/pdf");

            return result;
        }

        public async Task<DocumentViewModel> GetDocumentToInclude(string fileName, byte[] fileBytes, SignitWebApiClient serviceClient)
        {
            var document = new DocumentViewModel
            {
                DocumentType = DocumentType.Pdf,
                Description = "Sample document with filename: " + fileName,
                Title = fileName,
                LocalDocumentReference = fileName
            };
            if (fileName.ToLower().EndsWith("txt"))
            {
                document.DocumentType = DocumentType.Html;
                document.Base64Content = Convert.ToBase64String(fileBytes);
            }
            else if (fileName.ToLower().EndsWith("xml"))
            {
                document.DocumentType = DocumentType.Xml;
                document.Base64Content = Convert.ToBase64String(fileBytes);
            }
            else
            {
                var response = await serviceClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = Convert.ToBase64String(fileBytes),
                    DocumentName = fileName
                });
                var convertedDocumentBytes =
                        Convert.FromBase64String(response.Base64Content);
                document.Base64Content = Convert.ToBase64String(convertedDocumentBytes);
                document.LocalDocumentReference = fileName + ".pdf";
                document.Title = fileName + ".pdf";
            }
            return document;
        }
    }

    public class TokenRequest
    {
        public string UserName { get; set; }
        public string Secret { get; set; }
    }
}
