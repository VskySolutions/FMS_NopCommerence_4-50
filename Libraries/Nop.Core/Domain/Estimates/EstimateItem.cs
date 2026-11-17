using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Estimates
{
    public partial class EstimateItem : BaseEntity
    {
        /// <summary>
        /// Gets or sets the EstimateId
        /// </summary>
        public int EstimateId { get; set; }

        /// <summary>
        /// Gets or sets the ProductId
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets the CustomerId
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the Quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the CustomerEnteredPrice
        /// </summary>
        public decimal CustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the AttributesXml
        /// </summary>
        public string AttributesXml { get; set; }

        /// <summary>
        /// Gets or sets the StoreId
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the RentalStartDateUtc
        /// </summary>
        public DateTime? RentalStartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the RentalEndDateUtc
        /// </summary>
        public DateTime? RentalEndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the CreatedOnUtc
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the Customer
        /// </summary>
        public virtual Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets the Estimate
        /// </summary>
        public virtual Estimate Estimate { get; set; }

        /// <summary>
        /// Gets the product
        /// </summary>
        public virtual Product Product { get; set; }
    }
}
