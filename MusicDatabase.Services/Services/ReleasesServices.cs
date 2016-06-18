﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicDatabase.EntityFramework;
using MusicDatabase.Model;
using MusicDatabase.Services.Repositories;

namespace MusicDatabase.Services
{
    public class ReleasesService
    {
        public EntityRepository Repositiory;
        public ReleasesService(EntityRepository repository)
        {
            Repositiory = repository;
        }

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