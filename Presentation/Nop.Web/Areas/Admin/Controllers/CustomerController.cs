using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Gdpr;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Tax;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.ExportImport;
using Nop.Services.Forums;
using Nop.Services.Gdpr;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Directory;
using Microsoft.AspNetCore.Authorization;

namespace Nop.Web.Areas.Admin.Controllers
{
    public partial class CustomerController : BaseAdminController
    {
        #region Fields

        private readonly CustomerSettings _customerSettings;
        private readonly DateTimeSettings _dateTimeSettings;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ForumSettings _forumSettings;
        private readonly GdprSettings _gdprSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressService _addressService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly ICustomerModelFactory _customerModelFactory;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IExportManager _exportManager;
        private readonly IForumService _forumService;
        private readonly IGdprService _gdprService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly TaxSettings _taxSettings;
        private readonly ITaxJarService _taxJarService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ILogger _logger;
        #endregion

        #region Ctor

        public CustomerController(
            CustomerSettings customerSettings,
            DateTimeSettings dateTimeSettings,
            EmailAccountSettings emailAccountSettings,
            ForumSettings forumSettings,
            GdprSettings gdprSettings,
            IAddressAttributeParser addressAttributeParser,
            IAddressService addressService,
            ICustomerActivityService customerActivityService,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            ICustomerModelFactory customerModelFactory,
            ICustomerRegistrationService customerRegistrationService,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            IEmailAccountService emailAccountService,
            IEventPublisher eventPublisher,
            IExportManager exportManager,
            IForumService forumService,
            IGdprService gdprService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            INotificationService notificationService,
            IPermissionService permissionService,
            IQueuedEmailService queuedEmailService,
            IRewardPointService rewardPointService,
            IStoreContext storeContext,
            IStoreService storeService,
            ITaxService taxService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            TaxSettings taxSettings,
            ITaxJarService taxJarService,
            IStateProvinceService stateProvinceService,
            ICountryService countryService,
            ILogger logger)
        {
            _customerSettings = customerSettings;
            _dateTimeSettings = dateTimeSettings;
            _emailAccountSettings = emailAccountSettings;
            _forumSettings = forumSettings;
            _gdprSettings = gdprSettings;
            _addressAttributeParser = addressAttributeParser;
            _addressService = addressService;
            _customerActivityService = customerActivityService;
            _customerAttributeParser = customerAttributeParser;
            _customerAttributeService = customerAttributeService;
            _customerModelFactory = customerModelFactory;
            _customerRegistrationService = customerRegistrationService;
            _customerService = customerService;
            _dateTimeHelper = dateTimeHelper;
            _emailAccountService = emailAccountService;
            _eventPublisher = eventPublisher;
            _exportManager = exportManager;
            _forumService = forumService;
            _gdprService = gdprService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _queuedEmailService = queuedEmailService;
            _rewardPointService = rewardPointService;
            _storeContext = storeContext;
            _storeService = storeService;
            _taxService = taxService;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _taxSettings = taxSettings;
            _taxJarService = taxJarService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _logger = logger;
        }

        #endregion

        #region Utilities

