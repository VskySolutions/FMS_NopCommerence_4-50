namespace Nop.Core.Domain.Orders
{
    /// <summary>
    /// Represents a return status
    /// </summary>
    public enum ReturnRequestStatus
    {
        /// <summary>
        /// Pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Received
        /// </summary>
        Received = 10,

        /// <summary>
        /// Return authorized
        /// </summary>
        ReturnAuthorized = 20,

        /// <summary>
        /// Item(s) repaired
        /// </summary>
        ItemsRepaired = 30,

        /// <summary>
        /// Item(s) refunded
        /// </summary>
        ItemsRefunded = 40,

        /// <summary>
        /// Request rejected
        /// </summary>
        RequestRejected = 50,

        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled = 60,

        /// <summary>
        /// Item(s) replaced
        /// </summary>
        ItemsReplaced = 70,

        /// <summary>
        /// Repaired, Awaiting Item Return
        /// </summary>
        Repaired = 80,

        /// <summary>
        /// Refunded, Awaiting Item Return
        /// </summary>
        Refunded = 90,

        /// <summary>
        /// Exchanged, Awaiting Item Return
        /// </summary>
        Exchanged = 100,

        /// <summary>
        /// Store Credit Issued, Awaiting Item Return
        /// </summary>
        StoreCreditIssuedAwaiting = 110,

        /// <summary>
        /// Store Credit Issued
        /// </summary>
        StoreCreditIssued = 120,
    }
}
