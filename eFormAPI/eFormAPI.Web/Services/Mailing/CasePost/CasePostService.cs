﻿/*
The MIT License (MIT)

Copyright (c) 2007 - 2019 Microting A/S

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
namespace eFormAPI.Web.Services.Mailing.CasePost
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Abstractions;
    using EmailService;
    using Hosting.Helpers.DbOptions;
    using Infrastructure.Const;
    using Infrastructure.Database;
    using Infrastructure.Database.Entities.Mailing;
    using Infrastructure.Models;
    using Infrastructure.Models.Mailing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microting.eForm.Dto;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eFormApi.BasePn.Abstractions;
    using Microting.eFormApi.BasePn.Infrastructure.Extensions;
    using Microting.eFormApi.BasePn.Infrastructure.Models.API;
    using Microting.eFormApi.BasePn.Infrastructure.Models.Application;
    using Microting.eFormApi.BasePn.Infrastructure.Models.Application.CasePosts;
    using OpenStack.NetCoreSwiftClient.Extensions;

    public class CasePostService : ICasePostService, ICasePostBaseService
    {
        private readonly ILogger<CasePostService> _logger;
        private readonly IUserService _userService;
        private readonly ILocalizationService _localizationService;
        private readonly IEFormCoreService _coreService;
        private readonly IEmailService _emailService;
        private readonly BaseDbContext _dbContext;
        private readonly IDbOptions<EmailSettings> _emailSettings;

        public CasePostService(
            ILogger<CasePostService> logger,
            IUserService userService,
            ILocalizationService localizationService,
            IEFormCoreService coreService,
            BaseDbContext dbContext,
            IEmailService emailService,
            IDbOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _userService = userService;
            _coreService = coreService;
            _localizationService = localizationService;
            _dbContext = dbContext;
            _emailService = emailService;
            _emailSettings = emailSettings;
        }

        public async Task<OperationDataResult<CasePostsListModel>> GetAllPosts(
            CasePostsRequest requestModel)
        {
            try
            {
                var casePostsListModel = new CasePostsListModel();
                var casePostsQuery = _dbContext.CasePosts.AsQueryable();
                if (!string.IsNullOrEmpty(requestModel.Sort))
                {
                    if (requestModel.IsSortDsc)
                    {
                        casePostsQuery = casePostsQuery
                            .CustomOrderByDescending(requestModel.Sort);
                    }
                    else
                    {
                        casePostsQuery = casePostsQuery
                            .CustomOrderBy(requestModel.Sort);
                    }
                }
                else
                {
                    casePostsQuery = casePostsQuery
                        .OrderBy(x => x.Id);
                }

                casePostsQuery = casePostsQuery
                    .Where(x => x.CaseId == requestModel.CaseId);

                casePostsListModel.Total = await casePostsQuery.CountAsync();

                casePostsQuery = casePostsQuery
                    .Skip(requestModel.Offset)
                    .Take(requestModel.PageSize);


                var core = await _coreService.GetCore();
                var templateDto = await core.TemplateItemRead(requestModel.TemplateId);
                var caseDto = await core.CaseLookupCaseId(requestModel.CaseId);
                if (caseDto?.MicrotingUId == null || caseDto.CheckUId == null)
                {
                    throw new InvalidOperationException("caseDto not found");
                }

                var replyElement = await core.CaseRead((int) caseDto.MicrotingUId, (int) caseDto.CheckUId);
                if (replyElement.DocxExportEnabled || replyElement.JasperExportEnabled)
                {
                    casePostsListModel.PdfReportAvailable = true;
                }

                var casePostList = await casePostsQuery
                    .Select(x => new CasePostModel()
                    {
                        Id = x.Id,
                        Subject = x.Subject,
                        Text = x.Text,
                        Date = x.PostDate,
                        From = _dbContext.Users
                            .Where(y => y.Id == x.CreatedByUserId)
                            .Select(y => $"{y.FirstName} {y.LastName}")
                            .FirstOrDefault(),
                        ToRecipients = x.Recipients
                            .Select(y => $"{y.EmailRecipient.Name} ({y.EmailRecipient.Email})")
                            .ToList(),
                        ToRecipientsTags = x.Tags
                            .Select(y => y.EmailTag.Name)
                            .ToList(),
                        
                    }).ToListAsync();

                using (var dbContext = core.dbContextHelper.GetDbContext())
                {
                    var caseEntity = await dbContext.cases
                        .AsNoTracking()
                        .Include(x => x.Site)
                        .SingleOrDefaultAsync(x => x.Id == requestModel.CaseId);

                    if (caseEntity == null)
                    {
                        return new OperationDataResult<CasePostsListModel>(
                            false,
                            _localizationService.GetString("CaseNotFound"));
                    }

                    if (caseEntity.Site?.MicrotingUid != null)
                    {
                        var site = await core.SiteRead((int) caseEntity.Site.MicrotingUid);
                        casePostsListModel.WorkerName = site.SiteName;
                    }

                    casePostsListModel.EFormName = templateDto.Label;

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue1))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field1.Label,
                                Value = caseEntity.FieldValue1
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue2))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field2.Label,
                                Value = caseEntity.FieldValue2
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue3))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field3.Label,
                                Value = caseEntity.FieldValue3
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue4))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field4.Label,
                                Value = caseEntity.FieldValue4
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue5))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field5.Label,
                                Value = caseEntity.FieldValue5
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue6))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field6.Label,
                                Value = caseEntity.FieldValue6
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue7))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field7.Label,
                                Value = caseEntity.FieldValue7
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue8))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field8.Label,
                                Value = caseEntity.FieldValue8
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue9))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field9.Label,
                                Value = caseEntity.FieldValue9
                            });
                    }

                    if (!string.IsNullOrEmpty(caseEntity.FieldValue10))
                    {
                        casePostsListModel.AdditionalFields.Add(
                            new KeyValueStringModel
                            {
                                Key = templateDto.Field10.Label,
                                Value = caseEntity.FieldValue10
                            });
                    }
                }

                casePostsListModel.CasePostsList = casePostList;
                return new OperationDataResult<CasePostsListModel>(true, casePostsListModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationDataResult<CasePostsListModel>(false,
                    _localizationService.GetString("ErrorWhileObtainingPosts"));
            }
        }

        public async Task<OperationDataResult<CasePostViewModel>> GetPostForView(int id)
        {
            try
            {
                var casePost = await _dbContext.CasePosts
                    .Where(x => x.Id == id)
                    .Select(x => new CasePostViewModel
                    {
                        Id = x.Id,
                        Text = x.Text,
                        Subject = x.Subject,
                        AttachReport = x.AttachPdf,
                        AttachLinkToCase = x.LinkToCase,
                        ToRecipients = x.Recipients
                            .Select(y => $"{y.EmailRecipient.Name} ({y.EmailRecipient.Email})")
                            .ToList(),
                        ToRecipientsTags = x.Tags
                            .Select(y => y.EmailTag.Name)
                            .ToList(),
                    }).FirstOrDefaultAsync();

                if (casePost == null)
                {
                    return new OperationDataResult<CasePostViewModel>(
                        false,
                        _localizationService.GetString("PostNotFound"));
                }

                return new OperationDataResult<CasePostViewModel>(true, casePost);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationDataResult<CasePostViewModel>(false,
                    _localizationService.GetString("ErrorWhileObtainingPostViewInfo"));
            }
        }


        public async Task<OperationResult> CreatePost(CasePostCreateModel requestModel)
        {
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (string.IsNullOrEmpty(_emailSettings.Value.SendGridKey))
                    {
                        transaction.Rollback();
                        return new OperationResult(false,
                            _localizationService.GetString("SendGridKeyShouldBeAddedToSettings"));
                    }

                    var casePost = new CasePost
                    {
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Version = 1,
                        CreatedByUserId = _userService.UserId,
                        UpdatedByUserId = _userService.UserId,
                        AttachPdf = requestModel.AttachReport,
                        LinkToCase = requestModel.AttachLinkToCase,
                        Text = requestModel.Text,
                        Subject = requestModel.Subject,
                        CaseId = requestModel.CaseId,
                        PostDate = DateTime.UtcNow,
                    };

                    await _dbContext.CasePosts.AddAsync(casePost);
                    await _dbContext.SaveChangesAsync();

                    foreach (var tagsId in requestModel.ToTagsIds)
                    {
                        var casePostEmailTag = new CasePostEmailTag
                        {
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = 1,
                            CreatedByUserId = _userService.UserId,
                            UpdatedByUserId = _userService.UserId,
                            CasePostId = casePost.Id,
                            EmailTagId = tagsId,
                        };

                        await _dbContext.CasePostEmailTags.AddAsync(casePostEmailTag);
                    }

                    foreach (var recipientId in requestModel.ToRecipientsIds)
                    {
                        var casePostEmailRecipient = new CasePostEmailRecipient()
                        {
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Version = 1,
                            CreatedByUserId = _userService.UserId,
                            UpdatedByUserId = _userService.UserId,
                            CasePostId = casePost.Id,
                            EmailRecipientId = recipientId,
                        };

                        await _dbContext.CasePostEmailRecipients.AddAsync(casePostEmailRecipient);
                    }

                    await _dbContext.SaveChangesAsync();

                    // Send email
                    var currentUser = await _userService.GetCurrentUserAsync();

                    var casePostRecipientResult = await _dbContext.CasePosts
                        .AsNoTracking()
                        .Where(x => x.Id == casePost.Id)
                        .Select(x => new
                        {
                            Recipients = x.Recipients
                                .Select(y => y.EmailRecipient)
                                .ToList(),
                            EmailTags = x.Tags
                                .Select(y => y.EmailTagId)
                                .ToList()
                        })
                        .FirstOrDefaultAsync();

                    var emailTagsRecipients = await _dbContext.EmailTagRecipients
                        .Where(x => casePostRecipientResult.EmailTags.Contains(x.EmailTagId))
                        .Select(x => x.EmailRecipient)
                        .ToListAsync();

                    var recipients = new List<EmailRecipient>();
                    recipients.AddRange(casePostRecipientResult.Recipients);
                    recipients.AddRange(emailTagsRecipients);

                    var core = await _coreService.GetCore();
                    var caseDto = await core.CaseLookupCaseId(casePost.CaseId);
                    if (caseDto?.MicrotingUId == null || caseDto.CheckUId == null)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("caseDto not found");
                    }
                    var replyElement = await core.CaseRead((int) caseDto.MicrotingUId, (int) caseDto.CheckUId);
                    var assembly = Assembly.GetExecutingAssembly();
                    var assemblyName = assembly.GetName().Name;
                    var stream = assembly.GetManifestResourceStream($"{assemblyName}.Resources.Email.html");
                    string html;
                    if (stream == null)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Resource not found");
                    }
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        html = await reader.ReadToEndAsync();
                    }

                    if (casePost.LinkToCase)
                    {
                        html = html
                            .Replace("{{link}}",
                                $"{await core.GetSdkSetting(Settings.httpServerAddress)}/cases/edit/{casePost.CaseId}/{caseDto.CheckListId}")
                            .Replace("{{text}}", casePost.Text);
                    }
                    else
                    {
                        html = casePost.Text;
                    }

                    foreach (var recipient in recipients
                        .Where(r => r.WorkflowState != Constants.WorkflowStates.Removed)
                        .GroupBy(x => new { x.Email, x.Name})
                        .Select(x=> x.Key)
                        .ToList())
                    {
                        if (casePost.AttachPdf)
                        {
                            try
                            {
                                // Fix for broken SDK not handling empty customXmlContent well
                                var customXmlContent = new XElement("FillerElement",
                                    new XElement("InnerElement", "SomeValue")).ToString();

                                // get report file
                                var filePath = await core.CaseToPdf(
                                    casePost.CaseId,
                                    replyElement.Id.ToString(),
                                    DateTime.Now.ToString("yyyyMMddHHmmssffff"),
                                    $"{await core.GetSdkSetting(Settings.httpServerAddress)}/" +
                                    "api/template-files/get-image/",
                                    "pdf",
                                    customXmlContent);

                                if (!File.Exists(filePath))
                                {
                                    throw new Exception("Error while creating report file");
                                }

                                await _emailService.SendFileAsync(
                                    EformEmailConst.FromEmail,
                                    $"{currentUser.FirstName} {currentUser.LastName}",
                                    casePost.Subject.IsNullOrEmpty() ? "-" : casePost.Subject,
                                    recipient.Email,
                                    filePath,
                                    html: html);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                await _emailService.SendAsync(
                                    EformEmailConst.FromEmail,
                                    $"{currentUser.FirstName} {currentUser.LastName}",
                                    casePost.Subject.IsNullOrEmpty() ? "-" : casePost.Subject,
                                    recipient.Email,
                                    html: html);
                            }
                        }
                        else
                        {
                            await _emailService.SendAsync(
                                EformEmailConst.FromEmail,
                                $"{currentUser.FirstName} {currentUser.LastName}",
                                casePost.Subject.IsNullOrEmpty() ? "-" : casePost.Subject,
                                recipient.Email,
                                html: html);
                        }
                    }

                    transaction.Commit();
                    return new OperationResult(
                        true,
                        _localizationService.GetString("PostCreatedSuccessfully"));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _logger.LogError(e.Message);
                    transaction.Rollback();
                    return new OperationResult(false,
                        _localizationService.GetString("ErrorWhileCreatingPost"));
                }
            }
        }

        public async Task<OperationDataResult<CasePostsCommonModel>> GetCommonPosts(CasePostsRequestCommonModel requestModel)
        {
            try
            {
                var casePostsListModel = new CasePostsCommonModel();
                var casePostsQuery = _dbContext.CasePosts.AsQueryable();

                if (requestModel.CaseId != null)
                {
                    casePostsQuery = casePostsQuery
                        .Where(x => x.CaseId == requestModel.CaseId);
                }

                if (requestModel.TemplateId != null)
                {
                    var core = await _coreService.GetCore();
                    await using var microtingDbContext = core.dbContextHelper.GetDbContext();
                    var casesIds = await microtingDbContext.cases
                        .Where(x => x.CheckListId == requestModel.TemplateId)
                        .Select(x => x.Id)
                        .ToListAsync();

                    casePostsQuery = casePostsQuery
                        .Where(x => casesIds.Contains(x.CaseId));
                }

                casePostsListModel.Total = await casePostsQuery.CountAsync();

                casePostsQuery = casePostsQuery
                    .Skip(requestModel.Offset)
                    .Take(requestModel.PageSize);

                casePostsListModel.Entities = await casePostsQuery
                    .Select(x => new CasePostCommonModel()
                    {
                        Id = x.Id,
                        Subject = x.Subject,
                        Text = x.Text,
                        PostDate = x.PostDate,
                        ToRecipients = x.Recipients
                            .Select(y => $"{y.EmailRecipient.Name} ({y.EmailRecipient.Email})")
                            .ToList(),
                        ToRecipientsTags = x.Tags
                            .Select(y => y.EmailTag.Name)
                            .ToList(),
                    }).ToListAsync();

                return new OperationDataResult<CasePostsCommonModel>(
                    true,
                    casePostsListModel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.LogError(e.Message);
                return new OperationDataResult<CasePostsCommonModel>(
                    false,
                    _localizationService.GetString("ErrorWhileObtainingPosts"));
            }
        }
    }
}