using Nop.Core.Domain.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Estimates
{
    public partial class Estimate : BaseEntity
    {
        private ICollection<EstimateItem> _EstimateItems;

        /// <summary>
        /// Gets or sets the StoreId
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Discription
        /// </summary>
        public string Discription { get; set; }

        /// <summary>
        /// Gets or sets the Note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the CustomerId
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the EstimateStatusId
        /// </summary>
        public int EstimateStatusId { get; set; }

        /// <summary>
        /// Gets or sets the EstimateTotal
        /// </summary>
        public decimal EstimateTotal { get; set; }

        /// <summary>
        /// Gets or sets the CreatedOnUtc
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the Delete
        /// </summary>
        public bool Delete { get; set; }

        /// <summary>
        /// Gets or sets the CopyEstimateId
        /// </summary>
        public int? CopyEstimateId { get; set; }

        /// <summary>
        /// Gets or sets the IsAddToCart
        /// </summary>
        public bool IsAddToCart { get; set; }

        /// <summary>
        /// Gets or sets the CartAddedDate
        /// </summary>
        public DateTime? CartAddedDate { get; set; }

        /// <summary>
        /// Gets or sets the EstimateItems
        /// </summary>
        public virtual ICollection<EstimateItem> EstimateItems
        {
            get { return _EstimateItems ?? (_EstimateItems = new List<EstimateItem>()); }
            protected set { _EstimateItems = value; }
        }

        /// <summary>
        /// Gets or sets the Customer
        /// </summary>
        public virtual Customer Customer { get; set; }

        public int? CreatedBy { get; set; }
    }
}
