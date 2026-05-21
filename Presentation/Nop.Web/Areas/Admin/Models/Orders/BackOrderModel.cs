using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders
{
    public partial record BackOrderModel : BaseNopEntityModel
    {
        public BackOrderModel()
        {
            if (PageSize < 1)
                PageSize = 5;
        }

        public int PageSize { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string VendorName { get; set; }
        public string Sku { get; set; }
        public int? BackOrderSoldQty { get; set; }
        public string PictureThumbnailUrl { get; set; }
        public int? BackOrderQty { get; set; }
        public int Quantity { get; set; }
        public string UnitPriceInclTax { get; set; }
        public string UnitPriceExclTax { get; set; }
        public decimal UnitPriceInclTaxValue { get; set; }
        public decimal UnitPriceExclTaxValue { get; set; }
        public bool BackOrderStatus { get; set; }
    }
}
