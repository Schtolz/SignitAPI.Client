using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignitIntegrationClient.RemoteOrderService;

namespace SignitIntegrationClient.Client
{
    public class SignitWebApiClient : ISignitWebApiClient
    {
        private BearerToken _token = new BearerToken {Expires = DateTime.MinValue};

        public int MerchantId { get; set; }
        public string ApiBaseUri { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public BearerToken Token
        {
            get { return _token; }
            set { _token = value; }
        }

        public async Task<PutSigningOrderResponse> InsertOrder(SigningOrderModel insertOrder, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PutRequest<PutSigningOrderResponse>(requestToken.Token, ApiBaseUri, "/api/v1/order",
                        insertOrder);
            return response;
        }

        public async Task<GetMyOrdersResponse> SearchOrders(string searchTerm=null, string localSignerReference=null, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<GetMyOrdersResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/order/mine?searchTerm=" + searchTerm+"&localSignerReference="+localSignerReference);
            return response;
        }

        public async Task<ConvertDocumentToValidPdfResponse> ConvertDocumentToValidPdf(
            ConvertDocumentToValidPdfRequest request, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<ConvertDocumentToValidPdfResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/document/ConvertDocumentToValidPdf", request);
            return response;
        }

        public async Task<GetSigningOrderDetailsResponse> GetOrderDetails(string orderId,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<GetSigningOrderDetailsResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/order/" + orderId);
            return response;
        }

        public async Task<RequestSigningProcessResponse> RequestSigningProcess(GetSigningProcesses getSigningProcesses,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<RequestSigningProcessResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/order/requestsigningprocess", getSigningProcesses);
            return response;
        }

        public async Task<PreInitOrderResponse> PreInitOrderBeforeSigning(PreInitOrder preInitOrder,
            BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<PreInitOrderResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/order/PreInitOrderBeforeSigning", preInitOrder);
            return response;
        }

        public async Task<GetMyCompanyAccountsResponse> GetAccounts(BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<GetMyCompanyAccountsResponse>(requestToken.Token, ApiBaseUri,
                        "/api/v1/company/GetMyCompanyAccounts");
            return response;
        }

        public async Task<GetPAdESResponse> GetPades(GetPAdES request, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    GetRequest<GetPAdESResponse>(requestToken.Token, ApiBaseUri,
                        string.Format(
                            "/api/v1/document/GetPades?orderId={0}&IncludeSSN={1}&Language={2}&LocalDocumentReference={3}&PAdESDocumentReference={4}",
                            request.OrderID, request.IncludeSSN, request.Language, request.LocalDocumentReference,
                            request.PAdESDocumentReference));
            return response;
        }

        public async Task<SendSigningEmailResponse> SendSigningEmail(SendSigningEmailRequest request, BearerToken token = null)
        {
            var requestToken = token ?? await GetApiToken();
            var response =
                await
                    PostRequest<SendSigningEmailResponse>(requestToken.Token, ApiBaseUri, "/api/v1/order/sendsigningemail", request);
            return response;
        }

        public async Task<BearerToken> GetApiToken()
        {
            if (DateTime.Now >= Token.Expires)
            {
                var token = await GetApiToken(UserName, Password, ClientId, ClientSecret);
                Token = token;
                return Token;
            }
            return Token;
        }

        public async Task<BearerToken> GetApiToken(string userName, string password, string clientId, string clientSecret)
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
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret)
                });

                //send request
                var response = await client.PostAsync("/Token", formContent);

                //get access token from response body
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase + " " + responseString);
                }
                var jObject = JObject.Parse(responseString);
                token.Expires = DateTime.Now.AddSeconds(int.Parse(jObject.GetValue("expires_in").ToString()));
                token.Token = jObject.GetValue("access_token").ToString();
                token.RefreshToken = jObject.GetValue("refresh_token").ToString();
                token.TokenType = jObject.GetValue("token_type").ToString();
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
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase + " " + responseString);
                }
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
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase + " "+ responseString);
                }
                
                var jObject = JsonConvert.DeserializeObject<T>(responseString);

                return jObject;
            }
        }

        private async Task<T> PutRequest<T>(string token, string apiBaseUri, string requestPath, object data)
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
                        client.PutAsync(requestPath,
                            new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                var responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new CommunicationException(response.ReasonPhrase + " " + responseString);
                }
                var jObject = JsonConvert.DeserializeObject<T>(responseString);

                return jObject;
            }
        }


    }

    public class BearerToken
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public DateTime Expires { get; set; }
    }
}