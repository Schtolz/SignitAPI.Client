using System.Threading.Tasks;
using SignitIntegrationClient.RemoteOrderService;

namespace SignitIntegrationClient.Client
{
    public interface ISignitWebApiClient
    {
        string ApiBaseUri { get; set; }
        int MerchantId { get; set; }
        string Password { get; set; }
        BearerToken Token { get; set; }
        string UserName { get; set; }
        /// <summary>
        /// Tries to convert provided jpg/txt/pdf document to pdf/a format to be used later in order creation
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<ConvertDocumentToValidPdfResponse> ConvertDocumentToValidPdf(ConvertDocumentToValidPdfRequest request, BearerToken token = null);
        /// <summary>
        /// Requests API token based on "password" grant process. Requires login, password, clientId and secret provided
        /// </summary>
        /// <returns></returns>
        Task<BearerToken> GetApiToken();
        /// <summary>
        /// This method gets access token based on user login and password. Client id and secret should also be provided. Obtain those when editing company account settings.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns></returns>
        Task<BearerToken> GetApiToken(string userName, string password, string clientId, string clientSecret);
        /// <summary>
        /// Returns expanded signing order details
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<GetSigningOrderDetailsResponse> GetOrderDetails(string orderId, BearerToken token = null);
        /// <summary>
        /// Creates new signing order and triggers notifications and signing processes
        /// </summary>
        /// <param name="insertOrder"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<PutSigningOrderResponse> InsertOrder(SigningOrderModel insertOrder, BearerToken token = null);
        /// <summary>
        /// Required to setup redirect paths. Otherwise all redirects will turn to signit.
        /// </summary>
        /// <param name="preInitOrder"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<PreInitOrderResponse> PreInitOrderBeforeSigning(PreInitOrder preInitOrder, BearerToken token = null);
        /// <summary>
        /// Returns signing process by search request that includes signing link, sref, and signer information
        /// </summary>
        /// <param name="getSigningProcesses"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<RequestSigningProcessResponse> RequestSigningProcess(GetSigningProcesses getSigningProcesses, BearerToken token = null);

        /// <summary>
        /// Looks for an order based on search term. Will be expanded with additional information soon
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="localSignerReference">Signer reference. Will look into all orders containing this one as a signer</param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<GetMyOrdersResponse> SearchOrders(string searchTerm=null, string localSignerReference=null, BearerToken token = null);
        /// <summary>
        /// Returns current logged in user company accounts. It allows to create orders on behalf of company that user is in by setting "OwnerId" in order request
        /// </summary>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<GetMyCompanyAccountsResponse> GetAccounts(BearerToken token = null);
        /// <summary>
        /// Sends email that contains required information to sign the order on signit.dk
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token">You can provide custom token here. F.ex. requested via oAuth process</param>
        /// <returns></returns>
        Task<SendSigningEmailResponse> SendSigningEmail(SendSigningEmailRequest request,
            BearerToken token = null);
    }
}