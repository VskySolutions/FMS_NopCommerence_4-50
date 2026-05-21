using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.VideoGallarys;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.VideoGallaries;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.VideoGallaries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Web.Areas.Admin.Controllers
{
    public class VideoGallaryController : BaseAdminController
    {
        #region Fields
        private readonly IVideoGalleryModelFactory _VideoGalleryModelFactory;
        private readonly IVideoGallaryService _VideoGallaryService;
        private readonly IPermissionService _permissionService;
        private readonly INotificationService _notificationService;
        #endregion

        #region Ctor
        public VideoGallaryController(IVideoGallaryService videoGallaryService, 
                                        IPermissionService permissionService,
                                        IVideoGalleryModelFactory VideoGalleryModelFactory,
                                        INotificationService notificationService)
        {
            _VideoGallaryService = videoGallaryService;
            _permissionService = permissionService;
            _notificationService = notificationService;
            _VideoGalleryModelFactory = VideoGalleryModelFactory;
        }
        #endregion

        #region Methods

        #region Fields
        /// <summary>
        /// Add Video
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IActionResult> AddVideo()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();
            

            var model = new VideoGallaryModel();
            return View();
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddVideo(VideoGallaryModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var VideoCore = new VideoGallary();
            VideoCore.VideoTitle = model.VideoTitle;
            VideoCore.VideoUrl = model.VideoUrl;
            VideoCore.VideoTumbnailImage = getYouTubeThumbnail(model.VideoUrl);
            VideoCore.CreatedDate = DateTime.UtcNow;
            VideoCore.Publish = model.Publish;
            await _VideoGallaryService.InsertVideoGallary(VideoCore);

            _notificationService.SuccessNotification("New Video Url added");
            return View();
        }
        #endregion

        /// <summary>
        /// get YouTube Thumbnail
        /// </summary>
        /// <param name="YoutubeUrl"></param>
        /// <returns></returns>
        public string getYouTubeThumbnail(string YoutubeUrl)
        {
            string youTubeThumb = string.Empty;
            if (YoutubeUrl == "")
                return "";

            if (YoutubeUrl.IndexOf("=") > 0)
            {
                youTubeThumb = YoutubeUrl.Split('=')[1];
            }
            else if (YoutubeUrl.IndexOf("/v/") > 0)
            {
                string strVideoCode = YoutubeUrl.Substring(YoutubeUrl.IndexOf("/v/") + 3);
                int ind = strVideoCode.IndexOf("?");
                youTubeThumb = strVideoCode.Substring(0, ind == -1 ? strVideoCode.Length : ind);
            }
            else if (YoutubeUrl.IndexOf('/') < 6)
            {
                youTubeThumb = YoutubeUrl.Split('/')[3];
            }
            else if (YoutubeUrl.IndexOf('/') > 6)
            {
                youTubeThumb = YoutubeUrl.Split('/')[1];
            }

            return "http://img.youtube.com/vi/" + youTubeThumb + "/mqdefault.jpg";
        }

        /// <summary>
        /// List
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IActionResult> List()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prepare model
            var model = await _VideoGalleryModelFactory.PrepareVideoGalleryAsync(new VideoSearchModel());

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> VideoList(VideoSearchModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            //prepare model
            var model = await _VideoGalleryModelFactory.PrepareVideoGalleryListModelAsync(searchModel);

            return Json(model);
        }
        #endregion

        #region Video Gallery Edit
        /// <summary>
        /// Edit Video Gallery
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public virtual async Task<IActionResult> Edit(int Id)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var category = _VideoGallaryService.GetVideoGallaryById(Id).Result;
            if (category == null)
                //No category found with the specified id
                return RedirectToAction("List");

            var model = new VideoGallaryModel();
            model.Id = category.Id;
            model.VideoTitle = category.VideoTitle;
            model.VideoTumbnailImage = category.VideoTumbnailImage;
            model.VideoUrl = category.VideoUrl;
            model.Publish = category.Publish;

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit(VideoGallaryModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageCustomers))
                return AccessDeniedView();

            var category = _VideoGallaryService.GetVideoGallaryById(model.Id).Result;
            category.VideoTitle = model.VideoTitle;
            category.VideoTumbnailImage = getYouTubeThumbnail(model.VideoUrl);
            category.VideoUrl = model.VideoUrl;
            category.Publish = model.Publish;
            await _VideoGallaryService.UpdateVideoGallary(category);
            _notificationService.SuccessNotification("URL Updated");
            return RedirectToAction("List");
        }

        /// <summary>
        /// Delete Video
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteVideo(int Id=0)
        {
            var category = _VideoGallaryService.GetVideoGallaryById(Id).Result;
            await _VideoGallaryService.DeleteVideoGallary(category);
            _notificationService.SuccessNotification("Video Deleted.");
            return RedirectToAction("List");
        }
        #endregion
    }
}
