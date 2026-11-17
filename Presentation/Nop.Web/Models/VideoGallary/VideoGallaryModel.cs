using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Nop.Web.Models.VideoGallary
{
    public partial record VideoGallaryModel : BaseNopEntityModel
    {
        public VideoGallaryModel()
        {
            //VideoGallaryList = IList<ListVideoGallary>();
            VideoGallaryList = new List<ListVideoGallary>();
        }
        public IList<ListVideoGallary> VideoGallaryList { get; set; }
        public int OrderByDate { get; set; }
    }
    public partial class ListVideoGallary
    {
        [NopResourceDisplayName("Admin.Customers.Customers.Fields.VideoTitle")]
        public string VideoTitle { get; set; }

        [NopResourceDisplayName("Admin.Customers.Customers.Fields.VideoUrl")]
        public string VideoUrl { get; set; }
        public string VideoTumbnailImage { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool Publish { get; set; }

        public bool VideoDelete { get; set; }
    }
}
