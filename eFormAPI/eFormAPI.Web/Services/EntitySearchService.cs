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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eFormAPI.Web.Abstractions;
using eFormAPI.Web.Abstractions.Advanced;
using eFormAPI.Web.Infrastructure.Database;
using eFormAPI.Web.Infrastructure.Models;
using eFormAPI.Web.Infrastructure.Models.SearchableList;
using Microsoft.EntityFrameworkCore;
using Microting.eForm.Infrastructure.Constants;
using Microting.eFormApi.BasePn.Abstractions;
using Microting.eFormApi.BasePn.Infrastructure.Models.API;
using Microting.eFormApi.BasePn.Infrastructure.Models.Common;

namespace eFormAPI.Web.Services
{
    public class EntitySearchService : IEntitySearchService
    {
        private readonly BaseDbContext _dbContext;
        private readonly IEFormCoreService _coreHelper;
        private readonly ILocalizationService _localizationService;

        public EntitySearchService(BaseDbContext dbContext,
            IEFormCoreService coreHelper, 
            ILocalizationService localizationService)
        {
            _dbContext = dbContext;
            _coreHelper = coreHelper;
            _localizationService = localizationService;
        }


        public async Task<OperationDataResult<EntityGroupList>> Index(
            AdvEntitySearchableGroupListRequestModel requestModel)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                EntityGroupList model = await core.Advanced_EntityGroupAll(requestModel.Sort, requestModel.NameFilter,
                    requestModel.PageIndex, requestModel.PageSize, Constants.FieldTypes.EntitySearch,
                    requestModel.IsSortDsc,
                    Constants.WorkflowStates.NotRemoved);
                if (model != null)
                {
                    List<string> plugins = await _dbContext.EformPlugins.Select(x => x.PluginId).ToListAsync();
                    foreach (EntityGroup entityGroup in model.EntityGroups)
                    {
                        foreach (string plugin in plugins)
                        {
                            if (entityGroup.Name.Contains(plugin))
                            {
                                entityGroup.IsLocked = true;
                            }
                        }
                    }
                }
                
