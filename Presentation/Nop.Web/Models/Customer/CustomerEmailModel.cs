using Nop.Core.Domain.Customers;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static Nop.Web.Areas.Admin.Models.Customers.CustomerModel;

namespace Nop.Web.Models.Customer
{
    public partial record CustomerEmailModel : BaseNopEntityModel
    {
        #region Ctor
        public CustomerEmailModel()
        {
            CustomerEmailList = new List<CustomerEmailModel>(); 
        }
        #endregion

        #region
        public int CustomerId { get; set; }

        [DataType(DataType.EmailAddress)]
        [NopResourceDisplayName("Account.Fields.Email")]
        public string Email { get; set; }

        public bool IsOrder { get; set; }

        public bool IsInvoice { get; set; }

        public bool IsEstimate { get; set; }

        public bool IsRefund { get; set; }

        public bool IsOrderCancel { get; set; }

        public bool IsOrderShipped { get; set; }

        public bool Deleted { get; set; }

        public DateTime? CreatedOnUtc { get; set; }

        public DateTime? UpdatedOnUtc { get; set; }

        public List<CustomerEmailModel> CustomerEmailList { get; set; }
        #endregion
    }
}
