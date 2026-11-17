using Nop.Core;
using Nop.Core.Domain.VideoGallarys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.VideoGallaries
{
    public partial interface IVideoGallaryService
    {
        /// <summary>
        /// Search Video Gallary
        /// </summary>
        /// <param name="VideoTitleName"></param>
        /// <param name="createdFromUtc"></param>
        /// <param name="createdToUtc"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IPagedList<VideoGallary>> SearchVideoGallary(string VideoTitleName = null,
                                                                     DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
                                                                     int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Insert Video Gallary
        /// </summary>
        /// <param name="video"></param>
        Task InsertVideoGallary(VideoGallary video);

        /// <summary>
        /// Update Video Gallary
        /// </summary>
        /// <param name="video"></param>
        Task UpdateVideoGallary(VideoGallary video);

        /// <summary>
        /// Get Video Gallary ById
        /// </summary>
        /// <param name="videoid"></param>
        /// <returns></returns>
        Task<VideoGallary> GetVideoGallaryById(int videoid);

        /// <summary>
        /// Delete Video Gallary
        /// </summary>
        /// <param name="video"></param>
        Task DeleteVideoGallary(VideoGallary video);

        /// <summary>
        /// Get All List For Video Home Page Controller
        /// </summary>
        /// <returns></returns>
        IList<VideoGallary> GetAllListForVideoHomePageController();
    }
}
