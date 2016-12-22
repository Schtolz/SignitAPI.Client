using SignitIntegrationClient.RemoteOrderService;

namespace SignitIntegrationClient.Models
{
    public class OrderDetailsModel
    {
        public string LocalSignerReference { get; set; }
        public string OrderId { get; set; }
        public string LocalDocumentReference { get; set; }
        public int SuccessRedirectPage { get; set; }
        public GetOrderStatusResponseOrderStatus OrderStatus { get; set; }

    }
}