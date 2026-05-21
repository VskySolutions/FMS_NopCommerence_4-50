using Nop.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Common
{
    public partial interface IMarquee
    {
        /// <summary>
        /// Insert Marquee
        /// </summary>
        /// <param name="Marquee"></param>
        Task InsertMarquee(Marquee Marquee);

        /// <summary>
        /// Update Marquee
        /// </summary>
        /// <param name="Marquee"></param>
        Task UpdateMarquee(Marquee Marquee);

        /// <summary>
        /// GeMarquee By Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        Task<Marquee> GeMarqueeById(int Id);
    }
}
