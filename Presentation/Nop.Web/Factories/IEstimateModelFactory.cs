using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Estimates;
using Nop.Core.Domain.Orders;
using Nop.Web.Models.Estimate;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Factories
{
    public partial interface IEstimateModelFactory
    {
        EstimateDetailsModel PrepareAddNewList(EstimateDetailsModel model);
        Task<EstimateDetailsModel> PrepareGetEstimateDetailsById(int estimateid = 0, int CustID = 0);
        EstimateDetailsModel PrepareEstimateNavigationModel(EstimateDetailsModel model, int CustId = 0);

        /// <summary>
        /// Export Excel File
        /// </summary>
        /// <param name="EstimateCode"></param>
        /// <returns></returns>
        Task<byte[]> ExportExcelFile(int EstimateCode = 0);
    }
}
