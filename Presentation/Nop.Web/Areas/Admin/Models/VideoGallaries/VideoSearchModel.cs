using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;

namespace Nop.Web.Areas.Admin.Models.VideoGallaries
{
    public partial record VideoSearchModel : BaseSearchModel
    {

        /// <summary>
        /// Video Title
        /// </summary>
        [NopResourceDisplayName("Admin.Customers.Customers.Fields.VideoTitle")]
        public string VideoTitle { get; set; }

        /// <summary>
        /// Video Url
        /// </summary>
        [NopResourceDisplayName("Admin.Customers.Customers.Fields.VideoUrl")]
        public string VideoUrl { get; set; }
        public int Id { get; set; }

        /// <summary>
        /// Video Tumbnail Image
        /// </summary>
        public string VideoTumbnailImage { get; set; }

        /// <summary>
        /// Created Date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Publish
        /// </summary>
        public bool Publish { get; set; }

        /// <summary>
        /// Video Delete
        /// </summary>
        public bool VideoDelete { get; set; }
    }
}
