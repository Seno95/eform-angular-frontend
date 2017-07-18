﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using eFormAPI.Web.Infrastructure.Helpers;
using eFormCore;
using eFormShared;
using eFromAPI.Common.API;
using eFromAPI.Common.Models;

namespace eFormAPI.Web.Controllers
{
    public class WorkersController : ApiController
    {
        private readonly EFormCoreHelper _coreHelper = new EFormCoreHelper();

        [HttpGet]
        public OperationDataResult<List<Worker_Dto>> Index()
        {
            Core core = _coreHelper.GetCore();
            var workersDto = core.Advanced_WorkerReadAll("not_removed", null, null);

            return new OperationDataResult<List<Worker_Dto>>(true, workersDto);
        }

        [HttpGet]
        public OperationDataResult<Worker_Dto> Edit(int id)
        {
            Core core = _coreHelper.GetCore();
            var workerDto = core.Advanced_WorkerRead(id);

            return new OperationDataResult<Worker_Dto>(true, workerDto);
        }

        [HttpPost]
        public OperationResult Update(WorkerModel workerModel)
        {
            try
            {
                Core core = _coreHelper.GetCore();
                var workerDto = core.Advanced_WorkerRead(workerModel.Id);
                bool isUpdated = core.Advanced_WorkerUpdate(workerModel.Id, workerModel.UserFirstName,
                    workerModel.UserLastName, workerDto.Email);

                return isUpdated
                    ? new OperationResult(true, $"Worker with id {workerModel.Id} was updated!")
                    : new OperationResult(false, $"Worker with id {workerModel.Id} wasn't updated!");
            }
            catch (Exception)
            {
                return new OperationResult(false, $"Worker with id {workerModel.Id} can't be updated!");
            }
        }

        [HttpGet]
        public OperationResult Delete(int id)
        {
            try
            {
                Core core = _coreHelper.GetCore();
                var workerDto = core.Advanced_WorkerRead(id);

                if (workerDto.Equals(null))
                {
                    return new OperationDataResult<SiteNameModel>(false, $"Site with id {id} could not be deleted!");
                }

                return core.Advanced_WorkerDelete(id)
                    ? new OperationDataResult<SiteNameModel>(true,
                        $"Worker \"{workerDto.FirstName} {workerDto.LastName}\" " +
                        "deleted successfully")
                    : new OperationDataResult<SiteNameModel>(false,
                        $"Worker \"{workerDto.FirstName} {workerDto.LastName}\" " +
                        "could not be deleted!");
            }

            catch (Exception)
            {
                return new OperationDataResult<SiteNameModel>(false,
                    $"Site with id {id} could not be deleted!");
            }
        }
    }
}