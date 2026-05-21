using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nop.Core.Domian.TaxJar;
using Nop.Core.Caching;
using Nop.Data;

namespace Nop.Services.Tax
{
    public partial class TaxJarService : ITaxJarService
    {
        #region TaxJar Setup & Instructions
        //Cache 
        private readonly IStaticCacheManager _cacheManager;
        public TaxJarService(IStaticCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        //IsLiveEnvironment :- This used for setting the api environment to Production (true) / Sandbox (false).
        private static readonly bool IsLiveEnvironment = DataSettingsManager.LoadSettings().ConnectionString.Contains("db_a8c2bf_livefms");
        //Frank's Live Account
        //Email:- info@fmsaccessories.com.com
        //Production Keys:- 1498186acec0b43439d0a57be0a14747
        //Sandbox:- 0a83536ee0de30e3351e266c72bac61a

        //Vsky Test Account
        //Email:- fmsnopdev@yopmail.com
        //Password:- ig9*Q@KYLWpciw6
        //Production Keys:- 6cd0912193638a6903b88e44fc87cdea
        //Sandbox:- 

        // Base URL for TaxJar API
        private static readonly string TaxJarLiveApiUrl = "https://api.taxjar.com/v2/"; 
        
        //Live
        private static readonly string TaxJarLiveApiKey = "1498186acec0b43439d0a57be0a14747"; // Your API key here

        //Test
        private static readonly string TaxJarTestApiKey = "6cd0912193638a6903b88e44fc87cdea"; // Your API key here

        //Default Return Values
        //IsError :-  If this functions has error this flag will be set as true. 
        //ErrorMessage :- This will give the error type and description.
        public bool IsError = false;
        public string ErrorMessage = string.Empty;

        private static readonly CacheKey TaxRatesCacheKey = new CacheKey("Nop.cached_tax_rates");
        private static readonly CacheKey NexusStatesCacheKey = new CacheKey("Nop.cached_NexusStates");
        #endregion

        #region Taxation & Pre-Order Functions
        //Description :-  To find state where sales has exceeded tax limit.
        //Return Values:- 
        //NexusRegions :- Names of states where sales have exceeded tax nexus limits.
        public virtual async Task<(bool, string, string)> GetNexusExceededStates()
        {
            string NexusRegions = string.Empty;
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                // Retrieve the cached list of tax rates
                var cachedNexus = _cacheManager.Get(NexusStatesCacheKey, () => new List<string>());

                // Check if the tax rate for the provided zip code already exists in cache
                var cachedNexusRegions = cachedNexus.FirstOrDefault();

                if (cachedNexusRegions != null)
                    return ((IsError, ErrorMessage, cachedNexusRegions));

                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send a GET request to the "nexus/regions" endpoint
                    HttpResponseMessage response = await client.GetAsync("nexus/regions");

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        // Deserialize JSON response to NexusResponseModel
                        var taxRateResponse = JsonSerializer.Deserialize<NexusResponseModel>(responseBody);

                        if (taxRateResponse != null && taxRateResponse.regions != null)
                        {
                            foreach (var item in taxRateResponse.regions)
                                NexusRegions += $"{item.region}({item.region_code}), ";

                            NexusRegions = NexusRegions.Remove(NexusRegions.Length - 2);
                        }
                        else
                        {
                            IsError = true;
                            ErrorMessage = "No regions data found in the response.";
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                    }
                }

                // Add the new tax rate to the cached list
                cachedNexus.Add(NexusRegions);

                // Update the cache with the modified list of tax rates using CacheKey
                await _cacheManager.SetAsync(NexusStatesCacheKey, cachedNexus);
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle all other exceptions
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage, NexusRegions);
        }

