import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subject, Subscription} from 'rxjs';
import {debounceTime} from 'rxjs/operators';
import {CodeIcon, CsvIcon, ExcelIcon, FileUploadIcon, UserClaimsEnum, WordIcon} from 'src/app/common/const';
import {
  SavedTagModel,
  TemplateListModel,
  EformPermissionsSimpleModel,
  TemplateDto,
  CommonDictionaryModel,
} from 'src/app/common/models';
import {
  SecurityGroupEformsPermissionsService,
  EFormService,
  EformTagService,
  AuthService,
} from 'src/app/common/services';
import {saveAs} from 'file-saver';
import {EformsStateService} from '../../store';
import {AuthStateService} from 'src/app/common/store';
import {Sort} from '@angular/material/sort';
import {MatIconRegistry} from '@angular/material/icon';
import {DomSanitizer} from '@angular/platform-browser';
import {MatDialog} from '@angular/material/dialog';
import {EformsTagsComponent} from 'src/app/common/modules/eform-shared-tags/components';
import {
  EformColumnsModalComponent,
  EformCreateModalComponent,
  EformDuplicateConfirmModalComponent,
  EformEditParingModalComponent,
  EformEditTagsModalComponent,
  EformRemoveEformModalComponent,
  EformsBulkImportModalComponent,
  EformUploadZipModalComponent,
} from '../../components';
import {AutoUnsubscribe} from 'ngx-auto-unsubscribe';
import {Overlay} from '@angular/cdk/overlay';
import {dialogConfigHelper} from 'src/app/common/helpers';
import {MtxGridColumn} from '@ng-matero/extensions/grid';

@AutoUnsubscribe()
@Component({
  selector: 'app-eform-page',
  templateUrl: './eforms-page.component.html',
  styleUrls: ['./eforms-page.component.scss'],
})
export class EformsPageComponent implements OnInit, OnDestroy {
  @ViewChild('modalTags', {static: true}) modalTags: EformsTagsComponent;

  searchSubject = new Subject();
  templateListModel: TemplateListModel = new TemplateListModel();
  eformPermissionsSimpleModel: Array<EformPermissionsSimpleModel> = [];
  availableTags: Array<CommonDictionaryModel> = [];

  eformsBulkImportModalAfterClosedSub$: Subscription;
  downloadCSVFileSub$: Subscription;
  downloadEformXMLSub$: Subscription;
  getEformsSimplePermissionsSub$: Subscription;
  getSavedTagsSub$: Subscription;
  addSavedTagSub$: Subscription;
  deleteSavedTagSub$: Subscription;
  getAvailableTagsSub$: Subscription;
  loadAllTemplatesSub$: Subscription;
  searchSubjectSub$: Subscription;
  eformCreateModalComponentAfterClosedSub$: Subscription;
  eformDuplicateConfirmModalComponentAfterClosedSub$: Subscription;
  eformEditTagsModalComponentAfterClosedSub$: Subscription;
  eformUploadZipModalComponentAfterClosedSub$: Subscription;
  eformColumnsModalComponentAfterClosedSub$: Subscription;
  eformEditParingModalComponentAfterClosedSub$: Subscription;
  eformRemoveEformModalComponentAfterClosedSub$: Subscription;

  get userClaims() {
    return this.authStateService.currentUserClaims;
  }

  get userClaimsEnum() {
    return UserClaimsEnum;
  }

  tableHeaders: MtxGridColumn[] = [
    {header: 'Id', field: 'id', sortProp: {id: 'Id'}, sortable: true, type: 'number'},
    {header: 'CreatedAt', sortProp: {id: 'CreatedAt'}, field: 'createdAt', sortable: true},
    {
      header: 'Label',
      field: 'label',
      sortable: true,
      sortProp: {id: 'Text'},
    },
    {
      header: 'Description',
      field: 'description',
      sortProp: {id: 'Description'},
      sortable: true,
    },
    {header: 'Tags', field: 'tags'},
    {header: 'Pairing', field: 'pairingUpdate'},
    {header: 'Actions', field: 'actions'},
  ];

