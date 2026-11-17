using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Common
{
    public partial record HomePageArrivalModel : BaseNopEntityModel
    {
        public string ProductName { get; set; }
        public string ImagePath { get; set; }
    }
}
