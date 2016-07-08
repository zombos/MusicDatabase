﻿using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MusicDatabase.EntityFramework;
using MusicDatabase.Model;
using MusicDatabase.Services.Interfaces;

namespace MusicDatabase.Services
{
    public class ReleaseService : BaseService
    {
        public ReleaseService(IRepository repository, IMapper mapper)
            :base(repository, mapper) { }

        public IEnumerable<Release> RetrieveReleasesByYear(int year)
        {
            using (var context = new MusicDbContext())
            {
                var releases = Repositiory.Query<Release>(context, EntityState.Active)
                                    .Where(r => r.Year == year)
                                    .Where(r => r.Discographies.Any(d => d.MusicalEntity.IsActive))
                                    .Where(r => r.Copies.Any(c => (c.IsActive) &&
                                        (!(c.AcquisitionDetails is OnlinePurchase) || 
                                        (c.AcquisitionDetails is OnlinePurchase) && ((c.AcquisitionDetails as OnlinePurchase).DateAdded.HasValue))));

                return releases.ToList();
            }
        }

        // Retrieve Releases By Format
    }
}