using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Models;
using System;
using System.Collections.Generic;
using static Nop.Web.Areas.Admin.Models.Orders.OrderItemModel;

namespace Nop.Web.Areas.Admin.Models.Orders
{
    public partial record BackOrderSearchModel : BaseSearchModel
    {
        public BackOrderSearchModel()
        {
            PurchasedGiftCardIds = new List<int>();
            ReturnRequests = new List<ReturnRequestBriefModel>();
        }
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public int? OrderId { get; set; }
        public string ProductName { get; set; }
        public string VendorName { get; set; }
        public string Sku { get; set; }

        public string PictureThumbnailUrl { get; set; }

        public string UnitPriceInclTax { get; set; }
        public string UnitPriceExclTax { get; set; }
        public decimal UnitPriceInclTaxValue { get; set; }
        public decimal UnitPriceExclTaxValue { get; set; }


        public bool BackOrderStatus { get; set; }
        public int Quantity { get; set; }

        public int? BackOrderSoldQty { get; set; }

        public int BackOrderQty { get; set; }
        public string DiscountInclTax { get; set; }
        public string DiscountExclTax { get; set; }
        public decimal DiscountInclTaxValue { get; set; }
        public decimal DiscountExclTaxValue { get; set; }

        public string SubTotalInclTax { get; set; }
        public string SubTotalExclTax { get; set; }
        public decimal SubTotalInclTaxValue { get; set; }
        public decimal SubTotalExclTaxValue { get; set; }

        public string AttributeInfo { get; set; }
        public string RecurringInfo { get; set; }
        public string RentalInfo { get; set; }
        public IList<ReturnRequestBriefModel> ReturnRequests { get; set; }
        public IList<int> PurchasedGiftCardIds { get; set; }

        public bool IsDownload { get; set; }
        public int DownloadCount { get; set; }
        public DownloadActivationType DownloadActivationType { get; set; }
        public bool IsDownloadActivated { get; set; }
        //public Guid LicenseDownloadGuid { get; set; }

        #region Nested Classes

        //public partial class ReturnRequestBriefModel : BaseNopEntityModel
        //{
        //    public string CustomNumber { get; set; }
        //}


        public string OrderTotal { get; set; }

        public bool IsLoggedInAsVendor { get; set; }

        public string ProdBinLocation { get; set; }

        public DateTime OrderDate { get; set; }

        public string ProdSku { get; set; }

        #endregion
    }
}
