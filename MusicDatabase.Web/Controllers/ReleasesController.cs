﻿using System;
using System.Web.Mvc;
using MusicDatabase.Services;

namespace MusicDatabase.Web.Controllers
{
    public class ReleasesController : Controller
    {
        // GET: Releases
        public ActionResult Index()
        {
            return View();
        }
    }
}