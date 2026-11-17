using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Estimates;
using Nop.Core.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Estimates
{
    public partial interface IEstimateService
    {
        IList<EstimateItem> ProductExistOrNot(int estimateId, int productid = 0);
        Task<Estimate> GetEstimateById(int estimateID);

        Task UpdateEstimateItemUsingFor(int itemId = 0, int Quntity = 0);
        IList<Estimate> GetEstimateByCustomerNumber(int CustId = 0);

        ICollection<EstimateItem> ForUpdateEstimatesItems(int EstimateId);

        Task DeleteEstimate(Estimate estimate);

        Task InsertEstimate(Estimate estimate);
        Task InsertEstimateItem(EstimateItem estimateItem);
        Task UpdateEstimate(Estimate estimate);
        Task UpdateEstimateItem(EstimateItem estimateItem);
        Task<EstimateItem> GetEstimateItemById(int estimateItemId);
        EstimateItem GetEstimateItemByEstimateId(int estimateId);

        Task DeleteEstimateItem(EstimateItem orderItem);

        // Added by Yogesh Kumbhar on Dt: 12/10/2024
        Task<IList<EstimateItem>> GetEstimateItemsAsync(Customer customer, int estimateId,
            int storeId = 0, int? productId = null);
        Task<IList<EstimateItem>> GetEstimateItemsByEstimateIdAsync(int estimateId);
    }
}
