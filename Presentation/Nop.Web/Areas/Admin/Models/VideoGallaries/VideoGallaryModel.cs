using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.Collections.Generic;

namespace Nop.Web.Areas.Admin.Models.VideoGallaries
{
    public partial record VideoGallaryModel : BaseNopEntityModel, ILocalizedModel<VideoLocalizedModel>
    {
        public VideoGallaryModel()
        {
            if (PageSize < 1)
                PageSize = 5;

            Locales = new List<VideoLocalizedModel>();
        }

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

        [NopResourceDisplayName("Admin.Vendors.Fields.PageSize")]
        public int PageSize { get; set; }

        /// <summary>
        /// SeName
        /// </summary>
        [NopResourceDisplayName("Admin.Vendors.Fields.SeName")]
        public string SeName { get; set; }

        public IList<VideoLocalizedModel> Locales { get; set; }
        //public IList<int> SelectedStoreIds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public IList<SelectListItem> AvailableStores { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public IList<int> SelectedCustomerRoleIds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        //public IList<SelectListItem> AvailableCustomerRoles { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
    public partial record VideoLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.Description")]
        public string Description { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [NopResourceDisplayName("Admin.Vendors.Fields.SeName")]
        public string SeName { get; set; }
    }
}
