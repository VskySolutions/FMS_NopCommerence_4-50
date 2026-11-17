using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Common
{
    public partial class Marquee : BaseEntity, ILocalizedEntity, ISlugSupported
    {
        public string Text { get; set; }
    }
}
