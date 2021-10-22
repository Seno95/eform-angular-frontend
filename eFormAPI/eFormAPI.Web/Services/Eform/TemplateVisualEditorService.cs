/*
The MIT License (MIT)
Copyright (c) 2007 - 2021 Microting A/S
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

namespace eFormAPI.Web.Services.Eform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Abstractions.Eforms;
    using eFormCore;
    using Infrastructure.Models.VisualEformEditor;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microting.eForm.Infrastructure;
    using Microting.eForm.Infrastructure.Constants;
    using Microting.eForm.Infrastructure.Data.Entities;
    using Microting.eFormApi.BasePn.Abstractions;
    using Microting.eFormApi.BasePn.Infrastructure.Models.API;
    using Microting.eFormApi.BasePn.Infrastructure.Models.Common;

    public class TemplateVisualEditorService : ITemplateVisualEditorService
    {
        private readonly IEFormCoreService _coreHelper;
        private readonly ILogger<TemplateVisualEditorService> _logger;
        private readonly ILocalizationService _localizationService;

        public TemplateVisualEditorService(
            IEFormCoreService coreHelper,
            ILogger<TemplateVisualEditorService> logger,
            ILocalizationService localizationService
        )
        {
            _coreHelper = coreHelper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<OperationDataResult<EformVisualEditorModel>> ReadVisualTemplate(int id)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                var sdkDbContext = core.DbContextHelper.GetDbContext();
                // if checklist have cases or pair - read not approve
                if (!await sdkDbContext.Cases.Where(x => x.CheckListId == id).Where(x => x.WorkflowState != Constants.WorkflowStates.Removed).AnyAsync()
                    || !await sdkDbContext.CheckListSites.Where(x => x.CheckListId == id).Where(x => x.WorkflowState != Constants.WorkflowStates.Removed).AnyAsync())
                {
                    var count = await sdkDbContext.CheckLists
                        .Where(x => x.Id == id)
                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                        .Include(x => x.Children)
                        .Select(x =>
                            x.Children.Where(y => y.WorkflowState != Constants.WorkflowStates.Removed).ToList())
                        .FirstOrDefaultAsync();
                    if (count?.Count == 1)
                    {
                        id += 1;
                    }

                    var eform = await FindTemplates(id, sdkDbContext);
                    if (count?.Count == 1)
                    {
                        var checklist = await sdkDbContext.CheckLists
                            .Where(x => x.Id == id - 1)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Include(x => x.Translations)
                            .Include(x => x.Taggings)
                            .Select(x => new
                            {
                                x.Taggings,
                                x.Translations,
                            })
                            .FirstOrDefaultAsync();
                        eform.Translations = checklist?.Translations
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Select(x => new CommonTranslationsModel
                            {
                                Id = x.Id,
                                Description = x.Description,
                                LanguageId = x.LanguageId,
                                Name = x.Text,
                            })
                            .ToList();
                        eform.TagIds = checklist?.Taggings
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Select(x => (int)x.TagId).ToList();
                    }

                    return new OperationDataResult<EformVisualEditorModel>(true, eform);
                }
                return new OperationDataResult<EformVisualEditorModel>(false,
                    _localizationService.GetString("EformHaveCaseOrPairWithSite"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new OperationDataResult<EformVisualEditorModel>(false,
                    _localizationService.GetString("ErrorWhileObtainingEform"));
            }
        }

        public async Task<OperationResult> CreateVisualTemplate(EformVisualEditorModel model)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                var sdkDbContext = core.DbContextHelper.GetDbContext();

                // create main checkList
                var newCheckList = new CheckList
                {
                    DisplayIndex = 0,
                    Repeated = 1,
                    ReviewEnabled = 0,
                    ManualSync = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
                    MultiApproval = 0,
                    FastNavigation = 0,
                    DownloadEntities = 0,
                    QuickSyncEnabled = 0,
                    IsEditable = true,
                    IsLocked = false
                };
                await newCheckList.Create(sdkDbContext);

                // create two checkList
                var twoCheckList = new CheckList
                {
                    DisplayIndex = 0,
                    ParentId = newCheckList.Id,
                    ReviewEnabled = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
                    IsEditable = true,
                    IsLocked = false
                };
                if (!model.CheckLists.Any())
                {
                    await twoCheckList.Create(sdkDbContext);
                }

                // create translations to eform
                foreach (var translation in model.Translations)
                {
                    var newCheckListTranslation = new CheckListTranslation
                    {
                        CheckListId = newCheckList.Id,
                        LanguageId = translation.LanguageId,
                        Text = translation.Name,
                        Description = translation.Description
                    };
                    await newCheckListTranslation.Create(sdkDbContext);

                    if (!model.CheckLists.Any())
                    {
                        var twoCheckListTranslation = new CheckListTranslation
                        {
                            CheckListId = twoCheckList.Id,
                            LanguageId = translation.LanguageId,
                            Text = translation.Name,
                            Description = translation.Description
                        };
                        await twoCheckListTranslation.Create(sdkDbContext);
                    }
                }

                // add tags to eform
                foreach (var tag in model.TagIds.Select(tagId => new Tagging { CheckListId = newCheckList.Id, TagId = tagId }))
                {
                    await tag.Create(sdkDbContext);
                }

                // add fields to eform
                if (!model.CheckLists.Any())
                {
                    await CreateFields(twoCheckList.Id, sdkDbContext, model.Fields, core);
                }
                else // create checklists
                {
                    await CreateChecklist(model.CheckLists, sdkDbContext, newCheckList.Id, core);
                }

                return new OperationResult(true,
                    _localizationService.GetString("EformSuccessfullyCreated"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new OperationDataResult<EformVisualEditorModel>(false,
                    _localizationService.GetString("ErrorWhileCreateEform"));
            }
        }

        public async Task<OperationResult> UpdateVisualTemplate(EformVisualEditorUpdateModel model)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                var sdkDbContext = core.DbContextHelper.GetDbContext();
                CheckList parentEform = null;
                var dbEform = await sdkDbContext.CheckLists
                    .Where(x => x.Id == model.Checklist.Id)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Include(x => x.Taggings)
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync();

                if (dbEform == null)
                {
                    return new OperationDataResult<EformVisualEditorModel>(false,
                        _localizationService.GetString("EformNotFound"));
                }

                if (dbEform.ParentId != null)
                {
                    parentEform = await sdkDbContext.CheckLists
                        .Where(x => x.Id == model.Checklist.Id - 1)
                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                        .Include(x => x.Taggings)
                        .Include(x => x.Translations)
                        .FirstOrDefaultAsync();
                }

                // create translations if need
                foreach (var newCheckListTranslation in model.Checklist.Translations
                    .Where(x => x.Id == null)
                    .Select(translation => new CheckListTranslation
                    {
                        CheckListId = dbEform.Id,
                        LanguageId = translation.LanguageId,
                        Text = translation.Name,
                        Description = translation.Description
                    }))
                {
                    await newCheckListTranslation.Create(sdkDbContext);
                }

                // update translations
                foreach (var translationsModel in model.Checklist.Translations
                    .Where(x => x.Id != null))
                {
                    var translation = dbEform.Translations.First(x => x.LanguageId == translationsModel.LanguageId);

                    translation.Text = translationsModel.Name;
                    translation.Description = translationsModel.Description;
                    await translation.Update(sdkDbContext);

                    var translationForUpdate = parentEform?.Translations
                        .FirstOrDefault(x => x.LanguageId == translationsModel.LanguageId && (x.Text != translationsModel.Name || x.Description != translationsModel.Description));
                    if (translationForUpdate != null)
                    {
                        translationForUpdate.Text = translationsModel.Name;
                        translationForUpdate.Description = translationsModel.Description;
                        await translationForUpdate.Update(sdkDbContext);
                    }
                }

                //tagging
                var eformWithTags = parentEform ?? dbEform;

                var tagsIdForDelete = eformWithTags.Taggings
                    .Select(x => x.Id)
                    .Where(id => !model.Checklist.TagIds.Contains(id))
                    .ToList();

                // remove tags from eform
                foreach (var tagId in tagsIdForDelete)
                {
                    var tagging = await sdkDbContext.Taggings
                        .Where(x => x.TagId == tagId && x.CheckListId == eformWithTags.Id)
                        .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                        .FirstOrDefaultAsync();
                    if (tagging != null)
                    {
                        await tagging.Delete(sdkDbContext);
                    }
                }

                // create tags for eform
                var tagsForCreate = model.Checklist.TagIds
                    .Where(id => !eformWithTags.Taggings.Select(x => x.Id).Contains(id))
                    .ToList();

                foreach (var tagId in tagsForCreate)
                {
                    var tagging = new Tagging
                    {
                        CheckListId = eformWithTags.Id,
                        TagId = tagId,
                    };
                    await tagging.Create(sdkDbContext);
                }

                if (model.FieldForDelete.Any())
                {
                    await DeleteFields(model.FieldForDelete, sdkDbContext); // delete removed field
                }

                if (model.FieldForUpdate.Any())
                {
                    await UpdateFields(model.FieldForUpdate, sdkDbContext, core); // update fields
                }

                var checklistIds = model.FieldForCreate
                    .Select(x => x.ChecklistId)
                    .Distinct()
                    .ToList();

                foreach (var checklistId in checklistIds)
                {
                    await CreateFields(checklistId, sdkDbContext, model.FieldForCreate.Where(x => x.ChecklistId == checklistId).ToList(), core); // create new field
                }

                if (model.ChecklistForDelete.Any())
                {
                    await DeleteCheckLists(model.ChecklistForDelete, sdkDbContext); // delete checklists
                }

                if (model.ChecklistForUpdate.Any())
                {
                    await UpdateChecklist(model.ChecklistForUpdate, sdkDbContext); // update checklists
                }

                if (model.ChecklistForCreate.Any())
                {
                    await CreateChecklist(model, sdkDbContext, core); // create new checklists
                }

                return new OperationResult(true,
                    _localizationService.GetString("EformSuccessfullyUpdated"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new OperationDataResult<EformVisualEditorModel>(false,
                    _localizationService.GetString("ErrorWhileUpdateEform"));
            }
        }

        private static async Task DeleteCheckLists(List<int> checklistsForDelete, MicrotingDbContext sdkDbContext)
        {
            var eforms = await sdkDbContext.CheckLists
                .Where(x => checklistsForDelete.Contains(x.Id))
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Include(x => x.Translations)
                .Include(x => x.Fields)
                .Include(x => x.Children)
                .ToListAsync();

            foreach (var eformForDelete in eforms)
            {
                if (eformForDelete.Children.Any()) // delete child checklist, if need
                {
                    await DeleteCheckLists(eformForDelete.Children.Select(x => x.Id).ToList(), sdkDbContext);
                }

                await DeleteFields(eformForDelete.Fields.Select(x => x.Id).ToList(), sdkDbContext); // delete fields
                foreach (var checkListTranslation in eformForDelete.Translations) // delete translations
                {
                    await checkListTranslation.Delete(sdkDbContext);
                }

                await eformForDelete.Delete(sdkDbContext); // delete eform
            }
        }

        private static async Task UpdateChecklist(List<EformVisualEditorModel> checklistForUpdate, MicrotingDbContext sdkDbContext)
        {
            foreach (var visualEditorModel in checklistForUpdate)
            {
                var eform = await sdkDbContext.CheckLists
                    .Where(x => x.Id == visualEditorModel.Id)
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync();

                if (eform != null)
                {
                    eform.DisplayIndex = visualEditorModel.Position;
                    await eform.Update(sdkDbContext);

                    // create translations if need
                    foreach (var newCheckListTranslation in visualEditorModel.Translations
                        .Where(x => x.Id == null)
                        .Select(translation => new CheckListTranslation
                        {
                            CheckListId = eform.Id,
                            LanguageId = translation.LanguageId,
                            Text = translation.Name,
                            Description = translation.Description
                        }))
                    {
                        await newCheckListTranslation.Create(sdkDbContext);
                    }

                    // update translations
                    foreach (var translationsModel in visualEditorModel.Translations
                        .Where(x => x.Id != null))
                    {
                        var translation = eform.Translations.FirstOrDefault(x => x.LanguageId == translationsModel.LanguageId);
                        if (translation != null &&
                            (translation.Text != translationsModel.Name ||
                             translation.Description != translationsModel.Description)) // check if update is need
                        {
                            translation.Text = translationsModel.Name;
                            translation.Description = translationsModel.Description;
                            await translation.Update(sdkDbContext);
                        }
                    }
                }
            }
        }

        private static async Task UpdateFields(List<VisualEditorFields> fieldsForUpdate, MicrotingDbContext sdkDbContext, Core core)
        {
            foreach (var fieldForUpdate in fieldsForUpdate)
            {
                var fieldFromDb = await sdkDbContext.Fields
                    .Where(x => x.Id == fieldForUpdate.Id)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Include(x => x.Translations)
                    .Include(x => x.FieldOptions)
                    .ThenInclude(x => x.FieldOptionTranslations)
                    .Include(x => x.FieldType)
                    .FirstAsync();

                fieldFromDb.Color = fieldForUpdate.Color;
                fieldFromDb.FieldTypeId = fieldForUpdate.FieldType;
                fieldFromDb.Mandatory = Convert.ToInt16(fieldForUpdate.Mandatory);
                fieldFromDb.DecimalCount = fieldForUpdate.DecimalCount;
                fieldFromDb.DisplayIndex = fieldForUpdate.Position;
                fieldFromDb.MaxValue = fieldForUpdate.MaxValue;
                fieldFromDb.MinValue = fieldForUpdate.MinValue;
                fieldFromDb.DefaultValue = fieldForUpdate.Value;

                var hashAndLanguageIdList = new List<KeyValuePair<string, int>>();
                switch (fieldFromDb.FieldType.Type) // todo add specific behaviour for some fields
                {
                    case Constants.FieldTypes.SingleSelect or Constants.FieldTypes.MultiSelect:
                    {
                        var currentOptionIds = await sdkDbContext.FieldOptions.Where(x => x.FieldId == fieldFromDb.Id).Select(x => x.Id).ToListAsync();

                        var optionsForCreate = fieldForUpdate.Options
                            .Where(x => x.Id == null)
                            .Select(x => new FieldOption
                            {
                                FieldId = fieldFromDb.Id,
                                Selected = x.Selected,
                                DisplayOrder = x.DisplayOrder.ToString(),
                                Key = x.Key.ToString(),
                                FieldOptionTranslations = x.Translates
                                    .Select(y =>
                                        new FieldOptionTranslation
                                        {
                                            LanguageId = y.LanguageId,
                                            Text = y.Name
                                        })
                                    .ToList(),
                            })
                            .ToList();

                        var optionsForUpdate = fieldForUpdate.Options
                            .Where(y => y.Id != null)
                            .Select(x => new FieldOption
                            {
                                FieldId = fieldFromDb.Id,
                                Id = (int)x.Id,
                                Selected = x.Selected,
                                DisplayOrder = x.DisplayOrder.ToString(),
                                Key = x.Key.ToString(),
                                FieldOptionTranslations = x.Translates
                                    .Select(y => new FieldOptionTranslation
                                    {
                                        LanguageId = y.LanguageId,
                                        Text = y.Name,
                                        Id = (int)y.Id
                                    })
                                    .ToList(),
                            });

                        foreach (var fieldOption in optionsForUpdate)
                        {
                            foreach (var optionTranslation in fieldOption.FieldOptionTranslations)
                            {
                                var dbOptionTranslation =
                                    await sdkDbContext.FieldOptionTranslations.SingleOrDefaultAsync(x =>
                                        x.Id == optionTranslation.Id);
                                dbOptionTranslation.Text = optionTranslation.Text;
                                await dbOptionTranslation.Update(sdkDbContext);
                            }

                            currentOptionIds.Remove(fieldOption.Id);
                        }

                        foreach (var dbOption in optionsForCreate)
                        {
                            await dbOption.Create(sdkDbContext);
                        }

                        foreach (int currentOptionId in currentOptionIds)
                        {
                            FieldOption fieldOption =
                                await sdkDbContext.FieldOptions.SingleOrDefaultAsync(x => x.Id == currentOptionId);
                            await fieldOption.Delete(sdkDbContext);
                        }
                        break;
                    }
                    case Constants.FieldTypes.ShowPdf:
                    {
                        if (fieldForUpdate.PdfFiles.Any())
                        {
                            var folder = Path.Combine(Path.GetTempPath(), "templates",
                                Path.Combine("fields-pdf-files", fieldFromDb.CheckListId.ToString()));
                            Directory.CreateDirectory(folder);
                            foreach (var pdfFile in fieldForUpdate.PdfFiles)
                            {
                                if (pdfFile.File != null)
                                {
                                    var filePath = Path.Combine(folder, $"{DateTime.Now.Ticks}_{fieldFromDb.CheckListId}.pdf");
                                    using (var stream = new FileStream(filePath, FileMode.Create)) // if you replace using to await using - stream not start copy until it goes beyond the current block
                                    {
                                        await pdfFile.File.CopyToAsync(stream);
                                    }

                                    await core.PutFileToStorageSystem(filePath, pdfFile.File.FileName);
                                    hashAndLanguageIdList.Add(
                                        new KeyValuePair<string, int>(await core.PdfUpload(filePath),
                                            pdfFile.LanguageId));
                                }
                            }
                        }
                        break;
                    }
                }

                await fieldFromDb.Update(sdkDbContext);

                // translations
                var translations = await sdkDbContext.FieldTranslations
                    .Where(x => x.FieldId == fieldFromDb.Id)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .ToListAsync();

                // create new translations
                foreach (var translationsModel in fieldForUpdate.Translations
                    .Where(x => x.Id == null)
                    .Select(x => new FieldTranslation
                    {
                        FieldId = fieldFromDb.Id,
                        LanguageId = x.LanguageId,
                        Text = x.Name,
                        Description = x.Description,
                    })
                    .ToList())
                {
                    if (fieldFromDb.FieldType.Type == Constants.FieldTypes.ShowPdf)
                    {
                        var hash = hashAndLanguageIdList
                            .Where(x => x.Value == translationsModel.LanguageId)
                            .Select(x => x.Key)
                            .FirstOrDefault();
                        if (!string.IsNullOrEmpty(hash))
                        {
                            translationsModel.DefaultValue = hash; // for pdf
                        }
                    }
                    await translationsModel.Create(sdkDbContext);
                }

                // update translations
                foreach (var fieldTranslation in translations)
                {
                    var translation = fieldForUpdate.Translations.First(x => x.LanguageId == fieldTranslation.LanguageId);
                    if (translation.Name != fieldTranslation.Text ||
                        translation.Description != fieldTranslation.Description) // check if update is need
                    {
                        fieldTranslation.Description = translation.Description;
                        fieldTranslation.Text = translation.Name;

                        if (fieldFromDb.FieldType.Type == Constants.FieldTypes.ShowPdf)
                        {
                            var hash = hashAndLanguageIdList
                                .Where(x => x.Value == fieldTranslation.LanguageId)
                                .Select(x => x.Key)
                                .FirstOrDefault();
                            if (!string.IsNullOrEmpty(hash))
                            {
                                fieldTranslation.DefaultValue = hash; // for pdf
                            }
                        }
                        await fieldTranslation.Update(sdkDbContext);
                    }
                }

            }
        }

        private static async Task DeleteFields(List<int> fieldIdForDelete, MicrotingDbContext sdkDbContext)
        {

            foreach (var fieldId in fieldIdForDelete)
            {
                var fieldForDelete = await sdkDbContext.Fields
                    .Where(x => x.Id == fieldId)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync();
                foreach (var translate in fieldForDelete.Translations)
                {
                    await translate.Delete(sdkDbContext);
                }

                var fieldType = await sdkDbContext.FieldTypes
                    .Where(x => x.Id == fieldForDelete.FieldTypeId)
                    .Select(x => x.Type)
                    .FirstAsync();

                switch (fieldType)
                {
                    case Constants.FieldTypes.SingleSelect or Constants.FieldTypes.MultiSelect:
                    {
                        var options = sdkDbContext.FieldOptions
                            .Where(x => x.FieldId == fieldId)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Include(x => x.FieldOptionTranslations)
                            .ToList();
                        foreach (var optionTranslation in options.SelectMany(x => x.FieldOptionTranslations).ToList())
                        {
                            await optionTranslation.Delete(sdkDbContext);
                        }

                        foreach (var fieldOption in options)
                        {
                            await fieldOption.Delete(sdkDbContext);
                        }

                        break;
                    }
                    case Constants.FieldTypes.FieldGroup:
                    {
                        var fieldIds = await sdkDbContext.Fields
                            .Where(x => x.ParentFieldId == fieldForDelete.Id)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Select(x => x.Id)
                            .ToListAsync();
                        await DeleteFields(fieldIds, sdkDbContext);
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }

                await fieldForDelete.Delete(sdkDbContext);
            }
        }

        private static async Task<List<VisualEditorFields>> FindFields(int eformId, MicrotingDbContext sdkDbContext, int parentFieldId = -1)
        {
            var findFields = new List<VisualEditorFields>();
            var fieldQuery = sdkDbContext.Fields
                .Where(x => x.CheckListId == eformId)
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                .Include(x => x.FieldType)
                .Include(x => x.Translations)
                .OrderBy(x => x.DisplayIndex)
                .AsNoTracking();
            if (parentFieldId != -1)
            {
                fieldQuery = fieldQuery.Where(x => x.ParentFieldId == parentFieldId);
            }
            else
            {
                fieldQuery = fieldQuery.Where(x => x.ParentFieldId == null);
            }
            var fields = await fieldQuery
                .ToListAsync();

            foreach (var field in fields)
            {
                var editorField = new VisualEditorFields
                {
                    Id = field.Id,
                    Color = field.Color,
                    FieldType = (int)field.FieldTypeId,
                    Position = (int)field.DisplayIndex,
                    Translations = field.Translations.Select(x =>
                        new Microting.eForm.Infrastructure.Models.CommonTranslationsModel
                        {
                            Id = x.LanguageId,
                            Description = x.Description,
                            Name = x.Text,
                            LanguageId = x.LanguageId,
                        }).ToList(),
                    Mandatory = Convert.ToBoolean(field.Mandatory),
                    ChecklistId = (int)field.CheckListId,
                };

                switch (field.FieldType.Type)
                {
                    case Constants.FieldTypes.Number or Constants.FieldTypes.NumberStepper:
                    {
                        editorField.DecimalCount = field.DecimalCount;
                        editorField.MinValue = field.MinValue; //== null ? field.MinValue : long.Parse(field.MinValue);
                        editorField.MaxValue = field.MaxValue; //== null ? field.MaxValue : long.Parse(field.MaxValue);
                        editorField.Value = field.DefaultValue; //== null ? field.DefaultValue : long.Parse(field.DefaultValue);
                        findFields.Add(editorField);
                        break;
                    }
                    case Constants.FieldTypes.SaveButton:
                    {
                        editorField.Value = field.DefaultValue;
                        findFields.Add(editorField);
                        break;
                    }
                    case Constants.FieldTypes.FieldGroup:
                    {
                        var fieldsInGroups = await FindFields(eformId, sdkDbContext, field.Id);
                        editorField.Fields = fieldsInGroups;
                        findFields.Add(editorField);
                        break;
                    }
                    case Constants.FieldTypes.Date:
                    {
                        editorField.MinValue = field.MinValue;// == null ? field.MinValue : DateTime.Parse(field.MinValue);
                        editorField.MaxValue = field.MaxValue;// == null ? field.MaxValue : DateTime.Parse(field.MaxValue);
                        findFields.Add(editorField);
                        break;
                    }
                    case Constants.FieldTypes.SingleSelect or Constants.FieldTypes.MultiSelect:
                    {
                        editorField.Options = sdkDbContext.FieldOptions
                            .Where(x => x.FieldId == field.Id)
                            .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                            .Include(x => x.FieldOptionTranslations)
                            .Select(x =>
                                new FieldOptions
                                {
                                    DisplayOrder = int.Parse(x.DisplayOrder),
                                    Id = x.Id,
                                    Key = int.Parse(x.Key),
                                    Selected = x.Selected,
                                    Translates = x.FieldOptionTranslations
                                        .Select(y =>
                                            new CommonTranslationsModel
                                            {
                                                Id = y.Id,
                                                Name = y.Text,
                                                LanguageId = y.LanguageId
                                            }).ToList()
                                })
                            .ToList();
                        findFields.Add(editorField);
                        break;
                    }
                    default:
                    {
                        findFields.Add(editorField);
                        break;
                    }
                }
            }

            return findFields;
        }

        private static async Task CreateFields(int eformId, MicrotingDbContext sdkDbContext,
            List<VisualEditorFields> fieldsList, Core core, int? parentFieldId = null)
        {
            foreach (var field in fieldsList)
            {
                var dbField = new Field
                {
                    CheckListId = eformId,
                    Color = field.Color,
                    FieldTypeId = field.FieldType,
                    DecimalCount = field.DecimalCount,
                    DefaultValue = field.Value ?? null,
                    DisplayIndex = field.Position,
                    MaxValue = field.MaxValue ?? null,
                    MinValue = field.MinValue ?? null,
                    Mandatory = Convert.ToInt16(field.Mandatory),
                    ParentFieldId = parentFieldId,
                };
                await dbField.Create(sdkDbContext);

                var fieldType = await sdkDbContext.FieldTypes
                    .Where(x => x.Id == field.FieldType)
                    .Select(x => x.Type)
                    .FirstAsync();
                var hashAndLanguageIdList = new List<KeyValuePair<string, int>>();

                switch (fieldType)
                {
                    case Constants.FieldTypes.SingleSelect or Constants.FieldTypes.MultiSelect:
                    {
                        var optionsForCreate = field.Options.Select(x =>
                                new FieldOption
                                {
                                    FieldId = dbField.Id,
                                    Selected = x.Selected,
                                    DisplayOrder = x.DisplayOrder.ToString(),
                                    Key = x.Key.ToString(),
                                    FieldOptionTranslations = x.Translates
                                        .Select(y =>
                                            new FieldOptionTranslation
                                            {
                                                LanguageId = y.LanguageId,
                                                Text = y.Name
                                            })
                                        .ToList(),
                                })
                            .ToList();
                        foreach (var dbOption in optionsForCreate)
                        {
                            await dbOption.Create(sdkDbContext);
                            // foreach (var optionTranslation in dbOption.FieldOptionTranslations)
                            // {
                            //     optionTranslation.FieldOptionId = dbOption.Id;
                            //     await optionTranslation.Create(sdkDbContext);
                            // }
                        }
                        break;
                    }
                    case Constants.FieldTypes.FieldGroup:
                    {
                        await CreateFields(eformId, sdkDbContext, field.Fields, core, dbField.Id);
                        break;
                    }
                    case Constants.FieldTypes.ShowPdf:
                    {
                        if (field.PdfFiles.Any())
                        {
                            var folder = Path.Combine(Path.GetTempPath(), "templates",
                                Path.Combine("fields-pdf-files", eformId.ToString()));
                            Directory.CreateDirectory(folder);
                            foreach (var pdfFile in field.PdfFiles)
                            {
                                if (pdfFile.File != null)
                                {
                                    var filePath = Path.Combine(folder, $"{DateTime.Now.Ticks}_{eformId}.pdf");
                                    using (var stream = new FileStream(filePath, FileMode.Create)) // if you replace using to await using - stream not start copy until it goes beyond the current block
                                    {
                                        await pdfFile.File.CopyToAsync(stream);
                                    }

                                    await core.PutFileToStorageSystem(filePath, pdfFile.File.FileName);
                                    hashAndLanguageIdList.Add(
                                        new KeyValuePair<string, int>(await core.PdfUpload(filePath),
                                            pdfFile.LanguageId));
                                }
                            }
                        }
                        break;
                    }
                    //case Constants.FieldTypes.Number or Constants.FieldTypes.NumberStepper:
                    //{
                    //    dbField.MaxValue = field.MaxValue == null ? field.MaxValue : long.Parse(field.MaxValue);
                    //    dbField.MinValue = field.MinValue == null ? field.MinValue : long.Parse(field.MinValue);
                    //    await dbField.Update(sdkDbContext);
                    //        break;
                    //}
                    //case Constants.FieldTypes.Date:
                    //{
                    //    dbField.MaxValue = field.MaxValue == null ? field.MaxValue : DateTime.Parse(field.MaxValue);
                    //    dbField.MinValue = field.MinValue == null ? field.MinValue : DateTime.Parse(field.MinValue);
                    //    await dbField.Update(sdkDbContext);
                    //        break;
                    //}
                    default:
                    {
                        break;
                    }
                }

                var translates = field.Translations
                    .Select(x =>
                        new FieldTranslation
                        {
                            FieldId = dbField.Id,
                            LanguageId = x.LanguageId,
                            Text = x.Name,
                            Description = x.Description,
                        }).ToList();
                foreach (var fieldTranslation in translates)
                {
                    if (fieldType == Constants.FieldTypes.ShowPdf)
                    {
                        var hash = hashAndLanguageIdList
                            .Where(x => x.Value == fieldTranslation.LanguageId)
                            .Select(x => x.Key)
                            .FirstOrDefault();
                        if (!string.IsNullOrEmpty(hash))
                        {
                            fieldTranslation.DefaultValue = hash; // for pdf
                        }
                    }
                    await fieldTranslation.Create(sdkDbContext);
                }
            }
        }

        private static async Task<EformVisualEditorModel> FindTemplates(int idEform, MicrotingDbContext sdkDbContext)
        {
            var query = sdkDbContext.CheckLists
                .Include(x => x.Translations)
                .Include(x => x.Taggings)
                .Where(x => x.Id == idEform)
                .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed);

            var eform = await query
                .Select(x => new EformVisualEditorModel
                {
                    Id = x.Id,
                    Position = (int)x.DisplayIndex,
                    Translations = x.Translations.Select(y =>
                            new CommonTranslationsModel
                            {
                                Name = y.Text,
                                Description = y.Description,
                                Id = y.Id,
                                LanguageId = y.LanguageId,
                            })
                        .ToList(),
                    TagIds = x.Taggings.Select(y => (int)y.TagId).ToList(),
                    Fields = new List<VisualEditorFields>(),
                })
                .FirstOrDefaultAsync();
            if (eform == null)
            {
                throw new Exception("EformNotFound");
            }

            // add fields
            eform.Fields = await FindFields(idEform, sdkDbContext);

            // add eforms
            var childrenCheckListIds = await query
                .Include(x => x.Children)
                .Select(x => x.Children.OrderBy(y => y.DisplayIndex).Select(y => y.Id).ToList())
                .FirstAsync();

            foreach (var checkListId in childrenCheckListIds)
            {
                eform.CheckLists.Add(await FindTemplates(checkListId, sdkDbContext));
            }

            return eform;
        }

        private static async Task CreateChecklist(EformVisualEditorUpdateModel model, MicrotingDbContext sdkDbContext, Core core)
        {
            foreach (var checklistForCreate in model.ChecklistForCreate)
            {
                // create checkList
                var newCheckList = new CheckList
                {
                    DisplayIndex = 0,
                    ParentId = checklistForCreate.ParentChecklistId,
                    ReviewEnabled = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
                    IsEditable = true,
                    IsLocked = false
                };
                await newCheckList.Create(sdkDbContext);

                // create translations to eform
                foreach (var newCheckListTranslation in checklistForCreate.Translations
                    .Select(translation => new CheckListTranslation
                    {
                        CheckListId = newCheckList.Id,
                        LanguageId = translation.LanguageId,
                        Text = translation.Name,
                        Description = translation.Description
                    }))
                {
                    await newCheckListTranslation.Create(sdkDbContext);
                }

                var fieldsForCreate = model.FieldForCreate
                    .Where(x => x.ChecklistId == checklistForCreate.TempId)
                    .ToList();

                await CreateWhenUpdateFields(newCheckList.Id, sdkDbContext, model, core);

                foreach (var eformVisualEditorModel in model.ChecklistForCreate.Where(x => x.ParentChecklistId == checklistForCreate.TempId))
                {
                    eformVisualEditorModel.ParentChecklistId = newCheckList.Id;
                }
            }
        }

        private static async Task CreateChecklist(List<EformVisualEditorModel> model, MicrotingDbContext sdkDbContext,
            int parentId, Core core)
        {
            foreach (var visualEditorModel in model)
            {

                var checkList = new CheckList
                {
                    DisplayIndex = 0,
                    ParentId = parentId,
                    ReviewEnabled = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
                    IsLocked = false,
                    IsEditable = true
                };
                await checkList.Create(sdkDbContext);

                foreach (var translation in visualEditorModel.Translations.Select(x => new CheckListTranslation
                {
                    CheckListId = checkList.Id,
                    LanguageId = x.LanguageId,
                    Text = x.Name,
                    Description = x.Description,
                }))
                {
                    await translation.Create(sdkDbContext);
                }
                if (visualEditorModel.CheckLists.Any())
                {
                    await CreateChecklist(visualEditorModel.CheckLists, sdkDbContext, checkList.Id, core);
                }
                else
                {
                    await CreateFields(checkList.Id, sdkDbContext, visualEditorModel.Fields, core);
                }
            }
        }

        private static async Task CreateWhenUpdateFields(int eformId, MicrotingDbContext sdkDbContext, EformVisualEditorUpdateModel model, Core core)
        {
            var fieldTypeGroup =
                await sdkDbContext.FieldTypes
                    .FirstAsync(x => x.Type == Constants.FieldTypes.FieldGroup);

            var fieldGroups = model.FieldForCreate
                .Where(x => x.FieldType == fieldTypeGroup.Id)
                .Where(x => x.ParentFieldId == null)
                .ToList();

            foreach (var fieldGroup in fieldGroups)
            {
                var dbField = new Field
                {
                    CheckListId = eformId,
                    Color = fieldGroup.Color,
                    FieldTypeId = fieldGroup.FieldType,
                    DecimalCount = fieldGroup.DecimalCount,
                    DefaultValue = fieldGroup.Value,
                    DisplayIndex = fieldGroup.Position,
                    MaxValue = fieldGroup.MaxValue,
                    MinValue = fieldGroup.MinValue,
                    Mandatory = Convert.ToInt16(fieldGroup.Mandatory),
                };
                await dbField.Create(sdkDbContext);

                var translates = fieldGroup.Translations
                    .Select(x =>
                        new FieldTranslation
                        {
                            FieldId = dbField.Id,
                            LanguageId = x.LanguageId,
                            Text = x.Name,
                            Description = x.Description,
                        }).ToList();
                foreach (var fieldTranslation in translates)
                {
                    await fieldTranslation.Create(sdkDbContext);
                }

                var fieldsForCreate = model.FieldForCreate
                    .Where(x => x.ParentFieldId == fieldGroup.TempId)
                    .ToList();
                await CreateFields(eformId, sdkDbContext, fieldsForCreate, core, dbField.Id); // child fields to create when updating the checklist are **not added to the group field**

                model.FieldForCreate = model.FieldForCreate
                    .Where(x => x.ParentFieldId != fieldGroup.TempId || x.TempId != fieldGroup.TempId) // delete created fields from mas
                    .ToList();
            }
            await CreateFields(eformId, sdkDbContext, model.FieldForCreate, core);

        }
    }
}