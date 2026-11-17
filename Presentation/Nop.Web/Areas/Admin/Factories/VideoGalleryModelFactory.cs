using Nop.Services.Seo;
using Nop.Services.VideoGallaries;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Areas.Admin.Models.VideoGallaries;
using Nop.Web.Framework.Models.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Areas.Admin.Factories
{
    public partial class VideoGalleryModelFactory: IVideoGalleryModelFactory
    {
        private readonly IVideoGallaryService _VideoGallaryService;
        private readonly IUrlRecordService _urlRecordService;
        public VideoGalleryModelFactory(IVideoGallaryService videoGallaryService,
                                        IUrlRecordService urlRecordService)
        {
            _VideoGallaryService = videoGallaryService;
            _urlRecordService = urlRecordService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual Task<VideoSearchModel> PrepareVideoGalleryAsync(VideoSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //prepare page parameters
            searchModel.SetGridPageSize();

            return Task.FromResult(searchModel);
        }

        /// <summary>
        /// Prepare Video Gallery List ModelAsync
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task<VideoGalleryListModel> PrepareVideoGalleryListModelAsync(VideoSearchModel searchModel)
        {
            if (searchModel == null)
                throw new ArgumentNullException(nameof(searchModel));

            //get videolist
            var videolist = await _VideoGallaryService.SearchVideoGallary(
                VideoTitleName: searchModel.VideoTitle,
                createdFromUtc: searchModel.CreatedDate,
                createdToUtc: searchModel.CreatedDate,
                pageIndex: searchModel.Page - 1,
                pageSize: searchModel.PageSize
            );

            //prepare list model
            var model = new VideoGalleryListModel().PrepareToGrid(searchModel, videolist, () =>
            {
                return videolist.Select(video =>
                {
                    return new VideoGallaryModel
                    {
                        Id = video.Id,
                        VideoTitle = video.VideoTitle
                    };
                });
            });

            return model;
        }
    }
}
