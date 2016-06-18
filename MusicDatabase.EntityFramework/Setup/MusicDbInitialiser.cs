﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using MusicDatabase.Model;
using System.Configuration;

namespace MusicDatabase.EntityFramework
{
    public class MusicDbInitialiser : System.Data.Entity.DropCreateDatabaseAlways<MusicDbContext>
    {
        protected override void Seed(MusicDbContext context)
        {
            CreateReleaseFormats(context);
            ImportDataFromOldDatabase(context);
        }

        private void CreateReleaseFormats(MusicDbContext context)
        {
            var formats = new List<Format> {
                new Format("LP", "12\" Vinyl"),
                new Format("10\"", "10\" Vinyl"),
                new Format("7\"", "7\" Vinyl"),
                new Format("Flexi", "Flexidisc"),
                new Format("CD", "Compact Disc"),
                new Format("DVD"),
                new Format("Cass", "Cassette"),
                new Format("D/L", "Download"),
                new Format("D/L Card", "Download Card")
            };

            context.Set<Format>().AddRange(formats);
            context.SaveChanges();
        }

        private Dictionary<int, MusicalEntity> ImportMusicalEntities(MusicDbContext context, DataTable artistData)
        {
            var musicalEntities = new Dictionary<int, MusicalEntity>();
            var artists = new List<Artist>();
            var musicalGroups = new List<MusicalGroup>();

            foreach (DataRow row in artistData.Rows)
            {
                int artistID = int.Parse(row["ArtistID"].ToString());
                string name = row["Name"].ToString();
                string sortName = row["SortName"].ToString();
                bool isGroup = Convert.ToBoolean(row["IsGroup"]);

                if (isGroup)
                {
                    var musicalGroup = new MusicalGroup(name, sortName);
                    musicalGroups.Add(musicalGroup);
                    musicalEntities.Add(artistID, musicalGroup);
                }
                else
                {
                    var artist = new Artist(name, sortName);
                    artists.Add(artist);
                    musicalEntities.Add(artistID, artist);
                }
            }

            context.Set<Artist>().AddRange(artists);
            context.Set<MusicalGroup>().AddRange(musicalGroups);

            return musicalEntities;
        }

        private List<Location> ImportLocations(MusicDbContext context, DataTable locationData)
        {
            var locations = new List<Location>();

            foreach(DataRow row in locationData.Rows)
            {
                string name = row["Name"].ToString();
                if(!string.IsNullOrWhiteSpace(name))
                { 
                    locations.Add(new Location(name, 
                        row["City"].ToString(), 
                        row["State"].ToString(), 
                        row["Country"].ToString()));
                }
            }

            context.Set<Location>().AddRange(locations);
            context.SaveChanges();

            return locations;
        }

        private List<Website> ImportWebsites(MusicDbContext context, DataTable websiteData)
        {
            var websites = new List<Website>();

            foreach (DataRow row in websiteData.Rows)
            {
                string name = row["Name"].ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    websites.Add(new Website(name));
            }

            context.Set<Website>().AddRange(websites);
            context.SaveChanges();

            return websites;
        }

        private List<Person> ImportPeople(MusicDbContext context, DataTable peopleData)
        {
            var people = new List<Person>();

            foreach(DataRow row in peopleData.Rows)
            {
                string name = row["Name"].ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    people.Add(new Person(name, row["Notes"].ToString()));
            }

            context.Set<Person>().AddRange(people);
            context.SaveChanges();

            return people;
        }

