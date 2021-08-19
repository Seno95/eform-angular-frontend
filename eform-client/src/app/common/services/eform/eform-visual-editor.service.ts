import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  EformVisualEditorModel,
  EformVisualEditorUpdateModel,
  OperationDataResult,
  OperationResult,
} from 'src/app/common/models';
import { ApiBaseService } from 'src/app/common/services';

const TemplateVisualEditorMethods = {
  VisualEditor: '/api/template-visual-editor/',
};

@Injectable({
  providedIn: 'root',
})
export class EformVisualEditorService {
  constructor(private apiBaseService: ApiBaseService) {}

  getVisualEditorTemplate(
    id: number
  ): Observable<OperationDataResult<EformVisualEditorModel>> {
    return this.apiBaseService.get(TemplateVisualEditorMethods.VisualEditor, {
      id: id,
    });
  }

  createVisualEditorTemplate(
    model: EformVisualEditorModel
  ): Observable<OperationResult> {
    const formData = ApiBaseService.objectToFormData(model, true);
    return this.apiBaseService.postFormData(
      TemplateVisualEditorMethods.VisualEditor,
      formData
    );
  }

  updateVisualEditorTemplate(
    model: EformVisualEditorUpdateModel
  ): Observable<OperationResult> {
    const formData = ApiBaseService.objectToFormData(model, true);
    return this.apiBaseService.putFormData(
      TemplateVisualEditorMethods.VisualEditor,
      formData
    );
  }
}
