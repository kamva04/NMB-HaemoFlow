using NMB_HLabSys.Models.Domain;

namespace NMB_HLabSys.Models.ViewModels
{
    public class MySupplyRequestsViewModel
    {
        public List<ConsumableRequest> Requests { get; set; } = new();

        public int PendingCount => Requests.Count(r => r.Status == "Pending");
        public int ApprovedCount => Requests.Count(r => r.Status == "Approved" || r.Status == "PartiallyApproved");
        public int RejectedCount => Requests.Count(r => r.Status == "Rejected");
        public int FulfilledCount => Requests.Count(r => r.Status == "Fulfilled");
    }
}