  constructor(
    private eFormService: EFormService,
    private eFormTagService: EformTagService,
    private authService: AuthService,
    private securityGroupEformsService: SecurityGroupEformsPermissionsService,
    public eformsStateService: EformsStateService,
    public authStateService: AuthStateService,
    iconRegistry: MatIconRegistry,
    sanitizer: DomSanitizer,
    public dialog: MatDialog,
    private overlay: Overlay
  ) {
    iconRegistry.addSvgIconLiteral('file-word', sanitizer.bypassSecurityTrustHtml(WordIcon));
    iconRegistry.addSvgIconLiteral('file-code', sanitizer.bypassSecurityTrustHtml(CodeIcon));
    iconRegistry.addSvgIconLiteral('file-csv', sanitizer.bypassSecurityTrustHtml(CsvIcon));
    iconRegistry.addSvgIconLiteral('file-upload', sanitizer.bypassSecurityTrustHtml(FileUploadIcon));
    iconRegistry.addSvgIconLiteral('file-excel', sanitizer.bypassSecurityTrustHtml(ExcelIcon));
    this.searchSubjectSub$ = this.searchSubject.pipe(debounceTime(500)).subscribe((val: string) => {
      this.eformsStateService.updateNameFilter(val);
      this.loadAllTags();
    });
  }

  ngOnInit() {
    this.loadEformsPermissions();
    this.loadAllTags();
  }

  ngOnDestroy() {
  }

  loadAllTemplates() {
    this.loadAllTemplatesSub$ = this.eformsStateService.loadAllTemplates()
      .subscribe((operation) => {
        if (operation && operation.success) {
          this.templateListModel = operation.model;
        }
      });
  }

  loadAllTags() {
    // load tags after call load templates (not know why)
    if (this.userClaims.eformsReadTags) {
      this.getAvailableTagsSub$ = this.eFormTagService.getAvailableTags()
        .subscribe((data) => {
          if (data && data.success) {
            this.availableTags = data.model;
            this.loadSelectedUserTags();
          }
        });
    } else {
      this.loadAllTemplates();
    }
  }

  saveTag(e: any) {
    const savedTagModel = new SavedTagModel();
    savedTagModel.tagId = e.id;
    savedTagModel.tagName = e.name;
    this.addSavedTagSub$ = this.eFormTagService.addSavedTag(savedTagModel).subscribe((data) => {
      if (data && data.success) {
        this.eformsStateService.addOrRemoveTagIds(e.id);
        this.loadAllTemplates();
      }
    });
  }

  removeSavedTag(e: any) {
    this.deleteSavedTagSub$ = this.eFormTagService.deleteSavedTag(e.value.id).subscribe((data) => {
      if (data && data.success) {
        this.eformsStateService.addOrRemoveTagIds(e.value.id);
        this.loadAllTemplates();
      }
    });
  }

  loadSelectedUserTags() {
    this.getSavedTagsSub$ = this.eFormTagService.getSavedTags().subscribe((data) => {
      if (data && data.success) {
        if (data.model.tagList.length > 0) {
          this.eformsStateService.updateTagIds(
            data.model.tagList.map((x) => x.tagId)
          );
        }
        this.loadAllTemplates();
      }
    });
  }

  loadEformsPermissions() {
    this.getEformsSimplePermissionsSub$ = this.securityGroupEformsService
      .getEformsSimplePermissions()
      .subscribe((data) => {
        if (data && data.success) {
          this.eformPermissionsSimpleModel = this.securityGroupEformsService.mapEformsSimplePermissions(
            data.model
          );
        }
      });
  }

  onLabelInputChanged(label: string) {
    this.searchSubject.next(label);
  }

  sortTable(sort: Sort) {
    this.eformsStateService.onSortTable(sort.active);
    this.loadAllTemplates();
  }