        protected virtual async Task<string> ValidateCustomerRolesAsync(IList<CustomerRole> customerRoles, IList<CustomerRole> existingCustomerRoles)
        {
            if (customerRoles == null)
                throw new ArgumentNullException(nameof(customerRoles));

            if (existingCustomerRoles == null)
                throw new ArgumentNullException(nameof(existingCustomerRoles));

            //check ACL permission to manage customer roles
            var rolesToAdd = customerRoles.Except(existingCustomerRoles, new CustomerRoleComparerByName());
            var rolesToDelete = existingCustomerRoles.Except(customerRoles, new CustomerRoleComparerByName());
            if (rolesToAdd.Any(role => role.SystemName != NopCustomerDefaults.RegisteredRoleName) || rolesToDelete.Any())
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageAcl))
                    return await _localizationService.GetResourceAsync("Admin.Customers.Customers.CustomerRolesManagingError");
            }

            //ensure a customer is not added to both 'Guests' and 'Registered' customer roles
            //ensure that a customer is in at least one required role ('Guests' and 'Registered')
            var isInGuestsRole = customerRoles.FirstOrDefault(cr => cr.SystemName == NopCustomerDefaults.GuestsRoleName) != null;
            var isInRegisteredRole = customerRoles.FirstOrDefault(cr => cr.SystemName == NopCustomerDefaults.RegisteredRoleName) != null;
            if (isInGuestsRole && isInRegisteredRole)
                return await _localizationService.GetResourceAsync("Admin.Customers.Customers.GuestsAndRegisteredRolesError");
            if (!isInGuestsRole && !isInRegisteredRole)
                return await _localizationService.GetResourceAsync("Admin.Customers.Customers.AddCustomerToGuestsOrRegisteredRoleError");

            //no errors
            return string.Empty;
        }

        protected virtual async Task<string> ParseCustomCustomerAttributesAsync(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var attributesXml = string.Empty;
            var customerAttributes = await _customerAttributeService.GetAllCustomerAttributesAsync();
            foreach (var attribute in customerAttributes)
            {
                var controlId = $"{NopCustomerServicesDefaults.CustomerAttributePrefix}{attribute.Id}";
                StringValues ctrlAttributes;

                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        ctrlAttributes = form[controlId];
                        if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                        {
                            var selectedAttributeId = int.Parse(ctrlAttributes);
                            if (selectedAttributeId > 0)
                                attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                        }

                        break;
                    case AttributeControlType.Checkboxes:
                        var cblAttributes = form[controlId];
                        if (!StringValues.IsNullOrEmpty(cblAttributes))
                        {
                            foreach (var item in cblAttributes.ToString()
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var selectedAttributeId = int.Parse(item);
                                if (selectedAttributeId > 0)
                                    attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }

                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        //load read-only (already server-side selected) values
                        var attributeValues = await _customerAttributeService.GetCustomerAttributeValuesAsync(attribute.Id);
                        foreach (var selectedAttributeId in attributeValues
                            .Where(v => v.IsPreSelected)
                            .Select(v => v.Id)
                            .ToList())
                        {
                            attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                attribute, selectedAttributeId.ToString());
                        }

                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        ctrlAttributes = form[controlId];
                        if (!StringValues.IsNullOrEmpty(ctrlAttributes))
                        {
                            var enteredText = ctrlAttributes.ToString().Trim();
                            attributesXml = _customerAttributeParser.AddCustomerAttribute(attributesXml,
                                attribute, enteredText);
                        }

                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.FileUpload:
                    //not supported customer attributes
                    default:
                        break;
                }
            }

            return attributesXml;
        }

        private async Task<bool> SecondAdminAccountExistsAsync(Customer customer)
        {
            var customers = await _customerService.GetAllCustomersAsync(customerRoleIds: new[] { (await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.AdministratorsRoleName)).Id });

            return customers.Any(c => c.Active && c.Id != customer.Id);
        }

        #endregion

        #region Customers

        public virtual IActionResult Index()
        {
            return RedirectToAction("List");
        }

        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerSearchModelAsync(new CustomerSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CustomerList(CustomerSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerListModelAsync(searchModel);

            return Json(model);
        }

        public virtual async Task<IActionResult> Create()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerModelAsync(new CustomerModel(), null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public virtual async Task<IActionResult> Create(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.GetCustomerByEmailAsync(model.Email) != null)
                ModelState.AddModelError(string.Empty, "Email is already registered");

            if (!string.IsNullOrWhiteSpace(model.Username) && _customerSettings.UsernamesEnabled &&
                await _customerService.GetCustomerByUsernameAsync(model.Username) != null)
            {
                ModelState.AddModelError(string.Empty, "Username is already registered");
            }

            //validate customer roles
            var allCustomerRoles = await _customerService.GetAllCustomerRolesAsync(true);
            var newCustomerRoles = new List<CustomerRole>();
            foreach (var customerRole in allCustomerRoles)
                if (model.SelectedCustomerRoleIds.Contains(customerRole.Id))
                    newCustomerRoles.Add(customerRole);
            var customerRolesError = await ValidateCustomerRolesAsync(newCustomerRoles, new List<CustomerRole>());
            if (!string.IsNullOrEmpty(customerRolesError))
            {
                ModelState.AddModelError(string.Empty, customerRolesError);
                _notificationService.ErrorNotification(customerRolesError);
            }

            // Ensure that valid email address is entered if Registered role is checked to avoid registered customers with empty email address
            if (newCustomerRoles.Any() && newCustomerRoles.FirstOrDefault(c => c.SystemName == NopCustomerDefaults.RegisteredRoleName) != null &&
                !CommonHelper.IsValidEmail(model.Email))
            {
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.Customers.Customers.ValidEmailRequiredRegisteredRole"));

                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.ValidEmailRequiredRegisteredRole"));
            }

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributesAsync(form);
            if (newCustomerRoles.Any() && newCustomerRoles.FirstOrDefault(c => c.SystemName == NopCustomerDefaults.RegisteredRoleName) != null)
            {
                var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
                foreach (var error in customerAttributeWarnings)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            if (ModelState.IsValid)
            {
                //fill entity from model
                var customer = model.ToEntity<Customer>();
                var currentStore = await _storeContext.GetCurrentStoreAsync();

                customer.CustomerGuid = Guid.NewGuid();
                customer.CreatedOnUtc = DateTime.UtcNow;
                customer.LastActivityDateUtc = DateTime.UtcNow;

                //Pay later
                customer.IsOpenPayment = model.IsOpenPayment;
                customer.RegisteredInStoreId = currentStore.Id;

                await _customerService.InsertCustomerAsync(customer);

                //form fields
                if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.TimeZoneIdAttribute, model.TimeZoneId);
                if (_customerSettings.GenderEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute, model.Gender);
                if (_customerSettings.FirstNameEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                if (_customerSettings.LastNameEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                if (_customerSettings.DateOfBirthEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute, model.DateOfBirth);
                if (_customerSettings.CompanyEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute, model.Company);
                if (_customerSettings.StreetAddressEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute, model.StreetAddress);
                if (_customerSettings.StreetAddress2Enabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute, model.StreetAddress2);
                if (_customerSettings.ZipPostalCodeEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute, model.ZipPostalCode);
                if (_customerSettings.CityEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                if (_customerSettings.CountyEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                if (_customerSettings.CountryEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute, model.CountryId);
                if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute, model.StateProvinceId);
                if (_customerSettings.PhoneEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                if (_customerSettings.FaxEnabled)
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                //custom customer attributes
                await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                //newsletter subscriptions
                if (!string.IsNullOrEmpty(customer.Email))
                {
                    var allStores = await _storeService.GetAllStoresAsync();
                    foreach (var store in allStores)
                    {
                        var newsletterSubscription = await _newsLetterSubscriptionService
                            .GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
                        if (model.SelectedNewsletterSubscriptionStoreIds != null &&
                            model.SelectedNewsletterSubscriptionStoreIds.Contains(store.Id))
                        {
                            //subscribed
                            if (newsletterSubscription == null)
                            {
                                await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                                {
                                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                    Email = customer.Email,
                                    Active = true,
                                    StoreId = store.Id,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                            }
                        }
                        else
                        {
                            //not subscribed
                            if (newsletterSubscription != null)
                            {
                                await _newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(newsletterSubscription);
                            }
                        }
                    }
                }

                //password
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var changePassRequest = new ChangePasswordRequest(model.Email, false, _customerSettings.DefaultPasswordFormat, model.Password);
                    var changePassResult = await _customerRegistrationService.ChangePasswordAsync(changePassRequest);
                    if (!changePassResult.Success)
                    {
                        foreach (var changePassError in changePassResult.Errors)
                            _notificationService.ErrorNotification(changePassError);
                    }
                }

                //customer roles
                foreach (var customerRole in newCustomerRoles)
                {
                    //ensure that the current customer cannot add to "Administrators" system role if he's not an admin himself
                    if (customerRole.SystemName == NopCustomerDefaults.AdministratorsRoleName && !await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()))
                        continue;

                    await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = customerRole.Id });
                }

                await _customerService.UpdateCustomerAsync(customer);

                //ensure that a customer with a vendor associated is not in "Administrators" role
                //otherwise, he won't have access to other functionality in admin area
                if (await _customerService.IsAdminAsync(customer) && customer.VendorId > 0)
                {
                    customer.VendorId = 0;
                    await _customerService.UpdateCustomerAsync(customer);

                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminCouldNotbeVendor"));
                }

                //ensure that a customer in the Vendors role has a vendor account associated.
                //otherwise, he will have access to ALL products
                if (await _customerService.IsVendorAsync(customer) && customer.VendorId == 0)
                {
                    var vendorRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.VendorsRoleName);
                    await _customerService.RemoveCustomerRoleMappingAsync(customer, vendorRole);

                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.CannotBeInVendoRoleWithoutVendorAssociated"));
                }

                //activity log
                await _customerActivityService.InsertActivityAsync("AddNewCustomer",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.AddNewCustomer"), customer.Id), customer);
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Added"));

                if (!continueEditing)
                    return RedirectToAction("List");

                return RedirectToAction("Edit", new { id = customer.Id });
            }

            //prepare model
            model = await _customerModelFactory.PrepareCustomerModelAsync(model, null, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual async Task<IActionResult> Edit(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null || customer.Deleted)
                return RedirectToAction("List");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerModelAsync(null, customer);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public virtual async Task<IActionResult> Edit(CustomerModel model, bool continueEditing, IFormCollection form, IFormFile formFile)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null || customer.Deleted)
                return RedirectToAction("List");

            //validate customer roles
            var allCustomerRoles = await _customerService.GetAllCustomerRolesAsync(true);
            var newCustomerRoles = new List<CustomerRole>();
            foreach (var customerRole in allCustomerRoles)
                if (model.SelectedCustomerRoleIds.Contains(customerRole.Id))
                    newCustomerRoles.Add(customerRole);

            var customerRolesError = await ValidateCustomerRolesAsync(newCustomerRoles, await _customerService.GetCustomerRolesAsync(customer));

            if (!string.IsNullOrEmpty(customerRolesError))
            {
                ModelState.AddModelError(string.Empty, customerRolesError);
                _notificationService.ErrorNotification(customerRolesError);
            }

            // Ensure that valid email address is entered if Registered role is checked to avoid registered customers with empty email address
            if (newCustomerRoles.Any() && newCustomerRoles.FirstOrDefault(c => c.SystemName == NopCustomerDefaults.RegisteredRoleName) != null &&
                !CommonHelper.IsValidEmail(model.Email))
            {
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Admin.Customers.Customers.ValidEmailRequiredRegisteredRole"));
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.ValidEmailRequiredRegisteredRole"));
            }

            //custom customer attributes
            var customerAttributesXml = await ParseCustomCustomerAttributesAsync(form);
            if (newCustomerRoles.Any() && newCustomerRoles.FirstOrDefault(c => c.SystemName == NopCustomerDefaults.RegisteredRoleName) != null)
            {
                var customerAttributeWarnings = await _customerAttributeParser.GetAttributeWarningsAsync(customerAttributesXml);
                foreach (var error in customerAttributeWarnings)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            if (model.CustomerNumber > 0)
            {
                var DealerCodeData = _customerService.GetDelearCodeExistCount(model.CustomerNumber, model.Id);
                if (DealerCodeData.Count() > 0)
                {
                    _notificationService.ErrorNotification("Duplicate BMW Dealer Number.");
                    return RedirectToAction("Edit", new { id = model.Id });
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    customer.CustomerNumber = model.CustomerNumber;
                    customer.AdminComment = model.AdminComment;
                    customer.IsTaxExempt = model.IsTaxExempt;
                    customer.TaxFileExpDate = model.TaxFileExpDate;
                    //Mohit 10/22/2024 Added Exempt Type.
                    customer.TaxExemptType = model.TaxExemptType;

                    //Added by Yogesh Kumbhar.
                    if (!String.IsNullOrEmpty(customer.QBOID))
                        customer.IsRecentlyModified = true;

                    //Pay later
                    customer.IsOpenPayment = model.IsOpenPayment;

                    //Upload tax file
                    string path = Path.Combine("wwwroot/TaxFile_Upload");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    List<string> uploadedFiles = new List<string>();

                    if (formFile != null)
                    {
                        IFormFile postedFile = formFile;
                        string DocumentName = Path.GetFileName(Guid.NewGuid().ToString().Replace('-', '_') + ".pdf");
                        using (FileStream stream = new FileStream(Path.Combine(path, DocumentName), FileMode.Create))
                        {
                            postedFile.CopyTo(stream);
                            uploadedFiles.Add(DocumentName);

                            //Upload tax file name
                            customer.TaxFilePath = DocumentName;
                            customer.TaxFileUploadedDate = DateTime.Now;
                        }
                    }

                    //prevent deactivation of the last active administrator
                    if (!await _customerService.IsAdminAsync(customer) || model.Active || await SecondAdminAccountExistsAsync(customer))
                        customer.Active = model.Active;
                    else
                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminAccountShouldExists.Deactivate"));

                    //email
                    if (!string.IsNullOrWhiteSpace(model.Email))
                        await _customerRegistrationService.SetEmailAsync(customer, model.Email, false);
                    else
                        customer.Email = model.Email;

                    //username
                    if (_customerSettings.UsernamesEnabled)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Username))
                            await _customerRegistrationService.SetUsernameAsync(customer, model.Username);
                        else
                            customer.Username = model.Username;
                    }

                    //VAT number
                    if (_taxSettings.EuVatEnabled)
                    {
                        var prevVatNumber = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.VatNumberAttribute);

                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.VatNumberAttribute, model.VatNumber);
                        //set VAT number status
                        if (!string.IsNullOrEmpty(model.VatNumber))
                        {
                            if (!model.VatNumber.Equals(prevVatNumber, StringComparison.InvariantCultureIgnoreCase))
                            {
                                await _genericAttributeService.SaveAttributeAsync(customer,
                                    NopCustomerDefaults.VatNumberStatusIdAttribute,
                                    (int)(await _taxService.GetVatNumberStatusAsync(model.VatNumber)).vatNumberStatus);
                            }
                        }
                        else
                        {
                            await _genericAttributeService.SaveAttributeAsync(customer,
                                NopCustomerDefaults.VatNumberStatusIdAttribute,
                                (int)VatNumberStatus.Empty);
                        }
                    }

                    //vendor
                    customer.VendorId = model.VendorId;

                    //form fields
                    if (_dateTimeSettings.AllowCustomersToSetTimeZone)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.TimeZoneIdAttribute, model.TimeZoneId);
                    if (_customerSettings.GenderEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.GenderAttribute, model.Gender);
                    if (_customerSettings.FirstNameEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FirstNameAttribute, model.FirstName);
                    if (_customerSettings.LastNameEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.LastNameAttribute, model.LastName);
                    if (_customerSettings.DateOfBirthEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.DateOfBirthAttribute, model.DateOfBirth);
                    if (_customerSettings.CompanyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CompanyAttribute, model.Company);
                    if (_customerSettings.StreetAddressEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddressAttribute, model.StreetAddress);
                    if (_customerSettings.StreetAddress2Enabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StreetAddress2Attribute, model.StreetAddress2);
                    if (_customerSettings.ZipPostalCodeEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.ZipPostalCodeAttribute, model.ZipPostalCode);
                    if (_customerSettings.CityEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CityAttribute, model.City);
                    if (_customerSettings.CountyEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountyAttribute, model.County);
                    if (_customerSettings.CountryEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CountryIdAttribute, model.CountryId);
                    if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.StateProvinceIdAttribute, model.StateProvinceId);
                    if (_customerSettings.PhoneEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PhoneAttribute, model.Phone);
                    if (_customerSettings.FaxEnabled)
                        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.FaxAttribute, model.Fax);

                    //custom customer attributes
                    await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.CustomCustomerAttributes, customerAttributesXml);

                    //newsletter subscriptions
                    if (!string.IsNullOrEmpty(customer.Email))
                    {
                        var allStores = await _storeService.GetAllStoresAsync();
                        foreach (var store in allStores)
                        {
                            var newsletterSubscription = await _newsLetterSubscriptionService
                                .GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
                            if (model.SelectedNewsletterSubscriptionStoreIds != null &&
                                model.SelectedNewsletterSubscriptionStoreIds.Contains(store.Id))
                            {
                                //subscribed
                                if (newsletterSubscription == null)
                                {
                                    await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
                                    {
                                        NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                        Email = customer.Email,
                                        Active = true,
                                        StoreId = store.Id,
                                        CreatedOnUtc = DateTime.UtcNow
                                    });
                                }
                            }
                            else
                            {
                                //not subscribed
                                if (newsletterSubscription != null)
                                {
                                    await _newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(newsletterSubscription);
                                }
                            }
                        }
                    }

                    var currentCustomerRoleIds = await _customerService.GetCustomerRoleIdsAsync(customer, true);

                    //customer roles
                    foreach (var customerRole in allCustomerRoles)
                    {
                        //ensure that the current customer cannot add/remove to/from "Administrators" system role
                        //if he's not an admin himself
                        if (customerRole.SystemName == NopCustomerDefaults.AdministratorsRoleName &&
                            !await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()))
                            continue;

                        if (model.SelectedCustomerRoleIds.Contains(customerRole.Id))
                        {
                            //new role
                            if (currentCustomerRoleIds.All(roleId => roleId != customerRole.Id))
                                await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = customerRole.Id });
                        }
                        else
                        {
                            //prevent attempts to delete the administrator role from the user, if the user is the last active administrator
                            if (customerRole.SystemName == NopCustomerDefaults.AdministratorsRoleName && !await SecondAdminAccountExistsAsync(customer))
                            {
                                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminAccountShouldExists.DeleteRole"));
                                continue;
                            }

                            //remove role
                            if (currentCustomerRoleIds.Any(roleId => roleId == customerRole.Id))
                                await _customerService.RemoveCustomerRoleMappingAsync(customer, customerRole);
                        }
                    }

                    if (model.SelectedCustomerRoleIds.Contains(6) && customer.CustomerAccType != 6)
                        customer.CustomerAccType = 6;
                    else
                    {
                        var RoleList = model.SelectedCustomerRoleIds;
                        if (RoleList.Count > 1)
                        {
                            //Remove Registered
                            if (RoleList.Contains(3))
                                RoleList.Remove(3);
                            //Remove Guest.
                            if (RoleList.Contains(4))
                                RoleList.Remove(4);
                        }
                        customer.CustomerAccType = RoleList.Any() ? RoleList[0] : customer.CustomerAccType;
                    }

                    //Update Customer
                    await _customerService.UpdateCustomerAsync(customer);

                    //ensure that a customer with a vendor associated is not in "Administrators" role
                    //otherwise, he won't have access to the other functionality in admin area
                    if (await _customerService.IsAdminAsync(customer) && customer.VendorId > 0)
                    {
                        customer.VendorId = 0;
                        await _customerService.UpdateCustomerAsync(customer);
                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminCouldNotbeVendor"));
                    }

                    //ensure that a customer in the Vendors role has a vendor account associated.
                    //otherwise, he will have access to ALL products
                    if (await _customerService.IsVendorAsync(customer) && customer.VendorId == 0)
                    {
                        var vendorRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.VendorsRoleName);
                        await _customerService.RemoveCustomerRoleMappingAsync(customer, vendorRole);

                        _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.CannotBeInVendoRoleWithoutVendorAssociated"));
                    }

                    //activity log
                    await _customerActivityService.InsertActivityAsync("EditCustomer",
                        string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditCustomer"), customer.Id), customer);

                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Updated"));

                    if (!continueEditing)
                        return RedirectToAction("List");

                    return RedirectToAction("Edit", new { id = customer.Id });
                }
                catch (Exception exc)
                {
                    _notificationService.ErrorNotification(exc.Message);
                }
            }

            //prepare model
            model = await _customerModelFactory.PrepareCustomerModelAsync(model, customer, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("changepassword")]
        public virtual async Task<IActionResult> ChangePassword(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            //ensure that the current customer cannot change passwords of "Administrators" if he's not an admin himself
            if (await _customerService.IsAdminAsync(customer) && !await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()))
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.OnlyAdminCanChangePassword"));
                return RedirectToAction("Edit", new { id = customer.Id });
            }

            if (!ModelState.IsValid)
                return RedirectToAction("Edit", new { id = customer.Id });

            var changePassRequest = new ChangePasswordRequest(model.Email,
                false, _customerSettings.DefaultPasswordFormat, model.Password);
            var changePassResult = await _customerRegistrationService.ChangePasswordAsync(changePassRequest);
            if (changePassResult.Success)
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.PasswordChanged"));
            else
                foreach (var error in changePassResult.Errors)
                    _notificationService.ErrorNotification(error);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("markVatNumberAsValid")]
        public virtual async Task<IActionResult> MarkVatNumberAsValid(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            await _genericAttributeService.SaveAttributeAsync(customer,
                NopCustomerDefaults.VatNumberStatusIdAttribute,
                (int)VatNumberStatus.Valid);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("markVatNumberAsInvalid")]
        public virtual async Task<IActionResult> MarkVatNumberAsInvalid(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            await _genericAttributeService.SaveAttributeAsync(customer,
                NopCustomerDefaults.VatNumberStatusIdAttribute,
                (int)VatNumberStatus.Invalid);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("remove-affiliate")]
        public virtual async Task<IActionResult> RemoveAffiliate(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            customer.AffiliateId = 0;
            await _customerService.UpdateCustomerAsync(customer);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost]
        public virtual async Task<IActionResult> RemoveBindMFA(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return RedirectToAction("List");

            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedMultiFactorAuthenticationProviderAttribute, string.Empty);

            //raise event       
            await _eventPublisher.PublishAsync(new CustomerChangeMultiFactorAuthenticationProviderEvent(customer));

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.UnbindMFAProvider"));

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost]
        public virtual async Task<IActionResult> Delete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return RedirectToAction("List");

            try
            {
                //prevent attempts to delete the user, if it is the last active administrator
                if (await _customerService.IsAdminAsync(customer) && !await SecondAdminAccountExistsAsync(customer))
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminAccountShouldExists.DeleteAdministrator"));
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                //ensure that the current customer cannot delete "Administrators" if he's not an admin himself
                if (await _customerService.IsAdminAsync(customer) && !await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()))
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.OnlyAdminCanDeleteAdmin"));
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                //delete
                await _customerService.DeleteCustomerAsync(customer);

                //remove newsletter subscription (if exists)
                foreach (var store in await _storeService.GetAllStoresAsync())
                {
                    var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(customer.Email, store.Id);
                    if (subscription != null)
                        await _newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(subscription);
                }

                //activity log
                await _customerActivityService.InsertActivityAsync("DeleteCustomer",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteCustomer"), customer.Id), customer);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Deleted"));

                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                return RedirectToAction("Edit", new { id = customer.Id });
            }
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("impersonate")]
        public virtual async Task<IActionResult> Impersonate(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.AllowCustomerImpersonation))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return RedirectToAction("List");

            if (!customer.Active)
            {
                _notificationService.WarningNotification(
                    await _localizationService.GetResourceAsync("Admin.Customers.Customers.Impersonate.Inactive"));
                return RedirectToAction("Edit", customer.Id);
            }

            //ensure that a non-admin user cannot impersonate as an administrator
            //otherwise, that user can simply impersonate as an administrator and gain additional administrative privileges
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            if (!await _customerService.IsAdminAsync(currentCustomer) && await _customerService.IsAdminAsync(customer))
            {
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.NonAdminNotImpersonateAsAdminError"));
                return RedirectToAction("Edit", customer.Id);
            }

            //activity log
            await _customerActivityService.InsertActivityAsync("Impersonation.Started",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Started.StoreOwner"), customer.Email, customer.Id), customer);
            await _customerActivityService.InsertActivityAsync(customer, "Impersonation.Started",
                string.Format(await _localizationService.GetResourceAsync("ActivityLog.Impersonation.Started.Customer"), currentCustomer.Email, currentCustomer.Id), currentCustomer);

            //ensure login is not required
            customer.RequireReLogin = false;
            await _customerService.UpdateCustomerAsync(customer);
            await _genericAttributeService.SaveAttributeAsync<int?>(currentCustomer, NopCustomerDefaults.ImpersonatedCustomerIdAttribute, customer.Id);

            return RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("send-welcome-message")]
        public virtual async Task<IActionResult> SendWelcomeMessage(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.SendWelcomeMessage.Success"));

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("resend-activation-message")]
        public virtual async Task<IActionResult> ReSendActivationMessage(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            //email validation message
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
            await _workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.ReSendActivationMessage.Success"));

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        public virtual async Task<IActionResult> SendEmail(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            try
            {
                if (string.IsNullOrWhiteSpace(customer.Email))
                    throw new NopException("Customer email is empty");
                if (!CommonHelper.IsValidEmail(customer.Email))
                    throw new NopException("Customer email is not valid");
                if (string.IsNullOrWhiteSpace(model.SendEmail.Subject))
                    throw new NopException("Email subject is empty");
                if (string.IsNullOrWhiteSpace(model.SendEmail.Body))
                    throw new NopException("Email body is empty");

                var emailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    emailAccount = (await _emailAccountService.GetAllEmailAccountsAsync()).FirstOrDefault();
                if (emailAccount == null)
                    throw new NopException("Email account can't be loaded");
                var email = new QueuedEmail
                {
                    Priority = QueuedEmailPriority.High,
                    EmailAccountId = emailAccount.Id,
                    FromName = emailAccount.DisplayName,
                    From = emailAccount.Email,
                    ToName = await _customerService.GetCustomerFullNameAsync(customer),
                    To = customer.Email,
                    Subject = model.SendEmail.Subject,
                    Body = model.SendEmail.Body,
                    CreatedOnUtc = DateTime.UtcNow,
                    DontSendBeforeDateUtc = model.SendEmail.SendImmediately || !model.SendEmail.DontSendBeforeDate.HasValue ?
                        null : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.SendEmail.DontSendBeforeDate.Value)
                };
                await _queuedEmailService.InsertQueuedEmailAsync(email);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.SendEmail.Queued"));
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
            }

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        public virtual async Task<IActionResult> SendPm(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            try
            {
                if (!_forumSettings.AllowPrivateMessages)
                    throw new NopException("Private messages are disabled");
                if (await _customerService.IsGuestAsync(customer))
                    throw new NopException("Customer should be registered");
                if (string.IsNullOrWhiteSpace(model.SendPm.Subject))
                    throw new NopException(await _localizationService.GetResourceAsync("PrivateMessages.SubjectCannotBeEmpty"));
                if (string.IsNullOrWhiteSpace(model.SendPm.Message))
                    throw new NopException(await _localizationService.GetResourceAsync("PrivateMessages.MessageCannotBeEmpty"));

                var store = await _storeContext.GetCurrentStoreAsync();

                var privateMessage = new PrivateMessage
                {
                    StoreId = store.Id,
                    ToCustomerId = customer.Id,
                    FromCustomerId = customer.Id,
                    Subject = model.SendPm.Subject,
                    Text = model.SendPm.Message,
                    IsDeletedByAuthor = false,
                    IsDeletedByRecipient = false,
                    IsRead = false,
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _forumService.InsertPrivateMessageAsync(privateMessage);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.SendPM.Sent"));
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
            }

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        #endregion

        #region Reward points history

        [HttpPost]
        public virtual async Task<IActionResult> RewardPointsHistorySelect(CustomerRewardPointsSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareRewardPointsListModelAsync(searchModel, customer);

            return Json(model);
        }

        public virtual async Task<IActionResult> RewardPointsHistoryAdd(AddRewardPointsToCustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prevent adding a new row with zero value
            if (model.Points == 0)
                return ErrorJson(await _localizationService.GetResourceAsync("Admin.Customers.Customers.RewardPoints.AddingZeroValueNotAllowed"));

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (customer == null)
                return ErrorJson("Customer cannot be loaded");

            //check whether delay is set
            DateTime? activatingDate = null;
            if (!model.ActivatePointsImmediately && model.ActivationDelay > 0)
            {
                var delayPeriod = (RewardPointsActivatingDelayPeriod)model.ActivationDelayPeriodId;
                var delayInHours = delayPeriod.ToHours(model.ActivationDelay);
                activatingDate = DateTime.UtcNow.AddHours(delayInHours);
            }

            //whether points validity is set
            DateTime? endDate = null;
            if (model.PointsValidity > 0)
                endDate = (activatingDate ?? DateTime.UtcNow).AddDays(model.PointsValidity.Value);

            //add reward points
            await _rewardPointService.AddRewardPointsHistoryEntryAsync(customer, model.Points, model.StoreId, model.Message,
                activatingDate: activatingDate, endDate: endDate);

            return Json(new { Result = true });
        }

        #endregion

        #region Addresses

        [HttpPost]
        public virtual async Task<IActionResult> AddressesSelect(CustomerAddressSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerAddressListModelAsync(searchModel, customer);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressDelete(int id, int customerId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(customerId)
                ?? throw new ArgumentException("No customer found with the specified id", nameof(customerId));

            //try to get an address with the specified id
            var address = await _customerService.GetCustomerAddressAsync(customer.Id, id);

            if (address == null)
                return Content("No address found with the specified id");

            await _customerService.RemoveCustomerAddressAsync(customer, address);
            await _customerService.UpdateCustomerAsync(customer);

            //now delete the address record
            await _addressService.DeleteAddressAsync(address);

            return new NullJsonResult();
        }

        public virtual async Task<IActionResult> AddressCreate(int customerId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null)
                return RedirectToAction("List");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerAddressModelAsync(new CustomerAddressModel(), customer, null);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressCreate(CustomerAddressModel model, IFormCollection form)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (customer == null)
                return RedirectToAction("List");

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            if (ModelState.IsValid)
            {
                var address = model.Address.ToEntity<Address>();
                address.CustomAttributes = customAttributes;
                address.CreatedOnUtc = DateTime.UtcNow;

                //some validation
                if (address.CountryId == 0)
                    address.CountryId = null;
                if (address.StateProvinceId == 0)
                    address.StateProvinceId = null;

                await _addressService.InsertAddressAsync(address);

                await _customerService.InsertCustomerAddressAsync(customer, address);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Addresses.Added"));

                return RedirectToAction("AddressEdit", new { addressId = address.Id, customerId = model.CustomerId });
            }

            //prepare model
            model = await _customerModelFactory.PrepareCustomerAddressModelAsync(model, customer, null, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual async Task<IActionResult> AddressEdit(int addressId, int customerId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            if (customer == null)
                return RedirectToAction("List");

            //try to get an address with the specified id
            var address = await _addressService.GetAddressByIdAsync(addressId);
            if (address == null)
                return RedirectToAction("Edit", new { id = customer.Id });

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerAddressModelAsync(null, customer, address);

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddressEdit(CustomerAddressModel model, IFormCollection form)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (customer == null)
                return RedirectToAction("List");

            //try to get an address with the specified id
            var address = await _addressService.GetAddressByIdAsync(model.Address.Id);
            if (address == null)
                return RedirectToAction("Edit", new { id = customer.Id });

            //custom address attributes
            var customAttributes = await _addressAttributeParser.ParseCustomAddressAttributesAsync(form);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarningsAsync(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            if (ModelState.IsValid)
            {
                address = model.Address.ToEntity(address);
                address.CustomAttributes = customAttributes;
                await _addressService.UpdateAddressAsync(address);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Addresses.Updated"));

                return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, customerId = model.CustomerId });
            }

            //prepare model
            model = await _customerModelFactory.PrepareCustomerAddressModelAsync(model, customer, address, true);

            //if we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Orders

        [HttpPost]
        public virtual async Task<IActionResult> OrderList(CustomerOrderSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerOrderListModelAsync(searchModel, customer);

            return Json(model);
        }

        #endregion

        #region Customer

        public virtual async Task<IActionResult> LoadCustomerStatistics(string period)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return Content(string.Empty);

            var result = new List<object>();

            var nowDt = await _dateTimeHelper.ConvertToUserTimeAsync(DateTime.Now);
            var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();
            var searchCustomerRoleIds = new[] { (await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName)).Id };

            var culture = new CultureInfo((await _workContext.GetWorkingLanguageAsync()).LanguageCulture);

            switch (period)
            {
                case "year":
                    //year statistics
                    var yearAgoDt = nowDt.AddYears(-1).AddMonths(1);
                    var searchYearDateUser = new DateTime(yearAgoDt.Year, yearAgoDt.Month, 1);
                    for (var i = 0; i <= 12; i++)
                    {
                        result.Add(new
                        {
                            date = searchYearDateUser.Date.ToString("Y", culture),
                            value = (await _customerService.GetAllCustomersAsync(
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchYearDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchYearDateUser.AddMonths(1), timeZone),
                                customerRoleIds: searchCustomerRoleIds,
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchYearDateUser = searchYearDateUser.AddMonths(1);
                    }

                    break;
                case "month":
                    //month statistics
                    var monthAgoDt = nowDt.AddDays(-30);
                    var searchMonthDateUser = new DateTime(monthAgoDt.Year, monthAgoDt.Month, monthAgoDt.Day);
                    for (var i = 0; i <= 30; i++)
                    {
                        result.Add(new
                        {
                            date = searchMonthDateUser.Date.ToString("M", culture),
                            value = (await _customerService.GetAllCustomersAsync(
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchMonthDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchMonthDateUser.AddDays(1), timeZone),
                                customerRoleIds: searchCustomerRoleIds,
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchMonthDateUser = searchMonthDateUser.AddDays(1);
                    }

                    break;
                case "week":
                default:
                    //week statistics
                    var weekAgoDt = nowDt.AddDays(-7);
                    var searchWeekDateUser = new DateTime(weekAgoDt.Year, weekAgoDt.Month, weekAgoDt.Day);
                    for (var i = 0; i <= 7; i++)
                    {
                        result.Add(new
                        {
                            date = searchWeekDateUser.Date.ToString("d dddd", culture),
                            value = (await _customerService.GetAllCustomersAsync(
                                createdFromUtc: _dateTimeHelper.ConvertToUtcTime(searchWeekDateUser, timeZone),
                                createdToUtc: _dateTimeHelper.ConvertToUtcTime(searchWeekDateUser.AddDays(1), timeZone),
                                customerRoleIds: searchCustomerRoleIds,
                                pageIndex: 0,
                                pageSize: 1, getOnlyTotalCount: true)).TotalCount.ToString()
                        });

                        searchWeekDateUser = searchWeekDateUser.AddDays(1);
                    }

                    break;
            }

            return Json(result);
        }

        #endregion

        #region Current shopping cart/ wishlist

        [HttpPost]
        public virtual async Task<IActionResult> GetCartList(CustomerShoppingCartSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerShoppingCartListModelAsync(searchModel, customer);

            return Json(model);
        }

        #endregion

        #region Activity log

        [HttpPost]
        public virtual async Task<IActionResult> ListActivityLog(CustomerActivityLogSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerActivityLogListModelAsync(searchModel, customer);

            return Json(model);
        }

        #endregion

        #region Back in stock subscriptions

        [HttpPost]
        public virtual async Task<IActionResult> BackInStockSubscriptionList(CustomerBackInStockSubscriptionSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(searchModel.CustomerId)
                ?? throw new ArgumentException("No customer found with the specified id");

            //prepare model
            var model = await _customerModelFactory.PrepareCustomerBackInStockSubscriptionListModelAsync(searchModel, customer);

            return Json(model);
        }

        #endregion

        #region GDPR

        public virtual async Task<IActionResult> GdprLog()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prepare model
            var model = await _customerModelFactory.PrepareGdprLogSearchModelAsync(new GdprLogSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> GdprLogList(GdprLogSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return await AccessDeniedDataTablesJson();

            //prepare model
            var model = await _customerModelFactory.PrepareGdprLogListModelAsync(searchModel);

            return Json(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> GdprDelete(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return RedirectToAction("List");

            if (!_gdprSettings.GdprEnabled)
                return RedirectToAction("List");

            try
            {
                //prevent attempts to delete the user, if it is the last active administrator
                if (await _customerService.IsAdminAsync(customer) && !await SecondAdminAccountExistsAsync(customer))
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.AdminAccountShouldExists.DeleteAdministrator"));
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                //ensure that the current customer cannot delete "Administrators" if he's not an admin himself
                if (await _customerService.IsAdminAsync(customer) && !await _customerService.IsAdminAsync(await _workContext.GetCurrentCustomerAsync()))
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.OnlyAdminCanDeleteAdmin"));
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                //delete
                await _gdprService.PermanentDeleteCustomerAsync(customer);

                //activity log
                await _customerActivityService.InsertActivityAsync("DeleteCustomer",
                    string.Format(await _localizationService.GetResourceAsync("ActivityLog.DeleteCustomer"), customer.Id), customer);

                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Customers.Customers.Deleted"));

                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                return RedirectToAction("Edit", new { id = customer.Id });
            }
        }

        public virtual async Task<IActionResult> GdprExport(int id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return RedirectToAction("List");

            try
            {
                //log
                //_gdprService.InsertLog(customer, 0, GdprRequestType.ExportData, await _localizationService.GetResource("Gdpr.Exported"));
                //export
                //export
                var store = await _storeContext.GetCurrentStoreAsync();
                var bytes = await _exportManager.ExportCustomerGdprInfoToXlsxAsync(customer, store.Id);

                return File(bytes, MimeTypes.TextXlsx, $"customerdata-{customer.Id}.xlsx");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("Edit", new { id = customer.Id });
            }
        }
        #endregion

        #region Export / Import

        [HttpPost, ActionName("ExportExcel")]
        [FormValueRequired("exportexcel-all")]
        public virtual async Task<IActionResult> ExportExcelAll(CustomerSearchModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var customers = await _customerService.GetAllCustomersAsync(customerRoleIds: model.SelectedCustomerRoleIds.ToArray(),
                email: model.SearchEmail,
                username: model.SearchUsername,
                firstName: model.SearchFirstName,
                lastName: model.SearchLastName,
                dayOfBirth: int.TryParse(model.SearchDayOfBirth, out var dayOfBirth) ? dayOfBirth : 0,
                monthOfBirth: int.TryParse(model.SearchMonthOfBirth, out var monthOfBirth) ? monthOfBirth : 0,
                company: model.SearchCompany,
                phone: model.SearchPhone,
                zipPostalCode: model.SearchZipPostalCode);

            try
            {
                var bytes = await _exportManager.ExportCustomersToXlsxAsync(customers);
                return File(bytes, MimeTypes.TextXlsx, "customers.xlsx");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> ExportExcelSelected(string selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var customers = new List<Customer>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                customers.AddRange(await _customerService.GetCustomersByIdsAsync(ids));
            }

            try
            {
                var bytes = await _exportManager.ExportCustomersToXlsxAsync(customers);
                return File(bytes, MimeTypes.TextXlsx, "customers.xlsx");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost, ActionName("ExportXML")]
        [FormValueRequired("exportxml-all")]
        public virtual async Task<IActionResult> ExportXmlAll(CustomerSearchModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var customers = await _customerService.GetAllCustomersAsync(customerRoleIds: model.SelectedCustomerRoleIds.ToArray(),
                email: model.SearchEmail,
                username: model.SearchUsername,
                firstName: model.SearchFirstName,
                lastName: model.SearchLastName,
                dayOfBirth: int.TryParse(model.SearchDayOfBirth, out var dayOfBirth) ? dayOfBirth : 0,
                monthOfBirth: int.TryParse(model.SearchMonthOfBirth, out var monthOfBirth) ? monthOfBirth : 0,
                company: model.SearchCompany,
                phone: model.SearchPhone,
                zipPostalCode: model.SearchZipPostalCode);

            try
            {
                var xml = await _exportManager.ExportCustomersToXmlAsync(customers);
                return File(Encoding.UTF8.GetBytes(xml), "application/xml", "customers.xml");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> ExportXmlSelected(string selectedIds)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var customers = new List<Customer>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                customers.AddRange(await _customerService.GetCustomersByIdsAsync(ids));
            }

            try
            {
                var xml = await _exportManager.ExportCustomersToXmlAsync(customers);
                return File(Encoding.UTF8.GetBytes(xml), "application/xml", "customers.xml");
            }
            catch (Exception exc)
            {
                await _notificationService.ErrorNotificationAsync(exc);
                return RedirectToAction("List");
            }
        }

        #endregion

        #region TaxJar Customer
        // Address
        public virtual async Task<IActionResult> SyncAddressToTaxJar(string customerId, int addressid)
        {
            var Address = await _addressService.GetAddressByIdAsync(addressid);
            var (IsError, ErrorMessage, TaxJarCustomer) = await _taxJarService.GetCustomerById(customerId);
            if (IsError || Address == null || TaxJarCustomer == null)
            {
                string Mgs = !string.IsNullOrEmpty(ErrorMessage) ? ErrorMessage : "Address Not Found";
                await _logger.ErrorAsync(Mgs, null, await _workContext.GetCurrentCustomerAsync());
                _notificationService.ErrorNotification("TaxJar Sync Error:- " + Mgs);
                return RedirectToAction("Edit", new { id = customerId });
            }

            (TaxJarCustomer.Street, TaxJarCustomer.City, TaxJarCustomer.ZipCode, TaxJarCustomer.State, TaxJarCustomer.Country) = await GetAddressDetails(Address);

            (IsError, ErrorMessage) = await _taxJarService.UpdateCustomer(TaxJarCustomer);
            if (IsError)
            {
                await _logger.ErrorAsync(ErrorMessage, null, await _workContext.GetCurrentCustomerAsync());
                _notificationService.ErrorNotification("TaxJar Sync Error:- " + ErrorMessage);
                return RedirectToAction("Edit", new { id = customerId });
            }

            _notificationService.SuccessNotification("TaxJar Sync Successful:- Customer Address Updated");
            return RedirectToAction("Edit", new { id = customerId });
        }

        // Delete 
        [HttpPost]
        public virtual async Task<IActionResult> DeleteCustomersFromTaxJar(string selectedIds)
        {
            if (string.IsNullOrEmpty(selectedIds))
                return Content("Customer Ids Missing");

            var errorMessageList = new List<string>();
            var utcNow = DateTime.UtcNow;
            const int batchSize = 50;

            var customerIds = selectedIds.Split(',').Select(int.Parse).ToList();
            var customerBatches = SplitIntoBatches(customerIds, batchSize);

            foreach (var customerId in customerBatches)
            {
                var tasks = customerId.Select(async customerId =>
                {
                    var (isError, errorMessage) = await _taxJarService.DeleteCustomerById(customerId.ToString());
                    if (isError)
                        errorMessageList.Add($"{customerId}:- {errorMessage}");
                    else
                    {
                        var customer = await _customerService.GetCustomerByIdAsync(customerId);
                        if (customer.IsSyncedToTaxJar)
                        {
                            customer.IsSyncedToTaxJar = false;
                            customer.IsRecentlyModified = false;
                            customer.IsRecentlyDeleted = false;
                            customer.SyncedToTaxJarDateTime = utcNow;
                            await _customerService.UpdateCustomerAsync(customer);
                        }
                    }
                });

                await Task.WhenAll(tasks); // Process all items in the batch concurrently
            }

            if (errorMessageList.Any())
            {
                string Errors = string.Join(',', errorMessageList);
                await _logger.ErrorAsync(Errors, null, await _workContext.GetCurrentCustomerAsync());
                _notificationService.ErrorNotification(Errors);
                return RedirectToAction("List");
            }

            _notificationService.SuccessNotification("TaxJar Sync Successful:- Customers Deleted From Taxjar");
            return RedirectToAction("List");
        }

        // Delete Batches
        private List<List<int>> SplitIntoBatches(List<int> deleteCustomerList, int batchSize)
        {
            return deleteCustomerList
                .Select((customer, index) => new { customer, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.customer).ToList())
                .ToList();
        }

        // Customer
        [HttpPost]
        public virtual async Task<IActionResult> SyncToTaxJar(string selectedIds)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedIds))
                    return Content("Customer Ids Missing");

                var errorMessageList = new List<string>();

                errorMessageList = await StartSync(selectedIds, errorMessageList, 1);

                if (errorMessageList.Any())
                {
                    string Errors = string.Join(',', errorMessageList);
                    await _logger.ErrorAsync(Errors, null, await _workContext.GetCurrentCustomerAsync());
                    _notificationService.ErrorNotification("TaxJar Sync " + Errors);
                    return RedirectToAction("List");
                }

                _notificationService.SuccessNotification("TaxJar Sync Successful:- Customers Synced To Taxjar");
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                _notificationService.ErrorNotification($"TaxJar Sync Error:- {ex.Message} : {ex.InnerException?.Message}");
                return RedirectToAction("List");
            }
        }

        // Customer Sync
        private async Task<List<string>> StartSync(string selectedIds, List<string> errorMessageList, int IsCreateUpdateDelete)
        {
            const int batchSize = 25;
            var utcNow = DateTime.UtcNow;
            var customerIds = selectedIds.Split(',').Select(int.Parse).ToList();
            var allCustomers = _customerService.GetCustomersForSyncingToTaxJarList(customerIds);
            var syncCustomerList = await PrepareSyncCustomerList(allCustomers);
            var customerBatches = SplitIntoBatches(syncCustomerList, batchSize);

            foreach (var batch in customerBatches)
            {
                var tasks = batch.Select(async item =>
                {
                    var (isError, errorMessage) =
                        IsCreateUpdateDelete == 1 ? await _taxJarService.CreateCustomer(item) :
                        IsCreateUpdateDelete == 2 ? await _taxJarService.UpdateCustomer(item) :
                        IsCreateUpdateDelete == 3 ? await _taxJarService.DeleteCustomerById(item.CustomerId) :
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

        private async Task<List<Nop.Core.Domian.TaxJar.CustomerModel>> PrepareSyncCustomerList(List<Customer> allCustomers)
        {
            var syncCustomerList = new List<Nop.Core.Domian.TaxJar.CustomerModel>();

            var tasks = allCustomers.Select(async customer =>
            {
                var model = new Nop.Core.Domian.TaxJar.CustomerModel
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

        private List<List<Nop.Core.Domian.TaxJar.CustomerModel>> SplitIntoBatches(List<Nop.Core.Domian.TaxJar.CustomerModel> syncCustomerList, int batchSize)
        {
            return syncCustomerList
                .Select((customer, index) => new { customer, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.customer).ToList())
                .ToList();
        }

        #endregion

        #region Customer Secondary Emails
        //Added by Yogesh Kumbhar on Dt: 01-27-2025
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> DeleteCustomerEmail(int customerId, int emailId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return Json(new { success = false, message = "Access denied" });

            if (emailId == 0)
                return Json(new { success = false, message = "Email cannot be null or empty" });

            try
            {
                await _customerService.DeleteCustomerEmailAsync(customerId, emailId);

                return Json(new { success = true, message = "Customer email deleted successfully" });
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                return Json(new { success = false, message = exc.Message });
            }
        }

        //Added by Yogesh Kumbhar on Dt: 01-31-2025 & 02-03-2025
        public virtual async Task<IActionResult> InsertAndUpdateCustmerEmail(CustomerModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //try to get a customer with the specified id
            var customer = await _customerService.GetCustomerByIdAsync(model.Id);
            if (customer == null)
                return RedirectToAction("List");

            try
            {
                if (string.IsNullOrWhiteSpace(model.CustomerEmail.Email))
                    throw new NopException("Customer email is empty");

                if (await _customerService.ExistCustomerEmailAsync(model.CustomerEmail.Email, customer.Id, model.CustomerEmail.Id))
                    throw new NopException("This email is already associated with this customer. Please try a different one.");

                if (model.CustomerEmail.Id == 0)
                {
                    var custEmail = new CustomerEmail();
                    custEmail.CustomerId = customer.Id;
                    custEmail.Email = model.CustomerEmail.Email;
                    custEmail.IsOrder = model.CustomerEmail.IsOrder;
                    custEmail.IsOrderCancel = model.CustomerEmail.IsOrderCancel;
                    custEmail.IsOrderShipped = model.CustomerEmail.IsOrderShipped;
                    custEmail.IsEstimate = model.CustomerEmail.IsEstimate;
                    custEmail.IsRefund = model.CustomerEmail.IsRefund;
                    custEmail.CreatedOnUtc = DateTime.UtcNow;
                    await _customerService.InsertCustomerEmailAsync(custEmail);

                }
                else
                {
                    CustomerEmail customerEmail = await _customerService.GetCustomerEmailByIdAsync(model.CustomerEmail.Id);

                    customerEmail.Email = model.CustomerEmail.Email;
                    customerEmail.IsOrder = model.CustomerEmail.IsOrder;
                    customerEmail.IsOrderCancel = model.CustomerEmail.IsOrderCancel;
                    customerEmail.IsOrderShipped = model.CustomerEmail.IsOrderShipped;
                    customerEmail.IsEstimate = model.CustomerEmail.IsEstimate;
                    customerEmail.IsRefund = model.CustomerEmail.IsRefund;
                    customerEmail.UpdatedOnUtc = DateTime.UtcNow;
                    await _customerService.UpdateCustomerEmailAsync(customerEmail);
                }
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Customer email saved successfully"));

            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
            }

            return RedirectToAction("Edit", new { id = customer.Id });
        }
        
        //Added by Yogesh Kumbhar on Dt: 01-31-2025
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> GetCustomerEmail(int emailId)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return Json(new { success = false, message = "Access denied" });

            if (emailId == 0)
                return Json(new { success = false, message = "Email not found" });

            try
            {
                var customerEmail = await _customerService.GetCustomerEmailByIdAsync(emailId);

                return Json(new { success = true, response = customerEmail });
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                return Json(new { success = false, message = exc.Message });
            }
        }
        #endregion
    }
}