using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Tax;
using Nop.Services.Authentication;
using Nop.Services.Authentication.External;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Web.Extensions;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Framework.Security.Honeypot;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Collections.Generic;
using Nop.Services.ExportImport;
using Nop.Core.Domain.Orders;
using Nop.Services.ExportImport.Help;
using Nop.Core;
using Nop.Core.Domain.Localization;
using Nop.Web.Factories;
using Nop.Services.Localization;
using Nop.Services.Estimates;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Media;
using System.Threading.Tasks;
using Nop.Services.Authentication.MultiFactor;
using Nop.Web.Models.Estimate;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Estimates;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading;
using Nop.Core.Domain.Messages;
using Nop.Data;
using DocumentFormat.OpenXml.EMMA;
using System.Text.RegularExpressions;
//using iText.IO.Image;

namespace Nop.Web.Controllers
{
    public class EstimateController : Controller
    {
        // Create an instance of the document class which represents the PDF document itself.  
        Document document = new Document(PageSize.A4, 25, 25, 30, 30);

        #region Fields
        private readonly IWorkContext _workContext;
        private readonly IEstimateModelFactory _EstimateModelFactory;
        private readonly IEstimateService _EstimateService;
        private readonly IStoreContext _storeContext;
        private readonly IProductService _productService;
        private readonly IPermissionService _permissionService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IMultiFactorAuthenticationPluginManager _multiFactorAuthenticationPluginManager;
        private readonly ICustomerService _customerService;
        private IHostingEnvironment _environment;
        //Added by Yogesh Kumbhar on Dt: 01-152025
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        #endregion

        #region Ctor
        public EstimateController(
             IWorkContext workContext,
             IStoreContext storeContext,
             IEstimateModelFactory EstimateModelFactory,
             IEstimateService estimateService,
             IProductService productService,
             IPermissionService permissionService,
             IProductAttributeParser productAttributeParser,
             ICustomerService customerService,
             IHostingEnvironment environment,
             IMultiFactorAuthenticationPluginManager multiFactorAuthenticationPluginManager,
             IRepository<EmailAccount> emailAccountRepository
             )
        {
            _workContext = workContext;
            _EstimateModelFactory = EstimateModelFactory;
            _EstimateService = estimateService;
            _storeContext = storeContext;
            _productService = productService;
            _permissionService = permissionService;
            _productAttributeParser = productAttributeParser;
            _multiFactorAuthenticationPluginManager = multiFactorAuthenticationPluginManager;
            _customerService = customerService;
            _environment = environment;
            _emailAccountRepository = emailAccountRepository;
        }
        #endregion

        #region Methods

