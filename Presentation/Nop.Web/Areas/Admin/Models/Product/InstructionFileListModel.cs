using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
namespace Nop.Web.Areas.Admin.Models.Product
{
    public partial record InstructionFileListModel : BaseNopEntityModel
    {
        #region Properties

        [NopResourceDisplayName("Admin.Product.Piture.Path")]
        public string ImageName { get; set; }
        
        [NopResourceDisplayName("Admin.Product.Name")]
        public string ProductName { get; set; }
        
        [NopResourceDisplayName("Admin.Product.FileName")]
        public string FileName { get; set; }

       [NopResourceDisplayName("Admin.Product.Category")]
        public string Category { get; set; }
        
        [NopResourceDisplayName("Admin.Product.GuidFile")]
        public string GuidFile { get; set; }


        #endregion
    }
}
