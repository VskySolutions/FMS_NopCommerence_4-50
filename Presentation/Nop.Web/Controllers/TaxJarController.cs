using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nop.Core.Domian.TaxJar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nop.Services.Tax;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Core.Domain.Common;

namespace Nop.Web.Controllers
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public partial class TaxJarController : BasePublicController
    {
        #region Common Models, Services & Initialization
        public bool IsError = false;
        public string ErrorMessage = string.Empty;
        private readonly ITaxJarService _TaxJarService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ILogger _logger;
        public TaxJarController(
            ITaxJarService taxJarService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            ILogger logger)
        {
            _TaxJarService = taxJarService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _logger = logger;
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        #region Tax Calulations
        public async Task<IActionResult> GetTaxRateByZipCode(string Country,string State,string City,string Street, string ZipCode)
        {
            decimal TaxPercentage = decimal.Zero;
            string StateName = string.Empty;

            if (!string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(State) && !string.IsNullOrEmpty(ZipCode))
            {
                var Address = new TaxJarAddress();
                Address.Country = Country;
                Address.State = State;
                Address.City = City;
                Address.Street = Street;
                Address.ZipCode = ZipCode;

                (IsError, ErrorMessage, TaxPercentage, StateName) = await _TaxJarService.GetTaxByZipCode(Address);
            }
            else
            {
                IsError = true;
                ErrorMessage = "Error:- ZipCode is missing.";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Tax Percentage:- {StateName} {TaxPercentage}%";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetNexusRegions()
        {
            string NexusRegions = string.Empty;

            (IsError, ErrorMessage, NexusRegions) = await _TaxJarService.GetNexusExceededStates();

            //Return
            string ReturnMessage = IsError ? ErrorMessage : "Nexus States:- " + NexusRegions;
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetOrderTax(string Country, string State, string City, string Street, string ZipCode, decimal OrderTotal)
        {
            decimal TaxAmount = decimal.Zero;
            decimal TaxPercentage = decimal.Zero;

            if (string.IsNullOrEmpty(Country) || string.IsNullOrEmpty(State) || string.IsNullOrEmpty(ZipCode))
            {
                IsError = true;
                ErrorMessage = "Error:- Missing Zipcode";
            }
            else if (OrderTotal <= 0)
            {
                IsError = true;
                ErrorMessage = "Error:- Order Total cannot be negative or zero";
            }
            else
            {
                var address = new TaxJarAddress();
                address.Country = Country;
                address.State = State;
                address.ZipCode = ZipCode;
                address.City = City;
                address.Street = Street;

                (IsError, ErrorMessage, TaxAmount, TaxPercentage) = await _TaxJarService.GetTaxOnOrder(address, OrderTotal);
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Tax amount on ${OrderTotal} for Zip Code {ZipCode}: ${TaxAmount} at a rate of {TaxPercentage}%.";
            return Content(ReturnMessage);
        }
        #endregion

        #region Transactions
        public async Task<IActionResult> GetAllTransactions(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            var model = new TransactionDetialListModel();

            (IsError, ErrorMessage, model) = await _TaxJarService.GetAllTransactionsList(transaction_date, from_transaction_date, to_transaction_date, provider);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Result:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetAllTransactionIds(string transaction_date = "", string from_transaction_date="", string to_transaction_date = "", string provider = "")
        {
            var TransactionIdsList = new List<string>();

            (IsError, ErrorMessage, TransactionIdsList) = await _TaxJarService.GetAllTransactionIdsList(transaction_date, from_transaction_date, to_transaction_date, provider);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Id's:- {string.Join(',', TransactionIdsList)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetTransaction(string TransactionId)
        {
            var model = new TransactionModel();

            if (!string.IsNullOrEmpty(TransactionId))
                (IsError, ErrorMessage, model) = await _TaxJarService.GetTransactionById(TransactionId);
            else
            {
                IsError = true;
                ErrorMessage = "Missing Transaction Id";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Details:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GenerateTransaction()
        {
            decimal Tax = 0;
            decimal TaxPercentage = 0;
            var CDateTime = DateTime.Now;

            var model = new TransactionModel();
            model.TransactionId = new Random().Next(0, 100000).ToString();
            model.TransactionDate = CDateTime.ToString("MM/dd/yyyy hh:mm");
            model.Provider = "api";

            model.ToCountry = "US";
            model.ToZip = "32256";
            model.ToState = "FL";
            model.ToCity = "Jacksonville";
            model.ToStreet = "9140 Baymeadows Park Drive";

            model.Amount = 100;
            model.Shipping = 0;

            //Calulations of sales tax. 
            var Address = new TaxJarAddress();
            Address.Country = model.ToCountry;
            Address.State = model.ToState;
            Address.ZipCode = model.ToZip;

            (IsError, ErrorMessage, Tax, TaxPercentage) = await _TaxJarService.GetTaxOnOrder(Address, model.Amount);

            if (!IsError)
            {
                model.SalesTax = Tax > 0 ? Math.Round((Tax / model.Amount) * 100, 2) : 0;
                model.CustomerId = null;
                model.ExemptionType = model.SalesTax > 0 ? "non_exempt" : "wholesale";

                var LineItem = new LineItemModel();
                LineItem.Id = 308; //ProductId :- New FMS BT Audio Amplifier
                LineItem.Quantity = 1;
                LineItem.ProductIdentifier = null; //SKU No.
                LineItem.Description = "The New FMS Bluetooth amplifier provides handlebar switch control for changing volume, mute, and advancing playlist songs ... all from a single momentary button!  This new amplifier, Made in the USA, mounts to the inside radio box lid with Velcro, connecting to the BMW fairing speaker plug.  See below for momentary button availability and power source selection."; //Product Description
                LineItem.UnitPrice = 100;
                LineItem.Discount = 0;
                LineItem.SalesTax = Tax > 0 ? Math.Round((Tax / model.Amount) * 100, 2) : 0;
                model.LineItems.Add(LineItem);

                (IsError, ErrorMessage) = await _TaxJarService.CreateTransaction(model);
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Created:- Successful ({model.TransactionId})";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GenerateTestTransaction()
        {
            var model = new SalesTaxForOrderModel();
            var result = new ResponseForSalesTaxForOrderModel();

            var NexusAddress = new SalesTaxNexusAddresses();
            NexusAddress.id = "Main Location";
            model.nexus_addresses.Add(NexusAddress);

            model.to_country = "US";
            model.to_zip = "32256"; // 92563
            model.to_state = "FL"; // CA
            model.to_city = "Merrieta";
            model.to_street = "38302 Encanto Rd.";

            model.amount = 127.95F;
            model.shipping = 0F;

            var item1 = new SalesTaxLineItems();
            item1.id = "1";
            item1.quantity = "1";
            item1.unit_price = "9";
            item1.discount = "0";
            model.line_items.Add(item1);
            
            var item2 = new SalesTaxLineItems();
            item2.id = "2";
            item2.quantity = "1";
            item2.unit_price = "79.95";
            item2.discount = "0";
            model.line_items.Add(item2); 
            
            var item3 = new SalesTaxLineItems();
            item3.id = "3";
            item3.quantity = "1";
            item3.unit_price = "39.00";
            item3.discount = "0";
            model.line_items.Add(item3);

            (IsError, ErrorMessage, result) = await _TaxJarService.GetSalesTaxForOrder(model);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : JsonSerializer.Serialize(result);
            return Content(ReturnMessage);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction(TransactionModel model)
        {
            if (!string.IsNullOrEmpty(model.TransactionId))
                (IsError, ErrorMessage) = await _TaxJarService.CreateTransaction(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Created:- Successfully {model.TransactionId}";
            return Content(ReturnMessage);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTransaction(TransactionModel model)
        {
            if (!string.IsNullOrEmpty(model.TransactionId))
                (IsError, ErrorMessage) = await _TaxJarService.UpdateTransaction(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Updated:- Successfully {model.TransactionId}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> DeleteTransactionById(string Id)
        {
            if (string.IsNullOrEmpty(Id))
                return Content("Error: Transaction Id is missing.");

            (IsError, ErrorMessage) = await _TaxJarService.DeleteTransactionById(Id);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Transaction Deleted:- Successful ({Id})";
            return Content(ReturnMessage);
        }
        #endregion

        #region Refund Transactions
        public async Task<IActionResult> GetAllRefundTransactions(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            var model = new RefundDetialListModel();

            (IsError, ErrorMessage, model) = await _TaxJarService.GetAllRefundTransactionsList(transaction_date, from_transaction_date, to_transaction_date, provider);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Result:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetAllRefundTransactionIds(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            var RefundIdsList = new List<string>();

            (IsError, ErrorMessage, RefundIdsList) = await _TaxJarService.GetAllRefundTransactionIdsList(transaction_date, from_transaction_date, to_transaction_date, provider);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Id's:- {string.Join(',', RefundIdsList)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GetRefundTransaction(string TransactionId)
        {
            var model = new RefundModel();

            if (!string.IsNullOrEmpty(TransactionId))
                (IsError, ErrorMessage, model) = await _TaxJarService.GetRefundTransactionById(TransactionId);
            else
            {
                IsError = true;
                ErrorMessage = "Missing Transaction Id";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Transaction Details:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> GenerateRefundTransactionById(string TransactionId, decimal RefundPercentage, int RefundCount)
        {
            string RefundId = string.Empty;
            (IsError, ErrorMessage, RefundId) = await _TaxJarService.GenerateRefundTransactionById(TransactionId, RefundPercentage, RefundCount);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Transaction:- Successful ({RefundId})";
            return Content(ReturnMessage);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRefundTransaction(RefundModel model)
        {
            if (!string.IsNullOrEmpty(model.TransactionId))
                (IsError, ErrorMessage) = await _TaxJarService.CreateRefundTransaction(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Transaction Created:- Successfully {model.TransactionId}";
            return Content(ReturnMessage);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRefundTransaction(RefundModel model)
        {
            if (!string.IsNullOrEmpty(model.TransactionId))
                (IsError, ErrorMessage) = await _TaxJarService.UpdateRefundTransaction(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Transaction Updated:- Successfully {model.TransactionId}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> DeleteRefundTransactionById(string Id)
        {
            if (string.IsNullOrEmpty(Id))
                return Content("Error: Refund transaction id is missing.");

            (IsError, ErrorMessage) = await _TaxJarService.DeleteRefundTransactionById(Id);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Refund Transaction Deleted:- Successful ({Id})";
            return Content(ReturnMessage);
        }
        #endregion

        #region Customers
        public async Task<IActionResult> GetAllCustomers()
        {
            var model = new CustomerDetailListModel();

            (IsError, ErrorMessage, model) = await _TaxJarService.GetAllCustomerList();

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Result:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }
        
        public async Task<IActionResult> GetAllCustomerIds()
        {
            var CustomerIdList = new List<string>();

            (IsError, ErrorMessage, CustomerIdList) = await _TaxJarService.GetAllCustomerIdsList();

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Customer Id's:- {string.Join(',', CustomerIdList)}";
            return Content(ReturnMessage);
        }
        
        public async Task<IActionResult> GetCustomerById(string Id)
        {
            var model = new CustomerModel();

            (IsError, ErrorMessage, model) = await _TaxJarService.GetCustomerById(Id);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Result:- {JsonSerializer.Serialize(model)}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> CreateTestCustomer() 
        {
            var model = new CustomerModel();

            model.CustomerId = new Random().Next(0, 100000).ToString();
            model.ExemptionType = "non_exempt";
            model.FullName = "Vsky Testing";
            model.Country = "US";
            model.State = "FL";
            model.ZipCode = "32256";
            model.City = "Jacksonville";
            model.Street = "9140 Baymeadows Park Dr Suite 10S";

            (IsError, ErrorMessage) = await _TaxJarService.CreateCustomer(model);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Customer Created:- Successfully {model.CustomerId}";
            return Content(ReturnMessage);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CustomerModel model)
        {
            if(!string.IsNullOrEmpty(model.CustomerId))
                (IsError, ErrorMessage) = await _TaxJarService.CreateCustomer(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Customer Created:- Successfully {model.CustomerId}";
            return Content(ReturnMessage);
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(CustomerModel model)
        {
            if(!string.IsNullOrEmpty(model.CustomerId))
                (IsError, ErrorMessage) = await _TaxJarService.UpdateCustomer(model);
            else
            {
                IsError = true;
                ErrorMessage = "Required Fields Mandatory";
            }

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Customer Updated:- Successfully {model.CustomerId}";
            return Content(ReturnMessage);
        }

        public async Task<IActionResult> DeleteCustomerById(string Id)
        {
            if (string.IsNullOrEmpty(Id))
                return Content("Error: Customer Id is missing.");

            (IsError, ErrorMessage) = await _TaxJarService.DeleteCustomerById(Id);

            //Return
            string ReturnMessage = IsError ? ErrorMessage : $"Customer Deleted:- Successful ({Id})";
            return Content(ReturnMessage);
        }
        #endregion

        #region Sync Customer
        // Mohit - 11/06/2024
        // Auto
        // Customer
        [AllowAnonymous]
        [Route("AutoSyncToTaxJar")]
        public async Task<IActionResult> AutoSyncToTaxJar()
        {
            try
            {
                var errorMessageList = new List<string>();

                // Create
                int[] CreateIds = _customerService.GetCustomerIdsForTaxJar(false, null, null, false);
                if (CreateIds.Any())
                    errorMessageList = await StartSync(string.Join(',', CreateIds), errorMessageList, 1);

                // Update
                int[] UpdateIds = _customerService.GetCustomerIdsForTaxJar(true, true, false, false);
                if (UpdateIds.Any())
                    errorMessageList = await StartSync(string.Join(',', UpdateIds), errorMessageList, 2);

                // Delete
                int[] DeleteIds = _customerService.GetCustomerIdsForTaxJar(true, null, true, true);
                if (DeleteIds.Any())
                    errorMessageList = await StartSync(string.Join(',', DeleteIds), errorMessageList, 3);

                if (errorMessageList.Any())
                {
                    string Errors = string.Join(',', errorMessageList);
                    await _logger.ErrorAsync(Errors, null, null);
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Automatic TaxJar Customer Sync Error -> {" + ex.Message + ":- " + ex.InnerException + "}", null, null);
            }
            return Ok();
        }

        private async Task<List<string>> StartSync(string selectedIds, List<string> errorMessageList, int IsCreateUpdateDelete)
        {
            const int batchSize = 25;
            var utcNow = DateTime.UtcNow;
            var customerIds = selectedIds.Split(',').Select(int.Parse).ToList();
            var allCustomers = _customerService.GetCustomersForSyncingToTaxJarList(customerIds); // .Take(80).ToList();
            var syncCustomerList = await PrepareSyncCustomerList(allCustomers);
            var customerBatches = SplitIntoBatches(syncCustomerList, batchSize);

            foreach (var batch in customerBatches)
            {
                var tasks = batch.Select(async item =>
                {
                    var (isError, errorMessage) =
                        IsCreateUpdateDelete == 1 ? await _TaxJarService.CreateCustomer(item) :
                        IsCreateUpdateDelete == 2 ? await _TaxJarService.UpdateCustomer(item) :
                        IsCreateUpdateDelete == 3 ? await _TaxJarService.DeleteCustomerById(item.CustomerId) :
                        (false, "");

                    if (isError)
                        errorMessageList.Add($"{item.CustomerId}:- {errorMessage}");
                    else
                    {
                        var customer = allCustomers.First(m => m.Id == Convert.ToInt64(item.CustomerId));
                        customer.IsSyncedToTaxJar = IsCreateUpdateDelete == 3 ? false : true;
                        customer.SyncedToTaxJarDateTime = IsCreateUpdateDelete == 3 ? null : utcNow;
                        customer.IsRecentlyDeleted = IsCreateUpdateDelete == 3 ? false : customer.IsRecentlyDeleted;
                        await _customerService.UpdateCustomerAsync(customer);
                    }
                });

                await Task.WhenAll(tasks); // Process all items in the batch concurrently
            }

            return errorMessageList;
        }

        private async Task<List<CustomerModel>> PrepareSyncCustomerList(List<Customer> allCustomers)
        {
            var syncCustomerList = new List<CustomerModel>();

            var tasks = allCustomers.Select(async customer =>
            {
                var model = new CustomerModel
                {
                    CustomerId = customer.Id.ToString(),
                    ExemptionType = customer.TaxExemptType,
                    FullName = await GetFullNameWithEmail(customer)
                };

                (model.Street, model.City, model.ZipCode, model.State, model.Country) = await GetAddressOfCustomer(customer);
                return model;
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task<string> GetFullNameWithEmail(Customer customer)
        {
            var firstNameTask = _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.FirstNameAttribute);
            var lastNameTask = _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.LastNameAttribute);

            await Task.WhenAll(firstNameTask, lastNameTask);

            string email = !string.IsNullOrEmpty(customer.Email) ? $"({customer.Email})" : "";
            return $"{await firstNameTask} {await lastNameTask} {email}";
        }

        private async Task<(string Street, string City, string ZipCode, string TwoLetterStateName, string TwoLetterCountryName)> GetAddressOfCustomer(Customer customer)
        {
            var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
            var shippingAddress = await _customerService.GetCustomerShippingAddressAsync(customer);

            if (billingAddress != null)
                return await GetAddressDetails(billingAddress);

            if (shippingAddress != null)
                return await GetAddressDetails(shippingAddress);

            return (null, null, null, null, null);
        }

        private async Task<(string Street, string City, string ZipCode, string TwoLetterStateName, string TwoLetterCountryName)> GetAddressDetails(Address address)
        {
            string street = !string.IsNullOrEmpty(address.Address1) ? address.Address1 : null;
            string city = !string.IsNullOrEmpty(address.City) ? address.City : null;
            string zipCode = !string.IsNullOrEmpty(address.ZipPostalCode) ? address.ZipPostalCode : null;
            string stateAbbr = null;
            string countryAbbr = null;

            if (address.StateProvinceId != null)
            {
                var stateData = await _stateProvinceService.GetStateProvinceByIdAsync((int)address.StateProvinceId);
                stateAbbr = stateData.Abbreviation;
            }

            if (address.CountryId != null)
            {
                var countryData = await _countryService.GetCountryByIdAsync((int)address.CountryId);
                countryAbbr = countryData.TwoLetterIsoCode;
            }

            return (street, city, zipCode, stateAbbr, countryAbbr);
        }

        private List<List<CustomerModel>> SplitIntoBatches(List<CustomerModel> syncCustomerList, int batchSize)
        {
            return syncCustomerList
                .Select((customer, index) => new { customer, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.customer).ToList())
                .ToList();
        }
        #endregion
    }
}
