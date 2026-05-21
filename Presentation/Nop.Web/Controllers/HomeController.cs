using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.VideoGallaries;
using Nop.Web.Areas.Admin.Models.VideoGallaries;
using Nop.Web.Models.VideoGallary;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Nop.Web.Controllers
{
    public partial class HomeController : BasePublicController
    {
        private readonly IWorkContext _workContext;
        private readonly IVideoGallaryService _VideoGallarySerivce;
        private readonly ICustomerService _customerService;

        public HomeController(IWorkContext workContext, 
                              IVideoGallaryService VideoGallarySerivce,
                              ICustomerService customerService)
        {
            _workContext = workContext;
            _VideoGallarySerivce = VideoGallarySerivce;
            _customerService = customerService;
        }

        public virtual async Task<IActionResult> Index()
        {
            //await SendEmailToUser(null,0);
            await UserNotification();
            return View();
        }

        /// <summary>
        /// Video Gallery Map
        /// </summary>
        /// <returns></returns>
        public virtual ActionResult VideoGalleryMap(int OrderByDate=0)
        {
            var model = new Nop.Web.Models.VideoGallary.VideoGallaryModel();
            var DataList = _VideoGallarySerivce.GetAllListForVideoHomePageController();
            model.OrderByDate = OrderByDate;
            foreach (var item in DataList)
            {
                var Obj = new ListVideoGallary();
                Obj.VideoTitle = item.VideoTitle;
                Obj.VideoUrl = item.VideoUrl;
                Obj.CreatedDate = item.CreatedDate;
                Obj.VideoTumbnailImage = item.VideoTumbnailImage;
                model.VideoGallaryList.Add(Obj);
            }
            return View("VideoGallaryMap",model);
        }

        /// <summary>
        /// User Notification
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IActionResult> UserNotification()
        {
            var Customers = await _customerService.GetAllCustomersAsync();

            //Get Currunt User
            Customer customer = await _workContext.GetCurrentCustomerAsync();

            foreach (var item in Customers)
            {
                DateTime? TaxFileExpDate = item.TaxFileExpDate;

                if (TaxFileExpDate!=null)
                {
                    DateTime CurrentDateTime = DateTime.UtcNow;
                    Double? Dayscount = ((DateTime)TaxFileExpDate - CurrentDateTime).TotalDays;

                    int Days = (Dayscount == null?0: Convert.ToInt32(Dayscount));

                    if (Days>0)
                    {
                        if (Convert.ToInt32(Days)==7)
                        {
                            if (item.Email!=null)
                                await SendEmailToUser(item,Days);
                        }
                        else if (Convert.ToInt32(Days)==14)
                        {
                            if (item.Email!=null)
                                await SendEmailToUser(item, Days);
                        }
                        else if (Convert.ToInt32(Days)==21)
                        {
                            if (item.Email!=null)
                                await SendEmailToUser(item, Days);
                        }
                        else if (Convert.ToInt32(Days)==30)
                        {
                            if (item.Email!=null)
                                await SendEmailToUser(item, Days);
                        }

                        if (customer != null)
                        {
                            if (customer.Id == item.Id)
                            {
                                //Check Tax exampt file notification expiry 
                                if (Convert.ToInt32(Days) <= 30)
                                {
                                    ViewBag.TaxexamptFile = "Your tax exempt file will expire in "+ Days +" Days.";
                                }
                            }
                        }
                    }
                }
            }

            if (customer != null)
                if (customer.Email == null)
                    ViewBag.TaxexamptFile = "";

            return View();
        }

        /// <summary>
        /// Send Email To User
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendEmailToUser(Customer customers, int Days=0)
        {
            bool EmailStatus = false;
            var mail = new MailMessage();

            mail.To.Add("info@fmsaccessories.com");
            mail.Subject = "Tax exempt file upload";            
            mail.Body = "\r\n Tax exempt file will expire in "+ Days +" days, Please uploaded your Tax exempt file";
            mail.IsBodyHtml = true;
            
            await _customerService.SendEmailAsyncEvents(mail);
            return EmailStatus;
        }
    }
}