        //Description :- This is used to find tax in percentage (%) by using Zipcode.
        //Parameter
        //ZipCode :- This used to find tax rate of that locations.
        //Return Values:- 
        //TaxPercentage :- This will provide the percentage of tax that needs to be applied for the specified Zip code.
        //StateName :- This will provide name of that particular zipcode.
        public virtual async Task<(bool, string, decimal, string)> GetTaxByZipCode(TaxJarAddress model)
        {
            decimal TaxPercentage = decimal.Zero;
            string StateName = string.Empty;
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                // Retrieve the cached list of tax rates
                var cachedTaxRates = _cacheManager.Get(TaxRatesCacheKey, () => new List<TaxRatesByZipCodeCache>());

                // Check if the tax rate for the provided zip code already exists in cache
                var cachedTaxRate = cachedTaxRates.FirstOrDefault(t => t.Zipcode == model.ZipCode);

                if (cachedTaxRate != null)
                {
                    // Return the cached tax rate if it exists
                    return (false,"", cachedTaxRate.TaxPercentage, cachedTaxRate.StateName);
                }

                var Tax = new SalesTaxForOrderModel();
                Tax.to_country = model.Country;
                Tax.to_state = model.State;
                Tax.to_zip = model.ZipCode;
                Tax.to_city = model.City;
                Tax.to_street = model.Street;
                Tax.amount = 9F;
                Tax.shipping = 0F;

                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Configure JsonSerializer to ignore null values
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    // Serialize the model to JSON
                    string jsonContent = JsonSerializer.Serialize(Tax, options);
                    using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Send the POST request asynchronously
                        HttpResponseMessage response = await client.PostAsync("taxes", httpContent);

                        // Ensure the request was successful, else handle the error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Capture detailed error message
                            string errorContent = await response.Content.ReadAsStringAsync();
                            IsError = true;
                            ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                            return (IsError, ErrorMessage, 0, "");  // Return early on error
                        }
                        else
                        {
                            // Read and deserialize the response
                            string responseBody = await response.Content.ReadAsStringAsync();
                            var SalesTax = JsonSerializer.Deserialize<ResponseForSalesTaxForOrderModel>(responseBody);

                            TaxPercentage = Math.Round((decimal)SalesTax.tax.rate * 100, 2);
                            StateName = SalesTax.tax.jurisdictions != null ? SalesTax.tax.jurisdictions.state : model.State;
                        }
                    }
                }

                // If not cached, create a new tax rate (this should come from your database or external logic)
                var newTaxRate = new TaxRatesByZipCodeCache
                {
                    Zipcode = model.ZipCode,
                    TaxPercentage = TaxPercentage,
                    StateName = StateName
                };

                // Add the new tax rate to the cached list
                cachedTaxRates.Add(newTaxRate);

                // Update the cache with the modified list of tax rates using CacheKey
                await _cacheManager.SetAsync(TaxRatesCacheKey, cachedTaxRates);
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle all other exceptions
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            return (IsError, ErrorMessage, TaxPercentage, StateName);
        }

        //Description :-  This is used to calculate the tax rate with nexus limit on the Zip code.
        //Return Values:- 
        //TaxAmount :- Retunrs Taxrate for that particular zipcode.
        //TaxPercentage :- This will provide the percentage of tax that needs to be applied for the specified Zip code.
        //StateName :- This will provide name of that particular zipcode.
        public virtual async Task<(bool, string, decimal)> GetTaxRateByNexus(TaxJarAddress model)
        {
            decimal TaxPercentage = decimal.Zero;
            string StateName = string.Empty;
            string NexusRegions = string.Empty;
            IsError = false;
            ErrorMessage = string.Empty;

            // Get tax by zip code if ZipCode is not empty
            if (model != null)
            {
                (IsError, ErrorMessage, TaxPercentage, StateName) = await GetTaxByZipCode(model);

                // If no error from GetTaxByZipCode, proceed to check NexusRegions
                if (!IsError)
                {
                    (IsError, ErrorMessage, NexusRegions) = await GetNexusExceededStates();

                    // If no error from GetNexusExceededStates and StateName is not in NexusRegions, reset TaxPercentage
                    if (!IsError && !NexusRegions.Contains(StateName))
                        TaxPercentage = decimal.Zero;
                }
            }

            return (IsError, ErrorMessage, TaxPercentage);
        }

        //Description :-  This is used to calculate the amount of tax on the order total based on the Zip code.
        //Return Values:- 
        //TaxAmount :- Taxable amount on order total for that particular zipcode.
        public virtual async Task<(bool, string, decimal, decimal)> GetTaxOnOrder(TaxJarAddress model, decimal OrderTotal)
        {
            decimal TaxAmount = decimal.Zero;
            decimal TaxPercentage = decimal.Zero;
            string StateName = string.Empty;
            string NexusRegions = string.Empty;
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                // Get tax by zip code if ZipCode is not empty
                if (model != null && !string.IsNullOrEmpty(model.ZipCode))
                {
                    (IsError, ErrorMessage, TaxPercentage, StateName) = await GetTaxByZipCode(model);

                    // If no error from GetTaxByZipCode, proceed to check NexusRegions
                    if (!IsError)
                    {
                        (IsError, ErrorMessage, NexusRegions) = await GetNexusExceededStates();

                        // If no error from GetNexusExceededStates and StateName is not in NexusRegions, reset TaxPercentage
                        if (!IsError && !NexusRegions.Contains(StateName))
                            TaxPercentage = decimal.Zero;
                    }
                }

                // Calculate TaxAmount if TaxPercentage is greater than 0
                if (TaxPercentage > 0)
                    TaxAmount = Math.Round((OrderTotal / 100) * TaxPercentage, 2);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage, TaxAmount, TaxPercentage);
        }
        #endregion

        #region Sales tax for an order
        public virtual async Task<(bool, string, ResponseForSalesTaxForOrderModel)> GetSalesTaxForOrder(SalesTaxForOrderModel model)
        {
            var SalesTax = new ResponseForSalesTaxForOrderModel();
            IsError = false;
            ErrorMessage = string.Empty;

            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Configure JsonSerializer to ignore null values
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    // Serialize the model to JSON
                    string jsonContent = JsonSerializer.Serialize(model, options);
                    using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Send the POST request asynchronously
                        HttpResponseMessage response = await client.PostAsync("taxes", httpContent);

                        // Ensure the request was successful, else handle the error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Capture detailed error message
                            string errorContent = await response.Content.ReadAsStringAsync();
                            IsError = true;
                            ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                            return (IsError, ErrorMessage, SalesTax);  // Return early on error
                        }
                        else
                        {
                            // Read and deserialize the response
                            string responseBody = await response.Content.ReadAsStringAsync();
                            SalesTax = JsonSerializer.Deserialize<ResponseForSalesTaxForOrderModel>(responseBody);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage, SalesTax);
        }
        #endregion

        #region Transactions List & CRUD  Functions
        //Description :- Get all Transaction with details from Taxjar at a time.
        public virtual async Task<(bool, string, TransactionDetialListModel)> GetAllTransactionsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var model = new TransactionDetialListModel();
            try
            {
                bool IsValidURL = false;

                //Validaitons
                if (!string.IsNullOrEmpty(transaction_date) && string.IsNullOrEmpty(from_transaction_date) && string.IsNullOrEmpty(to_transaction_date) && string.IsNullOrEmpty(provider))
                    IsValidURL = true;
                if (string.IsNullOrEmpty(transaction_date) && !string.IsNullOrEmpty(from_transaction_date) && !string.IsNullOrEmpty(to_transaction_date))
                    IsValidURL = true;

                if (!IsValidURL)
                    return (true, "Error: Invalid URL Format.", model);

                //Actual Transactions Code
                var TransactionIdList = new List<string>();
                (IsError, ErrorMessage, TransactionIdList) = await GetAllTransactionIdsList(transaction_date, from_transaction_date, to_transaction_date, provider);
                if (!IsError)
                {
                    if (TransactionIdList.Any())
                    {
                        for (int i = 0; i < TransactionIdList.Count(); i++)
                        {
                            var Transaction = new TransactionModel();
                            (_, _, Transaction) = await GetTransactionById(TransactionIdList[i]);

                            if (!string.IsNullOrEmpty(Transaction.TransactionId))
                                model.TransactionList.Add(Transaction);
                        }

                        return (IsError, ErrorMessage, model);
                    }
                    else
                        return (true, "No Transaction Found", model);
                }
                else
                    return (true, $"Error: {ErrorMessage}", model);
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related issues
                IsError = true;
                ErrorMessage = $"Http Request Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization issues
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
        }

        //Description :-  Gets all TransactionId list from taxjar.
        public virtual async Task<(bool, string, List<string>)> GetAllTransactionIdsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var TransactionIdList = new List<string>();
            try
            {
                bool IsValidURL = false;

                //Validaitons
                if (!string.IsNullOrEmpty(transaction_date) && string.IsNullOrEmpty(from_transaction_date) && string.IsNullOrEmpty(to_transaction_date) && string.IsNullOrEmpty(provider))
                    IsValidURL = true;
                if (string.IsNullOrEmpty(transaction_date) && !string.IsNullOrEmpty(from_transaction_date) && !string.IsNullOrEmpty(to_transaction_date))
                    IsValidURL = true;

                if (!IsValidURL)
                    return (true, "Error: Invalid URL Format.", TransactionIdList);

                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    //Query Parameter
                    // Build the URL with query parameters using UriBuilder
                    var builder = new UriBuilder(client.BaseAddress + "transactions/orders");

                    if (!string.IsNullOrEmpty(transaction_date) || !string.IsNullOrEmpty(from_transaction_date) || !string.IsNullOrEmpty(to_transaction_date) || !string.IsNullOrEmpty(provider))
                    {
                        // Add query parameters
                        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);  // For ASP.NET

                        if (!string.IsNullOrEmpty(transaction_date))
                            query["transaction_date"] = transaction_date;

                        if (!string.IsNullOrEmpty(from_transaction_date))
                            query["from_transaction_date"] = from_transaction_date;

                        if (!string.IsNullOrEmpty(to_transaction_date))
                            query["to_transaction_date"] = to_transaction_date;

                        if (!string.IsNullOrEmpty(provider))
                            query["provider"] = provider;

                        // Assign the query string to the UriBuilder
                        builder.Query = query.ToString();
                    }

                    // Send GET request to fetch the transaction
                    HttpResponseMessage response = await client.GetAsync(builder.ToString());

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, TransactionIdList);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var TransactionListResponse = JsonSerializer.Deserialize<TransactionIdsListModel>(responseBody);

                    if (TransactionListResponse != null && TransactionListResponse.TransactionId != null && TransactionListResponse.TransactionId.Any())
                    {
                        // Extract Transaction and add to the list.
                        TransactionIdList = TransactionListResponse.TransactionId.ToList();
                        return (IsError, ErrorMessage, TransactionIdList);
                    }
                    else
                    {
                        IsError = true;
                        ErrorMessage = "No Transaction Found";
                        return (IsError, ErrorMessage, TransactionIdList);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
                return (IsError, ErrorMessage, TransactionIdList);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, TransactionIdList);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, TransactionIdList);
            }

        }

        //Description :-  (GET) To get an transaction from taxjar by using TransactionId.
        //Input Parameters
        //TransactionId :- Specific transaction id which needs to be fetch.
        //Return values
        //TransactionModel :- This contains transaction data.
        public virtual async Task<(bool, string, TransactionModel)> GetTransactionById(string TransactionId)
        {
            var Transaction = new TransactionModel();
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send GET request to fetch the transaction
                    HttpResponseMessage response = await client.GetAsync($"transactions/orders/{TransactionId}");

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, Transaction);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var transactionOrder = JsonSerializer.Deserialize<TransactionDetailModel>(responseBody);

                    if (transactionOrder != null)
                        Transaction = transactionOrder.Order;  // Assign the fetched order
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage, Transaction);
        }

        //Description :-  Create customer into Taxjar.
        public virtual async Task<(bool, string)> CreateTransaction(TransactionModel model)
        {
            return await CreateUpdateTransaction(model, "Create");
        }

        //Description :-  Update customer into Taxjar.
        public virtual async Task<(bool, string)> UpdateTransaction(TransactionModel model)
        {
            return await CreateUpdateTransaction(model, "Update");
        }

        //Description :-  Create/Update a transaction into taxjar.
        //Input Parameters
        //TransactionOrder :- All transaction data which needs to be assigned for that transaction.
        //Action:- This will descide whether to create or update transaction into Taxjar.
        private async Task<(bool, string)> CreateUpdateTransaction(TransactionModel model, string Action)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            // Initialize HttpClient within a 'using' block to ensure proper disposal
            using (var client = new HttpClient())
            {
                try
                {
                    // Set the BaseAddress depending on the environment
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Configure JsonSerializer to ignore null values
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    // Serialize the model to JSON
                    string jsonContent = JsonSerializer.Serialize(model, options);
                    using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Send the POST request asynchronously
                        var response = new HttpResponseMessage();
                        if (Action == "Create")
                            response = await client.PostAsync("transactions/orders", httpContent);
                        else
                            response = await client.PutAsync($"transactions/orders/{model.TransactionId}", httpContent);

                        // Ensure the request was successful, else handle the error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Read the error message from the response body (async)
                            string errorDetails = await response.Content.ReadAsStringAsync();

                            // Log error or set appropriate error flags/messages
                            IsError = true;
                            ErrorMessage = $"Error: {response.StatusCode} - {errorDetails}";
                            return (IsError, ErrorMessage);
                        }
                        else
                            return (IsError, ErrorMessage);
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle network-related errors
                    IsError = true;
                    ErrorMessage = $"Request error: {ex.Message}";
                    return (IsError, ErrorMessage);
                }
                catch (TaskCanceledException ex)
                {
                    // Handle request timeout or cancellation
                    IsError = true;
                    ErrorMessage = "Request timed out: " + ex.Message;
                    return (IsError, ErrorMessage);
                }
                catch (Exception ex)
                {
                    // Handle any other errors
                    IsError = true;
                    ErrorMessage = "An unexpected error occurred: " + ex.Message;
                    return (IsError, ErrorMessage);
                }
            }
        }

        //Description :-  To delete an transaction.
        //Input Parameters
        //Id :- TransactionId need to passed from Taxjar to delete.
        public virtual async Task<(bool, string)> DeleteTransactionById(string Id)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send a DELETE request to remove the transaction
                    HttpResponseMessage response = await client.DeleteAsync("transactions/orders/" + Id);

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error content
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related issues
                IsError = true;
                ErrorMessage = $"Http Request Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization issues
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage);
        }
        #endregion

        #region Refund List & CRUD  Functions
        //Description :- Get all refunded transactions with details from Taxjar.
        public virtual async Task<(bool, string, RefundDetialListModel)> GetAllRefundTransactionsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var model = new RefundDetialListModel();

            try
            {
                bool IsValidURL = false;

                //Validaitons
                if (!string.IsNullOrEmpty(transaction_date) && string.IsNullOrEmpty(from_transaction_date) && string.IsNullOrEmpty(to_transaction_date) && string.IsNullOrEmpty(provider))
                    IsValidURL = true;
                if (string.IsNullOrEmpty(transaction_date) && !string.IsNullOrEmpty(from_transaction_date) && !string.IsNullOrEmpty(to_transaction_date))
                    IsValidURL = true;

                if (!IsValidURL)
                    return (true, "Error: Invalid URL Format.", model);

                var RefundIdList = new List<string>();

                (IsError, ErrorMessage, RefundIdList) = await GetAllRefundTransactionIdsList(transaction_date, from_transaction_date, to_transaction_date, provider);
                if (!IsError)
                {
                    if (RefundIdList.Any())
                    {
                        for (int i = 0; i < RefundIdList.Count(); i++)
                        {
                            var RefundTransaction = new RefundModel();
                            (_, _, RefundTransaction) = await GetRefundTransactionById(RefundIdList[i]);

                            if (!string.IsNullOrEmpty(RefundTransaction.TransactionId))
                                model.RefundList.Add(RefundTransaction);
                        }

                        return (IsError, ErrorMessage, model);
                    }
                    else
                        return (true, "No Transaction Found", model);
                }
                else
                    return (true, $"Error: {ErrorMessage}", model);
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related issues
                IsError = true;
                ErrorMessage = $"Http Request Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization issues
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, model);
            }
        }

        //Description :-  Gets all RefundId list from taxjar.
        public virtual async Task<(bool, string, List<string>)> GetAllRefundTransactionIdsList(string transaction_date = "", string from_transaction_date = "", string to_transaction_date = "", string provider = "")
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var RefundTransactionIdList = new List<string>();
            try
            {
                bool IsValidURL = false;

                //Validaitons
                if (!string.IsNullOrEmpty(transaction_date) && string.IsNullOrEmpty(from_transaction_date) && string.IsNullOrEmpty(to_transaction_date) && string.IsNullOrEmpty(provider))
                    IsValidURL = true;
                if (string.IsNullOrEmpty(transaction_date) && !string.IsNullOrEmpty(from_transaction_date) && !string.IsNullOrEmpty(to_transaction_date))
                    IsValidURL = true;

                if (!IsValidURL)
                    return (true, "Error: Invalid URL Format.", RefundTransactionIdList);

                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    //Query Parameter
                    // Build the URL with query parameters using UriBuilder
                    var builder = new UriBuilder(client.BaseAddress + "transactions/refunds");

                    if (!string.IsNullOrEmpty(transaction_date) || !string.IsNullOrEmpty(from_transaction_date) || !string.IsNullOrEmpty(to_transaction_date) || !string.IsNullOrEmpty(provider))
                    {
                        // Add query parameters
                        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);  // For ASP.NET

                        if (!string.IsNullOrEmpty(transaction_date))
                            query["transaction_date"] = transaction_date;

                        if (!string.IsNullOrEmpty(from_transaction_date))
                            query["from_transaction_date"] = from_transaction_date;

                        if (!string.IsNullOrEmpty(to_transaction_date))
                            query["to_transaction_date"] = to_transaction_date;

                        if (!string.IsNullOrEmpty(provider))
                            query["provider"] = provider;

                        // Assign the query string to the UriBuilder
                        builder.Query = query.ToString();
                    }

                    // Send GET request to fetch the refund transaction
                    HttpResponseMessage response = await client.GetAsync(builder.ToString());

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, RefundTransactionIdList);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var RefundTransactionResponse = JsonSerializer.Deserialize<RefundIdsListModel>(responseBody);

                    if (RefundTransactionResponse != null && RefundTransactionResponse.TransactionId != null && RefundTransactionResponse.TransactionId.Any())
                    {
                        // Extract Transaction and add to the list.
                        RefundTransactionIdList = RefundTransactionResponse.TransactionId.ToList();
                        return (IsError, ErrorMessage, RefundTransactionIdList);
                    }
                    else
                    {
                        IsError = true;
                        ErrorMessage = "No Transaction Found";
                        return (IsError, ErrorMessage, RefundTransactionIdList);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
                return (IsError, ErrorMessage, RefundTransactionIdList);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, RefundTransactionIdList);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, RefundTransactionIdList);
            }
        }

        //Description :-  (GET) To get an refund transaction from taxjar by using TransactionId.
        //Input Parameters
        //TransactionId :- Specific transaction id which needs to be fetch.
        //Return values
        //TransactionModel :- This contains transaction data.
        public virtual async Task<(bool, string, RefundModel)> GetRefundTransactionById(string TransactionId)
        {
            var RefundTransaction = new RefundModel();
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send GET request to fetch the transaction
                    HttpResponseMessage response = await client.GetAsync($"transactions/refunds/{TransactionId}");

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, RefundTransaction);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var transactionOrder = JsonSerializer.Deserialize<RefundDetailModel>(responseBody);

                    if (transactionOrder != null)
                        RefundTransaction = transactionOrder.Refund;  // Assign the fetched order
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage, RefundTransaction);
        }


        //Description :-  (GET) To generate refund of transaction and send it to taxjar by using orderId as TransactionId.
        //Input Parameters
        //TransactionId :- Specific transaction id which needs to be fetch.
        //Return values
        //TransactionModel :- This contains error flag, error message and refund transaction id.
        public virtual async Task<(bool, string, string)> GenerateRefundTransactionById(string TransactionId, decimal RefundPercentage, int RefundCount)
        {
            //Validations
            if (string.IsNullOrEmpty(TransactionId))
                return (true, "Error: Missing Transaction Id", "");

            if (RefundPercentage <= 0)
                return (true, "Error: Refund Percentage Cannot Be Less Than Zero", "");

            var (IsError, ErrorMessage, TransactionData) = await GetTransactionById(TransactionId);
            if (IsError)
                return (true, $"Error: {ErrorMessage}", "");

            if (string.IsNullOrEmpty(TransactionData.TransactionId))
                return (true, "Error: Transaction Data Not Found", "");

            //Start Refund
            var Refund = new RefundModel();
            Refund.TransactionId = $"{TransactionData.TransactionId}_R{RefundCount}";
            Refund.CustomerId = TransactionData.CustomerId != null ? TransactionData.CustomerId.ToString() : TransactionData.CustomerId; //CustomerId i.e PK of Customers Table
            Refund.TransactionReferenceId = TransactionData.TransactionId;
            Refund.TransactionDate = DateTime.Now.ToString("MM/dd/yyyy hh:mm");
            Refund.Provider = "api";
            Refund.ExemptionType = TransactionData.ExemptionType;
            Refund.ToCountry = TransactionData.ToCountry;
            Refund.ToZip = TransactionData.ToZip;
            Refund.ToState = TransactionData.ToState;
            Refund.ToCity = TransactionData.ToCity;
            Refund.ToStreet = TransactionData.ToStreet;

            Refund.Amount = -((TransactionData.Amount / 100) * RefundPercentage);
            Refund.Shipping = -((TransactionData.Shipping / 100) * RefundPercentage);
            Refund.SalesTax = -((TransactionData.SalesTax / 100) * RefundPercentage);

            if (TransactionData.LineItems != null && TransactionData.LineItems.Count() > 0)
            {
                foreach (var item in TransactionData.LineItems)
                {
                    var LineItem = new LineItemModel();
                    LineItem.Id = item.Id;
                    LineItem.Quantity = item.Quantity;
                    LineItem.ProductIdentifier = item.ProductIdentifier;
                    LineItem.Description = item.Description;
                    LineItem.UnitPrice = -((item.UnitPrice / 100) * RefundPercentage);
                    LineItem.Discount = -((item.Discount / 100) * RefundPercentage);
                    LineItem.SalesTax = -((item.SalesTax / 100) * RefundPercentage);

                    Refund.LineItems.Add(LineItem);
                }
            }

            (IsError, ErrorMessage) = await CreateRefundTransaction(Refund);
            if (IsError)
                return (true, $"Error: {ErrorMessage}", "");

            return (IsError, "", Refund.TransactionId);
        }

        //Description :-  Create customer into Taxjar.
        public virtual async Task<(bool, string)> CreateRefundTransaction(RefundModel model)
        {
            return await CreateUpdateTransaction(model, "Create");
        }

        //Description :-  Update customer into Taxjar.
        public virtual async Task<(bool, string)> UpdateRefundTransaction(RefundModel model)
        {
            return await CreateUpdateTransaction(model, "Update");
        }

        //Description :-  Create/Update a refund transaction into taxjar.
        //Input Parameters
        //RefundOrder :- All refund data which needs to be assigned for that transaction.
        //Action:- This will descide whether to create or update transaction into Taxjar.
        private async Task<(bool, string)> CreateUpdateTransaction(RefundModel model, string Action)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            // Initialize HttpClient within a 'using' block to ensure proper disposal
            using (var client = new HttpClient())
            {
                try
                {
                    // Set the BaseAddress depending on the environment
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Configure JsonSerializer to ignore null values
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    // Serialize the model to JSON
                    string jsonContent = JsonSerializer.Serialize(model, options);
                    using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Send the POST request asynchronously
                        HttpResponseMessage response = new HttpResponseMessage();
                        if (Action == "Create")
                            response = await client.PostAsync("transactions/refunds", httpContent);
                        else
                            response = await client.PutAsync($"transactions/refunds/{model.TransactionId}", httpContent);

                        // Ensure the request was successful, else handle the error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Read the error message from the response body (async)
                            string errorDetails = await response.Content.ReadAsStringAsync();

                            // Log error or set appropriate error flags/messages
                            IsError = true;
                            ErrorMessage = $"Error: {response.StatusCode} - {errorDetails}";
                            return (IsError, ErrorMessage);
                        }
                        else
                            return (IsError, ErrorMessage);
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle network-related errors
                    IsError = true;
                    ErrorMessage = $"Request error: {ex.Message}";
                    return (IsError, ErrorMessage);
                }
                catch (TaskCanceledException ex)
                {
                    // Handle request timeout or cancellation
                    IsError = true;
                    ErrorMessage = "Request timed out: " + ex.Message;
                    return (IsError, ErrorMessage);
                }
                catch (Exception ex)
                {
                    // Handle any other errors
                    IsError = true;
                    ErrorMessage = "An unexpected error occurred: " + ex.Message;
                    return (IsError, ErrorMessage);
                }
            }
        }

        //Description :-  To delete an refund transaction.
        //Input Parameters
        //Id :- TransactionId need to passed from Taxjar to delete.
        public virtual async Task<(bool, string)> DeleteRefundTransactionById(string Id)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send a DELETE request to remove the refund transaction
                    HttpResponseMessage response = await client.DeleteAsync("transactions/refunds/" + Id);

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error content
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related issues
                IsError = true;
                ErrorMessage = $"Http Request Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization issues
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }

            return (IsError, ErrorMessage);
        }
        #endregion

        #region Customer List & CRUD  Functions
        //Description :- Get all customer with details from Taxjar at a time.
        public virtual async Task<(bool, string, CustomerDetailListModel)> GetAllCustomerList()
        {
            var model = new CustomerDetailListModel();
            var CustomerIdList = new List<string>();

            (IsError, ErrorMessage, CustomerIdList) = await GetAllCustomerIdsList();
            if (!IsError)
            {
                if (CustomerIdList.Any())
                {
                    for (int i = 0; i < CustomerIdList.Count(); i++)
                    {
                        var Customer = new CustomerModel();
                        (_, _, Customer) = await GetCustomerById(CustomerIdList[i]);

                        if (!string.IsNullOrEmpty(Customer.CustomerId))
                            model.CustomerList.Add(Customer);
                    }

                    return (IsError, ErrorMessage, model);
                }
                else
                    return (true, "No Customer Found", model);
            }
            else
                return (true, $"Error: {ErrorMessage}", model);
        }

        //Description :-  Gets all CustomerId list from taxjar.
        public async Task<(bool, string, List<string>)> GetAllCustomerIdsList()
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var CustomerIdList = new List<string>();
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send GET request to fetch the transaction
                    HttpResponseMessage response = await client.GetAsync($"customers");

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, CustomerIdList);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var customerListResponse = JsonSerializer.Deserialize<CustomerIdsListModel>(responseBody);

                    if (customerListResponse != null && customerListResponse.CustomerId.Any())
                    {
                        // Extract CustomerId from each customer and add to the list
                        CustomerIdList = customerListResponse.CustomerId.ToList();
                    }
                    else
                    {
                        IsError = true;
                        ErrorMessage = "No Customer Found";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
            }
            return (IsError, ErrorMessage, CustomerIdList);
        }

        //Description :-  Get a customer from taxjar.
        //Input Parameters
        //Id :- Customer Id should be passed to get Customer Details.
        public virtual async Task<(bool, string, CustomerModel)> GetCustomerById(string Id)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            var Customer = new CustomerModel();

            if (string.IsNullOrEmpty(Id))
                return (true, "Missing Customer Id", Customer);

            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send GET request to fetch the transaction
                    HttpResponseMessage response = await client.GetAsync($"customers/{Id}");

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error message
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage, Customer);  // Return early on error
                    }

                    // Read and deserialize the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var CustomerDetail = JsonSerializer.Deserialize<CustomerDetailModel>(responseBody);
                    if (CustomerDetail != null && CustomerDetail.Customer != null && CustomerDetail.Customer.CustomerId != null)
                    {
                        Customer = CustomerDetail.Customer;
                        return (IsError, ErrorMessage, Customer);
                    }
                    else
                    {
                        IsError = true;
                        ErrorMessage = "Error:-  Deserialize Error";
                        return (IsError, ErrorMessage, Customer);  // Return early on error
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related exceptions
                IsError = true;
                ErrorMessage = $"HTTP Request Error: {ex.Message}";
                return (IsError, ErrorMessage, Customer);  // Return early on error
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization errors
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, Customer);  // Return early on error
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException}";
                return (IsError, ErrorMessage, Customer);  // Return early on error
            }
        }

        //Description :-  Create customer into Taxjar.
        public virtual async Task<(bool, string)> CreateCustomer(CustomerModel model)
        {
            return await CreateUpdateCustomer(model, "Create");
        }

        //Description :-  Update customer into Taxjar.
        public virtual async Task<(bool, string)> UpdateCustomer(CustomerModel model)
        {
            return await CreateUpdateCustomer(model, "Update");
        }
        //Description :-  Create a new customer or Update an existing customer using customerId.
        //Input Parameters
        //model :- Customer Details Object.
        //Action :- Create/Update - This parameter will decide wheather to update customer or create a new into Taxjar.
        private async Task<(bool, string)> CreateUpdateCustomer(CustomerModel model, string Action)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            // Initialize HttpClient within a 'using' block to ensure proper disposal
            using (var client = new HttpClient())
            {
                try
                {
                    // Set the BaseAddress depending on the environment
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Configure JsonSerializer to ignore null values
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    // Serialize the model to JSON
                    string jsonContent = JsonSerializer.Serialize(model, options);
                    using (var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                    {
                        // Send the POST request asynchronously
                        HttpResponseMessage response = new HttpResponseMessage();
                        if (Action == "Create")
                            response = await client.PostAsync("customers", httpContent);
                        else
                            response = await client.PutAsync($"customers/{model.CustomerId}", httpContent);

                        // Ensure the request was successful, else handle the error
                        if (!response.IsSuccessStatusCode)
                        {
                            // Read the error message from the response body (async)
                            string errorDetails = await response.Content.ReadAsStringAsync();

                            // Log error or set appropriate error flags/messages
                            IsError = true;
                            ErrorMessage = $"Error: {response.StatusCode} - {errorDetails}";
                            return (IsError, ErrorMessage);
                        }
                        else
                            return (IsError, ErrorMessage);
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle network-related errors
                    IsError = true;
                    ErrorMessage = $"Request error: {ex.Message} || {ex.InnerException}";
                    return (IsError, ErrorMessage);
                }
                catch (TaskCanceledException ex)
                {
                    // Handle request timeout or cancellation
                    IsError = true;
                    ErrorMessage = $"Request timed out: {ex.Message} || {ex.InnerException}";
                    return (IsError, ErrorMessage);
                }
                catch (Exception ex)
                {
                    // Handle any other errors
                    IsError = true;
                    ErrorMessage = $"An unexpected error occurred: {ex.Message} || {ex.InnerException}";
                    return (IsError, ErrorMessage);
                }
            }
        }

        //Description :-  To delete a customer.
        //Input Parameters
        //Id :- CustomerId need to passed from Taxjar to delete.
        public virtual async Task<(bool, string)> DeleteCustomerById(string Id)
        {
            IsError = false;
            ErrorMessage = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    // Set the BaseAddress of the client
                    client.BaseAddress = new Uri(TaxJarLiveApiUrl);

                    // Set the Authorization header with the Bearer token
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IsLiveEnvironment ? TaxJarLiveApiKey : TaxJarTestApiKey);

                    // Send a DELETE request to remove the transaction
                    HttpResponseMessage response = await client.DeleteAsync($"customers/{Id}");

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        // Capture detailed error content
                        string errorContent = await response.Content.ReadAsStringAsync();
                        IsError = true;
                        ErrorMessage = $"Error: {response.StatusCode} - {errorContent}";
                        return (IsError, ErrorMessage);
                    }
                    else
                        return (IsError, ErrorMessage);
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle network-related issues
                IsError = true;
                ErrorMessage = $"Http Request Error: {ex.Message} -> {ex.InnerException?.Message}";
                return (IsError, ErrorMessage);
            }
            catch (JsonException ex)
            {
                // Handle JSON serialization/deserialization issues
                IsError = true;
                ErrorMessage = $"JSON Serialization Error: {ex.Message} -> {ex.InnerException?.Message}";
                return (IsError, ErrorMessage);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                IsError = true;
                ErrorMessage = $"Unexpected Error: {ex.Message} -> {ex.InnerException?.Message}";
                return (IsError, ErrorMessage);
            }
        }
        #endregion
    }
}