        private List<Concert> ImportConcerts(MusicDbContext context, Dictionary<int, MusicalEntity> musicalEntities, 
            List<Location> locations, List<Person> people, DataTable concertData)
        {
            var concerts = new List<Concert>();
            var performances = new List<Performance>();

            foreach (DataRow row in concertData.Rows)
            {
                // Retrieve Date
                DateTime date = DateTime.Parse(row["Date"].ToString());
                
                // Retrieve Location
                Location venue = locations.Find(l => l.Name == row["Venue"].ToString());

                var concert = new Concert(date, venue);
                int billPosition = 1;

                // Headliners
                string[] headliners = row["Headliners"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var headlinerName in headliners)
                {
                    var headliner = musicalEntities.Values.First(e => e.Name == headlinerName);
                    var performance = new Headliner(billPosition, concert, headliner);

                    concert.Lineup.Add(performance);
                    headliner.Performances.Add(performance);

                    billPosition++;
                }

                // Support
                string[] supports = row["Supports"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var supportName in supports)
                {
                    var support = musicalEntities.Values.First(e => e.Name == supportName);
                    var performance = new Support(billPosition, concert, support);

                    concert.Lineup.Add(performance);
                    support.Performances.Add(performance);

                    billPosition++;
                }

                // Attendees
                string[] attendees = row["With"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var attendeeName in attendees)
                {
                    var attendee = people.First(p => p.Name == attendeeName);
                    var attendance = new Attendance(attendee, concert);

                    attendee.MusicalEvents.Add(attendance);
                    concert.OtherAttendees.Add(attendance);
                }

                venue.MusicalEvents.Add(concert);
                concerts.Add(concert);
            }

            context.Set<Concert>().AddRange(concerts);
            context.Set<Performance>().AddRange(performances);
            context.SaveChanges();

            return concerts;
        }

        private List<Festival> ImportFestivals(MusicDbContext context, Dictionary<int, MusicalEntity> musicalEntities,
                    List<Location> locations, List<Person> people, DataTable festivalData)
        {
            var festivals = new List<Festival>();
            var performances = new List<Performance>();

            foreach (DataRow row in festivalData.Rows)
            {
                // Retrieve Date
                DateTime date = DateTime.Parse(row["Date"].ToString());

                // Retrieve Location
                Location venue = locations.Find(l => l.Name == row["Venue"].ToString());

                var festival = new Festival(date, row["Name"].ToString(), venue);
                int billPosition = 1;

                // Headliners
                string[] headliners = row["Headliners"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var headlinerName in headliners)
                {
                    var headliner = musicalEntities.Values.First(e => e.Name == headlinerName);
                    var performance = new Headliner(billPosition, festival, headliner);

                    festival.Lineup.Add(performance);
                    headliner.Performances.Add(performance);

                    billPosition++;
                }

                // Performers
                string[] performers = row["Performers"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var performerName in performers)
                {
                    var performer = musicalEntities.Values.First(e => e.Name == performerName);
                    var performance = new Performer(festival, performer);

                    festival.Lineup.Add(performance);
                    performer.Performances.Add(performance);
                }

                // Attendees
                string[] attendees = row["With"].ToString().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var attendeeName in attendees)
                {
                    var attendee = people.First(p => p.Name == attendeeName);
                    var attendance = new Attendance(attendee, festival);

                    attendee.MusicalEvents.Add(attendance);
                    festival.OtherAttendees.Add(attendance);
                }

                if (venue != null)
                    venue.MusicalEvents.Add(festival);

                festivals.Add(festival);
            }

            context.Set<Festival>().AddRange(festivals);
            context.Set<Performance>().AddRange(performances);
            context.SaveChanges();

            return festivals;
        }

