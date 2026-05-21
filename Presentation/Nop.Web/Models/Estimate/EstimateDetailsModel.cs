using System;
using System.Collections.Generic;
using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Common;
using Nop.Web.Validators.Estimates;
using Nop.Web.Framework;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Media;
using Nop.Web.Framework.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Models.Estimate
{
    public partial record EstimateDetailsModel : BaseNopEntityModel
    {
        public EstimateDetailsModel()
        {
            Items = new List<EstimateNavigationModel>();
            NavBar = new List<NavigationClass>();
            CustomerList = new List<SelectListItem>();
        }

        public int EstimateId { get; set; }

        public int StoreId { get; set; }

        [NopResourceDisplayName("Estimate.Fields.Name")]
        public string Name { get; set; }

        [NopResourceDisplayName("Estimate.Fields.Discription")]
        public string Discription { get; set; }
        public int CustomerId { get; set; }

        public string[] EstimateStatusIdArray { get; set; }
        public string ProductQuantityArray { get; set; }
        public int EstimateStatusId { get; set; }

        public decimal EstimateTotal { get; set; }
        public int[] removefromestimate { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public bool IsAddToCart { get; set; }
        public DateTime CartAddedDate { get; set; }

        public IList<EstimateNavigationModel> Items { get; set; }

        [NopResourceDisplayName("Estimate.Fields.CopyName")]
        public string CopyName { get; set; }
        [NopResourceDisplayName("Estimate.Fields.Discription")]
        public string CopyDescriptions { get; set; }

        public string SeName { get; set; }

        #region Nested Classes

        #endregion

        public IList<NavigationClass> NavBar { get; set; }

        public decimal Subtot { get; set; }

        public bool IsView { get; set; }
        
        // Added by Yogesh Kumbhar on Dt: 12-20-2024
        public IList<SelectListItem> CustomerList { get; set; }
        public int? CreatedBy { get; set; }
        public string UserRole { get; set; }
        // Added by Yogesh Kumbhar on Dt: 01-14-2024
        public string Email { get; set; }
        public string Company { get; set; }
        // Added by Yogesh Kumbhar on Dt: 02-24-2024
        public bool IsCustomer { get; set; }
        // Added by Yogesh Kumbhar on Dt: 03-13-2025
        public string Note { get; set; }

    }

    public partial record NavigationClass : BaseNopEntityModel
    {
        public int ListId { get; set; }
        public string Name { get; set; }

        //Added by Yogesh Kumbhar on Dt: 12-02-2024
        public string CreatedOn { get; set; }
        public string CustomerEmail { get; set; }
        public string Description { get; set; }
        public bool IsEstimateItems { get; set; }
        //Added by Yogesh Kumbhar on Dt: 12-20-2024
        public string CratedbyStr { get; set; }
    }

    public partial record EstimateNavigationModel : BaseNopEntityModel
    {
        public EstimateNavigationModel()
        {
            Picture = new PictureModel();

        }
        public int ListId { get; set; }
        public string Name { get; set; }
        public string ProductSeName { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public string AttributeDescription { get; set; }

        public string AttributesXml { get; set; }

        public string Sku { get; set; }

        public PictureModel Picture { get; set; }

        public string UnitPrice { get; set; }

        public string SubTotal { get; set; }


        public string Discount { get; set; }

        public int? MaximumDiscountedQty { get; set; }
    }


    public partial record PictureModel : BaseNopEntityModel
    {
        public string ImageUrl { get; set; }

        public string ThumbImageUrl { get; set; }

        public string FullSizeImageUrl { get; set; }

        public string Title { get; set; }

        public string AlternateText { get; set; }
    }

    public partial record ExelKeys : BaseNopEntityModel
    {
        public int Quantity { get; set; }
        public string Name { get; set; }

        public string SKU { get; set; }

        public string UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        //Added by Yogesh Kumbhar on Dt: 02-11-2025
        public string CreatedBy { get; set; }
        public string CreatedFor { get; set; }
    }
}