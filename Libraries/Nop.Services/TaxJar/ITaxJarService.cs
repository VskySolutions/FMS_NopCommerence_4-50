using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domian.TaxJar;

namespace Nop.Services.Tax
{
    public partial interface ITaxJarService
    {
        #region Taxation & Pre-Order Functions
        Task<(bool, string, string)> GetNexusExceededStates();
        Task<(bool, string, decimal, string)> GetTaxByZipCode(TaxJarAddress model);
        Task<(bool, string, decimal)> GetTaxRateByNexus(TaxJarAddress model);
        Task<(bool, string, decimal, decimal)> GetTaxOnOrder(TaxJarAddress model, decimal OrderTotal);
        #endregion

        #region Sales tax for an order
        Task<(bool, string, ResponseForSalesTaxForOrderModel)> GetSalesTaxForOrder(SalesTaxForOrderModel model);
        #endregion

        #region Transactions List & CRUD  Functions
        Task<(bool, string, TransactionDetialListModel)> GetAllTransactionsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "");
        Task<(bool, string, List<string>)> GetAllTransactionIdsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "");
        Task<(bool, string, TransactionModel)> GetTransactionById(string TransactionId);
        Task<(bool, string)> CreateTransaction(TransactionModel model);
        Task<(bool, string)> UpdateTransaction(TransactionModel model);
        Task<(bool, string)> DeleteTransactionById(string Id);
        #endregion

        #region Refund List & CRUD  Functions
        Task<(bool, string, RefundDetialListModel)> GetAllRefundTransactionsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "");
        Task<(bool, string, List<string>)> GetAllRefundTransactionIdsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "");
        Task<(bool, string, RefundModel)> GetRefundTransactionById(string TransactionId);
        Task<(bool, string, string)> GenerateRefundTransactionById(string TransactionId, decimal RefundPercentage, int RefundCount);
        Task<(bool, string)> CreateRefundTransaction(RefundModel model);
        Task<(bool, string)> UpdateRefundTransaction(RefundModel model);
        Task<(bool, string)> DeleteRefundTransactionById(string Id);
        #endregion

        #region Customer List & CRUD  Functions
        Task<(bool, string, CustomerDetailListModel)> GetAllCustomerList();
        Task<(bool, string, List<string>)> GetAllCustomerIdsList();
        Task<(bool, string, CustomerModel)> GetCustomerById(string Id);
        Task<(bool, string)> CreateCustomer(CustomerModel model);
        Task<(bool, string)> UpdateCustomer(CustomerModel model);
        Task<(bool, string)> DeleteCustomerById(string Id);
        #endregion
    }
}
