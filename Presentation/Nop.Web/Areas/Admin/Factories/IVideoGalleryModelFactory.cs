using Nop.Web.Areas.Admin.Models.VideoGallaries;
using System.Threading.Tasks;

namespace Nop.Web.Areas.Admin.Factories
{
    public partial interface IVideoGalleryModelFactory
    {
        /// <summary>
        /// Prepare Video Gallery 
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        Task<VideoSearchModel> PrepareVideoGalleryAsync(VideoSearchModel searchModel);

        /// <summary>
        /// Prepare Video Gallery List ModelAsync
        /// </summary>
        /// <param name="searchModel"></param>
        /// <returns></returns>
        Task<VideoGalleryListModel> PrepareVideoGalleryListModelAsync(VideoSearchModel searchModel);
    }
}
