using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nop.Core.Domian.TaxJar
{
    #region TaxJar API Implementation
    public class TaxJarAddress
    {
        public string Country { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
    }
    #region Taxation & Pre-Order Classes

    //Store result into cache for faster performance.
    public class TaxRatesByZipCodeCache
    {
        public string StateName { get; set; }
        public string Zipcode { get; set; }
        public decimal TaxPercentage { get; set; }
    }

    //TaxRate
    public class TaxRateModel
    {
        public string city { get; set; }
        public string city_rate { get; set; }
        public string combined_district_rate { get; set; }
        public string combined_rate { get; set; }
        public string country { get; set; }
        public string country_rate { get; set; }
        public string county { get; set; }
        public string county_rate { get; set; }
        public bool freight_taxable { get; set; }
        public string state { get; set; }
        public string state_rate { get; set; }
        public string zip { get; set; }
    }

    //TaxRate Response
    public class TaxRateResponseModel
    {
        public TaxRateResponseModel()
        {
            rate = new TaxRateModel();
        }

        public TaxRateModel rate { get; set; }
    }

    //Nexus Regions
    public class NexusRegionsModel
    {
        public string country_code { get; set; }
        public string country { get; set; }
        public string region_code { get; set; }
        public string region { get; set; }
    }

    //Nexus Regions Response
    public class NexusResponseModel
    {
        public NexusResponseModel()
        {
            regions = new List<NexusRegionsModel>();
        }

        public List<NexusRegionsModel> regions { get; set; }
    }
    #endregion

    #region Calculate sales tax for an order

    // For API Call
    public class SalesTaxForOrderModel
    {
        public SalesTaxForOrderModel()
        {
            nexus_addresses = new List<SalesTaxNexusAddresses>();
            line_items = new List<SalesTaxLineItems>();
        }

        public string from_country = "US";
        public string from_zip = "32259";
        public string from_state = "FL";
        public string from_city = "Saint Johns";
        public string from_street = "566 Falcon Fork Way";
        public string to_country { get; set; }
        public string to_zip { get; set; }
        public string to_state { get; set; }
        public string to_city { get; set; }
        public string to_street { get; set; }
        public float amount { get; set; }
        public float shipping { get; set; }
        public string customer_id { get; set; }
        public string exemption_type { get; set; }

        public List<SalesTaxNexusAddresses> nexus_addresses { get; set; }
        public List<SalesTaxLineItems> line_items { get; set; }
    }
    public class SalesTaxNexusAddresses
    {
        public string id { get; set; }
        public string country = "US";
        public string zip = "32259";
        public string state = "FL";
        public string city = "Saint Johns";
        public string street = "566 Falcon Fork Way";
    }
    public class SalesTaxLineItems
    {
        public string id { get; set; }
        public string quantity { get; set; }
        public string product_tax_code { get; set; }
        public string unit_price { get; set; }
        public string discount { get; set; }
    }

    // After API call
    public class ResponseForSalesTaxForOrderModel
    {
        public ResponseForSalesTaxForOrderModel()
        {
            tax = new ResponseForTax();
        }

        public ResponseForTax tax {get; set;}
    }
    public class ResponseForTax
    {
        public float order_total_amount { get; set; }
        public float shipping { get; set; }
        public float taxable_amount { get; set; }
        public float amount_to_collect { get; set; }
        public float rate { get; set; }
        public bool has_nexus { get; set; }
        public bool freight_taxable { get; set; }
        public string tax_source { get; set; }

        public ResponseForJurisdictions jurisdictions {  get; set; }
        public ResponseForBreakdown breakdown {  get; set; }
    }
    public class ResponseForJurisdictions
    {
        public string country { get; set; }
        public string state { get; set; }
        public string county { get; set; }
        public string city { get; set; }
    }
    public class ResponseForBreakdown
    {
        public ResponseForBreakdown()
        {
            line_items = new List<ResponseForLineItems>();
        }

        public float taxable_amount { get; set; }
        public float tax_collectable { get; set; }
        public float combined_tax_rate { get; set; }

        public float state_taxable_amount { get; set; }
        public float state_tax_rate { get; set; }
        public float state_tax_collectable { get; set; }

        public float county_taxable_amount { get; set; }
        public float county_tax_rate { get; set; }
        public float county_tax_collectable { get; set; }

        public float city_taxable_amount { get; set; }
        public float city_tax_rate { get; set; }
        public float city_tax_collectable { get; set; }

        public float special_district_taxable_amount { get; set; }
        public float special_tax_rate { get; set; }
        public float special_district_tax_collectable { get; set; }

        public List<ResponseForLineItems> line_items { get; set; }
    }
    public class ResponseForLineItems
    {
        public string id { get; set; }
        public float taxable_amount { get; set; }
        public float tax_collectable { get; set; }
        public float combined_tax_rate { get; set; }
        public float state_taxable_amount { get; set; }
        public float state_sales_tax_rate { get; set; }
        public float state_amount { get; set; }
        public float county_taxable_amount { get; set; }
        public float county_tax_rate { get; set; }
        public float county_amount { get; set; }
        public float city_taxable_amount { get; set; }
        public float city_tax_rate { get; set; }
        public float city_amount { get; set; }
        public float special_district_taxable_amount { get; set; }
        public float special_tax_rate { get; set; }
        public float special_district_amount { get; set; }
    }

    #endregion

    #region Transaction Classes
    //Transaction Id's List
    public class TransactionIdsListModel
    {
        public TransactionIdsListModel()
        {
            TransactionId = new List<string>();
        }

        [JsonPropertyName("orders")]
        public List<string> TransactionId { get; set; }
    }

    //Transaction's Detail List
    public class TransactionDetialListModel
    {
        public TransactionDetialListModel()
        {
            TransactionList = new List<TransactionModel>();
        }

        public List<TransactionModel> TransactionList { get; set; }
    }

    //Order Transactions - Outer Class for transactions
    public class TransactionDetailModel
    {
        public TransactionDetailModel()
        {
            Order = new TransactionModel();
        }

        [JsonPropertyName("order")]
        public TransactionModel Order { get; set; }
    }

    //Transactions
    public class TransactionModel
    {
        public TransactionModel()
        {
            LineItems = new List<LineItemModel>();
        }

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }

        [JsonPropertyName("transaction_date")]
        public string? TransactionDate { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }


        [JsonPropertyName("from_country")]
        public string FromCountry = "US";

        [JsonPropertyName("from_zip")]
        public string FromZip = "32259";

        [JsonPropertyName("from_state")]
        public string FromState = "FL";

        [JsonPropertyName("from_city")]
        public string FromCity = "Saint Johns";

        [JsonPropertyName("from_street")]
        public string FromStreet = "566 Falcon Fork Way";


        [JsonPropertyName("to_country")]
        public string ToCountry { get; set; }

        [JsonPropertyName("to_zip")]
        public string ToZip { get; set; }

        [JsonPropertyName("to_state")]
        public string ToState { get; set; }

        [JsonPropertyName("to_city")]
        public string? ToCity { get; set; }

        [JsonPropertyName("to_street")]
        public string? ToStreet { get; set; }


        [JsonPropertyName("amount")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Amount { get; set; }

        [JsonPropertyName("shipping")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Shipping { get; set; }

        [JsonPropertyName("sales_tax")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal SalesTax { get; set; }

        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("exemption_type")]
        public string? ExemptionType { get; set; }

        // A list of Line Items (can be more than one product in the order)
        [JsonPropertyName("line_items")]
        public List<LineItemModel> LineItems { get; set; }
    }

    //Transactions - Line Items
    public class LineItemModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("product_identifier")]
        public string? ProductIdentifier { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("unit_price")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Discount { get; set; }

        [JsonPropertyName("sales_tax")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal SalesTax { get; set; }
    }
    #endregion

    #region Refund Classes
    //Refund Id's List
    public class RefundIdsListModel
    {
        public RefundIdsListModel()
        {
            TransactionId = new List<string>();
        }

        [JsonPropertyName("refunds")]
        public List<string> TransactionId { get; set; }
    }

    //Refund's Detail List
    public class RefundDetialListModel
    {
        public RefundDetialListModel()
        {
            RefundList = new List<RefundModel>();
        }

        public List<RefundModel> RefundList { get; set; }
    }

    //Refund Transactions - Outer Class for refunds.
    public class RefundDetailModel
    {
        public RefundDetailModel()
        {
            Refund = new RefundModel();
        }

        [JsonPropertyName("refund")]
        public RefundModel Refund { get; set; }
    }

    //Transactions
    public class RefundModel
    {
        public RefundModel()
        {
            LineItems = new List<LineItemModel>();
        }

        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; } 
        
        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; }

        [JsonPropertyName("transaction_reference_id")]
        public string TransactionReferenceId { get; set; }

        [JsonPropertyName("transaction_date")]
        public string TransactionDate { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("exemption_type")]
        public string ExemptionType { get; set; }

        [JsonPropertyName("to_country")]
        public string ToCountry { get; set; }

        [JsonPropertyName("to_zip")]
        public string ToZip { get; set; }

        [JsonPropertyName("to_state")]
        public string ToState { get; set; }

        [JsonPropertyName("to_city")]
        public string? ToCity { get; set; }

        [JsonPropertyName("to_street")]
        public string? ToStreet { get; set; }

        [JsonPropertyName("amount")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Amount { get; set; }

        [JsonPropertyName("shipping")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Shipping { get; set; }

        [JsonPropertyName("sales_tax")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal SalesTax { get; set; }
        
        [JsonPropertyName("handling")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal HandlingCharges { get; set; }

        // A list of Line Items (can be more than one product in the order)
        [JsonPropertyName("line_items")]
        public List<LineItemModel> LineItems { get; set; }
    }

    //Refund - Line Items
    public class RefundLineItemModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("product_identifier")]
        public string? ProductIdentifier { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("unit_price")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal Discount { get; set; }

        [JsonPropertyName("sales_tax")]
        [JsonConverter(typeof(StringToDecimalConverter))]  // Use the custom converter
        public decimal SalesTax { get; set; }
    }
    #endregion

    #region Customer Classes
    //Customer Id's List
    public class CustomerIdsListModel
    {
        public CustomerIdsListModel()
        {
            CustomerId = new List<string>();
        }

        [JsonPropertyName("customers")]
        public List<string> CustomerId { get; set; }
    }

    //Customer's Detail List
    public class CustomerDetailListModel
    {
        public CustomerDetailListModel()
        {
            CustomerList = new List<CustomerModel>();
        }

        public List<CustomerModel> CustomerList { get; set; }
    }

    //Customer - Outer Class for Customer.
    public class CustomerDetailModel
    {
        public CustomerDetailModel()
        {
            Customer = new CustomerModel();
        }

        [JsonPropertyName("customer")]
        public CustomerModel Customer { get; set; }
    }

    //Customer
    public class CustomerModel
    {
        [JsonPropertyName("customer_id")]
        public string CustomerId { get; set; }

        [JsonPropertyName("exemption_type")]
        public string ExemptionType { get; set; }

        [JsonPropertyName("name")]
        public string FullName { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("zip")]
        public string? ZipCode { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("exempt_regions")]
        public List<ExemptRegionsModel> ExemptRegions { get; set; }
    }

    //Customer's Exempt Regions.
    public class ExemptRegionsModel
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
    }

    #endregion

    #endregion

    #region JSON: Custom Convert Functions
    //Json datatype conversion from string to decimal
    public class StringToDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string stringValue = reader.GetString();
                if (decimal.TryParse(stringValue, out var result))
                {
                    return result;
                }
            }
            return reader.GetDecimal();
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
    #endregion
}
