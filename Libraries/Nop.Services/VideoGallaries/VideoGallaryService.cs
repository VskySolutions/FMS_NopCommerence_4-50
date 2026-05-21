using Nop.Core;
using Nop.Core.Domain.VideoGallarys;
using Nop.Core.Events;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nop.Services.VideoGallaries
{
    public partial class VideoGallaryService : IVideoGallaryService
    {
        private readonly IRepository<VideoGallary> _videogallaryRepository;
        private readonly IEventPublisher _eventPublisher;

        public VideoGallaryService(IRepository<VideoGallary> videogallaryRepository, IEventPublisher eventPublisher)
        {
            this._videogallaryRepository = videogallaryRepository;
            this._eventPublisher = eventPublisher;
        }

        /// <summary>
        /// Search Video Gallary
        /// </summary>
        /// <param name="VideoTitleName"></param>
        /// <param name="createdFromUtc"></param>
        /// <param name="createdToUtc"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public virtual async Task<IPagedList<VideoGallary>> SearchVideoGallary(string VideoTitleName = null,
                                                                    DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
                                                                    int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var videoList = await _videogallaryRepository.GetAllPagedAsync(query =>
            {
                query = query.Where(o => o.VideoDelete == false);
                query = query.OrderByDescending(o => o.CreatedDate);
                return query;
            }, pageIndex, pageSize);
            return videoList;
        }


        public virtual  IList<VideoGallary> GetAllListForVideoHomePageController()
        {
            var query = _videogallaryRepository.Table;
            query = query.Where(a => a.Publish == true);
            query = query.Where(a => a.VideoDelete != true);
            return query.ToList();
        }

        public async Task InsertVideoGallary(VideoGallary video)
        {
            if (video == null)
                throw new ArgumentNullException("VideoGallary");

            await _videogallaryRepository.InsertAsync(video);

            //event notification
            await _eventPublisher.EntityInsertedAsync(video);
        }
        public async Task UpdateVideoGallary(VideoGallary video)
        {
            if (video == null)
                throw new ArgumentNullException("VideoGallary");

            await _videogallaryRepository.UpdateAsync(video);

            //event notification
            await _eventPublisher.EntityUpdatedAsync(video);
        }


        public async Task<VideoGallary> GetVideoGallaryById(int videoid)
        {
            if (videoid == 0)
                return null;

            return await _videogallaryRepository.GetByIdAsync(videoid);
        }


        public async Task DeleteVideoGallary(VideoGallary video)
        {
            if (video == null)
                throw new ArgumentNullException("VideoGallary");
            await _videogallaryRepository.DeleteAsync(video);
            //event notification
            await _eventPublisher.EntityDeletedAsync(video);
        }
    }
}
