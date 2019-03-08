﻿/*
The MIT License (MIT)

Copyright (c) 2007 - 2019 microting

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.IO;
using System.Threading.Tasks;
using eFormAPI.Web.Abstractions;
using eFormShared;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microting.eFormApi.BasePn.Abstractions;
using Microting.eFormApi.BasePn.Infrastructure.Database.Entities;
using Microting.eFormApi.BasePn.Infrastructure.Helpers;

namespace eFormAPI.Web.Controllers
{
    [Authorize]
    public class ImagesController : Controller
    {
        private readonly ILocalizationService _localizationService;
        private readonly IEFormCoreService _coreHelper;

        public ImagesController(IEFormCoreService coreHelper,
            ILocalizationService localizationService)
        {
            _coreHelper = coreHelper;
            _localizationService = localizationService;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/images/eform-images")]
        public async Task<IActionResult> GetImage(string fileName)
        {
            var filePath = PathHelper.GetEformSettingsImagesPath(fileName);
            string ext = Path.GetExtension(fileName).Replace(".", "");
            if (ext == "jpg")
            {
                ext = "jpeg";
            }
            string fileType = $"image/{ext}";
            if (!System.IO.File.Exists(filePath))
            {
                
                var core = _coreHelper.GetCore();
                if (core.GetSdkSetting(Settings.swiftEnabled).ToLower() == "true")
                {
                    var result =  await core.GetFileFromStorageSystem(fileName);
                    return new FileStreamResult(result, fileType);
                }
                else
                {
                    return NotFound();
                }
                
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, fileType);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("api/images/login-page-images")]
        public async Task<IActionResult> GetLoginPageImage(string fileName)
        {
            var filePath = PathHelper.GetEformLoginPageSettingsImagesPath(fileName);
            string ext = Path.GetExtension(fileName).Replace(".", "");
            if (ext == "jpg")
            {
                ext = "jpeg";
            }
            string fileType = $"image/{ext}";
            if (!System.IO.File.Exists(filePath))
            {
                var core = _coreHelper.GetCore();
                if (core.GetSdkSetting(Settings.swiftEnabled).ToLower() == "true")
                {
                    var result =  await core.GetFileFromStorageSystem(fileName);
                    return new FileStreamResult(result, fileType);
                }
                else
                {
                    return NotFound();
                }
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, fileType);
        }

        [HttpPost]        
        [AllowAnonymous]
//        [Authorize(Roles = EformRole.Admin, AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [Route("api/images/login-page-images")]
        public async Task<IActionResult> PostLoginPageImages(IFormFile file)
        {
            var iUploadedCnt = 0;
            var saveFolder = PathHelper.GetEformLoginPageSettingsImagesPath();
            if (string.IsNullOrEmpty(saveFolder))
            {
                return BadRequest(_localizationService.GetString("FolderError"));
            }

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            if (file.Length > 0)
            {
                var filePath = Path.Combine(saveFolder, Path.GetFileName(file.FileName));
                if (!System.IO.File.Exists(filePath))
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);

                        var core = _coreHelper.GetCore();
                        if (core.GetSdkSetting(Settings.swiftEnabled).ToLower() == "true")
                        {
                            await core.PutFilToStorageSystem(filePath, file.FileName, 0);
                        }
                    }
                    iUploadedCnt++;
                }
            }

            if (iUploadedCnt > 0)
            {                
                return Ok();
            }
            return BadRequest(_localizationService.GetString("InvalidRequest"));
        }

        [HttpPost]       
        [AllowAnonymous]
//        [Authorize(Roles = EformRole.Admin, AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
        [Route("api/images/eform-images")]
        public async Task<IActionResult> PostEformImages(IFormFile file)
        {
            var iUploadedCnt = 0;
            var saveFolder = PathHelper.GetEformSettingsImagesPath();
            if (string.IsNullOrEmpty(saveFolder))
            {
                return BadRequest(_localizationService.GetString("FolderError"));
            }

            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            if (file.Length > 0)
            {
                var filePath = Path.Combine(saveFolder, Path.GetFileName(file.FileName));
                if (!System.IO.File.Exists(filePath))
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                        var core = _coreHelper.GetCore();
                        if (core.GetSdkSetting(Settings.swiftEnabled).ToLower() == "true")
                        {
                            await core.PutFilToStorageSystem(filePath, file.FileName, 0);
                        }
                    }
                    iUploadedCnt++;
                }
            }

            if (iUploadedCnt > 0)
            {
                return Ok();
            }
            return BadRequest(_localizationService.GetString("InvalidRequest"));
        }
    }
}