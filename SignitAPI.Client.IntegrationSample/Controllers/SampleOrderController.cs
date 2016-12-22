using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SignitIntegrationClient.Client;
using SignitIntegrationClient.RemoteOrderService;
using SignitIntegrationSample.Models;

namespace SignitIntegrationSample.Controllers
{
    public class SampleOrderController : Controller
    {
        //
        // GET: /SampleOrder/
        private const string LocalSignerReference = "Fluffy Simpskin";
        private readonly string _merchantLogin = ConfigurationManager.AppSettings["SignitMerchantName"];
        private readonly string _merchantPassword = ConfigurationManager.AppSettings["SignitMerchantPassword"];
        private readonly string _apiBaseUri = ConfigurationManager.AppSettings["SignitApiBaseUri"];
        private readonly string _signitClientId = ConfigurationManager.AppSettings["SignitClientId"];
        private readonly string _signitClientSecret = ConfigurationManager.AppSettings["SignitClientSecret"];

        private const string LocalDocumentReference = "My local document reference";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult IndexApi()
        {
            return View();
        }

        /*[HttpPost]
        public async Task<ActionResult> PostCreateOrderJson(string someString)
        {
            var orderId = Guid.NewGuid();
            var message = "Test message";
            var messageId = "SampleMessage";
            byte[] fileData = null;
            using (var binaryReader = new BinaryReader(Request.Files[0].InputStream))
            {
                fileData = binaryReader.ReadBytes(Request.Files[0].ContentLength);
            }
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret
            };

            var documentToInclude =
                await GetDocumentToInclude(Request.Files[0].FileName, fileData, serviceClient);

            var orderDeadline = DateTime.Now.AddDays(1);
            try
            {
                var orderBatch = new SigningOrder
                {
                    UniqueId = orderId,
                    OrderDescription = message,
                    Deadline = orderDeadline,
                    Documents = new[]
                    {
                        documentToInclude
                    },
                    SignersInfoes = new[]
                    {
                        new SignerData
                        {
                            LocalSignerReference = LocalSignerReference,
                            Name = LocalSignerReference,
                            DateSigned = DateTime.Now
                        }
                    },
                    SigningExecutionDetails = new[]
                    {
                        new SigningExecutionDetails
                        {
                            OrderDeadline = orderDeadline,
                            SigningSteps = new[]
                            {
                                new SigningStep
                                {
                                    StepDeadline = orderDeadline,
                                    StepNumber = 1,
                                    SigningProcesses = new[]
                                    {
                                        new SigningProcess
                                        {
                                            LocalSignerReference = LocalSignerReference,
                                            LocalDocumentReference = LocalDocumentReference
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                try
                {
                    var response = await serviceClient.InsertOrder(orderBatch);
                    return Json(new
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = response.UniqueId
                    });
                }
                catch (CommunicationException e)
                {
                    return Json(new
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = "Communication error occured" + e.Message
                    });
                }
            }
            catch (Exception e)
            {
                return Json(new
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = e.Message
                });
            }
        }*/

        /*public async Task<ActionResult> DetailsJson(Guid orderId)
        {
            var serviceClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret
            };

            var model = new OrderDetailsModel
            {
                LocalDocumentReference = LocalDocumentReference,
                LocalSignerReference = LocalSignerReference,
                OrderId = orderId.ToString(),
                SuccessRedirectPage = 0
            };
            try
            {
                var details = await serviceClient.GetOrderDetails(new GetOrderDetails
                {
                    OrderID = orderId.ToString()
                });
                model.OrderStatus = details.OrderStatus;
                model.LocalSignerReference =
                    details.ExecutionDetails.Steps.StepDetails.First()
                        .SigningProcessDetails.First()
                        .LocalSignerReference;
                model.LocalDocumentReference =
                    details.Documents.DocumentDetails.First()
                        .LocalDocumentReference;
            }
            catch (CommunicationException e)
            {
                return Json(new
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = e.Message
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                StatusCode = HttpStatusCode.OK,
                Content = model
            }, JsonRequestBehavior.AllowGet);
        }
        */

        public async Task<ActionResult> SignJson(string localSignerReference, string orderId,
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
            await serviceClient.PreInitOrderBeforeSigning(new PreInitOrder
            {
                Sref = sref,
                ExitUrl = "http://localhost:61147/SampleOrder/IndexApi#/details/" + orderId
            }
                );

            return Json(new
            {
                StatusCode = HttpStatusCode.OK,
                Content = model
            }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult IndexTemplate()
        {
            return View();
        }

        public ActionResult DetailsTemplate()
        {
            return View();
        }

        public ActionResult SignTemplate()
        {
            return View();
        }

        public async Task<Document> GetDocumentToInclude(string fileName, byte[] fileBytes,
            SignitWebApiClient serviceClient)
        {
            var document = new Document
            {
                DocumentType = DocumentType.Pdf,
                Description = "Sample document with filename: " + fileName,
                Title = fileName,
                LocalDocumentReference = fileName
            };

            if (fileName.ToLower().EndsWith("xml"))
            {
                document.DocumentType = DocumentType.Xml;
                document.Base64Contents = Convert.ToBase64String(fileBytes);
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
                document.Base64Contents = Convert.ToBase64String(convertedDocumentBytes);
                document.LocalDocumentReference = fileName;
                document.Title = fileName;
            }
            return document;
        }




        public async Task<ActionResult> DownloadPAdES(string orderId, string localDocumentReference)
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
            return File(response.PAdESSignedDocumentBytes, "application/pdf",
                    "PAdES file.pdf");
        }

    }
}

