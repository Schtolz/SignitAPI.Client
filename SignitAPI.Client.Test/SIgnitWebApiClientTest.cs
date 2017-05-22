using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignitIntegrationClient.Client;
using SignitIntegrationClient.RemoteOrderService;

namespace WcfServicesTest
{
    [TestClass]
    public class SIgnitWebApiClientTest
    {
        private readonly string _merchantLogin = WebConfigurationManager.AppSettings["SignitMerchantName"];
        private readonly string _merchantPassword = WebConfigurationManager.AppSettings["SignitMerchantPassword"];
        private readonly string _apiBaseUri = WebConfigurationManager.AppSettings["SignitApiBaseUri"];
        private readonly string _signitClientId = WebConfigurationManager.AppSettings["SignitClientId"];
        private readonly string _signitClientSecret = WebConfigurationManager.AppSettings["SignitClientSecret"];
        private  ISignitWebApiClient _signitWebApiClient;

        [TestInitialize]
        public void Init()
        {
            ServicePointManager
                .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
            _signitWebApiClient = new SignitWebApiClient
            {
                ApiBaseUri = _apiBaseUri,
                UserName = _merchantLogin,
                Password = _merchantPassword,
                ClientId = _signitClientId,
                ClientSecret = _signitClientSecret,
            };
        }

        [TestMethod]
        public void Can_send_email()
        {
            var orderId = "54761e1b-4e00-49f9-9f56-79fa40f16ba3";
            var localSignerReference = "sko@nosunset.com";

            var responseAsync = _signitWebApiClient.SendSigningEmail(new SendSigningEmailRequest {LocalSignerReference = localSignerReference, OrderId = orderId});
            var response = responseAsync.Result;
            Assert.IsTrue(response.OrderId==orderId);
            Assert.IsTrue(response.ResponseTest.ToLower().Contains("email"));
        }

        [TestMethod]
        public void Can_create_order()
        {
            var fileName = "test.txt";
            var orderDescription = "Test description";
            var deadline = DateTime.Now.AddDays(1);

            var fileData = Encoding.UTF8.GetBytes("Test string for conversion");

            //Requesting the document to be converted to proper format. Size limit here is 3MB.
            var documentToIncludeTask = GetDocumentToInclude(fileName, fileData, _signitWebApiClient);
            var documentToInclude = documentToIncludeTask.Result;
            var uniqueId = Guid.NewGuid();
            var localSignerReference = "sko@nosunset.com";
            //Building the order entity
            var orderBatch = new SigningOrderModel
            {
                UniqueId = uniqueId,
                OrderDescription = orderDescription,
                Deadline = deadline,
                Message = orderDescription,
                IsPrivate = false,
                Documents = new[]
                {
                    documentToInclude
                },
                Signers = new[]
                {
                    new SignerDataViewModel
                    {
                        Email = "sko@nosunset.com",
                        LocalSignerReference = localSignerReference,
                        Name = "sko@nosunset.com"
                    }
                },
                SigningExecutionDetails = new[]
                {
                    new SigningExecutionDetails
                    {
                        OrderDeadline = deadline,
                        SigningSteps = new[]
                        {
                            new SigningStep
                            {
                                StepDeadline = deadline,
                                //Has to always start from "1"
                                StepNumber = 1,
                                //Direct relation signer-document has to present
                                SigningProcesses = new []
                                {
                                    new SigningProcess
                                    {
                                        LocalSignerReference = localSignerReference,
                                        LocalDocumentReference = documentToInclude.LocalDocumentReference
                                    }
                                }
                            }
                        }
                    }
                },
                SigningWebContexts = new []
                {
                    new SigningWebContextModel
                    {
                        ExitUrl = "http://localhost:7077/cart/exit",
                        LocalWebContextRef = localSignerReference,
                    } 
                }
                
            };
            var responseTask = _signitWebApiClient.InsertOrder(orderBatch);
            var response = responseTask.Result;
            Assert.IsTrue(response.SigningOrder.UniqueId == uniqueId);
            Assert.IsTrue(response.SigningOrder.SigningWebContexts.First().LocalWebContextRef.Equals(localSignerReference));
        }

        [TestMethod]
        public void Can_convert_document()
        {
            var fileName = "test.txt";
            var fileBytes = Encoding.UTF8.GetBytes("Test string for conversion");
            var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
            {
                Base64Content = Convert.ToBase64String(fileBytes),
                DocumentName = fileName
            });
            var response = responseTask.Result;
            Assert.IsNotNull(response);
            var convertedDocumentBytes =
                    Convert.FromBase64String(response.Base64Content);
            Assert.IsTrue(convertedDocumentBytes.Length>0);

            Assert.IsTrue(response.DocumentName == fileName);
        }

        [TestMethod]
        public void Can_convert_document_Fails_Validations()
        {
            var fileName = "test.txt";
            var wrongFileName = "test.tif";
            var longFileName = "test.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txttest.txt";
           
            var fileBytes = Encoding.UTF8.GetBytes("Test string for conversion");
            try
            {
                Console.WriteLine("Checking empty name");
                var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = Convert.ToBase64String(fileBytes),
                    DocumentName = string.Empty
                }).Result;
                Assert.Fail("We have to get exception when sending empty document name. But response was: "+responseTask);
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerExceptions.First().Message.Contains("The DocumentName field is required"));
            }

            try
            {
                Console.WriteLine("Checking empty content");
                var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = string.Empty,
                    DocumentName = fileName
                }).Result;
                Assert.Fail("We have to get exception when sending empty document name. But response was: " + responseTask);
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerExceptions.First().Message.Contains("The Base64Content field is required"));
            }

            try
            {
                Console.WriteLine("Checking file name is too long");
                var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = Convert.ToBase64String(fileBytes),
                    DocumentName = longFileName
                }).Result;
                Assert.Fail("We have to get exception when sending empty document name. But response was: " + responseTask);
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerExceptions.First().Message.Contains("55"));
            }

            try
            {
                Console.WriteLine("Checking file type is wrong");
                var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = Convert.ToBase64String(fileBytes),
                    DocumentName = wrongFileName
                }).Result;
                Assert.Fail("We have to get exception when sending empty document name. But response was: " + responseTask);
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerExceptions.First().Message.Contains("Forkert filtype"));
            }

            try
            {
                Console.WriteLine("Checking file size limitation of 3 megabytes");
                var responseTask = _signitWebApiClient.ConvertDocumentToValidPdf(new ConvertDocumentToValidPdfRequest
                {
                    Base64Content = Convert.ToBase64String(new byte[2800000]),
                    DocumentName = fileName
                }).Result;

                Assert.Fail("We have to get exception when sending empty document name. But response was: " + responseTask);
            }
            catch (AggregateException e)
            {
                Assert.IsTrue(e.InnerExceptions.First().Message.Contains("3 Megabyte"));
            }
        }





        public async Task<DocumentViewModel> GetDocumentToInclude(string fileName, byte[] fileBytes, ISignitWebApiClient serviceClient)
        {
            var document = new DocumentViewModel
            {
                DocumentType = DocumentType.Pdf,
                Description = "Sample document with filename: " + fileName,
                Title = fileName,
                LocalDocumentReference = fileName
            };

            if (fileName.ToLower().EndsWith("xml"))
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
}
