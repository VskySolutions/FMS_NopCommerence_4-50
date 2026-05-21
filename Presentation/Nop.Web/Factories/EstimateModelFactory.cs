using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Authentication.External;
using Nop.Services.Common;
using Nop.Services.Customers;
//using Nop.Services.Estimates;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Models.Common;
using Nop.Web.Models.Estimate;
//using WebGrease.Css.Extensions;
using Nop.Services.Authentication;
using Nop.Services.Tax;
using Nop.Services.Logging;
using Nop.Services.Events;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain;
using Nop.Services.Catalog;
using Nop.Services.Discounts;
using Nop.Core.Domain.Estimates;
using Nop.Services.ExportImport.Help;
using Nop.Services.ExportImport;
using System.IO;
using System.Globalization;
using Nop.Services.Estimates;
using System.Threading.Tasks;
using Nop.Data;
using OfficeOpenXml.Core.ExcelPackage;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domian.TaxJar;
using Nop.Web.Models.ShoppingCart;
using Nop.Services.Payments;
using System.Text.RegularExpressions;

namespace Nop.Web.Factories
{
    public partial class EstimateModelFactory : IEstimateModelFactory
    {
        #region Fields
        private readonly IRepository<EstimateItem> _estimateItemRepository;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IPictureService _pictureService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IEstimateService _EstimateService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly IExportManager _exportManager;
        private readonly IUrlRecordService _urlRecordService;
        private readonly ICustomerService _customerService;
        #endregion
        #region Ctor

        public EstimateModelFactory(
            ILocalizationService localizationService,
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            IPictureService pictureService,
             IEstimateService EstimateService,
             IProductAttributeParser productAttributeParser,
             IProductAttributeFormatter productAttributeFormatter,
             ITaxService taxService,
             IPriceCalculationService priceCalculationService,
             ICurrencyService currencyService,
             IPriceFormatter priceFormatter,
             IProductService productService,
             IExportManager exportManager,
             IUrlRecordService urlRecordService,
            IRepository<EstimateItem> estimateItemRepository,
            ICustomerService customerService)

        {
            _localizationService = localizationService;
            _workContext = workContext;
            _shoppingCartService = shoppingCartService;
            _estimateItemRepository = estimateItemRepository;
            _pictureService = pictureService;
            _EstimateService = EstimateService;
            _productAttributeParser = productAttributeParser;
            _productAttributeFormatter = productAttributeFormatter;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _productService = productService;
            _exportManager = exportManager;
            _urlRecordService = urlRecordService;
            _customerService = customerService;
        }
        #endregion

        #region Methods
        public virtual EstimateDetailsModel PrepareEstimateNavigationModel(EstimateDetailsModel model, int CustId = 0)
        {
            if (CustId == 0)
                return null;

            var MyList = _EstimateService.GetEstimateByCustomerNumber(CustId);

            foreach (var item in MyList)
            {
                //model.NavBar.Add(new NavigationClass { ListId = item.Id, Name = item.Name + "," + item.CreatedOnUtc.Date.ToShortDateString() + "," + customerEmail });
                var estItems = _EstimateService.GetEstimateItemByEstimateId(item.Id);
                //Added by Yogesh Kumbhar on Dt: 12-20-2024
                var createdBy = item.CreatedBy != null ? _customerService.GetCustomerByIdAsync((int)item.CreatedBy) : null;


                var itemCount = false;
                if (estItems != null)
                    itemCount = true;

                //Added by Yogesh Kumbhar on Dt: 12-02-2024
                model.NavBar.Add(new NavigationClass
                {
                    ListId = item.Id,
                    Name = item.Name,
                    CreatedOn = item.CreatedOnUtc.Date.ToShortDateString(),
                    //CustomerEmail = customerEmail,
                    Description = item.Discription,
                    IsEstimateItems = itemCount,
                    CratedbyStr = createdBy != null ? createdBy.Result.Email : ""
                });
            }
            return model;
        }

        public virtual EstimateDetailsModel PrepareAddNewList(EstimateDetailsModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            PrepareEstimateNavigationModel(model, model.CustomerId);

            return model;
        }

