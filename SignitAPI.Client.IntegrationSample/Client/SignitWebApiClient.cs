using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignitIntegrationSample.Models;
using SignitIntegrationSample.RemoteOrderService;

namespace SignitIntegrationSample.Client
{
    public class SignitWebApiClient : ISignitWebApiClient
    {
        private BearerToken _token = new BearerToken {Expires = DateTime.MinValue};

        public int MerchantId { get; set; }
        public string ApiBaseUri { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public BearerToken Token
        {
            get { return _token; }
            set { _token = value; }
        }

        public async Task<SigningOrder> InsertOrder(SigningOrder insertOrder, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<SigningOrder>(requestToken.Token, ApiBaseUri, "/umbraco/api/orderapi/postinsertorder",
                        insertOrder);
            return response;
        }

        public async Task<List<SigningOrder>> SearchOrders(string searchTerm, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<List<SigningOrder>>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/orderapi/getmyorders?searchTerm=" + searchTerm);
            return response;
        }

        public async Task<ConvertDocumentToValidPdfResponse> ConvertDocumentToValidPdf(
            ConvertDocumentToValidPdfRequest request, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<ConvertDocumentToValidPdfResponse>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/documentapi/ConvertDocumentToValidPdf", request);
            return response;
        }

        public async Task<GetOrderDetailsResponse> GetOrderDetails(GetOrderDetails getOrderDetails,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<GetOrderDetailsResponse>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/orderapi/getOrderDetails?orderId=" + getOrderDetails.OrderID);
            return response;
        }

        public async Task<GetSigningProcessesResponse> RequestSigningProcess(GetSigningProcesses getSigningProcesses,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<GetSigningProcessesResponse>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/orderapi/requestsigningprocess", getSigningProcesses);
            return response;
        }

        public async Task<PreInitOrderResponse> PreInitOrderBeforeSigning(PreInitOrder preInitOrder,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<PreInitOrderResponse>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/orderapi/PreInitOrderBeforeSigning", preInitOrder);
            return response;
        }

        public async Task<List<SigningGroupViewModel>> GetAccounts(BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<List<SigningGroupViewModel>>(requestToken.Token, ApiBaseUri,
                        "/umbraco/api/companyApi/GetMyCompanyAccounts");
            return response;
        }

        public async Task<BearerToken> GetApiToken()
        {
            if (DateTime.Now >= Token.Expires)
            {
                var token = await GetApiToken(UserName, Password);
                Token = token;
                return Token;
            }
            return Token;
        }

        public async Task<BearerToken> GetApiToken(string userName, string secret)
        {
            var token = new BearerToken();
            using (var client = new HttpClient())
            {
                //setup client
                client.BaseAddress = new Uri(ApiBaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //setup login data
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", userName),
                    new KeyValuePair<string, string>("password", secret),
                });

                //send request
                HttpResponseMessage responseMessage = await client.PostAsync("/Token", formContent);

                //get access token from response body
                var responseJson = await responseMessage.Content.ReadAsStringAsync();
                var jObject = JObject.Parse(responseJson);
                token.Expires = jObject.GetValue(".expires").ToObject<DateTime>();
                token.Token = jObject.GetValue("access_token").ToString();
            }
            return token;
        }

        private async Task<T> GetRequest<T>(string token, string apiBaseUri, string requestPath)
        {
            using (var client = new HttpClient())
            {
                //setup client
                client.BaseAddress = new Uri(apiBaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                //make request
                var response = await client.GetAsync(requestPath);
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase);
                }
                var responseString = await response.Content.ReadAsStringAsync();
                var jObject = JsonConvert.DeserializeObject<T>(responseString);
                return jObject;
            }
        }

        private async Task<T> PostRequest<T>(string token, string apiBaseUri, string requestPath, object data)
        {
            using (var client = new HttpClient())
            {
                //setup client
                client.BaseAddress = new Uri(apiBaseUri);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                //make request
                var response =
                    await
                        client.PostAsync(requestPath,
                            new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase);
                }
                var responseString = await response.Content.ReadAsStringAsync();
                var jObject = JsonConvert.DeserializeObject<T>(responseString);

                return jObject;
            }
        }

        
    }

    public class BearerToken
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
    }
}