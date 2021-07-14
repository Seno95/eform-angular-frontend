﻿/*
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
    using System.Linq;
    using System.Threading.Tasks;
    using Abstractions;
    using Abstractions.Eforms;
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
                var count = await sdkDbContext.CheckLists
                    .Where(x => x.Id == id)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Include(x => x.Children)
                    .Select(x => x.Children)
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
                    Color = model.Color,
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
                };
                await newCheckList.Create(sdkDbContext);

                // create empty checkList
                var twoCheckList = new CheckList
                {
                    Color = model.Color,
                    DisplayIndex = 0,
                    ParentId = newCheckList.Id,
                    ReviewEnabled = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
                };
                await twoCheckList.Create(sdkDbContext);

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
                    var twoCheckListTranslation = new CheckListTranslation
                    {
                        CheckListId = twoCheckList.Id,
                        LanguageId = translation.LanguageId,
                        Text = translation.Name,
                        Description = translation.Description
                    };

                    await newCheckListTranslation.Create(sdkDbContext);
                    await twoCheckListTranslation.Create(sdkDbContext);
                }

                // add tags to eform
                foreach (var tag in model.TagIds.Select(tagId => new Tagging { CheckListId = newCheckList.Id, TagId = tagId }))
                {
                    await tag.Create(sdkDbContext);
                }

                // add fields to eform
                await CreateFields(twoCheckList.Id, sdkDbContext, model.Fields);

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
                    if (translation.Text != translationsModel.Name ||
                        translation.Description != translationsModel.Description) // check if update is need
                    {
                        translation.Text = translationsModel.Name;
                        translation.Description = translationsModel.Description;
                        await translation.Update(sdkDbContext);
                    }

                    var translationForUpdate = parentEform?.Translations.FirstOrDefault(x =>
                        x.LanguageId == translationsModel.LanguageId && x.Text != translationsModel.Name);
                    if (translationForUpdate != null)
                    {
                        translationForUpdate.Text = translationsModel.Name;
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

                // delete removed field
                await DeleteFields(model.FieldForDelete, sdkDbContext);

                // update fields
                await UpdateFields(model.FieldForUpdate, sdkDbContext);

                var fieldForCreateOnThisCheckList = model.FieldForCreate
                        .Where(x => x.ChecklistId == dbEform.Id)
                        .ToList();
                // create new field
                await CreateFields(dbEform.Id, sdkDbContext, fieldForCreateOnThisCheckList);

                await CreateChecklist(model, sdkDbContext);

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

        private static async Task UpdateFields(List<VisualEditorFields> fieldsForUpdate, MicrotingDbContext sdkDbContext)
        {
            foreach (var fieldForUpdate in fieldsForUpdate)
            {
                var fieldFromDb = await sdkDbContext.Fields
                    .Where(x => x.Id == fieldForUpdate.Id)
                    .Where(x => x.WorkflowState != Constants.WorkflowStates.Removed)
                    .Include(x => x.Translations)
                    .Include(x => x.FieldOptions)
                    .FirstAsync();

                fieldFromDb.Color = fieldForUpdate.Color;
                fieldFromDb.FieldTypeId = fieldForUpdate.FieldType;
                fieldFromDb.Mandatory = Convert.ToInt16(fieldForUpdate.Mandatory);
                fieldFromDb.DecimalCount = fieldForUpdate.DecimalCount;
                fieldFromDb.DisplayIndex = fieldForUpdate.Position;
                fieldFromDb.MaxValue = fieldForUpdate.MaxValue;
                fieldFromDb.MinValue = fieldForUpdate.MinValue;
                fieldFromDb.DefaultValue = fieldForUpdate.Value;
                // todo add specific behaviour for some fields

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
                        new CommonTranslationsModel
                        {
                            Id = x.LanguageId,
                            Description = x.Description,
                            Name = x.Text,
                            LanguageId = x.LanguageId,
                        }).ToList(),
                    Mandatory = Convert.ToBoolean(field.Mandatory),
                    ChecklistId = (int) field.CheckListId,
                };

                switch (field.FieldType.Type)
                {
                    case Constants.FieldTypes.Number or Constants.FieldTypes.NumberStepper:
                        {
                            editorField.DecimalCount = (int)field.DecimalCount;
                            editorField.MinValue = long.Parse(field.MinValue);
                            editorField.MaxValue = long.Parse(field.MaxValue);
                            editorField.Value = long.Parse(field.DefaultValue);
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
                            editorField.MinValue = DateTime.Parse(field.MinValue);
                            editorField.MaxValue = DateTime.Parse(field.MaxValue);
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
            List<VisualEditorFields> fieldsList, int? parentFieldId = null)
        {
            foreach (var field in fieldsList)
            {
                var dbField = new Field
                {
                    CheckListId = eformId,
                    Color = field.Color,
                    FieldTypeId = field.FieldType,
                    DecimalCount = field.DecimalCount,
                    DefaultValue = field.Value,
                    DisplayIndex = field.Position,
                    MaxValue = field.MaxValue,
                    MinValue = field.MinValue,
                    Mandatory = Convert.ToInt16(field.Mandatory),
                    ParentFieldId = parentFieldId,
                };
                await dbField.Create(sdkDbContext);

                var fieldType = await sdkDbContext.FieldTypes
                    .Where(x => x.Id == field.FieldType)
                    .Select(x => x.Type)
                    .FirstAsync();

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
                                foreach (var optionTranslation in dbOption.FieldOptionTranslations)
                                {
                                    optionTranslation.FieldOptionId = dbOption.Id;
                                    await optionTranslation.Create(sdkDbContext);
                                }
                            }
                            break;
                        }
                    case Constants.FieldTypes.FieldGroup:
                        {
                            await CreateFields(eformId, sdkDbContext, field.Fields, dbField.Id);
                            break;
                        }
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
                    Position = (int) x.DisplayIndex,
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
                .Select(x => x.Children.Select(y => y.Id).ToList())
                .FirstAsync();

            foreach (var checkListId in childrenCheckListIds)
            {
                eform.CheckLists.Add(await FindTemplates(checkListId, sdkDbContext));
            }

            return eform;
        }

        private static async Task CreateChecklist(EformVisualEditorUpdateModel model, MicrotingDbContext sdkDbContext)
        {
            foreach (var checklistForCreate in model.ChecklistForCreate)
            {
                // create checkList
                var newCheckList = new CheckList
                {
                    Color = checklistForCreate.Color,
                    DisplayIndex = 0,
                    ParentId = checklistForCreate.ParentId,
                    ReviewEnabled = 0,
                    ExtraFieldsEnabled = 0,
                    DoneButtonEnabled = 0,
                    ApprovalEnabled = 0,
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

                await CreateFields(newCheckList.Id, sdkDbContext, fieldsForCreate);

                foreach (var eformVisualEditorModel in model.ChecklistForCreate.Where(x => x.ParentId == checklistForCreate.TempId))
                {
                    eformVisualEditorModel.ParentId = newCheckList.Id;
                }
            }
        }
    }
}
