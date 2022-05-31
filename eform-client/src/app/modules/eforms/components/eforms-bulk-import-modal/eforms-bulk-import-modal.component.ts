import {Component, ElementRef, EventEmitter, OnInit, Output, ViewChild,} from '@angular/core';
import {FileUploader} from 'ng2-file-upload';
import {ToastrService} from 'ngx-toastr';
import {TranslateService} from '@ngx-translate/core';
import {LoaderService} from 'src/app/common/services/loader.service';
import {AuthStateService} from 'src/app/common/store';
import {MatDialogRef} from '@angular/material/dialog';

@Component({
  selector: 'app-eforms-bulk-import-modal',
  templateUrl: './eforms-bulk-import-modal.component.html',
  styleUrls: ['./eforms-bulk-import-modal.component.scss'],
})
export class EformsBulkImportModalComponent implements OnInit {
  @ViewChild('xlsxEforms', {static: false})
  xlsxEformsInput: ElementRef;
  xlsxEformsFileUploader: FileUploader = new FileUploader({
    url: '/api/templates/import',
    authToken: this.authStateService.bearerToken,
  });
  errors: { row: number; col: number; message: string }[];

  constructor(
    private toastrService: ToastrService,
    private translateService: TranslateService,
    private loaderService: LoaderService,
    private authStateService: AuthStateService,
    public dialogRef: MatDialogRef<EformsBulkImportModalComponent>,
  ) {
  }

  ngOnInit() {
    this.xlsxEformsFileUploader.clearQueue();
    this.xlsxEformsFileUploader.onSuccessItem = (item, response) => {
      const model = JSON.parse(response).model;
      if (model) {
        this.errors = JSON.parse(response).model.errors;
        this.xlsxEformsFileUploader.clearQueue();
      }
      if (this.errors && this.errors.length > 0) {
        this.toastrService.warning(
          this.translateService.instant('Import has been finished partially')
        );
      } else {
        this.xlsxEformsFileUploader.clearQueue();
        this.toastrService.success(
          this.translateService.instant('Import has been finished successfully')
        );
        this.excelEformsModal(true);
      }
      this.loaderService.isLoading.next(false);
      this.xlsxEformsInput.nativeElement.value = '';
    };
    this.xlsxEformsFileUploader.onErrorItem = () => {
      this.xlsxEformsFileUploader.clearQueue();
      this.toastrService.error(
        this.translateService.instant('Error while making import')
      );
      this.xlsxEformsInput.nativeElement.value = '';
    };
    this.xlsxEformsFileUploader.onAfterAddingFile = (f) => {
      if (this.xlsxEformsFileUploader.queue.length > 1) {
        this.xlsxEformsFileUploader.removeFromQueue(
          this.xlsxEformsFileUploader.queue[0]
        );
      }
      this.errors = [];
    };
  }

  uploadExcelEformsFile() {
    this.xlsxEformsFileUploader.queue[0].upload();
    this.loaderService.isLoading.next(true);
  }

  excelEformsModal(result = false) {
    this.dialogRef.close(result);
    this.xlsxEformsFileUploader.clearQueue();
  }
}
