using Nop.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Domain.Customers
{
    public partial class CustomerEmail : BaseEntity
    {
        public int CustomerId { get; set; }

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
    }
}
