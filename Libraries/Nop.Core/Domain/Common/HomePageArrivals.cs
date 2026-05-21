using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Common
{
    public partial class HomePageArrivals: BaseEntity, ILocalizedEntity, ISlugSupported
    {
        public string ProductName { get; set; }

        public string ImagePath { get; set; }

        public int DisplayOrder { get; set; }

        public bool HomeDisplayFlag { get; set; }

        public bool Delete { get; set; }
    }
}
