﻿using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Ninject;
using PopForums.Services;
using PopForums.Web;

namespace PopForums.Controllers
{
	public class ImageController : Controller
	{
		public ImageController()
		{
			var container = PopForumsActivation.Kernel;
			ImageService = container.Get<IImageService>();
		}

		protected internal ImageController(IImageService imageService)
		{
			ImageService = imageService;
		}

		public IImageService ImageService { get; private set; }

		[PopForumsAuthorizationIgnore]
		public ActionResult Avatar(int id)
		{
			return SetupImageResult(ImageService.GetAvatarImageData, ImageService.GetAvatarImageLastModification, id);
		}

		[PopForumsAuthorizationIgnore]
		public ActionResult UserImage(int id)
		{
			return SetupImageResult(ImageService.GetUserImageData, ImageService.GetUserImageLastModifcation, id);
		}

		private ActionResult SetupImageResult(Func<int, byte[]> imageDataFetch, Func<int, DateTime?> imageLastMod, int id)
		{
			var timeStamp = imageLastMod(id);
			if (!timeStamp.HasValue)
				throw new HttpException(404, "Image not found");
			if (!String.IsNullOrEmpty(Request.Headers["If-Modified-Since"]))
			{
				var provider = CultureInfo.InvariantCulture;
				var lastMod = DateTime.ParseExact(Request.Headers["If-Modified-Since"], "r", provider);
				if (lastMod == timeStamp.Value.AddMilliseconds(-timeStamp.Value.Millisecond))
				{
					Response.StatusCode = 304;
					Response.StatusDescription = "Not Modified";
					return Content(String.Empty);
				}
			}
			var stream = new MemoryStream(imageDataFetch(id));
			Response.Cache.SetCacheability(HttpCacheability.Public);
			Response.Cache.SetLastModified(DateTime.SpecifyKind(timeStamp.Value, DateTimeKind.Utc));
			return File(stream, "image/jpeg");
		}
	}
}
