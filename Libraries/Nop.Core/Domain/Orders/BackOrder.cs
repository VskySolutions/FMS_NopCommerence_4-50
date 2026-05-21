using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Orders
{
    public partial class BackOrder : BaseEntity, ILocalizedEntity, ISlugSupported
    {
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public string ProductName { get; set; }
        public string VendorName { get; set; }
        public string Sku { get; set; }

        public string PictureThumbnailUrl { get; set; }

        public string UnitPriceInclTax { get; set; }
        public string UnitPriceExclTax { get; set; }
    }
}