        #region Estimate List, Create and Edit methods
        //EstimateList method: Added by Yogesh Kumbhar on Dt: 12-02-2024
        [HttpGet]
        public virtual async Task<IActionResult> EstimateList()
        {
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            bool isCustomer = true;
            if (customer == null || customer.Email == null)
                isCustomer = false;

            var model = new EstimateDetailsModel();
            model = _EstimateModelFactory.PrepareEstimateNavigationModel(model, customer.Id);
            model.UserRole = customer.TitleDiscription;
            model.IsCustomer = isCustomer;

            // Added by Yogesh Kumbhar on Dt: 12-30-2024
            if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin"))
            {
                // Added by Yogesh Kumbhar on Dt: 01-16-2025.
                model.CustomerList = await _customerService.GetCustomerSelectListAsync();
            }

            return View("EstimateNavigationList", model);
        }
        public virtual async Task<IActionResult> AddList()
        {
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            if (customer != null)
                if (customer.Email == null)
                    return RedirectToRoute("Login");

            var model = new EstimateDetailsModel();
            model.CustomerId = customer.Id;
            model.CopyDescriptions = string.Empty;
            model.CopyName = string.Empty;
            //model = _EstimateModelFactory.PrepareAddNewList(model);
            model.UserRole = customer.TitleDiscription;

            // Added by Yogesh Kumbhar on Dt: 12-19-2024 For customer dropdown on Estimate form.
            if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin"))
            {
                model.CustomerList = await _customerService.GetCustomerSelectListAsync();
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddList(EstimateDetailsModel model)
        {
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                return RedirectToRoute("Login");

            if (ModelState.IsValid)
            {
                var cust = new Estimate();
                cust.CustomerId = customer.Id;
                cust.Discription = model.Discription;
                cust.Name = model.Name;
                var store = await _storeContext.GetCurrentStoreAsync();
                cust.StoreId = store.Id;
                cust.EstimateTotal = 0;
                cust.CreatedOnUtc = DateTime.UtcNow;
                cust.CreatedBy = model.CreatedBy == 0 ? (int?)null : model.CreatedBy;
                await _EstimateService.InsertEstimate(cust);
            }
            //return RedirectToRoute("NewEstimate");
            return RedirectToRoute("EstimateList");
        }
        #endregion

        #region Edit Estimate
        public virtual async Task<ActionResult> ShowEstimateShop(int EstimateId, bool isView)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var model = new EstimateDetailsModel();
            model.CopyDescriptions = string.Empty;
            model.CopyName = string.Empty;
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            model = await _EstimateModelFactory.PrepareGetEstimateDetailsById(EstimateId, customer.Id);
            model.IsView = isView;
            model.UserRole = customer.TitleDiscription;

            // Added by Yogesh Kumbhar on Dt: 12-20-2024
            if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin"))
            {
                model.CustomerList = await _customerService.GetCustomerSelectListAsync();
            }
            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> EditEstimateDetails(int CreatedBy, string estimateName, string Discription, int estimateId, string note)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            if (String.IsNullOrEmpty(estimateName) || estimateId == 0)
                return Json(new { success = false, message = "Error: Estimate some details are missing!" });

            try
            {
                Estimate estimate = await _EstimateService.GetEstimateById(estimateId);
                if (estimate != null)
                {
                    estimate.Name = estimateName;
                    estimate.CreatedBy = CreatedBy == 0 ? (int?)null : CreatedBy;
                    estimate.Discription = Discription;
                    estimate.Note = note;

                    await _EstimateService.UpdateEstimate(estimate);
                }

                return Json(new { success = true, message = "Estimate saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion

        #region Add tems To Cart From Estimate
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> AddItemsToCartFromEstimate(EstimateDetailsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            if (model.EstimateId != 0)
            {
                Estimate ListEstimate = await _EstimateService.GetEstimateById(model.EstimateId);
                ListEstimate.IsAddToCart = true;
                ListEstimate.CartAddedDate = DateTime.UtcNow;
                await _EstimateService.UpdateEstimate(ListEstimate);
            }
            Customer customerdetails = await _workContext.GetCurrentCustomerAsync();
            model = await _EstimateModelFactory.PrepareGetEstimateDetailsById(model.EstimateId, customerdetails.Id);

            // Added by Yogesh Kumbhar on Dt: 01-13-2025
            Customer customer = new Customer();
            if (model.CreatedBy != null)
                customer = await _customerService.GetCustomerByIdAsync((int)model.CreatedBy);
            else
                customer = await _customerService.GetCustomerByIdAsync(customerdetails.Id);

            customer.HasShoppingCartItems = true;
            await _customerService.UpdateCustomerAsync(customer);

            //Success message 
            TempData["msg"] = "The product has been added to your shopping cart.";

            // Added by Yogesh Kumbhar on Dt: 01-14-2025 & 01-15-2025
            //Send Email notification to customer add items to their shopping cart
            if (customerdetails.TitleDiscription != null && customerdetails.TitleDiscription.Equals("Admin") && model.CreatedBy != null)
            {
                var fullName = await _customerService.GetCustomerFullNameAsync(customer);
                var ccEmails = await _customerService.GetAllCustomerEmailByFlagsAsync(customer.Id, isEstimate: true);
                var emailAccount = _emailAccountRepository.Table.FirstOrDefault();
                string baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}/cart?isUser=true";

                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.To.Add(customer.Email);
                foreach (var ccEmail in ccEmails)
                {
                    mail.CC.Add(ccEmail);
                }
                mail.From = new MailAddress(emailAccount.Email, emailAccount.DisplayName);
                mail.Subject = "Estimate Items Added to Your Cart";
                mail.Body = $@"<p>Hello {fullName},</p>
                            <p>Estimate items have been added to your cart by FMS Admin.</p>
                            <p>Please check your shopping cart and place the order...!</p><br/>
                            <a href = {baseUrl} target = '_blank' > Click here to open your shopping cart </a>
                            <br/><br/>
                            <p><b>Frank Stevens</b><br/>
                            <b>FMS Solutions, LLC</b><br/>
                            566 Falcon Fork Way<br/>
                            C: 201-264-8365<br/>
                            F: 201-590-1115<br/>
                            <a href='mailto:info@fmsaccessories.com'>info@fmsaccessories.com</a><br/>
                            <a href = 'https://www.fmsaccessories.com' target = '_blank' > https://www.fmsaccessories.com </a>
                            </p>";
                mail.IsBodyHtml = true;
                await _customerService.SendEmailAsyncEvents(mail);
            }

            //return View(model);
            return RedirectToAction("ShowEstimateShop", new { EstimateId = model.EstimateId });

        }
        #endregion

        #region Update Estimate
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> UpdateEstimate(EstimateDetailsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            Estimate MainEstimate = await _EstimateService.GetEstimateById(model.EstimateId);

            if (model.Name != null)
            {
                if (model.Discription != MainEstimate.Discription || model.Name != MainEstimate.Name || model.CreatedBy != MainEstimate.CreatedBy || model.Note != MainEstimate.Note)
                {
                    MainEstimate.Name = model.Name;
                    MainEstimate.Discription = model.Discription;
                    MainEstimate.CreatedBy = model.CreatedBy == null ? (int?)null : model.CreatedBy;
                    MainEstimate.Note = model.Note;
                    await _EstimateService.UpdateEstimate(MainEstimate);
                }
            }
            else
            {
                model.Name = MainEstimate.Name;
            }
            var ListEstimate = _EstimateService.ForUpdateEstimatesItems(model.EstimateId);
            var allIdsToRemove = model.removefromestimate;

            foreach (var sci in ListEstimate)
            {
                bool remove = allIdsToRemove != null ? allIdsToRemove.Contains(sci.Id) : false;
                if (remove)
                    await _EstimateService.DeleteEstimateItem(sci);
                else
                {
                    string[] ProductQuantityArray = model.ProductQuantityArray != null ? model.ProductQuantityArray.Split(',') : null;

                    for (int i = 0; i < ProductQuantityArray.Length; i++)
                    {
                        if (model.EstimateStatusIdArray[i] != null && ProductQuantityArray[i] != null)
                        {
                            await _EstimateService.UpdateEstimateItemUsingFor(Convert.ToInt32(model.EstimateStatusIdArray[i]), Convert.ToInt32(ProductQuantityArray[i]));
                        }
                    }
                }
            }

            Customer customer = await _workContext.GetCurrentCustomerAsync();
            model = await _EstimateModelFactory.PrepareGetEstimateDetailsById(model.EstimateId, customer.Id);
            return RedirectToAction("ShowEstimateShop", new { EstimateId = model.EstimateId });
        }
        #endregion

        #region Add Product To Estimate
        [HttpPost]
        public async Task<ActionResult> AddProductToEstimate(int productId, string EstimateId, IFormCollection form, EstimateDetailsModel model)
        {
            //Customer Details
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                return RedirectToRoute("Login");

            var product = await _productService.GetProductByIdAsync(productId);
            if (EstimateId == "1")
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("NewEstimate"),
                });
            }
            var addToCartWarnings = new List<string>();
            var estimate = _EstimateService.ProductExistOrNot(Convert.ToInt32(EstimateId), productId);
            string attributes = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

            if (estimate.Any(a => a.AttributesXml == attributes))
            {
                return Json(new
                {
                    message = "" + product.Name + "product already exists in same list"
                }); ;
            }
            else if (estimate.Count() > 0 && estimate.Any(a => a.AttributesXml != attributes))
            {
                var estimateitem = new EstimateItem();
                estimateitem.StoreId = _storeContext.GetCurrentStore().Id;
                estimateitem.ProductId = product.Id;
                estimateitem.EstimateId = Convert.ToInt32(EstimateId);
                estimateitem.Quantity = 1;
                estimateitem.AttributesXml = attributes;
                estimateitem.CustomerId = customer.Id;
                estimateitem.CustomerEnteredPrice = 0;
                estimateitem.CreatedOnUtc = DateTime.UtcNow;
                await _EstimateService.InsertEstimateItem(estimateitem);

                //Estimate item
                return Json(new
                {
                    Savemessage = "" + product.Name + " product added successfully to Estimate"
                });
            }
            else if (estimate.Count() == 0)
            {
                var estimateitem = new EstimateItem();
                estimateitem.StoreId = _storeContext.GetCurrentStore().Id;
                estimateitem.ProductId = product.Id;
                estimateitem.EstimateId = Convert.ToInt32(EstimateId);
                estimateitem.Quantity = 1;
                estimateitem.AttributesXml = attributes;
                estimateitem.CustomerId = customer.Id;
                estimateitem.CustomerEnteredPrice = 0;
                estimateitem.CreatedOnUtc = DateTime.UtcNow;
                await _EstimateService.InsertEstimateItem(estimateitem);

                //Estimate item
                return Json(new
                {
                    Savemessage = "" + product.Name + " product added successfully to Estimate"
                });
            }
            else
            {
                return Json(new
                {
                    message = "" + estimate.FirstOrDefault().Product.Name + "product already exists in same list"
                });
            }
        }
        #endregion

        #region Delete Estimate Items from Estimate
        //Added by Yogesh kumbhar on Dt: 03-14-2025
        /// <summary>
        /// Delete Estimate Item List
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> DeleteEstimateItems([FromBody] List<int> SelectedItems)
        {
            if (SelectedItems == null || !SelectedItems.Any())
            {
                return Json(new { success = false, message = "No items selected!" });
            }

            try
            {
                foreach (var itemId in SelectedItems)
                {
                    var item = await _EstimateService.GetEstimateItemById(itemId);
                    await _EstimateService.DeleteEstimateItem(item);
                }

                return Json(new { success = true, message = "Selected items deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        #endregion

        /// <summary>
        /// Delete Estimate List
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> DeleteEstimateList(EstimateDetailsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");
            if (model.EstimateId != 0)
            {
                var ListEstimate = await _EstimateService.GetEstimateById(model.EstimateId);
                ListEstimate.Delete = true;
                await _EstimateService.DeleteEstimate(ListEstimate);
            }
            return RedirectToRoute("EstimateList");
        }

        /// <summary>
        /// Delete Estimate
        /// </summary>
        /// <param name="estimateId"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> DeleteEstimate(int estimateId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");
            if (estimateId != 0)
            {
                var ListEstimate = await _EstimateService.GetEstimateById(estimateId);
                ListEstimate.Delete = true;
                await _EstimateService.DeleteEstimate(ListEstimate);
            }
            return RedirectToRoute("EstimateList");
        }

        /// <summary>
        /// Copy Estimate
        /// </summary>
        /// <param name="EstimateId"></param>
        /// <param name="Name"></param>
        /// <param name="Description"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> CopyEstimate(int EstimateId = 0, string Name = "", string
            Description = "", int? CustomerId = null)
        {
            Customer customer = await _workContext.GetCurrentCustomerAsync();
            if (customer == null)
                return RedirectToRoute("Login");

            if (EstimateId != 0 && Name != "")
            {
                var cust = new Estimate();
                cust.CustomerId = customer.Id;
                cust.Discription = Description;
                cust.Name = Name;
                var store = await _storeContext.GetCurrentStoreAsync();
                cust.StoreId = store.Id;
                cust.EstimateTotal = 0;
                cust.CopyEstimateId = EstimateId;
                cust.CreatedOnUtc = DateTime.UtcNow;

                //Added by Yogesh Kumbhar on Dt: 12-30-2024
                cust.CreatedBy = CustomerId == 0 ? (int?)null : CustomerId;

                await _EstimateService.InsertEstimate(cust);
            }
            else
            {
                return Json(new
                {
                    ErrorMsg = "Enter Values"
                });
            }
            return Json(new
            {
                // RedirectToRoute("goestimate", new { EstimateId = CopyEstimateId }),
                redirect = Url.RouteUrl("goestimate", new { EstimateId = 2 }),
            });
        }

        /// <summary>
        /// Export Excel All
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<ActionResult> ExportExcelAll(EstimateDetailsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            try
            {
                byte[] bytes = await _EstimateModelFactory.ExportExcelFile(model.EstimateId);
                return File(bytes, MimeTypes.TextXlsx, "Estimate.xlsx");
            }
            catch (Exception exc)
            {
                //ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        //Added by Yogesh Kumbhar on Dt: 02-12-2025
        public class PageEventHelper : PdfPageEventHelper
        {
            public override void OnStartPage(PdfWriter writer, Document document)
            {
                document.SetMargins(36f, 36f, 36f, 36f);
                base.OnStartPage(writer, document);
            }
        }

        /// <summary>
        /// Export To PDF
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> ExportToPDF(EstimateDetailsModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            try
            {
                Font TitleFont = FontFactory.GetFont("Arial", 22);
                Font SubTitleFont = FontFactory.GetFont("Arial", 12, Font.BOLD);
                Font SubTitleContaints = FontFactory.GetFont("Arial", 12);

                Customer customer = await _workContext.GetCurrentCustomerAsync();

                //Customer Addres
                var CustomerAddresses = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);
                string CustomerAddress = "";
                string CustomerName = "";

                if (CustomerAddresses != null)
                    CustomerAddress = CustomerAddresses.FirstOrDefault().Address1;

                if (CustomerName != null)
                    CustomerName = CustomerAddresses.FirstOrDefault().FirstName + " " + CustomerAddresses.FirstOrDefault().LastName;

                model = await _EstimateModelFactory.PrepareGetEstimateDetailsById(model.EstimateId, customer.Id);

                var webRoot = _environment.WebRootPath;
                string FileName = "EstimateList.pdf";
                var file = System.IO.Path.Combine(webRoot, FileName);
                MemoryStream workStream = new MemoryStream();

                // Set page size and margins (0.5 inch = 36 points)
                Document document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);

                PdfWriter writer = PdfWriter.GetInstance(document, workStream);
                writer.PageEvent = new PageEventHelper();

                document.Open();

                // Title
                Paragraph Title = new Paragraph("Estimate", TitleFont);
                Title.Alignment = Element.ALIGN_CENTER;
                Title.SpacingAfter = 0f;
                document.Add(Title);

                // Add horizontal line
                PdfPTable lineTable = new PdfPTable(1);
                lineTable.WidthPercentage = 100;
                PdfPCell lineCell = new PdfPCell(new Phrase(""));
                lineCell.BorderWidthBottom = 0.5f;
                lineCell.BorderWidthTop = 0f;
                lineCell.BorderWidthLeft = 0f;
                lineCell.BorderWidthRight = 0f;
                lineCell.PaddingTop = 5f;
                lineCell.PaddingBottom = 15f;
                lineTable.AddCell(lineCell);
                document.Add(lineTable);

                // Basic Information
                Paragraph basicInfo = new Paragraph();
                basicInfo.Add(new Chunk("Date: ", SubTitleFont));
                basicInfo.Add(new Chunk(DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss tt") + "\n", SubTitleContaints));
                basicInfo.Add(new Chunk("Estimate Name: ", SubTitleFont));
                basicInfo.Add(new Chunk(model.Name + "\n", SubTitleContaints));
                basicInfo.Add(new Chunk("From: ", SubTitleFont));
                basicInfo.Add(new Chunk("FMS Accessories.com\n\n", SubTitleContaints));
                document.Add(basicInfo);

                // Created By and Created For section
                if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin") && model.CreatedBy != null)
                {
                    var customerDetail = await _customerService.GetCustomerEmailWithCompanyAsync((int)model.CreatedBy);

                    PdfPTable createdTable = new PdfPTable(4); // Changed to 4 columns for better spacing
                    createdTable.WidthPercentage = 100;
                    createdTable.SetWidths(new float[] { 1f, 1f, 0.5f, 1.5f }); // Adjusted column widths

                    // Created By (spans 2 columns)
                    PdfPCell leftCell = new PdfPCell();
                    leftCell.Border = Rectangle.NO_BORDER;
                    leftCell.Colspan = 2;
                    Paragraph createdBy = new Paragraph();
                    createdBy.Add(new Chunk("Created By:\n", SubTitleFont));
                    createdBy.Add(new Chunk("FMS Solutions, LLC\n566 Falcon Fork Way\nSaint Johns,FL 32259\nC: 201-264-8365", SubTitleContaints));
                    createdBy.Alignment = Element.ALIGN_JUSTIFIED;
                    leftCell.AddElement(createdBy);
                    createdTable.AddCell(leftCell);

                    // Empty cell for spacing
                    PdfPCell spacerCell = new PdfPCell();
                    spacerCell.Border = Rectangle.NO_BORDER;
                    createdTable.AddCell(spacerCell);

                    // Created For
                    PdfPCell rightCell = new PdfPCell();
                    rightCell.Border = Rectangle.NO_BORDER;
                    Paragraph createdFor = new Paragraph();
                    createdFor.Add(new Chunk("Created For:\n", SubTitleFont));
                    createdFor.Add(new Chunk(customerDetail, SubTitleContaints));
                    createdFor.Alignment = Element.ALIGN_JUSTIFIED;
                    rightCell.AddElement(createdFor);
                    createdTable.AddCell(rightCell);

                    document.Add(createdTable);
                    document.Add(new Paragraph("\n"));
                }

                // Display product table
                document = DisplayList(document, model);

                if (customer.TitleDiscription != null && customer.TitleDiscription.Equals("Admin"))
                {
                    //Estimate note
                    document.Add(new Paragraph("\n"));
                    Paragraph estimateNote = new Paragraph();
                    estimateNote.Add(new Chunk("Note: ", SubTitleFont));
                    estimateNote.Add(new Chunk(model.Note, SubTitleContaints));
                    document.Add(estimateNote);
                }

                document.Close();
                writer.Close();

                byte[] byteInfo = workStream.ToArray();
                workStream.Write(byteInfo, 0, byteInfo.Length);
                workStream.Position = 0;

                return File(byteInfo, "application/pdf", FileName);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Display List in PDF
        /// </summary>
        /// <param name="document"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private Document DisplayList(Document document, EstimateDetailsModel model)
        {
            Font TitleFont = FontFactory.GetFont("Arial", 11, Font.BOLD);
            Font ContentFont = FontFactory.GetFont("Arial", 11);

            // Create Table
            PdfPTable table = new PdfPTable(6);
            table.WidthPercentage = 100;
            float[] widths = new float[] { 16f, 23f, 29f, 12f, 8f, 12f };
            table.SetWidths(widths);

            // Header style
            PdfPCell headerCell = new PdfPCell();
            headerCell.BackgroundColor = BaseColor.LightGray;
            headerCell.Padding = 5f;
            headerCell.BorderWidth = 0.5f;
            headerCell.HorizontalAlignment = Element.ALIGN_LEFT;
            headerCell.VerticalAlignment = Element.ALIGN_MIDDLE;

            // Add headers
            string[] headers = { "SKU", "Image", "Product(s)", "Price", "Qty.", "Total" };
            foreach (string header in headers)
            {
                headerCell.Phrase = new Phrase(header, TitleFont);
                if (header == "Total")
                    headerCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(headerCell);
            }

            // Content style
            PdfPCell contentCell = new PdfPCell();
            contentCell.BorderWidth = 0.5f;
            contentCell.Padding = 5f;
            contentCell.VerticalAlignment = Element.ALIGN_MIDDLE;

            foreach (var item in model.Items)
            {
                // SKU
                contentCell.Phrase = new Phrase(item.Sku, ContentFont);
                contentCell.HorizontalAlignment = Element.ALIGN_LEFT;
                table.AddCell(contentCell);

                // Image
                PdfPCell imageCell = new PdfPCell();
                imageCell.BorderWidth = 0.5f;
                imageCell.Padding = 5f;
                iTextSharp.text.Image productimage = iTextSharp.text.Image.GetInstance(item.Picture.ImageUrl);
                productimage.ScaleToFit(50f, 50f);
                imageCell.AddElement(productimage);
                imageCell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(imageCell);

                // Product Details
                contentCell.Phrase = new Phrase(
                    item.Name +
                    (string.IsNullOrEmpty(item.AttributeDescription)
                        ? ""
                        : "\n" + Regex.Replace(item.AttributeDescription, "<br\\s*/?>", "\n")),
                    ContentFont
                );
                contentCell.HorizontalAlignment = Element.ALIGN_LEFT;
                table.AddCell(contentCell);

                // Price
                contentCell.Phrase = new Phrase(item.UnitPrice, ContentFont);
                contentCell.HorizontalAlignment = Element.ALIGN_LEFT;
                table.AddCell(contentCell);

                // Quantity
                contentCell.Phrase = new Phrase(item.Quantity.ToString(), ContentFont);
                contentCell.HorizontalAlignment = Element.ALIGN_LEFT;
                table.AddCell(contentCell);

                // Total
                contentCell.Phrase = new Phrase(item.SubTotal, ContentFont);
                contentCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(contentCell);
            }

            // Total Amount
            PdfPCell totalCell = new PdfPCell(new Phrase("Total Amount : $" + model.Subtot.ToString(), ContentFont));
            totalCell.Colspan = 6;
            totalCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            totalCell.Padding = 5f;
            totalCell.BorderWidth = 0.5f;
            table.AddCell(totalCell);

            document.Add(table);
            return document;
        }
        #endregion

    }
}
