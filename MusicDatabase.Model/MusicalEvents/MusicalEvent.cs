﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicDatabase.Model
{
    public abstract class MusicalEvent : Entity
    {
        #region Properties
        public virtual Location Venue { get; set; }
        public string AlternateVenueName { get; set; }

        public DateTime? EventDate { get; set; }
        public string EventName { get; set; }
        public string Notes { get; set; }
        public virtual EventGroup EventGroup { get; set; }

        public virtual ICollection<Performance> Lineup { get; set; }
        public virtual ICollection<EventAttendee> OtherAttendees { get; set; }
        #endregion

        #region Constructors
        public MusicalEvent()
            : this(null, null)
        {

        }

        public MusicalEvent(DateTime? eventDate,  Location venue)
        {
            EventDate = eventDate;
            Venue = venue;

            Lineup = new List<Performance>();
            OtherAttendees = new List<EventAttendee>();
        }
        #endregion
    }
}