                return new OperationDataResult<EntityGroupList>(true, model);
            }
            catch (Exception)
            {
                return new OperationDataResult<EntityGroupList>(false,
                    _localizationService.GetString("SearchableListLoadingFailed"));
            }
        }

        public async Task<OperationResult> Create(AdvEntitySearchableGroupEditModel editModel)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                var groupCreate = await core.EntityGroupCreate(Constants.FieldTypes.EntitySearch, editModel.Name);
                if (editModel.AdvEntitySearchableItemModels.Any())
                {
                    var entityGroup = await core.EntityGroupRead(groupCreate.MicrotingUUID);
                    var nextItemUid = entityGroup.EntityGroupItemLst.Count;
                    foreach (var entityItem in editModel.AdvEntitySearchableItemModels)
                    {
                        await core.EntitySearchItemCreate(entityGroup.Id, entityItem.Name, entityItem.Description,
                            nextItemUid.ToString());

                        //entityGroup.EntityGroupItemLst.Add(new EntityItem(entityItem.Name,
                        //    entityItem.Description, nextItemUid.ToString(), Constants.WorkflowStates.Created));
                        nextItemUid++;
                    }

                    //core.EntityGroupUpdate(entityGroup);
                }

                return new OperationResult(true,
                    _localizationService.GetStringWithFormat("ParamCreatedSuccessfully", groupCreate.MicrotingUUID));
            }
            catch (Exception)
            {
                return new OperationResult(false, _localizationService.GetString("SearchableListCreationFailed"));
            }
        }

        public async Task<OperationResult> Update(AdvEntitySearchableGroupEditModel editModel)
        {
            try
            {
                var core = await _coreHelper.GetCore();
                var entityGroup = await core.EntityGroupRead(editModel.GroupUid);

                if (entityGroup.Name != editModel.Name)
                {
                    entityGroup.Name = editModel.Name;
                    await core.EntityGroupUpdate(entityGroup);
                }

                var currentIds = new List<int>();

                foreach (var entityItem in editModel.AdvEntitySearchableItemModels)
                {
                    if (string.IsNullOrEmpty(entityItem.MicrotingUUID))
                    {
                        var et = await core.EntitySearchItemCreate(entityGroup.Id, entityItem.Name,
                            entityItem.Description, entityItem.DisplayIndex.ToString());
                        currentIds.Add(et.Id);
                    }
                    else
                    {
                        await core.EntityItemUpdate(entityItem.Id, entityItem.Name, entityItem.Description,
                            entityItem.DisplayIndex.ToString(), entityItem.DisplayIndex);
                        currentIds.Add(entityItem.Id);
                    }
                }

                foreach (var entityItem in entityGroup.EntityGroupItemLst)
                {
                    if (!currentIds.Contains(entityItem.Id))
                    {
                        await core.EntityItemDelete(entityItem.Id);
                    }
                }

                return new OperationResult(true,
                    _localizationService.GetStringWithFormat("ParamUpdatedSuccessfully", editModel.GroupUid));
            }
            catch (Exception)
            {
                return new OperationResult(false, _localizationService.GetString("SearchableListCreationFailed"));
            }
        }

        public async Task<OperationDataResult<EntityGroup>> Read(string entityGroupUid)
        {
            try
            {
                var core = await _coreHelper.GetCore();

                EntityGroup entityGroup = await core.EntityGroupRead(entityGroupUid);

                List<string> plugins = await _dbContext.EformPlugins.Select(x => x.PluginId).ToListAsync();
                
                foreach (string plugin in plugins)
                {
                    if (entityGroup.Name.Contains(plugin))
                    {
                        entityGroup.IsLocked = true;
                    }
                }
                
                return new OperationDataResult<EntityGroup>(true, entityGroup);
            }
            catch (Exception)
            {
                return new OperationDataResult<EntityGroup>(false,
                    _localizationService.GetString("ErrorWhenObtainingSearchableList"));
            }
        }

        public async Task<OperationDataResult<List<CommonDictionaryTextModel>>> GetEntityGroupDictionary(string entityGroupUid,
            string searchString)
        {
            try
            {
                var core = await _coreHelper.GetCore();

                var entityGroup = await core.EntityGroupRead(entityGroupUid, "Name", searchString);

                var mappedEntityGroupDict = new List<CommonDictionaryTextModel>();

                foreach (var entityGroupItem in entityGroup.EntityGroupItemLst)
                {
                    mappedEntityGroupDict.Add(new CommonDictionaryTextModel()
                    {
                        Id = entityGroupItem.Id.ToString(),
                        Text = entityGroupItem.Name
                    });
                }

                return new OperationDataResult<List<CommonDictionaryTextModel>>(true, mappedEntityGroupDict);
            }
            catch (Exception)
            {
                return new OperationDataResult<List<CommonDictionaryTextModel>>(false,
                    _localizationService.GetString("ErrorWhenObtainingSearchableList"));
            }
        }

        public async Task<OperationResult> Delete(string entityGroupUid)
        {
            try
            {
                var core = await _coreHelper.GetCore();


                return await core.EntityGroupDelete(entityGroupUid)
                    ? new OperationResult(true, _localizationService.GetStringWithFormat("ParamDeletedSuccessfully", entityGroupUid))
                    : new OperationResult(false, _localizationService.GetString("ErrorWhenDeletingSearchableList"));
            }
            catch (Exception)
            {
                return new OperationResult(false, _localizationService.GetString("ErrorWhenDeletingSearchableList"));
            }
        }

        public async Task<OperationResult> SendSearchableGroup(string entityGroupUid)
        {
            try
            {
                var core = await _coreHelper.GetCore();


                return new OperationResult(true, _localizationService.GetStringWithFormat("ParamDeletedSuccessfully", entityGroupUid));
            }
            catch (Exception)
            {
                return new OperationResult(false, _localizationService.GetString("ErrorWhenDeletingSearchableList"));
            }
        }
    }
}