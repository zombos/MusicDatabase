﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicDatabase.Model
{
    // MusicalEvents have a lineup, which is a collection of Performances.

    // The basic Performance object contains all of the details involved with Performances, but is also extended
    // further into two additional types, Headliner and Support, which indicate a MusicalEntity's status at the a
    // Concert. Performances are ordered via the Position property, with the final performer of the night at 
    // listed first, and working backwards from there.

    // A MusicalEvent can have multiple Headliners (For example, the Queens of the Stone Age and Nine Inch Nails
    // co-headlining shows together, and playing their own individual sets).

    // There is currently no checking enforced on this, but as a standard, Concerts have Headliners and Supports, 
    // while Festivals have Headliners and Performances.

    // At this point in time, only peformances that I saw are included in the Lineup (So, if I missed a Support 
    // or had to leave before the Headliner, then they aren't included in the Lineup). If need be, it shouldn't be
    // too difficult to include all Performances, but add a "Did Not See" boolean or something.

    // The PerformingAs property covers when MusicalEntities will go under different names
    // For example, Devin Townsend performing as "Devin Townsend Project" or Something for Kate doing secret shows
    // under alternate names, like "George Kaplan and the Editors" or "Jerry and the Manmade Sharks".

    // A Performance also has collection of Performers, as a Performance can have multiple MusicalEntities involved: 
    // For example, Suzannah Espie and Ian Collard, two individual musicians performing an acoustic set together. 
    // Performers are also numbered (via the Position property) in the order they should be displayed

    // This can eventually go further, with fully-fledged recording and touring collaborations between MusicalEntities
    // For example: Miley Cyrus and Her Dead Petz (Miley Cyrus and The Flaming Lips) or Neil Young + The Promise of
    // the Real. These become standalone MusicalEntities, though at some point, these will be further wired up
    // to include references back to the contributing Entities, with the goal of including the collaborative 
    // Releases and Performances in each's own Discography and Performance lists. (Marked appropriately, of course).


    public class Performance
    {
        #region Properties
        public Guid ID { get; set; }
        public int? Position { get; set; }

        public virtual ICollection<Performer> Performers { get; set; }
        public string PerformingAs { get; set; }

        public virtual MusicalEvent Event { get; set; }
        public string Notes { get; set; }
        #endregion

        #region Constructors
        public Performance()
            : this(0, null, null)
        {

        }

        public Performance(int position, MusicalEvent musicalEvent, MusicalEntity musicalEntity)
        {
            ID = Guid.NewGuid();
            Position = position;
            Performers = new List<Performer>();

            if (musicalEntity != null)
                Performers.Add(new Performer(Performers.Count() + 1, musicalEntity));

            Event = musicalEvent;
        }
        #endregion
    }

    public class Headliner : Performance
    {
        public Headliner()
        {

        }

        public Headliner(int position, MusicalEvent musicalEvent, MusicalEntity musicalEntity)
            : base(position, musicalEvent, musicalEntity)
        {

        }
    }

    public class Support : Performance
    {
        public Support()
        {

        }

        public Support(int position, MusicalEvent musicalEvent, MusicalEntity musicalEntity)
            : base(position, musicalEvent, musicalEntity)
        {

        }
    }

    public class Performer
    {
        #region Properties
        public Guid ID { get; set; }
        public int Position { get; set; }
        public MusicalEntity MusicalEntity { get; set; }
        #endregion

        #region Constructors
        public Performer()
            : this(1, null)
        {

        }

        public Performer(int position, MusicalEntity musicalEntity)
        {
            ID = Guid.NewGuid();
            Position = position;
            MusicalEntity = musicalEntity;
        }
        #endregion
    }
}