  openNewEformModal() {
    this.eformCreateModalComponentAfterClosedSub$ = this.dialog.open(EformCreateModalComponent, {
      ...dialogConfigHelper(this.overlay, this.availableTags),
      minWidth: 400,
    }).afterClosed().subscribe(data => data ? this.loadAllTags() : undefined);
  }

  openEditColumnsModal(templateDto: TemplateDto) {
    this.eformColumnsModalComponentAfterClosedSub$ = this.dialog.open(EformColumnsModalComponent, {
      ...dialogConfigHelper(this.overlay, templateDto), minWidth: 400,
    }).afterClosed().subscribe(data => data ? undefined : undefined);
  }

  openEformDeleteModal(templateDto: TemplateDto) {
    this.eformRemoveEformModalComponentAfterClosedSub$ = this.dialog.open(EformRemoveEformModalComponent, {
      ...dialogConfigHelper(this.overlay, templateDto),
    }).afterClosed().subscribe(data => data ? this.loadAllTemplates() : undefined);
  }

  uploadZipFile(templateDto: TemplateDto) {
    this.eformUploadZipModalComponentAfterClosedSub$ = this.dialog.open(EformUploadZipModalComponent, {
      ...dialogConfigHelper(this.overlay, {availableTags: this.availableTags, selectedTemplate: templateDto}), minWidth: 400,
    }).afterClosed().subscribe(data => data ? undefined : undefined);
  }

  downloadItem(itemName: string, templateId: number) {
    if (itemName === 'XML') {
      this.downloadEformXMLSub$ = this.eFormService.downloadEformXML(templateId).subscribe((data) => {
        const blob = new Blob([data]);
        saveAs(blob, `eForm_${templateId}.xml`);
      });
    } else {
      this.downloadCSVFileSub$ = this.eFormService.downloadCSVFile(templateId).subscribe((data) => {
        const blob = new Blob([data]);
        saveAs(blob, `eForm_${templateId}.csv`);
      });
    }
  }

  openPairingModal(templateDto: TemplateDto) {
    this.eformEditParingModalComponentAfterClosedSub$ = this.dialog.open(EformEditParingModalComponent, {
      ...dialogConfigHelper(this.overlay, templateDto), minWidth: 600,
    }).afterClosed().subscribe(data => data ? this.loadAllTemplates() : undefined);
  }

  openEditTagsModal(templateDto: TemplateDto) {
    this.eformEditTagsModalComponentAfterClosedSub$ = this.dialog.open(EformEditTagsModalComponent, {
      ...dialogConfigHelper(this.overlay, {availableTags: this.availableTags, selectedTemplate: templateDto}),
      minWidth: 400,
    }).afterClosed().subscribe(data => data ? this.loadAllTemplates() : undefined);
  }

  openEformsImportModal() {
    this.eformsBulkImportModalAfterClosedSub$ = this.dialog.open(EformsBulkImportModalComponent, {
      ...dialogConfigHelper(this.overlay, this.availableTags),
      minWidth: 400,
    }).afterClosed().subscribe(data => data ? this.loadAllTags() : undefined);
  }

  checkEformPermissions(templateId: number, permissionIndex: number) {
    const foundEform = this.eformPermissionsSimpleModel.find(
      (x) => x.templateId === templateId
    );
    if (foundEform) {
      return foundEform.permissionsSimpleList.find(
        (x) => x === UserClaimsEnum[permissionIndex].toString()
      );
    } else {
      return this.userClaims[UserClaimsEnum[permissionIndex].toString()];
    }
  }

  openTagsModal() {
    this.modalTags.show();
  }

  openDuplicateConfirmModal(templateDto: TemplateDto) {
    this.eformDuplicateConfirmModalComponentAfterClosedSub$ = this.dialog.open(EformDuplicateConfirmModalComponent, {
      ...dialogConfigHelper(this.overlay, templateDto),
    }).afterClosed().subscribe(data => data ? this.loadAllTemplates() : undefined);
  }
}
