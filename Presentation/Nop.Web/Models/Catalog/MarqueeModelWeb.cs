using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog
{
    public partial record MarqueeModelWeb : BaseNopEntityModel
    {
        public string Text { get; set; }
    }
}
