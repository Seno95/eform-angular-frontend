import {
  EformVisualEditorFieldModel,
  EformVisualEditorModel,
} from 'src/app/common/models';

export class EformVisualEditorRecursionModel {
  fieldIndex?: number;
  checklistIndex?: number;
  checklistRecursionIndex?: number;
  checklistRecursionIndexes?: number[] = [];
}

export class EformVisualEditorRecursionFieldModel extends EformVisualEditorRecursionModel {
  field?: EformVisualEditorFieldModel = new EformVisualEditorFieldModel();
  fieldPosition?: number;
  checklistId?: number;
}

export class EformVisualEditorRecursionChecklistModel extends EformVisualEditorRecursionModel {
  checklist?: EformVisualEditorModel = new EformVisualEditorModel();
}

export class EformVisualEditorFieldsDnDRecursionModel {
  fields: EformVisualEditorFieldModel[] = [];
  checklistIndex?: number;
  checklistRecursionIndex: number;
  checklistRecursionIndexes?: number[] = [];
}