        public virtual async Task<EstimateDetailsModel> PrepareGetEstimateDetailsById(int estimateid = 0, int customerid = 0)
        {
            decimal Totsum = 0;
            if (estimateid == 0 || customerid == 0)
                throw new ArgumentNullException("model");
            var model = new EstimateDetailsModel();
            var MyList = _EstimateService.GetEstimateByCustomerNumber(customerid);
            foreach (var item in MyList)
            {
                model.NavBar.Add(new NavigationClass { ListId = item.Id, Name = item.Name + "," + item.CreatedOnUtc.Date.ToShortDateString() });
            }

            var estimate = await _EstimateService.GetEstimateById(estimateid);
            model.EstimateId = estimate.Id;
            model.IsAddToCart = estimate.IsAddToCart;
            model.CartAddedDate = Convert.ToDateTime(estimate.CartAddedDate);
            model.StoreId = estimate.StoreId;
            model.Name = estimate.Name;
            model.Discription = estimate.Discription;
            model.CustomerId = estimate.CustomerId;
            model.EstimateStatusId = estimate.EstimateStatusId;
            model.EstimateTotal = estimate.EstimateTotal;
            model.CreatedBy = estimate.CreatedBy;
            model.Note = estimate.Note;
            var EstimateItemsList = estimate.EstimateItems.ToList();

            var EstimateItems = _estimateItemRepository.GetAll().Where(m => m.EstimateId == estimateid).ToList();

            foreach (var item in EstimateItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                var skuatt = await _productService.FormatSkuAsync(product, item.AttributesXml);

                var obj = new EstimateNavigationModel();
                obj.ListId = item.Id;
                obj.ProductId = item.ProductId;
                if (string.IsNullOrEmpty(item.AttributesXml) == false)
                {
                    obj.Sku = product.Sku;
                }
                else
                {
                    obj.Sku = skuatt;
                }
                obj.Name = product.Name;
                obj.ProductSeName = await _urlRecordService.GetSeNameAsync(product);
                var orderItemPicture = await _pictureService.GetProductPictureAsync(product, item.AttributesXml);
                obj.Picture.ImageUrl = orderItemPicture != null ? await _pictureService.GetPictureUrlAsync(orderItemPicture.Id, 75, true) : null;
                obj.Quantity = item.Quantity;
                obj.AttributeDescription = await _productAttributeFormatter.FormatAttributesAsync(product, item.AttributesXml);

                Customer customer = await _workContext.GetCurrentCustomerAsync();
                ShoppingCartItem shoppingCartItem = new ShoppingCartItem();

                shoppingCartItem.RentalEndDateUtc = item.RentalEndDateUtc;
                shoppingCartItem.ProductId = item.ProductId;
                shoppingCartItem.CustomerId = item.ProductId;
                shoppingCartItem.Quantity = item.Quantity;
                shoppingCartItem.AttributesXml = item.AttributesXml;
                shoppingCartItem.CustomerEnteredPrice = item.CustomerEnteredPrice;
                shoppingCartItem.RentalStartDateUtc = item.RentalStartDateUtc;
                shoppingCartItem.RentalEndDateUtc = item.RentalEndDateUtc;
                shoppingCartItem.CustomerId = item.CustomerId;

                //unit prices
                if (product.CallForPrice)
                {
                    obj.UnitPrice = await _localizationService.GetResourceAsync("Products.CallForPrice");
                }
                else
                {
                    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                    var (shoppingCartUnitPriceWithDiscountBase, _) = await _taxService.GetProductPriceAsync(product, (await _shoppingCartService.GetUnitPriceAsync(shoppingCartItem, true)).unitPrice);
                    var shoppingCartUnitPriceWithDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartUnitPriceWithDiscountBase, currentCurrency);
                    obj.UnitPrice = await _priceFormatter.FormatPriceAsync(shoppingCartUnitPriceWithDiscount);

                }
                //subtotal, discount
                if (product.CallForPrice)
                {
                    obj.SubTotal = await _localizationService.GetResourceAsync("Products.CallForPrice");
                }
                else
                {
                    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                    var (subTotal, shoppingCartItemDiscountBase, _, maximumDiscountQty) = await _shoppingCartService.GetSubTotalAsync(shoppingCartItem, true);
                    var (shoppingCartItemSubTotalWithDiscountBase, _) = await _taxService.GetProductPriceAsync(product, subTotal);
                    var shoppingCartItemSubTotalWithDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartItemSubTotalWithDiscountBase, currentCurrency);
                    Totsum += shoppingCartItemSubTotalWithDiscount;
                    obj.SubTotal = await _priceFormatter.FormatPriceAsync(shoppingCartItemSubTotalWithDiscount);
                    //obj.SubTotalValue = shoppingCartItemSubTotalWithDiscount;
                    obj.MaximumDiscountedQty = maximumDiscountQty;

                    //display an applied discount amount
                    if (shoppingCartItemDiscountBase > decimal.Zero)
                    {
                        (shoppingCartItemDiscountBase, _) = await _taxService.GetProductPriceAsync(product, shoppingCartItemDiscountBase);
                        if (shoppingCartItemDiscountBase > decimal.Zero)
                        {
                            decimal shoppingCartItemDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartItemDiscountBase, await _workContext.GetWorkingCurrencyAsync());
                            obj.Discount = await _priceFormatter.FormatPriceAsync(shoppingCartItemDiscount);
                        }
                    }
                }
                model.Items.Add(obj);
            }
            model.Subtot = Totsum;
            return model;
        }

        public virtual async Task<byte[]> ExportExcelFile(int EstimateCode = 0)
        {
            var EstimateData = await _EstimateService.GetEstimateById(EstimateCode);
            var model = new EstimateDetailsModel();
            var ListD = new List<ExelKeys>();
            var EstimateItems = _estimateItemRepository.GetAll().Where(m => m.EstimateId == EstimateData.Id).ToList();
            foreach (var item in EstimateItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);

                ////var parsedProductAttributes = await _productAttributeParser.ParseProductAttributeMappingsAsync(item.AttributesXml);

                //Added by Yogesh Kumbhar on Dt: 01-23-2025
                var attributeDescription = await _productAttributeFormatter.FormatAttributesAsync(product, item.AttributesXml);
                var skuatt = await _productService.FormatSkuAsync(product, item.AttributesXml);

                var obj = new ExelKeys();
                obj.Quantity = item.Quantity;
                if (item.AttributesXml == string.Empty)
                {
                    obj.Name = product.Name;
                }
                else
                {
                    //var attStr = await _productAttributeParser.ParseProductAttributeMappingsAsync(item.AttributesXml);
                    //// _productAttributeFormatter.FormatAttributes(item.Product, item.AttributesXml);
                    //obj.Name = product.Name + " \r\n" + attStr + "";

                    //Added by Yogesh Kumbhar on Dt: 01-23-2025
                    obj.Name = product.Name + " \r\n" + Regex.Replace(attributeDescription, "<br\\s*/?>", "\n") + "";
                }

                if (item.AttributesXml == string.Empty)
                {
                    obj.SKU = product.Sku;
                }
                else
                {
                    obj.SKU = skuatt;
                }

                Customer customer = await _workContext.GetCurrentCustomerAsync();

                //Added by Yogesh Kumbhar on Dt: 02-11-2025
                if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin") && EstimateData.CreatedBy != null)
                {
                    var customerDetails = await _customerService.GetCustomerEmailWithCompanyAsync((int)EstimateData.CreatedBy);
                    obj.CreatedBy = "\r\nFMS Solutions, LLC\n566 Falcon Fork Way\nSaint Johns,FL 32259\nC: 201-264-8365";
                    obj.CreatedFor = "\r\n" + customerDetails;
                }

                ShoppingCartItem shoppingCartItem = new ShoppingCartItem();

                shoppingCartItem.RentalEndDateUtc = item.RentalEndDateUtc;
                shoppingCartItem.ProductId = item.ProductId;
                shoppingCartItem.CustomerId = item.ProductId;
                shoppingCartItem.Quantity = item.Quantity;
                shoppingCartItem.AttributesXml = item.AttributesXml;
                shoppingCartItem.CustomerEnteredPrice = item.CustomerEnteredPrice;
                shoppingCartItem.RentalStartDateUtc = item.RentalStartDateUtc;
                shoppingCartItem.RentalEndDateUtc = item.RentalEndDateUtc;
                shoppingCartItem.CustomerId = item.CustomerId;

                //unit prices
                if (product.CallForPrice)
                {
                    obj.UnitPrice = await _localizationService.GetResourceAsync("Products.CallForPrice");
                }
                else
                {
                    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                    var (shoppingCartUnitPriceWithDiscountBase, _) = await _taxService.GetProductPriceAsync(product, (await _shoppingCartService.GetUnitPriceAsync(shoppingCartItem, true)).unitPrice);
                    var shoppingCartUnitPriceWithDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartUnitPriceWithDiscountBase, currentCurrency);
                    obj.UnitPrice = await _priceFormatter.FormatPriceAsync(shoppingCartUnitPriceWithDiscount);
                    //obj.UnitPrice = string.IsNullOrEmpty(UnitProice) == false ? Decimal.TryParse(UnitProice,out obj.UnitPrice) : 0;  
                }
                // Total Sub Total
                //subtotal, discount
                if (product.CallForPrice)
                {
                    string TotalPrice = await _localizationService.GetResourceAsync("Products.CallForPrice");
                    obj.TotalPrice = string.IsNullOrEmpty(TotalPrice) == false ? Convert.ToDecimal(TotalPrice) : 0;
                }
                else
                {

                    var currentCurrency = await _workContext.GetWorkingCurrencyAsync();
                    var (subTotal, shoppingCartItemDiscountBase, _, maximumDiscountQty) = await _shoppingCartService.GetSubTotalAsync(shoppingCartItem, true);
                    var (shoppingCartItemSubTotalWithDiscountBase, _) = await _taxService.GetProductPriceAsync(product, subTotal);
                    var shoppingCartItemSubTotalWithDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartItemSubTotalWithDiscountBase, currentCurrency);
                    obj.TotalPrice = shoppingCartItemSubTotalWithDiscount;

                    //display an applied discount amount
                    if (shoppingCartItemDiscountBase > decimal.Zero)
                    {
                        (shoppingCartItemDiscountBase, _) = await _taxService.GetProductPriceAsync(product, shoppingCartItemDiscountBase);
                        if (shoppingCartItemDiscountBase > decimal.Zero)
                        {
                            decimal shoppingCartItemDiscount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(shoppingCartItemDiscountBase, await _workContext.GetWorkingCurrencyAsync());
                            //obj.Discount = await _priceFormatter.FormatPriceAsync(shoppingCartItemDiscount);
                        }
                    }
                }
                ListD.Add(obj);
            }

            return await PassListToExcelByteConverter(ListD);

        }

        public virtual async Task<byte[]> PassListToExcelByteConverter(IList<ExelKeys> orders)
        {
            //a vendor should have access only to part of order information nitu
            var ignore = await _workContext.GetCurrentVendorAsync() != null;

            //Added by Yogesh Kumbhar on Dt: 02-11-2025
            string reportTitle = "Estimate";
            string createdBy = orders.FirstOrDefault()?.CreatedBy ?? "";
            string createdFor = orders.FirstOrDefault()?.CreatedFor ?? "";

            //property array
            var properties = new[]
            {

                new PropertyByName<ExelKeys>("Quantity", p => p.Quantity),
                new PropertyByName<ExelKeys>("Description", p => p.Name),
                new PropertyByName<ExelKeys>("PN", p => p.SKU),
                new PropertyByName<ExelKeys>("Price Each(In $)", p => p.UnitPrice),
                new PropertyByName<ExelKeys>("Total Price(In $)",   P=>P.TotalPrice)
            };

            return await _exportManager.ExportToXlsx(properties, orders, reportTitle, createdBy, createdFor);
        }

        #endregion
    }
}
