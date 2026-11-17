using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Events;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial class MarqueeService : IMarquee
    {
        private readonly IRepository<Marquee> _MarqueeRepository;
        private readonly IEventPublisher _eventPublisher;
        //private readonly DistributedCacheManager _cacheManager;

        public MarqueeService(
             IRepository<Marquee> marqueeRepository,
             IEventPublisher eventPublisher
             //DistributedCacheManager cacheManager
        )
        {
            _MarqueeRepository = marqueeRepository;
            _eventPublisher = eventPublisher;
            //_cacheManager = cacheManager;
        }

        /// <summary>
        /// Update Marquee
        /// </summary>
        /// <param name="marquee"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task UpdateMarquee(Marquee marquee)
        {
            if (marquee == null)
                throw new ArgumentNullException("Marquee");

            //update
            await _MarqueeRepository.UpdateAsync(marquee);

            //event notification
            await _eventPublisher.EntityUpdatedAsync(marquee);
        }

        /// <summary>
        /// Insert Marquee
        /// </summary>
        /// <param name="marquee"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task InsertMarquee(Marquee marquee)
        {
            if (marquee == null)
                throw new ArgumentNullException("Marquee");

            //insert
            await _MarqueeRepository.InsertAsync(marquee);

            //event notification
            await _eventPublisher.EntityInsertedAsync(marquee);
        }

        /// <summary>
        /// Get Marquee By Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual async Task<Marquee> GeMarqueeById(int Id)
        {
            if (Id == 0)
                throw new ArgumentNullException("Marquee");
            var query = await _MarqueeRepository.GetByIdAsync(Id);
            return query;
        }
    }
}
