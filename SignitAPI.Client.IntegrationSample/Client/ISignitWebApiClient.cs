using System.Collections.Generic;
using System.Threading.Tasks;
using SignitIntegrationSample.Models;
using SignitIntegrationSample.RemoteOrderService;

namespace SignitIntegrationSample.Client
{
    public interface ISignitWebApiClient
    {
        string ApiBaseUri { get; set; }
        int MerchantId { get; set; }
        string Password { get; set; }
        BearerToken Token { get; set; }
        string UserName { get; set; }

        Task<ConvertDocumentToValidPdfResponse> ConvertDocumentToValidPdf(ConvertDocumentToValidPdfRequest request, BearerToken token = null);
        Task<List<SigningGroupViewModel>> GetAccounts(BearerToken token = null);
        Task<BearerToken> GetApiToken();
        Task<BearerToken> GetApiToken(string userName, string secret);
        Task<GetOrderDetailsResponse> GetOrderDetails(GetOrderDetails getOrderDetails, BearerToken token = null);
        Task<SigningOrder> InsertOrder(SigningOrder insertOrder, BearerToken token = null);
        Task<PreInitOrderResponse> PreInitOrderBeforeSigning(PreInitOrder preInitOrder, BearerToken token = null);
        Task<GetSigningProcessesResponse> RequestSigningProcess(GetSigningProcesses getSigningProcesses, BearerToken token = null);
        Task<List<SigningOrder>> SearchOrders(string searchTerm, BearerToken token = null);
    }
}