        private void ImportDataFromOldDatabase(MusicDbContext context)
        {
            var artistData = new DataTable();
            var releaseData = new DataTable();
            var copyData = new DataTable();

            var peopleData = new DataTable();
            var locationData = new DataTable();
            var websiteData = new DataTable();
            var concertData = new DataTable();
            var festivalData = new DataTable();

            // Retrieve necessary data
            using (var sqlConnect = new MySqlConnection(ConfigurationManager.ConnectionStrings["mysql.musicdb"].ConnectionString))
            {
                // Retrieve Artists
                var sqlCommand = new MySqlCommand("SELECT * FROM rvwartist", sqlConnect);
                var sqlAdapter = new MySqlDataAdapter(sqlCommand);
                sqlAdapter.Fill(artistData);

                // Retrieve Releases
                sqlCommand = new MySqlCommand(
                    @"SELECT d.artistID, d.position, r.releaseID, r.name, r.year, 
                        r.releasedby, r.isEP, r.isCompilation, r.isSingle, r.isSoundtrack,
                        r.isLiveAlbum, r.isRemixAlbum
                    FROM rvwartistdiscography d, rvwrelease r 
                    WHERE d.releaseID = r.releaseID
                    ORDER BY d.artistID, d.position", sqlConnect);
                sqlAdapter = new MySqlDataAdapter(sqlCommand);
                sqlAdapter.Fill(releaseData);

                // Retrieve Copies
                sqlCommand = new MySqlCommand(
                    @"SELECT c.releaseID, c.purchaseDate, c.notes, c.formatID, 
                        c.price, c.markedPrice, c.salePrice, c.postage, c.calculateTax,
                        c.purchaseLocation, c.purchaseNotes, c.copyNotes,            
                        c.isGift, c.giftNotes, 
                        c.isCompetitionItem, c.competitionItemDetails,
                        c.isGigPurchase, c.gigHeadliner,
                        c.isOnlinePurchase, c.website,
                        c.includesDownloadCard, c.includesFlexidisc, c.includesCompactDiscs, c.includesDVDs, 
                        c.includesDownload, 
                        c.isSecondhand, c.isPledgeItem, c.isRecordStoreDayItem
                    FROM rvwreleasecopy c", sqlConnect);
                sqlAdapter = new MySqlDataAdapter(sqlCommand);
                sqlAdapter.Fill(copyData);
            }

            using (var oleDbConnect = new OleDbConnection(ConfigurationManager.ConnectionStrings["excel.import.data"].ConnectionString))
            {

                var oleDbCommand = new OleDbCommand("SELECT * FROM [People$]", oleDbConnect);
                var oleDbAdapter = new OleDbDataAdapter(oleDbCommand);
                oleDbAdapter.Fill(peopleData);

                oleDbCommand = new OleDbCommand("SELECT * FROM [Locations$]", oleDbConnect);
                oleDbAdapter = new OleDbDataAdapter(oleDbCommand);
                oleDbAdapter.Fill(locationData);

                oleDbCommand = new OleDbCommand("SELECT * FROM [Websites$]", oleDbConnect);
                oleDbAdapter = new OleDbDataAdapter(oleDbCommand);
                oleDbAdapter.Fill(websiteData);

                oleDbCommand = new OleDbCommand("SELECT * FROM [Concerts$]", oleDbConnect);
                oleDbAdapter = new OleDbDataAdapter(oleDbCommand);
                oleDbAdapter.Fill(concertData);

                oleDbCommand = new OleDbCommand("SELECT * FROM [Festivals$]", oleDbConnect);
                oleDbAdapter = new OleDbDataAdapter(oleDbCommand);
                oleDbAdapter.Fill(festivalData);
            }

            var musicalEntities = ImportMusicalEntities(context, artistData);
            var locations = ImportLocations(context, locationData);
            var websites = ImportWebsites(context, websiteData);
            var people = ImportPeople(context, peopleData);
            var concerts = ImportConcerts(context, musicalEntities, locations, people, concertData);
            var festivals = ImportFestivals(context, musicalEntities, locations, people, festivalData);

            List<Release> releases = new List<Release>();

            foreach (int artistID in musicalEntities.Keys)
            {
                var musicalEntity = musicalEntities[artistID];

                // Create Releases
                var artistReleases = releaseData.Select("artistID = " + artistID);
                foreach (var artistRelease in artistReleases)
                {
                    int releaseID = int.Parse(artistRelease["ReleaseID"].ToString());
                    string title = artistRelease["Name"].ToString();
                    int year = int.Parse(artistRelease["Year"].ToString());
                    int position = int.Parse(artistRelease["Position"].ToString());
                    string releasedBy = artistRelease["ReleasedBy"].ToString();

                    // Tidy up name
                    title = title.Replace(" [OST]", "");
                    title = title.Replace(" (7\")", "");
                    title = title.Replace(" (Single)", "");

                    var release = new Release(title, year);

                    // Released By
                    release.ReleasedBy = releasedBy;

                    // ReleaseType
                    bool isEP = Convert.ToBoolean(artistRelease["IsEP"]);
                    bool isSingle = Convert.ToBoolean(artistRelease["IsSingle"]);

                    var releaseType = ReleaseType.Album;
                    if (isEP)
                        releaseType = ReleaseType.EP;
                    if (isSingle)
                        releaseType = ReleaseType.Single;

                    release.ReleaseType = releaseType;

                    if (Convert.ToBoolean(artistRelease["IsCompilation"]))
                        release.Tags.Add(new Tag(Model.Constants.RELEASE_TAG_COMPILATION));

                    if (Convert.ToBoolean(artistRelease["IsSoundtrack"]))
                        release.Tags.Add(new Tag(Model.Constants.RELEASE_TAG_SOUNDTRACK));

                    if (Convert.ToBoolean(artistRelease["IsLiveAlbum"]))
                        release.Tags.Add(new Tag(Model.Constants.RELEASE_TAG_LIVE));

                    if (Convert.ToBoolean(artistRelease["IsRemixAlbum"]))
                        release.Tags.Add(new Tag(Model.Constants.RELEASE_TAG_REMIXES));

                    releases.Add(release);

                    // Create Copies
                    var releaseCopies = copyData.Select("releaseID = " + releaseID);
                    foreach (var releaseCopy in releaseCopies)
                    {
                        var copy = new Copy();

                        copy.Notes = releaseCopy["CopyNotes"].ToString();
                        copy.OldNotes = releaseCopy["Notes"].ToString();

                        var dateAdded = releaseCopy["PurchaseDate"] as DateTime?;

                        // Configure all of the acquisition details...
                        if (Convert.ToBoolean(releaseCopy["isGift"]))
                        {
                            var giftDetails = new GiftDetails();

                            string[] giftNotes = releaseCopy["GiftNotes"].ToString().Split(new char[] { ';' }, StringSplitOptions.None);

                            if (giftNotes.Length == 2)
                            {
                                giftDetails.Occasion = giftNotes[0];
                                giftDetails.From = giftNotes[1];
                            }

                            copy.AcquisitionDetails = giftDetails;
                        }
                        else if (Convert.ToBoolean(releaseCopy["isCompetitionItem"]))
                        {
                            var competitionItemDetails = new CompetitionItemDetails();
                            competitionItemDetails.From = releaseCopy["CompetitionItemDetails"].ToString();
                            copy.AcquisitionDetails = competitionItemDetails;
                        }
                        else 
                        {
                            PurchaseDetails purchaseDetails = null;
                            var purchaseLocation = locations.Find(l => l.Name == releaseCopy["PurchaseLocation"].ToString());

                            if (Convert.ToBoolean(releaseCopy["isOnlinePurchase"]))
                            {
                                var onlinePurchase = new OnlinePurchase();

                                // Try and pull the purchase date from the notes via Regex
                                // If you can, that becomes the purchase date, and the existing 
                                // purchase date becomes the received date
                                var matchedDate = Regex.Match(copy.OldNotes, @"(\d{2})/(\d{2})/(\d{4})");
                                if (matchedDate.Success)
                                {
                                    onlinePurchase.PurchaseDate = new DateTime(int.Parse(matchedDate.Groups[3].Value),
                                                                        int.Parse(matchedDate.Groups[2].Value),
                                                                        int.Parse(matchedDate.Groups[1].Value));
                                }

                                if (releaseCopy["Postage"] != DBNull.Value)
                                    onlinePurchase.Postage = decimal.Parse(releaseCopy["Postage"].ToString());

                                // Retrieve Website
                                if (releaseCopy["Website"] != DBNull.Value)
                                    onlinePurchase.Website = websites.Find(w => w.Name == releaseCopy["Website"].ToString());

                                purchaseDetails = onlinePurchase;
                            }
                            else if (Convert.ToBoolean(releaseCopy["isGigPurchase"]))
                            {
                                var eventPurchase = new EventPurchase();

                                // Find Event...
                                var musicalEvent = concerts.Find(c => (c.EventDate == dateAdded) &&
                                    (c.Venue == purchaseLocation) &&
                                    (c.Lineup.Any(h => h.MusicalEntity.SortName == releaseCopy["GigHeadliner"].ToString())));

                                if (musicalEvent != null)
                                    eventPurchase.Event = musicalEvent;

                                purchaseDetails = eventPurchase;
                            }
                            else
                            {
                                // By default, all copies are store purchases...
                                var storePurchase = new StorePurchase();
                                storePurchase.PurchaseLocation = locations.Find(l => l.Name == releaseCopy["PurchaseLocation"].ToString());
                                purchaseDetails = storePurchase;
                            }

                            // Setup the costs / prices...
                            if (releaseCopy["Price"] != DBNull.Value)
                                purchaseDetails.GrandTotal = decimal.Parse(releaseCopy["Price"].ToString());

                            if (releaseCopy["MarkedPrice"] != DBNull.Value)
                                purchaseDetails.MarkedPrice = decimal.Parse(releaseCopy["MarkedPrice"].ToString());
                            
                            if (releaseCopy["SalePrice"] != DBNull.Value)
                                purchaseDetails.SalePrice = decimal.Parse(releaseCopy["SalePrice"].ToString());

                            // Calculate Tax, if necessary
                            if (Convert.ToBoolean(releaseCopy["CalculateTax"]))
                            {
                                if ((purchaseDetails.GrandTotal.HasValue) && (purchaseDetails.GrandTotal > 0))
                                {
                                    if ((purchaseDetails.SalePrice != null) && (purchaseDetails.SalePrice < purchaseDetails.GrandTotal))
                                        purchaseDetails.Tax = purchaseDetails.GrandTotal - purchaseDetails.SalePrice;
                                    else if ((purchaseDetails.MarkedPrice != null) && (purchaseDetails.MarkedPrice < purchaseDetails.GrandTotal))
                                        purchaseDetails.Tax = purchaseDetails.GrandTotal - purchaseDetails.MarkedPrice;
                                }
                            }

                            copy.AcquisitionDetails = purchaseDetails;
                        }
                        
                        copy.AcquisitionDetails.DateAdded = dateAdded;
                        copy.AcquisitionDetails.Notes = releaseCopy["PurchaseNotes"].ToString().Trim();

                        // Add in additional descriptive tags
                        if (Convert.ToBoolean(releaseCopy["IsPledgeItem"]))
                            copy.Tags.Add(new Tag(Model.Constants.COPY_TAG_PLEDGE_ITEM));

                        if (Convert.ToBoolean(releaseCopy["IsSecondhand"]))
                            copy.Tags.Add(new Tag(Model.Constants.COPY_TAG_SECONDHAND));

                        if (Convert.ToBoolean(releaseCopy["IsRecordStoreDayItem"]))
                            copy.Tags.Add(new Tag(Model.Constants.COPY_TAG_RECORD_STORE_DAY_ITEM));

                        // Add in copy elements
                        int formatID = int.Parse(releaseCopy["FormatID"].ToString());
                        switch (formatID)
                        {
                            case 1:     // CD
                                AddElementToCopy(context, copy, 1, "CD", 1);
                                break;
                            case 2:     // CD + DVD
                                AddElementToCopy(context, copy, 1, "CD", 1);
                                AddElementToCopy(context, copy, 1, "DVD", 2);
                                break;
                            case 3:     // 2CD
                                AddElementToCopy(context, copy, 2, "CD", 1);
                                break;
                            case 4:     // 2CD + DVD
                                AddElementToCopy(context, copy, 2, "CD", 1);
                                AddElementToCopy(context, copy, 1, "DVD", 2);
                                break;
                            case 5:     // 2CD + 2DVD
                                AddElementToCopy(context, copy, 2, "CD", 1);
                                AddElementToCopy(context, copy, 2, "DVD", 2);
                                break;
                            case 6:     // 3CD
                                AddElementToCopy(context, copy, 3, "CD", 1);
                                break;
                            case 7:     // LP
                                AddElementToCopy(context, copy, 1, "LP", 1);
                                break;
                            case 8:     // 2LP
                                AddElementToCopy(context, copy, 2, "LP", 1);
                                break;
                            case 9:     // 3LP
                                AddElementToCopy(context, copy, 3, "LP", 1);
                                break;
                            case 10:    // 4LP
                                AddElementToCopy(context, copy, 4, "LP", 1);
                                break;
                            case 11:    // Cassette
                                AddElementToCopy(context, copy, 1, "Cass", 1);
                                break;
                            case 12:    // Download
                                AddElementToCopy(context, copy, 1, "D/L", 1);
                                break;
                            case 13:    // 5LP
                                AddElementToCopy(context, copy, 5, "LP", 1);
                                break;
                            case 14:    // 6LP
                                AddElementToCopy(context, copy, 6, "LP", 1);
                                break;
                            case 15:    // 7LP
                                AddElementToCopy(context, copy, 7, "LP", 1);
                                break;
                            case 16:    // 8LP
                                AddElementToCopy(context, copy, 8, "LP", 1);
                                break;
                            case 17:    // 7"
                                AddElementToCopy(context, copy, 1, "7\"", 1);
                                break;
                            case 18:    // 10"
                                AddElementToCopy(context, copy, 1, "10\"", 1);
                                break;
                            case 19:    // Flexidisc
                                AddElementToCopy(context, copy, 1, "Flexi", 1);
                                break;
                            case 20:    // Download Card
                                AddElementToCopy(context, copy, 1, "D/L Card", 1);
                                break;
                        }

                        // Plus any additional Elements...
                        bool includesDownloadCard = Convert.ToBoolean(releaseCopy["IncludesDownloadCard"]);
                        bool includesFlexidisc = Convert.ToBoolean(releaseCopy["IncludesFlexidisc"]);
                        int includesCompactDiscs = Convert.ToInt32(releaseCopy["IncludesCompactDiscs"]);
                        int includesDVDs = Convert.ToInt32(releaseCopy["includesDVDs"]);
                        bool includesDownload = Convert.ToBoolean(releaseCopy["IncludesDownload"]);

                        if ((includesCompactDiscs > 0) && (copy.Elements.Count(e => e.Format.Code == "CD") == 0))
                            AddElementToCopy(context, copy, includesCompactDiscs, "CD", copy.Elements.Count + 1);

                        if ((includesDVDs > 0) && (copy.Elements.Count(e => e.Format.Code == "DVD") == 0))
                            AddElementToCopy(context, copy, includesDVDs, "DVD", copy.Elements.Count + 1);

                        if ((includesFlexidisc) && (copy.Elements.Count(e => e.Format.Code == "Flexi") == 0))
                            AddElementToCopy(context, copy, 1, "Flexi", copy.Elements.Count + 1);

                        if ((includesDownloadCard) && (copy.Elements.Count(e => e.Format.Code == "D/L Card") == 0))
                            AddElementToCopy(context, copy, 1, "D/L Card", copy.Elements.Count + 1);

                        if ((includesDownload) && (copy.Elements.Count(e => e.Format.Code == "D/L") == 0))
                            AddElementToCopy(context, copy, 1, "D/L", copy.Elements.Count + 1);


                        // Finally, removal info... Hey, it has happened!
                        //DateTime? removedDate = releaseCopy["RemovedDate"] as DateTime?;
                        //if (removedDate.HasValue)
                        //{
                        //    copy.Removed = true;
                        //    copy.RemovedDate = removedDate;
                        //    copy.RemovalNotes = releaseCopy["RemovalNotes"].ToString();
                        //}

                        release.Copies.Add(copy);
                    }

                    // Create a Discography Entry
                    musicalEntity.Discography.Add(new DiscographyEntry(position, release));
                }
            }

            // Next, a bunch of painful split release stuff...

            // All of this is hard-coded, because... ahh, it's easier to just hard-code it all in, rather
            // than come up with some nice, elegant solution. I mean, presumably, once this has been completed
            // I won't need to run the import again, and it'll be fine just to have it hard-coded in.

            // Neil Finn / Paul Kelly
            var splitEntity = musicalEntities.Values.First(a => a.SortName == "Kelly, Paul");
            var splitRelease = releases.Find(r => r.Title == "Goin' Your Way");

            var entries = splitEntity.Discography.Where(e => e.Position >= 10);
            foreach (var entry in entries)
                entry.Position = entry.Position + 1;

            splitEntity.Discography.Add(new DiscographyEntry(10, splitRelease));

            // Create a group for Neil Finn + Paul Kelly as well. (Mainly for live purposes).
            splitEntity = musicalEntities.Values.First(g => g.SortName == "Neil Finn + Paul Kelly");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // Courtney Barnett / Jen Cloher
            splitEntity = musicalEntities.Values.First(a => a.SortName == "Cloher, Jen");

            splitRelease = releases.Find(r => r.Title == "History Eraser / Mount Beauty");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            splitRelease = releases.Find(r => r.Title == "Needle in the Hay / Swan Street Swagger");
            splitEntity.Discography.Add(new DiscographyEntry(2, splitRelease));

            // David Sylvian / Robert Fripp
            splitEntity = musicalEntities.Values.First(a => a.SortName == "Fripp, Robert");
            splitRelease = releases.Find(r => r.Title == "The First Day");

            splitEntity.Discography.Add(new DiscographyEntry(splitEntity.Discography.Count + 1, splitRelease));

            // David Byrne and Brian Eno
            splitEntity = musicalEntities.Values.First(a => a.SortName == "Eno, Brian");
            splitRelease = releases.Find(r => r.Title == "Everything That Happens Will Happen Today");

            splitEntity.Discography.Add(new DiscographyEntry(splitEntity.Discography.Count + 1, splitRelease));

            // Brian Eno and David Byrne
            splitEntity = musicalEntities.Values.First(a => a.SortName == "Byrne, David");
            splitRelease = releases.Find(r => r.Title == "My Life in the Bush of Ghosts");

            foreach (var entry in splitEntity.Discography)
                entry.Position = entry.Position + 1;

            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // Al Di Meola, John McLaughlin and Paco de Lucía
            splitRelease = releases.Find(r => r.Title == "Friday Night in San Francisco");

            splitEntity = musicalEntities.Values.First(a => a.SortName == "McLaughlin, John");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            splitEntity = musicalEntities.Values.First(a => a.SortName == "de Lucía, Paco");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // John Lennon and Yoko Ono
            splitEntity = musicalEntities.Values.First(a => a.SortName == "Ono, Yoko");
            splitRelease = releases.Find(r => r.Title == "Double Fantasy");

            foreach (var entry in splitEntity.Discography)
                entry.Position = entry.Position + 1;

            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // Suuns and Jerusalem in My Heart
            splitRelease = releases.Find(r => r.Title == "Suuns and Jerusalem in My Heart");

            splitEntity = musicalEntities.Values.First(a => a.SortName == "Jerusalem in My Heart");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            splitEntity = musicalEntities.Values.First(g => g.SortName == "Suuns");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // Electric Light Orchestra and Olivia Newton-John 
            splitRelease = releases.Find(r => r.Title == "Xanadu");

            splitEntity = musicalEntities.Values.First(a => a.SortName == "Newton-John, Olivia");
            splitEntity.Discography.Add(new DiscographyEntry(1, splitRelease));

            // 801 / Phil Manzanera
            splitRelease = releases.Find(r => r.Title == "Listen Now");

            splitEntity = musicalEntities.Values.First(a => a.SortName == "Manzanera, Phil");
            splitEntity.Discography.Add(new DiscographyEntry(2, splitRelease));

            // Save everything to the database
            context.Set<Release>().AddRange(releases);
            context.SaveChanges();
        }

        private void AddElementToCopy(MusicDbContext context, Copy copy, int count, string formatCode, int position)
        {
            var format = context.Set<Format>().First(f => f.Code == formatCode);
            copy.Elements.Add(new Element(count, format, position));
        }
    }
}